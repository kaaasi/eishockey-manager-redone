using System.Text;

namespace EishockeyManager.Formats;

/// <summary>Inhalt des unverschlüsselten Kopfs eines Original-Spielstands, den die Spielstand-Info des Dialogs zeigt.</summary>
/// <param name="Year">Kopf +0x0A (4 Byte): Kalenderjahr des Spielstands.</param>
/// <param name="MatchDay">Kopf +0x0F: nächster Spieltag (Kopfbyte wie 0x7A9 im Datenteil).</param>
/// <param name="Managers">Kopf +0x10: bis zu 4 Einträge zu je 0x28 Byte, <c>Managerzahl</c> davon belegt.</param>
public sealed record OriginalSaveHeaderInfo(int Year, int MatchDay, IReadOnlyList<OriginalSaveManagerHeader> Managers);

/// <summary>
/// Kopf-Leser der Spielstand-Info (Spur hok-import, Teil 5; A339). Liest nur die ersten 0xB0 Byte und prüft weder
/// Länge noch Prüfsummen des Datenteils – wie das Original.
/// </summary>
/// <remarks>
/// [ORIGINAL] em.exe Datei 0x5B661–0x5B736: Datei öffnen, 10 Byte Kennung mit DGROUP 0x55DF („KRON_0.99“ + 0) vergleichen,
/// dann 4 Byte Jahr, 1 Byte Managerzahl, 1 Byte Spieltag, 0xA0 Byte = 4 Managereinträge zu 0x28. Der Kopf steht
/// unverschlüsselt vor dem Datenteil (Abgleich FILE46: Jahr 1994, 1 Manager, Spieltag 1, „Joerg“, „KÖLNER EC“, Liga 0).
/// </remarks>
public static class OriginalSaveHeader
{
    /// <summary>Länge des gelesenen Kopfs: 0x10 + 4 × 0x28.</summary>
    public const int Length = 0xB0;

    /// <summary>Liest den Kopf; null, wenn die Datei zu kurz ist oder die Kennung nicht stimmt („Kein Eishockey Manager Spielstand“).</summary>
    public static OriginalSaveHeaderInfo? TryRead(ReadOnlySpan<byte> file)
    {
        if (file.Length < Length)
            return null;
        var magic = Encoding.ASCII.GetBytes(OriginalSaveGame.Magic);
        if (!file[..magic.Length].SequenceEqual(magic) || file[magic.Length] != 0)
            return null;

        var count = Math.Min((int)file[0x0E], 4);
        var managers = new List<OriginalSaveManagerHeader>();
        for (var i = 0; i < count; i++)
        {
            var e = file.Slice(0x10 + 0x28 * i, 0x28);
            managers.Add(new OriginalSaveManagerHeader(e[0], e[1] != 0, CString(e.Slice(2, 0x13)), CString(e.Slice(0x15, 0x13))));
        }
        return new OriginalSaveHeaderInfo(BitConverter.ToInt32(file.Slice(0x0A, 4)), file[0x0F], managers);
    }

    private static string CString(ReadOnlySpan<byte> bytes)
    {
        var end = bytes.IndexOf((byte)0);
        return GameText.Decode(end < 0 ? bytes : bytes[..end]);
    }
}
