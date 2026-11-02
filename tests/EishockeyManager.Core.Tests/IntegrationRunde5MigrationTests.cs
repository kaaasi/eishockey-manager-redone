using System.Text.Json.Nodes;
using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Integration Runde 5, Befund R5-01 (#130): Es gibt zwei Formen von Schema 5 – aus der Spur taktik-verein (#121, mit
/// den Vereinsfeldern <c>_dressed</c>/<c>_dressedByHand</c>) und aus der Spur bundestrainer-modus4 (#123, mit
/// <c>_nationalTurn</c>, aber ohne die Vereinsfelder und mit Taktikwerten alter Bedeutung). Beide müssen laden.
/// </summary>
public class IntegrationRunde5MigrationTests
{
    /// <summary>Form von #123: Zug des Bundestrainers offen, Vereine ohne „Eingesetzt“, alte Taktikwerte.</summary>
    [Fact]
    public void SchemaFive_FromMode4Track_IsMigrated_AndKeepsOpenTurn()
    {
        // Erster Seed ab 3 mit offenem Zug des Bundestrainers. Vorbedingung über die Stärke der Managerkader (#204: Wanderung
        // der Teamwerte) und den Seed (Zufallsverbrauch, zuletzt Listenwechsel 0x494EF); Integration Runde 9: beides.
        GameState game = null!;
        for (var seed = 3; seed < 40 && game?.NationalTurn != true; seed++)
        {
            game = GameModeTests.Create(seed, true, true, 8, 2);
            ManagedSquads.PlayAtTopStrength(game);
            NationalCoachMode4Tests.PlayToClubSeasonEnd(game);
            game.Continue();
            game.Continue();
        }
        Assert.True(game.NationalTurn);

        var root = JsonNode.Parse(SaveGame.Save(game))!.AsObject();
        root["schema"] = 5;
        foreach (var club in root["vereine"]!.AsArray().Select(n => n!.AsObject()))
        {
            Assert.True(club.Remove("_dressed"));
            Assert.True(club.Remove("_dressedByHand"));
        }
        root["vereine"]![2]!["Tactics"]!["_effort"]!["items"]![0] = -3; // Wertebereich vor taktik-verein
        Assert.True(root["spiel"]!["_nationalTurn"]!.GetValue<bool>());

        var result = SaveGame.Load(System.Text.Encoding.UTF8.GetBytes(root.ToJsonString()));
        Assert.True(result.Success, result.Error);
        var loaded = result.Game!;
        Assert.True(loaded.NationalTurn);
        Assert.Equal(new Tactics().Effort(0, 0), loaded.Clubs[2].Tactics.Effort(0, 0));
        // Teil 8 (A068): Die automatische Aufstellung hält ihre Eingesetzten fest (_dressedByHand = true); Schema 5 kennt
        // sie nicht, die Migration 4 → 5 nimmt „nicht von Hand“ (bis zum nächsten Tageslauf). Verglichen wird ohne „Eingesetzt“.
        Assert.All(loaded.Clubs, c => Assert.Equal(c.Lineup.Players.Count(), c.Lineup.Players.Count(c.IsDressed)));
        Assert.Equal(WithoutDressed(SaveGame.Save(game)), WithoutDressed(SaveGame.Save(loaded)));
    }

    private static string WithoutDressed(byte[] save)
    {
        var root = JsonNode.Parse(save)!.AsObject();
        foreach (var club in root["vereine"]!.AsArray().Select(n => n!.AsObject()))
        {
            club["_dressed"]!["items"] = new JsonArray();
            club["_dressedByHand"] = false;
        }
        return root.ToJsonString();
    }
}
