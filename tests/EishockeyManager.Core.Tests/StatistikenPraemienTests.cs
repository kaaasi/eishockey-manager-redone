using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur statistiken (Runde 8, #135): Einzelprämien des Saisonendes – bester Torhüter, Topscorer und Fair-Play-Erster
/// der eigenen Liga je 100.000 DM über <see cref="GameState.Book"/> (em.exe 0x27184–0x2725D). Gegen Base 10eb6c5 rot:
/// dort zahlte das Remake keine Einzelprämie.
/// </summary>
public class StatistikenPraemienTests
{
    private static GameState AtClubSeasonOver(int club, int seed)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Prämie", Head = 0, ClubIndex = club, StartLeague = NewGame.DatabaseLeague(club) ?? League.Oberliga });
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, seed);
        for (var guard = 0; game.Phase == SeasonPhase.Running; guard++)
        {
            Assert.True(guard < 1000);
            game.Managers[0].Money = 10_000_000;
            game.Continue();
        }
        return game;
    }

    private static void FinishSeason(GameState game)
    {
        for (var guard = 0; game.Phase == SeasonPhase.ClubSeasonOver; guard++)
        {
            Assert.True(guard < 1000);
            game.Continue();
        }
    }

    /// <summary>Eigener Feldspieler mit überragender Scorerbilanz, eigener Torhüter ohne Gegentor.</summary>
    private static void MakeOwnPlayersBest(GameState game, Club club)
    {
        var skater = club.Players.First(p => p.Role != PlayerRole.Goalkeeper);
        skater.Goals = 200;
        skater.Assists = 200;
        var goalie = club.Players.First(p => p.Role == PlayerRole.Goalkeeper);
        goalie.GoalsAgainst = 0;
        goalie.GoalieMinutes = 5000;
        foreach (var other in game.LeagueClubs(club.League!.Value).Where(c => c != club).SelectMany(c => c.Players))
            if (other.Role == PlayerRole.Goalkeeper && other.GoalsAgainst == 0)
                other.GoalsAgainst = 1;
    }

    /// <summary>Regressionstest #135 (nur Schnittstellen der Base, deshalb auch gegen 10eb6c5 übersetzbar und dort rot).</summary>
    [Fact]
    public void OwnTopScorerAndBestGoalie_EachEarn100000()
    {
        var game = AtClubSeasonOver(5, 3);
        var club = game.Clubs[game.ActiveManager.ClubIndex];
        MakeOwnPlayersBest(game, club);
        var before = game.Finance(game.ActiveManager).Book.Current(FinanceCategory.Praemien);
        FinishSeason(game);

        var result = game.History[^1].Managers.Single();
        Assert.True(result.Premium >= 2 * GameState.SinglePremium, $"Prämie {result.Premium}");
        Assert.Equal(before + result.Premium, game.Finance(game.ActiveManager).Book.Current(FinanceCategory.Praemien));
    }

    [Fact]
    public void Premiums_Exact_WithEventsInOriginalOrder()
    {
        var game = AtClubSeasonOver(5, 3);
        var club = game.Clubs[game.ActiveManager.ClubIndex];
        MakeOwnPlayersBest(game, club);
        foreach (var c in game.LeagueClubs(League.Bundesliga))
            c.LeaguePenaltyMinutes = 500;
        club.LeaguePenaltyMinutes = 0;
        FinishSeason(game);

        var result = game.History[^1].Managers.Single();
        var single = result.Events.Where(e => e is SeasonEvent.BestGoalie or SeasonEvent.TopScorer or SeasonEvent.FairPlay).ToList();
        Assert.Equal([SeasonEvent.BestGoalie, SeasonEvent.TopScorer, SeasonEvent.FairPlay], single);
        var others = result.Events.Except(single).ToList();
        Assert.Equal(GameState.SeasonPremium(result.League, result.Rank, others) + 3 * GameState.SinglePremium, result.Premium);
    }

    [Fact]
    public void OtherClubLeads_NoSinglePremium()
    {
        var game = AtClubSeasonOver(5, 13); // kader-start U15-a/R-K5 (L13): Saat 3 → 4 (auf main mit S-K6-c); #675 (Tageszähler 0x3B7, anderer Zufallsverlauf): 4 → 13, Vorbedingung Verein 5 Erster der Satzfolge der neuen Liga
        var club = game.Clubs[game.ActiveManager.ClubIndex];
        var rival = game.Clubs[6];
        MakeOwnPlayersBest(game, rival);
        foreach (var p in club.Players.Where(p => p.Role == PlayerRole.Goalkeeper))
            p.GoalsAgainst = Math.Max(1, p.GoalsAgainst);
        foreach (var c in game.LeagueClubs(League.Bundesliga))
            c.LeaguePenaltyMinutes = 500;
        // #676: Bei Gleichstand entscheidet die am 29. April gemischte Satzfolge der neuen Liga (S-K6-c, Zufall). Mit der neuen
        // Halbfinal-Paarung verbraucht die Saison andere Zufallszahlen, die Mischung stellt dann Verein 6 vor 5; der Rivale hat
        // deshalb eine Minute mehr, damit Fair-Play eindeutig beim Manager bleibt.
        // Nach dem Merge von main (#672, Saat 13) erneut geprüft: mit Rivale 0 weiter rot (Gleichstand), mit 1 grün.
        rival.LeaguePenaltyMinutes = 1;
        club.LeaguePenaltyMinutes = 0;
        FinishSeason(game);

        var events = game.History[^1].Managers.Single().Events;
        Assert.DoesNotContain(SeasonEvent.BestGoalie, events);
        Assert.DoesNotContain(SeasonEvent.TopScorer, events);
        Assert.Contains(SeasonEvent.FairPlay, events);
    }

    [Fact]
    public void NewSeason_ResetsPenaltyMinutes()
    {
        var game = AtClubSeasonOver(5, 3);
        Assert.True(game.Clubs.Sum(c => c.LeaguePenaltyMinutes) > 0);
        FinishSeason(game);
        game.StartNextSeason();
        Assert.All(game.Clubs, c => Assert.Equal(0, c.LeaguePenaltyMinutes));
    }

    /// <summary>
    /// Mehrsaisonprobe (Geld, AGENTS.md §5): vier Saisonwechsel mit passivem Bundesliga- und aktivem Oberligamanager.
    /// Je Saison stimmen die Einzelprämien mit den Ranglisten am 29. April überein, alle Prämien stehen genau einmal im
    /// Finanzbuch und die Strafminuten beginnen jede Saison bei 0.
    /// </summary>
    [Fact]
    public void MultiSeason_FourChanges_PassiveAndActiveManager()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Passiv", Head = 0, ClubIndex = 2, StartLeague = League.Bundesliga });
        setup.Managers.Add(new ManagerSetup { Name = "Aktiv", Head = 1, ClubIndex = 30, StartLeague = League.Oberliga });
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, 5);
        var paid = 0;
        for (var change = 0; change < 4; change++)
        {
            var active = game.Managers[1];
            foreach (var p in game.Clubs[active.ClubIndex].Players)
                p.ContractYears = Math.Max(p.ContractYears, 2);
            var boosted = false;
            for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
            {
                Assert.True(guard < 2000, "Saisonende nicht erreicht");
                foreach (var m in game.Managers)
                    m.Money = 10_000_000;
                if (game.Phase == SeasonPhase.ClubSeasonOver && !boosted)
                {
                    // Der aktive Manager macht einen Feldspieler zum Topscorer seiner Liga.
                    boosted = true;
                    if (game.Clubs[active.ClubIndex].Players.FirstOrDefault(p => p.Role != PlayerRole.Goalkeeper) is { } star)
                        star.Goals += 300;
                }
                game.Continue();
            }
            var record = game.History[^1];
            foreach (var result in record.Managers)
            {
                var manager = game.Managers[result.Manager];
                // Spur saison Teil 3 (S-K6-c): Fair-Play über die Liga der nächsten Saison (SeasonFlow.FairPlay.cs).
                var single = game.SinglePremiumEvents(manager, result.League).Where(e => e != SeasonEvent.FairPlay).ToList();
                // #620 (R13GN-03): Sieger unabhängig von FairPlayPremiumWinner – wenigste Strafminuten in der Liga der nächsten
                // Saison, bei Gleichstand der erste in der am 29. April gebildeten Satzfolge.
                if (ExpectedFairPlayWinner(game, record, manager.ClubIndex) == manager.ClubIndex)
                    single.Add(SeasonEvent.FairPlay);
                Assert.Equal(single, result.Events.Where(e => e is SeasonEvent.BestGoalie or SeasonEvent.TopScorer or SeasonEvent.FairPlay));
                Assert.Equal(result.Premium, game.Finance(manager).Book.Current(FinanceCategory.Praemien));
                paid += single.Count;
            }
            if (game.Clubs[active.ClubIndex].Players.Count > 0)
                Assert.Contains(SeasonEvent.TopScorer, record.Managers.Single(r => r.Manager == 1).Events);
            game.StartNextSeason();
            Assert.All(game.Clubs, c => Assert.Equal(0, c.LeaguePenaltyMinutes));
            Assert.All(game.Clubs.SelectMany(c => c.Players), p => Assert.Equal(0, p.Goals));
        }
        Assert.True(paid >= 4, "Zu wenige Einzelprämien – Probe prüft wenig");
    }

    private static int? ExpectedFairPlayWinner(GameState game, SeasonRecord record, int club)
    {
        if (record.NextLeague(club, game.Clubs[club].League) is not { } league)
            return null;
        var order = game.NextLeagueSlots(league);
        Assert.Equal(GameState.ClubsPerLeague, order.Count);
        return order.OrderBy(c => game.Clubs[c].LeaguePenaltyMinutes).First(); // stabil: Satzfolge bei Gleichstand
    }
}
