using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration der Spur optionen-wirkung (Runde 9; vorläufig Schema 17 → 18, die Leitstelle nummeriert in der Integration):
/// Spielstand-Optionen (<see cref="GameSetup.Options"/>). Ältere Spielstände bekommen die Anfangswerte des Originals
/// (<see cref="GameOptions"/>: DEB-Pokal an, sonst aus, Spielgeschwindigkeit 70) – genau die Annahme, mit der das Remake
/// bisher gerechnet hat, soweit eine Option überhaupt wirkt. Unerwartete Formen bleiben unverändert; der Reader meldet sie.
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigrateOptionenWirkung(JsonObject root)
    {
        if (root["spiel"] is not JsonObject game || game["Setup"] is not JsonObject setup)
            return root;
        if (setup["Options"] is null)
        {
            var defaults = new GameOptions();
            var reports = new JsonArray();
            foreach (var report in defaults.Reports)
                reports.Add(report);
            setup["Options"] = new JsonObject
            {
                ["$id"] = NewObjectId,
                ["LiveMode"] = defaults.LiveMode,
                ["MatchSpeed"] = defaults.MatchSpeed,
                ["Reports"] = new JsonObject { ["$id"] = NewObjectId, ["items"] = reports },
                ["ShowDebCup"] = defaults.ShowDebCup,
                ["ShowEuropeanCup"] = defaults.ShowEuropeanCup,
                ["ShowWorldCup"] = defaults.ShowWorldCup,
            };
            SortFields(setup);
        }
        // Objektnummern laufen in Dateireihenfolge fortlaufend; Verweise folgen der neuen Nummerierung.
        Renumber(root, new Dictionary<int, int>(), [0]);
        return root;
    }
}
