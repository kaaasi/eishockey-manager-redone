using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration Schema 50 → 51 (Runde 20, Spur pruefung-karte Teil 2, #872): jede Taktik bekommt die Reihen auf dem Eis am Ende
/// des letzten Spiels (<c>Tactics._matchDefenseLine</c>, <c>_matchAttackLine</c> = Vereinsfelder 0x2E/0x30,
/// <c>Squad.Tactics.Lines.cs</c>). [APPROX] Ältere Stände kennen sie nicht und beginnen bei 0 wie ein neues Spiel (FILE02); ab
/// dem nächsten Spiel des Vereins stehen die Reihen dieses Spiels. Unerwartete Formen bleiben unverändert; der Reader meldet sie.
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigratePruefungKarte(JsonObject root)
    {
        foreach (var tactics in TacticsObjects(root))
        {
            tactics["_matchAttackLine"] ??= 0;
            tactics["_matchDefenseLine"] ??= 0;
            SortFields(tactics);
        }
        return root;
    }
}
