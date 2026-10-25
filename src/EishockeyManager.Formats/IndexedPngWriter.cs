using System.Buffers.Binary;
using System.IO.Compression;

namespace EishockeyManager.Formats;

/// <summary>Minimaler PNG-Schreiber für 8-Bit-Palettenbilder (ohne externe Abhängigkeiten).</summary>
public static class IndexedPngWriter
{
    private static readonly byte[] Signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly uint[] CrcTable = BuildCrcTable();

    public static void Save(string path, VgaImage image, Palette fallback)
    {
        using var stream = File.Create(path);
        Write(stream, image, fallback);
    }

    public static void Write(Stream output, VgaImage image, Palette fallback)
    {
        var palette = image.Palette ?? fallback;
        output.Write(Signature);

        var ihdr = new byte[13];
        BinaryPrimitives.WriteUInt32BigEndian(ihdr, (uint)image.Width);
        BinaryPrimitives.WriteUInt32BigEndian(ihdr.AsSpan(4), (uint)image.Height);
        ihdr[8] = 8;  // Bittiefe
        ihdr[9] = 3;  // Farbtyp: Palette
        ihdr[10] = 0; // Kompression
        ihdr[11] = 0; // Filter
        ihdr[12] = 0; // kein Interlacing
        WriteChunk(output, "IHDR", ihdr);

        WriteChunk(output, "PLTE", palette.Rgb.ToArray());

        using var raw = new MemoryStream();
        using (var zlib = new ZLibStream(raw, CompressionLevel.Optimal, leaveOpen: true))
        {
            for (var y = 0; y < image.Height; y++)
            {
                zlib.WriteByte(0); // Filter "None"
                zlib.Write(image.Pixels, y * image.Width, image.Width);
            }
        }
        WriteChunk(output, "IDAT", raw.ToArray());
        WriteChunk(output, "IEND", []);
    }

    private static void WriteChunk(Stream output, string type, byte[] data)
    {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(buffer, (uint)data.Length);
        output.Write(buffer);

        var typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
        output.Write(typeBytes);
        output.Write(data);

        var crc = 0xFFFFFFFFu;
        crc = UpdateCrc(crc, typeBytes);
        crc = UpdateCrc(crc, data);
        BinaryPrimitives.WriteUInt32BigEndian(buffer, crc ^ 0xFFFFFFFFu);
        output.Write(buffer);
    }

    private static uint UpdateCrc(uint crc, ReadOnlySpan<byte> data)
    {
        foreach (var b in data)
            crc = CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
        return crc;
    }

    private static uint[] BuildCrcTable()
    {
        var table = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            var c = n;
            for (var k = 0; k < 8; k++)
                c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
            table[n] = c;
        }
        return table;
    }
}
