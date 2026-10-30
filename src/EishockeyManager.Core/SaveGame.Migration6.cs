using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration Schema 6 → 7 (Spur heimspiel-statistik, #136): Zuschauerstatistik je Manager (<c>_homeStatistics</c> in
/// <see cref="ManagerFinance"/>, siehe <see cref="StadiumStatistics"/>). Ältere Spielstände haben keine Statistik; das
/// Feld wird <c>null</c> und beim nächsten Zugriff leer angelegt (wie zu Saisonbeginn). Die Werte der laufenden Saison
/// lassen sich nicht nachbilden. Unerwartete Formen bleiben unverändert; der Reader meldet sie.
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigrateFrom6(JsonObject root)
    {
        if (root["manager"] is not JsonArray managers)
            return root;
        foreach (var node in managers)
        {
            if (node is not JsonObject manager || manager["FinanceData"] is not JsonObject finance)
                continue;
            finance["_homeStatistics"] ??= null;
            SortFields(finance);
        }
        return root;
    }
}
