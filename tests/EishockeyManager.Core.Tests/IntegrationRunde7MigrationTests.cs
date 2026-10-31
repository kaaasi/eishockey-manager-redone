using System.Text.Json.Nodes;
using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Integration Runde 7: boerse-start und spielberechnung reichten beide die Migration 9 → 10 ein. boerse-start bleibt
/// 9 → 10 (<c>SaveGame.Migration9.cs</c>), spielberechnung wird 10 → 11 (<c>SaveGame.Migration10.cs</c>). Schema 10 gibt
/// es damit in zwei Formen; beide und die echte Kette 9 → 10 → 11 müssen laden (Lehre aus R5-01 #130, R6-01).
/// </summary>
public class IntegrationRunde7MigrationTests
{
    private static readonly string[] NewStockFields = ["Base", "Direction", "Countdown"];
    private static readonly int[] Means = [101, 120, 79, 174];

    private static (GameState Game, JsonObject Root) Saved()
    {
        var game = TestGames.Create(3);
        TestSeason.PlayMatchDay(game);
        return (game, JsonNode.Parse(SaveGame.Save(game))!.AsObject());
    }

    /// <summary>Kursmodell vor boerse-start: Mittelwert, Tendenz in DM, 110 Tage Verlauf.</summary>
    private static void OldStockModel(JsonObject root)
    {
        var stocks = root["spiel"]!["_stocks"]!["Stocks"]!["items"]!.AsArray();
        for (var i = 0; i < 4; i++)
        {
            var stock = stocks[i]!.AsObject();
            foreach (var key in NewStockFields)
                Assert.True(stock.Remove(key));
            stock["BasePrice"] = Means[i];
            stock["Trend"] = 0.5;
            stock["Exact"] = null;
            stock["Price"] = Means[i] + 3;
            var items = stock["History"]!["items"]!.AsArray();
            items.Clear();
            for (var k = 0; k < 110; k++)
                items.Add(Means[i]);
        }
    }

    private static int StripGoalieCounters(JsonObject root)
    {
        var removed = 0;
        foreach (var node in root["spieler"]!.AsArray())
        {
            var player = node!.AsObject();
            if (player.Remove("GoalsAgainst") & player.Remove("GoalieMinutes"))
                removed++;
        }
        return removed;
    }

    private static GameState Load(JsonObject root)
    {
        var result = SaveGame.Load(System.Text.Encoding.UTF8.GetBytes(root.ToJsonString()));
        Assert.True(result.Success, result.Error);
        return result.Game!;
    }

    private static void AssertMigratedStocks(GameState loaded)
    {
        for (var i = 0; i < 4; i++)
        {
            Assert.Equal(Means[i] * 100L, loaded.Stocks.Stocks[i].Base);
            Assert.Equal(Stock.HistoryLength, loaded.Stocks.Stocks[i].History.Count);
        }
    }

    /// <summary>Schema 10 aus der Spur spielberechnung: Torhüterzähler vorhanden, altes Kursmodell.</summary>
    [Fact]
    public void SchemaTen_FromMatchEngineTrack_IsMigrated_AndKeepsGoalieCounters()
    {
        var (game, root) = Saved();
        var goalie = game.Clubs.SelectMany(c => c.Players).First(p => p.GoalieMinutes > 0);
        root["schema"] = 10;
        OldStockModel(root);

        var loaded = Load(root);
        AssertMigratedStocks(loaded);
        var same = loaded.Clubs.SelectMany(c => c.Players).First(p => p.Name == goalie.Name && p.GoalieMinutes > 0);
        Assert.Equal((goalie.GoalsAgainst, goalie.GoalieMinutes), (same.GoalsAgainst, same.GoalieMinutes));
    }

    /// <summary>Echter Schema-9-Stand: altes Kursmodell, keine Torhüterzähler – Kette 9 → 10 → 11.</summary>
    [Fact]
    public void SchemaNine_IsMigrated_ThroughTenToEleven()
    {
        var (game, _) = Saved();
        foreach (var p in game.Clubs.SelectMany(c => c.Players))
            p.GoalsAgainst = p.GoalieMinutes = 0;
        var root = JsonNode.Parse(SaveGame.Save(game))!.AsObject();
        root["schema"] = 9;
        OldStockModel(root);
        Assert.True(StripGoalieCounters(root) > 0);

        var loaded = Load(root);
        AssertMigratedStocks(loaded);
        Assert.All(loaded.Clubs.SelectMany(c => c.Players), p => Assert.Equal((0, 0), (p.GoalsAgainst, p.GoalieMinutes)));
        var again = SaveGame.Save(loaded);
        Assert.Equal(again, SaveGame.Save(Load(JsonNode.Parse(again)!.AsObject())));
    }
}
