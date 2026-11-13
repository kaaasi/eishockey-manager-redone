using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// L3 (Runde 18, Spur <c>livespiel-ui</c> B1): „Spiele laufen“ ohne Live-Anzeige, em.exe Datei 0x37B82–0x37C5C mit der
/// Spieluhr 0x51E90 (Balken 0x533D0, Ziffern 0x53234). Beleg im Video: <c>karriere_02315</c> (Minute 08, oberer Balken
/// 32 Pixel gelb, die beiden anderen rot, Ziffern „0“ ab x 96 und „8“ ab x 106).
/// </summary>
public class SpieleLaufenOriginalTests
{
    private static byte At(Surface s, int x, int y) => s.Pixels[y * Surface.Width + x];

    /// <summary>„Tag weiter“ mit Managerspiel: Banner, dann „Spiele laufen“ bis zur gegebenen Minute.</summary>
    private static ScreenDriver RunningAt(int minute)
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var menu = Assert.IsType<MainMenuScreen>(d.Host.Current);
        d.Host.Show(new MatchDayScreen(d.Host, menu, [new MatchesStep(0, Competition.League, true)]));
        d.Frames(MatchDayScreen.BannerFrames + 1); // Banner bis zum Wechsel, dann Minute 0
        Assert.Null(((MatchDayScreen)d.Host.Current!).CurrentBanner);
        d.Frames(minute * MatchDayScreen.FramesPerMinute);
        return d;
    }

    [GameDataFact]
    public void Minute8_ClockLikeOriginal()
    {
        var s = RunningAt(8).Surface;

        // [ORIGINAL] 0x51E90 → 0x533D0(9, 192 + 8k, 79, 4 · (8 − 20k)): oberer Balken zweizeilig 0x18/0x13 von x 9 bis 41,
        // Rest bis x 88 in 0x11; die beiden anderen Balken ganz 0x11.
        for (var x = 9; x <= 41; x++)
        {
            Assert.Equal(0x18, At(s, x, 192));
            Assert.Equal(0x13, At(s, x, 193));
        }
        for (var x = 42; x <= 88; x++)
        {
            Assert.Equal(0x11, At(s, x, 192));
            Assert.Equal(0x11, At(s, x, 193));
        }
        foreach (var y in new[] { 200, 201, 208, 209 })
            for (var x = 9; x <= 88; x++)
                Assert.Equal(0x11, At(s, x, y));
        Assert.Equal((byte)3, At(s, 89, 192));
        Assert.Equal((byte)3, At(s, 9, 194));
    }

    [GameDataFact]
    public void Minute8_DigitsAt96And106_Style1()
    {
        var d = RunningAt(8);
        var s = d.Surface;
        var digits = d.Host.Assets.Image(LiveWidgets.DigitsGraphic);

        // [ORIGINAL] 0x51EE9–0x51F33: „0“ bei (96 | 196), „8“ bei (106 | 196); Stil 1: Zellgrund Farbe 3.
        foreach (var (digit, x0) in new[] { (0, 96), (8, 106) })
            for (var y = 0; y < 10; y++)
                for (var x = 0; x < 8; x++)
                {
                    var p = digits.Pixels[y * digits.Width + digit * 8 + x];
                    Assert.Equal(p == 0 ? LiveWidgets.DigitCellFill : p, At(s, x0 + x, 196 + y));
                }
        // zwischen den Ziffern (x 104, 105) und davor (x 95) bleibt die Füllung des Kastens
        Assert.Equal((byte)3, At(s, 95, 200));
        Assert.Equal((byte)3, At(s, 104, 200));
        Assert.Equal((byte)3, At(s, 105, 200));
    }

    [GameDataFact]
    public void Box_FrameColor0_Fill3()
    {
        var s = RunningAt(8).Surface;

        // [ORIGINAL] 0x37BD3–0x37C16: Rahmen (3 | 183)–(123 | 218) Farbe 0, Füllung (4 | 184)–(122 | 217) Farbe 3.
        for (var x = 3; x <= 123; x++)
        {
            Assert.Equal(0, At(s, x, 183));
            Assert.Equal(0, At(s, x, 218));
        }
        for (var y = 183; y <= 218; y++)
        {
            Assert.Equal(0, At(s, 3, y));
            Assert.Equal(0, At(s, 123, y));
        }
        Assert.Equal(3, At(s, 4, 184));
        Assert.Equal(3, At(s, 122, 217));
        Assert.Equal(3, At(s, 120, 190));
    }

    [GameDataFact]
    public void Text_Baseline80_Color30_Shadow0()
    {
        var d = RunningAt(8);
        var s = d.Surface;

        // [ORIGINAL] 0x37BAA–0x37BCB: Schrift 1, zentriert x 0–319, Grundlinie 80, Farbe 30, Schatten 0 (1 Pixel rechts unten).
        int minX = int.MaxValue, maxX = -1;
        for (var y = 40; y < 100; y++)
            for (var x = 0; x < 240; x++)
                if (At(s, x, y) == 30)
                {
                    minX = Math.Min(minX, x);
                    maxX = Math.Max(maxX, x);
                }
        // Grundlinie = unterste Zeile des „S“ (ohne Unterlänge; das „p“ reicht tiefer)
        var maxYOfS = -1;
        for (var y = 40; y < 100; y++)
            for (var x = minX; x < minX + 5; x++)
                if (At(s, x, y) == 30)
                    maxYOfS = Math.Max(maxYOfS, y);
        Assert.Equal(80, maxYOfS);
        // zentriert x 0–319: x = 0 + 319 / 2 − Breite / 2 (0x0B6D9, beide Hälften abgerundet); Text ohne Rand rechts
        var width = Screens.Competitions.CompetitionWidgets.Width(d.Host.Assets.Bold, "Spiele laufen");
        Assert.Equal(319 / 2 - width / 2, minX);
        // Schatten Farbe 0 rechts unten neben der letzten Spalte des Texts
        var shadowFound = false;
        for (var y = 60; y <= 85; y++)
            shadowFound |= At(s, maxX + 1, y) == 0;
        Assert.True(shadowFound, "kein Schatten in Farbe 0");
    }

    [GameDataFact]
    public void Clock_MinuteByMinute_BarsFillFourPixelsPerMinute()
    {
        var assets = new Assets(TestGameData.Load());
        foreach (var minute in new[] { 0, 1, 19, 20, 21, 40, 59, 60 })
        {
            var s = new Surface();
            s.Fill(new Rect(0, 0, Surface.Width, Surface.Height), 3);
            LiveWidgets.Clock(s, assets, 1, minute);
            for (var k = 0; k < 3; k++)
            {
                var value = Math.Clamp(4 * (minute - 20 * k), 0, 79);
                var yellow = 0;
                for (var x = 9; x <= 88; x++)
                    if (At(s, x, 192 + 8 * k) == 0x18)
                        yellow++;
                // [ORIGINAL] 0x533D0: Linie (x | y)–(x + Wert | y), beide Enden eingeschlossen
                Assert.Equal(value == 0 ? 0 : value + 1, yellow);
            }
        }
    }
}
