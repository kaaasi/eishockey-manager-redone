namespace EishockeyManager.Formats;

/// <summary>256-Farben-Palette mit 8-Bit-RGB-Werten.</summary>
public sealed class Palette
{
    public const int ColorCount = 256;

    /// <summary>Länge einer Palette in den Spieldateien: 255 Farben × 3 Bytes.</summary>
    public const int StoredLength = 765;

    private readonly byte[] _rgb = new byte[ColorCount * 3];

    public ReadOnlySpan<byte> Rgb => _rgb;

    public (byte R, byte G, byte B) this[int index] =>
        (_rgb[index * 3], _rgb[index * 3 + 1], _rgb[index * 3 + 2]);

    /// <summary>
    /// Liest eine gespeicherte Palette. Die meisten Dateien (z. B. kb\21.vga und die
    /// in Bildern eingebetteten Paletten) enthalten bereits 8-Bit-Werte (VGA-DAC-Wert × 4).
    /// Bei <paramref name="sixBit"/> = true werden 6-Bit-DAC-Werte (0..63) hochskaliert.
    /// </summary>
    public static Palette FromBytes(ReadOnlySpan<byte> data, bool sixBit = false)
    {
        var palette = new Palette();
        var length = Math.Min(data.Length, palette._rgb.Length);
        for (var i = 0; i < length; i++)
        {
            var value = data[i];
            palette._rgb[i] = sixBit ? (byte)((value << 2) | (value >> 4)) : value;
        }
        return palette;
    }

    public static Palette Load(string path, bool sixBit = false) =>
        FromBytes(File.ReadAllBytes(path), sixBit);

    /// <summary>Graustufen-Palette als Notlösung, wenn keine Palette verfügbar ist.</summary>
    public static Palette Grayscale()
    {
        var palette = new Palette();
        for (var i = 0; i < ColorCount; i++)
        {
            palette._rgb[i * 3] = (byte)i;
            palette._rgb[i * 3 + 1] = (byte)i;
            palette._rgb[i * 3 + 2] = (byte)i;
        }
        return palette;
    }
}
