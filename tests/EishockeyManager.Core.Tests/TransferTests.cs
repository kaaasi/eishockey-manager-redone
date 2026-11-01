using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

public class TransferTests
{
    private static Player NewPlayer(PlayerRole role, int c, int t, int f, int age) =>
        new() { Name = "TEST", Role = role, Condition = c, Technique = t, Form = f, Age = age };

    [Theory]
    [InlineData(PlayerRole.Forward, 76, 79, 47, 19, 338_000)] // LAPORTE
    [InlineData(PlayerRole.Defender, 16, 20, 53, 18, 150_000)] // HANFT
    [InlineData(PlayerRole.Goalkeeper, 25, 37, 48, 26, 231_000)] // LANGE
    public void Price_IsCloseToVideo(PlayerRole role, int c, int t, int f, int age, long video)
    {
        var price = TransferMarket.Price(NewPlayer(role, c, t, f, age));
        Assert.InRange(price, video - 8_000, video + 8_000);
        Assert.Equal(0, price % 1000);
    }

    [Fact]
    public void LoanFee_IsAThirdOfThePrice()
    {
        var game = TestGames.Create();
        var l = game.TransferMarket.Listings[0];
        l.Price = 338_000;
        Assert.Equal(112_666, l.LoanFee); // Video: 338.000 DM (K) / 112.666 DM (L)
    }

    [Fact]
    public void NewGame_HasSixListingsFromComputerClubs()
    {
        var game = TestGames.Create();
        var market = game.TransferMarket;
        Assert.Equal(TransferMarket.StartListings, market.Listings.Count);
        Assert.All(market.Listings, l =>
        {
            Assert.Contains(l.Player, l.From.Players);
            Assert.NotEqual(game.ActiveManager.ClubIndex, l.From.Index);
            Assert.StartsWith("vom ", l.OriginText);
        });
    }

    [Fact]
    public void Buy_MovesPlayerAndPays()
    {
        var game = TestGames.Create();
        var manager = game.ActiveManager;
        var club = game.Clubs[manager.ClubIndex];
        var listing = game.TransferMarket.Listings[0];
        var from = listing.From;
        var money = manager.Money = 1_000_000;
        var offer = listing.Price * 135 / 100; // 135 %: im Original immer angenommen (über random(110, 130) %, 0x4991D; R8-C01)

        Assert.Equal(TransferResult.Accepted, game.TransferMarket.Buy(game, manager, listing, offer, loan: false));
        Assert.Contains(listing.Player, club.Players);
        Assert.DoesNotContain(listing.Player, from.Players);
        Assert.DoesNotContain(listing, game.TransferMarket.Listings);
        Assert.Equal(money - offer, manager.Money);
        Assert.False(listing.Player.OnLoan);
        Assert.Equal(Contracts.PurchaseYears, listing.Player.ContractYears); // 1 Jahr (0x42CB1, transfer-vertraege-komplett Teil 2)
        Assert.DoesNotContain(club.Players, p => p != listing.Player && p.Number == listing.Player.Number);
    }

    [Fact]
    public void Loan_MarksPlayer()
    {
        var game = TestGames.Create();
        var manager = game.ActiveManager;
        var listing = game.TransferMarket.Listings[1];
        manager.Money = 1_000_000;
        var offer = listing.LoanFee * 135 / 100; // 135 %: im Original immer angenommen (über random(110, 130) %, 0x4991D; R8-C01)
        Assert.Equal(TransferResult.Accepted, game.TransferMarket.Buy(game, manager, listing, offer, loan: true));
        Assert.True(listing.Player.OnLoan);
        Assert.Equal(1_000_000 - offer, manager.Money);
    }

    [Fact]
    public void Buy_Refusals()
    {
        var game = TestGames.Create();
        var manager = game.ActiveManager;
        var market = game.TransferMarket;
        var listing = market.Listings[0];

        var offer = listing.Price * 135 / 100; // 135 %: im Original immer angenommen (über random(110, 130) %, 0x4991D; R8-C01)
        manager.Money = offer - 1;
        Assert.Equal(TransferResult.NotEnoughMoney, market.Buy(game, manager, listing, offer, false));
        manager.Money = 10_000_000;
        Assert.Equal(TransferResult.Rejected, market.Buy(game, manager, listing, listing.Price / 2, false));
        Assert.Equal(TransferResult.Unfair, market.Buy(game, manager, listing, 0, false));

        var club = game.Clubs[manager.ClubIndex];
        while (club.Players.Count < TransferMarket.MaxSquad)
            club.Players.Add(new Player { Name = "X", Role = PlayerRole.Forward });
        Assert.Equal(TransferResult.SquadFull, market.Buy(game, manager, listing, listing.Price, false));
    }

    [Fact]
    public void Offer_LimitsAndWithdraw()
    {
        var game = TestGames.Create();
        var manager = game.ActiveManager;
        var market = game.TransferMarket;
        var club = game.Clubs[manager.ClubIndex];
        var forwards = club.Players.Where(p => p.Role == PlayerRole.Forward).ToList();

        for (var i = 0; i < TransferMarket.MaxOwnListings; i++)
            Assert.Equal(TransferResult.Accepted, market.Offer(game, manager, forwards[i]));
        Assert.Equal(TransferResult.TooManyOwnListings, market.Offer(game, manager, forwards[3]));
        var own = market.ListingOf(forwards[0])!;
        Assert.Equal(manager, own.Seller);
        market.Withdraw(own);
        Assert.Null(market.ListingOf(forwards[0]));

        var goalie = club.Players.First(p => p.Role == PlayerRole.Goalkeeper);
        Assert.Equal(TransferResult.Impossible, market.Offer(game, manager, goalie)); // nur 2 Torhüter
    }

    [Fact]
    public void Bids_ComeInAndCanBeAccepted()
    {
        var game = TestGames.Create();
        var manager = game.ActiveManager;
        var market = game.TransferMarket;
        var club = game.Clubs[manager.ClubIndex];
        var player = club.Players.First(p => p.Role == PlayerRole.Forward);
        market.Offer(game, manager, player);

        for (var day = 0; day < 40 && market.Bids.Count == 0; day++)
            market.AdvanceDay(game, game.Random);
        var bid = Assert.Single(market.Bids);
        var money = manager.Money;
        market.Accept(game, bid);
        Assert.DoesNotContain(player, club.Players);
        Assert.Contains(player, bid.Buyer.Players);
        Assert.Equal(money + bid.Amount, manager.Money);
        Assert.Empty(market.Bids);
    }

    [Fact]
    public void CatchUp_AdvancesOncePerPlayedMatchDay()
    {
        var game = TestGames.Create();
        TestSeason.PlayMatchDay(game);
        TestSeason.PlayMatchDay(game);
        game.TransferMarket.CatchUp(game);
        Assert.Equal(2, game.TransferMarket.UpdatedDay);
        Assert.InRange(game.TransferMarket.Listings.Count, 2, TransferMarket.MaxListings);
    }
}
