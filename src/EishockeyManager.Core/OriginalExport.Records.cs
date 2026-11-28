using EishockeyManager.Formats;

namespace EishockeyManager.Core;

public sealed partial class GameState
{
    /// <summary>
    /// Sätze 0–63 im Block 0xAB7B: Serien +0xA00 + 21 · Satz, Rekorde +0x1194 + 0x10 · Satz + 2 · Art mit Gegner +0xF94 + 8 · Satz + Art,
    /// Kopf-an-Kopf +0x280 · m + 10 · Satz (Managerplätze 0–3), Rekordserien der Manager +0xF40 + 21 · m.
    /// [APPROX] Der übrige Block (Ewige Tabelle?) bleibt 0.
    /// </summary>
    private void WriteTeamTables(OriginalSaveWriter w, int[] slotOf)
    {
        var block = w.Block("AB7B");
        // [APPROX] Gegner eines Rekords ohne Wert: an jedem Stand ein fester Satz (FILE01: 4, FILE09: 2), vermutlich der Satz des
        // Vereins mit Datenbankindex 0.
        block.AsSpan(OriginalImport.RecordOpponentOffset, 8 * OriginalImport.TeamSlots).Fill((byte)slotOf[0]);
        foreach (var club in Clubs.Where(c => c.Index < OriginalImport.TeamSlots && slotOf[c.Index] < OriginalImport.TeamSlots))
        {
            var slot = slotOf[club.Index];
            for (var k = 0; k < Series.Count; k++)
                block[OriginalImport.SeriesOffset + Series.Count * slot + k] = OriginalExport.Byte(club.SeriesCurrent[k]);
            for (var kind = 0; kind < TeamRecords.Kinds; kind++)
            {
                var i = TeamRecords.Index(club.Index, (TeamRecordKind)kind);
                var word = _teamRecords[i];
                if (word == 0)
                    continue;
                OriginalExport.Write16(block, OriginalImport.RecordOffset + 0x10 * slot + 2 * kind, word);
                var opponent = _teamRecordOpponents[i];
                block[OriginalImport.RecordOpponentOffset + 8 * slot + kind] =
                    (byte)(opponent >= 0 && opponent < slotOf.Length ? slotOf[opponent] : 0);
            }
            for (var m = 0; m < OriginalImport.HeadToHeadManagers; m++)
                for (var n = 0; n < HeadToHeadLength; n++)
                    block[OriginalImport.HeadToHeadOffset + 0x280 * m + HeadToHeadLength * slot + n] =
                        (byte)_headToHead[(m * TeamRecords.Teams + club.Index) * HeadToHeadLength + n];
        }
        for (var m = 0; m < Math.Min(Managers.Count, OriginalImport.HeadToHeadManagers); m++)
            for (var k = 0; k < Series.Count; k++)
                block[OriginalImport.ManagerSeriesOffset + Series.Count * m + k] = OriginalExport.Byte(Managers[m].SeriesRecords[k]);
    }
}
