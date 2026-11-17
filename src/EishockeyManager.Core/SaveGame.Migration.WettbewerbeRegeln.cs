using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration der Spur wettbewerbe-regeln (Runde 22, Teil 1; Schema 56 → 57, vorläufig – die Leitstelle nummeriert beim Merge):
/// Vereinsfeld 0x2C Bit 3 am Ende des letzten Spiels (<c>Tactics._goaliePulledAtEnd</c>, A838). [APPROX] Ältere Stände kennen
/// es nicht: false (Torhüter im Tor) – der seltene Fall eines Spielendes ohne Torhüter beginnt das nächste Spiel mit der
/// gespeicherten Reihe.
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigrateWettbewerbeRegeln(JsonObject root)
    {
        foreach (var tactics in TacticsObjects(root))
        {
            tactics["_goaliePulledAtEnd"] ??= false;
            SortFields(tactics);
        }
        return root;
    }
}
