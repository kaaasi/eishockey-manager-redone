using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration Schema 6 → 7 (Spur finanzen-werbung, #132/#133): gespeicherte Werbebewertung je Manager
/// (<see cref="ManagerFinance.SponsorRating"/>, −1 = beim nächsten Gebrauch berechnen), Bewertung beim Angebot
/// (<see cref="SponsorOffer.Rating"/>) und beim Abschluss (<see cref="SponsorContract.SignedRating"/>). Die zweite
/// Eistafel (<see cref="AdSlotKind.Eisflaeche2"/>) braucht keine Daten; alte Eisverträge gelten für Tafel 1.
/// [APPROX] Ältere Spielstände kennen die Bewertung beim Abschluss nicht: 0, also führt eine Verhandlung immer zur
/// Frage. Offene Verlängerungsfragen im Posteingang bleiben beantwortbar (<see cref="MessageKind.SponsorRenewal"/>).
/// Unerwartete Formen bleiben unverändert; der Reader meldet sie.
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigrateFrom6(JsonObject root)
    {
        if (root["manager"] is JsonArray managers)
            foreach (var node in managers)
                if (node is JsonObject manager && manager["FinanceData"] is JsonObject finance)
                {
                    finance["SponsorRating"] ??= -1;
                    SortFields(finance);
                }
        Migrate6Walk(root, []);
        return root;
    }

    private static void Migrate6Walk(JsonNode? node, HashSet<int> handled)
    {
        switch (node)
        {
            case JsonObject obj:
                if (obj["$id"] is JsonValue idValue && idValue.TryGetValue<int>(out var id) && !handled.Add(id))
                    return;
                if (obj.ContainsKey("Kind") && obj.ContainsKey("Sponsor") && obj.ContainsKey("MonthlyAmount"))
                {
                    // Angebot (Months) bzw. Vertrag (MonthsLeft).
                    if (obj.ContainsKey("Months"))
                        obj["Rating"] ??= 0;
                    if (obj.ContainsKey("MonthsLeft"))
                        obj["SignedRating"] ??= 0;
                    SortFields(obj);
                }
                foreach (var (_, child) in obj.ToArray())
                    Migrate6Walk(child, handled);
                break;
            case JsonArray array:
                foreach (var child in array.ToArray())
                    Migrate6Walk(child, handled);
                break;
        }
    }
}
