using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration Schema 55 → 56 (Runde 22, Spur hok-play-off-marken, A612; Nummer nach `kaderplatz-werte` Schema 55): Marken der Play-off-Spiele
/// (<see cref="PlayoffSeries.Marks"/>). Ältere Stände kennen nur Gleichstand + Penaltysieger: zwei Marken bei
/// <see cref="KnockoutMatch.PenaltyWinner"/> (Gleichstand nach 60 Minuten, <see cref="PlayoffSeries.MarksFromReport"/>), sonst 0 –
/// wie <see cref="PlayoffSeries.MarksOf"/> ohne Eintrag. [APPROX] Ein früher in der Verlängerung entschiedenes Spiel bekommt 0
/// (der Stand führt die Verlängerung nicht), ein importiertes Spiel verliert seine Marken (der Import zog sie bisher ab).
/// Spielstände bleiben unverändert; unerwartete Formen lässt die Migration stehen, der Reader meldet sie.
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigrateHokPlayOffMarken(JsonObject root)
    {
        var byId = new Dictionary<int, JsonObject>();
        var series = new List<JsonObject>();
        Collect(root);

        var added = false;
        foreach (var s in series)
        {
            if ((s["Games"] as JsonObject)?["items"] is not JsonArray games)
                continue;
            var items = new JsonArray();
            foreach (var game in games)
                items.Add(Resolve(game)?["PenaltyWinner"] is JsonValue ? PlayoffSeries.TieMarks : 0);
            s["Marks"] = new JsonObject { ["$id"] = NewObjectId, ["items"] = items };
            SortFields(s);
            added = true;
        }
        if (added)
            Renumber(root, new Dictionary<int, int>(), [0]);
        return root;

        JsonObject? Resolve(JsonNode? node) =>
            node is JsonObject obj && obj["$ref"] is JsonValue r && r.TryGetValue<int>(out var id)
                ? byId.GetValueOrDefault(id)
                : node as JsonObject;

        void Collect(JsonNode? current)
        {
            switch (current)
            {
                case JsonObject obj:
                    if (obj["$id"] is JsonValue v && v.TryGetValue<int>(out var id))
                        byId[id] = obj;
                    if (obj.ContainsKey("HomeClub") && obj.ContainsKey("AwayClub") && obj.ContainsKey("Games") && !obj.ContainsKey("Marks"))
                        series.Add(obj);
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
