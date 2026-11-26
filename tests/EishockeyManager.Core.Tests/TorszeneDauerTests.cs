using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur torszene-dauer, Teil 2 (A472, <c>docs/analyse/torszene-dauer.md</c> §10): Spielkern bei Darstellung 2 „Dauer-Eisfeld“
/// und Schüsse des Penaltyschießens mit Anzeige (T7). Vor dem Teil rot: der Strafgrund wurde auch mit Dauer-Eisfeld gezogen,
/// der Merker „Manager am Zug“ galt als mit Live-Anzeige, und das Penaltyschießen kannte keine Schüsse
/// (<see cref="OriginalMinutes.ShootoutShots"/> fehlte, Build-Fehler).
/// </summary>
public sealed class TorszeneDauerTests
{
    private static GameState OneManager(int liveMode)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Eins", Head = 0, ClubIndex = 2 });
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, 3);
        game.Options.LiveMode = liveMode;
        game.Options.MatchSpeed = 60;
        return game;
    }

    /// <summary>
    /// [ORIGINAL] Mit Darstellung 2 setzt 0x36B5D vor den Minuten DGROUP 0x5A56 = 0 (Datei 0x3710B–0x37171); der Reporter
    /// 0x52520 kehrt dann sofort zurück (Datei 0x5252A) und zieht keinen Strafgrund (Datei 0x52A9A). Mit Darstellung 1 wie bisher.
    /// </summary>
    [Theory]
    [InlineData(1, 1)]
    [InlineData(GameOptions.PermanentRinkLiveMode, 0)]
    public void PenaltyReason_NotDrawnOnPermanentRink(int liveMode, int expected)
    {
        var game = OneManager(liveMode);
        var (home, away) = (game.Clubs[2], game.Clubs[3]);
        var h = MatchEngine.LiveTeamOf(game, home, away, managed: true, managerMatch: true);
        var a = MatchEngine.LiveTeamOf(game, away, home, managed: false, managerMatch: true);
        var row = new LiveScoreboardRow(home.Index, away.Index);
        var previous = OriginalMinutes.Display;
        OriginalMinutes.Display = new LiveDisplay(row, DayActive: true, game.Options);
        try
        {
            var random = new Random(7);
            OriginalMinutes.PenaltyReason(h, a, 0, 5, 2, random);
            Assert.Equal(expected, row.PenaltyReasons.Count);
            var reference = new Random(7);
            for (var i = 0; i < expected; i++)
                reference.Next(0, GoalSceneSelection.ReasonCount);
            Assert.Equal(reference.Next(), random.Next()); // kein Zug mit Dauer-Eisfeld
        }
        finally
        {
            OriginalMinutes.Display = previous;
        }
    }

    /// <summary>
    /// A071 mit Dauer-Eisfeld: [ORIGINAL] in den Minuten ist DGROUP 0x5A56 = 0, die Verletzung setzt also den Merker
    /// [0xA70E] wie ohne Live-Anzeige (Datei 0x5419A; <see cref="TurnManagerRegister.LiveDisplay"/>).
    /// </summary>
    [Theory]
    [InlineData(1, true)]
    [InlineData(GameOptions.PermanentRinkLiveMode, false)]
    public void TurnManager_NoLiveDisplayOnPermanentRink(int liveMode, bool expected)
    {
        var game = OneManager(liveMode);
        var match = MatchEngine.BeginMatch(game, game.Clubs[2], game.Clubs[3], MatchCompetition.League, knockOut: false, new Random(1));
        Assert.NotNull(match.Row); // Managerverein zum Zusehen markiert → Reihe, DGROUP 0x5A56 = 1 aus 0x544C0
        Assert.Equal(expected, match.HomeTeam!.TurnManager!.LiveDisplay);
    }

    /// <summary>
    /// T7: [ORIGINAL] Anzeigezweig 0x3901B Schuss für Schuss (A101) – je Schuss random(0, 10) &gt; 6 (Datei 0x39429–0x39443), dann
    /// die Torszene 0x53C57 mit Penalty-Kennzeichen 1 (Datei 0x39479–0x394C5), deren Szenenwahl 0x575C3 mit Darstellung ≠ 0 aus
    /// dem Spielzufall zieht (random(2, Normal), random(0, Tor ? 210 : 255), random(2, Penalty)). Seite wechselt nach jedem Schuss.
    /// </summary>
    [Fact]
    public void ShootoutWithDisplay_ShotsAndScenesInOriginalOrder()
    {
        for (var seed = 0; seed < 30; seed++)
        {
            var report = new MatchReport();
            var options = new GameOptions { LiveMode = 1 };
            var goals = OriginalMinutes.Shootout(new Random(seed), watched: true, report, options);
            var shots = OriginalMinutes.ShootoutShots(report);

            // Nachbildung der Zugfolge.
            var r = new Random(seed);
            var side = r.Next(0, 2);
            var expected = new List<ShootoutShot>();
            var counts = GoalSceneSelection.OriginalCounts;
            var (g, rounds, first) = (new int[2], 0, side);
            do
            {
                var goal = r.Next(0, 11) > 6;
                if (goal)
                    g[side]++;
                var n = r.Next(2, counts.Normal + 1);
                r.Next(0, (goal ? GoalSceneSelection.GoalPenaltyRange : GoalSceneSelection.MissPenaltyRange) + 1);
                n = r.Next(2, counts.Penalty + 1);
                var name = $"{n}.{(side == 0 ? 'h' : 'a')}{(goal ? 't' : 'v')}e";
                expected.Add(new ShootoutShot(side, goal, new GoalSceneChoice(n, true, false, name)));
                side ^= 1;
                if (side == first)
                    rounds++;
            }
            while (rounds < OriginalMinutes.ShootoutRounds || g[0] == g[1] || side != first);

            Assert.Equal(expected, shots);
            Assert.Equal((g[0], g[1]), goals);
            Assert.Equal(goals.Home, shots.Count(s => s.Side == 0 && s.Goal));
            Assert.Equal(goals.Away, shots.Count(s => s.Side == 1 && s.Goal));
        }
    }

    /// <summary>Darstellung 0: [ORIGINAL] keine Szenenwahl (Datei 0x53DDC), Treffer wie bisher (<see cref="OriginalMinutes.Shootout(Random, bool)"/>).</summary>
    [Fact]
    public void ShootoutDisplayZero_NoScenes_SameAsBefore()
    {
        for (var seed = 0; seed < 30; seed++)
        {
            var report = new MatchReport();
            var goals = OriginalMinutes.Shootout(new Random(seed), watched: true, report, new GameOptions { LiveMode = 0 });
            Assert.Equal(OriginalMinutes.Shootout(new Random(seed), watched: true), goals);
            var shots = OriginalMinutes.ShootoutShots(report);
            Assert.NotEmpty(shots);
            Assert.All(shots, s => Assert.Null(s.Scene));
            Assert.Empty(OriginalMinutes.ShootoutShots(new MatchReport()));
        }
    }
}
