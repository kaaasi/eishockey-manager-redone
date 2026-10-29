using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration Schema 2 → 3 (Spur wettbewerbe-weltkugel): Spielmodus mit Fahne/Weltkugel und Buchungsliste des
/// Nationalkontos. Unerwartete Formen bleiben unverändert; der Reader meldet sie danach als beschädigt.
/// </summary>
public static partial class SaveGame
{
    /// <summary>Platzhalter für die Objektnummer eines neu eingefügten Objekts (vor dem Neunummerieren).</summary>
    private const int NewObjectId = -1;

    private static JsonObject MigrateFrom2(JsonObject root)
    {
        if (root["spiel"] is not JsonObject game)
            return root;

        // Schema 2 kannte nur Modi mit Vereinssaison: Der linke Knopf hieß „DEB-Pokal“ und schaltete nur den Pokal.
        // Die Fahne ist also gedrückt; der Pokalschalter bleibt, wie er war.
        if (game["Setup"] is JsonObject setup)
        {
            setup["ClubCompetitions"] = true;
            SortFields(setup);
        }

        // Nationalkonto: Der bisherige Stand wird eine Übertragsbuchung, damit Konto = Summe der Buchungen bleibt.
        var items = new JsonArray();
        if (game["NationalAccount"] is JsonValue accountValue && accountValue.TryGetValue<long>(out var account) && account != 0)
        {
            var season = game["Season"] is JsonValue seasonValue && seasonValue.TryGetValue<int>(out var s) ? s : 0;
            items.Add(new JsonObject
            {
                ["$id"] = NewObjectId,
                ["Amount"] = account,
                ["Kind"] = nameof(NationalBookingKind.Carryover),
                ["Season"] = season,
            });
        }
        game["NationalBookings"] = new JsonObject { ["$id"] = NewObjectId, ["items"] = items };
        SortFields(game);

        // Objektnummern laufen in Dateireihenfolge fortlaufend; Verweise folgen der neuen Nummerierung.
        Renumber(root, new Dictionary<int, int>(), [0]);
        return root;
    }

    /// <summary>Felder eines Objekts wie der Writer ordnen: Kopf (<c>$id</c>, <c>$type</c>), dann Namen ordinal.</summary>
    private static void SortFields(JsonObject obj)
    {
        var fields = obj.ToList();
        obj.Clear();
        foreach (var (key, value) in fields.OrderBy(f => f.Key switch { "$id" => 0, "$type" => 1, _ => 2 })
                     .ThenBy(f => f.Key, StringComparer.Ordinal))
            obj[key] = value;
    }

    /// <summary>Objektnummern in Dateireihenfolge neu vergeben und <c>$ref</c>-Verweise umschreiben.</summary>
    private static void Renumber(JsonNode? node, Dictionary<int, int> map, int[] next)
    {
        switch (node)
        {
            case JsonObject obj:
                if (obj["$id"] is JsonValue idValue && idValue.TryGetValue<int>(out var id))
                {
                    if (id != NewObjectId)
                        map[id] = next[0];
                    obj["$id"] = next[0]++;
                }
                else if (obj.Count == 1 && obj["$ref"] is JsonValue refValue && refValue.TryGetValue<int>(out var target)
                         && map.TryGetValue(target, out var renumbered))
                    obj["$ref"] = renumbered;
                foreach (var (key, child) in obj.ToArray())
                    if (key is not "$id" and not "$ref")
                        Renumber(child, map, next);
                break;
            case JsonArray array:
                foreach (var child in array.ToArray())
                    Renumber(child, map, next);
                break;
        }
    }
}
