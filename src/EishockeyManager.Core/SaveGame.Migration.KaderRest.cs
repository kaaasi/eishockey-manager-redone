using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration der Spur kader-rest (Runde 17, Teil 1; Schema 49 → 50, vorläufig – die Leitstelle nummeriert beim Merge):
/// Poolform (<see cref="Player.PoolForm"/>, Pool +0x17, A066). [APPROX] Ältere Stände kennen sie nicht: null (= Form des
/// Kaderplatzes); eine dort schon gesunkene Form verletzter Spieler bleibt, wie sie ist.
/// Teil 3 (Ereignissymbol 9): Jugendwechsel und Jugend am Saisonende laufen seit Schema 50 schon am 29. April
/// (<c>SeasonFlow.SeasonEndYouth.cs</c>). Ein älterer Stand vom Bildschirm „Saisonende“ (Phase <c>SeasonOver</c>) hat sie noch
/// vor sich: <c>_seasonEndYouthPending</c> = true, der Saisonwechsel holt sie nach; sonst false.
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigrateKaderRest(JsonObject root)
    {
        if (root["spiel"] is JsonObject game && !game.ContainsKey("_seasonEndYouthPending"))
        {
            game["_seasonEndYouthPending"] = game["Phase"] is JsonValue phase
                && phase.TryGetValue<string>(out var name) && name == nameof(SeasonPhase.SeasonOver);
            SortFields(game);
        }
        if (root["spieler"] is not JsonArray players)
            return root;
        foreach (var player in players.OfType<JsonObject>())
        {
            if (!player.ContainsKey("PoolForm"))
                player["PoolForm"] = null;
            SortFields(player);
        }
        return root;
    }
}
