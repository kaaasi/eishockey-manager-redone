using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration Schema 10 → 11 (Spur spielberechnung; in der Integration Runde 7 von 9 → 10 umnummeriert, weil
/// boerse-start 9 → 10 belegt): Torhüterzähler je Spieler (<see cref="Player.GoalsAgainst"/>,
/// <see cref="Player.GoalieMinutes"/>; im Original die Spielerfelder 0x1B und 0x1F beim Torhüter). Ältere Spielstände
/// haben noch keine gezählten Ereignisse: 0. Unerwartete Formen bleiben unverändert; der Reader meldet sie.
/// <para>
/// Schema 10 gibt es in zwei Formen (wie R5-01, R6-01): aus der Spur boerse-start (neues Kursmodell, keine
/// Torhüterzähler) und aus der Spur spielberechnung (Torhüterzähler, altes Kursmodell mit <c>BasePrice</c>). Der Schritt
/// 9 → 10 der Börse wirkt nur auf Aktien mit <c>BasePrice</c> und wird deshalb hier immer zuerst nachgeholt.
/// </para>
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigrateFrom10(JsonObject root)
    {
        root = MigrateFrom9(root);
        if (root["spieler"] is not JsonArray players)
            return root;
        foreach (var player in players.OfType<JsonObject>())
        {
            if (!player.ContainsKey("Penalties5"))
                continue;
            player["GoalsAgainst"] ??= 0;
            player["GoalieMinutes"] ??= 0;
            SortFields(player);
        }
        return root;
    }
}
