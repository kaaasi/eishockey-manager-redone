using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// #757 (R752-B2): Aktienkurs 0. Der Tageskurs kann auf 0 fallen (FinanceStocks.Advance wie em.exe Datei 0x5799F–0x57C50:
/// der Tag beginnt mit mindestens 10, Ereignisse ziehen bis zu v/2 + 10 + 1 ab). Im Original teilt der Höchstwert
/// „kaufen“ (Datei 0x59702–0x5972E) Konto / Kurs über die Laufzeit-Division (Datei 0x0227C → `div bx` Datei 0x0231A);
/// bei Kurs 0 löst das INT 0 aus, der Handler (Datei 0x01F74) gibt „Divide error“ (DGROUP 0x2F) aus und beendet das
/// Programm mit Rückgabe 3 (Datei 0x02088). Das Remake bricht nicht ab: Höchstwert 0, kein Kauf.
/// </summary>
public class StockPriceZeroTests
{
    private static GameState Game(out Manager manager)
    {
        var game = TestGames.Create();
        manager = game.ActiveManager;
        manager.Money = 100_000;
        game.Stocks.Stocks[0].Price = 0;
        return game;
    }

    /// <summary>Probe aus #757: Kurs 0 und positives Konto → vorher <see cref="DivideByZeroException"/>.</summary>
    [Fact]
    public void BuyStock_AtPriceZero_BuysNothing()
    {
        var game = Game(out var m);

        Assert.Equal(0, game.BuyStock(m, 0, 10));
        Assert.Equal(100_000, m.Money);
        Assert.Equal(0, game.Finance(m).Holdings[0].Count);
        Assert.Equal(0, game.Finance(m).Holdings[0].PurchaseCost);
    }

    /// <summary>Höchstwert links: Konto / Kurs (Datei 0x59702–0x5972E), 0 bei Konto ≤ 0 (Datei 0x596F2–0x59737) und bei Kurs 0.</summary>
    [Fact]
    public void MaxStockPurchase_FollowsOriginal_AndIsZeroAtPriceZero()
    {
        var game = Game(out var m);
        game.Stocks.Stocks[1].Price = 300;

        Assert.Equal(0, game.MaxStockPurchase(m, 0));
        Assert.Equal(333, game.MaxStockPurchase(m, 1));
        m.Money = 0;
        Assert.Equal(0, game.MaxStockPurchase(m, 1));
        m.Money = -5_000;
        Assert.Equal(0, game.MaxStockPurchase(m, 1));
    }

    /// <summary>Verkauf teilt nicht durch den Kurs: bei Kurs 0 gehen die Stücke für 0 DM weg (Datei 0x5915F–0x5952D, w = 0).</summary>
    [Fact]
    public void SellStock_AtPriceZero_SellsForNothing()
    {
        var game = Game(out var m);
        var holding = game.Finance(m).Holdings[0];
        holding.Count = 10;
        holding.PurchaseCost = 1_015;

        Assert.Equal(4, game.SellStock(m, 0, 4));
        Assert.Equal(100_000, m.Money);
        Assert.Equal(6, holding.Count);
        Assert.Equal(1_015 * 60 / 100, holding.PurchaseCost);
    }
}
