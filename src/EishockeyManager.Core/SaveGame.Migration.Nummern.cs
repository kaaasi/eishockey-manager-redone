using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration Schema 38 → 39 (Runde 13, Fix R13G-01/#613 und R13KI-04/#606): Nr. = Kaderplatz + 1 für alle Vereine.
/// [ORIGINAL] A091: Das Original kennt keine eigene Rückennummer, die „Nr.“ ist der Kaderplatz + 1 (Kauf nach Spielerart einsortiert Datei 0x42D55,
/// R13GN-01, Stürmer hinten FILE07; Verkauf rückt auf Datei 0x425B6, FILE05 → FILE26; <see cref="Club.RenumberPlayers"/>).
/// [APPROX] Remake-Anpassung ohne Gegenstück im Original: Spielstände aus der Zeit der freien Rückennummern (bis zur Integration
/// kader-start, PR #600) führen abweichende Nummern. Bisher glich sie erst die Neuaufstellung (Hauptmenü, auch bei ausgeschalteter
/// Automatik) bzw. bei Computervereinen erst Kauf oder Abgang an; jetzt einmalig beim Laden, danach nur noch bei Kaderänderungen.
/// Spieler ohne Verein behalten ihre Nummer. Unerwartete Formen bleiben unverändert; der Reader meldet sie.
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigrateNummern(JsonObject root)
    {
        if (root["vereine"] is not JsonArray clubs || root["spieler"] is not JsonArray players)
            return root;
        var byId = new Dictionary<int, JsonObject>();
        foreach (var node in players)
            if (node is JsonObject player && player["Id"] is JsonValue id && id.TryGetValue<int>(out var value))
                byId.TryAdd(value, player);
        foreach (var club in clubs)
        {
            if (club?["Players"]?["items"] is not JsonArray items)
                continue;
            for (var i = 0; i < items.Count; i++)
                if (items[i]?["$spieler"] is JsonValue reference && reference.TryGetValue<int>(out var id)
                    && byId.TryGetValue(id, out var player) && player.ContainsKey("Number"))
                    player["Number"] = i + 1;
        }
        return root;
    }
}
