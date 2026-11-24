using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration der Spur aufstellung (Runde 25, K07; Schema 63 → 64): <see cref="Manager.AutoLineupEnabled"/> (A936, „automatisch
/// aufstellen“ je Manager, DGROUP 0x375 + 2 · Manager) und <see cref="Player.LastLine"/> (A939, Kaderplatz +0x15 außerhalb der
/// Aufstellung). Ältere Stände führen die Einstellung am Verein: der Manager übernimmt den Wert seines Vereins (Vereinsliste in
/// Platzfolge, Platz = <see cref="Manager.ClubIndex"/>). [APPROX] Ältere Stände kennen +0x15 außerhalb der Aufstellung nicht: 0 (wie
/// nach dem Leeren von Hand); die nächste Neuaufstellung bzw. Verletzung setzt es neu.
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigrateAufstellung(JsonObject root)
    {
        var clubs = root["vereine"] as JsonArray;
        if (root["manager"] is JsonArray managers)
        {
            foreach (var manager in managers.OfType<JsonObject>())
            {
                if (!manager.ContainsKey("AutoLineupEnabled"))
                {
                    var index = manager["ClubIndex"]?.GetValue<int>() ?? -1;
                    var club = clubs is not null && index >= 0 && index < clubs.Count ? clubs[index] as JsonObject : null;
                    manager["AutoLineupEnabled"] = club?["_autoLineup"]?.GetValue<bool>() ?? true;
                }
                SortFields(manager);
            }
        }
        if (root["spieler"] is JsonArray players)
        {
            foreach (var player in players.OfType<JsonObject>())
            {
                player["LastLine"] ??= 0;
                SortFields(player);
            }
        }
        return root;
    }
}
