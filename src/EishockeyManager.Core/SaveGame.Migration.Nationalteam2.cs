using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration der Spur nationalteam-2 (Runde 8, Welle 3; vorläufig Schema 14 → 15, die Leitstelle nummeriert in der
/// Integration): Platzierungen der bisherigen Weltmeisterschaften für die WM-Statistik (<c>_worldCupRankings</c>,
/// <see cref="GameState.WorldCupRankings"/>). [APPROX] Ältere Spielstände haben die Reihenfolgen früherer WMs nicht
/// geführt: Die Liste beginnt leer, die WM-Statistik zählt ab der nächsten WM. Unerwartete Formen bleiben unverändert;
/// der Reader meldet sie.
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigrateFromNationalteam2(JsonObject root)
    {
        if (root["spiel"] is not JsonObject game)
            return root;
        if (!game.ContainsKey("_worldCupRankings"))
        {
            game["_worldCupRankings"] = new JsonObject { ["$id"] = NewObjectId, ["items"] = new JsonArray() };
            SortFields(game);
            Renumber(root, new Dictionary<int, int>(), [0]);
        }
        return root;
    }
}
