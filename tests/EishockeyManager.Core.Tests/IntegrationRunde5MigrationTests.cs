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
        var game = GameModeTests.Create(3, true, true, 8, 2);
        ManagedSquads.PlayAtTopStrength(game); // Vorbedingung Bundestrainer (#204: Wanderung der Teamwerte)
        NationalCoachMode4Tests.PlayToClubSeasonEnd(game);
        game.Continue();
        game.Continue();
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
        Assert.Equal(SaveGame.Save(game), SaveGame.Save(loaded));
    }
}
