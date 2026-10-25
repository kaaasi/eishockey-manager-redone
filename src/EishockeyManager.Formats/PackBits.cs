namespace EishockeyManager.Formats;

/// <summary>
/// RLE-Variante, die der Original-Loader (em.exe, Segment 0BBF:45C6) verwendet.
/// Steuerbyte c:
///   0x00..0x7F  → die nächsten c+1 Bytes werden unverändert kopiert
///   0x80        → Ende des Datenstroms
///   0x81..0xFF  → das nächste Byte wird (c &amp; 0x7F)+1 mal wiederholt
/// </summary>
public static class PackBits
{
    public const byte EndMarker = 0x80;

    /// <summary>
    /// Dekodiert einen Datenstrom ab <paramref name="position"/> bis zum Endmarker.
    /// </summary>
    /// <returns>Anzahl der in <paramref name="destination"/> geschriebenen Bytes.</returns>
    public static int Decode(ReadOnlySpan<byte> source, ref int position, Span<byte> destination)
    {
        var written = 0;
        while (true)
        {
            if (position >= source.Length)
                throw new InvalidDataException("PackBits-Datenstrom endet ohne Endmarker.");

            var control = source[position++];
            if (control == EndMarker)
                return written;

            if (control < 0x80)
            {
                var count = control + 1;
                if (position + count > source.Length)
                    throw new InvalidDataException("PackBits-Literal läuft über das Dateiende.");
                if (written + count > destination.Length)
                    throw new InvalidDataException("PackBits-Daten sind größer als das Zielbild.");
                source.Slice(position, count).CopyTo(destination[written..]);
                position += count;
                written += count;
            }
            else
            {
                var count = (control & 0x7F) + 1;
                if (position >= source.Length)
                    throw new InvalidDataException("PackBits-Wiederholung ohne Wert am Dateiende.");
                if (written + count > destination.Length)
                    throw new InvalidDataException("PackBits-Daten sind größer als das Zielbild.");
                destination.Slice(written, count).Fill(source[position++]);
                written += count;
            }
        }
    }

    /// <summary>Kodiert Daten im selben Format (für Tests und spätere eigene Grafiken).</summary>
    public static byte[] Encode(ReadOnlySpan<byte> data)
    {
        var output = new List<byte>(data.Length + data.Length / 64 + 2);
        var i = 0;
        while (i < data.Length)
        {
            var run = 1;
            while (i + run < data.Length && run < 128 && data[i + run] == data[i])
                run++;

            if (run >= 2)
            {
                output.Add((byte)(0x80 | (run - 1)));
                output.Add(data[i]);
                i += run;
                continue;
            }

            var start = i;
            var length = 0;
            while (i < data.Length && length < 128)
            {
                if (i + 1 < data.Length && data[i] == data[i + 1])
                    break;
                i++;
                length++;
            }
            output.Add((byte)(length - 1));
            for (var k = 0; k < length; k++)
                output.Add(data[start + k]);
        }
        output.Add(EndMarker);
        return output.ToArray();
    }
}
