using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration Schema 1 → 2 (Spur finanzen-2, PR #67): neue Finanzfelder und Finanzbuch ohne „Gastronomie“.
/// Unerwartete Formen bleiben unverändert; der Reader meldet sie danach als beschädigt.
/// </summary>
public static partial class SaveGame
{
    /// <summary>Index der entfallenen Kategorie „Gastronomie“ im Finanzbuch von Schema 1.</summary>
    private const int RemovedGastronomyIndex = 1;

    private static JsonObject MigrateFrom1(JsonObject root)
    {
        // Bundestrainer-Konto: In Schema 1 ging der Etat aufs Vereinskonto und ist dort schon verbucht.
        // Kein zweites Geld erzeugen – das Nationalkonto beginnt leer. [APPROX] nur für alte Spielstände.
        if (root["spiel"] is JsonObject game)
            game["NationalAccount"] = 0;

        var clubs = root["vereine"] as JsonArray;
        if (root["manager"] is JsonArray managers)
        {
            foreach (var node in managers)
            {
                if (node is not JsonObject manager || manager["FinanceData"] is not JsonObject finance)
                    continue;
                // Kostenstufe = Liga der laufenden Saison (0x563C0); ein Ligawechsel wirkt erst zum Saisonwechsel,
                // daher entspricht sie der aktuellen Liga des Vereins.
                finance["CostLeague"] = CurrentLeague(manager, clubs);
                // Schema 1 buchte Zinsen sofort; aufgelaufen ist nichts.
                finance["InterestAccrualThirds"] = 0;
                finance["ManagerLoanAccrualThirds"] = 0;
            }
        }

        var handled = new HashSet<int>();
        Migrate1Walk(root, handled);
        return root;
    }

    private static string CurrentLeague(JsonObject manager, JsonArray? clubs)
    {
        if (manager["ClubIndex"] is JsonValue indexValue && indexValue.TryGetValue<int>(out var index)
            && clubs is not null && index >= 0 && index < clubs.Count
            && clubs[index] is JsonObject club && club["League"] is JsonValue league
            && league.TryGetValue<string>(out var name) && Enum.TryParse<League>(name, out _))
            return name;
        return nameof(League.Oberliga);
    }

    private static void Migrate1Walk(JsonNode? node, HashSet<int> handled)
    {
        switch (node)
        {
            case JsonObject obj:
                if (obj["$id"] is JsonValue idValue && idValue.TryGetValue<int>(out var id) && !handled.Add(id))
                    return;
                // Werbeangebot bzw. -vertrag (Felder alphabetisch, Positionsparameter des Records): Option 0 = keine.
                if (obj.ContainsKey("Kind") && obj.ContainsKey("Sponsor") && obj.ContainsKey("MonthlyAmount")
                    && !obj.ContainsKey("OptionMonths"))
                    obj["OptionMonths"] = 0;
                // Finanzbuch: „Gastronomie“ entfällt. Der Betrag stammte aus Heimspielen und geht in „Eintritt“,
                // damit die Summe des Finanzbuchs (= Kontobewegungen) erhalten bleibt.
                if (obj.Count == 3 && obj["_current"] is not null && obj["_previous"] is not null)
                {
                    RemoveGastronomy(obj["_current"]);
                    RemoveGastronomy(obj["_previous"]);
                }
                foreach (var (_, child) in obj.ToArray())
                    Migrate1Walk(child, handled);
                break;
            case JsonArray array:
                foreach (var child in array.ToArray())
                    Migrate1Walk(child, handled);
                break;
        }
    }

    private static void RemoveGastronomy(JsonNode? ledger)
    {
        if (ledger is not JsonObject obj || obj["items"] is not JsonArray items || items.Count != FinanceBook.CategoryCount + 1)
            return;
        if (items[0] is JsonValue first && first.TryGetValue<long>(out var admission)
            && items[RemovedGastronomyIndex] is JsonValue removed && removed.TryGetValue<long>(out var gastronomy))
        {
            items[0] = admission + gastronomy;
            items.RemoveAt(RemovedGastronomyIndex);
        }
    }
}
