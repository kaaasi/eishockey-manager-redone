using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration der Spur wettbewerbe-regeln (Runde 22, Teile 1–2; Schema 57 → 58 nach `national`, vorläufig – die Leitstelle nummeriert beim Merge):
/// Vereinsfeld 0x2C Bit 3 am Ende des letzten Spiels (<c>Tactics._goaliePulledAtEnd</c>, A838). [APPROX] Ältere Stände kennen
/// es nicht: false (Torhüter im Tor) – der seltene Fall eines Spielendes ohne Torhüter beginnt das nächste Spiel mit der
/// gespeicherten Reihe.
/// Teil 2 (A384/A670): Die Jugendteams eines Tages laufen jetzt nach den Menüs (<c>SeasonFlow.YouthAfterMenus.cs</c>). Ein älterer
/// Stand nach dem Tageswechsel eines Tages ab 1 (<c>DayOpened</c>) hat sie schon gerechnet: <c>_youthOfDayDone</c> = true, sonst
/// false (Tag 0 rechnete auch früher erst nach dem Menü).
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigrateWettbewerbeRegeln(JsonObject root)
    {
        foreach (var tactics in TacticsObjects(root))
        {
            tactics["_goaliePulledAtEnd"] ??= false;
            SortFields(tactics);
        }
        if (root["spiel"] is JsonObject game && !game.ContainsKey("_youthOfDayDone"))
        {
            game["_youthOfDayDone"] = game["DayOpened"] is JsonValue opened && opened.TryGetValue<bool>(out var o) && o
                && game["CurrentDay"] is JsonValue current && current.TryGetValue<int>(out var day) && day > 0;
            SortFields(game);
        }
        return root;
    }
}
