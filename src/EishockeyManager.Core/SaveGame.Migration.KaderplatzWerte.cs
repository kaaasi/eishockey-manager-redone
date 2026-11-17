using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration der Spur kaderplatz-werte (Runde 22, Teil 1; Schema 54 → 55, vorläufig – die Leitstelle nummeriert beim Merge):
/// Kondition und Technik des Poolsatzes (<see cref="Player.PoolCondition"/>, <see cref="Player.PoolTechnique"/>, Pool +0x15/+0x16,
/// A714), Teil 2 die eingespielte Reihe (<see cref="Player.PlayedLine"/>, Kaderplatz +0x02, A093). [APPROX] Ältere Stände kennen
/// sie nicht: null (= Werte des Kaderplatzes) bzw. 0 (keine Reihe, wie ein neuer Kaderplatz); die Drift bisher gesicherter
/// Saisonwechsel fehlt den Spielern mit Kaderplatz.
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigrateKaderplatzWerte(JsonObject root)
    {
        if (root["spieler"] is not JsonArray players)
            return root;
        foreach (var player in players.OfType<JsonObject>())
        {
            if (!player.ContainsKey("PoolCondition"))
                player["PoolCondition"] = null;
            if (!player.ContainsKey("PoolTechnique"))
                player["PoolTechnique"] = null;
            if (!player.ContainsKey("PlayedLine"))
                player["PlayedLine"] = 0;
            SortFields(player);
        }
        return root;
    }
}
