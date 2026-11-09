using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.ManagerArea;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur geld, Teil 5 (G4; A312, A315, A316, F158): Betragsbalken em.exe Datei 0x58409 und Bedienung im Aufrufer Datei
/// 0x58A57 (docs/analyse/re-geld.md, „Datei 0x58409 – Betragsbalken“).
/// </summary>
public class FinanceBedienungTests
{
    /// <summary>q = max / 154 + 1 in Drittel-DM; Klick: s · q auf 3.000 Drittel abgerundet, höchstens max (A315).</summary>
    [Fact]
    public void Bar_PixelsTimesQuantum_RoundedToThousand()
    {
        Assert.Equal(6_000_000 / 154 + 1, FinanceAmountBar.Quantum(2_000_000, stock: false));
        Assert.Equal(12_000, FinanceAmountBar.FromPixels(1, 2_000_000, stock: false));   // 38.962 Drittel → 36.000
        Assert.Equal(2_000_000, FinanceAmountBar.FromPixels(154, 2_000_000, stock: false));
        Assert.Equal(0, FinanceAmountBar.FromPixels(0, 2_000_000, stock: false));
        Assert.Equal(1_000, FinanceAmountBar.FromPixels(150, 1_234, stock: false));    // max 1.234 DM: q = 25 Drittel
        Assert.Equal(1_000, FinanceAmountBar.FromPixels(154, 1_234, stock: false));    // 3.850 Drittel → 3.000
        Assert.Equal(2_000_000, FinanceAmountBar.FromPixels(-5, 2_000_000, stock: false)); // andere Hälfte: vorzeichenlos → max
    }

    /// <summary>Aktien: q = max / 154 + 1 Stück, keine Rundung.</summary>
    [Fact]
    public void Bar_Stocks_NoRounding()
    {
        Assert.Equal(1, FinanceAmountBar.Quantum(100, stock: true));
        Assert.Equal(50, FinanceAmountBar.FromPixels(50, 100, stock: true));
        Assert.Equal(100, FinanceAmountBar.FromPixels(120, 100, stock: true));
        Assert.Equal(3, FinanceAmountBar.Quantum(400, stock: true));
        Assert.Equal(30, FinanceAmountBar.FromPixels(10, 400, stock: true));
        Assert.Equal(16, FinanceAmountBar.Pixels(50, 400, stock: true));
    }

    /// <summary>Knöpfe: ±1.000 DM bzw. ±1 Stück; „−“ bei 0 und „+“ am Höchstwert wirken nicht; unter 1.000 DM wird „−“ zum Höchstwert.</summary>
    [Fact]
    public void Buttons_StepAndUnsignedWrap()
    {
        Assert.Equal(6_000, FinanceAmountBar.Press(5_000, +1, 10_000, stock: false));
        Assert.Equal(10_000, FinanceAmountBar.Press(9_500, +1, 10_000, stock: false));
        Assert.Null(FinanceAmountBar.Press(10_000, +1, 10_000, stock: false));
        Assert.Null(FinanceAmountBar.Press(0, -1, 10_000, stock: false));
        Assert.Equal(10_000, FinanceAmountBar.Press(500, -1, 10_000, stock: false));
        Assert.Equal(4, FinanceAmountBar.Press(5, -1, 10, stock: true));
    }

