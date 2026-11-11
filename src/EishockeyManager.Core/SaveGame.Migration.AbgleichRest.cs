using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration Schema 47 → 48 (Runde 16, Spur abgleich-rest, #772; die Leitstelle nummeriert beim Merge): jede Taktik bekommt
/// Einsatz und Angriff des letzten Spiels (<c>Tactics._matchEffort</c>, <c>_matchAttack</c> = Vereinsfelder 0x26 und 0x2C
/// Bits 4–6). [APPROX] Ältere Stände kennen die Werte nicht und beginnen bei 0 wie ein neues Spiel (FILE02); ab dem nächsten
/// Spiel des Vereins stehen die Werte dieses Spiels. Unerwartete Formen bleiben unverändert; der Reader meldet sie.
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigrateAbgleichRest(JsonObject root)
    {
        foreach (var tactics in TacticsObjects(root))
        {
            tactics["_matchAttack"] ??= 0;
            tactics["_matchEffort"] ??= 0;
            SortFields(tactics);
        }
        return root;
    }
}
