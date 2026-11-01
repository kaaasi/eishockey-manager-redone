using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration der Spur transfer-vertraege-komplett, Teil 2 (Schema 19 → 20, Integration Runde 9): Versicherung
/// als Prämie in DM (<see cref="Player.InsurancePremium"/>, Kaderplatz +0x30) statt des Merkers „Insured“, dazu der Abschluss im
/// Verhandlungsfenster (<see cref="Player.InsuranceLocked"/>, +0x2C Bit 31). [APPROX] Ältere Stände kennen den Betrag nicht:
/// versichert → bisherige Prämie <see cref="Contracts.LegacyInsurancePercent"/> % des Gehalts (mindestens 1 DM), damit die
/// monatlichen Kosten gleich bleiben; nicht abgeschlossen (umschaltbar). Unerwartete Formen bleiben unverändert; der Reader
/// meldet sie.
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigrateTransferVertraegeKomplett(JsonObject root)
    {
        if (root["spieler"] is not JsonArray players)
            return root;
        foreach (var player in players.OfType<JsonObject>())
        {
            if (player["Insured"] is not JsonValue insured || !insured.TryGetValue<bool>(out var value))
                continue;
            var salary = player["MonthlySalary"] is JsonValue s && s.TryGetValue<int>(out var v) ? v : 0;
            player.Remove("Insured");
            player["InsurancePremium"] = value ? Math.Max(1, salary * Contracts.LegacyInsurancePercent / 100) : 0;
            player["InsuranceLocked"] = false;
            SortFields(player);
        }
        return root;
    }
}
