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
            // Über 130 % der Leihgebühr nimmt der Computermarkt immer an (em.exe 0x4991D, Spur transfer-original).
            game.TransferMarket.Buy(game, game.ActiveManager, listing, listing.LoanFee * 135 / 100, true));
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
        // Integration Runde 9 (Leitstelle): Der Markt kommt jetzt aus dem Spielerpool (transfer-vertraege-komplett Teil 5,
        // 0x494EF). Ein geliehener Poolspieler kehrt in seinen Poolplatz zurück; ob dieser Platz beim Saisonwechsel in den
        // Kader seines Vereins wechselt, hängt am Tausch Pool ↔ Computerkader. Stimmig sind beide Ausgänge (im Verein mit um ein
        // Jahr verkürztem Vertrag oder im Pool ohne Vertrag – nie in beiden, nie beim Manager); der Ablauf ist aber
        // deterministisch, geprüft wird deshalb genau der beobachtete (#353, R9S-05). Bis Teil 4 der Spur kader-transfer: Pool
        // (Verein 9); seit Teil 5 (Poolumverteilung beim Spielstart, andere erste Liste): Nationalspieler, Tausch in den Kader von
        // Verein 59 (0x5ACA0 Art 0). Ändert sich der Zufallsverlauf, hier den neuen Ausgang eintragen und begründen.
        Assert.Equal(59, origin.Index);
        Assert.Contains(player, origin.Players);
        Assert.Equal(2, player.ContractYears);
        Assert.DoesNotContain(player, game.TransferMarket.PoolPlayers);
        Assert.All(game.Clubs.Where(c => c != origin), c => Assert.DoesNotContain(player, c.Players));
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
