using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration der Spur wettbewerbe-spiel, Runde 12, Teil 2 (A184; Schema 32 → 33, vorläufig – die Leitstelle nummeriert in
/// der Integration): Belastung je Spieler (<see cref="Player.Strain"/>, Kaderplatz +0x13) als eigenes Feld statt der Energie.
/// Bis Schema 32 leitete das Remake die Belastung als 150 − Energie ab; genau diesen Wert übernimmt die Migration, damit ein
/// geladener Stand sich wie vorher verhält. Das Feld <c>Energy</c> entfällt (die Energie ist jetzt aus der Belastung abgeleitet
/// und wird nicht gespeichert). Unerwartete Formen bleiben unverändert; der Reader meldet sie.
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigrateWettbewerbeSpiel12(JsonObject root)
    {
        if (root["spieler"] is not JsonArray players)
            return root;
        foreach (var player in players.OfType<JsonObject>())
        {
            if (player["Strain"] is null && player["Energy"] is JsonValue v && v.TryGetValue<int>(out var energy))
            {
                player["Strain"] = Math.Clamp(150 - energy, 0, 255);
                player.Remove("Energy");
            }
            SortFields(player);
        }
        return root;
    }
}
