using System.Buffers.Binary;
using EishockeyManager.Formats;

namespace EishockeyManager.Core;

/// <summary>
/// Import eines Original-Spielstands, Teil 4a (Spur hok-import, Runde 14, E14-4 = A): Bereiche, die Teil 2 noch aus dem
/// Nachspielen übernahm. Hier: Managerdarlehen und Sparbuch, Nationalkonto, Börse, Werbung, Trainingsplan, Taktik und
/// „automatisch aufstellen“, Serien, Rekorde und Kopf-an-Kopf-Liste, Strafminuten und Platzfelder, Über-/Unterzahl,
/// Talentsuche, Kennzeichen „neue Nachricht“. Fundstellen je Feld in der Feldkarte (<c>docs/formate/spielstand-original.md</c>).
/// </summary>
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

    /// <summary>Werbefläche des Originals (0–6 Banden, 7 Trikot, 8 Fernsehen, 9/10 Eis) → Art des Remakes.</summary>
    public static AdSlotKind AreaKind(int area) => area switch
    {
        < Sponsoring.BoardCount => AdSlotKind.Bande,
        7 => AdSlotKind.Trikot,
        8 => AdSlotKind.Fernsehen,
        9 => AdSlotKind.Eisflaeche,
        _ => AdSlotKind.Eisflaeche2,
    };

    /// <summary>Art k der Angebotstabelle 0xC313 (0 Bande, 1 Trikot, 2 Fernsehen, 3/4 Eis) → Art des Remakes.</summary>
    public static AdSlotKind OfferKind(int k) => k switch
    {
        0 => AdSlotKind.Bande,
        1 => AdSlotKind.Trikot,
        2 => AdSlotKind.Fernsehen,
        3 => AdSlotKind.Eisflaeche,
        _ => AdSlotKind.Eisflaeche2,
    };
}

public sealed partial class Manager
{
    /// <summary>Kennzeichen „neue Nachricht“ aus dem Import (DGROUP 0x5A50 + Manager).</summary>
    internal void SetNewMessage(bool value) => _newMessage = value;
}

public sealed partial class GameState
{
    private void ApplyOriginalRest(OriginalSaveGame save, int[] clubOf)
    {
        var records = save.Block("965B");
        ManagerLoans.Clear();
        for (var m = 0; m < Managers.Count; m++)
        {
            var record = records.AsSpan(m * OriginalSaveGame.ManagerRecordLength, OriginalSaveGame.ManagerRecordLength);
            var manager = Managers[m];
            ApplyLoans(m, record);
            ApplyHoldings(save, m);
            ApplySponsoring(save, m);
            ApplyTraining(save, m);
            if (!WorldOnly)
                ApplyTactics(save, Clubs[manager.ClubIndex], m);
            // [ORIGINAL] Über-/Unterzahl 965B+0x177 (DGROUP 0x97D2 + 2 · k), Wörter.
            for (var k = 0; k < manager.PowerPlayCounts.Length; k++)
                manager.PowerPlayCounts[k] = BinaryPrimitives.ReadInt16LittleEndian(record[(0x177 + 2 * k)..]);
            // [ORIGINAL] Talentsuche: Restzähler 965B+0xEC (0 = keine Suche).
            var ticks = record[0xEC];
            Finance(manager).TalentSearch = ticks == 0 ? null : new TalentSearch { TicksLeft = ticks };
            manager.SetNewMessage(save.Block("5A50")[m] != 0);
        }
        ApplyNationalAccount(records);
        ApplyStocks(save);
        ApplyTeamTables(save, clubOf);
    }

