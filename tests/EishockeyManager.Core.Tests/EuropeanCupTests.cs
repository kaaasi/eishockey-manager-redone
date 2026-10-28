using System.Text;
using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Paket „wettbewerbe“, PR 2: Europapokal über <see cref="ISeasonCompetition"/>.
/// Befund vorher (nach PR 1): Mit „Europapokal + WM an“ (Standard) gab es keinen Europapokal – der Deutsche Meister
/// (zu Spielbeginn Datenbankverein 2, im Original Düsseldorfer EG) spielte im November nie, am Saisonende fehlte
/// der Europacupsieger.
/// </summary>
public class EuropeanCupTests
{
    /// <summary>Synthetische Datenbank mit Europapokal-Gegnern: gerade Indizes stark (90), ungerade schwach (40).</summary>
    private static ClubDatabase DatabaseWithOpponents()
    {
        var data = new byte[ClubDatabase.FileLength];
        for (var i = 0; i < ClubDatabase.ClubsWithSquad; i++)
        {
            Encoding.ASCII.GetBytes($"VEREIN {i}").CopyTo(data, ClubDatabase.SquadOffset + i * ClubDatabase.SquadRecordLength);
            data[ClubDatabase.StrengthOffset + i * 3] = (byte)(100 - i % 12 * 5);
        }
        for (var i = ClubDatabase.ClubsWithSquad; i < ClubDatabase.TotalClubs; i++)
        {
            Encoding.ASCII.GetBytes($"EUROPA {i}").CopyTo(data, ClubDatabase.EuropeanNamesOffset + (i - ClubDatabase.ClubsWithSquad) * ClubDatabase.NameLength);
            data[ClubDatabase.StrengthOffset + i * 3] = (byte)(i % 2 == 0 ? 90 : 40);
        }
        return ClubDatabase.Parse(data);
    }

    private static GameState Create(int seed, bool europe = true, params int[] clubs)
    {
        var setup = new GameSetup { EuropeAndWorldCup = europe };
        for (var i = 0; i < clubs.Length; i++)
            setup.Managers.Add(new ManagerSetup { Name = $"M{i}", Head = i, ClubIndex = clubs[i] });
        return NewGame.Create(DatabaseWithOpponents(), setup, seed);
    }

    private static List<SeasonStep> PlayToSeasonEnd(GameState game, Action<GameState>? beforeEachStep = null)
    {
        var steps = new List<SeasonStep>();
        for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
        {
            Assert.True(guard < 2000, "Saisonende nicht erreicht");
            foreach (var manager in game.Managers)
                manager.Money = Math.Max(manager.Money, 10_000_000);
            beforeEachStep?.Invoke(game);
            steps.AddRange(game.Continue());
        }
        return steps;
    }

