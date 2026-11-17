using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration der Spur national, Runde 22 Teil 1 (A097; Schema 56 → 57, vorläufig – die Nummer vergibt die Leitstelle beim Rebase in
/// Integrationsfolge): Über-/Unterzahl-Zähler im Satz des Bundestrainers (<c>NationalCoach.PowerPlayCounts</c>, DGROUP 0x97D2 +
/// 5 · 0x1AF). [APPROX] Ältere Stände kennen die Zähler nicht und beginnen bei 0; ab dem nächsten Länderspiel mit Minutenrechnung
/// zählt das Remake wie das Original. Unerwartete Formen bleiben unverändert; der Reader meldet sie.
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigrateNational(JsonObject root)
    {
        if (root["spiel"] is JsonObject game && game["NationalCoach"] is JsonObject coach)
        {
            coach["PowerPlayCounts"] ??= Zeros(PowerPlay.Count);
            SortFields(coach);
            // Objektnummern laufen in Dateireihenfolge fortlaufend; Verweise folgen der neuen Nummerierung.
            Renumber(root, new Dictionary<int, int>(), [0]);
        }
        return root;
    }
}
