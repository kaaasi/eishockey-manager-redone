using EishockeyManager.Formats;

namespace EishockeyManager.Core;

public static partial class OriginalImport
{
    /// <summary>[ORIGINAL] Nachrichtenspeicher DGROUP 0xD294: 24 Sätze zu 0x71 Byte (re-rest.md, Abschnitt H).</summary>
    public const int MessageRecordLength = 0x71;

    /// <summary>
    /// Zeilen des Nachrichtentexts ab Satz +3: nullterminiert, Zeilen durch „^“ getrennt (FILE20/FILE48: Datum, Kopf, Spieler).
    /// </summary>
    public static string[] MessageLines(ReadOnlySpan<byte> record)
    {
        var text = record[3..];
        var end = text.IndexOf((byte)0);
        return GameText.Decode(end < 0 ? text : text[..end]).Split('^', StringSplitOptions.RemoveEmptyEntries);
    }
}

public sealed partial class GameState
{
    /// <summary>
    /// Nachrichten (Teil 4b). [ORIGINAL] Satz DGROUP 0xD294 + 0x71 · k (k &lt; 24): +0 Bits 0–2 Manager, Bit 3 = ohne Spieler,
    /// oberes Halbbyte Restlaufzeit, 0 = frei; +1 Sortierschlüssel (Tageszähler, höchstens 255); +2 Poolspieler; ab +3 der Text
    /// (<see cref="OriginalImport.MessageLines"/>). FILE20: zwei Verletzungsmeldungen an Manager 0, Tageszähler 217 und 221.
    /// [APPROX] Tag = Schlüssel − 1 (Regel 5; ab Tageszähler 255 abgeschnitten), Saison = laufende Saison; der Vorrang (oberes Byte
    /// des Schlüssels) ist nicht belegt. Sätze eines Managers, den es nicht gibt, entfallen. Die Nachrichten des Nachspielens
    /// entfallen.
    /// </summary>
    private void ApplyMessages(OriginalSaveGame save, Dictionary<int, Player> pool)
    {
        foreach (var manager in Managers)
            Finance(manager).Inbox.Clear();
        var block = save.Block("D294");
        for (var k = 0; k < MessageCapacity; k++)
        {
            var r = block.AsSpan(k * OriginalImport.MessageRecordLength, OriginalImport.MessageRecordLength);
            var m = r[0] & 7;
            if (r[0] == 0 || m >= Managers.Count)
                continue;
            var message = new Message
            {
                Kind = MessageKind.Info,
                Day = Math.Max(0, r[1] - 1),
                Lines = OriginalImport.MessageLines(r),
                Season = Season,
                DaysLeft = r[0] >> 4,
                Player = (r[0] & 8) == 0 ? pool.GetValueOrDefault(r[2]) : null,
            };
            Finance(Managers[m]).Inbox.Add(message);
        }
        foreach (var manager in Managers)
            SortInbox(Finance(manager).Inbox);
    }
}
