using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration der Spur geld, Teil 1 (Runde 11; vorläufig Schema 22 → 23, die Leitstelle nummeriert): Stadionausbau nach
/// em.exe mit parallelen Baustellen. <see cref="Stadium"/> verliert die eine <c>Construction</c> und bekommt je Projekt
/// Zielstufe (<c>_targets</c>), Restwochen (<c>_weeksLeft</c>), den Merker „Baufirma beleidigt“ (<c>_offended</c>) sowie
/// <c>SeatsTarget</c>/<c>StandingTarget</c>. Eine laufende Baustelle bleibt mit Ziel und Restwochen erhalten; Plätze nach
/// den alten Schritten (500 Sitz-, 1.000 Stehplätze je Schritt). Bezahlt war sie schon (Finanzbuch „Bau“). Unerwartete
/// Formen bleiben unverändert; der Reader meldet sie.
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigrateGeld1(JsonObject root)
    {
        if (root["manager"] is not JsonArray managers)
            return root;
        foreach (var node in managers)
        {
            if (node is not JsonObject manager || manager["FinanceData"] is not JsonObject finance
                || finance["Stadium"] is not JsonObject stadium || stadium.ContainsKey("_targets"))
                continue;
            var targets = new int[Stadium.FeatureCount];
            var weeks = new int[Stadium.ProjectCount];
            int seatsTarget = 0, standingTarget = 0;
            if (stadium["Construction"] is JsonObject construction)
            {
                var target = construction["Target"]?.GetValue<int>() ?? 0;
                var left = Math.Max(1, construction["WeeksLeft"]?.GetValue<int>() ?? 1);
                switch (construction["Project"]?.GetValue<string>())
                {
                    case nameof(StadiumProject.Sitzplaetze):
                        seatsTarget = (stadium["Seats"]?.GetValue<int>() ?? 0) + 500 * target;
                        weeks[Stadium.SeatsProject] = left;
                        break;
                    case nameof(StadiumProject.Stehplaetze):
                        standingTarget = (stadium["Standing"]?.GetValue<int>() ?? 0) + 1_000 * target;
                        weeks[Stadium.StandingProject] = left;
                        break;
                    default:
                        if (Enum.TryParse<StadiumFeature>(construction["Feature"]?.GetValue<string>(), out var feature) && target > 0)
                        {
                            targets[(int)feature] = target;
                            weeks[(int)feature] = left;
                        }
                        break;
                }
            }
            stadium.Remove("Construction");
            stadium["SeatsTarget"] = seatsTarget;
            stadium["StandingTarget"] = standingTarget;
            stadium["_targets"] = GeldItems(targets.Select(v => (JsonNode)v));
            stadium["_weeksLeft"] = GeldItems(weeks.Select(v => (JsonNode)v));
            stadium["_offended"] = GeldItems(Enumerable.Range(0, Stadium.ProjectCount).Select(_ => (JsonNode)false));
            SortFields(stadium);
        }
        Renumber(root, new Dictionary<int, int>(), [0]);
        return root;
    }

    private static JsonObject GeldItems(IEnumerable<JsonNode> values)
    {
        var items = new JsonArray();
        foreach (var value in values)
            items.Add(value);
        return new JsonObject { ["$id"] = NewObjectId, ["items"] = items };
    }
}
