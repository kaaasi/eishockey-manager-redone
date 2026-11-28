using System.Buffers.Binary;
using EishockeyManager.Formats;

namespace EishockeyManager.Core;

public static partial class OriginalImport
{
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

public sealed partial class GameState
{
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
    /// +6 Option, Bit 0x80 = „Kündbar.“ nach Aufstieg (A582, finanzen-rekonstruktion.md); +7 Logo, +8 Bewertung), Angebote 0xC313 + 0x1F4 · Manager + 0x64 · Art + 10 · Partner (+0 Betrag, +4 Banden,
    /// +5 Laufzeit, +6 Option, +7 Logo, +8 Bewertung). [ORIGINAL] Aufbau em.exe 0x3FB47/0x3FD52, 0x2A9AD (FinanceSponsors.cs), Angebote an
    /// FILE05 (Logo +7 = Partnerindex, Fernsehen nur Partner 0–4). [APPROX] Ein Angebot mit Betrag 0 gilt als gelöscht.
    /// [ORIGINAL] A614 (Runde 23, FILE76): Der Werbebildschirm kopiert das Angebot über die Fläche, der Partner steht damit in +7
    /// (Logo); bei Banden ist +4 die Vertragsnummer DGROUP 0x17C (Datei 0x2BB5B–0x2BB5E, nach allen Banden <c>inc byte [0x17C]</c>
    /// Datei 0x2BB80), bei Trikot, Fernsehen und Eis die Bandenzahl des Angebots. Bandenflächen mit gleicher Nummer (und gleichem
    /// Partner, Betrag, Restlaufzeit) bilden einen Vertrag; Bandenverträge stehen in der Folge ihrer Nummern.
    /// </summary>
    private void ApplySponsoring(OriginalSaveGame save, int m)
    {
        var sponsoring = Finance(Managers[m]).Sponsoring;
        var block = save.Block("C15B");
        var contracts = block.AsSpan(0x6E * m, 0x6E);
        sponsoring.Contracts.Clear();
        // Bandenflächen desselben Vertrags: Schlüssel aus dem Rohbetrag (Drittel-DM), nicht aus dem gerundeten Monatsbetrag –
        // 1.000.001 / 3 · 3 = 999.999 fand den Schlüssel nicht (Review R14HI-03, KeyNotFoundException).
        var boards = new Dictionary<(int Number, int Sponsor, int Amount, int Months), List<int>>();
        var keys = new List<(int Number, int Sponsor, int Amount, int Months)>(); // Schlüssel je Vertrag in sponsoring.Contracts
        foreach (var area in Enumerable.Range(0, 11).OrderBy(a => a < Sponsoring.BoardCount ? block[0x6E * m + 10 * a + 4] : 0x100).ThenBy(a => a))
        {
            var c = contracts.Slice(10 * area, 10);
            var amount = BinaryPrimitives.ReadInt32LittleEndian(c);
            if (amount == 0)
                continue;
            var key = (area < Sponsoring.BoardCount ? c[4] : -1, (int)c[7], amount, (int)c[5]);
            if (area < Sponsoring.BoardCount)
            {
                if (!boards.TryGetValue(key, out var list))
                    boards[key] = list = [];
                list.Add(area);
                if (list.Count > 1)
                    continue;
            }
            keys.Add(key);
            sponsoring.Contracts.Add(new SponsorContract
            {
                Kind = OriginalImport.AreaKind(area),
                Sponsor = c[7],
                Boards = [],
                MonthlyAmount = amount / 3,
                MonthsLeft = c[5],
                OptionMonths = c[6] & 0x7F, Cancellable = (c[6] & 0x80) != 0,
                SignedRating = c[8],
            });
        }
        // Bandenverträge: Felder je Partner nachtragen (Boards ist init-only, daher neu anlegen).
        for (var i = 0; i < sponsoring.Contracts.Count; i++)
        {
            var contract = sponsoring.Contracts[i];
            if (contract.Kind != AdSlotKind.Bande)
                continue;
            var areas = boards[keys[i]];
            sponsoring.Contracts[i] = new SponsorContract
            {
                Kind = contract.Kind, Sponsor = contract.Sponsor, Boards = areas.ToArray(), MonthlyAmount = contract.MonthlyAmount,
                MonthsLeft = contract.MonthsLeft, OptionMonths = contract.OptionMonths, SignedRating = contract.SignedRating,
                Cancellable = contract.Cancellable,
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
}
