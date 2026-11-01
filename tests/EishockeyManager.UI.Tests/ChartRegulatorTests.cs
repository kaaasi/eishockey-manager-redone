using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens.ManagerArea;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur finanzen-rest, Teil 3 (#196): Regler für Ausschnitt und Maßstab des Kursverlaufs nach em.exe 0x58D1E–0x58EC5
/// (Ziehen mit der linken Taste im Verlaufsfeld, Mauszeile 107–235; linke Hälfte x &lt; 180 = Ausschnitt, rechte = Maßstab).
/// </summary>
public class ChartRegulatorTests
{
    /// <summary>Höchster Verlaufswert 150 DM = 450 Drittel-DM (0x58BED–0x58C1A: Maximum über alle 4 × 36 Werte).</summary>
    private static (ScreenDriver D, FinanceScreen S) Open()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        int[] top = [100, 110, 120, 150];
        game.Stocks = new StockMarket
        {
            Stocks = StockMarket.Defaults.Select((def, i) =>
            {
                var stock = new Stock { Name = def.Name, Base = def.Base, Price = top[i] };
                stock.History.AddRange(Enumerable.Repeat(top[i], Stock.HistoryLength));
                return stock;
            }).ToList(),
        };
        d.OpenTile("Finanzen");
        var screen = Assert.IsType<FinanceScreen>(d.Host.Current);
        d.Move(319, 239);
        return (d, screen);
    }

    [GameDataFact]
    public void DragLeftHalf_ChangesOffset_ScaleStays()
    {
        var (d, s) = Open();
        var start = s.ChartOffset;
        d.Drag((150, 107), (150, 170));
        // 0x58DF6–0x58E35: Versatz = Maximum − (y − 106) · Maximum / 131 = 450 − 64 · 450 / 131
        Assert.Equal(450 - 64 * 450 / 131, s.ChartOffset);
        Assert.NotEqual(start, s.ChartOffset);
        Assert.Equal(2, s.Scale);
    }

    [GameDataFact]
    public void DragRightHalf_ChangesScale_OffsetStays()
    {
        var (d, s) = Open();
        var start = s.ChartOffset;
        d.Drag((200, 107), (200, 170));
        // 0x58E7E–0x58EC5: Maßstab = (32 − (y − 106) · 32 / 131) · 2, mindestens 2
        Assert.Equal((32 - 64 * 32 / 131) * 2, s.Scale);
        Assert.Equal(start, s.ChartOffset);
        d.Drag((200, 235));
        Assert.Equal(2, s.Scale);
    }

    [GameDataFact]
    public void Drag_ClampsMouseRow_To104()
    {
        var (d, s) = Open();
        d.Drag((200, 107), (200, 0));
        Assert.Equal(64, s.Scale); // y = 104: (104 − 106) · 32 / 131 = 0 (abgeschnitten) → 32 · 2
        d.Drag((150, 107), (150, 0));
        Assert.Equal(450 + 6, s.ChartOffset); // −2 · 450 / 131 = −6 (abgeschnitten)
    }

    [GameDataFact]
    public void Press_OutsideTheChartField_DoesNothing()
    {
        var (d, s) = Open();
        var (offset, scale) = (s.ChartOffset, s.Scale);
        foreach (var (x, y) in new[] { (130, 150), (243, 150), (150, 106), (150, 236), (200, 106) })
            d.Drag((x, y), (x, 200));
        Assert.Equal((offset, scale), (s.ChartOffset, s.Scale));
    }

    [GameDataFact]
    public void Scale_MovesTheLines()
    {
        var (d, s) = Open();
        d.Drag((200, 107), (200, 170));
        Assert.Equal(34, s.Scale);
        Assert.Equal(130 - 450 / 34 + s.ChartOffset / 34, FinanceScreen.CanvasY(150, s.ChartOffset, s.Scale));
        Assert.Equal(FinanceScreen.ChartCanvas.Y + FinanceScreen.CanvasY(150, s.ChartOffset, s.Scale), s.ChartY(150));
    }

    [GameDataFact]
    public void Marker_IsDrawnBesideTheChart_AtTheMouseRow()
    {
        var (d, s) = Open();
        byte At(int x, int y) => d.Surface.Pixels[y * 320 + x];
        d.Drag((150, 107), (150, 170));
        d.Move(319, 239);
        // 0x58DB0–0x58DF3: Streifen x 132–133 (Farbe 0), Marke 2 × 2 bei der Mauszeile in Farbe 0x18
        Assert.Equal(24, At(132, 170));
        Assert.Equal(24, At(133, 171));
        Assert.NotEqual(24, At(132, 172));
        d.Drag((200, 107), (200, 120));
        d.Move(319, 239);
        Assert.Equal(24, At(240, 120));
        Assert.Equal(24, At(241, 121));
    }

    [GameDataFact]
    public void Reopening_ResetsScale()
    {
        var (d, s) = Open();
        d.Drag((200, 107), (200, 170));
        d.Press(Key.Escape);
        d.OpenTile("Finanzen");
        s = Assert.IsType<FinanceScreen>(d.Host.Current);
        Assert.Equal(2, s.Scale);
    }
}
