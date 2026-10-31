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
        // Seed 5: der Markt ist am Saisonende nicht voll, das eigene Angebot also möglich. Mit Seed 3 steht der Markt seit
        // der Spielberechnung des Originals (Spur spielberechnung, anderer Zufallsverbrauch) am Saisonende auf 11 Angeboten,
        // mit Seed 2 seit dem Lader der Teamwerte (Spur spielstaerke-manager, Zufallszüge beim ersten Spiel) ebenso.
        var game = TestGames.Create(5);
        FinishSeason(game);
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

    [Fact]
    public void R204_ExpiredPlayerRemainsAvailableAsFreeAgentAndCanJoinWithNewContract()
    {
        var game = TestGames.Create();
        FinishSeason(game);
        var club = game.Clubs[game.ActiveManager.ClubIndex];
        var player = club.Players.First(p => p.Role == PlayerRole.Forward);
        player.ContractYears = 1;
        game.StartNextSeason();
        Assert.Contains(player, game.TransferMarket.FreeAgents);
        var listing = Assert.Single(game.TransferMarket.Listings, l => l.Player == player);
        Assert.Equal(0, listing.Price);
        Assert.Equal(TransferResult.Impossible, game.TransferMarket.Buy(game, game.ActiveManager, listing, 1, true));
        var lineup = club.Lineup.Players.ToArray();
        Assert.Equal(TransferResult.Accepted, game.TransferMarket.Buy(game, game.ActiveManager, listing, 0, false));
        Assert.Equal(lineup, club.Lineup.Players);
        Assert.Contains(player, club.Players);
        Assert.DoesNotContain(player, game.TransferMarket.FreeAgents);
        Assert.Equal(2, player.ContractYears);
        Assert.True(player.MonthlySalary > 0);
        Assert.Equal(TransferResult.Impossible, game.TransferMarket.Buy(game, game.ActiveManager, listing, 0, false));
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
        staying.RejectedOfferDay = 0;
        club.Lineup.Assign(LineupSlot.Forward(0, 0), leaving);

        game.StartNextSeason();

        Assert.Equal(28, staying.Age);
        Assert.Equal(2, staying.ContractYears);
        Assert.Null(staying.RejectedOfferDay);
        Assert.Equal(28, leaving.Age);
        Assert.Equal(0, leaving.ContractYears);
        Assert.DoesNotContain(leaving, club.Players);
        Assert.DoesNotContain(leaving, club.Lineup.Players);
    }

    [Fact]
    public void R204_LoanReturnsToOriginalClubAndKeepsItsRemainingContract()
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
            game.TransferMarket.Buy(game, game.ActiveManager, listing, listing.LoanFee, true));
        borrower.Lineup.Assign(new LineupSlot(0, player.Role, 0), player);

        game.StartNextSeason();

        Assert.False(player.OnLoan);
        Assert.Equal(28, player.Age);
        Assert.Equal(2, player.ContractYears);
        Assert.Equal(1234, player.MonthlySalary);
        Assert.True(player.Insured);
        Assert.Contains(player, origin.Players);
        Assert.DoesNotContain(player, borrower.Players);
        Assert.DoesNotContain(player, borrower.Lineup.Players);
        Assert.Single(game.Clubs.SelectMany(c => c.Players), p => p == player);
        Assert.Single(origin.Players, p => p.Number == player.Number);
    }
}
