using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

public class SeasonFlowTests
{
    private static GameState Create(int clubIndex = 2, GameDuration duration = GameDuration.Unbegrenzt, int seed = 3)
    {
        var setup = new GameSetup { Duration = duration };
        setup.Managers.Add(new ManagerSetup { Name = "Test", Head = 0, ClubIndex = clubIndex });
        return NewGame.Create(TestGames.CreateDatabase(), setup, seed);
    }

    /// <summary>„und tschüß…“ bis zum Saisonende; liefert alle Schritte.</summary>
    private static List<SeasonStep> PlaySeason(GameState game)
    {
        var steps = new List<SeasonStep>();
        var guard = 0;
        while (game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver && guard++ < 500)
            steps.AddRange(game.Continue());
        return steps;
    }

    [Fact]
    public void Continue_StopsAtNextOwnMatchDay()
    {
        var game = Create();
        Assert.Equal(0, game.CurrentDay);
        Assert.Equal(1, game.UpcomingLeagueMatchDay);

        var steps = game.Continue();

        Assert.Equal(1, game.PlayedMatchDays);
        Assert.Equal(3, game.CurrentDay); // Mittwoch, 13. Sept.
        Assert.Equal(2, game.UpcomingLeagueMatchDay);
        Assert.Contains(steps, s => s is MatchesStep { Competition: Competition.League, ManagerInvolved: true });
        Assert.Equal("Mittwoch, 13. Sept.  1993", game.DateText);
    }

    [Fact]
    public void AfterMainRound_ClubOutsidePlayoffsJumpsToSeasonEnd()
    {
        // Verein 15 (2. Liga, Mittelfeld): weder Play-Offs noch Aufstiegsrunde → wie im Video vom 44. Spieltag
        // direkt zum „Samstag, 14. April“ mit „Saisonende“.
        var game = Create(clubIndex: 17);
        while (!game.RegularSeasonOver)
            game.Continue();
        var tables = game.FinalTables();
        var rank = tables[League.ZweiteLiga].ToList().IndexOf(17) + 1;
        Assert.InRange(rank, 3, 10); // bei diesem Startwert nicht in der Aufstiegsrunde

        Assert.Equal(SeasonPhase.ClubSeasonOver, game.Phase);
        Assert.Equal(SeasonCalendar.ClubSeasonEnd + 1, game.CurrentDay);
        Assert.Equal("Samstag, 14. April  1994", game.DateText);
        Assert.Null(game.UpcomingMatch(17));
        Assert.NotNull(game.CurrentRecord);
    }

    [Fact]
    public void Playoffs_ProduceChampionFromTopEight()
    {
        var game = Create();
        var steps = PlaySeason(game);

        Assert.NotNull(game.Playoffs);
        var champion = game.Playoffs!.Champion;
        Assert.NotNull(champion);
        Assert.Contains(champion!.Value, game.Playoffs.Seeds);
        Assert.Equal(champion, game.History[^1].Titles[Title.GermanChampion]);
        Assert.Contains(steps, s => s is TrophyStep { Title: Title.GermanChampion });
        Assert.Contains(steps, s => s is BannerStep { Text: "Play-Off / Aufstiegsrunden" });
        Assert.All(game.Playoffs.Rounds.SelectMany(r => r), series =>
        {
            Assert.True(series.IsDecided);
            Assert.InRange(series.Games.Count, 3, 5);
        });
        // Alle Play-Off-Spiele liegen auf Play-Off-Terminen
        Assert.All(game.PlayoffResults, g => Assert.Contains(g.Day, SeasonCalendar.DaysOf(DayEvents.Playoffs)));
        Assert.Contains(steps, s => s is SeasonEndStep { Day: SeasonCalendar.SeasonEnd });
    }

    [Fact]
    public void RelegationGroups_TopTwoPlayInUpperLeague()
    {
        var game = Create();
        PlaySeason(game);
        var record = game.History[^1];

        foreach (var group in game.RelegationGroups)
        {
            Assert.True(group.IsFinished);
            Assert.Equal(6, group.Rounds.Count);
            var table = group.Table(game.Clubs);
            Assert.All(table, r => Assert.Equal(6, r.Games));
            var up = group.Qualified(game.Clubs);
            foreach (var club in group.Clubs)
                Assert.Equal(up.Contains(club) ? group.Upper : group.Lower, record.NextLeague(club, game.Clubs[club].League));
            // gleich viele Auf- wie Absteiger
            Assert.Equal(record.PromotedInto[group.Upper].Count, record.RelegatedInto[group.Lower].Count);
        }
    }

