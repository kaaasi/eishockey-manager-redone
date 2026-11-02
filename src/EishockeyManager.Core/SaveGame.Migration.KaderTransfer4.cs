using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration der Spur kader-transfer, Teil 4 (Schema 28 → 29, nummeriert in der Integration Runde 11):
/// Tendenzwert (<see cref="Player.TrendScore"/>, Kaderplatz +0x0E) und Steuerbyte (<see cref="Player.TrendControl"/>, +0x14).
/// [APPROX] Ältere Stände kennen beide nicht: 0, es gilt die gespeicherte Stufe (<see cref="Player.Trend"/>) wie bisher.
/// Die Zufallsquelle <see cref="GameState.TrendRandom"/> fehlt (null) und beginnt beim ersten Zug.
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigrateKaderTransfer4(JsonObject root)
    {
        if (root["spiel"] is JsonObject game && !game.ContainsKey("_trendRandom"))
        {
            game["_trendRandom"] = null;
            SortFields(game);
        }
        if (root["spieler"] is not JsonArray players)
            return root;
        foreach (var player in players.OfType<JsonObject>())
        {
            player["TrendScore"] ??= 0;
            player["TrendControl"] ??= 0;
            SortFields(player);
        }
        return root;
    }
}
