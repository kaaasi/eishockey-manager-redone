using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur live-verschraenkt, Teil 2 (S01: A442, A450, A547, A766; <c>docs/analyse/live-spieltag.md</c>): Ein Ligaspieltag rechnet
/// alle Paarungen aller Ligen in einer Minutenschleife ([ORIGINAL] 0x37A62, Datei 0x37DB7–0x380CD: Ligen 0 → 2, je Liga die
/// Paarungen des Spieltags; Nachbildung <c>tools/reverse-engineering/live/minutenschleife.py</c> Fall 1).
/// </summary>
public sealed class LiveVerschraenktTests
{
    private const int FirstClub = 2;

    /// <summary>Neues Spiel mit zwei Managern: Bundesliga und 2. Liga (Platzwahl nach der Start-Liga, A558).</summary>
    private static GameState NewGame(int seed)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "A", Head = 0, ClubIndex = FirstClub });
        setup.Managers.Add(new ManagerSetup { Name = "B", Head = 1, ClubIndex = 20, StartLeague = League.ZweiteLiga });
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

    private static List<(int Home, int Away)> ScheduledFixtures(GameState game) =>
        [.. game.Schedules.OrderBy(s => s.Key).Where(s => game.PlayedMatchDays < s.Value.Count)
            .SelectMany(s => s.Value[game.PlayedMatchDays].Matches)];

    /// <summary>
    /// E1: zwei Managerpaarungen in verschiedenen Ligen laufen gleichzeitig. Der Treiber bekommt je Ligaspieltag genau einen
    /// Minutenschritt mit allen Paarungen in der Reihenfolge des Originals; beide Reihen haben nach jeder Minute dieselbe
    /// Minute. Vor dem Teil rot: je Managerpaarung ein eigener Minutenschritt mit einer Paarung (zwei Aufrufe, Count = 1).
    /// </summary>
    [Fact]
    public void LeagueDay_OneMinuteLoopOverAllPairings()
    {
        var game = NewGame(5);
        Assert.NotEqual(game.Clubs[game.Managers[0].ClubIndex].League, game.Clubs[game.Managers[1].ClubIndex].League);
        var driver = new RecordingDriver();
        for (var day = 0; day < 3; day++)
        {
            var expected = ScheduledFixtures(game);
            var runsBefore = driver.Days.Count;
            using (MatchEngine.UseLiveDriver(driver))
                TestSeason.PlayMatchDay(game);
            var runs = driver.Days.Skip(runsBefore).Where(d => d.Matches[0].Competition == MatchCompetition.League).ToList();
            var run = Assert.Single(runs);
            Assert.Equal(expected, run.Matches.Select(m => (m.Home.Index, m.Away.Index)).ToList());
            var rows = run.Matches.Where(m => m.Row is not null).Select(m => m.Row!).ToList();
            Assert.Equal(2, rows.Count);
            // [ORIGINAL] Reihen in Paarungsreihenfolge (0x544C0 für alle Paarungen vor den Minuten)
            Assert.Equal(rows, game.LiveScoreboard!.Rows.Take(2));
            Assert.All(run.Matches, m => Assert.False(m.Running));
        }
        Assert.Empty(driver.Mismatches);
    }

    /// <summary>
    /// E2: Ergebnis eines Ligaspieltags = Minutenschritt über alle Paarungen aus dem gemeinsamen Zufall (verschränkt, 0x37A62),
    /// ohne Treiber wie mit Treiber ohne Eingriff (ganzer Spielstand gleich, mehrere Startwerte).
    /// </summary>
    [Fact]
    public void LeagueDay_WithAndWithoutDriver_SameGame()
    {
        for (var seed = 0; seed < 3; seed++)
        {
            var a = NewGame(seed);
            var b = NewGame(seed);
            var driver = new RecordingDriver();
            for (var i = 0; i < 8; i++)
            {
                a.Continue();
                using (MatchEngine.UseLiveDriver(driver))
                    b.Continue();
                Assert.Equal(SaveGame.Save(a), SaveGame.Save(b));
            }
            Assert.NotEmpty(driver.Days);
        }
    }

    /// <summary>
    /// E4 (A766): [ORIGINAL] In der Verlängerung rechnen nur die Paarungen mit Gleichstand nach 60 Minuten (Datei
    /// 0x3825F–0x3826C, Nachbildung Fall 4); entschiedene Paarungen enden mit Minute 60 und rechnen keine weitere Minute.
    /// </summary>
    [Fact]
    public void KnockOutDay_OnlyTiedPairingsPlayOvertime()
    {
        var found = false;
        for (var seed = 0; seed < 40 && !found; seed++)
        {
            var game = NewGame(seed);
            var random = new Random(seed);
            var fixtures = Enumerable.Range(0, 6)
                // K.-o.-Spiele über den Minutenweg (ohne schnellen Pokalweg 0x3731F, der nur die Pokalwettbewerbe betrifft)
                .Select(i => new LiveFixture(game.Clubs[2 * i], game.Clubs[2 * i + 1], MatchCompetition.League, true, random))
                .ToList();
            var day = LiveMatchDay.Begin(game, fixtures);
            while (day.Minute < 60)
                day.Step();
            var tied = day.Matches.Where(m => m.Running).ToList();
            if (tied.Count == 0 || tied.Count == day.Matches.Count)
            {
                day.Finish();
                continue;
            }
            found = true;
            Assert.All(tied, m => Assert.Equal(m.Report.HomeGoals, m.Report.AwayGoals));
            var decided = day.Matches.Except(tied).ToList();
            Assert.True(day.Step());
            Assert.All(tied, m => Assert.Equal(61, m.LastMinute));
            Assert.All(decided, m => Assert.Equal(60, m.LastMinute));
            day.Finish();
            Assert.All(decided, m => Assert.Equal(60, m.LastMinute));
            Assert.All(tied, m => Assert.Equal(70, m.LastMinute));
        }
        Assert.True(found, "kein Spieltag mit Verlängerung und entschiedener Paarung gefunden");
    }

    /// <summary>
    /// Mehrsaisonprobe (AGENTS.md §5): vier Saisonwechsel mit zwei Managern, Live an (Treiber rechnet jede Minute), passiv und
    /// aktiv (Kader halten). Spielstand mit Treiber = ohne Treiber nach jedem „Tag weiter“; Ligaspieltage mit allen Paarungen;
    /// Ergebnisse und Geld plausibel.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FourSeasonChanges_TwoManagersLive_Plausible(bool active)
    {
        var a = NewGame(11);
        var b = NewGame(11);
        var driver = new RecordingDriver();
        for (var change = 0; change < 4; change++)
        {
            for (var guard = 0; a.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
            {
                Assert.True(guard < 1000, "Saisonende nicht erreicht");
                if (active)
                {
                    foreach (var g in new[] { a, b })
                        foreach (var m in g.Managers)
                            TestSeason.KeepSquad(g, m, extendExpired: true);
                }
                a.Continue();
                using (MatchEngine.UseLiveDriver(driver))
                    b.Continue();
                Assert.Equal(a.Phase, b.Phase);
            }
            Assert.Equal(SaveGame.Save(a), SaveGame.Save(b));
            Assert.All(a.Results, r => Assert.InRange(r.HomeGoals + r.AwayGoals, 0, 40));
            Assert.All(a.Managers, m => Assert.InRange(m.Money, -100_000_000L, 100_000_000L));
            a.StartNextSeason();
            b.StartNextSeason();
        }
        Assert.Equal(SaveGame.Save(a), SaveGame.Save(b));
        var leagueDays = driver.Days.Where(d => d.Matches[0].Competition == MatchCompetition.League).ToList();
        Assert.NotEmpty(leagueDays);
        Assert.All(leagueDays, d => Assert.True(d.Matches.Count > 2));
        Assert.Empty(driver.Mismatches);
    }
}
