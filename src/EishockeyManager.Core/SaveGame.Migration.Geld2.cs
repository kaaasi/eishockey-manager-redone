using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration der Spur geld, Teil 2 (Runde 11; vorläufig Schema 23 → 24, die Leitstelle nummeriert): Platzverlauf je
/// Verein für die Werbebewertung (<c>Club._rankSlots</c>, im Original DGROUP 0x828B + 0x50 · Verein) und der Zähler
/// <see cref="GameState.RankSlotsRecorded"/>. [APPROX] Ältere Spielstände kennen den Verlauf der Vorsaisons nicht: alle
/// Felder 0 (wie nach dem Spielstart); die gespielten Spieltage der laufenden Saison schreibt der nächste Tagesabschluss
/// nach. Unerwartete Formen bleiben unverändert; der Reader meldet sie.
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigrateGeld2(JsonObject root)
    {
        if (root["spiel"] is JsonObject game)
        {
            game["RankSlotsRecorded"] ??= 0;
            SortFields(game);
        }
        if (root["vereine"] is JsonArray clubs)
            foreach (var club in clubs.OfType<JsonObject>())
            {
                club["_rankSlots"] ??= Filled(GameState.RankSlotCount, 0);
                SortFields(club);
            }
        Renumber(root, new Dictionary<int, int>(), [0]);
        return root;
    }
}
