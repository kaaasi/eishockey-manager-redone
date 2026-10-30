using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Paket „wettbewerbe“, PR 3: Weltmeisterschaft über <see cref="ISeasonCompetition"/>.
/// Befund vorher (nach PR 2): Mit „Europapokal + WM an“ gab es keine WM – zwischen 14. und 28. April fand kein Spiel
/// statt, am Saisonende stand beim Weltmeister „Wer weiß...“.
/// </summary>
public class WorldCupTests
{
    private static GameState Create(int seed, bool world = true, params int[] clubs)
    {
        var setup = new GameSetup { EuropeAndWorldCup = world };
        foreach (var (club, i) in clubs.Select((c, i) => (c, i)))
            setup.Managers.Add(new ManagerSetup { Name = $"M{i}", Head = i, ClubIndex = club });
        return NewGame.Create(TestGames.CreateDatabase(), setup, seed);
    }

    private static List<SeasonStep> PlayToSeasonEnd(GameState game)
    {
        var steps = new List<SeasonStep>();
        for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
        {
            Assert.True(guard < 2000, "Saisonende nicht erreicht");
            foreach (var manager in game.Managers)
                manager.Money = Math.Max(manager.Money, 10_000_000);
            steps.AddRange(game.Continue());
        }
        return steps;
    }

    /// <summary>Regressionstest (vor dem Fix rot): WM-Spiele zwischen Vereinssaison und Saisonende, Weltmeister im Rückblick.</summary>
    [Fact]
    public void Season_WithWorldCupEnabled_PlaysWorldCupAfterClubSeason_AndCrownsChampion()
    {
        var game = Create(5, true, 2);
        var steps = PlayToSeasonEnd(game);
        var wmDays = SeasonCalendar.DaysOf(DayEvents.WorldCup);
        var played = steps.OfType<MatchesStep>().Where(s => s.Competition == Competition.WorldCup).Select(s => s.Day).ToList();
        Assert.Equal(wmDays, played);
        Assert.True(game.History[^1].Titles.TryGetValue(Title.WorldChampion, out var champion), "Kein Weltmeister am Saisonende");
        Assert.InRange(champion, NationalTeams.First, NationalTeams.Last);
    }

    /// <summary>[ORIGINAL] Spielwerte aus em.exe, Abgleich mit „Stärken Länderteams – Gesamt“ (Video 13:18): Kondition, Technik.</summary>
    [Theory]
    [InlineData(52, 99, 104)] // Russland (Anzeige auf 99 begrenzt)
    [InlineData(53, 91, 99)]  // Schweden
    [InlineData(54, 98, 93)]  // Kanada
    [InlineData(56, 88, 91)]  // Deutschland
    [InlineData(63, 96, 96)]  // Tschechei
    [InlineData(61, 72, 79)]  // Frankreich
    public void NationalTeamRatings_MatchVideo(int club, int condition, int technique)
    {
        var (c, t) = NationalTeamRatings.Average(club);
        Assert.Equal(condition, (int)c);
        Assert.Equal(technique, (int)t);
    }

    [Fact]
    public void FullTournament_GroupsQuarterFinalsPlacements_AsInOriginal()
    {
        for (var seed = 1; seed <= 4; seed++)
        {
            var game = Create(seed, true, 30);
            var steps = PlayToSeasonEnd(game);
            var wm = game.WorldCup!;
            var days = SeasonCalendar.DaysOf(DayEvents.WorldCup);

            Assert.Equal(Enumerable.Range(52, 12), wm.Groups.SelectMany(g => g.Teams).Order());
            foreach (var group in wm.Groups)
            {
                Assert.Equal(15, group.Results.Count); // jeder gegen jeden
                Assert.Equal(15, group.Results.Select(r => (Math.Min(r.Home, r.Away), Math.Max(r.Home, r.Away))).Distinct().Count());
                Assert.All(group.Results, r => Assert.Contains(r.Day, days.Take(5)));
            }

            var a = wm.Groups[0].Table(game.Clubs).Select(r => r.Club.Index).ToList();
            var b = wm.Groups[1].Table(game.Clubs).Select(r => r.Club.Index).ToList();
            var qf = wm.Knockout[0];
            Assert.Equal([(a[0], b[3]), (a[1], b[2]), (a[2], b[1]), (a[3], b[0])], qf.Select(m => (m.Home, m.Away)));
            Assert.All(qf, m => Assert.Equal(days[5], m.Day));

            var second = wm.Knockout[1];
            Assert.Equal([(qf[0].Winner, qf[1].Winner), (qf[2].Winner, qf[3].Winner), (qf[0].Loser, qf[1].Loser), (qf[2].Loser, qf[3].Loser)],
                second.Select(m => (m.Home, m.Away)));
            var third = wm.Knockout[2];
            Assert.Equal([(second[0].Winner, second[1].Winner), (second[0].Loser, second[1].Loser),
                (second[2].Winner, second[3].Winner), (second[2].Loser, second[3].Loser)], third.Select(m => (m.Home, m.Away)));
            Assert.All(third, m => Assert.Equal(days[7], m.Day));

            Assert.Equal(third[0].Winner, wm.Winner);
            Assert.Equal(12, wm.Ranking.Distinct().Count());
            Assert.Equal([third[0].Winner, third[0].Loser, third[1].Winner, third[1].Loser, third[2].Winner, third[2].Loser, third[3].Winner, third[3].Loser],
                wm.Ranking.Take(8));
            Assert.Equal([wm.Winner!.Value], steps.OfType<TrophyStep>().Where(t => t.Title == Title.WorldChampion).Select(t => t.Club));
            Assert.Equal(wm.Winner, game.History[^1].Titles[Title.WorldChampion]);
        }
    }

