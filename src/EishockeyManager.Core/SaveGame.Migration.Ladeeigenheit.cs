using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration Schema 46 → 47 (Runde 14, Spur pruefung-karte Teil 5; Entscheidung Nutzer 09.11.2026, C):
/// <see cref="GameState.LoadCounterOffset"/>. Ältere Stände haben keinen Ladeversatz (0): Ein früher importierter FILE44-Stand
/// bleibt beim Tageszähler des Remakes. Unerwartete Formen bleiben unverändert; der Reader meldet sie.
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigrateLadeeigenheit(JsonObject root)
    {
        if (root["spiel"] is JsonObject game)
        {
            game["LoadCounterOffset"] ??= 0;
            SortFields(game);
        }
        return root;
    }
}
