using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration Schema 5 → 6 (Spur bundestrainer-modus4, in der Integration Runde 5 von 4 → 5 umnummeriert): Zug des Bundestrainers im Modus 4 (<c>_nationalTurn</c>,
/// siehe <see cref="GameState.NationalTurn"/>). Spielstände bis Schema 5 kennen diesen Zug nicht – er ist dort nie
/// offen. Unerwartete Formen bleiben unverändert; der Reader meldet sie.
/// <para>
/// Schema 5 gibt es in zwei Formen (Befund R5-01, #130): aus der Spur taktik-verein (Vereine mit <c>_dressed</c>,
/// ohne <c>_nationalTurn</c>) und aus der Spur bundestrainer-modus4 (mit <c>_nationalTurn</c>, Vereine ohne
/// <c>_dressed</c>, Taktikwerte alter Bedeutung). Fehlt den Vereinen <c>_dressed</c>, holt diese Migration zuerst den
/// Schritt 4 → 5 nach (<see cref="MigrateFrom4"/>); ein vorhandener offener Zug bleibt erhalten.
/// </para>
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigrateFrom5(JsonObject root)
    {
        if (root["vereine"] is JsonArray clubs && clubs.OfType<JsonObject>().Any(c => !c.ContainsKey("_dressed")))
            root = MigrateFrom4(root);
        if (root["spiel"] is not JsonObject game)
            return root;
        game["_nationalTurn"] ??= false;
        SortFields(game);
        return root;
    }
}