    private static (ScreenDriver Driver, FinanceScreen Screen, GameState Game) TwoManagers()
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
        return (d, Assert.IsType<FinanceScreen>(d.Host.Current), game);
    }

    /// <summary>
    /// E2 / Datei 0x59269 ff. (A034, A312): Links beim Mitmanager holen bucht sofort, ohne Nachricht im Posteingang; danach ist
    /// nichts mehr gewählt.
    /// </summary>
    [GameDataFact]
    public void BorrowFromPartner_BooksAtOnce()
    {
        var (d, screen, game) = TwoManagers();
        var (me, other) = (game.Managers[0], game.Managers[1]);
        var (money, otherMoney, messages) = (me.Money, other.Money, game.Inbox(other).Count);
        var partner = FinanceScreen.PartnerSelectArea(1);

        d.Click(partner.X + 10, partner.Y + 10);
        d.Click(60, 80);
        Assert.False(screen.ActiveSide);
        Assert.Null(screen.Amount);
        d.Click(158 - 20, 93);
        var amount = screen.Amount!.Value;
        Assert.Equal(FinanceAmountBar.FromPixels(20, game.MaxBorrowFromManager(0, 1), stock: false), amount);
        Assert.True(amount > 0);

        d.Click(60, 85); // Buchungszeile, irgendwo außer auf den Knöpfen
        Assert.Equal(money + amount, me.Money);
        Assert.Equal(otherMoney - amount, other.Money);
        Assert.Equal(amount, game.LoanAmount(lender: 1, borrower: 0));
        Assert.Equal(messages, game.Inbox(other).Count);
        Assert.Equal(FinanceScreen.SelectionKind.None, screen.Selected.Kind);
    }

    /// <summary>Datei 0x58F80 ff.: Gehaltener Knopf wiederholt; gezeichnet wird kein gedrückter Knopf (A316), nur der Balken.</summary>
    [GameDataFact]
    public void HeldButton_Repeats_AndDrawsBar()
    {
        var (d, screen, game) = TwoManagers();
        d.Click(95, 30);
        d.Click(230, 92);
        d.Click(160 + 10, 93);
        var start = screen.Amount!.Value;
        var before = d.Surface.Pixels.ToArray();

        d.Drag(Enumerable.Repeat((FinanceScreen.RightPlus.X + 2, FinanceScreen.RightPlus.Y + 2), 30).ToArray());
        var steps = (screen.Amount!.Value - start) / 1_000;
        Assert.InRange(steps, 4, 29);
        d.Move(150, 80);
        Assert.Equal(steps * 1_000 + start, screen.Amount);

        // Balken rechts 160 … 159 + s in Farbe 9, links davon Farbe 0, Knopf unverändert
        var s = FinanceAmountBar.Pixels(screen.Amount!.Value, screen.MaxAmount, stock: false);
        Assert.Equal(FinanceScreen.BarColor, d.Surface.Pixels[96 * 320 + 160]);
        Assert.Equal(FinanceScreen.BarColor, d.Surface.Pixels[96 * 320 + 159 + s]);
        Assert.Equal(0, d.Surface.Pixels[96 * 320 + 160 + s]);
        Assert.Equal(0, d.Surface.Pixels[96 * 320 + 158]);
        var button = FinanceScreen.RightPlus;
        for (var y = button.Y; y < button.Bottom; y++)
            for (var x = button.X; x < button.Right; x++)
                Assert.Equal(before[y * 320 + x], d.Surface.Pixels[y * 320 + x]);
        Assert.Equal(0, game.Finance(game.ActiveManager).Bank.Savings);
    }

    /// <summary>
    /// Datei 0x59ADD / 0x59B37 / 0x59CA3: Klick außerhalb des Streifens löscht Seite und Auswahl; der alte Rahmen wird mit
    /// Farbe 0 übermalt, der neue in Farbe 7 (Bank) bzw. 0x18 (Aktie) gezeichnet.
    /// </summary>
    [GameDataFact]
    public void SelectionFrames_AndDeselect()
    {
        var (d, screen, _) = TwoManagers();
        d.Click(95, 30);
        Assert.Equal(FinanceScreen.SelectionColor, d.Surface.Pixels[6 * 320 + 23]);
        Assert.Equal(FinanceScreen.SelectionColor, d.Surface.Pixels[50 * 320 + 83]);
        d.Click(60, 92);
        Assert.False(screen.ActiveSide);

        var logo = FinanceScreen.StockSelectArea(2);
        d.Click(logo.X + 10, logo.Y + 10);
        Assert.Null(screen.ActiveSide);
        Assert.Equal(FinanceScreen.StockSelectionColor, d.Surface.Pixels[(106 + 68) * 320 + 4]);
        Assert.Equal(0, d.Surface.Pixels[6 * 320 + 23]);

        d.Click(300, 60); // unter den Kästen, über dem Streifen: nichts getroffen
        Assert.Equal(FinanceScreen.SelectionKind.None, screen.Selected.Kind);
        Assert.Equal(0, d.Surface.Pixels[(106 + 68) * 320 + 4]);
    }

    /// <summary>Datei 0x59A63 ff.: Hinweis steht, solange die Seite gewählt ist, zentriert zwischen x und x + 115, Farbe 30.</summary>
    [GameDataFact]
    public void Hint_ShownForActiveSide()
    {
        var (d, screen, _) = TwoManagers();
        d.Click(95, 30);
        d.Move(150, 80);
        var baseline = d.Surface.Pixels.ToArray();
        d.Move(60, 92); // Überfahren zeigt nichts
        Assert.Equal(baseline, d.Surface.Pixels);
        d.Click(230, 92);
        d.Move(150, 80);
        Assert.Contains(FinanceScreen.BarTextColor, Row(d, 77, 167, 282));
        Assert.Equal(baseline[(77 * 320 + 40)..(77 * 320 + 160)], Row(d, 77, 40, 160));
        Assert.Equal(("Geld aufs Sparbuch", "einzahlen (1#)"), (screen.Hints().Right[0], screen.Hints().Right[1]));
    }

    /// <summary>A313: Balken und Seite verschwinden, sobald die Maus den Streifen (y 73–100) verlässt, ohne Klick (Datei 0x59531).</summary>
    [GameDataFact]
    public void AmountBar_DisappearsWhenMouseLeavesStrip()
    {
        var (d, screen, _) = TwoManagers();
        d.Click(95, 30);
        d.Click(230, 92);
        d.Click(160 + 10, 93);
        Assert.NotNull(screen.ActiveSide);
        Assert.Equal(FinanceScreen.BarColor, d.Surface.Pixels[96 * 320 + 160]);
        d.Move(150, 80); // noch im Streifen
        Assert.NotNull(screen.ActiveSide);
        d.Move(150, 60); // darüber
        Assert.Null(screen.ActiveSide);
        Assert.NotEqual(FinanceScreen.BarColor, d.Surface.Pixels[96 * 320 + 160]);
    }

    private static byte[] Row(ScreenDriver d, int y, int x1, int x2) => d.Surface.Pixels[(y * 320 + x1)..(y * 320 + x2)];
}
