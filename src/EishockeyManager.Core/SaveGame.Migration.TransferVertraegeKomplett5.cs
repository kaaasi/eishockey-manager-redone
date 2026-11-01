using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration der Spur transfer-vertraege-komplett, Teil 5 (Schema 21 → 22, Integration Runde 9): Spielerpool
/// des Transfermarkts (<see cref="TransferMarket.Pool"/>, mit den Datenbanknamen <c>_databaseNames</c>) und Herkunft eines Markt-Eintrags aus dem Pool
/// (<see cref="TransferListing.FromPool"/>). Ältere Stände haben keinen Pool: leere Liste – der erste Listenwechsel baut ihn
/// (<see cref="TransferMarket.ChangeList"/>); ihre Markt-Einträge stammen aus Kadern: false.
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigrateTransferVertraegeKomplett5(JsonObject root)
    {
        var added = false;
        foreach (var market in MarketObjects(root))
        {
            if (!market.ContainsKey("_pool"))
            {
                market["_pool"] = new JsonObject { ["$id"] = NewObjectId, ["items"] = new JsonArray() };
                added = true;
            }
            // R9T-P1-01 (#345): Datenbanknamen des Pools (Teil 5, „jeder Name nur einmal“) fehlen älteren Ständen ebenso;
            // leer – der erste Listenwechsel baut sie mit dem Pool.
            if (!market.ContainsKey("_databaseNames"))
            {
                market["_databaseNames"] = new JsonObject { ["$id"] = NewObjectId, ["items"] = new JsonArray() };
                added = true;
            }
            SortFields(market);
        }
        if (added)
            Renumber(root, new Dictionary<int, int>(), [0]);
        foreach (var listing in ListingObjects(root))
        {
            listing["FromPool"] ??= false;
            SortFields(listing);
        }
        return root;
    }

    /// <summary>Objekte des Transfermarkts, an den Feldern der Liste und der Vertragslosen erkannt.</summary>
    private static List<JsonObject> MarketObjects(JsonNode? node)
    {
        var found = new List<JsonObject>();
        Collect(node);
        return found;

        void Collect(JsonNode? current)
        {
            switch (current)
            {
                case JsonObject obj:
                    if (obj.ContainsKey("_listings") && obj.ContainsKey("_freeAgents"))
                        found.Add(obj);
                    foreach (var (_, child) in obj.ToArray())
                        Collect(child);
                    break;
                case JsonArray array:
                    foreach (var child in array)
                        Collect(child);
                    break;
            }
        }
    }
}
