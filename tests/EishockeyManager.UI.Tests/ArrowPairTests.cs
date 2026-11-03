using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI;

namespace EishockeyManager.UI.Tests;

/// <summary>M-K4: Pfeilpaar em.exe Datei 0x4BA58 (Stub 0x120:0x3E).</summary>
public class ArrowPairTests
{
    // Werbung, Datei 0x2BA72: (138, 183, 138, 192, Halten 0)
    private static ArrowPair Advertising() => new(138, 183, 138, 192);

    private static VgaImage Sheet()
    {
        var pixels = new byte[40 * 40];
        for (var i = 0; i < pixels.Length; i++)
            pixels[i] = (byte)(i % 251 + 1);
        return new VgaImage(40, 40, pixels, null);
    }

    private static InputState Press(int x, int y) => new() { MouseX = x, MouseY = y, LeftClick = true, LeftDown = true };

    private static InputState Hold(int x, int y) => new() { MouseX = x, MouseY = y, LeftDown = true };

    private static InputState Release() => new() { MouseX = 0, MouseY = 0 };

    [Theory]
    [InlineData(138, 183, -1)]
    [InlineData(145, 191, -1)]
    [InlineData(146, 191, 0)]   // x1 + 8 liegt außerhalb
    [InlineData(138, 192, 1)]
    [InlineData(145, 200, 1)]
    [InlineData(145, 201, 0)]   // y2 + 9 liegt außerhalb
    [InlineData(137, 185, 0)]
    public void HitAreas_Are8x9(int x, int y, int expected)
    {
        Assert.Equal(expected, Advertising().Update(Press(x, y)));
    }

    [Fact]
    public void HoldZero_RepeatsWhileHeld_ThenZeroOnRelease()
    {
        var arrows = Advertising();
        Assert.Equal(1, arrows.Update(Press(140, 195)));
        Assert.Equal(1, arrows.Update(Hold(10, 10))); // gemerkte Richtung, auch außerhalb der Fläche (DGROUP 0x5350)
        Assert.Equal(1, arrows.Update(Hold(140, 195)));
        Assert.Equal(1, arrows.Pressed);
        Assert.Equal(0, arrows.Update(Release()));
        Assert.Equal(0, arrows.Pressed);
        Assert.Equal(0, arrows.Update(Release()));
    }

    [Fact]
    public void HoldNonZero_ReturnsDirectionOnlyOnRelease()
    {
        var arrows = new ArrowPair(150, 169, 174, 169, waitForRelease: true); // Datei 0x5E3B5
        Assert.Equal(0, arrows.Update(Press(151, 170)));
        Assert.Equal(-1, arrows.Pressed);
        Assert.Equal(0, arrows.Update(Hold(151, 170)));
        Assert.Equal(-1, arrows.Update(Release()));
        Assert.Equal(0, arrows.Pressed);
        Assert.Equal(0, arrows.Update(Release()));
    }

    [Fact]
    public void Pressed_DrawsKb4Source_22_19Plus9j_At_XPlus2_YPlus1()
    {
        var sheet = Sheet();
        foreach (var (mx, my, j, x, y) in new[] { (139, 184, 0, 138, 183), (139, 193, 1, 138, 192) })
        {
            var arrows = Advertising();
            arrows.Update(Press(mx, my));
            var s = new Surface();
            arrows.Draw(s, sheet);
            for (var dy = 0; dy < 9; dy++)
                for (var dx = 0; dx < 10; dx++)
                {
                    var inside = dx >= 2 && dx < 2 + 8 && dy >= 1 && dy < 1 + 7;
                    var expected = inside ? sheet.Pixels[(19 + 9 * j + dy - 1) * 40 + 22 + dx - 2] : (byte)0;
                    Assert.Equal(expected, s.Pixels[(y + dy) * Surface.Width + x + dx]);
                }
        }
    }

    [Fact]
    public void NotPressed_DrawsNothing()
    {
        var s = new Surface();
        Advertising().Draw(s, Sheet());
        Assert.All(s.Pixels, p => Assert.Equal(0, p));
    }

    [Fact]
    public void Released_SourceDependsOnHold()
    {
        var sheet = Sheet();
        var s = new Surface();
        Advertising().DrawReleased(s, sheet, 1);
        Assert.Equal(sheet.Pixels[10 * 40 + 22], s.Pixels[193 * Surface.Width + 140]);
        s = new Surface();
        new ArrowPair(150, 169, 174, 169, waitForRelease: true).DrawReleased(s, sheet, -1);
        Assert.Equal(sheet.Pixels[1 * 40 + 21], s.Pixels[170 * Surface.Width + 151]);
    }

    [GameDataFact]
    public void Kb4_PressedArrowsDifferFromReleased()
    {
        var kb4 = new Assets(TestGameData.Load()).Image(ArrowPair.Image);
        for (var j = 0; j < 2; j++)
        {
            var differs = false;
            for (var dy = 0; dy < ArrowPair.ArrowHeight; dy++)
                for (var dx = 0; dx < ArrowPair.ArrowWidth; dx++)
                    differs |= kb4.Pixels[(19 + 9 * j + dy) * kb4.Width + 22 + dx] != kb4.Pixels[(1 + 9 * j + dy) * kb4.Width + 22 + dx];
            Assert.True(differs, $"Pfeil {j}");
        }
    }
}
