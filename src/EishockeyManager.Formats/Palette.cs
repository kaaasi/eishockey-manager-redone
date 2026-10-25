namespace EishockeyManager.Formats;

/// <summary>256-Farben-Palette mit 8-Bit-RGB-Werten für die Darstellung.</summary>
/// <remarks>
/// Der VGA-DAC verarbeitet nur 6 Bit pro Farbkanal. Das Spiel schreibt die gespeicherten
/// Bytes unverändert in den DAC; die Hardware wertet daher nur die unteren 6 Bit aus.
/// Deshalb wird jeder gespeicherte Wert mit <c>&amp; 0x3F</c> maskiert und dann auf 8 Bit
/// hochgerechnet. Nur so stimmen die Farben mit dem Original überein (abgeglichen mit Videos).
/// </remarks>
public sealed class Palette
{
    public const int ColorCount = 256;

    /// <summary>Länge einer Palette in den Spieldateien: 255 Farben × 3 Bytes.</summary>
    public const int StoredLength = 765;

    private readonly byte[] _rgb = new byte[ColorCount * 3];
    private readonly byte[] _stored = new byte[StoredLength];

    /// <summary>Farben für die Darstellung (8 Bit je Kanal).</summary>
    public ReadOnlySpan<byte> Rgb => _rgb;

    /// <summary>Die Bytes so, wie sie in der Datei stehen (zum Zurückschreiben).</summary>
    public ReadOnlySpan<byte> StoredBytes => _stored;

    public (byte R, byte G, byte B) this[int index] =>
        (_rgb[index * 3], _rgb[index * 3 + 1], _rgb[index * 3 + 2]);

    /// <summary>Rechnet einen gespeicherten Palettenwert so um, wie ihn der VGA-DAC anzeigt.</summary>
    public static byte DacToRgb(byte stored)
    {
        var dac = stored & 0x3F;
        return (byte)((dac << 2) | (dac >> 4));
    }

    /// <summary>Liest eine gespeicherte Palette (kb\21.vga, kb\247.vga oder eingebettet in ein Bild).</summary>
    public static Palette FromBytes(ReadOnlySpan<byte> data)
    {
        var palette = new Palette();
        var length = Math.Min(data.Length, palette._rgb.Length);
        for (var i = 0; i < length; i++)
            palette._rgb[i] = DacToRgb(data[i]);
        data[..Math.Min(data.Length, StoredLength)].CopyTo(palette._stored);
        return palette;
    }

    public static Palette Load(string path) => FromBytes(File.ReadAllBytes(path));

    /// <summary>Erzeugt eine Kopie, in der eine Farbe ersetzt ist (das Spiel färbt z. B. Index 30 zur Laufzeit um).</summary>
    public Palette WithColor(int index, byte r, byte g, byte b)
    {
        var copy = new Palette();
        _rgb.CopyTo(copy._rgb, 0);
        _stored.CopyTo(copy._stored, 0);
        copy._rgb[index * 3] = r;
        copy._rgb[index * 3 + 1] = g;
        copy._rgb[index * 3 + 2] = b;
        if (index * 3 + 2 < StoredLength)
        {
            copy._stored[index * 3] = (byte)(r >> 2);
            copy._stored[index * 3 + 1] = (byte)(g >> 2);
            copy._stored[index * 3 + 2] = (byte)(b >> 2);
        }
        return copy;
    }

    /// <summary>Graustufen-Palette als Notlösung, wenn keine Palette verfügbar ist.</summary>
    public static Palette Grayscale()
    {
        var palette = new Palette();
        for (var i = 0; i < ColorCount; i++)
        {
            palette._rgb[i * 3] = (byte)i;
            palette._rgb[i * 3 + 1] = (byte)i;
            palette._rgb[i * 3 + 2] = (byte)i;
            if (i * 3 + 2 < StoredLength)
                palette._stored.AsSpan(i * 3, 3).Fill((byte)(i >> 2));
        }
        return palette;
    }
}
