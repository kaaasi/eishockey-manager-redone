using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration der Spur national, Runde 22 Teil 1 (A097; Schema 56 → 57, vorläufig – die Nummer vergibt die Leitstelle beim Rebase in
/// Integrationsfolge): Über-/Unterzahl-Zähler im Satz des Bundestrainers (<c>NationalCoach.PowerPlayCounts</c>, DGROUP 0x97D2 +
/// 5 · 0x1AF); Teil 2 (A498): <c>GameState._worldCupLastPenaltyCoachDay</c> = 0, (A825) <c>WorldCup._nextOrder</c> = null; Teil 3 (A412):
/// <c>NationalSquadMember.Source</c> = Spieler des Eintrags. [APPROX] Ältere Stände kennen die Zähler nicht und beginnen bei 0; ab dem nächsten Länderspiel mit Minutenrechnung
/// zählt das Remake wie das Original. Unerwartete Formen bleiben unverändert; der Reader meldet sie.
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigrateNational(JsonObject root)
    {
        if (root["spiel"] is not JsonObject game)
            return root;
        // Teil 2 (A498): WM-Tag mit letzter Strafe beim Land des Bundestrainers; ältere Stände kennen ihn nicht (0 = keiner).
        game["_worldCupLastPenaltyCoachDay"] ??= 0;
        SortFields(game);
        // Teil 2 (A825): am 29. April gemischte Länderreihenfolge; ältere Stände mischen beim Saisonwechsel (WorldCup.StartSeason).
        foreach (var worldCup in WorldCupObjects(root))
        {
            worldCup["_nextOrder"] ??= null;
            SortFields(worldCup);
        }
        if (game["NationalCoach"] is JsonObject coach)
        {
            coach["PowerPlayCounts"] ??= Zeros(PowerPlay.Count);
            SortFields(coach);
            // Teil 3 (A412): Kaderplatz des Satzes 5 und Spieler im Verein/Pool. [APPROX] Ältere Stände führen keinen eigenen
            // Platz: Beide verweisen bis zum Saisonwechsel auf den Spieler (wie bis Runde 22).
            if ((coach["_squad"] as JsonObject)?["items"] is JsonArray squad)
                foreach (var member in squad.OfType<JsonObject>())
                    if (member["Player"] is { } player && member["Source"] is null)
                    {
                        member["Source"] = player.DeepClone();
                        SortFields(member);
                    }
            // Objektnummern laufen in Dateireihenfolge fortlaufend; Verweise folgen der neuen Nummerierung.
            Renumber(root, new Dictionary<int, int>(), [0]);
        }
        return root;
    }

    /// <summary>Alle Objekte vom Typ <c>WorldCup</c> der Datei (Wettbewerbsliste, „$type“ wie der Writer).</summary>
    private static List<JsonObject> WorldCupObjects(JsonNode? node)
    {
        var found = new List<JsonObject>();
        void Walk(JsonNode? n)
        {
            switch (n)
            {
                case JsonObject o:
                    if (o["$type"] is JsonValue t && t.TryGetValue<string>(out var name) && name == nameof(WorldCup))
                        found.Add(o);
                    foreach (var (_, child) in o.ToList())
                        Walk(child);
                    break;
                case JsonArray a:
                    foreach (var child in a)
                        Walk(child);
                    break;
            }
        }
        Walk(node);
        return found;
    }
}
