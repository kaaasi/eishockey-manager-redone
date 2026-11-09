using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration der Spur saison, Teil 4 (Runde 14; Schema 39 → 40, #349): Einstellung <see cref="GameOptions.ManagerDayMinuteLoop"/>.
/// Ältere Spielstände bekommen die Vorgabe (aus = Remake-Verhalten, Entscheidung R14-D4 = A); ihr Zufallsverbrauch bleibt damit
/// unverändert. Unerwartete Formen bleiben unverändert; der Reader meldet sie.
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigrateSaison4(JsonObject root)
    {
        if (root["spiel"] is not JsonObject game || game["Setup"] is not JsonObject setup || setup["Options"] is not JsonObject options)
            return root;
        if (!options.ContainsKey("ManagerDayMinuteLoop"))
        {
            options["ManagerDayMinuteLoop"] = false;
            SortFields(options);
        }
        return root;
    }
}