    /// <summary>
    /// Darlehensliste 965B+0x19B: fünf 32-Bit-Werte je Satz, Eintrag i = Stand mit Manager i (0–3) bzw. der Bank (i = 4), negativ =
    /// Schuld, Drittel-DM. [ORIGINAL] Bank an FILE08 (Manager 0 Eintrag 4 = −2.103.000, Satz 4 Eintrag 0 = +2.103.000).
    /// [APPROX] Ein positiver Bankeintrag ist das Sparbuch (Saldo wie im Remake: Sparbuch − Kredit; an keinem Stand belegt);
    /// Darlehen zwischen Managern (Eintrag i = 0–3) nach demselben Muster, an keinem Stand belegt.
    /// </summary>
    private void ApplyLoans(int m, ReadOnlySpan<byte> record)
    {
        var bank = Finance(Managers[m]).Bank;
        var balance = BinaryPrimitives.ReadInt32LittleEndian(record[(0x19B + 4 * 4)..]) / 3L;
        bank.Credit = Math.Max(0, -balance);
        bank.Savings = Math.Max(0, balance);
        for (var lender = 0; lender < Managers.Count; lender++)
        {
            var value = BinaryPrimitives.ReadInt32LittleEndian(record[(0x19B + 4 * lender)..]);
            if (lender != m && value < 0)
                ManagerLoans.Add(new ManagerLoan { Lender = lender, Borrower = m, Amount = -value / 3L });
        }
    }

    /// <summary>[ORIGINAL] Konto des Nationalteams: Satz 5 +0x18F (DGROUP 0xA055), Drittel-DM; Buchungsliste beginnt mit dem Stand.</summary>
    private void ApplyNationalAccount(byte[] records)
    {
        var account = BinaryPrimitives.ReadInt32LittleEndian(records.AsSpan(5 * OriginalSaveGame.ManagerRecordLength + 0x18F)) / 3L;
        NationalBookings.Clear();
        NationalAccount = 0;
        if (account != 0)
            BookNational(NationalBookingKind.Carryover, account);
    }

    /// <summary>
    /// [ORIGINAL] Börse: Kursverlauf DGROUP 0xD0F4 + 0x48 · Aktie (36 Wörter, Drittel-DM, letztes = Kurs), Basiswert 0x1FD + 4 · Aktie,
    /// Restdauer 0xA891 + Aktie, Tendenz 0xA895 + Aktie (Stock); FILE01: Kurse 84 / 119 / 135 / 147 DM wie im Bild.
    /// </summary>
    private void ApplyStocks(OriginalSaveGame save)
    {
        var history = save.Block("D0F4");
        var stocks = StockMarket.Defaults.Select((d, i) =>
        {
            var stock = new Stock
            {
                Name = d.Name,
                Base = BinaryPrimitives.ReadInt32LittleEndian(save.Block("01FD").AsSpan(4 * i)),
                Countdown = save.Block("A891")[i],
                Direction = save.Block("A895")[i],
            };
            for (var k = 0; k < Stock.HistoryLength; k++)
                stock.History.Add(BinaryPrimitives.ReadUInt16LittleEndian(history.AsSpan(0x48 * i + 2 * k)) / 3);
            stock.Price = stock.History[^1];
            return stock;
        }).ToList();
        Stocks = new StockMarket { Stocks = stocks };
    }

    /// <summary>[ORIGINAL] Aktienbesitz DGROUP 0xD214 + 32 · Manager + 8 · Aktie: +0 Kaufpreis (Drittel-DM), +4 Stückzahl.</summary>
    private void ApplyHoldings(OriginalSaveGame save, int m)
    {
        var block = save.Block("D214");
        var holdings = Finance(Managers[m]).Holdings;
        for (var i = 0; i < holdings.Length; i++)
        {
            holdings[i].PurchaseCost = BinaryPrimitives.ReadInt32LittleEndian(block.AsSpan(32 * m + 8 * i)) / 3;
            holdings[i].Count = BinaryPrimitives.ReadInt32LittleEndian(block.AsSpan(32 * m + 8 * i + 4));
        }
    }

