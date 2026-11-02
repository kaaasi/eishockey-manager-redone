using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>Paket „wettbewerbe“, PR 5: Ewige Tabelle, Ewige Bilanz und „Früher“.</summary>
public class StatisticsHistoryTests
{
    private static GameState Create(int seed, params int[] clubs)
    {
        var setup = new GameSetup();
        for (var i = 0; i < clubs.Length; i++)
            setup.Managers.Add(new ManagerSetup { Name = $"M{i}", Head = i, ClubIndex = clubs[i] });
        return NewGame.Create(TestGames.CreateDatabase(), setup, seed);
    }

    private static void PlayToSeasonEnd(GameState game)
    {
        for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
        {
            Assert.True(guard < 2000);
            foreach (var m in game.Managers)
                m.Money = Math.Max(m.Money, 10_000_000);
            game.Continue();
        }
    }

    /// <summary>[ORIGINAL] Formel aus em.exe 0x5667A, Werte aus dem Video (16:38) nach der ersten Saison.</summary>
    [Theory]
    [InlineData(League.Oberliga, 7, false, false, false, 5)]     // Berliner SC, 8. der Oberliga
    [InlineData(League.Bundesliga, 0, false, false, false, 52)]  // Mannheimer ERC, 1. der Bundesliga
    [InlineData(League.Bundesliga, 1, true, false, true, 116)]   // Düsseldorfer EG: 2., Meister, Europacupsieger
    [InlineData(League.Bundesliga, 2, false, true, false, 75)]   // EC Hedos München: 3., DEB-Pokalsieger
    [InlineData(League.ZweiteLiga, 0, false, false, false, 35)]  // Erster der 2. Liga: 20 + 12 + 3
    [InlineData(null, 0, false, false, false, 0)]                // ohne Liga
    public void EternalPoints_AsInOriginal(League? league, int rank0, bool champion, bool cup, bool europe, int expected) =>
        Assert.Equal(expected, GameState.EternalPoints(league, rank0, champion, cup, europe));

    /// <summary>Regressionstest Kern: Nach einer Saison steht jeder Ligaverein mit seinen Punkten in der Ewigen Tabelle.</summary>
    [Fact]
    public void AfterOneSeason_EternalTableMatchesFinalTablesAndTitles()
    {
        var game = Create(5, 30);
        PlayToSeasonEnd(game);
        var record = game.History[^1];
        foreach (var league in Enum.GetValues<League>())
        {
            var table = record.FinalTables[league];
            for (var r = 0; r < table.Count; r++)
            {
                var club = table[r];
                // Spur saisonwechsel Teil 3 (S-K4): Liga der nächsten Saison, Platz der gespielten (em.exe nach dem Austausch).
                var expected = GameState.EternalPoints(record.NextLeague(club, league), r,
                    record.Titles.GetValueOrDefault(Title.GermanChampion, -1) == club,
                    record.Titles.GetValueOrDefault(Title.CupWinner, -1) == club,
                    record.Titles.GetValueOrDefault(Title.EuropeanCupWinner, -1) == club);
                Assert.Equal(expected, game.Eternal(club).Points);
            }
        }
        Assert.Equal(1, game.Eternal(record.Titles[Title.GermanChampion]).Championships);
        var ranking = game.EternalTable;
        Assert.Equal(42, ranking.Count); // Spur saisonwechsel (A197): Plätze 0–41 wie em.exe, ohne die Vereine auf 42–47
        Assert.Equal(ranking.Select(e => e.Points).OrderDescending(), ranking.Select(e => e.Points));
    }

    [Fact]
    public void ManagerBalance_CountsLeagueGamesHomeAndAway()
    {
        var game = Create(6, 30);
        PlayToSeasonEnd(game);
        var (home, away) = game.ManagerBalance(0);
        Assert.Equal(22, home.Wins + home.Losses + home.Draws);
        Assert.Equal(22, away.Wins + away.Losses + away.Draws);
        var results = game.Results.Where(r => r.Involves(30)).ToList();
        Assert.Equal(results.Where(r => r.Home == 30).Sum(r => r.HomeGoals), home.GoalsFor);
        Assert.Equal(results.Where(r => r.Away == 30).Sum(r => r.HomeGoals), away.GoalsAgainst);
        var total = home.Plus(away);
        Assert.Equal(88, total.PointsPlus + total.PointsMinus);
    }

