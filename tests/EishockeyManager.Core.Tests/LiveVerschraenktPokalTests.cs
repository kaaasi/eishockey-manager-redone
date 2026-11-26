using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur live-verschraenkt, Teil 2b (S01 für die Pokaltermine, Entscheidung Leitstelle 25.11.2026 zu PR #1032;
/// <c>docs/analyse/live-spieltag.md</c> §2 Punkt 3): [ORIGINAL] Ein Pokal-, Play-off- oder WM-Termin rechnet je Minute alle
/// Paarungen der Pokalzeile in Paarungsreihenfolge (0x37A62 Datei 0x380D0–0x3853E, Nachbildung
/// <c>tools/reverse-engineering/live/minutenschleife.py</c> Fall 4).
/// </summary>
public sealed class LiveVerschraenktPokalTests
{
    /// <summary>Neues Spiel mit zwei Managern in der Bundesliga.</summary>
    private static GameState NewGame(int seed, int secondClub)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "A", Head = 0, ClubIndex = 2 });
        setup.Managers.Add(new ManagerSetup { Name = "B", Head = 1, ClubIndex = secondClub });
        return Core.NewGame.Create(TestGames.CreateDatabase(), setup, seed);
    }

    /// <summary>Treiber ohne Eingriff: rechnet jede Minute und schreibt mit.</summary>
    private sealed class RecordingDriver : ILiveMatchDriver
    {
        public List<LiveMatchDay> Days { get; } = [];
        public List<string> Mismatches { get; } = [];

        public void Run(LiveMatchDay day)
        {
            Days.Add(day);
            while (day.Step())
            {
                foreach (var m in day.Matches.Where(m => m.Stepped && m.Row is not null && m.Running))
                {
                    if (m.LastMinute != day.Minute)
                        Mismatches.Add($"{m.Home.Index}-{m.Away.Index}: Minute {m.LastMinute} statt {day.Minute}");
                }
            }
        }
    }

    /// <summary>
    /// DEB-Pokaltermin mit zwei Managerpaarungen: ein Minutenschritt mit beiden Paarungen (beide Reihen ab Minute 1, nach jeder
    /// Minute dieselbe Minute). Vor dem Teil rot: je Managerpaarung ein eigener Treiberaufruf mit einer Paarung
    /// (<c>CupsDeb.cs</c> rief <c>MatchEngine.PlayWithEvents</c> je Paarung).
    /// </summary>
    [Fact]
    public void DebCupDate_TwoManagerPairings_OneMinuteLoop()
    {
        var found = false;
        for (var seed = 0; seed < 10 && !found; seed++)
        {
            var game = NewGame(seed, 4);
            var driver = new RecordingDriver();
            var managerClubs = game.Managers.Select(m => m.ClubIndex).ToHashSet();
            for (var guard = 0; guard < 400 && !found && game.Phase is SeasonPhase.Running; guard++)
            {
                var before = driver.Days.Count;
                using (MatchEngine.UseLiveDriver(driver))
                    game.Continue();
                var cupRuns = driver.Days.Skip(before).Where(d => d.Matches[0].Competition == MatchCompetition.DebCup).ToList();
                if (cupRuns.Count == 0)
                    continue;
                var managed = cupRuns.SelectMany(d => d.Matches)
                    .Where(m => managerClubs.Contains(m.Home.Index) || managerClubs.Contains(m.Away.Index)).ToList();
                if (managed.Count < 2)
                    continue;
                found = true;
                var run = Assert.Single(cupRuns);
                Assert.Equal(2, run.Matches.Count(m => m.Row is not null));
                Assert.All(run.Matches, m => Assert.Equal(MatchCompetition.DebCup, m.Competition));
            }
            Assert.Empty(driver.Mismatches);
        }
        Assert.True(found, "kein DEB-Pokaltermin mit zwei Managerpaarungen gefunden");
    }

    /// <summary>
    /// Spielstand mit Treiber ohne Eingriff = ohne Treiber über die erste Saison (Liga, DEB-Pokal, Europapokal, Play-offs).
    /// </summary>
    [Fact]
    public void FirstSeason_WithAndWithoutDriver_SameGame()
    {
        var a = NewGame(3, 4);
        var b = NewGame(3, 4);
        var driver = new RecordingDriver();
        for (var guard = 0; a.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
        {
            Assert.True(guard < 1000, "Saisonende nicht erreicht");
            a.Continue();
            using (MatchEngine.UseLiveDriver(driver))
                b.Continue();
            Assert.Equal(SaveGame.Save(a), SaveGame.Save(b));
        }
        Assert.Contains(driver.Days, d => d.Matches[0].Competition == MatchCompetition.DebCup);
        Assert.Empty(driver.Mismatches);
    }

    /// <summary>
    /// [APPROX] Termin mit beiden Wegen (<see cref="MatchEngine.PlayDate"/>): die Paarungen der Minutenschleife zuerst (alle
    /// gemeinsam), dann die des schnellen Wegs in ihrer Reihenfolge; das Ergebnis hält die Reihenfolge der Vorgabe.
    /// </summary>
    [Fact]
    public void PlayDate_MinuteLoopFirst_ThenFastPathInOrder()
    {
        var game = NewGame(1, 4);
        game.Options.ShowDebCup = false; // Anzeigeschalter aus: Paarungen ohne Manager nehmen den schnellen Weg 0x3731F
        var random = new Random(1);
        var clubs = new[] { (10, 11), (2, 12), (13, 14), (4, 15) };
        var fixtures = clubs.Select(c => new LiveFixture(game.Clubs[c.Item1], game.Clubs[c.Item2], MatchCompetition.DebCup, true,
            random)).ToList();
        var order = new List<int>();
        var reports = MatchEngine.PlayDate(game, fixtures, (i, _) => order.Add(i));
        Assert.Equal([1, 3, 0, 2], order);
        Assert.Equal(4, reports.Count);
        Assert.False(reports[1].FastCupPath);
        Assert.False(reports[3].FastCupPath);
        Assert.True(reports[0].FastCupPath);
        Assert.True(reports[2].FastCupPath);
    }
}
