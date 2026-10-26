using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

public class TransferTransactionRegressionTests
{
    public static TransferBid CreateBid(GameState game)
    {
        var market = game.TransferMarket;
        var club = game.Clubs[game.ActiveManager.ClubIndex];
        Assert.Equal(TransferResult.Accepted,
            market.Offer(game, game.ActiveManager, club.Players.First(p => p.Role == PlayerRole.Forward)));
        for (var i = 0; market.Bids.Count == 0 && i < 100; i++)
            market.AdvanceDay(game, game.Random);
        return Assert.Single(market.Bids);
    }

    [Theory]
    [InlineData("repeat")]
    [InlineData("withdraw")]
    [InlineData("decline")]
    [InlineData("forged")]
    [InlineData("ownership")]
    public void R209_InvalidBidCannotBookOrMove(string action)
    {
        var game = TestGames.Create();
        var market = game.TransferMarket;
        var bid = CreateBid(game);
        switch (action)
        {
            case "repeat": market.Accept(game, bid); break;
            case "withdraw": market.Withdraw(bid.Listing); break;
            case "decline": market.Decline(bid); break;
            case "forged": bid = new TransferBid(bid.Listing, bid.Buyer, bid.Amount); break;
            case "ownership": bid.Listing.From.Players.Remove(bid.Listing.Player); break;
        }
        var money = game.ActiveManager.Money;
        var book = game.Finance(game.ActiveManager).Book.Current(FinanceCategory.Transfers);
        var rosters = game.Clubs.Select(c => c.Players.ToArray()).ToArray();
        market.Accept(game, bid);
        Assert.Equal(money, game.ActiveManager.Money);
        Assert.Equal(book, game.Finance(game.ActiveManager).Book.Current(FinanceCategory.Transfers));
        for (var i = 0; i < rosters.Length; i++)
            Assert.Equal(rosters[i], game.Clubs[i].Players);
    }
}
