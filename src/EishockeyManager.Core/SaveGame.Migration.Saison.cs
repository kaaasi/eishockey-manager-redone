using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration der Spur saison, Teil 3 (Runde 13; Schema 37 → 38, Nummer nach Entscheidung der Leitstelle L5 in PR #567):
/// Satzfolge der nächsten Saison (<c>_nextLeagueSlots</c>, <see cref="GameState.NextLeagueSlots"/>), gebildet am 29. April
/// wie die Ligamischung em.exe Datei 0x5644E vor dem Fenster „Saisonende“ (Datei 0x5645D).
/// [APPROX] Ältere Spielstände führen sie nicht: Die Liste beginnt leer; ein Spielstand vom Bildschirm „Saisonende“ bildet die
/// Folge dann wie bis Schema 37 erst beim Saisonwechsel, seine Fair-Play-Prämie ist schon gebucht. Unerwartete Formen bleiben
/// unverändert; der Reader meldet sie.
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigrateSaison(JsonObject root)
    {
        if (root["spiel"] is not JsonObject game)
            return root;
        if (!game.ContainsKey("_nextLeagueSlots"))
        {
            game["_nextLeagueSlots"] = new JsonObject { ["$id"] = NewObjectId, ["items"] = new JsonArray() };
            SortFields(game);
            Renumber(root, new Dictionary<int, int>(), [0]);
        }
        return root;
    }

    /// <summary>
    /// Schema 36 → 37 war der Spur kader-start vorbehalten (L5). Sie brauchte die Lücke in Runde 13 nicht; der Schritt bleibt ohne Änderung, damit Spielstände mit Schema 36 laden
    /// (#621, R13GN-05).
    /// </summary>
    private static JsonObject MigrateSaisonPlatzhalter37(JsonObject root) => root;
}
