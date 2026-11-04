using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI;
using EishockeyManager.UI.Screens.ManagerArea;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>ui-rest Teil 1 (#535): Pfeile der Werbung über <see cref="ArrowPair"/> (em.exe Datei 0x2BA72).</summary>
public class AdvertisingArrowTests
{
    private static (ScreenDriver D, AdvertisingScreen Screen) Open()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        d.OpenTile("Werbung");
        var screen = Assert.IsType<AdvertisingScreen>(d.Host.Current);
        d.Click(AdvertisingScreen.BoardRect(0).X + 5, AdvertisingScreen.BoardRect(0).Y + 5);
        return (d, screen);
    }

    [GameDataFact]
    public void HitAreas_Are8x9_AtOriginalPositions()
    {
        Assert.Equal(new Rect(138, 183, 8, 9), AdvertisingScreen.ArrowUp);
        Assert.Equal(new Rect(138, 192, 8, 9), AdvertisingScreen.ArrowDown);
        var (d, screen) = Open();
        d.Click(146, 195); // x1 + 8 liegt außerhalb
        Assert.Equal(-1, screen.Cursor);
        d.Click(145, 195);
        Assert.Equal(0, screen.Cursor);
    }

    [GameDataFact]
    public void HeldArrow_RepeatsOnlyAfterBrake_AndClampsAtTheEnd()
    {
        var (d, screen) = Open();
        d.Frame(i => { i.MouseX = 142; i.MouseY = 196; i.LeftClick = true; i.LeftDown = true; });
        Assert.Equal(0, screen.Cursor);
        for (var f = 1; f < AdvertisingScreen.RepeatDelayFrames; f++)
            d.Frame(i => { i.MouseX = 142; i.MouseY = 196; i.LeftDown = true; });
        Assert.Equal(0, screen.Cursor); // noch in der Bremse
        d.Frame(i => { i.MouseX = 142; i.MouseY = 196; i.LeftDown = true; });
        Assert.Equal(1, screen.Cursor);
        for (var f = 0; f < 20 * AdvertisingScreen.RepeatEveryFrames; f++)
            d.Frame(i => { i.MouseX = 142; i.MouseY = 196; i.LeftDown = true; });
        Assert.Equal(Sponsor.Companies.Length - 1, screen.Cursor); // [ORIGINAL] 0x2BE0A: kein Umbruch
        d.Frame(); // losgelassen
        d.Click(142, 187);
        Assert.Equal(Sponsor.Companies.Length - 2, screen.Cursor);
    }

    [GameDataFact]
    public void PressedArrow_IsDrawnFromKb4()
    {
        var (d, _) = Open();
        d.Frame(i => { i.MouseX = 142; i.MouseY = 196; i.LeftClick = true; i.LeftDown = true; });
        d.Frame(i => { i.MouseX = 142; i.MouseY = 196; i.LeftDown = true; });
        var pressed = new byte[8 * 7];
        var released = new byte[8 * 7];
        for (var y = 0; y < 7; y++)
            for (var x = 0; x < 8; x++)
                pressed[y * 8 + x] = d.Surface.Pixels[(192 + 1 + y) * 320 + 138 + 2 + x];
        d.Frame();
        for (var y = 0; y < 7; y++)
            for (var x = 0; x < 8; x++)
                released[y * 8 + x] = d.Surface.Pixels[(192 + 1 + y) * 320 + 138 + 2 + x];
        Assert.NotEqual(pressed, released);
    }
}
