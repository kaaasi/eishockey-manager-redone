using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration der Spur kader-rest (Runde 17, Teil 1; Schema 49 → 50, vorläufig – die Leitstelle nummeriert beim Merge):
/// Poolform (<see cref="Player.PoolForm"/>, Pool +0x17, A066). [APPROX] Ältere Stände kennen sie nicht: null (= Form des
/// Kaderplatzes); eine dort schon gesunkene Form verletzter Spieler bleibt, wie sie ist.
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigrateKaderRest(JsonObject root)
    {
        if (root["spieler"] is not JsonArray players)
            return root;
        foreach (var player in players.OfType<JsonObject>())
        {
            if (!player.ContainsKey("PoolForm"))
                player["PoolForm"] = null;
            SortFields(player);
        }
        return root;
    }
}
