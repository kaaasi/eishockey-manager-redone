using EishockeyManager.Formats;

namespace EishockeyManager.Core;

public sealed partial class GameState
{
    /// <summary>
    /// Umkehrung von Teil 4a des Imports (<c>OriginalImport.Rest.cs</c>): Börse, Aktienbesitz, Werbung, Trainingsplan, Taktik und
    /// „automatisch aufstellen“, Serien, Rekorde, Kopf-an-Kopf-Liste. Fundstellen wie dort.
    /// </summary>
    private void WriteOriginalRest(OriginalSaveWriter w, int[] slotOf)
    {
        WriteStocks(w);
        for (var m = 0; m < Managers.Count; m++)
        {
            var manager = Managers[m];
            var f = Finance(manager);
            var holdings = w.Block("D214");
            for (var i = 0; i < f.Holdings.Length; i++)
            {
                OriginalExport.Write32(holdings, 32 * m + 8 * i, OriginalExport.Thirds(f.Holdings[i].PurchaseCost));
                OriginalExport.Write32(holdings, 32 * m + 8 * i + 4, f.Holdings[i].Count);
            }
            WriteSponsoring(w, m, f.Sponsoring);
            var units = w.Block("A8A1");
            for (var day = 0; day < TrainingPlan.Days; day++)
                for (var unit = 0; unit < TrainingPlan.UnitsPerDay; unit++)
                    units[28 * m + day * TrainingPlan.UnitsPerDay + unit] = (byte)manager.Training[day, unit];
            if (m == 0)
                OriginalExport.Write32(w.Block("02BC"), 0, manager.Training.PriceIndex);
            if (WorldOnly)
                w.Block("0375")[2 * m] = w.Block("0375")[2 * m + 1] = 1; // [APPROX] FILE11, FILE18–FILE21: 1
            else
            {
                var club = Clubs[manager.ClubIndex];
                WriteTacticValues(w, club.Tactics, m);
                // [ORIGINAL] 0x375 + 2 · m aktuell, 0x376 + 2 · m gesichert (gleich außerhalb der Spiele des Tages).
                w.Block("0375")[2 * m] = w.Block("0375")[2 * m + 1] = (byte)(club.AutoLineupEnabled ? 1 : 0);
            }
        }
        if (Managers.Count > 0)
            OriginalExport.Write32(w.Block("02BC"), 0, Managers[0].Training.PriceIndex);
        else
            OriginalExport.Write32(w.Block("02BC"), 0, 100);
        WriteTeamTables(w, slotOf);
    }

    /// <summary>Kursverlauf 0xD0F4 + 0x48 · Aktie (36 Wörter, Drittel-DM), Basiswert 0x1FD, Restdauer 0xA891, Tendenz 0xA895.</summary>
    private void WriteStocks(OriginalSaveWriter w)
    {
        var history = w.Block("D0F4");
        for (var i = 0; i < Math.Min(Stocks.Stocks.Count, 4); i++)
        {
            var stock = Stocks.Stocks[i];
            var values = stock.History.TakeLast(Stock.HistoryLength).ToList();
            if (values.Count == 0)
                values.Add(stock.Price);
            // Vorn mit dem ältesten Wert auffüllen; das letzte Wort ist der Kurs.
            while (values.Count < Stock.HistoryLength)
                values.Insert(0, values[0]);
            values[^1] = stock.Price;
            for (var k = 0; k < Stock.HistoryLength; k++)
                OriginalExport.Write16(history, 0x48 * i + 2 * k, (int)Math.Clamp(values[k] * 3L, 0, ushort.MaxValue));
            OriginalExport.Write32(w.Block("01FD"), 4 * i, stock.Base);
            w.Block("A891")[i] = OriginalExport.Byte(stock.Countdown);
            w.Block("A895")[i] = OriginalExport.Byte(stock.Direction);
        }
    }

