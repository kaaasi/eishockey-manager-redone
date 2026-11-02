using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration der Spur wettbewerbe-spiel, Teil 8 (vorläufig Schema 23 → 24, die Leitstelle nummeriert in der Integration):
/// Einstellung „automatisch aufstellen“ je Verein (<c>Club._autoLineup</c>). Ältere Stände bekommen den Startwert des
/// Originals (1, DGROUP 0x375); die Neuaufstellung folgt am nächsten Tag. Unerwartete Formen bleiben unverändert; der
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
            club["_autoLineup"] ??= true;
            SortFields(club);
        }
        return root;
    }
}
