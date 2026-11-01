using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Paket „wettbewerbe“, PR 1: DEB-Pokal über <see cref="ISeasonCompetition"/>.
/// Befund vorher (main cc26bde): <see cref="GameState.Competitions"/> blieb leer – über <see cref="GameState.Continue"/>
/// gab es nie ein Pokalspiel, am Saisonende fehlte der DEB-Pokalsieger („Wer weiß...“), obwohl „DEB-Pokal an“ eingestellt war.
/// </summary>
public class DebCupTests
{
    private static GameState Create(int seed, bool debCup = true, params int[] clubs)
    {
        var setup = new GameSetup { DebCup = debCup };
        for (var i = 0; i < clubs.Length; i++)
            setup.Managers.Add(new ManagerSetup { Name = $"M{i}", Head = i, ClubIndex = clubs[i] });
        return NewGame.Create(TestGames.CreateDatabase(), setup, seed);
    }

    private static void PlayToSeasonEnd(GameState game, Action<GameState>? beforeEachStep = null)
    {
        for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
        {
            Assert.True(guard < 2000, "Saisonende nicht erreicht");
            foreach (var manager in game.Managers)
                manager.Money = Math.Max(manager.Money, 10_000_000); // keine Entlassung im Testlauf
            beforeEachStep?.Invoke(game);
            game.Continue();
        }
    }

