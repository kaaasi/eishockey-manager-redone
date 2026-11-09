using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration Schema 41 → 42 (Runde 14, Spur kader-start Teil 1, R13KI-03/#605): Start-Liga in <see cref="GameSetup"/> selbst
/// (Feld <c>_startLeague</c>, [ORIGINAL] ein Wert für alle Manager, DGROUP 0x11A, L1). Ältere Spielstände übernehmen die
/// Start-Liga von Manager 0 (so las sie <see cref="GameSetup.StartLeague"/> bisher), ohne Manager Bundesliga ([ORIGINAL] Vorgabe 0).
/// Auf dem Spurbranch vorläufig 39 → 40; bei der Integration Runde 14 neu nummeriert (main hat 40 und 41 belegt). Unerwartete Formen bleiben unverändert; der Reader meldet sie.
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigrateKaderStart(JsonObject root)
    {
        if (root["spiel"] is not JsonObject game || game["Setup"] is not JsonObject setup || setup.ContainsKey("_startLeague"))
            return root;
        var league = setup["Managers"]?["items"] is JsonArray { Count: > 0 } items
                     && items[0]?["StartLeague"] is JsonValue value && value.TryGetValue<string>(out var name)
            ? name
            : nameof(League.Bundesliga);
        setup["_startLeague"] = league;
        SortFields(setup);
        return root;
    }
}
