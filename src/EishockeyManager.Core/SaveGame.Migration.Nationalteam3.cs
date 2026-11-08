using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration der Spur nationalteam, Runde 14 Teil 3 (A009, L8; Schema 39 → 40 auf dem Branch, die Leitstelle nummeriert bei der
/// Integration): Einstellung „automatisch aufstellen“ des Bundestrainers (<c>NationalCoach._autoLineup</c> = DGROUP 0x37F,
/// <c>_autoLineupSaved</c> = 0x380, <c>_turnOpen</c>) und ihr Stand zwischen zwei Ämtern (<c>GameState._nationalAutoLineupSaved</c>).
/// Ältere Stände bekommen den Startwert des Originals (1) – außer der Bundestrainer hat eine eigene Aufstellung: Die stammt im
/// Remake von Hand (oder „A“), und im Original schaltet jeder Eingriff die Einstellung ab (Datei 0x40ADE/0x40DE4). Mit leerer
/// Aufstellung spielte der Stand bisher mit der Automatik des Remakes; mit Einstellung stellt der nächste Menüaufbau auf.
/// Der Zug gilt als nicht offen; der nächste Aufbau des Menüs nach dem Laden beginnt ihn (wie im Original). Unerwartete Formen
/// bleiben unverändert; der Reader meldet sie.
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigrateNationalteam3(JsonObject root)
    {
        if (root["spiel"] is not JsonObject game)
            return root;
        game["_nationalAutoLineupSaved"] ??= true;
        SortFields(game);
        if (game["NationalCoach"] is JsonObject coach)
        {
            var auto = !HasPlayers(coach["Formation"]);
            coach["_autoLineup"] ??= auto;
            coach["_autoLineupSaved"] ??= auto;
            coach["_turnOpen"] ??= false;
            SortFields(coach);
        }
        return root;
    }

    /// <summary>Steht in der gespeicherten Aufstellung (Torhüter, Feldplätze) mindestens ein Spieler?</summary>
    private static bool HasPlayers(JsonNode? lineup)
    {
        if (lineup is not JsonObject l)
            return false;
        if (l["_goalkeeper"] is not null)
            return true;
        foreach (var key in new[] { "_defenders", "_forwards" })
            if (l[key] is JsonObject grid && grid["items"] is JsonArray items && items.Any(i => i is not null))
                return true;
        return false;
    }
}
