using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration der Spur kader-regeln (Runde 22, Teil 1; Schema 57 → 58, nach `national`): Spiel-Level
/// und Start-Stärke sind ein Wert für das ganze Spiel ([ORIGINAL] DGROUP 0x118/0x11B, A054/A518), geführt in
/// <see cref="GameSetup"/> selbst (Felder <c>_level</c>, <c>_startStrength</c>, wie <c>_startLeague</c> seit Schema 42).
/// [APPROX] Ältere Stände konnten je Manager verschiedene Werte haben; alle übernehmen die von Manager 0 (so lesen
/// <see cref="GameSetup.Level"/> und <see cref="GameSetup.StartStrength"/>), auch <see cref="Manager.Level"/> der laufenden Manager.
/// Ohne Manager gelten die Vorgaben des Originals (Level 3, Mittelfeld). Unerwartete Formen bleiben unverändert.
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigrateKaderRegeln(JsonObject root)
    {
        if (root["spiel"] is not JsonObject game || game["Setup"] is not JsonObject setup)
            return root;
        var items = setup["Managers"]?["items"] as JsonArray;
        var first = items is { Count: > 0 } ? items[0] as JsonObject : null;
        var level = first?["Level"] is JsonValue lv && lv.TryGetValue<int>(out var l) ? l : 3;
        var strength = first?["StartStrength"] is JsonValue sv && sv.TryGetValue<string>(out var s) ? s : nameof(StartStrength.Mittelfeld);
        if (!setup.ContainsKey("_level"))
            setup["_level"] = level;
        if (!setup.ContainsKey("_startStrength"))
            setup["_startStrength"] = strength;
        SortFields(setup);
        if (first is null)
            return root;
        foreach (var item in items!.OfType<JsonObject>())
        {
            item["Level"] = level;
            item["StartStrength"] = strength;
        }
        if (root["manager"] is JsonArray managers)
        {
            foreach (var manager in managers.OfType<JsonObject>().Where(m => m.ContainsKey("Level")))
                manager["Level"] = level;
        }
        return root;
    }
}
