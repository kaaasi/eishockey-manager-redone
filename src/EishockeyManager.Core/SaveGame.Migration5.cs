using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration Schema 5 → 6 (Spur bundestrainer-modus4, in der Integration Runde 5 von 4 → 5 umnummeriert): Zug des Bundestrainers im Modus 4 (<c>_nationalTurn</c>,
/// siehe <see cref="GameState.NationalTurn"/>). Spielstände bis Schema 5 kennen diesen Zug nicht – er ist dort nie
/// offen. Unerwartete Formen bleiben unverändert; der Reader meldet sie.
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigrateFrom5(JsonObject root)
    {
        if (root["spiel"] is not JsonObject game)
            return root;
        game["_nationalTurn"] ??= false;
        SortFields(game);
        return root;
    }
}
