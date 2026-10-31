using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur credits (#207): Klick in die Titelleiste des Hauptmenüs öffnet das Credits-Fenster kb\15 wie im Original
/// (em.exe 0x45483 → Overlay 0xF8, Datei 0x399D0), ergänzt um die Zeile „Remake“ / „Jörg Klinge“.
/// </summary>
public class CreditsScreenTests
{
    private static ScreenDriver MainMenu()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        Assert.IsType<MainMenuScreen>(d.Host.Current);
        return d;
    }

    /// <summary>Regressionstest #207: Vorher blieb der Klick auf „EISHOCKEY MANAGER“ ohne Wirkung.</summary>
    [GameDataFact]
    public void ClickOnTitleBar_OpensCredits_AndNextClickCloses()
    {
        var d = MainMenu();
        d.Click(160, 10);
        Assert.IsType<CreditsScreen>(d.Host.Current);

        d.Click(5, 230); // irgendwo, auch außerhalb des Fensters [ORIGINAL] 0xB908
        Assert.IsType<MainMenuScreen>(d.Host.Current);
    }

    /// <summary>[ORIGINAL] Auslöser: Mausereignis 1 oder 3 und y &lt; 0x15, x beliebig (0x4544E–0x45488).</summary>
    [GameDataFact]
    public void Trigger_IsLeftOrRightClick_AboveRow21_AnyX()
    {
        var d = MainMenu();
        d.RightClick(2, 20);
        Assert.IsType<CreditsScreen>(d.Host.Current);
        d.RightClick(300, 200);
        Assert.IsType<MainMenuScreen>(d.Host.Current);

        d.Click(317, 0);
        Assert.IsType<CreditsScreen>(d.Host.Current);
        d.Click(160, 100);

        d.Click(160, 21); // erste Zeile der Infoleiste: keine Credits
        Assert.IsType<MainMenuScreen>(d.Host.Current);
    }

    /// <summary>[ORIGINAL] Tasten schließen das Fenster nicht (Wartefunktion 0xB908 prüft nur Mausereignisse).</summary>
    [GameDataFact]
    public void Keys_DoNotClose_AndDoNotReachMainMenu()
    {
        var d = MainMenu();
        var day = d.Host.Game!.Day;
        d.Click(160, 10);
        d.Press(Key.Space); // im Hauptmenü wäre das „und tschüß…“
        d.Press(Key.Escape);
        Assert.IsType<CreditsScreen>(d.Host.Current);
        Assert.Equal(day, d.Host.Game.Day);
    }

    [GameDataFact]
    public void OpeningAndClosing_ChangesNoGameState()
    {
        var d = MainMenu();
        var before = StateFingerprint.Of(d.Host.Game!);
        d.Click(160, 10);
        d.Frames(3);
        d.Click(160, 100);
        d.Frame();
        var after = StateFingerprint.Of(d.Host.Game!);
        Assert.True(before == after, StateFingerprint.FirstDifference(before, after));
    }

    /// <summary>[ORIGINAL] kb\15 bei (51 | 61), darunter bleibt das Hauptmenü stehen.</summary>
    [GameDataFact]
    public void Window_ShowsOriginalPictureAt51_61_OverMainMenu()
    {
        var d = MainMenu();
        d.Move(1, 1);
        var menu = (byte[])d.Surface.Pixels.Clone();
        d.Click(160, 10);
        d.Move(1, 1);

        var picture = d.Host.Assets.Image(CreditsScreen.Picture);
        Assert.Equal((CreditsScreen.OriginalWidth, CreditsScreen.OriginalHeight), (picture.Width, picture.Height));
        var area = CreditsScreen.Area;
        for (var y = 0; y < Surface.Height; y++)
        for (var x = 0; x < Surface.Width; x++)
        {
            var actual = d.Surface.Pixels[y * Surface.Width + x];
            if (!area.Contains(x, y))
                Assert.True(menu[y * Surface.Width + x] == actual, $"Hauptmenü verändert bei ({x} | {y})");
            else if (y - area.Y < 147) // bis über dem unteren Rand unverändert das Originalbild
                Assert.True(picture[x - area.X, y - area.Y] == actual, $"kb\\15 weicht ab bei ({x} | {y})");
        }
        Assert.True(area.Bottom <= Surface.Height, "verlängertes Fenster passt auf den Bildschirm");
    }

    /// <summary>
    /// Die Ergänzung steht in Spalte, Schrift, Farbe und Abstand wie die Zeile davor: Die letzte Originalzeile
    /// („PC Umsetzung“ / „Ralph Noeske“) und die neue Zeile sind mit demselben Zeichenweg pixelgenau nachgebildet.
    /// </summary>
    [GameDataFact]
    public void RemakeLine_HasSameColumnFontColorAndSpacing_AsLineBefore()
    {
        var assets = new Assets(TestGameData.Load());
        var original = assets.Image(CreditsScreen.Picture);
        var window = CreditsScreen.BuildWindow(assets);

        // Messung am Original: die letzten Zeilen des Bilds sind genau so gezeichnet
        AssertLine(assets.Mini, original, CreditsScreen.LineBeforeLast, null, "Franz Hauerstein");
        AssertLine(assets.Mini, original, CreditsScreen.LastOriginalLine, "PC Umsetzung", "Ralph Noeske");
        AssertLine(assets.Mini, window, CreditsScreen.LastOriginalLine, "PC Umsetzung", "Ralph Noeske");

        // Ergänzung: gleicher Zeichenweg, Abstand wie zwischen den beiden letzten Originalzeilen
        AssertLine(assets.Mini, window, CreditsScreen.RemakeLine, "Remake", "Jörg Klinge");
        Assert.Equal(CreditsScreen.LastOriginalLine - CreditsScreen.LineBeforeLast,
            CreditsScreen.RemakeLine - CreditsScreen.LastOriginalLine);

        // Unterer Rand des Originals (letzte vier Zeilen) rückt unverändert nach unten
        for (var r = 1; r <= 4; r++)
        for (var x = 0; x < original.Width; x++)
            Assert.Equal(original[x, original.Height - r], window[x, window.Height - r]);
    }

    /// <summary>
    /// Prüft eine Credits-Zeile: Mini-Font, Schatten Farbe 0 rechts unten, links x 11 Farbe 30, rechts x 100 Farbe 7 –
    /// jedes Pixel der Zeile (Höhe 7 mit Schatten) über die Breite des Textes stimmt, keine fremden Text- oder Schattenpixel.
    /// </summary>
    private static void AssertLine(BitmapFont font, VgaImage image, int y, string? left, string right)
    {
        if (left is not null)
            AssertText(font, image, CreditsScreen.LeftColumn, y, left, CreditsScreen.LeftColor);
        AssertText(font, image, CreditsScreen.RightColumn, y, right, CreditsScreen.RightColor);
    }

    private static void AssertText(BitmapFont font, VgaImage image, int x0, int y0, string text, byte color)
    {
        var w = font.Measure(text) + 1;
        var h = font.Height + 1;
        var mask = new byte[w * h];
        font.Draw(mask, w, 1, 1, text, 1);
        font.Draw(mask, w, 0, 0, text, 2);
        for (var y = 0; y < h; y++)
        for (var x = 0; x < w; x++)
        {
            var actual = image[x0 + x, y0 + y];
            switch (mask[y * w + x])
            {
                case 2: Assert.True(actual == color, $"„{text}“: Schriftpixel ({x0 + x} | {y0 + y}) = {actual}, erwartet {color}"); break;
                case 1: Assert.True(actual == CreditsScreen.ShadowColor, $"„{text}“: Schattenpixel ({x0 + x} | {y0 + y}) = {actual}"); break;
                default:
                    Assert.True(actual != color && actual != CreditsScreen.ShadowColor,
                        $"„{text}“: fremdes Text-/Schattenpixel bei ({x0 + x} | {y0 + y})");
                    break;
            }
        }
    }
}
