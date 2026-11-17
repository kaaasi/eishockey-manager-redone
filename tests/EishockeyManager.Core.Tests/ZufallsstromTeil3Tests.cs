using System.Text;
using System.Text.Json.Nodes;
using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur zufallsstrom, Teil 3 (N3 = B), Tageslauf und Managerzug: Lösen der Transfersperre (A521), Tendenz im Tageslauf (A524),
/// Faktor je Managerzug (A533) ziehen aus dem gemeinsamen Strom <see cref="GameState.Random"/>, an der Stelle des Originals; die
/// Quellen <c>TrendRandom</c> und <c>TurnRandom</c> entfallen (Rest von A725, Spielstand Schema 53). Vor der Spur rot: gezogen
/// wurde aus <c>ContractRandom</c>, <c>TrendRandom</c> und <c>TurnRandom</c>.
/// </summary>
public class ZufallsstromTeil3Tests
{
    private static GameState Create(int seed = 3, params int[] clubs)
    {
        var setup = new GameSetup();
        foreach (var (club, i) in (clubs.Length == 0 ? [2] : clubs).Select((c, i) => (c, i)))
            setup.Managers.Add(new ManagerSetup { Name = $"Trainer {i}", Head = i, ClubIndex = club });
        return NewGame.Create(TestGames.CreateDatabase(), setup, seed);
    }

    private static GameRandom Copy(GameRandom random) => GameRandom.FromState(random.Capture());

    /// <summary>
    /// A521: 0x07169 im Lauf von Manager 0 (Datei 0x0718F), je Markt-Platz nach dem Verfall: ist allein Manager 0 gesperrt
    /// (+0x1A &amp; 0x7F = 1), löst random(0, 2) = 0 die Sperre (Datei 0x071CC–0x07202, Wurf 0x071EC) – ein Wurf je gesperrtem
    /// Platz aus dem gemeinsamen Strom, vor den Verträgen. Der Vertragsteil (<c>AnnounceContractEnds</c>) löst keine Sperre mehr.
    /// </summary>
    [Fact]
    public void BidLock_ReleasedInMarketPartOfManagerZero_FromSharedStream()
    {
        var (game, twin) = (Create(3), Create(3));
        var manager = game.Managers[0];
        var day = game.CurrentDay;
        twin.DailyRunMarket(day, twin.Managers[0]);
        // Ohne Sperre, ohne Gebote und ohne eigene Markt-Plätze zieht der Markt-Teil nichts: Die Probe misst nur die Sperre.
        Assert.Equal(game.Random.Capture(), twin.Random.Capture());

        var locked = game.TransferMarket.Listings.Take(3).ToList();
        Assert.Equal(3, locked.Count);
        foreach (var listing in locked)
            listing.RejectedBy = 0x81;
        var contract = game.ComputerSquadRandom.Capture();
        var sim = Copy(game.Random);
        var expected = locked.Select(_ => TransferRules.Random16(sim, 0, 2) == 0 ? 0x80 : 0x81).ToList();

        game.DailyRunMarket(day, manager);

        Assert.Equal(expected, locked.Select(l => l.RejectedBy).ToList());
        Assert.Equal(sim.Capture(), game.Random.Capture());

        foreach (var listing in locked)
            listing.RejectedBy = 0x81;
        for (var run = 0; run < 40; run++)
            game.AnnounceContractEnds(day, manager);
        Assert.All(locked, l => Assert.Equal(0x81, l.RejectedBy));
        Assert.Equal(contract, game.ComputerSquadRandom.Capture());
    }

    /// <summary>
    /// A524: Kaderteil von 0x07169 (Datei 0x07AF2–0x07C07) je Kaderplatz verschränkt: Training 0x0659C (Datei 0x07B5E), dann
    /// Tendenz (Datei 0x07B64–0x07BFC: random(0, 3) bei 0x07B77, am Ende des Zählers 0x4907A) – beides aus dem gemeinsamen Strom.
    /// Der Zwilling rechnet dieselbe Folge Platz für Platz von Hand.
    /// </summary>
    [Fact]
    public void SquadRun_TrainingAndTrendInterleavedPerSlot_FromSharedStream()
    {
        var (game, twin) = (Create(3), Create(3));
        var day = Enumerable.Range(0, 14).First(d => GameState.IsDailyRunDay(d) && !PlayerInjuries.IsHealingDay(DayCounter.Of(d)));
        var other = twin.Managers[0];
        var plan = twin.PrepareTraining(other);
        foreach (var p in twin.Clubs[other.ClubIndex].Players)
        {
            Assert.NotEqual(0, p.TrendControl); // Spielstart: jeder Kaderplatz hat Tendenz und Steuerbyte (0x4907A)
            TrainingRules.Apply(p, plan, other.Level, twin.Random);
            GameState.AdvanceTrend(p, twin.Random);
        }

        game.ManagerSquadRun(day, game.Managers[0]);

        static List<(int, int, int, int, int, int)> Values(GameState g) => g.Clubs[g.Managers[0].ClubIndex].Players
            .Select(p => (p.Condition, p.Technique, p.Form, p.Strain, p.TrendScore, p.TrendControl)).ToList();
        Assert.Equal(Values(twin), Values(game));
        Assert.Equal(twin.Random.Capture(), game.Random.Capture());
    }

