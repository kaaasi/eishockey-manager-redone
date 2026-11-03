using EishockeyManager.Tests.Support;
using EishockeyManager.UI;
using EishockeyManager.UI.Screens;

namespace EishockeyManager.UI.Tests;

/// <summary>M-K9: Zwischenbild beim Bildschirmwechsel (em.exe Datei 0x0C5CE/0x45D46).</summary>
public class ScreenTransitionTests
{
    private sealed class Blank(ScreenHost host) : Screen(host)
    {
        public override void Update(InputState input) { }
        public override void Draw(Surface surface) { }
    }

    private static readonly ScreenHost Host = new(null!);
    private static TimeSpan Ms(int ms) => TimeSpan.FromMilliseconds(ms);

    [Fact]
    public void Switch_StartsTransitionForDuration()
    {
        var t = new ScreenTransition();
        var a = new Blank(Host);
        t.Observe(a, Ms(0));
        Assert.False(t.IsActive(Ms(0))); // erster Bildschirm: kein Wechsel
        t.Observe(a, Ms(10));
        Assert.False(t.IsActive(Ms(10)));
        t.Observe(new Blank(Host), Ms(100));
        Assert.True(t.IsActive(Ms(100)));
        Assert.True(t.IsActive(Ms(100) + ScreenTransition.DefaultDuration - Ms(1)));
        Assert.False(t.IsActive(Ms(100) + ScreenTransition.DefaultDuration));
    }

    [Fact]
    public void SkippedScreensAndZeroDuration_HaveNoTransition()
    {
        var overlay = new Blank(Host);
        var t = new ScreenTransition(s => ReferenceEquals(s, overlay));
        t.Observe(new Blank(Host), Ms(0));
        t.Observe(overlay, Ms(1));
        Assert.False(t.IsActive(Ms(1)));

        var off = new ScreenTransition { Duration = TimeSpan.Zero };
        off.Observe(new Blank(Host), Ms(0));
        off.Observe(new Blank(Host), Ms(1));
        Assert.False(off.IsActive(Ms(1)));
    }

    [GameDataFact]
    public void Draw_ShowsKb22WithPageFlip_BlackWithout()
    {
        var assets = new Assets(TestGameData.Load());
        var t = new ScreenTransition();
        t.Observe(new Blank(Host), Ms(0));
        t.Observe(new Blank(Host), Ms(1));

        var s = new Surface();
        Assert.True(t.Draw(s, assets, Ms(2)));
        Assert.Equal(assets.Image(MainMenuScreen.Background).Pixels, s.Pixels);

        t.PageFlip = false; // nach dem Laden eines Spielstands (Datei 0x5687A): Bild aus
        s = new Surface();
        s.Fill(new Rect(0, 0, Surface.Width, Surface.Height), 5);
        Assert.True(t.Draw(s, assets, Ms(2)));
        Assert.All(s.Pixels, p => Assert.Equal(0, p));
        Assert.Equal(((byte)0, (byte)0, (byte)0), s.Palette![0]);

        Assert.False(t.Draw(s, assets, Ms(1) + ScreenTransition.DefaultDuration));
    }
}
