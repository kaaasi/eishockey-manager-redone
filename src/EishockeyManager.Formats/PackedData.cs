using System.Buffers.Binary;

namespace EishockeyManager.Formats;

/// <summary>
/// Komprimierte Rohdaten ohne Bildkopf (kb\231–235):
///   UInt16 LE  Länge der folgenden Daten
///   ...        ein oder mehrere PackBits-Ströme, jeweils mit 0x80 abgeschlossen
/// </summary>
public static class PackedData
{
    public static byte[] Decode(ReadOnlySpan<byte> data, int maxSize = 128 * 1024)
    {
        if (data.Length < 3)
            throw new InvalidDataException("Datei ist zu kurz.");

        var length = BinaryPrimitives.ReadUInt16LittleEndian(data);
        var end = 2 + length;
        if (end > data.Length)
            throw new InvalidDataException("Angegebene Länge überschreitet die Dateigröße.");

        var buffer = new byte[maxSize];
        var position = 2;
        var written = 0;
        while (position < end)
            written += PackBits.Decode(data[..end], ref position, buffer.AsSpan(written));

        return buffer.AsSpan(0, written).ToArray();
    }

    public static byte[] Load(string path) => Decode(File.ReadAllBytes(path));
}
