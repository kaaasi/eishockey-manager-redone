using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration Schema 13 → 14 (Spur spielstaerke-manager, #204; auf dem Branch 11 → 12, Integration Runde 8): Teamwerte des
/// Laders 0x2EF1B je Verein (<see cref="Club.TeamValues"/>). Ältere Spielstände bekommen <c>null</c>; die Werte werden
/// dann beim nächsten Spiel gezogen (<see cref="MatchEngine.EnsureTeamValues"/>). Unerwartete Formen bleiben unverändert;
/// der Reader meldet sie.
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigrateFromTeamValues(JsonObject root)
    {
        if (root["vereine"] is not JsonArray clubs)
            return root;
        foreach (var club in clubs.OfType<JsonObject>())
        {
            if (!club.ContainsKey("TeamValues"))
                club["TeamValues"] = null;
            SortFields(club);
        }
        return root;
    }
}
