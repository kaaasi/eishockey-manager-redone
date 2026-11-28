using System.Text.Json.Nodes;
using EishockeyManager.Core;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur nationalteam, Teil taktik (#192, WD-01): Taktik des Nationalteams ohne Startwert.
/// Befund vorher: Unter „AUTOM.“ zeigte die Taktik des Bundestrainers schon vor dem ersten Spiel 60/0/25/20/30/0 und
/// 0/4/5/6/7/4, die eigenen Werte begannen mit derselben Zeile, und jede Ernennung begann neu. Beleg: DOSBox
/// `dosbox/nur-weltkugel/26_*.png` (Modus 1 nach „auto“, vor dem ersten WM-Spiel: alle Werte 0) und em.exe 0x8365
/// (`docs/dosbox-befunde.md`, Zeile „Taktik des Nationalteams zu Beginn alle 0“).
/// </summary>
public class NationalTacticsStartTests
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
        // Ob ein Manager Bundestrainer wird, hängt an der Stärke der Managerkader (#204) und am Zufallsverbrauch (zuletzt
        // Listenwechsel 0x494EF); Integration Runde 9: erster Seed ab 3 mit Ernennung, Kader in Bestbesetzung.
        GameState game = null!;
        for (var seed = 3; seed < 40 && game?.NationalCoach is null; seed++)
        {
            game = GameModeTests.Create(seed, true, true, 8, 2);
            ManagedSquads.PlayAtTopStrength(game);
            NationalCoachMode4Tests.PlayToClubSeasonEnd(game);
        }
        var coach = game.NationalCoach ?? throw new InvalidOperationException("kein Bundestrainer");
        AssertZero(coach.Tactics);
    }

    /// <summary>
    /// Vereine: seit A180 (kader-start Runde 14) wie das Nationalteam (<see cref="Club_AutoDisplay_KeepsLastLine"/>); Spielstände
    /// vor Runde 14 (<see cref="Tactics.LegacyStart"/>) zeigen weiter die Zeile für ein ausgeglichenes Spiel ([APPROX]).
    /// </summary>
    [Fact]
    public void Club_AutoDisplay_Unchanged()
    {
        var tactics = Tactics.LegacyStart();
        Assert.False(tactics.KeepsLastAutoLine);
        Assert.Equal([60, 0, 25, 20, 30, 0], ShownEffort(tactics));
        Assert.Equal([0, 4, 5, 6, 7, 4], ShownAttack(tactics));
        Assert.Equal(60, tactics.Effort(0, ScoreSituation.Base));
        tactics.RecordAutoLine(0, 100);
        Assert.Equal([60, 0, 25, 20, 30, 0], ShownEffort(tactics));
    }

    /// <summary>A180: Ein neuer Verein zeigt unter „AUTOM.“ 0, nach einem Spiel dessen Zeile.</summary>
    [Fact]
    public void Club_AutoDisplay_KeepsLastLine()
    {
        var tactics = new Tactics();
        Assert.True(tactics.KeepsLastAutoLine);
        Assert.All(ShownEffort(tactics), v => Assert.Equal(0, v));
        tactics.RecordAutoLine(0, 100);
        Assert.Equal([40, 20, 20, 15, 20, 0], ShownEffort(tactics));
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

    /// <summary>
    /// [ORIGINAL] Auch Freundschaftsspiele laufen über den Spieltagsablauf 0x36B5D (Datei 0x30365, A008/A110) und damit über die
    /// Spielvorbereitung 0x8365 (Datei 0x36F27/0x36F5A): Die Anzeige zeigt danach die Zeile des Spiels. Spur national Teil 1 (A494;
    /// DOSBox FILE18 → FILE19, Satz 54: 0x2D = 40, 0x27–0x2A = 20/20/15/20); bis dahin hieß der Test <c>Friendly_DoesNotWriteAutoLine</c>
    /// (Annahme Runde 8, das Freundschaftsspiel rufe 0x8365 nicht). Rückschaltprobe: alter Code → Anzeige 0 → rot.
    /// </summary>
    [Fact]
    public void Friendly_WritesAutoLine()
    {
        var game = Mode1();
        game.AutoNominate();
        Assert.Equal(FriendlyResult.Played, game.PlayFriendly(53, home: false));
        AssertAutoLine(game.NationalCoach!.Tactics);
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

    private static GameState Load(JsonObject root)
    {
        var result = SaveGame.Load(System.Text.Encoding.UTF8.GetBytes(root.ToJsonString()));
        Assert.True(result.Success, result.Error);
        return result.Game!;
    }
}