    /// <summary>
    /// A533: Faktor der Gehaltsforderung je Managerzug, Datei 0x44E3F: random(0x55, 0x73) aus dem gemeinsamen Strom, einmal am
    /// Zuganfang. Der Aufbau des Hauptmenüs (<see cref="GameState.OnMainMenu"/>, Datei 0x44FBD) zieht nichts.
    /// </summary>
    [Fact]
    public void TurnFactor_DrawnFromSharedStream_MenuBuildDrawsNothing()
    {
        var game = Create(3);
        var sim = Copy(game.Random);
        var expected = ContractRules.TurnFactor(sim);

        game.DrawContractTurnFactor();

        Assert.Equal(expected, game.ContractTurnFactor);
        Assert.Equal(sim.Capture(), game.Random.Capture());

        _ = game.OnMainMenu(game.Managers[0], isReturn: false);
        _ = game.OnMainMenu(game.Managers[0], isReturn: true);
        Assert.Equal(expected, game.ContractTurnFactor);
        Assert.Equal(sim.Capture(), game.Random.Capture());
    }

    /// <summary>
    /// Rest von A725, A533: Spielstand Schema 52 mit den entfallenen Quellen <c>_trendRandom</c> und <c>_turnRandom</c> lädt nach
    /// der Migration 52 → 53; der gemeinsame Strom und der Faktor des laufenden Zugs bleiben.
    /// </summary>
    [Fact]
    public void SaveGame_Schema51_DropsOwnSources()
    {
        var game = Create(3);
        var root = JsonNode.Parse(Encoding.UTF8.GetString(SaveGame.Save(game)))!.AsObject();
        var state = root["spiel"]!.AsObject();
        Assert.False(state.ContainsKey("_trendRandom"));
        Assert.False(state.ContainsKey("_turnRandom"));
        // Stand „Schema 52“ nachbauen: beide Quellen als eigene Objekte an ihrem Platz in der Feldfolge, Objektnummern fortlaufend
        // in Dateireihenfolge (wie der Schreiber von Schema 52), Verweise umgeschrieben.
        root["schema"] = 52;
        var trendSource = state["Random"]!.DeepClone().AsObject();
        var turnSource = state["Random"]!.DeepClone().AsObject();
        trendSource["$id"] = -1;
        turnSource["$id"] = -2;
        state["_trendRandom"] = trendSource;
        state["_turnRandom"] = turnSource;
        var fields = state.ToList();
        state.Clear();
        foreach (var (key, value) in fields.OrderBy(f => f.Key switch { "$id" => 0, "$type" => 1, _ => 2 })
                     .ThenBy(f => f.Key, StringComparer.Ordinal))
            state[key] = value;
        var map = new Dictionary<int, int>();
        var next = 0;
        void Number(JsonNode? node)
        {
            if (node is JsonObject obj)
            {
                if (obj["$id"] is JsonValue id)
                {
                    map[id.GetValue<int>()] = next;
                    obj["$id"] = next++;
                }
                else if (obj.Count == 1 && obj["$ref"] is JsonValue target)
                    obj["$ref"] = map[target.GetValue<int>()];
                foreach (var (key, child) in obj.ToArray())
                    if (key is not "$id" and not "$ref")
                        Number(child);
            }
            else if (node is JsonArray array)
                foreach (var child in array.ToArray())
                    Number(child);
        }
        Number(root);
        Assert.True(turnSource["$id"]!.GetValue<int>() > 0 && turnSource["$id"]!.GetValue<int>() < next - 1,
            "Die Quelle soll mitten in der Nummernfolge stehen, sonst prüft die Probe die Umnummerierung nicht.");

        var loaded = SaveGame.Load(Encoding.UTF8.GetBytes(root.ToJsonString()));

        Assert.True(loaded.Success, loaded.Error);
        Assert.Equal(game.Random.Capture(), loaded.Game!.Random.Capture());
        Assert.Equal(game.ContractTurnFactor, loaded.Game.ContractTurnFactor);
    }
}
