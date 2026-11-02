using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration der Spur kader-transfer, Teil 2 (Schema 27 → 28, nummeriert in der Integration Runde 11):
/// Kaufpreis (<see cref="Player.PurchasePrice"/>, Kaderplatz +0x2C) und Verletzungsart (<see cref="Player.InjuryKind"/>, +0x17).
/// [APPROX] Ältere Stände kennen beide nicht: Kaufpreis 0 (Anzeige „-“), Art <see cref="InjuryKinds.Unknown"/> („Verletzt.“
/// ohne Klammer). Den Preis nachträglich als Spielerwert zu rechnen wäre falsch für gekaufte Spieler (Gebot).
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigrateKaderTransfer(JsonObject root)
    {
        if (root["spieler"] is not JsonArray players)
            return root;
        foreach (var player in players.OfType<JsonObject>())
        {
            player["PurchasePrice"] ??= 0L;
            player["InjuryKind"] ??= InjuryKinds.Unknown;
            SortFields(player);
        }
        return root;
    }
}
