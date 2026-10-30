using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration Schema 9 → 10 (Spur boerse-start, #169): Kursmodell der Börse nach em.exe. <see cref="Stock"/> verliert
/// <c>BasePrice</c>, <c>Trend</c> und <c>Exact</c> und bekommt den Basiswert (<see cref="Stock.Base"/>, Hundertstel-DM),
/// die Tendenz (<see cref="Stock.Direction"/>) und deren Restdauer (<see cref="Stock.Countdown"/>); der Verlauf ist
/// 36 statt 110 Tage lang. [APPROX] Alte Stände: Basiswert = alter Mittelwert · 100, Tendenz 1 (keine) mit Restdauer 0
/// (am nächsten Tag neu gewürfelt), vom Verlauf bleiben die letzten 36 Tage. Unerwartete Formen bleiben unverändert;
/// der Reader meldet sie.
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigrateFrom9(JsonObject root)
    {
        Migrate9Walk(root, []);
        return root;
    }

    private static void Migrate9Walk(JsonNode? node, HashSet<int> handled)
    {
        switch (node)
        {
            case JsonObject obj:
                if (obj["$id"] is JsonValue idValue && idValue.TryGetValue<int>(out var id) && !handled.Add(id))
                    return;
                if (obj["BasePrice"] is JsonValue basePrice && basePrice.TryGetValue<long>(out var mean)
                    && obj.ContainsKey("Trend") && obj.ContainsKey("History") && obj.ContainsKey("Price"))
                {
                    obj.Remove("BasePrice");
                    obj.Remove("Trend");
                    obj.Remove("Exact");
                    obj["Base"] = mean * 100;
                    obj["Direction"] = 1;
                    obj["Countdown"] = 0;
                    if (obj["History"] is JsonObject { } history && history["items"] is JsonArray items)
                        while (items.Count > Stock.HistoryLength)
                            items.RemoveAt(0);
                    SortFields(obj);
                    return;
                }
                foreach (var (_, child) in obj.ToArray())
                    Migrate9Walk(child, handled);
                break;
            case JsonArray array:
                foreach (var child in array.ToArray())
                    Migrate9Walk(child, handled);
                break;
        }
    }
}
