namespace EishockeyManager.Core;

/// <summary>
/// Eine der vier Aktien der Börse (kb\152: Herrenhäuser, Play Time, Software 2000, KRON Soft).
/// [ORIGINAL] Datensatz in em.exe: Kursverlauf und Kurs DGROUP <c>0xD0F4 + 0x48·Aktie</c> (36 Wörter in Drittel-DM,
/// das letzte ist der aktuelle Kurs <c>0xD13A</c>), Basiswert <c>0x1FD + 4·Aktie</c>, Restdauer der Tendenz
/// <c>0xA891 + Aktie</c>, Tendenz <c>0xA895 + Aktie</c> (<c>docs/finanzen-rekonstruktion.md</c>, Abschnitt Börse).
/// </summary>
public sealed class Stock
{
    /// <summary>[ORIGINAL] Länge des Kursverlaufs: 36 Tage (DGROUP 0xD0F4, 0x48 Byte je Aktie), letzter Wert = Kurs.</summary>
    public const int HistoryLength = 36;

    public required string Name { get; init; }

    /// <summary>Aktueller Kurs in DM (im Original Drittel-DM, immer ein Vielfaches von 3).</summary>
    public int Price { get; set; }

    /// <summary>
    /// [ORIGINAL] Basiswert (DGROUP 0x1FD, 32 Bit) in Hundertstel-DM: Zielkurs = Basiswert / 100. Wächst täglich um
    /// 3/10.000 und springt bei seltenen Kursereignissen.
    /// </summary>
    public long Base { get; set; }

    /// <summary>[ORIGINAL] Tendenz (DGROUP 0xA895): 0 = fallend (−1 DM je Tag), 2 = steigend (+1 DM); zu Programmstart 0.</summary>
    public int Direction { get; set; }

    /// <summary>[ORIGINAL] Restliche Tage der Tendenz (DGROUP 0xA891); bei 0 wird eine neue ausgewürfelt.</summary>
    public int Countdown { get; set; }

    /// <summary>Kursverlauf in DM, ältester Wert zuerst (höchstens <see cref="HistoryLength"/> Einträge, letzter = Kurs).</summary>
    public List<int> History { get; } = [];

    public void Record()
    {
        History.Add(Price);
        if (History.Count > HistoryLength)
            History.RemoveAt(0);
    }
}

/// <summary>Aktienbesitz eines Managers an einer Aktie.</summary>
public sealed class StockHolding
{
    public int Count { get; set; }

    /// <summary>Summe der Kaufpreise des aktuellen Bestands („Kauf:“).</summary>
    public long PurchaseCost { get; set; }
}

/// <summary>Börse mit vier Aktien.</summary>
public sealed class StockMarket
{
    /// <summary>
    /// [ORIGINAL] Namen wie in em.exe (DGROUP 0x1DC5 ff.) und Basiswerte zu Programmstart (DGROUP 0x1FD:
    /// 10.000 / 11.000 / 13.500 / 17.000, also Zielkurse 100 / 110 / 135 / 170 DM).
    /// </summary>
    public static readonly (string Name, long Base)[] Defaults =
    [
        ("Herrenhäuser", 10_000),
        ("Play Time", 11_000),
        ("Software 2000", 13_500),
        ("KRON Soft", 17_000),
    ];

    /// <summary>[ORIGINAL] Börsentage vor Spielbeginn (Spielstart 0x43AD5: 90 Aufrufe der Tagesrechnung).</summary>
    public const int StartDays = 90;

    /// <summary>[ORIGINAL] Kurs, ab dem die Aktie geteilt wird (Tagesrechnung 0x57BBE: über 2.000 DM → 1.000 DM).</summary>
    public const int SplitAbove = 2_000;

    public required IReadOnlyList<Stock> Stocks { get; init; }

    /// <summary>
    /// [ORIGINAL] Spielstart (em.exe 0x43AA4–0x43AE0): Kurs = Basiswert / 50 Drittel-DM (66 / 73 / 90 / 113 DM), dann
    /// 90 Börsentage. Danach ist der ganze Verlauf (36 Tage) gefüllt; die Startkurse sind ausgewürfelt (FILE01:
    /// 84 / 119 / 135 / 147 DM, folge01: 98 / 96 / 134 / 126 DM).
    /// </summary>
    public static StockMarket Create(Random random)
    {
        var market = new StockMarket
        {
            Stocks = Defaults.Select(d => new Stock { Name = d.Name, Base = d.Base, Price = (int)(d.Base / 50 / 3) }).ToList(),
        };
        for (var day = 0; day < StartDays; day++)
            market.Advance(random);
        return market;
    }

