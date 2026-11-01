using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration der Spur statistiken-verein (Runde 8, Welle 3; Schema 14 → 15, bestätigt in der Integration):
/// laufende Serien je Verein (<see cref="Club.SeriesCurrent"/>), Rekordserien und Über-/Unterzahl je Manager
/// (<see cref="Manager.SeriesRecords"/>, <see cref="Manager.PowerPlayCounts"/>). [APPROX] Ältere Spielstände haben nichts
/// gezählt und beginnen mit 0; ab dem nächsten Ligaspiel wird gezählt. Unerwartete Formen bleiben unverändert; der Reader
/// meldet sie.
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigrateStatistikenVerein(JsonObject root)
    {
        if (root["vereine"] is JsonArray clubs)
            foreach (var club in clubs.OfType<JsonObject>())
            {
                club["SeriesCurrent"] ??= Zeros(Series.Count);
                SortFields(club);
            }
        if (root["manager"] is JsonArray managers)
            foreach (var manager in managers.OfType<JsonObject>())
            {
                manager["SeriesRecords"] ??= Zeros(Series.Count);
                manager["PowerPlayCounts"] ??= Zeros(PowerPlay.Count);
                SortFields(manager);
            }
        // Objektnummern laufen in Dateireihenfolge fortlaufend; Verweise folgen der neuen Nummerierung.
        Renumber(root, new Dictionary<int, int>(), [0]);
        return root;
    }

    /// <summary>Neues Feld <c>int[]</c> wie der Writer (Referenzobjekt mit <c>$id</c> und <c>items</c>).</summary>
    private static JsonObject Zeros(int count)
    {
        var items = new JsonArray();
        for (var i = 0; i < count; i++)
            items.Add(0);
        return new JsonObject { ["$id"] = NewObjectId, ["items"] = items };
    }
}
