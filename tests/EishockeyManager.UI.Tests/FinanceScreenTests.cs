using EishockeyManager.Tests.Support;
using EishockeyManager.Core;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.ManagerArea;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>Bildschirm Finanzen mit den Originaldateien (ohne „gamedata“ übersprungen).</summary>
public class FinanceScreenTests
{
    private static (ScreenDriver Driver, FinanceScreen Screen) Open()
    {
        var d = FlowTests.CreateDriver();
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

    [GameDataFact]
    public void BuyAndSellStocks_WithButtons()
    {
        var (d, screen) = Open();
        var game = d.Host.Game!;
        var me = game.ActiveManager;
        var price = game.Stocks.Stocks[1].Price;

        var row = FinanceScreen.StockRow(1);
        d.Click(row.X + 10, row.Y + 10);
        Assert.Equal(new FinanceScreen.Selection(FinanceScreen.SelectionKind.Stock, 1), screen.Selected);

        Press(d, FinanceScreen.LeftPlus, right: true);   // +100
        Press(d, FinanceScreen.LeftPlus);                // +10
        Press(d, FinanceScreen.LeftMinus);               // −10
        Assert.Equal(100, screen.LeftAmount);
        Press(d, FinanceScreen.LeftArrow);
        Assert.Equal(100, game.Finance(me).Holdings[1].Count);
        var cost = 100L * price + FinanceRules.StockFee(100L * price);   // Gebühr 1,5 % (em.exe Datei 0x5915F ff.)
        Assert.Equal(FinanceRules.StartMoney(me.Level) - cost, me.Money);
        Assert.Equal(0, screen.LeftAmount);

        Press(d, FinanceScreen.RightPlus, right: true);  // 100 verkaufen
        Press(d, FinanceScreen.RightPlus);               // mehr als vorhanden geht nicht
        Assert.Equal(100, screen.RightAmount);
        Press(d, FinanceScreen.RightArrow);
        Assert.Equal(0, game.Finance(me).Holdings[1].Count);
        Assert.Equal(FinanceRules.StartMoney(me.Level) - cost + 100L * price - FinanceRules.StockFee(100L * price), me.Money);
    }

    [GameDataFact]
    public void Bank_DepositAndWithdraw()
    {
        var (d, screen) = Open();
        var game = d.Host.Game!;
        var me = game.ActiveManager;
        var bank = game.Finance(me).Bank;

        d.Click(95, 30); // Bankkasten neben dem Bankbild (das Bankbild öffnet das Finanzbuch)
        Assert.Equal(FinanceScreen.SelectionKind.Bank, screen.Selected.Kind);
        for (var i = 0; i < 3; i++)
            Press(d, FinanceScreen.RightPlus);
        Press(d, FinanceScreen.RightArrow);
        Assert.Equal(30_000, bank.Savings);

        Press(d, FinanceScreen.LeftPlus, right: true);   // 100.000 holen: 30.000 Sparbuch + 70.000 Kredit
        Press(d, FinanceScreen.LeftArrow);
        Assert.Equal(0, bank.Savings);
        Assert.Equal(70_000, bank.Credit);
        Assert.Equal(FinanceRules.StartMoney(me.Level) + 70_000, me.Money);
    }

    [GameDataFact]
    public void Chart_ScrollsToClickedStock()
    {
        var (d, screen) = Open();
        var stock = d.Host.Game!.Stocks.Stocks[3];
        var row = FinanceScreen.StockRow(3);
        d.Click(row.X + 10, row.Y + 10);
        Assert.Equal(stock.Price, screen.ViewPrice);
        Assert.True(FinanceScreen.Chart.Contains(FinanceScreen.Chart.X, screen.ChartY(stock.Price)));
    }

    /// <summary>
    /// Befund NB-02 (#206): Auch Bank und Mitmanager werden erst per Klick gewählt (em.exe 0x59B2D–0x59BF2, nur bei
    /// gedrückter linker Maustaste), Überfahren ändert die Auswahl nicht.
    /// </summary>
    [GameDataFact]
    public void BankAndPartner_SelectedOnlyByClick()
    {
        var d = FlowTests.CreateDriver();
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "M0", Head = 0, ClubIndex = 2 });
        setup.Managers.Add(new ManagerSetup { Name = "M1", Head = 1, ClubIndex = 14 });
        var game = NewGame.Create(d.Host.Assets.Database, setup, 5);
        d.Host.Game = game;
        d.Host.Show(new MainMenuScreen(d.Host));
        d.Frame();
        d.OpenTile("Finanzen");
        var screen = Assert.IsType<FinanceScreen>(d.Host.Current);
        var partner = FinanceScreen.PartnerBox(1);

        d.Move(partner.X + 10, partner.Y + 10);
        Assert.Equal(FinanceScreen.SelectionKind.Bank, screen.Selected.Kind);
        d.Click(partner.X + 10, partner.Y + 10);
        Assert.Equal(new FinanceScreen.Selection(FinanceScreen.SelectionKind.Manager, 1), screen.Selected);

        d.Move(95, 30);
        Assert.Equal(FinanceScreen.SelectionKind.Manager, screen.Selected.Kind);
        d.Click(95, 30);
        Assert.Equal(FinanceScreen.SelectionKind.Bank, screen.Selected.Kind);
    }

    [GameDataFact]
    public void FinanceBook_OpensFromBankPictureAndCloses()
    {
        var (d, screen) = Open();
        d.Click(FinanceScreen.BankPicture.X + 10, FinanceScreen.BankPicture.Y + 10);
        Assert.IsType<FinanceBookScreen>(d.Host.Current);
        d.Click(10, 10);
        Assert.Same(screen, d.Host.Current);
        d.Press(Key.Escape);
        Assert.IsType<MainMenuScreen>(d.Host.Current);
    }
}