    /// <summary>
    /// Werbeverträge 0xC15B + 0x6E · m (11 Flächen zu 10 Byte) und Angebote 0xC313 + 0x1F4 · m + 0x64 · Art + 10 · Partner.
    /// [APPROX] Logo +7 = Partner (FILE05); Option +6 ohne Bit 0x80 („Kündbar.“ führt das Remake nicht).
    /// </summary>
    private static void WriteSponsoring(OriginalSaveWriter w, int m, Sponsoring sponsoring)
    {
        var contracts = w.Block("C15B");
        foreach (var c in sponsoring.Contracts)
        {
            var areas = c.Kind == AdSlotKind.Bande ? c.Boards : [AreaOf(c.Kind)];
            foreach (var area in areas)
            {
                var at = 0x6E * m + 10 * area;
                OriginalExport.Write32(contracts, at, OriginalExport.Thirds(c.MonthlyAmount));
                contracts[at + 4] = OriginalExport.Byte(c.Sponsor);
                contracts[at + 5] = OriginalExport.Byte(c.MonthsLeft);
                contracts[at + 6] = OriginalExport.Byte(c.OptionMonths & 0x7F);
                contracts[at + 7] = OriginalExport.Byte(c.Sponsor);
                contracts[at + 8] = OriginalExport.Byte(c.SignedRating);
            }
        }
        var offers = w.Block("C313");
        for (var k = 0; k < 5; k++)
        {
            for (var p = 0; p < 10; p++)
                offers[0x1F4 * m + 0x64 * k + 10 * p + 7] = (byte)p; // [ORIGINAL] Logo = Partner, auch ohne Angebot (FILE01)
            if (!sponsoring.Offers.TryGetValue(OriginalImport.OfferKind(k), out var byPartner))
                continue;
            foreach (var (p, o) in byPartner)
            {
                if (p is < 0 or >= 10)
                    continue;
                var at = 0x1F4 * m + 0x64 * k + 10 * p;
                OriginalExport.Write32(offers, at, OriginalExport.Thirds(o.MonthlyAmount));
                offers[at + 4] = OriginalExport.Byte(o.Boards);
                offers[at + 5] = OriginalExport.Byte(o.Months);
                offers[at + 6] = OriginalExport.Byte(o.OptionMonths);
                offers[at + 7] = (byte)p;
                offers[at + 8] = OriginalExport.Byte(o.Rating);
            }
        }
    }

    /// <summary>Fläche des Originals je Art (Umkehrung von <see cref="OriginalImport.AreaKind"/> ohne Banden).</summary>
    private static int AreaOf(AdSlotKind kind) => kind switch
    {
        AdSlotKind.Trikot => 7,
        AdSlotKind.Fernsehen => 8,
        AdSlotKind.Eisflaeche => 9,
        _ => 10,
    };

    /// <summary>Modi 0xA6FB/0xA707/0xA701 + Satz, Werte 0xCAE9 + 0x30 · Satz, Minuten 0xCC43 + 8 · Satz (Umkehrung von <c>ApplyTacticValues</c>).</summary>
    private static void WriteTacticValues(OriginalSaveWriter w, Tactics tactics, int record)
    {
        w.Block("A6FB")[record] = (byte)tactics.EffortMode;
        w.Block("A707")[record] = (byte)tactics.AttackMode;
        w.Block("A701")[record] = (byte)tactics.LineChange;
        var values = w.Block("CAE9");
        for (var b = 0; b < Tactics.Blocks; b++)
            for (var i = 0; i < Tactics.Situations; i++)
            {
                values[0x30 * record + 6 * b + i] = OriginalExport.Byte(tactics.Effort(b, (ScoreSituation)i));
                values[0x30 * record + 0x18 + 6 * b + i] = OriginalExport.Byte(tactics.Attack(b, (ScoreSituation)i));
            }
        var minutes = w.Block("CC43");
        for (var part = 0; part < 2; part++)
            for (var line = 0; line < Lineup.Blocks; line++)
                minutes[8 * record + 4 * part + line] = OriginalExport.Byte(tactics.Minutes(line, part == 1));
    }

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
