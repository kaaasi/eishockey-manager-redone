using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration der Spur transfer-original-vertrag (Runde 8, Welle 3; vorläufig Schema 14 → 15, die Leitstelle nummeriert in
/// der Integration): Verhandlungssperre in Tagen statt des Spieltags der letzten Ablehnung (<see cref="Player.NegotiationLock"/>,
/// Original Kaderplatz +0x18) und Sperre nach abgelehntem Gebot je Markt-Eintrag (<see cref="TransferListing.RejectedBy"/>,
/// Kaderplatz +0x1A). [APPROX] Eine alte Ablehnung („bereits abgelehnt“ nur am selben Spieltag) ist am nächsten Spieltag
/// verfallen und wird nicht in Tage umgerechnet: 0. Märkte älterer Stände haben keine Sperren: 0. Unerwartete Formen bleiben
/// unverändert; der Reader meldet sie.
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigrateTransferOriginalVertrag(JsonObject root)
    {
        if (root["spieler"] is JsonArray players)
            foreach (var player in players.OfType<JsonObject>())
            {
                if (!player.ContainsKey("RejectedOfferDay"))
                    continue;
                player.Remove("RejectedOfferDay");
                player["NegotiationLock"] ??= 0;
                SortFields(player);
            }

        foreach (var listing in ListingObjects(root))
        {
            listing["RejectedBy"] ??= 0;
            SortFields(listing);
        }
        return root;
    }

    /// <summary>Einträge des Transfermarkts (Liste, vertragslose Spieler) an ihren Feldern erkannt, wo immer sie stehen.</summary>
    private static List<JsonObject> ListingObjects(JsonNode? node)
    {
        var found = new List<JsonObject>();
        Collect(node);
        return found;

        void Collect(JsonNode? current)
        {
            switch (current)
            {
                case JsonObject obj:
                    if (obj.ContainsKey("ListedDay") && obj.ContainsKey("IsFreeAgent") && obj.ContainsKey("Price"))
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
