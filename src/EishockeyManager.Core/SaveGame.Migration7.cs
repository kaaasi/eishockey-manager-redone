using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration Schema 7 → 8 (Spur heimspiel-statistik, #136; in der Integration Runde 6 von 6 → 7 umnummeriert, weil
/// finanzen-werbung 6 → 7 belegt): Zuschauerstatistik je Manager (<c>_homeStatistics</c> in
/// <see cref="ManagerFinance"/>, siehe <see cref="StadiumStatistics"/>). Ältere Spielstände haben keine Statistik; das
/// Feld wird <c>null</c> und beim nächsten Zugriff leer angelegt (wie zu Saisonbeginn). Die Werte der laufenden Saison
/// lassen sich nicht nachbilden. Unerwartete Formen bleiben unverändert; der Reader meldet sie.
/// <para>
/// Schema 7 gibt es wie Schema 5 in Runde 5 (R5-01, #130) in zwei Formen: aus der Spur finanzen-werbung (mit
/// <c>SponsorRating</c>, ohne <c>_homeStatistics</c>) und aus der Spur heimspiel-statistik (mit <c>_homeStatistics</c>,
/// ohne Werbebewertung). Fehlt einem Manager <c>SponsorRating</c>, holt diese Migration zuerst den Schritt 6 → 7 nach
/// (<see cref="MigrateFrom6"/>); eine vorhandene Statistik bleibt erhalten.
/// </para>
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigrateFrom7(JsonObject root)
    {
        if (root["manager"] is not JsonArray managers)
            return root;
        if (managers.OfType<JsonObject>().Any(m => m["FinanceData"] is JsonObject f && !f.ContainsKey("SponsorRating")))
            root = MigrateFrom6(root);
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
