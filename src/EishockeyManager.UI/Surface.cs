using EishockeyManager.Formats;

namespace EishockeyManager.UI;

public readonly record struct Rect(int X, int Y, int W, int H)
{
    public int Right => X + W;
    public int Bottom => Y + H;
    public bool Contains(int x, int y) => x >= X && x < Right && y >= Y && y < Bottom;
    public Rect Inflate(int d) => new(X - d, Y - d, W + 2 * d, H + 2 * d);
}

/// <summary>
/// Bildschirmspeicher wie im Original: 320 × 240 Pixel, ein Byte (Palettenindex) pro Pixel.
/// Alle Bildschirme zeichnen hier hinein; die Darstellung (Fenster, PNG) rechnet am Ende in RGB um.
/// </summary>
public sealed class Surface
{
    public const int Width = 320;
    public const int Height = 240;

    public byte[] Pixels { get; } = new byte[Width * Height];

    /// <summary>Aktive Palette (wie der VGA-DAC).</summary>
    public Palette Palette { get; set; } = Palette.Grayscale();

    public void Clear(byte color) => Array.Fill(Pixels, color);

    public void Set(int x, int y, byte color)
    {
        if ((uint)x < Width && (uint)y < Height)
            Pixels[y * Width + x] = color;
    }

    public void Fill(Rect r, byte color)
    {
        var x0 = Math.Max(0, r.X);
        var x1 = Math.Min(Width, r.Right);
        if (x1 <= x0)
            return;
        for (var y = Math.Max(0, r.Y); y < Math.Min(Height, r.Bottom); y++)
            Pixels.AsSpan(y * Width + x0, x1 - x0).Fill(color);
    }

    public void HLine(int x, int y, int length, byte color) => Fill(new Rect(x, y, length, 1), color);

    public void VLine(int x, int y, int length, byte color) => Fill(new Rect(x, y, 1, length), color);

    /// <summary>Rechteckrahmen (1 Pixel).</summary>
    public void Outline(Rect r, byte color)
    {
        HLine(r.X, r.Y, r.W, color);
        HLine(r.X, r.Bottom - 1, r.W, color);
        VLine(r.X, r.Y, r.H, color);
        VLine(r.Right - 1, r.Y, r.H, color);
    }

    public void Blit(VgaImage image, int x, int y, int? transparent = null) =>
        Blit(image.Pixels, image.Width, new Rect(0, 0, image.Width, image.Height), x, y, transparent);

    public void Blit(VgaImage image, Rect source, int x, int y, int? transparent = null) =>
        Blit(image.Pixels, image.Width, source, x, y, transparent);

    /// <summary>Kopiert einen Ausschnitt eines Palettenbildes; Pixel außerhalb des Bildschirms werden abgeschnitten.</summary>
    public void Blit(ReadOnlySpan<byte> pixels, int stride, Rect source, int x, int y, int? transparent = null)
    {
        for (var sy = 0; sy < source.H; sy++)
        {
            var dy = y + sy;
            if ((uint)dy >= Height)
                continue;
            var srcRow = pixels.Slice((source.Y + sy) * stride + source.X, source.W);
            for (var sx = 0; sx < source.W; sx++)
            {
                var dx = x + sx;
                if ((uint)dx >= Width)
                    continue;
                var value = srcRow[sx];
                if (transparent == value)
                    continue;
                Pixels[dy * Width + dx] = value;
            }
        }
    }

    /// <summary>Text mit einem Originalfont; liefert die x-Position hinter dem Text.</summary>
    public int Text(BitmapFont font, int x, int y, string text, byte color) =>
        font.Draw(Pixels, Width, x, y, text, color);

    /// <summary>Text mittig um <paramref name="centerX"/> (bei ungerader Restbreite wie im Original abgerundet).</summary>
    public void TextCentered(BitmapFont font, int centerX, int y, string text, byte color) =>
        Text(font, centerX - font.Measure(text) / 2, y, text, color);

    public void TextRight(BitmapFont font, int rightX, int y, string text, byte color) =>
        Text(font, rightX - font.Measure(text), y, text, color);

    public VgaImage ToImage() => new(Width, Height, (byte[])Pixels.Clone(), null);

    /// <summary>RGBA-Daten für die Anzeige (Index 0..255 über die aktive Palette).</summary>
    public void ToRgba(Span<byte> rgba)
    {
        var rgb = Palette.Rgb;
        for (var i = 0; i < Pixels.Length; i++)
        {
            var p = Pixels[i] * 3;
            rgba[i * 4] = rgb[p];
            rgba[i * 4 + 1] = rgb[p + 1];
            rgba[i * 4 + 2] = rgb[p + 2];
            rgba[i * 4 + 3] = 255;
        }
    }
}