    /// <summary>
    /// Werbung: Verträge DGROUP 0xC15B + 0x6E · Manager (11 Flächen zu 10 Byte: +0 Betrag Drittel-DM, +4 Partner, +5 Restmonate,
    /// +6 Option, Bit 0x80 = „Kündbar.“ nach Aufstieg – im Remake nicht geführt, finanzen-rekonstruktion.md; +7 Logo, +8 Bewertung), Angebote 0xC313 + 0x1F4 · Manager + 0x64 · Art + 10 · Partner (+0 Betrag, +4 Banden,
    /// +5 Laufzeit, +6 Option, +7 Logo, +8 Bewertung). [ORIGINAL] Aufbau em.exe 0x3FB47/0x3FD52, 0x2A9AD (FinanceSponsors.cs), Angebote an
    /// FILE05 (Logo +7 = Partnerindex, Fernsehen nur Partner 0–4). [APPROX] Ein Angebot mit Betrag 0 gilt als gelöscht; Bandenflächen
    /// desselben Partners mit gleichem Betrag und gleicher Restlaufzeit bilden einen Vertrag. Verträge an keinem Stand belegt.
    /// </summary>
    private void ApplySponsoring(OriginalSaveGame save, int m)
    {
        var sponsoring = Finance(Managers[m]).Sponsoring;
        var contracts = save.Block("C15B").AsSpan(0x6E * m, 0x6E);
        sponsoring.Contracts.Clear();
        var boards = new Dictionary<(int Sponsor, int Amount, int Months), List<int>>();
        for (var area = 0; area < 11; area++)
        {
            var c = contracts.Slice(10 * area, 10);
            var amount = BinaryPrimitives.ReadInt32LittleEndian(c);
            if (amount == 0)
                continue;
            if (area < Sponsoring.BoardCount)
            {
                var key = (c[4], amount, c[5]);
                if (!boards.TryGetValue(key, out var list))
                    boards[key] = list = [];
                list.Add(area);
                if (list.Count > 1)
                    continue;
            }
            sponsoring.Contracts.Add(new SponsorContract
            {
                Kind = OriginalImport.AreaKind(area),
                Sponsor = c[4],
                Boards = [],
                MonthlyAmount = amount / 3,
                MonthsLeft = c[5],
                OptionMonths = c[6] & 0x7F,
                SignedRating = c[8],
            });
        }
        // Bandenverträge: Felder je Partner nachtragen (Boards ist init-only, daher neu anlegen).
        for (var i = 0; i < sponsoring.Contracts.Count; i++)
        {
            var contract = sponsoring.Contracts[i];
            if (contract.Kind != AdSlotKind.Bande)
                continue;
            var areas = boards[(contract.Sponsor, (int)(contract.MonthlyAmount * 3), contract.MonthsLeft)];
            sponsoring.Contracts[i] = new SponsorContract
            {
                Kind = contract.Kind, Sponsor = contract.Sponsor, Boards = areas.ToArray(), MonthlyAmount = contract.MonthlyAmount,
                MonthsLeft = contract.MonthsLeft, OptionMonths = contract.OptionMonths, SignedRating = contract.SignedRating,
            };
        }

        var offers = save.Block("C313").AsSpan(0x1F4 * m, 0x1F4);
        sponsoring.Offers.Clear();
        for (var k = 0; k < 5; k++)
        {
            var kind = OriginalImport.OfferKind(k);
            var byPartner = new Dictionary<int, SponsorOffer>();
            for (var p = 0; p < Sponsor.For(kind).Length; p++)
            {
                var o = offers.Slice(0x64 * k + 10 * p, 10);
                var amount = BinaryPrimitives.ReadInt32LittleEndian(o);
                if (amount != 0)
                    byPartner[p] = new SponsorOffer(kind, p, o[5], o[4], amount / 3, o[6], o[8]);
            }
            sponsoring.Offers[kind] = byPartner;
        }
    }

