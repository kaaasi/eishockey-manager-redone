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

    [Fact]
    public void ManagerPlaysCupDay_OptionOn_AllPairingsUseMinuteLoop_RandomDiffers()
    {
        const Competition kind = Competition.DebCup;
        // Rot ohne Fix: Die Einstellung gab es nicht; beide Läufe verbrauchen gleich viel Zufall (je Paarung entschieden).
        // Bundesligist Köln (5) spielt im DEB-Pokal; Schalter 0x227 aus → ohne Merker schneller Weg
        // für die Paarungen ohne Manager.
        var off = Create(seed: 5, option: false, 5);
        var on = Create(seed: 5, option: true, 5);
        var (randomOff, dayOff) = PlayManagerCupDay(off, kind);
        var (randomOn, dayOn) = PlayManagerCupDay(on, kind);
        Assert.Equal(dayOff, dayOn);                // Liga ist von der Einstellung unberührt: gleicher Verlauf bis dahin
        Assert.NotEqual(randomOff, randomOn);       // Paarungen ohne Manager rechnen Minute für Minute (anderer Zufallsverbrauch)
    }

    [Fact]
    public void NoManagerInvolved_OptionDoesNothing_SameRandom()
    {
        // Tag mit Wettbewerb, an dem der Manager nicht spielt, ändert die Einstellung nichts: gesamte Saison bis zum
        // Vereinssaisonende ohne Managerverein im Pokal – Manager in der Oberliga, Pokal-Tage ohne ihn sind selten; wir
        // vergleichen darum nur, dass ein Lauf mit Einstellung aus und einer an bis zum ersten Pokaltag des Managers
        // dieselbe Zufallsfolge haben (Ligaspiele rechnen immer Minute für Minute).
        var off = Create(seed: 9, option: false, 30);
        var on = Create(seed: 9, option: true, 30);
        for (var i = 0; i < 20; i++)
        {
            Hold(off);
            Hold(on);
            if (off.MatchOn(off.CurrentDay, 30) is { } m && m.Competition != Competition.League)
                break;
            off.Continue();
            on.Continue();
            Assert.Equal(off.CaptureRng(), on.CaptureRng());
        }
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