    [Fact]
    public void History_RecordsLeagueAndCups()
    {
        var game = Create(7, 2);
        PlayToSeasonEnd(game);
        var entry = Assert.Single(game.ManagerHistory(0));
        Assert.Equal(GameState.FirstSeason, entry.Season);
        Assert.Equal(2, entry.Club);
        Assert.Equal(League.Bundesliga, entry.League);
        Assert.Equal(game.History[^1].FinalTables[League.Bundesliga].ToList().IndexOf(2) + 1, entry.Rank);
        Assert.NotEqual(CupOutcome.NotParticipating, entry.Deb);           // Manager-Vereine sind immer im DEB-Pokal
        Assert.NotEqual(CupOutcome.NotParticipating, entry.Europe);        // Verein 2 = Meister zu Spielbeginn
        if (game.History[^1].Titles[Title.GermanChampion] == 2)
            Assert.Equal(LeagueOutcome.GermanChampion, entry.LeagueOutcome);
    }

    /// <summary>
    /// Mehrsaisonprobe (Runde 3): fünf Saisons = vier Saisonwechsel, passiver und aktiver Manager im Hot-Seat.
    /// Die Ewige Tabelle wächst je Saison genau um die Formelpunkte, jede Saison steht genau einmal in „Früher“,
    /// die Ewige Bilanz zählt 44 Ligaspiele je Saison – auch wenn der aktive Manager zwischendurch Geld bewegt.
    /// </summary>
    [Fact]
    public void MultiSeasonProbe_FourSeasonChanges_PassiveAndActiveManager()
    {
        var game = Create(8, 31, 3);
        var active = game.Managers[1];
        long previousTotal = 0;
        for (var season = 0; season < 5; season++)
        {
            var step = 0;
            for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
            {
                Assert.True(guard < 2000);
                foreach (var m in game.Managers)
                    m.Money = Math.Max(m.Money, 10_000_000);
                game.Finance(active).Stadium.TicketPrice = 5 + step++ % 7;
                game.Continue();
            }
            var record = game.History[^1];
            long seasonPoints = 0;
            foreach (var league in Enum.GetValues<League>())
                for (var r = 0; r < 12; r++)
                {
                    var club = record.FinalTables[league][r];
                    seasonPoints += GameState.EternalPoints(record.NextLeague(club, league), r, // S-K4: Liga der nächsten Saison
                        record.Titles.GetValueOrDefault(Title.GermanChampion, -1) == club,
                        record.Titles.GetValueOrDefault(Title.CupWinner, -1) == club,
                        record.Titles.GetValueOrDefault(Title.EuropeanCupWinner, -1) == club);
                }
            var total = game.EternalTable.Sum(e => e.Points);
            Assert.Equal(previousTotal + seasonPoints, total);
            previousTotal = total;
            for (var m = 0; m < 2; m++)
            {
                Assert.Equal(season + 1, game.ManagerHistory(m).Count);
                Assert.Equal(game.Season, game.ManagerHistory(m)[^1].Season);
                var (home, away) = game.ManagerBalance(m);
                Assert.Equal(44 * (season + 1), home.Wins + home.Losses + home.Draws + away.Wins + away.Losses + away.Draws);
            }
            if (season < 4)
                game.StartNextSeason();
        }
        Assert.Equal(5, game.EternalTable.Sum(e => e.Championships));
    }

    /// <summary>Ein zweiter Aufruf in derselben Saison bucht nichts doppelt.</summary>
    [Fact]
    public void RecordSeasonStatistics_IsIdempotent()
    {
        var game = Create(9, 30);
        PlayToSeasonEnd(game);
        var before = game.EternalTable.Select(e => (e.Club, e.Points)).ToList();
        typeof(GameState).GetMethod("RecordSeasonStatistics", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(game, null);
        Assert.Equal(before, game.EternalTable.Select(e => (e.Club, e.Points)));
        Assert.Single(game.ManagerHistory(0));
    }
}