    /// <summary>
    /// [ORIGINAL] Tagesrechnung der Börse (em.exe Overlay 0x138, Einsprung 0x2F, Datei 0x5784A–0x57C63), Reihenfolge der
    /// Zufallszahlen wie im Original. <paramref name="split"/> erhält den Index jeder Aktie, die geteilt wurde (der
    /// Aufrufer verdoppelt die Stückzahlen der Manager).
    /// </summary>
    public void Advance(Random random, Action<int>? split = null)
    {
        int Between(int min, int max) => SquadSetup.Between(random, Math.Min(min, max), Math.Max(min, max));
        int Sign() => Between(0, 1) * 2 - 1;

        // Basiswert wächst um 3/10.000 je Tag (0x57854–0x57897).
        foreach (var stock in Stocks)
            stock.Base += stock.Base * 3 / 10_000;

        for (var i = 0; i < Stocks.Count; i++)
        {
            var stock = Stocks[i];
            long v = stock.Price;
            var target = stock.Base / 100;
            v += stock.Direction - 1;
            if (v < 10)
                v = 10;

            // Tendenz: 0 (fallend) oder 2 (steigend) für random(2, 8) bzw. random(2, 9) Tage (0x57926–0x5799B).
            if (stock.Countdown != 0)
                stock.Countdown--;
            if (stock.Countdown == 0)
            {
                do
                    stock.Direction = Between(0, 2);
                while (stock.Direction == 1);
                stock.Countdown = Between(2, 8 + (stock.Direction > 0 ? 1 : 0));
            }

            // Großes Kursereignis mit Wahrscheinlichkeit 1/(401 − 50·i): ±(v/5 … v/2), zur Hälfte mit neuem
            // Basiswert v·100 ± 15 (0x5799F–0x57A5C).
            if (Between(0, 400 - 50 * i) == 0)
            {
                var amount = Between((int)(v / 5), (int)(v / 2));
                v += amount * Sign();
                if (Between(0, 1) == 0)
                    stock.Base = v * 100 + 15 * Sign();
            }

            // Kleines Ereignis mit Wahrscheinlichkeit 1/(23 − 2·i): ±(2 … 10) DM, zu einem Viertel auch der Basiswert
            // um ±(2 … 10) Hundertstel (0x57A60–0x57AEE).
            if (Between(0, 22 - 2 * i) == 0)
            {
                v += Between(2, 10) * Sign();
                if (Between(0, 3) == 0)
                    stock.Base += Between(2, 10) * Sign();
            }

            // Rauschen: zur Hälfte −1, 0 oder +1 DM (0x57AF2–0x57B17).
            if (Between(0, 1) != 0)
                v += Between(0, 2) - 1;

            // Rückzug zum Zielkurs: über dem Ziel (sonst zu 50 %) bremst eine steigende Tendenz, unter dem Ziel eine
            // fallende (0x57B1A–0x57B8D). Vergleiche vorzeichenlos wie im Original.
            if (((uint)v > (uint)target || Between(0, 1) != 0) && stock.Direction == 2)
                v--;
            if (((uint)v < (uint)target || Between(0, 1) != 0) && stock.Direction == 0)
                v++;

            // Aktienteilung: über 2.000 DM fällt der Kurs auf 1.000 DM, die Stückzahlen verdoppeln sich (0x57BBE–0x57C2E).
            if (v > SplitAbove)
            {
                v = SplitAbove / 2;
                split?.Invoke(i);
            }

            // Gespeichert wird v·3 als 16-Bit-Wort (0x57C30–0x57C50); der nächste Tag liest es vorzeichenlos.
            stock.Price = (ushort)(v * 3) / 3;
            stock.Record();
        }
    }
}

public sealed partial class GameState
{
    /// <summary>
    /// Ein Börsentag (Tagesabschluss 0x3ED4A): Kurse weiterrechnen; bei einer Aktienteilung verdoppelt das Original die
    /// Stückzahlen aller Manager (0x57BD7–0x57C2E, Kaufpreise bleiben).
    /// </summary>
    public void AdvanceStocks() =>
        Stocks.Advance(FinanceRandom, i =>
        {
            foreach (var manager in Managers)
                Finance(manager).Holdings[i].Count *= 2;
        });

    /// <summary>Kauft Aktien (soweit das Geld reicht). Liefert die Zahl der gekauften Stücke.</summary>
    public int BuyStock(Manager manager, int stockIndex, int count)
    {
        var stock = Stocks.Stocks[stockIndex];
        count = (int)Math.Min(count, Math.Max(0, manager.Money) / stock.Price);
        if (count <= 0)
            return 0;
        var holding = Finance(manager).Holdings[stockIndex];
        holding.Count += count;
        holding.PurchaseCost += (long)count * stock.Price;
        manager.Money -= (long)count * stock.Price;
        return count;
    }

    /// <summary>Verkauft Aktien. Gewinn/Verlust gegenüber dem Kaufpreis geht ins Finanzbuch (Aktien).</summary>
    public int SellStock(Manager manager, int stockIndex, int count)
    {
        var stock = Stocks.Stocks[stockIndex];
        var holding = Finance(manager).Holdings[stockIndex];
        count = Math.Min(count, holding.Count);
        if (count <= 0)
            return 0;
        var cost = holding.PurchaseCost * count / holding.Count;
        var revenue = (long)count * stock.Price;
        holding.Count -= count;
        holding.PurchaseCost -= cost;
        manager.Money += revenue;
        Finance(manager).Book.Add(FinanceCategory.Aktien, revenue - cost);
        return count;
    }

    /// <summary>Summe der Kaufpreise aller Aktien eines Managers.</summary>
    public long DepotPurchaseCost(Manager manager) => Finance(manager).Holdings.Sum(h => h.PurchaseCost);

    /// <summary>Aktueller Wert aller Aktien eines Managers.</summary>
    public long DepotValue(Manager manager) =>
        Finance(manager).Holdings.Select((h, i) => (long)h.Count * Stocks.Stocks[i].Price).Sum();
}
