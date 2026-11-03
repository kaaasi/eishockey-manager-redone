using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration der Spur saisonwechsel, Teil 3 (Runde 12; vorläufig Schema 32 → 33, die Leitstelle nummeriert): Satzfolge der
/// Vereine je Liga (<c>_leagueSlots</c>, <see cref="GameState.LeagueSlots"/>, Ligamischung em.exe Datei 0x35EDD).
/// [APPROX] Ältere Spielstände führen keine Satzfolge: Die Liste beginnt leer, bis zum nächsten Saisonwechsel gilt die
/// Indexfolge (bisheriges Verhalten), danach wird gemischt. Unerwartete Formen bleiben unverändert; der Reader meldet sie.
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigrateSaisonwechsel3(JsonObject root)
    {
        if (root["spiel"] is not JsonObject game)
            return root;
        if (!game.ContainsKey("_leagueSlots"))
        {
            game["_leagueSlots"] = new JsonObject { ["$id"] = NewObjectId, ["items"] = new JsonArray() };
            SortFields(game);
            Renumber(root, new Dictionary<int, int>(), [0]);
        }
        return root;
    }
}
