using EishockeyManager.Formats;

namespace EishockeyManager.UI.Screens.Competitions;

/// <summary>Bausteine der Wettbewerbs-Bildschirme (Paket „wettbewerbe“).</summary>
public static class CompetitionWidgets
{
    /// <summary>
    /// Oberkante der Zeichenzelle zu einer Grundlinie wie in den Textaufrufen des Originals (em.exe 0x902:0x885):
    /// die Grundlinie ist die unterste Zeile der Großbuchstaben (Höhe − Unterlänge − 1 unter der Zellenoberkante).
    /// </summary>
    public static int Top(BitmapFont font, int baseline) => baseline - (font.Height - font.Baseline - 1);

    /// <summary>
    /// [ORIGINAL] Text zentriert zwischen <paramref name="x1"/> und <paramref name="x2"/> auf der Grundlinie, mit
    /// Schatten (1 Pixel rechts unten) in der Farbe <paramref name="shadow"/> – Aufruf 0x902:0x885
    /// (Ziel, Text, x1, x2, Grundlinie, Farbe, Schattenfarbe). Vermessen an „DEB-Pokal Auslosung“ (Video 3:55).
    /// </summary>
    public static void Centered(Surface s, BitmapFont font, int x1, int x2, int baseline, string text, byte color, byte shadow)
    {
        var x = x1 + (x2 - x1 - font.Measure(text)) / 2;
        var y = Top(font, baseline);
        s.Text(font, x + 1, y + 1, text, shadow);
        s.Text(font, x, y, text, color);
    }
}
