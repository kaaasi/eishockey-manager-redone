using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

public class LifecycleRegressionTests
{
    [Fact]
    public void R204_InvalidLoanAbortsBeforeAnySeasonMutation()
    {
        var game = TestGames.Create();
        FinishSeason(game);
        var player = game.Clubs[game.ActiveManager.ClubIndex].Players.First();
        player.OnLoan = true; // Kein gespeicherter Eigentümer: nicht raten oder teilweise fortschreiben.
        var season = game.Season;
        var ages = game.Clubs.SelectMany(c => c.Players).Select(p => p.Age).ToArray();
        var leagues = game.Clubs.Select(c => c.League).ToArray();
        var listings = game.TransferMarket.Listings.ToArray();
        var money = game.ActiveManager.Money;
        Assert.Throws<InvalidOperationException>(() => game.StartNextSeason());
        Assert.Equal(season, game.Season);
        Assert.Equal(SeasonPhase.SeasonOver, game.Phase);
        Assert.Equal(ages, game.Clubs.SelectMany(c => c.Players).Select(p => p.Age));
        Assert.Equal(leagues, game.Clubs.Select(c => c.League));
        Assert.Equal(listings, game.TransferMarket.Listings);
        Assert.Equal(money, game.ActiveManager.Money);
    }

    [Fact]
    public void R204_NewSeasonCancelsListingsAndBids()
    {
        // Vorbedingung: der Markt ist am Saisonende nicht voll, das eigene Angebot also möglich. Ob er voll ist, hängt am
        // Zufallsverbrauch der Spielberechnung (Seeds wurden dafür schon zweimal verschoben, R8-A02); deshalb der erste
        // Seed, bei dem Platz ist (spielstaerke-manager-2: Managerpaarungen ziehen je Minute zusätzlich).
        GameState game = null!;
        for (var seed = 1; seed <= 20; seed++)
        {
            game = TestGames.Create(seed);
            FinishSeason(game);
            if (game.TransferMarket.Listings.Count < TransferMarket.MaxListings)
                break;
        }
        var bid = TransferTransactionRegressionTests.CreateBid(game);
        game.StartNextSeason();
        Assert.DoesNotContain(bid.Listing, game.TransferMarket.Listings);
        Assert.Empty(game.TransferMarket.Bids);
        var money = game.ActiveManager.Money;
        var players = game.Clubs[game.ActiveManager.ClubIndex].Players.ToArray();
        game.TransferMarket.Accept(game, bid);
        Assert.Equal(money, game.ActiveManager.Money);
        Assert.Equal(players, game.Clubs[game.ActiveManager.ClubIndex].Players);
    }

    // Spur kader Teil 3 (R204, R15-D4 = A): geändert – vorher „bleibt als Vertragsloser verpflichtbar“ (Remake-Ergänzung, Liste
    // zum Preis 0). Wie im Original (Datei 0x27DE3–0x27E9D, FILE44 → FILE46 KJELLBERG) steht er nicht auf der Liste; Belege und
    // Mehrsaisonprobe in KaderVertragsloseTests.
    [Fact]
    public void R204_ExpiredPlayerBecomesFreeAgentButIsNotListed()
    {
        var game = TestGames.Create();
        FinishSeason(game);
        var club = game.Clubs[game.ActiveManager.ClubIndex];
        var player = club.Players.First(p => p.Role == PlayerRole.Forward);
        player.ContractYears = 1;
        game.StartNextSeason();
        Assert.Contains(player, game.TransferMarket.FreeAgents);
        Assert.DoesNotContain(player, club.Players);
        Assert.DoesNotContain(game.TransferMarket.Listings, l => l.Player == player);
    }

    [Fact]
    public void R204_ContinueCleansUnavailableAndForeignLineupReferences()
    {
        var game = TestGames.Create();
        var club = game.Clubs[game.ActiveManager.ClubIndex];
        var injured = club.Lineup.Players.First();
        injured.InjuredGames = 2;
        var departed = club.Lineup.Players.Last();
        club.Players.Remove(departed);
        game.Continue();
        Assert.DoesNotContain(injured, club.Lineup.Players);
        Assert.DoesNotContain(departed, club.Lineup.Players);
    }

    internal static void FinishSeason(GameState game)
    {
        foreach (var manager in game.Managers)
            manager.Money = 10_000_000;
        for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
        {
            Assert.True(guard < 500, "Saisonende nicht erreicht");
            game.Continue();
        }
        Assert.Equal(SeasonPhase.SeasonOver, game.Phase);
    }

    [Fact]
    public void R204_NewSeason_AgesShortensAndEndsContractsWithoutOpeningScreens()
    {
        var game = TestGames.Create();
        FinishSeason(game);
        var club = game.Clubs[game.ActiveManager.ClubIndex];
        var staying = club.Players[0];
        var leaving = club.Players.First(p => p.Role == PlayerRole.Forward);
        staying.Age = leaving.Age = 27;
        staying.ContractYears = 3;
        leaving.ContractYears = 1;
        staying.NegotiationLock = 5;
        club.Lineup.Assign(LineupSlot.Forward(0, 0), leaving);

        game.StartNextSeason();

        Assert.Equal(28, staying.Age);
        Assert.Equal(2, staying.ContractYears);
        Assert.Equal(5, staying.NegotiationLock); // R8W3I-09 (transfer-vertraege-komplett): Sperre bleibt, 0x274E8–0x27539
        Assert.Equal(28, leaving.Age);
        Assert.Equal(0, leaving.ContractYears);
        Assert.DoesNotContain(leaving, club.Players);
        Assert.DoesNotContain(leaving, club.Lineup.Players);
    }

    [Fact]
    public void R204_MarketLoanEndsFree_NotBackWithItsRemainingContract()
    {
        var game = TestGames.Create();
        FinishSeason(game);
        var listing = game.TransferMarket.Listings.First();
        var origin = listing.From;
        var player = listing.Player;
        player.Age = 27;
        player.ContractYears = 3;
        player.MonthlySalary = 1234;
        player.Insured = true;
        var borrower = game.Clubs[game.ActiveManager.ClubIndex];
        Assert.Equal(TransferResult.Accepted,
            game.TransferMarket.Buy(game, game.ActiveManager, listing, listing.LoanFee * 135 / 100, true)); // 135 %: im Original immer angenommen (über random(110, 130) %, 0x4991D; R8-C01)
        borrower.Lineup.Assign(new LineupSlot(0, player.Role, 0), player);

        game.StartNextSeason();

        // A115 (kader-start Runde 14 Teil 4, re-runde-14 FILE44 → FILE46 VALLIERE): Leihe vom Markt endet frei (Pool +0x1A = 6,
        // Datei 0x275FF), nicht mit dem Stammvertrag beim Herkunftsverein (bis dahin R204).
        Assert.False(player.OnLoan);
        Assert.Equal(28, player.Age);
        // Review R14KS-03: so streng wie der alte R204-Test (genau ein Ort, eine Nummer) – jetzt bei den Vertragslosen statt im
        // Kader des Herkunftsvereins, ohne Vertrag.
        Assert.Equal((0, 0, false), (player.ContractYears, player.MonthlySalary, player.Insured));
        Assert.DoesNotContain(player, borrower.Players);
        Assert.DoesNotContain(player, borrower.Lineup.Players);
        Assert.DoesNotContain(player, origin.Players);
        Assert.DoesNotContain(game.Clubs, c => c.Players.Contains(player));
        Assert.Single(game.TransferMarket.FreeAgents, q => q == player);
        Assert.Single(game.AllPlayers, p => p == player);
    }
}