    /// <summary>Regressionstest (vor dem Fix rot): eine Saison mit „DEB-Pokal an“ über die Zeitleiste.</summary>
    [Fact]
    public void Season_WithDebCupEnabled_PlaysCupOverContinue_AndCrownsWinner()
    {
        var game = TestGames.Create(seed: 5);
        Assert.True(game.Setup.DebCup);
        var club = game.ActiveManager.ClubIndex;

        var cupDaysStopped = 0;
        PlayToSeasonEnd(game, g =>
        {
            if (g.Phase == SeasonPhase.Running && g.MatchOn(g.CurrentDay, club) is { Competition: Competition.DebCup })
                cupDaysStopped++;
        });

        Assert.True(cupDaysStopped >= 2, $"Manager hat nur {cupDaysStopped} Pokaltermine erreicht (jede Runde mindestens zwei Spiele)");
        var record = game.History[^1];
        Assert.True(record.Titles.TryGetValue(Title.CupWinner, out var winner), "Kein DEB-Pokalsieger am Saisonende");
        Assert.NotNull(game.Clubs[winner].League);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void FirstRound_Has32DistinctLeagueClubs_IncludingAllManagerClubs(int seed)
    {
        // Ein Bundesligist, ein Zweitligist und ein Oberligist als Manager
        var game = Create(seed, true, 3, 17, 33);
        var cup = game.DebCup!;

        var participants = cup.Participants;
        Assert.Equal(DebCup.Teams, participants.Count);
        Assert.Equal(DebCup.Teams, participants.Distinct().Count());
        Assert.All(participants, c => Assert.NotNull(game.Clubs[c].League));
        Assert.Contains(3, participants);
        Assert.Contains(17, participants);
        Assert.Contains(33, participants);
        Assert.Single(cup.Rounds);
        Assert.Equal(16, cup.Rounds[0].Count);
    }

    /// <summary>[ORIGINAL] 0x347EF: der unterklassige Verein steht links und hat im ersten und dritten Spiel Heimrecht.</summary>
    [Fact]
    public void LowerClassClub_HasHomeRight_InEveryRound()
    {
        var checkedTies = 0;
        var mixed = 0;
        for (var seed = 1; seed <= 4; seed++)
        {
            var game = Create(seed, true, 30);
            PlayToSeasonEnd(game);
            foreach (var tie in game.DebCup!.Rounds.SelectMany(r => r))
            {
                var first = CupClass.Of(game.Clubs[tie.First]);
                var second = CupClass.Of(game.Clubs[tie.Second]);
                Assert.False((second == 4 && first < 4) || (second == 2 && first == 1),
                    $"{game.Clubs[tie.First].Name} ({first}) hat Heimrecht gegen {game.Clubs[tie.Second].Name} ({second})");
                checkedTies++;
                if (first != second)
                    mixed++;
            }
        }
        Assert.Equal(4 * 31, checkedTies);
        Assert.True(mixed > 20, "Kaum Paarungen verschiedener Ligen – Test prüft nichts");
    }

    /// <summary>[ORIGINAL] 0x34B76: höchstens drei Spiele, das dritte nur bei 1:1, Heimrecht wechselt, feste Termine.</summary>
    [Fact]
    public void EveryTie_IsBestOfThree_WithAlternatingHomeRight_OnCalendarDays()
    {
        var thirdGames = 0;
        for (var seed = 1; seed <= 3; seed++)
        {
            var game = Create(seed, true, 5);
            PlayToSeasonEnd(game);
            var cup = game.DebCup!;
            Assert.Equal(DebCup.RoundCount, cup.Rounds.Count);
            Assert.Equal([16, 8, 4, 2, 1], cup.Rounds.Select(r => r.Count));
            for (var r = 0; r < cup.Rounds.Count; r++)
            {
                foreach (var tie in cup.Rounds[r])
                {
                    Assert.InRange(tie.Games.Count, 2, 3);
                    Assert.NotNull(tie.Winner);
                    if (tie.Games.Count == 3)
                    {
                        Assert.Equal(1, tie.Games.Take(2).Count(g => g.Winner == tie.First));
                        thirdGames++;
                    }
                    else
                        Assert.Equal(tie.Games[0].Winner, tie.Games[1].Winner);
                    for (var g = 0; g < tie.Games.Count; g++)
                    {
                        Assert.Equal(g == 1 ? (tie.Second, tie.First) : (tie.First, tie.Second), (tie.Games[g].Home, tie.Games[g].Away));
                        Assert.Equal(DebCup.DayOf(r, g), tie.Games[g].Day);
                    }
                }
                if (r > 0) // Sieger der Vorrunde und nur diese
                    Assert.Equal(cup.Rounds[r - 1].Select(t => t.Winner!.Value).Order(),
                        cup.Rounds[r].SelectMany(t => new[] { t.First, t.Second }).Order());
            }
            Assert.Equal(cup.Rounds[^1][0].Winner, cup.Winner);
            Assert.Equal(cup.Winner, game.History[^1].Titles[Title.CupWinner]);
        }
        Assert.True(thirdGames > 0, "Kein drittes Spiel – Test prüft nichts");
    }

    /// <summary>„und tschüß…“ hält genau an den Pokalterminen, an denen der Manager-Verein spielt.</summary>
    [Fact]
    public void Continue_StopsExactlyOnOwnCupGames()
    {
        for (var seed = 1; seed <= 3; seed++)
        {
            var game = Create(seed, true, 20);
            var stops = new List<int>();
            PlayToSeasonEnd(game, g =>
            {
                if (g.Phase == SeasonPhase.Running && SeasonCalendar.Events(g.CurrentDay).HasFlag(DayEvents.DebCup))
                    stops.Add(g.CurrentDay);
            });
            var ownGames = game.DebCup!.Matches.Where(m => m.Involves(20)).Select(m => m.Day).ToList();
            Assert.Equal(ownGames, stops);
        }
    }

    [Fact]
    public void DebCupDisabled_NoCupGamesNoTitle()
    {
        var game = Create(4, false, 2);
        Assert.Null(game.DebCup);
        Assert.DoesNotContain(game.Competitions, c => c.Kind == Competition.DebCup);
        PlayToSeasonEnd(game);
        Assert.False(game.History[^1].Titles.ContainsKey(Title.CupWinner));
    }

    /// <summary>Der Pokal hat eine eigene Zufallsquelle: Liga und Nachsaison sind mit und ohne Pokal gleich.</summary>
    [Fact]
    public void LeagueResults_DoNotDependOnCupSetting()
    {
        var with = Create(6, true, 2);
        var without = Create(6, false, 2);
        // Gleiche Aufstellungen: Pokalspiele verändern Energie/Verletzungen nicht (MatchEngine nutzt nur die Stärke)
        TestSeason.PlayRegularSeason(with);
        TestSeason.PlayRegularSeason(without);
        Assert.Equal(without.Results, with.Results);
    }

    /// <summary>
    /// Mehrsaisonprobe (Runde 3, verbindlich): fünf Saisons = vier Saisonwechsel, ein passiver und ein aktiver Manager
    /// im Hot-Seat. Je Saison: Pokal vollständig über <see cref="GameState.Continue"/>, neue Auslosung aus den
    /// aktuellen Ligen, Pokalsieger im Saisonrückblick, jedes Heimspiel (Liga, Nachsaison, Pokal) genau einmal mit
    /// dem zum Spielzeitpunkt gültigen Eintrittspreis im Finanzbuch.
    /// </summary>
    [Fact]
    public void MultiSeasonProbe_FourSeasonChanges_PassiveAndActiveManager()
    {
        var game = Create(7, true, 30, 6); // passiv: Oberligist 30, aktiv: Bundesligist 6
        var passive = game.Managers[0];
        var active = game.Managers[1];
        for (var i = 0; i < 2; i++)
        {
            var stadium = game.Finance(game.Managers[i]).Stadium;
            stadium.Seats = 9 + 5 * i;  // Nachfrage liegt sicher darüber: Betrag je Spiel bekannt
            stadium.Standing = 0;
        }

        var cupHomeGames = new int[2];
        for (var season = 0; season < 5; season++)
        {
            var expected = new long[2];
            var settledGames = new int[2];
            var step = 0;
            PlayToSeasonEnd(game, g =>
            {
                // aktiver Manager: Eintrittspreis vor jedem Schritt ändern
                g.Finance(active).Stadium.TicketPrice = 5 + step++ % 7;
                // Spur heimspiel-statistik (#136): beide Manager halten den Kader, sonst „Weniger als neun Spieler“ ohne Eintritt
                foreach (var m in g.Managers)
                    TestSquadKeeper.KeepPlayable(g, m);
            });

            // Kontrolle nach der Saison über alle Heimspiele in Spielreihenfolge
            for (var i = 0; i < 2; i++)
            {
                var manager = game.Managers[i];
                var club = manager.ClubIndex;
                var homeGames = HomeGameDays(game, club);
                var cup = game.DebCup!.Matches.Count(m => m.Home == club);
                cupHomeGames[i] += cup;
                settledGames[i] = homeGames.Count;
                var capacity = game.Finance(manager).Stadium.Capacity;
                if (manager == passive)
                    expected[i] = (long)homeGames.Count * capacity * game.Finance(manager).Stadium.TicketPrice;
                Assert.True(homeGames.Count > 0);
            }
            Assert.Equal(expected[0], game.Finance(passive).Book.Current(FinanceCategory.Eintritt));

            // aktiver Manager: Summe muss zu einer Preisfolge passen – mindestens jedes Spiel einmal zum Mindestpreis,
            // höchstens jedes Spiel einmal zum Höchstpreis (doppelte Buchung sprengt die Obergrenze bei ≥ 2 Spielen)
            var activeCapacity = game.Finance(active).Stadium.Capacity;
            var activeSum = game.Finance(active).Book.Current(FinanceCategory.Eintritt);
            Assert.InRange(activeSum, (long)settledGames[1] * activeCapacity * 5, (long)settledGames[1] * activeCapacity * 11);

            var cupSeason = game.DebCup!;
            Assert.NotNull(cupSeason.Winner);
            Assert.Equal(cupSeason.Winner, game.History[^1].Titles[Title.CupWinner]);
            Assert.All(cupSeason.Participants, c => Assert.NotNull(game.Clubs[c].League));
            Assert.Contains(passive.ClubIndex, cupSeason.Participants);
            Assert.Contains(active.ClubIndex, cupSeason.Participants);
            Assert.Equal(31, cupSeason.Rounds.Sum(r => r.Count));

            if (season < 4)
            {
                game.StartNextSeason();
                // neue Saison: neue Auslosung mit den Ligen nach Auf- und Abstieg, Vorjahr vergessen
                Assert.Single(game.DebCup!.Rounds);
                Assert.Null(game.DebCup.Winner);
                Assert.Empty(game.DebCup.Matches);
                Assert.All(game.DebCup.Participants, c => Assert.NotNull(game.Clubs[c].League));
            }
        }
        Assert.Equal(5, game.History.Count);
        Assert.All(game.History, r => Assert.True(r.Titles.ContainsKey(Title.CupWinner)));
        Assert.True(cupHomeGames.Sum() > 0, "Kein Pokal-Heimspiel der Manager – Probe prüft die Abrechnung nicht");
    }

    /// <summary>Exakte Abrechnung des aktiven Managers: Betrag je Schritt mit dem Preis dieses Schritts.</summary>
    [Fact]
    public void ActiveManager_EveryCupHomeGameSettledOnce_AtPriceOfItsDay()
    {
        var found = 0;
        for (var seed = 1; seed <= 3; seed++)
        {
            var game = Create(seed, true, 31);
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
                var homeGames = HomeGameDays(game, club).Count;
                expected += (long)(homeGames - counted) * stadium.Capacity * price;
                counted = homeGames;
                Assert.Equal(expected, game.Finance(manager).Book.Current(FinanceCategory.Eintritt));
                price = price % 9 + 4;
            }
            found += game.DebCup!.Matches.Count(m => m.Home == club);
        }
        Assert.True(found > 0, "Kein Pokal-Heimspiel – Test prüft nichts");
    }

    private static List<int> HomeGameDays(GameState game, int club) =>
        game.Results.Where(r => r.Home == club).Select(r => game.MatchDayDates[r.MatchDay - 1])
            .Concat(game.PlayoffResults.Where(r => r.Home == club).Select(r => r.Day))
            .Concat(game.RelegationGroups.SelectMany(g => g.Results).Where(r => r.Home == club).Select(r => -r.MatchDay))
            .Concat(game.DebCup?.Matches.Where(m => m.Home == club).Select(m => m.Day) ?? [])
            .ToList();
}
