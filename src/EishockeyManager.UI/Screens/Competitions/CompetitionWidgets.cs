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
    public static void Centered(Surface s, BitmapFont font, int x1, int x2, int baseline, string text, byte color, byte? shadow, bool left = false)
    {
        // x2 = 0xFFFF im Original: linksbündig ab x1
        var x = left ? x1 : x1 + (x2 - x1 - Width(font, text)) / 2;
        var y = Top(font, baseline);
        if (shadow is { } dark) // Schattenfarbe 0xFF im Original = ohne Schatten
            Draw(s, font, x + 1, y + 1, text, dark);
        Draw(s, font, x, y, text, color);
    }

    /// <summary>
    /// Zahl wie die Zahlenausgabe des Originals (em.exe 0x99F:0x2C2 mit Mindestbreite DGROUP 0x374): fehlende Stellen
    /// werden mit der Breite der Ziffer 0 aufgefüllt (vermessen: Titel „Gruppe 1“, Termine „ 4.11.“, Tabelle „ 0: 0“,
    /// Video 13:11). Liefert Folgen aus Text und Leerraum für <see cref="Width"/> und <see cref="Draw"/>.
    /// </summary>
    public static string Number(int value, int width)
    {
        var digits = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return new string(FigureSpace, Math.Max(0, width - digits.Length)) + digits;
    }

    /// <summary>Platzhalter für eine fehlende Ziffer (wird so breit wie „0“ gezeichnet).</summary>
    public const char FigureSpace = '\u2007';

    /// <summary>Breite eines Textes mit <see cref="FigureSpace"/>.</summary>
    public static int Width(BitmapFont font, string text)
    {
        var figures = text.Count(c => c == FigureSpace);
        return font.Measure(text.Replace(FigureSpace.ToString(), "")) + figures * font.Measure("0");
    }

    /// <summary>Zeichnet Text mit <see cref="FigureSpace"/> ab Zellenoberkante <paramref name="top"/>.</summary>
    public static void Draw(Surface s, BitmapFont font, int x, int top, string text, byte color)
    {
        foreach (var part in text.Split(FigureSpace))
        {
            if (part.Length > 0)
                s.Text(font, x, top, part, color);
            x += font.Measure(part) + font.Measure("0");
        }
    }

    /// <summary>[ORIGINAL] Linksbündiger Text ab <paramref name="x"/> auf der Grundlinie (Aufruf 0x902:0x831, ohne Schatten).</summary>
    public static void Left(Surface s, BitmapFont font, int x, int baseline, string text, byte color) =>
        Draw(s, font, x, Top(font, baseline), text, color);
}
