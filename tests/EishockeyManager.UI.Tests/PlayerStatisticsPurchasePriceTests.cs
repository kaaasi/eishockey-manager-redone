using EishockeyManager.Tests.Support;
using EishockeyManager.Core;
using EishockeyManager.UI.Screens.Statistics;
using EishockeyManager.UI.Screens.Team;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Integration Runde 11 (Anschluss aus kader-transfer Teil 2, A360, R11P-03): Das Spielerdaten-Fenster aus der
/// Spielerstatistik zeigt im Feld „Kaufpreis“ den Kaufpreis des Kaderplatzes (+0x2C, <see cref="Player.PurchasePrice"/>),
/// nicht den Spielerwert.
/// </summary>
public class PlayerStatisticsPurchasePriceTests
{
    [GameDataFact]
    public void OpenPlayerData_ShowsPurchasePrice()
    {
        var game = CreateGame();
        var manager = game.Managers[0];
        var player = game.Clubs[manager.ClubIndex].Players[0];
        player.PurchasePrice = 123_456;
        var info = new PlayerInfoPopup();

        Assert.True(PlayerStatisticsScreen.OpenPlayerData(info, game, player));
        Assert.Same(player, info.Player);
        Assert.Equal(123_456, info.Price);
    }

    private static GameState CreateGame()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        return d.Host.Game!;
    }
}
