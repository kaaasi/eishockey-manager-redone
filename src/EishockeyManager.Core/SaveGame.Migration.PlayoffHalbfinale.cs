using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration Schema 43 → 44 (Fixlauf #676, Review R683-01/-04, Leitstelle Runde 14):
/// <list type="bullet">
/// <item><see cref="PlayoffSeries"/>: Felder <c>HigherSeed</c>/<c>LowerSeed</c> heißen <see cref="PlayoffSeries.HomeClub"/>/
///   <see cref="PlayoffSeries.AwayClub"/> (ab dem Halbfinale ist der erste Verein nicht immer der besser Platzierte).</item>
/// <item>Viertelfinale (<c>Playoffs._rounds[0]</c>) in Platzfolge des Originals (Plätze 0, 2, 4, 6 von DGROUP 0x5D3B, also
///   1–8, 2–7, 3–6, 4–5), denn <see cref="Playoffs"/> paart Serie k mit Serie k + 2 (Tabelle DGROUP 0x144). Ältere Stände
///   führten die Serien als 1–8, 4–5, 2–7, 3–6 (eigene Spiele) bzw. 1–8, 3–6, 2–7, 4–5 (Import); ohne Umordnung paarte ein
///   während des Viertelfinales gesicherter Stand ein falsches Halbfinale. Im Viertelfinale ist der erste Verein immer der besser
///   Platzierte, die Ordnung nach seinem Platz in <c>Seeds</c> ist daher eindeutig. Ein schon angelegtes Halbfinale bleibt, wie
///   es ist (das Endspiel paart bei zwei Serien gleich).</item>
/// </list>
/// Unerwartete Formen bleiben unverändert; der Reader meldet sie.
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigratePlayoffHalbfinale(JsonObject root)
    {
        var changed = false;
        foreach (var playoffs in Descendants(root).Where(o => o.ContainsKey("_rounds") && o.ContainsKey("Seeds")).ToList())
        {
            if (playoffs["_rounds"]?["items"] is not JsonArray rounds)
                continue;
            foreach (var series in rounds.OfType<JsonObject>().SelectMany(r => r["items"] as JsonArray ?? []).OfType<JsonObject>())
            {
                if (series["HigherSeed"] is { } home && series["LowerSeed"] is { } away)
                {
                    series.Remove("HigherSeed");
                    series.Remove("LowerSeed");
                    series["HomeClub"] = home;
                    series["AwayClub"] = away;
                    SortFields(series);
                }
            }
            if (playoffs["Seeds"]?["items"] is not JsonArray seedItems || rounds.FirstOrDefault()?["items"] is not JsonArray first)
                continue;
            var seeds = seedItems.Select(s => s is JsonValue v && v.TryGetValue<int>(out var c) ? c : int.MinValue).ToList();
            var place = (JsonNode? s) => s?["HomeClub"] is JsonValue v && v.TryGetValue<int>(out var c) ? seeds.IndexOf(c) : -1;
            var series0 = first.ToList();
            if (series0.Any(s => place(s) < 0))
                continue;
            var ordered = series0.OrderBy(place).ToList();
            if (ordered.SequenceEqual(series0))
                continue;
            first.Clear();
            foreach (var s in ordered)
                first.Add(s);
            changed = true;
        }
        if (changed)
            Renumber(root, new Dictionary<int, int>(), [0]);
        return root;
    }

    private static IEnumerable<JsonObject> Descendants(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject obj:
                yield return obj;
                foreach (var (_, child) in obj)
                    foreach (var d in Descendants(child))
                        yield return d;
                break;
            case JsonArray array:
                foreach (var child in array)
                    foreach (var d in Descendants(child))
                        yield return d;
                break;
        }
    }
}
