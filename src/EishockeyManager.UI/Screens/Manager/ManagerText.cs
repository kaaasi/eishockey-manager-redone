using EishockeyManager.Formats;

namespace EishockeyManager.UI.Screens.ManagerArea;

/// <summary>Textbausteine der Manager-Bildschirme.</summary>
internal static class ManagerText
{
    private static readonly byte[] Scratch = new byte[Surface.Width * Surface.Height];

    /// <summary>
    /// Zweifarbige Beschriftung wie „Kontostand:“ im Bildschirm Finanzen: die oberen Pixelreihen der Glyphen
    /// in <paramref name="top"/>, die unteren in <paramref name="bottom"/> (Video 5:45).
    /// </summary>
    public static int TwoTone(Surface s, BitmapFont font, int x, int y, string text, byte top, byte bottom, int split = 3)
    {
        Array.Clear(Scratch);
        var end = font.Draw(Scratch, Surface.Width, x, y, text, 1);
        var x1 = Math.Min(Surface.Width, end + 1);
        for (var row = 0; row < font.Height; row++)
        {
            var py = y + row;
            if ((uint)py >= Surface.Height)
                continue;
            var color = row < split ? top : bottom;
            for (var px = Math.Max(0, x); px < x1; px++)
                if (Scratch[py * Surface.Width + px] != 0)
                    s.Set(px, py, color);
        }
        return end;
    }

    /// <summary>Text einer Nachricht für den Original-Font: „%“ fehlt dort, em.exe schreibt dafür „#“.</summary>
    public static string ForFont(string text) => text.Replace('%', '#');

    /// <summary>„1.234 DM“ ohne Vorzeichenwechsel (Formatierung wie im Original).</summary>
    public static string Money(long amount) => Core.GameState.FormatMoney(amount);

    /// <summary>Stückzahl mit Tausenderpunkt („1.000 St.“).</summary>
    public static string Pieces(long count) =>
        count.ToString("#,0", System.Globalization.CultureInfo.GetCultureInfo("de-DE")) + " St.";
}
