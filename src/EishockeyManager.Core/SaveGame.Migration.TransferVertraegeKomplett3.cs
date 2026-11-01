using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration der Spur transfer-vertraege-komplett, Teil 3 (Schema 20 → 21, Integration Runde 9):
/// unaufgefordertes Angebot je Spieler (<see cref="Player.UnsolicitedOffer"/>, Kaderplatz +0x09 Bit 0x40 / +0x16). Ältere
/// Stände kennen keins: null.
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigrateTransferVertraegeKomplett3(JsonObject root)
    {
        if (root["spieler"] is not JsonArray players)
            return root;
        foreach (var player in players.OfType<JsonObject>())
        {
            if (player.ContainsKey("UnsolicitedOffer"))
                continue;
            player["UnsolicitedOffer"] = null;
            SortFields(player);
        }
        return root;
    }
}
