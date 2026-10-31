using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens.ManagerArea;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur boerse-start (#169): Kursverlauf und anfänglicher Bildausschnitt im Bildschirm Finanzen nach em.exe
/// (0x588CA Zeichnen, 0x58BD4 Ausschnitt). Vorlage <c>folge01_00510</c> (nur im privaten Referenz-Repo).
/// </summary>
public class BoerseStartScreenTests
{
    /// <summary>
    /// Kursverläufe aus <c>folge01_00510</c> zurückgerechnet (wie <c>Tools/Scenarios/BoerseStart.cs</c>): Oberkante der
    /// Linie je Tag, ±1 DM; letzter Tag = angezeigte Kurse 98 / 96 / 134 / 126 DM.
    /// </summary>
    private static readonly int[][] Folge01History =
    [
        [97, 96, 95, 96, 95, 96, 97, 98, 98, 98, 98, 97, 97, 97, 97, 96, 96, 96, 96, 96, 96, 97, 97, 99, 99, 98, 99, 99, 99, 97, 96, 96, 96, 96, 97, 98],
        [91, 91, 91, 91, 91, 91, 92, 92, 92, 91, 90, 90, 91, 91, 92, 93, 94, 94, 94, 95, 94, 96, 95, 94, 94, 94, 95, 96, 96, 96, 95, 95, 95, 95, 96, 96],
        [131, 132, 131, 130, 129, 130, 130, 129, 128, 128, 129, 131, 131, 132, 131, 129, 130, 130, 131, 132, 133, 133, 133, 132, 131, 132, 133, 133, 133, 133, 134, 134, 135, 135, 134, 134],
        [111, 111, 109, 105, 105, 109, 113, 114, 115, 116, 116, 115, 115, 115, 112, 113, 115, 115, 116, 115, 119, 123, 124, 124, 124, 123, 123, 125, 126, 127, 128, 128, 127, 127, 125, 126],
    ];

    private static StockMarket Market(IReadOnlyList<int[]> histories) => new()
    {
        Stocks = StockMarket.Defaults.Select((d, i) =>
        {
            var stock = new Stock { Name = d.Name, Base = d.Base, Price = histories[i][^1] };
            stock.History.AddRange(histories[i]);
            return stock;
        }).ToList(),
    };

    private static int[] Flat(int price) => Enumerable.Repeat(price, Stock.HistoryLength).ToArray();

    [Fact]
    public void InitialOffset_Folge01_CenterNear97DM()
    {
        // Endpunkte im Bild: rot (134 DM) Zeile 9, orange (126) 21, blau (96) 66 der Zeichenfläche → Versatz 160.
        var offset = FinanceScreen.InitialChartOffset(Market(Folge01History));
        Assert.Equal(160, offset);
        Assert.Equal([9, 21, 66], new[] { 134, 126, 96 }.Select(p => FinanceScreen.CanvasY(p, offset)));
        // Mittlere Zeile (Bildschirmzeile 172) bei (160 + 130) / 3 ≈ 96,7 DM – nicht (höchster + niedrigster Kurs) / 2 = 115.
        Assert.Equal(65, FinanceScreen.CanvasY(97, offset));
    }

    [Fact]
    public void InitialOffset_TakesFirstBestOffset_PlusTen()
    {
        // Alle Kurse 100 DM (300 Drittel): Mittel 300, geprüft ab 100. Sichtbar (0 < 150 − v/2 < 130) ab v = 50 bis 298;
        // schon v = 100 zeigt alle 144 Punkte (> 140) → Abbruch, gemerkt wird 100 + 10.
        Assert.Equal(110, FinanceScreen.InitialChartOffset(Market([Flat(100), Flat(100), Flat(100), Flat(100)])));
        // Niedrige Kurse: Versatz nicht unter 0 (Start bei max(0, Mittel − 200)).
        Assert.Equal(10, FinanceScreen.InitialChartOffset(Market([Flat(20), Flat(20), Flat(20), Flat(20)])));
    }

    [Fact]
    public void InitialOffset_PrefersMostVisiblePoints()
    {
        // Drei Aktien bei 100 DM, eine bei 300 DM (900 Drittel): Mittel 450, geprüft 250 … 640. Die drei unteren sind
        // sichtbar, solange v/2 < 150, also bis v = 290; die obere ab v/2 > 320, also ab v = 650 – nie im Suchbereich.
        // Erster Versatz mit 108 Punkten ist 250 → gemerkt 260.
        Assert.Equal(260, FinanceScreen.InitialChartOffset(Market([Flat(100), Flat(100), Flat(100), Flat(300)])));
    }

    [GameDataFact]
    public void Screen_DrawsThreePixelLines_OverTheFullWidth()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        int[] prices = [90, 100, 110, 120];
        game.Stocks = Market(prices.Select(Flat).ToArray());
        d.OpenTile("Finanzen");
        var screen = Assert.IsType<FinanceScreen>(d.Host.Current);
        d.Move(319, 239);
        Assert.Equal(FinanceScreen.InitialChartOffset(game.Stocks), screen.ChartOffset);

        byte At(int x, int y) => d.Surface.Pixels[y * 320 + x];
        var canvas = FinanceScreen.ChartCanvas;
        for (var i = 0; i < 4; i++)
        {
            var y = screen.ChartY(prices[i]);
            Assert.InRange(y, canvas.Y, canvas.Bottom - 3);
            // 36 Tage im Abstand von 3 Pixeln: x = 134 … 239
            foreach (var x in new[] { canvas.X, canvas.X + 35 * FinanceScreen.ChartDayWidth })
            {
                Assert.Equal(FinanceScreen.ChartTopColors[i], At(x, y));
                Assert.Equal(FinanceScreen.ChartTopColors[i], At(x, y + 1));
                Assert.Equal(FinanceScreen.ChartBottomColors[i], At(x, y + 2));
            }
            Assert.NotEqual(FinanceScreen.ChartTopColors[i], At(canvas.X - 1, y));
            Assert.NotEqual(FinanceScreen.ChartTopColors[i], At(canvas.X + 36 * FinanceScreen.ChartDayWidth - 2, y));
        }
    }

    [GameDataFact]
    public void Screen_NewGame_AllFourLinesStartAtTheLeftEdge()
    {
        // Vorher begann der Verlauf nach einem Zehntel der Breite (Video 5:45); im Original ist er ab Spielbeginn voll.
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        d.OpenTile("Finanzen");
        var screen = Assert.IsType<FinanceScreen>(d.Host.Current);
        d.Move(319, 239);
        var stocks = d.Host.Game!.Stocks.Stocks;
        Assert.All(stocks, s => Assert.Equal(Stock.HistoryLength, s.History.Count));
        var visible = stocks.Count(s => screen.ChartY(s.History[0]) is var y && FinanceScreen.ChartCanvas.Contains(FinanceScreen.ChartCanvas.X, y));
        var leftColumn = Enumerable.Range(FinanceScreen.ChartCanvas.Y, FinanceScreen.ChartCanvas.H)
            .Select(y => d.Surface.Pixels[y * 320 + FinanceScreen.ChartCanvas.X]).ToHashSet();
        Assert.Equal(visible, Enumerable.Range(0, 4).Count(i => leftColumn.Contains(FinanceScreen.ChartTopColors[i])));
        Assert.True(visible >= 2, "Ausschnitt zeigt zu wenige Verläufe");
    }

    [GameDataFact]
    public void Screen_OpeningAndHovering_ChangesNoGameState()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        var before = StateFingerprint.Of(game);
        d.OpenTile("Finanzen");
        var row = FinanceScreen.StockRow(2);
        for (var i = 0; i < 50; i++)
            d.Move(row.X + 10, row.Y + 10);
        FlowTests.ReturnToMainMenu(d);
        var after = StateFingerprint.Of(game);
        Assert.True(before == after, StateFingerprint.FirstDifference(before, after));
    }

    /// <summary>
    /// Review-Befund HB-01 (#214): Nur ein Linksklick in die Logospalte (x &lt; 47, em.exe 0x59BF2) wählt eine Aktie; ein
    /// Klick ins Infofeld daneben und ein Rechtsklick auf die Logospalte (0x59B2D: nur Ereignis 1) ändern nichts.
    /// </summary>
    [GameDataFact]
    public void OnlyLeftClickOnLogoColumn_SelectsStock()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        game.Stocks = Market(Folge01History);
        d.OpenTile("Finanzen");
        var screen = Assert.IsType<FinanceScreen>(d.Host.Current);
        d.Move(319, 239);
        var offset = screen.ChartOffset;
        var logo = FinanceScreen.StockSelectArea(2);

        d.Click(60, logo.Y + 10); // Infofeld rechts der Logospalte
        Assert.Equal(FinanceScreen.SelectionKind.Bank, screen.Selected.Kind);
        Assert.Equal(offset, screen.ChartOffset);

        d.RightClick(logo.X + 10, logo.Y + 10);
        Assert.Equal(FinanceScreen.SelectionKind.Bank, screen.Selected.Kind);
        Assert.Equal(offset, screen.ChartOffset);
    }

    /// <summary>
    /// Befund NB-02 (#206): Erst der Klick wählt die Aktie und setzt den Ausschnitt (em.exe 0x59B2D: Auswahl nur bei
    /// Maustaste <c>DGROUP 0x5A16 == 1</c>, dann 0x59C75 Versatz = Kurs · 3 − 130). Überfahren ändert nichts.
    /// Ersetzt <c>Hover_SetsOffsetImmediately</c> aus #188.
    /// </summary>
    [GameDataFact]
    public void Hover_KeepsOffset_ClickSetsItImmediately()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        game.Stocks = Market(Folge01History);
        d.OpenTile("Finanzen");
        var screen = Assert.IsType<FinanceScreen>(d.Host.Current);
        d.Move(319, 239);
        Assert.Equal(160, screen.ChartOffset);
        var row = FinanceScreen.StockRow(2);
        for (var i = 0; i < 5; i++)
            d.Move(row.X + 10, row.Y + 10);
        Assert.Equal(160, screen.ChartOffset);
        Assert.Equal(FinanceScreen.SelectionKind.Bank, screen.Selected.Kind);

        d.Click(row.X + 10, row.Y + 10);
        Assert.Equal(new FinanceScreen.Selection(FinanceScreen.SelectionKind.Stock, 2), screen.Selected);
        Assert.Equal(134 * 3 - 130, screen.ChartOffset);
        Assert.Equal(FinanceScreen.ChartCanvas.Y + 65, screen.ChartY(134));

        // Überfahren einer anderen Aktie lässt Auswahl und Ausschnitt stehen
        var other = FinanceScreen.StockRow(0);
        d.Move(other.X + 10, other.Y + 10);
        Assert.Equal(2, screen.Selected.Index);
        Assert.Equal(134 * 3 - 130, screen.ChartOffset);
    }
}
