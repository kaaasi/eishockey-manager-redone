using System.Text;

namespace EishockeyManager.Formats;

/// <summary>
/// Zeichensatz des Spiels. Die Bitmapfonts kennen nur 0x20–0x7F; die deutschen Umlaute liegen auf
/// <c>[ \ ] { | } ~</c> = Ä Ö Ü ä ö ü ß. In der Datenbank kommen vereinzelt auch CP437-Umlaute vor.
/// </summary>
public static class GameText
{
    private static readonly Dictionary<byte, char> SpecialToChar = new()
    {
        [(byte)'['] = 'Ä', [(byte)'\\'] = 'Ö', [(byte)']'] = 'Ü',
        [(byte)'{'] = 'ä', [(byte)'|'] = 'ö', [(byte)'}'] = 'ü', [(byte)'~'] = 'ß',
        // CP437
        [0x8E] = 'Ä', [0x99] = 'Ö', [0x9A] = 'Ü', [0x84] = 'ä', [0x94] = 'ö', [0x81] = 'ü', [0xE1] = 'ß',
        [0x82] = 'é', [0x8A] = 'è', [0x85] = 'à', [0x87] = 'ç', [0x83] = 'â', [0x88] = 'ê',
    };

    private static readonly Dictionary<char, byte> CharToSpecial = new()
    {
        ['Ä'] = (byte)'[', ['Ö'] = (byte)'\\', ['Ü'] = (byte)']',
        ['ä'] = (byte)'{', ['ö'] = (byte)'|', ['ü'] = (byte)'}', ['ß'] = (byte)'~',
    };

    /// <summary>Dekodiert einen nullterminierten Text aus den Spieldaten.</summary>
    public static string Decode(ReadOnlySpan<byte> bytes)
    {
        var end = bytes.IndexOf((byte)0);
        if (end >= 0)
            bytes = bytes[..end];

        var sb = new StringBuilder(bytes.Length);
        foreach (var b in bytes)
        {
            if (SpecialToChar.TryGetValue(b, out var c))
                sb.Append(c);
            else if (b is >= 0x20 and < 0x7F)
                sb.Append((char)b);
            else
                sb.Append('?');
        }
        return sb.ToString();
    }

    /// <summary>Kodiert Text für die Originalfonts; nicht darstellbare Zeichen werden ersetzt (é → e).</summary>
    public static byte[] Encode(string text)
    {
        var result = new List<byte>(text.Length);
        foreach (var ch in text)
        {
            if (CharToSpecial.TryGetValue(ch, out var special))
            {
                result.Add(special);
                continue;
            }
            if (IsPlainAscii(ch))
            {
                result.Add((byte)ch);
                continue;
            }
            var plain = ch.ToString().Normalize(NormalizationForm.FormD)[0];
            result.Add(IsPlainAscii(plain) ? (byte)plain : (byte)'?');
        }
        return result.ToArray();
    }

    /// <summary>ASCII-Zeichen, die im Spielzeichensatz unverändert darstellbar sind.</summary>
    private static bool IsPlainAscii(char ch) =>
        ch is >= ' ' and < '\u007F' && ch is not ('[' or '\\' or ']' or '{' or '|' or '}' or '~');
}
