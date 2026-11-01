using System.Text.Json.Nodes;
using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur nationalteam, Teil taktik (#192, WD-01): Taktik des Nationalteams ohne Startwert.
/// Befund vorher: Unter „AUTOM.“ zeigte die Taktik des Bundestrainers schon vor dem ersten Spiel 60/0/25/20/30/0 und
/// 0/4/5/6/7/4, die eigenen Werte begannen mit derselben Zeile, und jede Ernennung begann neu. Beleg: DOSBox
/// `dosbox/nur-weltkugel/26_*.png` (Modus 1 nach „auto“, vor dem ersten WM-Spiel: alle Werte 0) und em.exe 0x8365
/// (`docs/dosbox-befunde.md`, Zeile „Taktik des Nationalteams zu Beginn alle 0“).
/// </summary>
public class NationalteamTests
{
    private static readonly ScoreSituation[] AllSituations = Enum.GetValues<ScoreSituation>();

    private static int[] ShownEffort(Tactics t) => AllSituations.Select(s => t.ShownEffort(0, s)).ToArray();

    private static int[] ShownAttack(Tactics t) => AllSituations.Select(s => t.ShownAttack(0, s)).ToArray();

    private static int[] AutoEffortLine(bool even) =>
        AllSituations.Select(s => Tactics.AutoEffort(0, even ? 0 : 50, s)).ToArray();

    private static int[] AutoAttackLine(bool underdog) =>
        AllSituations.Select(s => Tactics.AutoAttack(0, underdog ? 1 : 0, s)).ToArray();

    private static void AssertZero(Tactics t)
    {
        Assert.All(ShownEffort(t), v => Assert.Equal(0, v));
        Assert.All(ShownAttack(t), v => Assert.Equal(0, v));
    }

    /// <summary>Eine Zeile der Automatik (ausgeglichen/unausgeglichen bzw. schwächer/stärker), nicht 0.</summary>
    private static void AssertAutoLine(Tactics t)
    {
        var effort = ShownEffort(t);
        Assert.True(effort.SequenceEqual(AutoEffortLine(true)) || effort.SequenceEqual(AutoEffortLine(false)),
            "Einsatz: " + string.Join("/", effort));
        var attack = ShownAttack(t);
        Assert.True(attack.SequenceEqual(AutoAttackLine(true)) || attack.SequenceEqual(AutoAttackLine(false)),
            "Angriff: " + string.Join("/", attack));
    }

    /// <summary>Weiter bis einschließlich zum ersten Tag mit einem WM-Spiel Deutschlands.</summary>
    private static void PlayToFirstGermanWorldCupMatch(GameState game)
    {
        for (var guard = 0; ; guard++)
        {
            Assert.True(guard < 3000, "kein WM-Spiel Deutschlands");
            var before = GermanMatches(game);
            game.Continue();
            if (GermanMatches(game) > before)
                return;
        }
    }

    private static int GermanMatches(GameState game) =>
        game.WorldCup is { } cup
            ? cup.Groups.Sum(g => g.Results.Count(m => m.Home == NationalTeams.Germany || m.Away == NationalTeams.Germany))
            : 0;

    /// <summary>Modus 1 nach dem ersten „Los jetzt!“: Sprung zum ersten WM-Termin, Halt vor dem ersten Spiel (Ernennung).</summary>
    private static GameState Mode1()
    {
        var game = GameModeTests.Create(7, false, true, 2);
        game.Continue();
        Assert.NotNull(game.NationalCoach);
        return game;
    }

    // ---- Startwert -------------------------------------------------------------------------------------------

    /// <summary>
    /// DOSBox `nur-weltkugel/26`: Modus 1 nach „Los jetzt!“ und „auto“, vor dem ersten WM-Spiel – Einsatz und Angriff
    /// „AUTOM.“, alle Werte 0.
    /// </summary>
    [Fact]
    public void Mode1_BeforeFirstMatch_AutoShowsZero()
    {
        var game = Mode1();
        var coach = game.NationalCoach ?? throw new InvalidOperationException("Modus 1 ohne Bundestrainer");
        game.AutoNominate();
        Assert.Equal(TacticMode.Auto, coach.Tactics.EffortMode);
        Assert.Equal(TacticMode.Auto, coach.Tactics.AttackMode);
        AssertZero(coach.Tactics);
    }

    /// <summary>[ORIGINAL] Auch die eigenen Werte (SEMI) beginnen bei 0 (DGROUP 0xCAE9/0xCB01 nur vom Taktikbildschirm beschrieben).</summary>
    [Fact]
    public void Mode1_OwnValues_StartAtZero()
    {
        var tactics = Mode1().NationalCoach!.Tactics;
        tactics.EffortMode = TacticMode.Semi;
        tactics.AttackMode = TacticMode.Semi;
        for (var b = 0; b < Tactics.Blocks; b++)
            foreach (var s in AllSituations)
            {
                Assert.Equal(0, tactics.Effort(b, s));
                Assert.Equal(0, tactics.Attack(b, s));
            }
        AssertZero(tactics);
    }

    /// <summary>Keine Unterscheidung nach Modus (0x8365): Im Modus 4 steht nach der Ernennung ebenfalls 0.</summary>
    [Fact]
    public void Mode4_AfterAppointment_AutoShowsZero()
    {
        // Ob ein Manager Bundestrainer wird, hängt vom Zufallsverbrauch ab (zuletzt Listenwechsel 0x494EF, transfer-vertraege-
        // komplett Teil 5): erster Seed ab 3 mit Ernennung.
        GameState game = null!;
        for (var seed = 3; seed < 40 && game?.NationalCoach is null; seed++)
        {
            game = GameModeTests.Create(seed, true, true, 8, 2);
            NationalCoachMode4Tests.PlayToClubSeasonEnd(game);
        }
        var coach = game.NationalCoach ?? throw new InvalidOperationException("kein Bundestrainer");
        AssertZero(coach.Tactics);
    }

    /// <summary>Vereine bleiben unverändert: „AUTOM.“ zeigt die Zeile für ein ausgeglichenes Spiel ([APPROX]).</summary>
    [Fact]
    public void Club_AutoDisplay_Unchanged()
    {
        var tactics = new Tactics();
        Assert.False(tactics.KeepsLastAutoLine);
        Assert.Equal([60, 0, 25, 20, 30, 0], ShownEffort(tactics));
        Assert.Equal([0, 4, 5, 6, 7, 4], ShownAttack(tactics));
        Assert.Equal(60, tactics.Effort(0, ScoreSituation.Base));
        tactics.RecordAutoLine(0, 100);
        Assert.Equal([60, 0, 25, 20, 30, 0], ShownEffort(tactics));
    }

    // ---- Werte des letzten Spiels ----------------------------------------------------------------------------

    /// <summary>Nach dem ersten WM-Spiel zeigt „AUTOM.“ die Zeile, die die Automatik vor dem Spiel geschrieben hat.</summary>
    [Fact]
    public void Mode1_AfterFirstWorldCupMatch_AutoShowsLastMatchLine()
    {
        var game = Mode1();
        game.AutoNominate();
        PlayToFirstGermanWorldCupMatch(game);
        AssertAutoLine(game.NationalCoach!.Tactics);
    }

    /// <summary>[ORIGINAL] Freundschaftsspiele rufen die Spielvorbereitung 0x8365 nicht auf: Anzeige bleibt 0.</summary>
    [Fact]
    public void Friendly_DoesNotWriteAutoLine()
    {
        var game = Mode1();
        game.AutoNominate();
        Assert.Equal(FriendlyResult.Played, game.PlayFriendly(53, home: false));
        AssertZero(game.NationalCoach!.Tactics);
    }

    /// <summary>[ORIGINAL] Einsatz und Angriff getrennt: Bei „SEMI“ schreibt die Automatik nur den anderen Wert.</summary>
    [Fact]
    public void RecordAutoLine_RespectsModesSeparately()
    {
        var tactics = Tactics.National();
        tactics.EffortMode = TacticMode.Semi;
        tactics.RecordAutoLine(40, 60);
        Assert.Equal(AutoAttackLine(true), ShownAttack(tactics));
        tactics.EffortMode = TacticMode.Auto;
        Assert.All(ShownEffort(tactics), v => Assert.Equal(0, v));
        tactics.RecordAutoLine(60, 40);
        Assert.Equal(AutoEffortLine(false), ShownEffort(tactics));
        Assert.Equal(AutoAttackLine(false), ShownAttack(tactics));
    }

    /// <summary>
    /// Mehrsaisonprobe Modus 1 und 4 (Modus 2 als Gegenprobe), vier Saisonwechsel, passiv und aktiv: Die Werte des
    /// letzten Spiels bleiben über den Saisonwechsel und die neue Ernennung stehen; es gibt genau einen Halter der
    /// Taktik. Spielergebnisse ändern sich nicht (die Anzeige greift nicht in die Spielberechnung ein).
    /// </summary>
    [Theory]
    [InlineData(false, true, false)]
    [InlineData(false, true, true)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    [InlineData(true, false, false)]
    public void MultiSeason_LastMatchLine_SurvivesSeasonChange(bool flag, bool globe, bool active)
    {
        var game = flag ? GameModeTests.Create(3, flag, globe, 2, 30) : GameModeTests.Create(7, flag, globe, 2);
        for (var change = 0; change <= 4; change++)
        {
            for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
            {
                Assert.True(guard < 3000, "Saisonende nicht erreicht");
                foreach (var m in game.Managers)
                    m.Money = Math.Max(m.Money, 10_000_000);
                if (active && game.NationalCoach is { Squad.Count: 0 })
                    game.AutoNominate();
                if (active && flag)
                    GameModeTests.ActiveSecondManager(game);
                game.Continue();
            }
            if (game.Phase == SeasonPhase.GameOver)
                break;
            Assert.Same(game.NationalTactics, game.NationalCoach?.Tactics ?? game.NationalTactics);
            if (globe)
                AssertAutoLine(game.NationalTactics);
            else
                AssertZero(game.NationalTactics); // Modus 2: keine WM, keine Spielvorbereitung für Deutschland
            var effort = ShownEffort(game.NationalTactics);
            var attack = ShownAttack(game.NationalTactics);
            var held = game.NationalTactics;
            if (change == 4)
                break;
            game.StartNextSeason();
            Assert.Same(held, game.NationalTactics);
            Assert.Equal(effort, ShownEffort(game.NationalTactics));
            Assert.Equal(attack, ShownAttack(game.NationalTactics));
            if (game.NationalCoach is { } coach)
                Assert.Same(held, coach.Tactics);
        }
    }

    // ---- Spielstand ------------------------------------------------------------------------------------------

    [Fact]
    public void SaveLoad_KeepsLastMatchLine_AndHolder()
    {
        var game = Mode1();
        game.AutoNominate();
        PlayToFirstGermanWorldCupMatch(game);
        var effort = ShownEffort(game.NationalTactics);
        var loaded = SaveGame.Load(SaveGame.Save(game));
        Assert.True(loaded.Success, loaded.Error);
        Assert.Equal(effort, ShownEffort(loaded.Game!.NationalCoach!.Tactics));
        Assert.Same(loaded.Game.NationalCoach.Tactics, loaded.Game.NationalTactics);
        Assert.Equal(SaveGame.Save(game), SaveGame.Save(loaded.Game));
    }

    /// <summary>Taktik-Felder aus einem Schema-11-Stand entfernen (Stand vor dieser Spur).</summary>
    private static JsonObject ToSchema11(GameState game)
    {
        var root = JsonNode.Parse(SaveGame.Save(game))!.AsObject();
        root["schema"] = 11;
        var spiel = root["spiel"]!.AsObject();
        Assert.True(spiel.Remove("_vacantNationalTactics"));
        var stripped = 0;
        Strip(root);
        Assert.True(stripped > 0);
        return root;

        void Strip(JsonNode? node)
        {
            switch (node)
            {
                case JsonObject obj:
                    if (obj.Remove("_shownAutoEffort") & obj.Remove("_shownAutoAttack"))
                        stripped++;
                    foreach (var (_, child) in obj.ToArray())
                        Strip(child);
                    break;
                case JsonArray array:
                    foreach (var child in array.ToArray())
                        Strip(child);
                    break;
            }
        }
    }

    private static GameState Load(JsonObject root)
    {
        var result = SaveGame.Load(System.Text.Encoding.UTF8.GetBytes(root.ToJsonString()));
        Assert.True(result.Success, result.Error);
        return result.Game!;
    }

    /// <summary>Schema 11 mit Bundestrainer: eigene Werte bleiben, Anzeige unter „AUTOM.“ beginnt bei 0 ([APPROX]).</summary>
    [Fact]
    public void Schema11_WithCoach_IsMigrated()
    {
        var game = Mode1();
        game.AutoNominate();
        var tactics = game.NationalCoach!.Tactics;
        tactics.AttackMode = TacticMode.Semi;
        tactics.SetAttack(1, ScoreSituation.Tied, 3);
        PlayToFirstGermanWorldCupMatch(game);

        var loaded = Load(ToSchema11(game));
        var copy = loaded.NationalCoach!.Tactics;
        Assert.Same(copy, loaded.NationalTactics);
        Assert.True(copy.KeepsLastAutoLine);
        Assert.Equal(3, copy.Attack(1, ScoreSituation.Tied));
        Assert.All(ShownEffort(copy), v => Assert.Equal(0, v));
        Assert.False(loaded.Clubs[2].Tactics.KeepsLastAutoLine);
        Assert.Equal([60, 0, 25, 20, 30, 0], ShownEffort(loaded.Clubs[2].Tactics));
        // Danach Gleichlauf mit dem frisch gesicherten Stand.
        Assert.Equal(SaveGame.Save(loaded), SaveGame.Save(Load(JsonNode.Parse(SaveGame.Save(loaded))!.AsObject())));
    }

    /// <summary>Schema 11 ohne Bundestrainer (Modus 2): Der Spielstand bekommt die Taktik des Nationalteams mit allen Werten 0.</summary>
    [Fact]
    public void Schema11_WithoutCoach_IsMigrated()
    {
        var game = GameModeTests.Create(3, true, false, 2);
        TestSeason.PlayMatchDay(game);
        Assert.Null(game.NationalCoach);

        var loaded = Load(ToSchema11(game));
        Assert.True(loaded.NationalTactics.KeepsLastAutoLine);
        AssertZero(loaded.NationalTactics);
        Assert.Equal(SaveGame.Save(game), SaveGame.Save(loaded));
    }
}
