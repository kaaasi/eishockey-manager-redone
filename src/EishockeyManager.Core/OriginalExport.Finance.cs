using EishockeyManager.Formats;

namespace EishockeyManager.Core;

public sealed partial class GameState
{
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
    /// [ORIGINAL] Logo +7 = Partner (FILE05, FILE76); Option +6 mit Bit 0x80 = „Kündbar.“ (A582). [ORIGINAL] A614 (Runde 23): bei
    /// Banden +4 = Vertragsnummer aus DGROUP 0x17C, danach 0x17C + 1 (Datei 0x2BB5B–0x2BB80, FILE76: Nummern 1 und 2, 0x17C = 3).
    /// [APPROX] Nummern 1 … n in der Folge der Verträge (Manager, dann Abschluss), 0x17C = n + 1 – das Original zählt auch
    /// abgelaufene Verträge; bei Trikot, Fernsehen und Eis ist +4 im Original die Bandenzahl des Angebots, die der Vertrag im Remake
    /// nicht führt (Export: Partner wie bisher).
    /// </summary>
    private static void WriteSponsoring(OriginalSaveWriter w, int m, Sponsoring sponsoring, ref int contractNumber)
    {
        var contracts = w.Block("C15B");
        foreach (var c in sponsoring.Contracts)
        {
            var areas = c.Kind == AdSlotKind.Bande ? c.Boards : [AreaOf(c.Kind)];
            var number = c.Kind == AdSlotKind.Bande ? contractNumber++ : c.Sponsor;
            foreach (var area in areas)
            {
                var at = 0x6E * m + 10 * area;
                OriginalExport.Write32(contracts, at, OriginalExport.Thirds(c.MonthlyAmount));
                contracts[at + 4] = OriginalExport.Byte(number);
                contracts[at + 5] = OriginalExport.Byte(c.MonthsLeft);
                contracts[at + 6] = (byte)(OriginalExport.Byte(c.OptionMonths & 0x7F) | (c.Cancellable ? 0x80 : 0)); // [ORIGINAL] Bit 0x80 = „Kündbar.“ (0x2713F)
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
}
