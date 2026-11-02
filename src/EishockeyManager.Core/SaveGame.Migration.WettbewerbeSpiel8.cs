using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration der Spur wettbewerbe-spiel, Teil 8 (Schema 31 → 32, nummeriert in der Integration Runde 11):
/// Einstellung „automatisch aufstellen“ je Verein (<c>Club._autoLineup</c>). Ältere Stände bekommen den Startwert des
/// Originals (1, DGROUP 0x375) – außer der Verein wurde von Hand aufgestellt (<c>_dressedByHand</c>): Im Original schaltet
/// jeder Eingriff von Hand die Einstellung ab (Datei 0x40ADE/0x40DE4), sonst ginge die Wahl des Spielers am nächsten Tag
/// verloren (WS-01, Review #468). Die Neuaufstellung folgt am nächsten Tag. Unerwartete Formen bleiben unverändert; der
/// Reader meldet sie.
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigrateWettbewerbeSpiel8(JsonObject root)
    {
        if (root["vereine"] is not JsonArray clubs)
            return root;
        foreach (var club in clubs.OfType<JsonObject>())
        {
            var byHand = club["_dressedByHand"] is JsonValue v && v.TryGetValue<bool>(out var b) && b;
            club["_autoLineup"] ??= !byHand;
            SortFields(club);
        }
        return root;
    }
}