    /// <summary>
    /// [ORIGINAL] Trainingsplan DGROUP 0xA8A1 + 28 · Manager, Einheit Tag · 4 + Einheit, Wert = Art (FILE05: Standardplan wie
    /// <see cref="TrainingPlan.Prepare"/>). Preisindex DGROUP 0x2BC (an allen Ständen 100). [APPROX] Die Preisstufe bleibt aus dem
    /// Nachspielen (Liga beim Spielstart).
    /// </summary>
    private void ApplyTraining(OriginalSaveGame save, int m)
    {
        var plan = Managers[m].Training;
        var units = save.Block("A8A1").AsSpan(28 * m, 28);
        for (var day = 0; day < TrainingPlan.Days; day++)
            for (var unit = 0; unit < TrainingPlan.UnitsPerDay; unit++)
                plan[day, unit] = (TrainingKind)Math.Min((int)units[day * TrainingPlan.UnitsPerDay + unit], TrainingPlan.Kinds - 1);
        plan.PriceIndex = BinaryPrimitives.ReadInt32LittleEndian(save.Block("02BC"));
    }

    /// <summary>
    /// [ORIGINAL] Taktik des Managervereins: Modi DGROUP 0xA6FB (Einsatz), 0xA707 (Angriff), 0xA701 (Reihenwechsel) je Manager; Werte
    /// 0xCAE9 + 0x30 · Manager (Einsatz, Block · 6 + Spielstand) und + 0x18 (Angriff); Minuten 0xCC43 + 8 · Manager + 4 · Teil + Reihe
    /// (Teil 0 Abwehr, 1 Sturm; re-geld.md); „automatisch aufstellen“ 0x375 + 2 · Manager (re-runde-14.md, Taktik 375h).
    /// FILE24/FILE40: Block 0 trägt die Werte der Automatik, Blöcke 1–3 stehen auf 0.
    /// </summary>
    private void ApplyTactics(OriginalSaveGame save, Club club, int m)
    {
        ApplyTacticValues(save, club.Tactics, m);
        club.AutoLineupEnabled = save.Block("0375")[2 * m] != 0;
    }

    /// <summary>Modi, Werte und Minuten des Satzes <paramref name="m"/> (0–3 Manager, 5 Bundestrainer; Teil 4b).</summary>
    private static void ApplyTacticValues(OriginalSaveGame save, Tactics tactics, int m)
    {
        tactics.EffortMode = (TacticMode)Math.Min((int)save.Block("A6FB")[m], 2);
        tactics.AttackMode = (TacticMode)Math.Min((int)save.Block("A707")[m], 2);
        tactics.LineChange = (LineChangeMode)Math.Min((int)save.Block("A701")[m], (int)LineChangeMode.FullyManual);
        var values = save.Block("CAE9").AsSpan(0x30 * m, 0x30);
        for (var b = 0; b < Tactics.Blocks; b++)
        {
            // Erst leeren, dann Grundwert, dann Zuschläge: die Grenzen hängen jeweils von den anderen Feldern ab.
            for (var i = 0; i < Tactics.Situations; i++)
            {
                tactics.SetEffort(b, (ScoreSituation)i, 0);
                tactics.SetAttack(b, (ScoreSituation)i, 0);
            }
            for (var i = 0; i < Tactics.Situations; i++)
            {
                tactics.SetEffort(b, (ScoreSituation)i, values[6 * b + i]);
                tactics.SetAttack(b, (ScoreSituation)i, values[0x18 + 6 * b + i]);
            }
        }
        var minutes = save.Block("CC43").AsSpan(8 * m, 8);
        for (var part = 0; part < 2; part++)
            for (var line = 0; line < Lineup.Blocks; line++)
                tactics.SetMinutes(line, part == 1, 0);
        for (var part = 0; part < 2; part++)
            for (var line = 0; line < Lineup.Blocks; line++)
                tactics.SetMinutes(line, part == 1, minutes[4 * part + line]);
    }

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
        }
        for (var m = 0; m < Math.Min(Managers.Count, OriginalImport.HeadToHeadManagers); m++)
            for (var k = 0; k < Series.Count; k++)
                Managers[m].SeriesRecords[k] = block[OriginalImport.ManagerSeriesOffset + Series.Count * m + k];
    }
}
