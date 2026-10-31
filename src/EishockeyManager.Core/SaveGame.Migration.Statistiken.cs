using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration der Spur statistiken (Runde 8, #135; Schema 12 → 13, auf dem Branch 11 → 12, nummeriert in der
/// Integration): Strafminuten der Ligaspiele je Verein (<see cref="Club.LeaguePenaltyMinutes"/>, im Original
/// DGROUP 0x82A1 + 0x50 · Verein). [APPROX] Ältere Spielstände haben die Minuten der laufenden Saison nicht gezählt
/// und bekommen 0; ab dem nächsten Ligaspiel wird gezählt. Unerwartete Formen bleiben unverändert; der Reader meldet sie.
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigrateStatistiken(JsonObject root)
    {
        if (root["vereine"] is not JsonArray clubs)
            return root;
        foreach (var club in clubs.OfType<JsonObject>())
        {
            club["LeaguePenaltyMinutes"] ??= 0;
            SortFields(club);
        }
        return root;
    }
}
