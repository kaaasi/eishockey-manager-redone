using System.Buffers.Binary;

namespace EishockeyManager.Formats;

/// <summary>
/// Bild im .vga-Format der DOS-Version (Verzeichnis kb\).
///
/// Aufbau (Little Endian):
///   UInt16  Breite
///   UInt16  Höhe
///   UInt16  Länge der komprimierten Daten (inkl. Endmarker)
///   ...     PackBits-Daten; bei Höhe &gt; 200 zwei Ströme (Zeilen 0..199, dann der Rest)
///   [765]   optional: eigene Palette (255 Farben, 8-Bit-RGB)
/// </summary>
public sealed class VgaImage
{
    public const int HeaderLength = 6;

    /// <summary>Der Original-Loader teilt Bilder mit mehr als 200 Zeilen in zwei Ströme.</summary>
    public const int SplitHeight = 200;

    public VgaImage(int width, int height, byte[] pixels, Palette? palette = null)
    {
        if (pixels.Length != width * height)
            throw new ArgumentException("Pixelanzahl passt nicht zu Breite × Höhe.", nameof(pixels));
        Width = width;
        Height = height;
        Pixels = pixels;
        Palette = palette;
    }

    public int Width { get; }
    public int Height { get; }

    /// <summary>Palettenindizes, zeilenweise von links oben.</summary>
    public byte[] Pixels { get; }

    /// <summary>Im Bild eingebettete Palette, sonst null (dann gilt die Standardpalette kb\21.vga).</summary>
    public Palette? Palette { get; }

    public byte this[int x, int y] => Pixels[y * Width + x];

    public static VgaImage Load(string path) => Parse(File.ReadAllBytes(path));

    public static bool TryParse(ReadOnlySpan<byte> data, out VgaImage? image)
    {
        try
        {
            image = Parse(data);
            return true;
        }
        catch (InvalidDataException)
        {
            image = null;
            return false;
        }
    }

    public static VgaImage Parse(ReadOnlySpan<byte> data)
    {
        if (data.Length < HeaderLength + 1)
            throw new InvalidDataException("Datei ist zu kurz für ein VGA-Bild.");

        var width = BinaryPrimitives.ReadUInt16LittleEndian(data);
        var height = BinaryPrimitives.ReadUInt16LittleEndian(data[2..]);
        var packedLength = BinaryPrimitives.ReadUInt16LittleEndian(data[4..]);

        if (width == 0 || height == 0 || width > 640 || height > 480)
            throw new InvalidDataException($"Unplausible Bildgröße {width}×{height}.");
        if (HeaderLength + packedLength > data.Length)
            throw new InvalidDataException("Angegebene Datenlänge überschreitet die Dateigröße.");

        var pixels = new byte[width * height];
        var position = HeaderLength;

        if (height > SplitHeight)
        {
            var firstPart = width * SplitHeight;
            var written = PackBits.Decode(data, ref position, pixels.AsSpan(0, firstPart));
            written += PackBits.Decode(data, ref position, pixels.AsSpan(written));
            if (written != pixels.Length)
                throw new InvalidDataException("Bilddaten unvollständig.");
        }
        else
        {
            var written = PackBits.Decode(data, ref position, pixels);
            if (written != pixels.Length)
                throw new InvalidDataException("Bilddaten unvollständig.");
        }

        if (position != HeaderLength + packedLength)
            throw new InvalidDataException("Datenlänge im Header stimmt nicht mit dem Datenstrom überein.");

        Palette? palette = null;
        if (data.Length >= position + Palette.StoredLength)
            palette = Palette.FromBytes(data.Slice(position, Palette.StoredLength));

        return new VgaImage(width, height, pixels, palette);
    }

    /// <summary>Schreibt das Bild wieder im Originalformat (ohne Aufteilung bei Höhe &gt; 200 nur für kleine Bilder sinnvoll).</summary>
    public byte[] ToBytes()
    {
        using var stream = new MemoryStream();
        var header = new byte[HeaderLength];
        BinaryPrimitives.WriteUInt16LittleEndian(header, (ushort)Width);
        BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(2), (ushort)Height);

        byte[] packed;
        if (Height > SplitHeight)
        {
            var split = Width * SplitHeight;
            var a = PackBits.Encode(Pixels.AsSpan(0, split));
            var b = PackBits.Encode(Pixels.AsSpan(split));
            packed = [.. a, .. b];
        }
        else
        {
            packed = PackBits.Encode(Pixels);
        }

        BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(4), checked((ushort)packed.Length));
        stream.Write(header);
        stream.Write(packed);
        if (Palette is not null)
            stream.Write(Palette.StoredBytes);
        return stream.ToArray();
    }

    /// <summary>Wandelt das Bild in 32-Bit-RGBA um.</summary>
    public byte[] ToRgba(Palette fallback, int? transparentIndex = null)
    {
        var palette = Palette ?? fallback;
        var rgba = new byte[Pixels.Length * 4];
        for (var i = 0; i < Pixels.Length; i++)
        {
            var index = Pixels[i];
            var (r, g, b) = palette[index];
            rgba[i * 4] = r;
            rgba[i * 4 + 1] = g;
            rgba[i * 4 + 2] = b;
            rgba[i * 4 + 3] = transparentIndex == index ? (byte)0 : (byte)255;
        }
        return rgba;
    }
}