    [Fact]
    public void ThreeSeasons_WithPromotionAndRelegation()
    {
        var game = Create(clubIndex: 30, duration: GameDuration.DreiSaisons, seed: 11);
        var leaguesOfManager = new List<League?>();

        for (var season = 0; season < 3; season++)
        {
            Assert.Equal(1993 + season, game.Season);
            var steps = PlaySeason(game);
            Assert.Equal(44, game.PlayedMatchDays);
            Assert.Single(steps.OfType<SeasonEndStep>());

            var record = game.History[^1];
            Assert.Equal(1993 + season, record.Season);
            Assert.Equal(2, record.LeftOberliga.Count);
            Assert.Equal(2, record.PromotedInto[League.Oberliga].Count);
            Assert.Single(record.Managers);

            if (season < 2)
            {
                Assert.Equal(SeasonPhase.SeasonOver, game.Phase);
                game.StartNextSeason();
                Assert.Equal(SeasonPhase.Running, game.Phase);
                Assert.Equal(0, game.CurrentDay);
                Assert.Equal(0, game.PlayedMatchDays);
                Assert.Empty(game.Results);
                foreach (var league in Enum.GetValues<League>())
                {
                    Assert.Equal(GameState.ClubsPerLeague, game.LeagueClubs(league).Count());
                    Assert.Equal(44, game.Schedules[league].Count);
                    Assert.All(game.Schedules[league].SelectMany(d => d.Matches),
                        m => Assert.Equal(league, game.Clubs[m.Home].League));
                }
                // Neue Vereine spielen tatsächlich in ihrer neuen Liga
                foreach (var club in record.PromotedInto[League.Oberliga])
                    Assert.Equal(League.Oberliga, game.Clubs[club].League);
                foreach (var club in record.LeftOberliga)
                    Assert.Null(game.Clubs[club].League);
            }
            leaguesOfManager.Add(game.Clubs[30].League);
        }

        Assert.Equal(SeasonPhase.GameOver, game.Phase);
        Assert.True(game.IsGameOver);
        Assert.Equal(3, game.History.Count);
        Assert.Throws<InvalidOperationException>(() => game.Continue());

        // Über drei Saisons gab es zwischen allen Ligen Bewegung
        Assert.Contains(game.History, r => r.PromotedInto[League.Bundesliga].Count > 0);
        Assert.Contains(game.History, r => r.PromotedInto[League.ZweiteLiga].Count > 0);
    }

    [Fact]
    public void ManagerInRelegationGroup_StopsOnGroupDates()
    {
        // Stärkster Verein der Oberliga (Index 24) wird mit diesem Startwert Erster → Aufstiegsrunde
        var game = Create(clubIndex: 24, seed: 5);
        while (!game.RegularSeasonOver)
            game.Continue();
        var group = game.RelegationGroups.Single(g => g.Upper == League.ZweiteLiga);
        Assert.Contains(24, group.Clubs);

        var stops = new List<int>();
        while (game.Phase == SeasonPhase.Running)
        {
            stops.Add(game.CurrentDay);
            Assert.Equal(Competition.Relegation, game.UpcomingMatch(24)!.Value.Competition);
            game.Continue();
        }
        var playoffDays = SeasonCalendar.DaysOf(DayEvents.Playoffs);
        Assert.Equal(RelegationGroup.DateIndices.Select(i => playoffDays[i]), stops);
        Assert.Equal(SeasonCalendar.ClubSeasonEnd + 1, game.CurrentDay);
    }

    [Fact]
    public void PremiumForFirstPlaceInOberliga_AsInVideo()
    {
        Assert.Equal(400_000, GameState.Premium(League.Oberliga, 1));
        Assert.Equal(0, GameState.Premium(League.Oberliga, 4));
    }

    [Fact]
    public void Debts_LeadToDismissalAfterEightWeeks()
    {
        var game = Create();
        game.ActiveManager.Money = -2_000_000;
        var steps = new List<SeasonStep>();
        while (!game.IsGameOver)
            steps.AddRange(game.Continue());

        var dismissal = Assert.Single(steps.OfType<DismissalStep>());
        Assert.Equal(Manager.DebtDaysUntilDismissal - 1, dismissal.Day);
        Assert.True(game.Managers[0].Dismissed);
    }

    [Fact]
    public void Dismissal_OtherManagersContinue()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "A", ClubIndex = 2 });
        setup.Managers.Add(new ManagerSetup { Name = "B", ClubIndex = 14 });
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, 1);
        game.Managers[0].Money = -1_600_000;
        while (!game.Managers[0].Dismissed)
            game.Continue();
        Assert.False(game.IsGameOver);
        Assert.Equal(1, game.CurrentManager);
        Assert.Single(game.ActiveManagers);
    }
}
