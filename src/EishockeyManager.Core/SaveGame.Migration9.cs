using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration Schema 9 → 10 (Spur spielberechnung): Torhüterzähler je Spieler (<see cref="Player.GoalsAgainst"/>,
/// <see cref="Player.GoalieMinutes"/>; im Original die Spielerfelder 0x1B und 0x1F beim Torhüter). Ältere Spielstände
/// haben noch keine gezählten Ereignisse: 0. Unerwartete Formen bleiben unverändert; der Reader meldet sie.
/// Die Leitstelle nummeriert die Schemaversion in der Integration um.
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigrateFrom9(JsonObject root)
    {
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
