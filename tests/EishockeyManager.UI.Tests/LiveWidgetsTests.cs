using EishockeyManager.Tests.Support;
using EishockeyManager.UI;

namespace EishockeyManager.UI.Tests;

/// <summary>Bausteine der Live-Anzeige (L3): Balken 0x533D0/0x53307 und Lampe 0x53453 aus Overlay 0x130.</summary>
public class LiveWidgetsTests
{
    private static byte At(Surface s, int x, int y) => s.Pixels[y * Surface.Width + x];

    private static Surface Blank(byte color = 3)
    {
        var s = new Surface();
        s.Fill(new Rect(0, 0, Surface.Width, Surface.Height), color);
        return s;
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(32)]
    [InlineData(78)]
    [InlineData(79)]
    [InlineData(240)]
    public void HorizontalBar_TwoLinesOnBackground(int value)
    {
        var s = Blank();
        LiveWidgets.HorizontalBar(s, 9, 192, 79, value);
        var v = Math.Min(value, 79);
        for (var x = 9; x <= 88; x++)
        {
            var filled = v > 0 && x <= 9 + v;
            Assert.Equal(filled ? 0x18 : 0x11, At(s, x, 192));
            Assert.Equal(filled ? 0x13 : 0x11, At(s, x, 193));
        }
        // nichts außerhalb von (9 | 192)–(88 | 193)
        Assert.Equal(3, At(s, 8, 192));
        Assert.Equal(3, At(s, 89, 192));
        Assert.Equal(3, At(s, 9, 191));
        Assert.Equal(3, At(s, 9, 194));
    }

    [Fact]
    public void HorizontalBar_Full_NoBackgroundRectangle()
    {
        // [ORIGINAL] 0x533E7: Rechteck 0x11 nur, wenn max > Wert
        var s = Blank();
        LiveWidgets.HorizontalBar(s, 0, 0, 10, 10);
        Assert.Equal(0x18, At(s, 10, 0));
        Assert.Equal(0x13, At(s, 10, 1));
        Assert.Equal(3, At(s, 11, 0));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(20)]
    [InlineData(40)]
    [InlineData(50)]
    public void VerticalBar_ValuePixelsFromBottom(int value)
    {
        const int x = 30, y = 50, max = 40;
        var s = Blank();
        LiveWidgets.VerticalBar(s, x, y, max, value);
        var v = Math.Min(value, max);
        for (var k = 0; k < 2; k++)
        {
            var colored = 0;
            for (var yy = y; yy <= y + max; yy++)
            {
                var p = At(s, x + 2 + k, yy);
                if (p == LiveWidgets.BarColors[k])
                    colored++;
                else
                    Assert.Equal(LiveWidgets.BarEmpty, p);
            }
            // [ORIGINAL] 0x53307: unten Wert Pixel in 0x18/0x13 (die Leerlinie überdeckt die zwei obersten der Wertlinie)
            Assert.Equal(v == max ? v + 1 : v, colored);
            Assert.Equal(v > 0 ? LiveWidgets.BarColors[k] : LiveWidgets.BarEmpty, At(s, x + 2 + k, y + max));
        }
        Assert.Equal(3, At(s, x + 1, y + max));
        Assert.Equal(3, At(s, x + 4, y + max));
        Assert.Equal(v == max ? LiveWidgets.BarColors[0] : 3, At(s, x + 2, y - 1));
    }

    [Fact]
    public void Lamp_Off_Rectangle0x10()
    {
        var s = Blank();
        LiveWidgets.Lamp(s, null!, 10, 228, false);
        // [ORIGINAL] 0x53496: Rechteck (x | y + 1)–(x + 4 | y + 5) Farbe 0x10
        for (var yy = 229; yy <= 233; yy++)
            for (var xx = 10; xx <= 14; xx++)
                Assert.Equal(0x10, At(s, xx, yy));
        Assert.Equal(3, At(s, 15, 229));
        Assert.Equal(3, At(s, 10, 228));
        Assert.Equal(3, At(s, 10, 234));
    }

    [GameDataFact]
    public void Lamp_On_CopiesKb38Cell()
    {
        var assets = new Assets(TestGameData.Load());
        var src = assets.Image(LiveWidgets.DigitsGraphic);
        var s = Blank();
        LiveWidgets.Lamp(s, assets, 10, 228, true);
        // [ORIGINAL] 0x53462: kb\38 (76 | 10) 4 × 4 nach (x + 1 | y + 2), Durchsichtigkeit 0xC0
        for (var yy = 0; yy < 4; yy++)
            for (var xx = 0; xx < 4; xx++)
            {
                var p = src.Pixels[(10 + yy) * src.Width + 76 + xx];
                Assert.Equal(p == 0xC0 ? (byte)3 : p, At(s, 11 + xx, 230 + yy));
            }
        Assert.Equal(3, At(s, 10, 230));
        Assert.Equal(3, At(s, 11, 229));
    }
}
