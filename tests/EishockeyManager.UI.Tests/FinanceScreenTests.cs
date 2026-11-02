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

    private static void Press(ScreenDriver d, Rect r) => d.Click(r.X + 2, r.Y + 2);

    /// <summary>
    /// Kauf und Verkauf nach em.exe (Spur geld Teil 5, G4): Klick links in den Streifen wählt „kaufen“ (keine Vorgabe),
    /// Klick auf den Balken setzt Pixel · q (Datei 0x590E0), „+“/„−“ ändern um 1 Stück, Klick in die Buchungszeile bucht.
    /// Rechts ist die Vorgabe der ganze Bestand (Datei 0x5974E).
    /// </summary>
    [GameDataFact]
    public void BuyAndSellStocks_WithBar()
    {
        var (d, screen) = Open();
        var game = d.Host.Game!;
        var me = game.ActiveManager;
        var price = game.Stocks.Stocks[1].Price;

        var row = FinanceScreen.StockSelectArea(1);
        d.Click(row.X + 10, row.Y + 10);
        Assert.Equal(new FinanceScreen.Selection(FinanceScreen.SelectionKind.Stock, 1), screen.Selected);

        d.Click(60, 92); // linke Hälfte des Streifens: Seite „kaufen“
        Assert.False(screen.ActiveSide);
        Assert.Null(screen.Amount);
        var max = screen.MaxAmount;
        Assert.Equal(me.Money / price, max);

        d.Click(158 - 10, 93); // 10 Pixel links der Mitte
        Assert.Equal(FinanceAmountBar.FromPixels(10, max, stock: true), screen.Amount);
        var count = screen.Amount!.Value + 1;
        Press(d, FinanceScreen.LeftPlus);
        Assert.Equal(count, screen.Amount);
        Press(d, FinanceScreen.LeftArrow);
        Assert.Equal(count, game.Finance(me).Holdings[1].Count);
        var cost = count * price + FinanceRules.StockFee(count * price);   // Gebühr 1,5 % (em.exe Datei 0x5915F ff.)
        Assert.Equal(FinanceRules.StartMoney(me.Level) - cost, me.Money);
        Assert.Equal(FinanceScreen.SelectionKind.None, screen.Selected.Kind); // nach der Buchung ist nichts gewählt
        Assert.Null(screen.ActiveSide);

        d.Click(row.X + 10, row.Y + 10);
        d.Click(230, 92);                 // rechts: Vorgabe = alle Aktien
        Assert.Equal(count, screen.Amount);
        Press(d, FinanceScreen.RightPlus); // mehr als vorhanden geht nicht
        Assert.Equal(count, screen.Amount);
        Press(d, FinanceScreen.RightArrow);
        Assert.Equal(0, game.Finance(me).Holdings[1].Count);
        Assert.Equal(FinanceRules.StartMoney(me.Level) - cost + count * price - FinanceRules.StockFee(count * price), me.Money);
    }

    /// <summary>Bank: rechts einzahlen mit „+“ in 1.000-DM-Schritten, links holen mit der Vorgabe Sparbuch (Datei 0x59872).</summary>
    [GameDataFact]
    public void Bank_DepositAndWithdraw()
    {
        var (d, screen) = Open();
        var game = d.Host.Game!;
        var me = game.ActiveManager;
        var bank = game.Finance(me).Bank;

        d.Click(95, 30); // Bankkasten neben dem Bankbild (das Bankbild öffnet das Finanzbuch)
        Assert.Equal(FinanceScreen.SelectionKind.Bank, screen.Selected.Kind);
        d.Click(230, 92);
        Assert.True(screen.ActiveSide);
        Assert.Null(screen.Amount);      // kein Kredit: keine Vorgabe, die Knöpfe wirken erst nach dem Balken
        Press(d, FinanceScreen.RightPlus);
        Assert.Null(screen.Amount);
        d.Click(160, 93);                // Pixel 0: Betrag 0
        Assert.Equal(0, screen.Amount);
        for (var i = 0; i < 30; i++)
            Press(d, FinanceScreen.RightPlus);
        Assert.Equal(30_000, screen.Amount);
        Press(d, FinanceScreen.RightArrow);
        Assert.Equal(30_000, bank.Savings);

        d.Click(95, 30);
        d.Click(60, 92);
        Assert.Equal(30_000, screen.Amount); // Vorgabe: Sparbuch
        for (var i = 0; i < 70; i++)
            Press(d, FinanceScreen.LeftPlus);
        Assert.Equal(100_000, screen.Amount);
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
        Assert.Equal(FinanceScreen.SelectionKind.None, screen.Selected.Kind); // anfangs nichts gewählt (Datei 0x58A68)
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
