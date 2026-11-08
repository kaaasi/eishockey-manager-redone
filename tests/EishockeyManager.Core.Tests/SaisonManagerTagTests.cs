using System.Text.Json.Nodes;
using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur saison, Teil 4 (#349, R14-D4 = A): Merker „Managerverein spielt“ je Spieltag (em.exe Datei 0x36FEA–0x3700B, Sprung bei
/// 0x37028 vor den Wettbewerbsprüfungen 0x3702E–0x37085) als Einstellung <see cref="GameOptions.ManagerDayMinuteLoop"/>;
/// Vorgabe aus = Remake-Verhalten (Entscheidung je Paarung).
/// </summary>
public class SaisonManagerTagTests
{
    private static GameState Create(int seed, bool option, params int[] clubs)
    {
        var setup = new GameSetup { Duration = GameDuration.Unbegrenzt, EuropeAndWorldCup = true };
        for (var i = 0; i < clubs.Length; i++)
            setup.Managers.Add(new ManagerSetup { Name = $"M{i}", Head = i, ClubIndex = clubs[i], StartLeague = (League)(clubs[0] / 12) });
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, seed);
        game.Options.ShowDebCup = false;      // Pokal ohne Managerverein: schneller Weg 0x3731F …
        game.Options.ShowEuropeanCup = false;
        game.Options.ManagerDayMinuteLoop = option; // … es sei denn, der Merker greift
        return game;
    }

    private static void Hold(GameState game)
    {
        foreach (var m in game.Managers)
        {
            m.Money = 10_000_000;
            TestSeason.KeepSquad(game, m);
        }
    }

    /// <summary>
    /// Spielt bis zu einem Tag, an dem der Manager einen Wettbewerb außer der Liga bestreitet; liefert den Zufallszustand
    /// nach diesem Tag (ein <see cref="GameState.Continue"/>).
    /// </summary>
    private static (RngSnapshot AfterDay, int Day) PlayManagerCupDay(GameState game, Competition kind)
    {
        for (var guard = 0; guard < 5000; guard++)
        {
            Hold(game);
            if (game.Phase == SeasonPhase.Running
                && game.MatchOn(game.CurrentDay, game.Managers[0].ClubIndex) is { } m && m.Competition == kind)
            {
                var day = game.CurrentDay;
                game.Continue();
                return (game.CaptureRng(), day);
            }
            if (game.Phase is SeasonPhase.SeasonOver or SeasonPhase.GameOver)
                break;
            game.Continue();
        }
        throw new InvalidOperationException($"Kein Tag mit {kind} für den Manager gefunden.");
    }

    /// <summary>Wie <see cref="PlayManagerCupDay"/>, zählt aber schnellen Weg und Minutenschleife des einen Tages.</summary>
    private static (int Fast, int Loop, int Day) CountManagerCupDay(GameState game, Competition kind)
    {
        for (var guard = 0; guard < 5000; guard++)
        {
            Hold(game);
            if (game.Phase == SeasonPhase.Running
                && game.MatchOn(game.CurrentDay, game.Managers[0].ClubIndex) is { } m && m.Competition == kind)
            {
                var day = game.CurrentDay;
                OriginalMinutes.FastCupCalls = 0;
                OriginalMinutes.MinuteLoopCalls = 0;
                game.Continue();
                return (OriginalMinutes.FastCupCalls, OriginalMinutes.MinuteLoopCalls, day);
            }
            if (game.Phase is SeasonPhase.SeasonOver or SeasonPhase.GameOver)
                break;
            game.Continue();
        }
        throw new InvalidOperationException($"Kein Tag mit {kind} für den Manager gefunden.");
    }

    [Fact]
    public void ManagerPlaysCupDay_OptionOn_AllPairingsUseMinuteLoop_RandomDiffers()
    {
        const Competition kind = Competition.DebCup;
        // R14S-01/-02: Rot am Head 194f387 – der Merker wurde je Paarung neu gebildet und kippte, sobald das Spiel des Managers
        // gerechnet war (13 von 16 Paarungen am Tag 12 liefen im schnellen Weg). Bundesligist Köln (5), DEB-Pokal-Schalter aus.
        var off = Create(seed: 5, option: false, 5);
        var on = Create(seed: 5, option: true, 5);
        var (fastOff, loopOff, dayOff) = CountManagerCupDay(off, kind);
        var (fastOn, loopOn, dayOn) = CountManagerCupDay(on, kind);
        Assert.Equal(dayOff, dayOn);          // Liga ist von der Einstellung unberührt: gleicher Verlauf bis dahin
        Assert.True(fastOff > 0, "Vorgabe aus: Paarungen ohne Manager rechnen im schnellen Weg");
        Assert.Equal(0, fastOn);              // an: KEINE Paarung im schnellen Weg
        Assert.True(loopOn >= fastOff + loopOff, "alle Paarungen laufen durch die Minutenschleife");
        Assert.NotEqual(off.CaptureRng(), on.CaptureRng());
    }

    [Fact]
    public void NoManagerInvolved_OptionDoesNothing_SameRandom()
    {
        // R14S-02: Pokal-/Play-off-Tag, an dem KEIN Managerverein spielt (Köln ist ausgeschieden bzw. nicht dabei), mit
        // eingeschalteter Option: derselbe Zustand, einmal aus, einmal an → gleiche Zufallsfolge und gleich viele Paarungen im
        // schnellen Weg. Mutation „Bedingung Managerverein spielt entfernt“ (Option allein schaltet die Minutenschleife) macht
        // den Lauf „an“ minutenweise → rot. Zwei Klone desselben Zustands, damit frühere Unterschiede nichts verfälschen.
        var game = Create(seed: 5, option: false, 5);
        var checkedKinds = new HashSet<DayEvents>();
        for (var guard = 0; guard < 5000 && game.Phase is not (SeasonPhase.SeasonOver or SeasonPhase.GameOver); guard++)
        {
            Hold(game);
            var day = game.CurrentDay;
            // Continue() hält nur an Manager-Tagen; die Tage dazwischen (ohne Managerverein) laufen in einem Aufruf. Beginnt der
            // Aufruf an einem Ligatag des Managers (Wettbewerb unberührt), ist jeder Pokal-/Play-off-Tag darin einer ohne Manager.
            var own = game.MatchOn(day, game.Managers[0].ClubIndex);
            var kind = own is null ? DayEvents.None : DayEvents.League;
            if (game.Phase == SeasonPhase.Running && own is { Competition: Competition.League } && !game.RegularSeasonOver
                && checkedKinds.Count < 1)
            {
                var bytes = SaveGame.Save(game);
                var a = SaveGame.Load(bytes).Game!;
                var b = SaveGame.Load(bytes).Game!;
                b.Options.ManagerDayMinuteLoop = true;
                OriginalMinutes.FastCupCalls = 0;
                a.Continue();
                var fastA = OriginalMinutes.FastCupCalls;
                OriginalMinutes.FastCupCalls = 0;
                b.Continue();
                var fastB = OriginalMinutes.FastCupCalls;
                Assert.Equal(a.CaptureRng(), b.CaptureRng());
                Assert.Equal(fastA, fastB);
                if (fastA > 0)
                    checkedKinds.Add(kind);
            }
            game.Continue();
        }
        Assert.NotEmpty(checkedKinds); // mindestens ein Aufruf ohne Managerverein mit schnellem Weg gefunden
    }

    [Fact]
    public void Default_IsOff_AndSaveRoundTripKeepsIt()
    {
        var game = Create(seed: 3, option: false, 5);
        Assert.False(new GameOptions().ManagerDayMinuteLoop);
        game.Options.ManagerDayMinuteLoop = true;
        var loaded = SaveGame.Load(SaveGame.Save(game));
        Assert.True(loaded.Success, loaded.Error);
        Assert.True(loaded.Game!.Options.ManagerDayMinuteLoop);
        Assert.Equal(SaveGame.Save(game), SaveGame.Save(loaded.Game));
    }

    [Fact]
    public void Schema39Save_Migrates_OptionOff()
    {
        var game = Create(seed: 4, option: false, 5);
        var root = JsonNode.Parse(SaveGame.Save(game))!.AsObject();
        root["schema"] = 39;
        Assert.True(root["spiel"]!["Setup"]!["Options"]!.AsObject().Remove("ManagerDayMinuteLoop"));
        var loaded = SaveGame.Load(System.Text.Encoding.UTF8.GetBytes(root.ToJsonString()));
        Assert.True(loaded.Success, loaded.Error);
        Assert.False(loaded.Game!.Options.ManagerDayMinuteLoop);
    }

    [Theory]
    [InlineData(false, false)] // passiv, Einstellung aus
    [InlineData(false, true)]  // passiv, an
    [InlineData(true, false)]  // aktiv, aus
    [InlineData(true, true)]   // aktiv, an
    public void MultiSeason_FourSeasonChanges_BothSettings(bool active, bool option)
    {
        // Mehrsaisonprobe (AGENTS.md §5): 4 Saisonwechsel, passiver und aktiver Manager, beide Einstellungen, ohne Fehler;
        // Satzfolge der Ligen bleibt vollständig.
        var game = Create(seed: 21, option, 5, 30);
        for (var season = 0; season < 4; season++)
        {
            for (var guard = 0; game.Phase != SeasonPhase.SeasonOver; guard++)
            {
                Assert.True(guard < 5000, "Saisonende nicht erreicht");
                Hold(game);
                if (active && !game.RegularSeasonOver)
                    TestSeason.PlayMatchDay(game);
                else
                    game.Continue();
            }
            game.StartNextSeason();
            foreach (var league in Enum.GetValues<League>())
                Assert.Equal(GameState.ClubsPerLeague, game.LeagueSlots(league).Count);
        }
    }
}