    /// <summary>Kein Manager spielt die WM: „und tschüß…“ hält nach dem 14. April erst am Saisonende.</summary>
    [Fact]
    public void Continue_AfterClubSeason_RunsThroughWorldCupToSeasonEnd()
    {
        var game = Create(3, true, 30);
        while (game.Phase == SeasonPhase.Running)
            game.Continue();
        Assert.Equal(SeasonCalendar.ClubSeasonEnd + 1, game.CurrentDay);
        var steps = game.Continue();
        Assert.Equal(SeasonPhase.SeasonOver, game.Phase);
        Assert.Equal(8, steps.OfType<MatchesStep>().Count(s => s.Competition == Competition.WorldCup && !s.ManagerInvolved));
        Assert.Single(steps.OfType<SeasonEndStep>());
    }

    [Fact]
    public void WorldCupOff_NoGamesNoTitle()
    {
        var game = Create(4, false, 2);
        Assert.Null(game.WorldCup);
        PlayToSeasonEnd(game);
        Assert.False(game.History[^1].Titles.ContainsKey(Title.WorldChampion));
    }

    [Fact]
    public void LeagueResults_DoNotDependOnWorldCup()
    {
        var with = Create(6, true, 2);
        var without = Create(6, false, 2);
        TestSeason.PlayRegularSeason(with);
        TestSeason.PlayRegularSeason(without);
        Assert.Equal(without.Results, with.Results);
    }

    /// <summary>
    /// Mehrsaisonprobe (Runde 3): fünf Saisons = vier Saisonwechsel, passiver und aktiver Manager im Hot-Seat.
    /// Je Saison: WM vollständig über <see cref="GameState.Continue"/>, neuer Wettbewerb nach dem Saisonwechsel,
    /// Weltmeister im Rückblick, kein Geld durch WM-Spiele (Nationalteams haben keinen Manager), Eintritt des
    /// passiven Managers weiter exakt aus Liga- und Pokal-Heimspielen.
    /// </summary>
    [Fact]
    public void MultiSeasonProbe_FourSeasonChanges_PassiveAndActiveManager()
    {
        var game = Create(9, true, 33, 4);
        var passive = game.Managers[0];
        var active = game.Managers[1];
        foreach (var manager in game.Managers)
        {
            var stadium = game.Finance(manager).Stadium;
            stadium.Seats = 9;
            stadium.Standing = 0;
        }
        for (var season = 0; season < 5; season++)
        {
            var step = 0;
            for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
            {
                Assert.True(guard < 2000);
                foreach (var manager in game.Managers)
                    manager.Money = Math.Max(manager.Money, 10_000_000);
                game.Finance(active).Stadium.TicketPrice = 5 + step++ % 7;
                // Spur heimspiel-statistik (#136): beide Manager halten den Kader, sonst „Weniger als neun Spieler“ ohne Eintritt
                foreach (var manager in game.Managers)
                    TestSeason.KeepSquad(game, manager);
                if (game.Phase == SeasonPhase.ClubSeasonOver) // WM: kein Manager-Geld durch Eintritt
                {
                    var before = game.Managers.Select(m => game.Finance(m).Book.Current(FinanceCategory.Eintritt)).ToList();
                    game.Continue();
                    Assert.Equal(before, game.Managers.Select(m => game.Finance(m).Book.Current(FinanceCategory.Eintritt)));
                }
                else
                    game.Continue();
            }
            var wm = game.WorldCup!;
            Assert.Equal(30 + 12, wm.Matches.Count());
            Assert.Equal(wm.Winner, game.History[^1].Titles[Title.WorldChampion]);

            var club = passive.ClubIndex;
            var homeGames = game.Results.Count(r => r.Home == club) + game.PlayoffResults.Count(r => r.Home == club)
                + game.RelegationGroups.Sum(g => g.Results.Count(r => r.Home == club)) + game.CupMatches.Count(m => m.Home == club);
            var stadium = game.Finance(passive).Stadium;
            Assert.Equal((long)homeGames * stadium.Capacity * stadium.TicketPrice, game.Finance(passive).Book.Current(FinanceCategory.Eintritt));

            if (season < 4)
            {
                game.StartNextSeason();
                Assert.Empty(game.WorldCup!.Matches);
                Assert.Null(game.WorldCup.Winner);
                Assert.Empty(game.WorldCup.Knockout);
            }
        }
        Assert.Equal(5, game.History.Count);
        Assert.All(game.History, r => Assert.True(r.Titles.ContainsKey(Title.WorldChampion)));
    }
}