    /// <summary>Regressionstest (vor dem Fix rot): Der Deutsche Meister (Verein 2) spielt im Europapokal.</summary>
    [Fact]
    public void GermanChampion_PlaysEuropeanCupOverContinue_AndSeasonHasWinner()
    {
        var game = Create(5, true, 2);
        var stops = 0;
        PlayToSeasonEnd(game, g =>
        {
            if (g.Phase == SeasonPhase.Running && g.MatchOn(g.CurrentDay, 2) is { Competition: Competition.EuropeanCup })
                stops++;
        });
        Assert.True(stops >= 6, $"Deutscher Meister hat nur {stops} Europapokaltermine erreicht (Gruppenphase: sechs)");
        Assert.True(game.History[^1].Titles.ContainsKey(Title.EuropeanCupWinner), "Kein Europacupsieger am Saisonende");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Draw_EightDistinctTeams_GermanChampionIncluded_GroupsBySlot(int seed)
    {
        var game = Create(seed, true, 30);
        var cup = game.EuropeanCup!;
        Assert.Equal([EuropeanCup.InitialChampion], cup.GermanEntrants);
        Assert.Equal(EuropeanCup.Teams, cup.Draw.Count);
        Assert.Equal(EuropeanCup.Teams, cup.Draw.Distinct().Count());
        Assert.Contains(EuropeanCup.InitialChampion, cup.Draw);
        Assert.All(cup.Draw.Where(c => c != EuropeanCup.InitialChampion), c => Assert.InRange(c, 64, 149));
        Assert.Equal(cup.Draw.Take(4), cup.Groups[0].Teams);
        Assert.Equal(cup.Draw.Skip(4), cup.Groups[1].Teams);
        Assert.Null(cup.FinalRound);
    }

    /// <summary>[ORIGINAL] 0x341AB: Gegner unter Stärke 70 nur zu einem Viertel angenommen → Anteil 1/4 : (1/4 + 1) = 20 %.</summary>
    [Fact]
    public void WeakOpponents_AreAcceptedOnlyOneInFour()
    {
        int weak = 0, total = 0;
        for (var seed = 1; seed <= 60; seed++)
        {
            var game = Create(seed, true, 30);
            foreach (var club in game.EuropeanCup!.Draw.Where(c => c >= EuropeanCup.FirstOpponent))
            {
                total++;
                if (game.Clubs[club].Strength < EuropeanCup.WeakOpponent)
                    weak++;
            }
        }
        var share = (double)weak / total;
        Assert.InRange(share, 0.14, 0.27);
    }

    /// <summary>[ORIGINAL] Spielplan „abcd / dabc / acbd“ (em.exe DGROUP 0x474 ff., Video 13:11), Rückrunde getauscht.</summary>
    [Fact]
    public void GroupSchedule_AsInOriginal()
    {
        var group = new CupGroup(1, [10, 20, 30, 40]);
        Assert.Equal([(10, 20), (30, 40)], group.Pairings(0));
        Assert.Equal([(40, 10), (20, 30)], group.Pairings(1));
        Assert.Equal([(10, 30), (20, 40)], group.Pairings(2));
        Assert.Equal([(20, 10), (40, 30)], group.Pairings(3));
        Assert.Equal(6, group.RoundCount);
        // Jeder spielt gegen jeden einmal zu Hause und einmal auswärts
        var all = Enumerable.Range(0, 6).SelectMany(group.Pairings).ToList();
        Assert.Equal(12, all.Distinct().Count());
    }

    [Fact]
    public void FullCompetition_GroupsThenFinalRound_WinnerIsFinalLeader()
    {
        var germanTrophies = 0;
        var foreignWinners = 0;
        // Bis beide Fälle (deutscher und ausländischer Sieger) vorkamen, höchstens 16 Saisons
        for (var seed = 1; seed <= 16 && (germanTrophies == 0 || foreignWinners == 0 || seed <= 4); seed++)
        {
            var game = Create(seed, true, 30);
            var steps = PlayToSeasonEnd(game);
            var cup = game.EuropeanCup!;
            var days = SeasonCalendar.DaysOf(DayEvents.EuropeanCup);

            foreach (var group in cup.Groups)
            {
                Assert.Equal(12, group.Results.Count); // 6 Termine × 2 Spiele
                Assert.Equal(days.Take(6).SelectMany(d => new[] { d, d }), group.Results.Select(r => r.Day));
                Assert.All(group.Teams, t => Assert.Equal(3, group.Results.Count(r => r.Home == t)));
            }
            var tables = cup.Groups.Select(g => g.Table(game.Clubs)).ToList();
            var final = cup.FinalRound!;
            Assert.Equal([tables[0][0].Club.Index, tables[0][1].Club.Index, tables[1][0].Club.Index, tables[1][1].Club.Index], final.Teams);
            Assert.Equal(12, final.Results.Count);
            Assert.Equal(days.Skip(6).SelectMany(d => new[] { d, d }), final.Results.Select(r => r.Day));
            Assert.Equal(final.Table(game.Clubs)[0].Club.Index, cup.Winner);
            Assert.Equal(cup.Winner, game.History[^1].Titles[Title.EuropeanCupWinner]);

            // Meisterfoto nur für einen deutschen Sieger
            var trophies = steps.OfType<TrophyStep>().Where(t => t.Title == Title.EuropeanCupWinner).ToList();
            if (cup.Winner < EuropeanCup.FirstOpponent)
            {
                Assert.Equal([cup.Winner!.Value], trophies.Select(t => t.Club));
                germanTrophies++;
            }
            else
            {
                Assert.Empty(trophies);
                foreignWinners++;
            }
        }
        Assert.True(germanTrophies > 0, "Kein deutscher Sieger – Meisterfoto ungeprüft");
        Assert.True(foreignWinners > 0, "Kein ausländischer Sieger – fehlendes Meisterfoto ungeprüft");
    }

    [Fact]
    public void Continue_StopsExactlyOnOwnEuropeanGames()
    {
        var game = Create(3, true, EuropeanCup.InitialChampion);
        var stops = new List<int>();
        PlayToSeasonEnd(game, g =>
        {
            if (g.Phase == SeasonPhase.Running && SeasonCalendar.Events(g.CurrentDay).HasFlag(DayEvents.EuropeanCup))
                stops.Add(g.CurrentDay);
        });
        var own = game.EuropeanCup!.Matches.Where(m => m.Involves(EuropeanCup.InitialChampion)).Select(m => m.Day).ToList();
        Assert.True(own.Count >= 6);
        Assert.Equal(own, stops);
    }

    [Fact]
    public void EuropeOff_NoEuropeanCup()
    {
        var game = Create(4, false, EuropeanCup.InitialChampion);
        Assert.Null(game.EuropeanCup);
        PlayToSeasonEnd(game);
        Assert.False(game.History[^1].Titles.ContainsKey(Title.EuropeanCupWinner));
        Assert.DoesNotContain(game.CupMatches, m => m.Involves(EuropeanCup.InitialChampion) && SeasonCalendar.Events(m.Day).HasFlag(DayEvents.EuropeanCup));
    }

    [Fact]
    public void LeagueResults_DoNotDependOnEuropeSetting()
    {
        var with = Create(6, true, 2);
        var without = Create(6, false, 2);
        TestSeason.PlayRegularSeason(with);
        TestSeason.PlayRegularSeason(without);
        Assert.Equal(without.Results, with.Results);
    }

    /// <summary>Exakte Abrechnung: nach jedem Schritt Eintritt = Heimspiele × Kapazität × Preis dieses Schritts.</summary>
    [Fact]
    public void ActiveManager_EveryEuropeanHomeGameSettledOnce_AtPriceOfItsDay()
    {
        var found = 0;
        for (var seed = 1; seed <= 3; seed++)
        {
            var game = Create(seed, true, EuropeanCup.InitialChampion);
            var manager = game.ActiveManager;
            var stadium = game.Finance(manager).Stadium;
            stadium.Seats = 13;
            stadium.Standing = 0;
            var club = manager.ClubIndex;
            long expected = 0;
            var counted = 0;
            var price = 4;
            while (game.Phase == SeasonPhase.Running)
            {
                manager.Money = 10_000_000;
                stadium.TicketPrice = price;
                game.Continue();
                var homeGames = HomeGames(game, club);
                expected += (long)(homeGames - counted) * stadium.Capacity * price;
                counted = homeGames;
                Assert.Equal(expected, game.Finance(manager).Book.Current(FinanceCategory.Eintritt));
                price = price % 9 + 4;
            }
            found += game.EuropeanCup!.Matches.Count(m => m.Home == club);
        }
        Assert.True(found >= 9, "Zu wenige Europapokal-Heimspiele – Test prüft nichts");
    }

    /// <summary>
    /// Mehrsaisonprobe (Runde 3): fünf Saisons = vier Saisonwechsel, passiver Manager mit dem Meister der
    /// Vorsaison (Verein 2) und aktiver Manager (wechselnde Eintrittspreise) im Hot-Seat. Je Saison: Teilnehmer aus
    /// dem Saisonrückblick der Vorsaison, vollständiger Wettbewerb über <see cref="GameState.Continue"/>, Sieger im
    /// Rückblick, kein Spiel einer Vorsaison übrig, Eintritt des passiven Managers exakt.
    /// </summary>
    [Fact]
    public void MultiSeasonProbe_FourSeasonChanges_PassiveAndActiveManager()
    {
        var game = Create(8, true, EuropeanCup.InitialChampion, 7);
        var passive = game.Managers[0];
        var active = game.Managers[1];
        foreach (var manager in game.Managers)
        {
            var stadium = game.Finance(manager).Stadium;
            stadium.Seats = 9;
            stadium.Standing = 0;
        }
        var managerEuropeGames = 0;
        for (var season = 0; season < 5; season++)
        {
            var cup = game.EuropeanCup!;
            Assert.Equal(EuropeanCup.Entrants(game), cup.GermanEntrants);
            Assert.All(cup.GermanEntrants, g => Assert.Contains(g, cup.Draw));
            if (season > 0)
                Assert.Contains(game.History[^1].Titles[Title.GermanChampion], cup.Draw);

            var step = 0;
            PlayToSeasonEnd(game, g => g.Finance(active).Stadium.TicketPrice = 5 + step++ % 7);

            Assert.Equal(24, cup.Groups.Sum(g => g.Results.Count));
            Assert.Equal(12, cup.FinalRound!.Results.Count);
            Assert.All(cup.Matches, m => Assert.Contains(m.Day, SeasonCalendar.DaysOf(DayEvents.EuropeanCup)));
            Assert.Equal(cup.Winner, game.History[^1].Titles[Title.EuropeanCupWinner]);
            managerEuropeGames += cup.Matches.Count(m => game.Managers.Any(x => m.Involves(x.ClubIndex)));

            var club = passive.ClubIndex;
            var stadiumPassive = game.Finance(passive).Stadium;
            Assert.Equal((long)HomeGames(game, club) * stadiumPassive.Capacity * stadiumPassive.TicketPrice,
                game.Finance(passive).Book.Current(FinanceCategory.Eintritt));

            if (season < 4)
            {
                game.StartNextSeason();
                Assert.Empty(game.EuropeanCup!.Matches);
                Assert.Null(game.EuropeanCup.Winner);
                Assert.Null(game.EuropeanCup.FinalRound);
            }
        }
        Assert.Equal(5, game.History.Count);
        Assert.True(managerEuropeGames >= 6, "Manager nie im Europapokal – Probe prüft die Abrechnung nicht");
    }

    private static int HomeGames(GameState game, int club) =>
        game.Results.Count(r => r.Home == club)
        + game.PlayoffResults.Count(r => r.Home == club)
        + game.RelegationGroups.Sum(g => g.Results.Count(r => r.Home == club))
        + game.CupMatches.Count(m => m.Home == club);
}
