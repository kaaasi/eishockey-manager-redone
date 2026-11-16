using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration Schema 51 → 52 (Runde 21, Spur zusehen, Teil 1): Markierung zum Zusehen je Verein (<see cref="Club.WatchMode"/>,
/// Vereinsfeld 0x34 Bits 6–7). Ältere Stände kannten nur die feste Regel „Managervereine und Land des Bundestrainers“; sie
/// bekommen genau diese Vereine im Modus 2 (Vereine nicht entlassener Manager, <c>NationalCoach.Nation</c>), alle anderen 0 – die
/// Live-Anzeige bleibt damit wie vor dem Laden. Unerwartete Formen bleiben unverändert; der Reader meldet sie.
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigrateZusehen(JsonObject root)
    {
        var watched = new HashSet<int>();
        if (root["manager"] is JsonArray managers)
            foreach (var manager in managers.OfType<JsonObject>())
                if (!ZusehenIsTrue(manager["Dismissed"]) && ZusehenInt(manager["ClubIndex"]) is { } club)
                    watched.Add(club);
        if (root["spiel"] is JsonObject game && game["NationalCoach"] is JsonObject coach && ZusehenInt(coach["Nation"]) is { } nation)
            watched.Add(nation);
        if (root["vereine"] is not JsonArray clubs)
            return root;
        foreach (var club in clubs.OfType<JsonObject>())
        {
            club["WatchMode"] ??= ZusehenInt(club["Index"]) is { } index && watched.Contains(index) ? GameState.WatchLive : GameState.WatchOff;
            SortFields(club);
        }
        return root;
    }

    private static bool ZusehenIsTrue(JsonNode? node) => node is JsonValue v && v.TryGetValue<bool>(out var b) && b;

    private static int? ZusehenInt(JsonNode? node) => node is JsonValue v && v.TryGetValue<int>(out var i) ? i : null;
}
