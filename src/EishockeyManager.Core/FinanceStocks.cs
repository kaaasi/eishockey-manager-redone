namespace EishockeyManager.Core;

/// <summary>Eine der vier Aktien der Börse (kb\152: Herrenhäuser, Play Time, Software 2000, KRON Soft).</summary>
public sealed class Stock
{
    /// <summary>Länge des Kursverlaufs = Breite des schwarzen Feldes im Bildschirm Finanzen.</summary>
    public const int HistoryLength = 110;

    public required string Name { get; init; }

    /// <summary>Aktueller Kurs in DM.</summary>
    public int Price { get; set; }

    /// <summary>[APPROX] Langfristiger Mittelwert, um den der Kurs schwankt.</summary>
    public required int BasePrice { get; init; }

    /// <summary>[APPROX] Aktuelle Tendenz in DM pro Tag (Kurse laufen im Video in längeren Auf- und Abwärtsphasen).</summary>
    public double Trend { get; set; }

    /// <summary>Ungerundeter Kurs für die Kursrechnung (null = <see cref="Price"/>).</summary>
    public double? Exact { get; set; }

    /// <summary>Kursverlauf, ältester Wert zuerst (höchstens <see cref="HistoryLength"/> Einträge).</summary>
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
    /// Namen wie in em.exe; Startkurse aus dem UI-Video (5:45: 101, 120, 79, 174 DM).
    /// [APPROX] Ob das Original die Startkurse auswürfelt, ist offen.
    /// </summary>
    public static readonly (string Name, int Price)[] Defaults =
    [
        ("Herrenhäuser", 101),
        ("Play Time", 120),
        ("Software 2000", 79),
        ("KRON Soft", 174),
    ];

    public required IReadOnlyList<Stock> Stocks { get; init; }

    public static StockMarket Create(Random random)
    {
        var market = new StockMarket
        {
            Stocks = Defaults.Select(d => new Stock { Name = d.Name, BasePrice = d.Price, Price = d.Price }).ToList(),
        };
        // Vorgeschichte, damit der Kursverlauf schon zu Spielbeginn gefüllt ist (wie im Video).
        // Rückwärts simulieren, sodass der letzte Wert genau dem Startkurs entspricht.
        foreach (var stock in market.Stocks)
        {
            var path = new List<int> { stock.Price };
            var price = (double)stock.Price;
            var trend = 0.0;
            for (var i = 1; i < Stock.HistoryLength; i++)
            {
                (price, trend) = Step(price, trend, stock.BasePrice, random);
                path.Add((int)Math.Round(price));
            }
            path.Reverse();
            stock.History.AddRange(path);
        }
        return market;
    }

    /// <summary>Kurse eines Tages weiterrechnen.</summary>
    public void Advance(Random random)
    {
        foreach (var stock in Stocks)
        {
            var (price, trend) = Step(stock.Exact is { } e && Math.Round(e) == stock.Price ? e : stock.Price, stock.Trend, stock.BasePrice, random);
            stock.Exact = price;
            stock.Trend = trend;
            stock.Price = (int)Math.Round(price);
            stock.Record();
        }
    }

    /// <summary>
    /// [APPROX] Kursmodell: Tendenz, die sich alle paar Tage ändert und zum Mittelwert zieht, dazu kleines Rauschen
    /// und selten ein Sprung. Im Video laufen die Kurven in ruhigen Auf- und Abwärtsphasen mit 1–2 DM pro Tag,
    /// gelegentlich mit einem Sprung von etwa 10 DM; durch das Runden auf ganze DM wirken sie treppenförmig.
    /// </summary>
    private static (double Price, double Trend) Step(double price, double trend, int basePrice, Random random)
    {
        if (random.NextDouble() < 0.08)
            trend = (random.NextDouble() - 0.5) * 0.024 * price;
        trend += (basePrice - price) * 0.0015;
        price += trend + (random.NextDouble() - 0.5) * 0.004 * price;
        if (random.NextDouble() < 0.01)
            price *= 1 + (random.NextDouble() - 0.5) * 0.16;
        return (Math.Clamp(price, 5, 999), trend);
    }
}

public sealed partial class GameState
{
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
