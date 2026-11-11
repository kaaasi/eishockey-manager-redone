using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens.ManagerArea;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// #757: Finanzbildschirm bei Aktienkurs 0. Wählen der Aktie und Seite „kaufen“ rechnet den Höchstwert Konto / Kurs
/// (em.exe Datei 0x59702–0x5972E); das Original bricht dabei mit „Divide error“ ab (Datei 0x0231A → Handler 0x01F74), das
/// Remake zeigt Höchstwert 0 ([APPROX], `GameState.MaxStockPurchase`).
/// </summary>
public class FinanceStockPriceZeroScreenTests
{
    [GameDataFact]
    public void BuySide_AtPriceZero_HasMaxZero_AndBooksNothing()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        var me = game.ActiveManager;
        game.Stocks.Stocks[1].Price = 0;
        var money = me.Money;
        d.OpenTile("Finanzen");
        var screen = Assert.IsType<FinanceScreen>(d.Host.Current);

        var row = FinanceScreen.StockSelectArea(1);
        d.Click(row.X + 10, row.Y + 10);
        Assert.Equal(new FinanceScreen.Selection(FinanceScreen.SelectionKind.Stock, 1), screen.Selected);
        d.Click(60, 92); // linke Hälfte des Streifens: Seite „kaufen“
        Assert.False(screen.ActiveSide);
        Assert.Equal(0, screen.MaxAmount);

        d.Click(FinanceScreen.LeftPlus.X + 2, FinanceScreen.LeftPlus.Y + 2);
        d.Click(FinanceScreen.LeftArrow.X + 2, FinanceScreen.LeftArrow.Y + 2);
        Assert.Equal(0, game.Finance(me).Holdings[1].Count);
        Assert.Equal(money, me.Money);
    }
}
