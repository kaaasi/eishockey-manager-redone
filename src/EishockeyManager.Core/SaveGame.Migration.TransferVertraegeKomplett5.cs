using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration der Spur transfer-vertraege-komplett, Teil 5 (Schema 21 → 22, Integration Runde 9): Spielerpool
/// des Transfermarkts (<see cref="TransferMarket.Pool"/>) und Herkunft eines Markt-Eintrags aus dem Pool
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
            if (market.ContainsKey("_pool"))
                continue;
            market["_pool"] = new JsonObject { ["$id"] = NewObjectId, ["items"] = new JsonArray() };
            SortFields(market);
            added = true;
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
