using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration der Spur einzelpunkte (Runde 22, Welle 2; Schema 58 → 59, nach `kader-regeln`):
/// <see cref="Manager.OneSeasonFlag"/> (A847, Managerbyte 0x9744) und <see cref="Lineup.GoalieStandIn"/> (A672, Feldspieler im Tor).
/// [APPROX] Ältere Stände kennen beides nicht: false bzw. niemand (die nächste Neuaufstellung setzt den Feldspieler im Tor neu).
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigrateEinzelpunkte(JsonObject root)
    {
        if (root["manager"] is JsonArray managers)
        {
            foreach (var manager in managers.OfType<JsonObject>())
            {
                manager["OneSeasonFlag"] ??= false;
                SortFields(manager);
            }
        }
        AddStandIn(root);
        return root;
    }

    /// <summary>Jede Aufstellung (Objekt mit <c>_goalkeeper</c>) bekommt das Feld <c>_goalieStandIn</c>.</summary>
    private static void AddStandIn(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject obj:
                if (obj.ContainsKey("_goalkeeper") && !obj.ContainsKey("_goalieStandIn"))
                {
                    obj["_goalieStandIn"] = null;
                    SortFields(obj);
                }
                foreach (var child in obj.Select(kv => kv.Value).ToList())
                    AddStandIn(child);
                break;
            case JsonArray array:
                foreach (var child in array)
                    AddStandIn(child);
                break;
        }
    }
}
