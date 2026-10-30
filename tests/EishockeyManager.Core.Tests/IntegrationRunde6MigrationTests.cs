using System.Text.Json.Nodes;
using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Integration Runde 6: Beide Spuren reichten die Migration 6 → 7 ein. finanzen-werbung bleibt 6 → 7
/// (<c>SaveGame.Migration6.cs</c>), heimspiel-statistik wird 7 → 8 (<c>SaveGame.Migration7.cs</c>). Geprüft werden ein
/// echter Spielstand von Schema 6 (Kette 6 → 7 → 8) und die zweite Form von Schema 7 aus der Spur heimspiel-statistik
/// (Statistik vorhanden, Werbebewertung fehlt) – Lehre aus R5-01 (#130).
/// </summary>
public class IntegrationRunde6MigrationTests
{
    private static readonly string[] SponsorFields = ["SponsorRating", "Rating", "SignedRating"];

    private static (GameState Game, JsonObject Root) Saved(bool withStatistics)
    {
        var game = TestGames.Create(3);
        var me = game.ActiveManager;
        game.Finance(me).Stadium.SetLevel(StadiumFeature.Kuehlanlage, 2); // Eistafel 2 braucht die Kühlanlage
        game.Ads(me).Offers[AdSlotKind.Eisflaeche2] = new() { [4] = new SponsorOffer(AdSlotKind.Eisflaeche2, 4, 12, 1, 3_000, 12, 30) };
        Assert.NotNull(game.AcceptOffer(me, new AdSlot(AdSlotKind.Eisflaeche2), 4));
        // Ohne Statistik bleibt das Feld null; ein entferntes Objekt brächte die Objektnummern durcheinander.
        if (withStatistics)
        {
            game.Finance(me).HomeStatistics.Add(3_049, 5);
            game.Finance(me).HomeStatistics.Add(1_046, 7);
        }
        return (game, JsonNode.Parse(SaveGame.Save(game))!.AsObject());
    }

    private static int Strip(JsonNode? node, IReadOnlyCollection<string> keys)
    {
        var removed = 0;
        switch (node)
        {
            case JsonObject obj:
                foreach (var key in keys)
                    if (obj.Remove(key))
                        removed++;
                foreach (var (_, child) in obj.ToArray())
                    removed += Strip(child, keys);
                break;
            case JsonArray array:
                foreach (var child in array.ToArray())
                    removed += Strip(child, keys);
                break;
        }
        return removed;
    }

    private static GameState Load(JsonObject root)
    {
        var result = SaveGame.Load(System.Text.Encoding.UTF8.GetBytes(root.ToJsonString()));
        Assert.True(result.Success, result.Error);
        return result.Game!;
    }

    /// <summary>Echter Schema-6-Stand: weder Werbebewertung noch Zuschauerstatistik.</summary>
    [Fact]
    public void SchemaSix_IsMigrated_ThroughSevenToEight()
    {
        var (_, root) = Saved(withStatistics: false);
        root["schema"] = 6;
        Assert.True(Strip(root, SponsorFields) > 2);
        Assert.True(Strip(root, ["_homeStatistics"]) > 0);

        var loaded = Load(root);
        var me = loaded.ActiveManager;
        Assert.Equal(-1, loaded.Finance(me).SponsorRating);
        Assert.Equal(0, Assert.Single(loaded.Ads(me).Contracts).SignedRating);
        Assert.Equal(0, loaded.Finance(me).HomeStatistics.Games);
        Assert.Equal(StadiumStatistics.InitialMinimum, loaded.Finance(me).HomeStatistics.Minimum);
        var again = SaveGame.Save(loaded);
        Assert.Equal(again, SaveGame.Save(Load(JsonNode.Parse(again)!.AsObject())));
    }

    /// <summary>Schema 7 aus der Spur heimspiel-statistik: Statistik vorhanden, Werbebewertung fehlt.</summary>
    [Fact]
    public void SchemaSeven_FromHomeStatisticsTrack_IsMigrated_AndKeepsStatistics()
    {
        var (game, root) = Saved(withStatistics: true);
        Assert.True(Strip(root, SponsorFields) > 2);
        root["schema"] = 7;

        var loaded = Load(root);
        var me = loaded.ActiveManager;
        Assert.Equal(-1, loaded.Finance(me).SponsorRating);
        Assert.Equal(0, Assert.Single(loaded.Ads(me).Contracts).SignedRating);
        var original = game.Finance(game.ActiveManager).HomeStatistics;
        Assert.Equal(original.Games, loaded.Finance(me).HomeStatistics.Games);
        Assert.Equal(2, loaded.Finance(me).HomeStatistics.Games);
        Assert.Equal(original.Minimum, loaded.Finance(me).HomeStatistics.Minimum);
        Assert.Equal(original.Record, loaded.Finance(me).HomeStatistics.Record);
    }
}
