using System.Buffers.Binary;

namespace EishockeyManager.Formats;

public enum FontKind
{
    /// <summary>6 Pixel hoch.</summary>
    Mini = 1,

    /// <summary>8 Pixel hoch.</summary>
    Flow = 2,

    /// <summary>20 Pixel hoch.</summary>
    NewBold = 3,
}

/// <summary>
/// Bitmapfonts aus em.exe. Sie liegen im Grafiksegment 0BBF; die Fonttabelle bei Segmentoffset 0x06CD
/// enthält vier Zeiger (normal, mini, flow, newbold – "normal" nutzt den VGA-BIOS-Font).
///
/// Fontkopf: UInt16 Namenszeiger, UInt16 Höhe, UInt16 Grundlinie, 96 × UInt16 Glyphzeiger (0x20–0x7F).
/// Glyph: Byte Breite, dann Höhe × ⌈Breite/8⌉ Bytes (1 Bit pro Pixel, MSB zuerst).
/// Die Breite enthält den Zeichenabstand bereits (letzte Spalte leer).
/// </summary>
public sealed class BitmapFont
{
    public const int GraphicsSegment = 0x0BBF;
    public const int FontTableOffset = 0x06CD;
    public const int FirstChar = 0x20;
    public const int CharCount = 96;

    private readonly Glyph[] _glyphs;

    private BitmapFont(string name, int height, int baseline, Glyph[] glyphs)
    {
        Name = name;
        Height = height;
        Baseline = baseline;
        _glyphs = glyphs;
    }

    public string Name { get; }
    public int Height { get; }
    public int Baseline { get; }

    public sealed record Glyph(int Width, bool[] Pixels)
    {
        public bool this[int x, int y] => Pixels[y * Width + x];
    }

    /// <summary>Glyph für ein Byte im Spielzeichensatz; unbekannte Zeichen ergeben das Leerzeichen.</summary>
    public Glyph this[byte code] =>
        code >= FirstChar && code < FirstChar + CharCount ? _glyphs[code - FirstChar] : _glyphs[0];

    /// <summary>Breite eines Textes in Pixeln (ohne zusätzlichen Zeichenabstand).</summary>
    public int Measure(string text) => GameText.Encode(text).Sum(b => this[b].Width);

    /// <summary>Lädt einen Font direkt aus em.exe.</summary>
    public static BitmapFont LoadFromExe(string exePath, FontKind kind)
    {
        var exe = File.ReadAllBytes(exePath);
        if (exe.Length < 0x20 || exe[0] != 'M' || exe[1] != 'Z')
            throw new InvalidDataException("Keine gültige DOS-EXE.");

        var headerParagraphs = BinaryPrimitives.ReadUInt16LittleEndian(exe.AsSpan(8));
        var segmentStart = headerParagraphs * 16 + GraphicsSegment * 16;
        var segment = exe.AsSpan(segmentStart, Math.Min(0x10000, exe.Length - segmentStart));

        var headerOffset = BinaryPrimitives.ReadUInt16LittleEndian(segment[(FontTableOffset + (int)kind * 2)..]);
        return Parse(segment, headerOffset);
    }

    /// <summary>Liest einen Font aus einem Speichersegment ab dem angegebenen Kopf-Offset.</summary>
    public static BitmapFont Parse(ReadOnlySpan<byte> segment, int headerOffset)
    {
        var namePointer = BinaryPrimitives.ReadUInt16LittleEndian(segment[headerOffset..]);
        var height = BinaryPrimitives.ReadUInt16LittleEndian(segment[(headerOffset + 2)..]);
        var baseline = BinaryPrimitives.ReadUInt16LittleEndian(segment[(headerOffset + 4)..]);
        if (height is 0 or > 64)
            throw new InvalidDataException($"Unplausible Fonthöhe {height}.");

        var nameBytes = segment[namePointer..];
        var name = GameText.Decode(nameBytes[..Math.Min(32, nameBytes.Length)]);

        var glyphs = new Glyph[CharCount];
        for (var c = 0; c < CharCount; c++)
        {
            var pointer = BinaryPrimitives.ReadUInt16LittleEndian(segment[(headerOffset + 6 + c * 2)..]);
            var width = segment[pointer];
            var bytesPerRow = (width + 7) / 8;
            var pixels = new bool[width * height];
            for (var y = 0; y < height; y++)
            {
                var row = segment.Slice(pointer + 1 + y * bytesPerRow, bytesPerRow);
                for (var x = 0; x < width; x++)
                    pixels[y * width + x] = (row[x / 8] & (0x80 >> (x % 8))) != 0;
            }
            glyphs[c] = new Glyph(width, pixels);
        }

        return new BitmapFont(name, height, baseline, glyphs);
    }

    /// <summary>Zeichnet Text in ein Palettenbild (Farbe <paramref name="color"/>, Hintergrund unverändert).</summary>
    public int Draw(byte[] pixels, int imageWidth, int x, int y, string text, byte color)
    {
        var imageHeight = pixels.Length / imageWidth;
        foreach (var code in GameText.Encode(text))
        {
            var glyph = this[code];
            for (var gy = 0; gy < Height; gy++)
            {
                var py = y + gy;
                if (py < 0 || py >= imageHeight)
                    continue;
                for (var gx = 0; gx < glyph.Width; gx++)
                {
                    var px = x + gx;
                    if (px >= 0 && px < imageWidth && glyph[gx, gy])
                        pixels[py * imageWidth + px] = color;
                }
            }
            x += glyph.Width;
        }
        return x;
    }
}
