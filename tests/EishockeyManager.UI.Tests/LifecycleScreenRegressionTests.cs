using EishockeyManager.Tests.Support;
using EishockeyManager.Core;
using EishockeyManager.UI.Screens.Team;

namespace EishockeyManager.UI.Tests;

public class LifecycleScreenRegressionTests
{
    [GameDataFact]
    public void R204_OriginalDataLoanAndContractEndAreConsistentAfterContinueThroughSeason()
    {
        var driver = FlowTests.CreateDriver();
        driver.StartNewGame();
        var game = driver.Host.Game!;
        game.ActiveManager.Money = 10_000_000;
        var listing = game.TransferMarket.Listings.First();
        var player = listing.Player;
        var origin = listing.From;
        player.Age = 27;
        player.ContractYears = 3;
        Assert.Equal(TransferResult.Accepted,
            game.TransferMarket.Buy(game, game.ActiveManager, listing, listing.LoanFee, true));
        var club = game.Clubs[game.ActiveManager.ClubIndex];
        var expired = club.Players.First(p => !p.OnLoan);
        expired.ContractYears = 1;
        for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
        {
            Assert.True(guard < 500);
            game.Continue();
        }
        game.StartNextSeason();
        Assert.Equal(28, player.Age);
        Assert.False(player.OnLoan);
        Assert.Equal(2, player.ContractYears);
        Assert.Contains(player, origin.Players);
        Assert.DoesNotContain(player, club.Players);
        Assert.DoesNotContain(expired, club.Players);
        Assert.All(game.Clubs, c => Assert.All(c.Lineup.Players, p => Assert.Contains(p, c.Players)));
    }

    [GameDataFact]
    public void R204_OpeningLineupDoesNotCleanOrChangeTheLineup()
    {
        var driver = FlowTests.CreateDriver();
        driver.StartNewGame();
        var club = driver.Host.Game!.Clubs[driver.Host.Game.ActiveManager.ClubIndex];
        var player = club.Lineup.Players.First();
        player.InjuredGames = 1;
        var before = club.Lineup.Players.ToArray();
        driver.Host.Show(new LineupScreen(driver.Host, driver.Host.Current!));
        Assert.Equal(before, club.Lineup.Players);
    }
}
