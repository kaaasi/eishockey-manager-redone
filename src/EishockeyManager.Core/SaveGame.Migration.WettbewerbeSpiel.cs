using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration der Spur wettbewerbe-spiel, Teil 1 (vorläufig Schema 22 → 23, die Leitstelle nummeriert in der Integration):
/// Ergebnismarken der Pokalspiele (<see cref="CupTie.Marks"/>, WM-Endrunde <c>_knockoutMarks</c>; <see cref="CupMatch"/>).
/// Ältere Stände kennen nur Gleichstand + Penaltysieger: Marke 1 bei <see cref="KnockoutMatch.PenaltyWinner"/>, sonst 0 –
/// so zeigten sie die Bildschirme schon vorher (DEB-Pokal ein Punkt; die WM-Endrunde zeigt jetzt einen statt zwei).
/// Spielstände bleiben unverändert; unerwartete Formen lässt die Migration stehen, der Reader meldet sie.
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigrateWettbewerbeSpiel(JsonObject root)
    {
        var byId = new Dictionary<int, JsonObject>();
        var ties = new List<JsonObject>();
        var worldCups = new List<JsonObject>();
        Collect(root);

        var added = false;
        foreach (var tie in ties)
        {
            if ((tie["Games"] as JsonObject)?["items"] is not JsonArray games)
                continue;
            tie["Marks"] = new JsonObject { ["$id"] = NewObjectId, ["items"] = MarksOf(games) };
            SortFields(tie);
            added = true;
        }
        foreach (var wm in worldCups)
        {
            if ((wm["_knockout"] as JsonObject)?["items"] is not JsonArray stages)
                continue;
            var marks = new JsonArray();
            foreach (var stage in stages)
                if (Resolve(stage)?["items"] is JsonArray games)
                    marks.Add(new JsonObject { ["$id"] = NewObjectId, ["items"] = MarksOf(games) });
            wm["_knockoutMarks"] = new JsonObject { ["$id"] = NewObjectId, ["items"] = marks };
            SortFields(wm);
            added = true;
        }
        if (added)
            Renumber(root, new Dictionary<int, int>(), [0]);
        return root;

        JsonArray MarksOf(JsonArray games)
        {
            var items = new JsonArray();
            foreach (var game in games)
                items.Add(Resolve(game)?["PenaltyWinner"] is JsonValue ? 1 : 0);
            return items;
        }

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
                    if (obj.ContainsKey("First") && obj.ContainsKey("Second") && obj.ContainsKey("Games") && !obj.ContainsKey("Marks"))
                        ties.Add(obj);
                    if (obj.ContainsKey("_knockout") && obj.ContainsKey("_slots") && !obj.ContainsKey("_knockoutMarks"))
                        worldCups.Add(obj);
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
