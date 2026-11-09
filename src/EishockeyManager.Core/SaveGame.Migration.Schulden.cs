using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration Schema 45 → 46 (#688, R688S-03, Leitstelle Runde 14): Die Remake-Regel „Entlassung nach acht Wochen unter −1,5 Mio. DM“
/// ist entfernt (im Original nicht vorhanden, <c>SeasonDismissal.cs</c>); der Zähler <c>Manager.DebtDays</c> entfällt. Das
/// Kennzeichen <c>Dismissed</c> älterer Stände bleibt. Unerwartete Formen bleiben unverändert; der Reader meldet sie.
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigrateSchulden(JsonObject root)
    {
        if (root["manager"] is not JsonArray managers)
            return root;
        foreach (var manager in managers.OfType<JsonObject>())
            manager.Remove("DebtDays");
        return root;
    }
}
