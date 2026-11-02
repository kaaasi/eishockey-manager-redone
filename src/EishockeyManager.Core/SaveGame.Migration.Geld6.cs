using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration der Spur geld, Teil 6 (Runde 11; vorläufig Schema 25 → 26, die Leitstelle nummeriert): Talentsuche nach em.exe.
/// Der Zähler läuft zweimal je Woche (<see cref="TalentSearch.TicksLeft"/>, Managersatz +0xEC) statt wöchentlich
/// (<c>WeeksLeft</c>), der Preis je Manager steht in <see cref="ManagerFinance.TalentPrice"/> (DGROUP 0x20D + 4 · Manager).
/// [APPROX] Ältere Spielstände: Restzähler = 2 · Restwochen (gleiche Restzeit), Preis = Startpreis 90.000 DM. Unerwartete
/// Formen bleiben unverändert; der Reader meldet sie.
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigrateGeld6(JsonObject root)
    {
        if (root["manager"] is not JsonArray managers)
            return root;
        foreach (var node in managers)
        {
            if (node is not JsonObject manager || manager["FinanceData"] is not JsonObject finance)
                continue;
            finance["TalentPrice"] ??= TalentScout.StartPrice;
            if (finance["TalentSearch"] is JsonObject search && search["WeeksLeft"] is JsonValue weeks
                && weeks.TryGetValue<int>(out var w))
            {
                search.Remove("WeeksLeft");
                search["TicksLeft"] = Math.Max(1, 2 * w);
                SortFields(search);
            }
            SortFields(finance);
        }
        Renumber(root, new Dictionary<int, int>(), [0]);
        return root;
    }
}
