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
            for (var i = 1; i < Stock.HistoryLength; i++)
            {
                price = Step(price, stock.BasePrice, random);
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
            stock.Price = (int)Math.Round(Step(stock.Price, stock.BasePrice, random));
            stock.Record();
        }
    }

    /// <summary>
    /// [APPROX] Kursmodell: meist unverändert, sonst kleiner Schritt mit Zug zum Mittelwert, selten ein Sprung.
    /// Im Video sieht der Verlauf treppenförmig aus (waagerechte Stücke mit Sprüngen).
    /// </summary>
    private static double Step(double price, int basePrice, Random random)
    {
        var r = random.NextDouble();
        if (r < 0.45)
            return price;
        var drift = (basePrice - price) * 0.02;
        var change = (random.NextDouble() - 0.5) * price * 0.06;
        if (r > 0.97)
            change = (random.NextDouble() - 0.5) * price * 0.4;
        return Math.Clamp(price + drift + change, 5, 999);
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
