using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration Schema 44 → 45 (#688, R688-04, Leitstelle Runde 14): <see cref="GameState.DayOpened"/>. Die Zeitleiste rechnet den
/// Tageswechsel (Tagesabschluss, Takt 0x07169, Listenwechsel) jetzt wie im Original vor dem Halt, also vor den Managermenüs. Ältere
/// Stände sind vor diesem Tageswechsel gesichert: false, dann holt <c>ProcessDay</c> ihn vor den Spielen des Tages nach (wie bisher).
/// Unerwartete Formen bleiben unverändert; der Reader meldet sie.
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigrateTagestakt(JsonObject root)
    {
        if (root["spiel"] is JsonObject game)
        {
            game["DayOpened"] ??= false;
            SortFields(game);
        }
        return root;
    }
}
