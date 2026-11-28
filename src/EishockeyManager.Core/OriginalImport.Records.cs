using System.Buffers.Binary;
using EishockeyManager.Formats;

namespace EishockeyManager.Core;

public static partial class OriginalImport
{
    /// <summary>Plätze der Rekord-, Serien- und Platzsätze im Original (Vereine 0–51, Länder 52–63).</summary>
    public const int TeamSlots = 64;

    /// <summary>Versätze im Block DGROUP 0xAB7B (5524 Byte; Aufteilung an der Blocklänge und den Fundstellen der Felder).</summary>
    public const int HeadToHeadOffset = 0x000, SeriesOffset = 0xA00, ManagerSeriesOffset = 0xF40, RecordOpponentOffset = 0xF94,
        RecordOffset = 0x1194;

    /// <summary>Kopf-an-Kopf-Liste des Originals: vier Managerplätze (0x280 Byte je Manager, 10 je Gegner).</summary>
    public const int HeadToHeadManagers = 4;

    /// <summary>Tabellensatz eines Satzplatzes (DGROUP 0x825A + 0x50 · Satz) im Block 0x8257: Versatz eines DGROUP-Felds.</summary>
    public static int TableField(int dgroup, int slot) => dgroup - 0x8257 + 0x50 * slot;
}

public sealed partial class GameState
{
    /// <summary>
    /// Sätze der Vereine und Länder 0–63 (Satzplatz → Vereinsindex nach Regel 1): laufende Serien DGROUP 0xB57B + 0x15 · Satz,
    /// Rekordserien der Manager 0xBABB + 0x15 · Manager, Rekorde 0xBD0F + 0x10 · Satz + 2 · Art mit Gegner 0xBB0F + 8 · Satz + Art,
    /// Kopf-an-Kopf-Liste 0xAB7B + 0x280 · Manager + 10 · Satz (alle im Block 0xAB7B); Strafminuten 0x82A1 + 0x50 · Satz (Wort) und
    /// Platzfelder 0x828B + 0x50 · Satz (Halbbytes, Feld i im Byte i / 2, gerades i unten; FILE03 gegen Tabelle 0x5CCB).
    /// [APPROX] Sätze, deren Verein im Remake keinen Index unter 64 hat, entfallen (nicht beobachtet).
    /// </summary>
    private void ApplyTeamTables(OriginalSaveGame save, int[] clubOf)
    {
        var block = save.Block("AB7B");
        var table = save.Block("8257");
        int? Team(int slot) => slot < clubOf.Length && clubOf[slot] < OriginalImport.TeamSlots ? clubOf[slot] : null;

        Array.Fill(_headToHead, HeadToHeadEmpty);
        Array.Clear(_teamRecords);
        Array.Clear(_teamRecordOpponents);
        for (var slot = 0; slot < OriginalImport.TeamSlots; slot++)
        {
            if (Team(slot) is not { } club)
                continue;
            var target = Clubs[club];
            for (var k = 0; k < Series.Count; k++)
                target.SeriesCurrent[k] = block[OriginalImport.SeriesOffset + Series.Count * slot + k];
            for (var kind = 0; kind < TeamRecords.Kinds; kind++)
            {
                var word = BinaryPrimitives.ReadUInt16LittleEndian(block.AsSpan(OriginalImport.RecordOffset + 0x10 * slot + 2 * kind));
                if (word == 0)
                    continue;
                var i = TeamRecords.Index(club, (TeamRecordKind)kind);
                _teamRecords[i] = word;
                _teamRecordOpponents[i] = Team(block[OriginalImport.RecordOpponentOffset + 8 * slot + kind]) ?? 0;
            }
            for (var m = 0; m < OriginalImport.HeadToHeadManagers; m++)
                for (var n = 0; n < HeadToHeadLength; n++)
                    _headToHead[(m * TeamRecords.Teams + club) * HeadToHeadLength + n] =
                        block[OriginalImport.HeadToHeadOffset + 0x280 * m + HeadToHeadLength * slot + n];

            target.LeaguePenaltyMinutes = BinaryPrimitives.ReadInt16LittleEndian(table.AsSpan(OriginalImport.TableField(0x82A1, slot)));
            for (var i = 0; i < RankSlotCount; i++)
            {
                var b = table[OriginalImport.TableField(0x828B, slot) + i / 2];
                target.StoreRankSlot(i, i % 2 == 0 ? b & 0x0F : b >> 4);
            }
            // A046 (kader-start Runde 14, Schema 43): Tabellenfeld der Zuschauerformel DGROUP 0x8288 + 0x50 · Satz (0 = Platz 1).
            target.StoreTableRank(table[OriginalImport.TableField(0x8288, slot)]);
        }
        for (var m = 0; m < Math.Min(Managers.Count, OriginalImport.HeadToHeadManagers); m++)
            for (var k = 0; k < Series.Count; k++)
                Managers[m].SeriesRecords[k] = block[OriginalImport.ManagerSeriesOffset + Series.Count * m + k];
    }
}
