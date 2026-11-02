using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration der Spur geld, Teil 3 (Runde 11; vorläufig Schema 24 → 25, die Leitstelle nummeriert): Stärke je
/// Jugendteam (<see cref="YouthTeam.Strength"/>, im Original DGROUP 0xA075 + 0x15 · Manager). [APPROX] Ältere Spielstände
/// kennen die Stärke nicht: Startstärke der Liga, in der das Team gerade spielt (<see cref="YouthTeam.StartStrength"/>,
/// Datei 0x29B76). Unerwartete Formen bleiben unverändert; der Reader meldet sie.
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigrateGeld3(JsonObject root)
    {
        if (root["manager"] is not JsonArray managers)
            return root;
        foreach (var node in managers)
        {
            if (node is not JsonObject manager || manager["FinanceData"] is not JsonObject finance)
                continue;
            var teams = finance["Youth"] switch
            {
                JsonObject wrapped when wrapped["items"] is JsonArray items => items,
                JsonArray plain => plain,
                _ => null,
            };
            foreach (var team in teams?.OfType<JsonObject>() ?? [])
            {
                if (team.ContainsKey("Strength"))
                    continue;
                var league = Math.Clamp(team["League"]?.GetValue<int>() ?? YouthTeam.Leagues, 1, YouthTeam.Leagues);
                team["Strength"] = YouthTeam.StartStrength((League)(league - 1));
                SortFields(team);
            }
        }
        Renumber(root, new Dictionary<int, int>(), [0]);
        return root;
    }
}
