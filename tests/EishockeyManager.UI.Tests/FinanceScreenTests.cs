using EishockeyManager.Core;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.ManagerArea;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>Bildschirm Finanzen mit den Originaldateien (ohne „gamedata“ übersprungen).</summary>
public class FinanceScreenTests
{
    private static (ScreenDriver Driver, FinanceScreen Screen)? Open()
    {
        var d = FlowTests.CreateDriver();
        if (d is null)
            return null;
        d.StartNewGame();
        d.OpenTile("Finanzen");
        return (d, Assert.IsType<FinanceScreen>(d.Host.Current));
    }

    private static void Press(ScreenDriver d, Rect r, bool right = false)
    {
        if (right)
            d.RightClick(r.X + 4, r.Y + 4);
        else
            d.Click(r.X + 4, r.Y + 4);
    }

    [Fact]
    public void BuyAndSellStocks_WithButtons()
    {
        if (Open() is not var (d, screen))
            return;
        var game = d.Host.Game!;
        var me = game.ActiveManager;
        var price = game.Stocks.Stocks[1].Price;

        var row = FinanceScreen.StockRow(1);
        d.Move(row.X + 10, row.Y + 10);
        Assert.Equal(new FinanceScreen.Selection(FinanceScreen.SelectionKind.Stock, 1), screen.Selected);

        Press(d, FinanceScreen.LeftPlus, right: true);   // +100
        Press(d, FinanceScreen.LeftPlus);                // +10
        Press(d, FinanceScreen.LeftMinus);               // −10
        Assert.Equal(100, screen.LeftAmount);
        Press(d, FinanceScreen.LeftArrow);
        Assert.Equal(100, game.Finance(me).Holdings[1].Count);
        Assert.Equal(GameState.StartMoney - 100L * price, me.Money);
        Assert.Equal(0, screen.LeftAmount);

        Press(d, FinanceScreen.RightPlus, right: true);  // 100 verkaufen
        Press(d, FinanceScreen.RightPlus);               // mehr als vorhanden geht nicht
        Assert.Equal(100, screen.RightAmount);
        Press(d, FinanceScreen.RightArrow);
        Assert.Equal(0, game.Finance(me).Holdings[1].Count);
        Assert.Equal(GameState.StartMoney, me.Money);
    }

    [Fact]
    public void Bank_DepositAndWithdraw()
    {
        if (Open() is not var (d, screen))
            return;
        var game = d.Host.Game!;
        var me = game.ActiveManager;
        var bank = game.Finance(me).Bank;

        d.Move(50, 30);
        Assert.Equal(FinanceScreen.SelectionKind.Bank, screen.Selected.Kind);
        for (var i = 0; i < 3; i++)
            Press(d, FinanceScreen.RightPlus);
        Press(d, FinanceScreen.RightArrow);
        Assert.Equal(30_000, bank.Savings);

        Press(d, FinanceScreen.LeftPlus, right: true);   // 100.000 holen: 30.000 Sparbuch + 70.000 Kredit
        Press(d, FinanceScreen.LeftArrow);
        Assert.Equal(0, bank.Savings);
        Assert.Equal(70_000, bank.Credit);
        Assert.Equal(GameState.StartMoney + 70_000, me.Money);
    }

    [Fact]
    public void Chart_ScrollsToHoveredStock()
    {
        if (Open() is not var (d, screen))
            return;
        var stock = d.Host.Game!.Stocks.Stocks[3];
        var row = FinanceScreen.StockRow(3);
        for (var i = 0; i < 200; i++)
            d.Move(row.X + 10, row.Y + 10);
        Assert.Equal(stock.Price, screen.ViewPrice);
        Assert.True(FinanceScreen.Chart.Contains(FinanceScreen.Chart.X, screen.ChartY(stock.Price)));
    }

    [Fact]
    public void FinanceBook_OpensFromBankPictureAndCloses()
    {
        if (Open() is not var (d, screen))
            return;
        d.Click(FinanceScreen.BankPicture.X + 10, FinanceScreen.BankPicture.Y + 10);
        Assert.IsType<FinanceBookScreen>(d.Host.Current);
        d.Click(10, 10);
        Assert.Same(screen, d.Host.Current);
        d.Press(Key.Escape);
        Assert.IsType<MainMenuScreen>(d.Host.Current);
    }
}
