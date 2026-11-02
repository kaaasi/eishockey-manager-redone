using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration der Spur saisonwechsel, Teil 2 (Schema 22 → 23, vorläufige Nummer – die Leitstelle nummeriert in der Integration):
/// neues Ereignis <see cref="SeasonEvent.ForcedRelegation"/> (Zwangsabstieg, Statusbit 0x04). Ereignisse stehen mit ihrem Namen
/// im Spielstand; ältere Stände enthalten den neuen Namen nicht, die Felder bleiben unverändert.
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigrateSaisonwechsel(JsonObject root) => root;
}
