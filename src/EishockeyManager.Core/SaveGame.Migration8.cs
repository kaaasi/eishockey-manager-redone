using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration Schema 8 → 9 (Spur heimspiel-statistik, #165): Merker „nicht spielfähig“ je Verein
/// (<c>ForfeitsMatch</c> in <see cref="Club"/>, gesetzt vor den Spielen jedes Tages, siehe
/// <see cref="GameState.MarkShortHandedClubs"/>; im Original der Managerwert <c>[0x974B]</c>). Ältere Spielstände
/// bekommen <c>false</c>; vor dem nächsten Spiel wird der Merker ohnehin neu gesetzt. Unerwartete Formen bleiben
/// unverändert; der Reader meldet sie.
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigrateFrom8(JsonObject root)
    {
        if (root["vereine"] is not JsonArray clubs)
            return root;
        foreach (var club in clubs.OfType<JsonObject>())
        {
            club["ForfeitsMatch"] ??= false;
            SortFields(club);
        }
        return root;
    }
}
