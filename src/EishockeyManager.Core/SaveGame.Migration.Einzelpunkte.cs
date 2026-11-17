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

    /// <summary>
    /// Schema 57 → 58 (A582): Jeder Werbevertrag bekommt <c>Cancellable</c> = false. [APPROX] Ältere Stände kennen „Kündbar.“ nicht; das
    /// Kennzeichen entsteht beim nächsten Aufstieg.
    /// </summary>
    private static JsonObject MigrateKuendbar(JsonObject root)
    {
        AddCancellable(root);
        return root;
    }

    private static void AddCancellable(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject obj:
                if (obj.ContainsKey("Kind") && obj.ContainsKey("Sponsor") && obj.ContainsKey("MonthlyAmount") && obj.ContainsKey("MonthsLeft")
                    && !obj.ContainsKey("Cancellable"))
                {
                    obj["Cancellable"] = false;
                    SortFields(obj);
                }
                foreach (var child in obj.Select(kv => kv.Value).ToList())
                    AddCancellable(child);
                break;
            case JsonArray array:
                foreach (var child in array)
                    AddCancellable(child);
                break;
        }
    }

    /// <summary>
    /// Schema 60 → 61 (A665): Jeder Saisonrückblick bekommt <c>InnerMoves</c> = leer. [APPROX] Ältere Stände haben keine Platzverschiebung
    /// innerhalb der unteren Liga gespeichert; der Sonderfall (Zwangsabsteiger schon unter den Plätzen 3/4) bleibt dort wie bisher.
    /// </summary>
    private static JsonObject MigrateInnerMoves(JsonObject root)
    {
        var added = false;
        AddInnerMoves(root, ref added);
        if (added)
            Renumber(root, new Dictionary<int, int>(), [0]); // neue Objektnummern wie in MigrateHokPlayOffMarken
        return root;
    }

    private static void AddInnerMoves(JsonNode? node, ref bool added)
    {
        switch (node)
        {
            case JsonObject obj:
                if (obj.ContainsKey("FinalTables") && obj.ContainsKey("PromotedInto") && obj.ContainsKey("RelegatedInto")
                    && !obj.ContainsKey("InnerMoves"))
                {
                    obj["InnerMoves"] = new JsonObject { ["$id"] = NewObjectId, ["items"] = new JsonArray() }; // Liste wie in den übrigen Migrationen
                    SortFields(obj);
                    added = true;
                }
                foreach (var child in obj.Select(kv => kv.Value).ToList())
                    AddInnerMoves(child, ref added);
                break;
            case JsonArray array:
                foreach (var child in array)
                    AddInnerMoves(child, ref added);
                break;
        }
    }
}
