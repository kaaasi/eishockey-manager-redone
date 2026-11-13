using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur livespiel-kern, Teil A4 (L4/L7-Kern, R18-D3 = A): Zufall der Anzeige im Spielkern – Szenenwahl 0x575C3 und
/// Strafgrund 0x52520 (<c>docs/analyse/re-livespiel.md</c> §5, §11). Vor dem Teil rot: der Kern zog diese Züge nicht
/// (<c>GoalSceneSelection</c> fehlte, Build-Fehler; ein Spiel mit Darstellung ≠ 0 lief zugleich wie ohne Anzeige).
/// </summary>
public sealed class LivespielKernGoalSceneTests
{
    /// <summary>Zufall mit vorgegebenen Werten; zählt die Züge und prüft die Grenzen (random(a, b) = Next(a, b + 1)).</summary>
    private sealed class ScriptedRandom(params int[] values) : Random
    {
        private int _next;
        public List<(int Min, int Max)> Calls { get; } = [];

        public override int Next(int minValue, int maxValue)
        {
            Calls.Add((minValue, maxValue - 1));
            return values[_next++];
        }
    }

    private static readonly GoalSceneCounts Counts = GoalSceneSelection.OriginalCounts;

    /// <summary>Abnahme A4: Szenenwahl mit festem Zufall ergibt den erwarteten Namen (Beispiele §11) und die Zugfolge.</summary>
    [Fact]
    public void Choose_HomeMiss_TwoDraws()
    {
        var r = new ScriptedRandom(7, 1);
        var choice = GoalSceneSelection.Choose(goal: false, penalty: false, side: 0, sixSkaters: false, Counts, r);
        Assert.Equal("7.hv", choice.FileName);
        Assert.Equal([(2, 11), (0, 255)], r.Calls);
    }

    [Fact]
    public void Choose_AwayGoalPenaltyByDraw_ThreeDraws()
    {
        var r = new ScriptedRandom(9, 0, 2);
        var choice = GoalSceneSelection.Choose(goal: true, penalty: false, side: 1, sixSkaters: true, Counts, r);
        Assert.Equal("2.ate", choice.FileName);
        Assert.True(choice.Penalty);
        Assert.False(choice.SixSkaters); // Penalty geht vor „sechs“ (Datei 0x5761E)
        Assert.Equal([(2, 11), (0, 210), (2, 2)], r.Calls);
    }

    [Fact]
    public void Choose_HomeGoalSix_ThreeDraws()
    {
        var r = new ScriptedRandom(4, 5, 2);
        var choice = GoalSceneSelection.Choose(goal: true, penalty: false, side: 0, sixSkaters: true, Counts, r);
        Assert.Equal("2.htj", choice.FileName);
        Assert.Equal([(2, 11), (0, 210), (2, 2)], r.Calls);
    }

    [Fact]
    public void Choose_PenaltyFlag_AwayMiss()
    {
        var r = new ScriptedRandom(3, 8, 2);
        Assert.Equal("2.ave", GoalSceneSelection.Choose(goal: false, penalty: true, side: 1, sixSkaters: false, Counts, r).FileName);
        Assert.Equal(3, r.Calls.Count);
    }

    /// <summary>[ORIGINAL] 0x52A8E–0x52AA8: 2 Minuten random(0, 12), 5 Minuten Index 5 ohne Zug.</summary>
    [Fact]
    public void PenaltyReason_Draws()
    {
        var r = new ScriptedRandom(12);
        Assert.Equal(12, GoalSceneSelection.PenaltyReason(2, r));
        Assert.Equal(5, GoalSceneSelection.PenaltyReason(5, r));
        Assert.Equal([(0, 12)], r.Calls);
        Assert.Equal("Übertr. Härte.", GoalSceneSelection.Reasons[5]);
        Assert.Equal(GoalSceneSelection.ReasonCount, GoalSceneSelection.Reasons.Length);
    }

    private static (MatchReport Report, GameState Game, int Next) Play(int liveMode, int speed, int seed = 5)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "M", Head = 0, ClubIndex = 2 });
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, 3);
        game.Options.LiveMode = liveMode;
        game.Options.MatchSpeed = speed;
        var home = game.Clubs[2];
        var away = game.Clubs.First(c => c.Index != 2 && c.League == home.League);
        var random = new Random(seed);
        var report = MatchEngine.PlayWithEvents(game, home, away, MatchCompetition.League, knockOut: false, random);
        return (report, game, random.Next());
    }

    /// <summary>
    /// Abnahme A4 (R18-D3): Zufallsfolge mit und ohne Anzeige getrennt geprüft. Darstellung 0 und Schnelldurchlauf: kein Zug
    /// der Anzeige (gleiche Folge wie die Rechnung allein); Darstellung ≠ 0: je gezählter Chance eine Szene; Geschwindigkeit
    /// unter 70: je 2-Minuten-Strafe ein Grund.
    /// </summary>
    [Fact]
    public void RandomSequence_WithAndWithoutDisplay()
    {
        var plain = Play(liveMode: 0, speed: 70);
        var row = plain.Game.LiveScoreboard!.Rows[0];
        Assert.Empty(row.Scenes);
        Assert.Empty(row.PenaltyReasons);
        // Gleiche Folge wie mit Darstellung 0 bei anderer Geschwindigkeit ab 70.
        var fast = Play(liveMode: 0, speed: 80);
        Assert.Equal(plain.Next, fast.Next);

        var scenes = Play(liveMode: 1, speed: 70);
        var sceneRow = scenes.Game.LiveScoreboard!.Rows[0];
        Assert.Equal(sceneRow.ChanceList.Count, sceneRow.Scenes.Count);
        Assert.Equal(sceneRow.ChanceList.Select(c => (c.Minute, c.Side, c.Goal)),
            sceneRow.Scenes.Select(s => (s.Minute, s.Side, s.Goal)));
        Assert.All(sceneRow.Scenes, s => Assert.Matches(@"^\d+\.[ha][tv]e?j?$", s.Choice.FileName));
        Assert.NotEqual(plain.Next, scenes.Next);

        var slow = Play(liveMode: 0, speed: 30, seed: 9);
        var slowRow = slow.Game.LiveScoreboard!.Rows[0];
        Assert.Empty(slowRow.Scenes);
        Assert.Equal(slow.Report.Events.Where(e => e.Kind == MatchEventKind.Penalty).Select(e => (e.Minute, e.Side)),
            slowRow.PenaltyReasons.Select(p => (p.Minute, p.Side)));
        Assert.All(slowRow.PenaltyReasons, p => Assert.InRange(p.Reason, 0, 12));
    }

    /// <summary>Paarung ohne Manager: auch mit Darstellung und langsamem Ablauf kein Zug der Anzeige (keine Reihe, kein Reporter).</summary>
    [Fact]
    public void ComputerPairing_NoDisplayDraws()
    {
        int Run(int liveMode, int speed)
        {
            var setup = new GameSetup();
            setup.Managers.Add(new ManagerSetup { Name = "M", Head = 0, ClubIndex = 2 });
            var game = NewGame.Create(TestGames.CreateDatabase(), setup, 3);
            game.Options.LiveMode = liveMode;
            game.Options.MatchSpeed = speed;
            var clubs = game.Clubs.Where(c => c.Index != 2 && c.League == game.Clubs[2].League).Take(2).ToList();
            var random = new Random(4);
            MatchEngine.PlayWithEvents(game, clubs[0], clubs[1], MatchCompetition.League, knockOut: false, random);
            return random.Next();
        }
        Assert.Equal(Run(0, 70), Run(2, 0));
    }
}
