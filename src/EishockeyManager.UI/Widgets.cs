using EishockeyManager.Formats;

namespace EishockeyManager.UI;

public enum FrameStyle
{
    /// <summary>Grau-blauer Rahmen (kb\2, linke Spalte).</summary>
    Gray = 0,

    /// <summary>Orangefarbener Rahmen der Menükacheln (kb\2, rechte Spalte).</summary>
    Orange = 1,
}

/// <summary>Bausteine der Oberfläche, die das Original aus Einzelteilen zusammensetzt.</summary>
public static class Widgets
{
    private const int PieceWidth = 24;
    private const int PieceHeight = 19;

    /// <summary>
    /// Fenster/Rahmen aus den neun Teilen in kb\2 (je 24 × 19 Pixel, Raster 25 × 20):
    /// Reihen 0–8 = oben links, oben, oben rechts, links, Mitte, rechts, unten links, unten, unten rechts.
    /// Ränder und Mitte werden gekachelt. Optional wird das Innere ab <paramref name="fillInset"/> gefüllt.
    /// </summary>
    public static void Frame(Surface s, Assets a, Rect r, FrameStyle style, byte? fill = null, int fillInset = 4)
    {
        var sheet = a.Image(2);
        var col = (int)style * 25;
        Rect Piece(int index, int w = PieceWidth, int h = PieceHeight) => new(col, index * 20, w, h);

        // Mitte
        for (var y = 0; y < r.H; y += PieceHeight)
            for (var x = 0; x < r.W; x += PieceWidth)
                s.Blit(sheet, Piece(4, Math.Min(PieceWidth, r.W - x), Math.Min(PieceHeight, r.H - y)), r.X + x, r.Y + y);

        // Ränder oben/unten
        for (var x = 0; x < r.W; x += PieceWidth)
        {
            var w = Math.Min(PieceWidth, r.W - x);
            s.Blit(sheet, Piece(1, w), r.X + x, r.Y);
            s.Blit(sheet, Piece(7, w), r.X + x, r.Bottom - PieceHeight);
        }

        // Ränder links/rechts
        for (var y = 0; y < r.H; y += PieceHeight)
        {
            var h = Math.Min(PieceHeight, r.H - y);
            s.Blit(sheet, Piece(3, PieceWidth, h), r.X, r.Y + y);
            s.Blit(sheet, Piece(5, PieceWidth, h), r.Right - PieceWidth, r.Y + y);
        }

        // Ecken
        s.Blit(sheet, Piece(0), r.X, r.Y);
        s.Blit(sheet, Piece(2), r.Right - PieceWidth, r.Y);
        s.Blit(sheet, Piece(6), r.X, r.Bottom - PieceHeight);
        s.Blit(sheet, Piece(8), r.Right - PieceWidth, r.Bottom - PieceHeight);

        if (fill is { } color)
            s.Fill(new Rect(r.X + fillInset, r.Y + fillInset, r.W - 2 * fillInset, r.H - 2 * fillInset), color);
    }

    /// <summary>Lampe aus kb\3: grün = bereit, rot = am Zug.</summary>
    public static void Lamp(Surface s, Assets a, int x, int y, bool red) =>
        s.Blit(a.Image(3), new Rect(196, red ? 219 : 225, 6, 6), x, y);

    /// <summary>Managerkopf 0–11 aus kb\0 (Zellen 40 × 40 im Raster 41).</summary>
    public static Rect HeadSource(int head) => new(head % 3 * 41, head / 3 * 41, 40, 40);

    public static void Head(Surface s, Assets a, int head, int x, int y) =>
        s.Blit(a.Image(0), HeadSource(head), x, y);

    /// <summary>Vereinslogo (38 × 38) oder nichts, wenn der Verein kein Logo hat.</summary>
    public static void Logo(Surface s, Assets a, int? logoGraphic, int x, int y)
    {
        if (logoGraphic is { } n)
            s.Blit(a.Image(n), x, y);
    }
}
