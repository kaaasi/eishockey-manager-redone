using System.Text.Json.Nodes;
using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur boerse-start (#169): Börse zu Spielbeginn und Tagesrechnung der Kurse nach em.exe (Overlay 0x138, Einsprung
/// 0x2F, Datei 0x5784A; Spielstart 0x43AA4). Belege: <c>docs/finanzen-rekonstruktion.md</c>, Abschnitt „Börse“.
/// </summary>
public class BoerseStartTests
{
    /// <summary>
    /// Zufallsquelle mit vorgegebenen Antworten: protokolliert jeden Aufruf von random(a, b) als (a, b) und antwortet
    /// aus der Liste bzw. mit der Regel.
    /// </summary>
    private sealed class ScriptedRandom(Func<int, int, int> answer) : Random
    {
        private readonly Queue<int>? _answers;

        public ScriptedRandom(params int[] answers) : this((_, _) => 0) => _answers = new Queue<int>(answers);

        public List<(int Min, int Max)> Calls { get; } = [];

        public override int Next(int minValue, int maxValue)
        {
            var max = maxValue - 1;
            Calls.Add((minValue, max));
            var value = _answers is null ? answer(minValue, max) : _answers.Dequeue();
            Assert.InRange(value, minValue, max);
            return value;
        }

        public override int Next() => throw new InvalidOperationException("Die Börse zieht nur random(a, b).");
        public override int Next(int maxValue) => throw new InvalidOperationException("Die Börse zieht nur random(a, b).");
        public override double NextDouble() => throw new InvalidOperationException("Die Börse zieht nur random(a, b).");
    }

    /// <summary>Immer die obere Grenze: kein Kursereignis, Rauschen +1, Tendenz steigend für 9 Tage.</summary>
    private static ScriptedRandom AlwaysMax() => new((_, max) => max);

    private static StockMarket Single(int price, long baseValue, int direction, int countdown) =>
        new() { Stocks = [new Stock { Name = "Herrenhäuser", Price = price, Base = baseValue, Direction = direction, Countdown = countdown }] };

    [Fact]
    public void History_Has36Days_LikeTheOriginal()
    {
        // [ORIGINAL] DGROUP 0xD0F4: 0x48 Byte je Aktie = 36 Wörter, gezeichnet im Abstand von 3 Pixeln (0x5898E).
        Assert.Equal(36, Stock.HistoryLength);
    }

    [Fact]
    public void Start_Runs90Days_FullHistory_BaseGrowsBy3PerTenThousand()
    {
        var random = AlwaysMax();
        var market = StockMarket.Create(random);
        Assert.Equal(["Herrenhäuser", "Play Time", "Software 2000", "KRON Soft"], market.Stocks.Select(s => s.Name));
        Assert.All(market.Stocks, s =>
        {
            Assert.Equal(Stock.HistoryLength, s.History.Count);
            Assert.Equal(s.Price, s.History[^1]);
        });
        // 90 Tage ohne Ereignis: Basiswert += Basiswert·3/10.000 (ganzzahlig) – 10.000 → 10.270 usw. FILE01 (Spielstart
        // im Original) hat 10.273 / 11.283 / 13.864 / 17.443: dieselben Zuwächse plus kleine Basisereignisse (±2…10).
        Assert.Equal([10_270L, 11_270L, 13_860L, 17_450L], market.Stocks.Select(s => s.Base));
        // Je Tag und Aktie: großes Ereignis, kleines Ereignis, Rauschen (2), zwei Rückzüge (einer nur ohne Kurzschluss);
        // in 90 Tagen zusätzlich die Tendenzwechsel.
        Assert.Equal(StockMarket.StartDays * 4, random.Calls.Count(c => c.Max is 400 or 350 or 300 or 250));
    }

    [Fact]
    public void Start_PricesAreRolled_NotFixed()
    {
        // Vorher feste Startkurse 101 / 120 / 79 / 174 aus dem UI-Video. Im Original gewürfelt (FILE01: 84 / 119 / 135 /
        // 147, folge01: 98 / 96 / 134 / 126).
        var starts = Enumerable.Range(0, 40).Select(seed => string.Join("/", StockMarket.Create(new Random(seed)).Stocks.Select(s => s.Price))).ToHashSet();
        Assert.True(starts.Count > 30, $"nur {starts.Count} verschiedene Startkurse");
        var all = Enumerable.Range(0, 200).SelectMany(seed => StockMarket.Create(new Random(seed)).Stocks.Select(s => s.Price)).ToList();
        Assert.All(all, p => Assert.InRange(p, 1, StockMarket.SplitAbove));
    }

    [Fact]
    public void Day_WithoutEvent_FollowsTendency_AndPullsTowardsTarget()
    {
        // Kurs 100, Ziel 10.003/100 = 100, Tendenz steigend (2) mit 3 Tagen Rest.
        var random = new ScriptedRandom(1, 1, 1, 2, 0);
        var market = Single(100, 10_000, 2, 3);
        market.Advance(random);
        var s = market.Stocks[0];
        // 100 + 1 (Tendenz) + 1 (Rauschen 2 − 1) − 1 (über dem Ziel, steigend) = 101; unter dem Ziel nicht → zieht random(0, 1).
        Assert.Equal([(0, 400), (0, 22), (0, 1), (0, 2), (0, 1)], random.Calls);
        Assert.Equal(101, s.Price);
        Assert.Equal(10_003, s.Base);
        Assert.Equal((2, 2), (s.Direction, s.Countdown));
        Assert.Equal([101], s.History);
    }

    [Fact]
    public void Day_NewTendency_AndBigEvent_SetsBase()
    {
        // Tendenz 0 läuft ab: neu würfeln bis ≠ 1, Dauer random(2, 8) für fallend. Großes Ereignis −30, neuer Basiswert
        // 69·100 + 15; danach unter dem Ziel (100) und fallend → +1.
        var random = new ScriptedRandom(1, 0, 5, 0, 30, 0, 0, 1, 5, 0, 1);
        var market = Single(100, 10_000, 0, 1);
        market.Advance(random);
        var s = market.Stocks[0];
        Assert.Equal([(0, 2), (0, 2), (2, 8), (0, 400), (19, 49), (0, 1), (0, 1), (0, 1), (0, 22), (0, 1), (0, 1)], random.Calls);
        Assert.Equal(70, s.Price);
        Assert.Equal(6_915, s.Base);
        Assert.Equal((0, 5), (s.Direction, s.Countdown));
    }

    [Fact]
    public void Day_SmallEvent_ChangesPriceAndBase()
    {
        var random = new ScriptedRandom(7, 0, 10, 1, 0, 4, 0, 1, 0, 1);
        var market = Single(50, 5_000, 2, 4);
        market.Advance(random);
        var s = market.Stocks[0];
        // 50 + 1 + 10 − 1 (Rauschen) − 1 (über dem Ziel 50, steigend: ohne Zufallszahl) = 59; Basis 5.001 − 4.
        Assert.Equal([(0, 400), (0, 22), (2, 10), (0, 1), (0, 3), (2, 10), (0, 1), (0, 1), (0, 2), (0, 1)], random.Calls);
        Assert.Equal(59, s.Price);
        Assert.Equal(4_997, s.Base);
        Assert.Equal(3, s.Countdown);
    }

    [Fact]
    public void Day_EventChancesDependOnStockIndex()
    {
        var random = AlwaysMax();
        var market = new StockMarket
        {
            Stocks = StockMarket.Defaults.Select(d => new Stock { Name = d.Name, Base = d.Base, Price = 100, Direction = 2, Countdown = 5 }).ToList(),
        };
        market.Advance(random);
        Assert.Equal([400, 350, 300, 250], random.Calls.Where(c => c.Max >= 250).Select(c => c.Max));
        Assert.Equal([22, 20, 18, 16], random.Calls.Where(c => c.Max is >= 16 and <= 22).Select(c => c.Max));
    }

    [Fact]
    public void Day_PriceBelowTen_IsRaisedToTen()
    {
        // Kurs 9, fallend: 9 − 1 = 8 → 10; Rauschen +1; unter dem Ziel, fallend → +1.
        var market = Single(9, 10_000, 0, 5);
        market.Advance(new ScriptedRandom(1, 1, 1, 2, 0));
        Assert.Equal(12, market.Stocks[0].Price);
    }

    [Fact]
    public void Day_Above2000_SplitsStock()
    {
        var split = new List<int>();
        var market = Single(2_000, 200_000, 2, 5);
        market.Advance(AlwaysMax(), split.Add);
        // 2.000 + 1 + 1 − 1 = 2.001 > 2.000 → 1.000 DM
        Assert.Equal(1_000, market.Stocks[0].Price);
        Assert.Equal([0], split);
    }

    [Fact]
    public void GameState_Split_DoublesHoldingsOfAllManagers_KeepsPurchaseCost()
    {
        var game = TestGames.Create();
        var me = game.ActiveManager;
        var stock = game.Stocks.Stocks[2];
        Assert.Equal(10, game.BuyStock(me, 2, 10));
        var cost = game.Finance(me).Holdings[2].PurchaseCost;
        // Weit über 2.000 DM: kein Ereignis eines Tages bringt den Kurs darunter.
        stock.Price = 30_000;
        game.AdvanceStocks();
        Assert.Equal(1_000, stock.Price);
        Assert.Equal(20, game.Finance(me).Holdings[2].Count);
        Assert.Equal(cost, game.Finance(me).Holdings[2].PurchaseCost);
    }

    [Fact]
    public void Day_NegativePrice_WrapsLikeThe16BitWord()
    {
        // [ORIGINAL] Gespeichert wird v·3 als 16-Bit-Wort, der nächste Tag liest es vorzeichenlos (0x57C30, 0x578A9).
        // Kurs 10, fallend: 10 − 1 → 10 (Mindestwert) → großes Ereignis −5 = 5 → kleines −10 = −5 → kein Rauschen →
        // unter dem Ziel (vorzeichenlos nicht!) per Zufall und fallend +1 = −4 → Wort 65.524 / 3 = 21.841 DM.
        var random = new ScriptedRandom(0, 5, 0, 1, 0, 10, 0, 3, 0, 1);
        var market = Single(10, 10_000, 0, 5);
        market.Advance(random);
        Assert.Equal([(0, 400), (2, 5), (0, 1), (0, 1), (0, 22), (2, 10), (0, 1), (0, 3), (0, 1), (0, 1)], random.Calls);
        Assert.Equal(21_841, market.Stocks[0].Price);
    }

    /// <summary>
    /// Mehrsaisonprobe (der Zufallsverbrauch der Finanzen ändert sich): zwei Manager über vier Saisonwechsel, der aktive
    /// kauft zu jeder Saison Aktien und verkauft die Hälfte am Saisonende. Kurse bleiben im Bereich, der Verlauf hat jeden
    /// Tag 36 Einträge mit dem Kurs als letztem, Gewinne und Verluste landen im Finanzbuch (Aktien).
    /// </summary>
    [Fact]
    public void MultiSeason_FourChanges_PassiveAndActiveManager()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Passiv", Head = 0, ClubIndex = 2, StartLeague = League.Bundesliga });
        setup.Managers.Add(new ManagerSetup { Name = "Aktiv", Head = 1, ClubIndex = 30, StartLeague = League.Oberliga });
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, 7);
        var passive = game.Managers[0];
        var active = game.Managers[1];
        var firstSeason = game.Season;
        var bases = game.Stocks.Stocks.Select(s => s.Base).ToArray();

        for (var change = 0; change < 4; change++)
        {
            TestSeason.KeepSquad(game, passive);
            TestSeason.KeepSquad(game, active);
            active.Money = Math.Max(active.Money, 1_000_000);
            for (var i = 0; i < 4; i++)
                Assert.True(game.BuyStock(active, i, 50) > 0);
            var trading = game.Finance(active).Book.Current(FinanceCategory.Aktien);

            for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
            {
                Assert.True(guard < 2000, "Saisonende nicht erreicht");
                foreach (var manager in game.Managers)
                    manager.Money = Math.Max(manager.Money, 5_000_000); // keine Entlassung
                game.Continue();
                Assert.All(game.Stocks.Stocks, s =>
                {
                    Assert.Equal(Stock.HistoryLength, s.History.Count);
                    Assert.Equal(s.Price, s.History[^1]);
                    Assert.InRange(s.Price, 1, StockMarket.SplitAbove);
                    Assert.True(s.Base > 0);
                });
            }

            var holdings = game.Finance(active).Holdings;
            long expected = 0;
            for (var i = 0; i < 4; i++)
            {
                var h = holdings[i];
                var count = h.Count / 2;
                expected += (long)count * game.Stocks.Stocks[i].Price - h.PurchaseCost * count / h.Count;
                Assert.Equal(count, game.SellStock(active, i, count));
            }
            Assert.Equal(trading + expected, game.Finance(active).Book.Current(FinanceCategory.Aktien));
            Assert.All(game.Finance(passive).Holdings, h => Assert.Equal(0, h.Count));
            game.StartNextSeason();
        }
        Assert.Equal(firstSeason + 4, game.Season);
        // Der Basiswert wächst um 3/10.000 je Börsentag (ohne Ereignisse etwa 11 % im Jahr).
        Assert.True(game.Stocks.Stocks.Count(s => s.Base > 0) == 4);
        Assert.True(game.Stocks.Stocks.Zip(bases).Count(p => p.First.Base > p.Second) >= 2, "Basiswerte wachsen nicht");
    }

    /// <summary>
    /// Migration 9 → 10: ein Stand mit dem alten Kursmodell (Mittelwert, Tendenz in DM, 110 Tage Verlauf) lädt; der
    /// Basiswert ist Mittelwert · 100, die Tendenz neutral, der Verlauf auf 36 Tage gekürzt; danach läuft die Börse weiter.
    /// </summary>
    [Fact]
    public void Migration_FromSchema9_OldStockModel()
    {
        var game = TestGames.Create(3);
        var root = JsonNode.Parse(SaveGame.Save(game))!.AsObject();
        Assert.Equal(SaveGame.SchemaVersion, (int)root["schema"]!);
        root["schema"] = 9;
        var stocks = root["spiel"]!["_stocks"]!["Stocks"]!["items"]!.AsArray();
        int[] means = [101, 120, 79, 174];
        for (var i = 0; i < 4; i++)
        {
            var stock = stocks[i]!.AsObject();
            stock.Remove("Base");
            stock.Remove("Direction");
            stock.Remove("Countdown");
            stock["BasePrice"] = means[i];
            stock["Trend"] = 0.5;
            stock["Exact"] = null;
            stock["Price"] = means[i] + 3;
            var items = stock["History"]!["items"]!.AsArray();
            items.Clear();
            for (var k = 0; k < 110; k++)
                items.Add(k);
        }

        var result = SaveGame.Load(System.Text.Encoding.UTF8.GetBytes(root.ToJsonString()));
        Assert.True(result.Success, result.Error);
        var loaded = result.Game!;
        for (var i = 0; i < 4; i++)
        {
            var s = loaded.Stocks.Stocks[i];
            Assert.Equal(means[i] * 100L, s.Base);
            Assert.Equal((1, 0), (s.Direction, s.Countdown));
            Assert.Equal(means[i] + 3, s.Price);
            Assert.Equal(Enumerable.Range(110 - Stock.HistoryLength, Stock.HistoryLength), s.History);
        }
        loaded.AdvanceStocks();
        Assert.All(loaded.Stocks.Stocks, s => Assert.Equal(Stock.HistoryLength, s.History.Count));
    }
}
