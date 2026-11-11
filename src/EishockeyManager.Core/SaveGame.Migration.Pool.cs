using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration der Spur pool (Runde 16, #743; Schema 47 → 48, die Leitstelle nummeriert beim Merge): Managerkader im Spielerpool.
/// Die Felder bleiben gleich; ältere Remake-Spielstände führen die Startkader aber außerhalb der 199 Poolplätze. Nach dem Laden
/// setzt <see cref="TransferMarket.SeatManagerSquads"/> jeden Kaderspieler eines Managers ohne Platz auf einen freien Ligaplatz
/// seiner Art ([APPROX], dessen Poolspieler entfällt; im Original stehen sie seit dem Spielstart auf den Plätzen 2…, 22…, 98…,
/// <c>docs/formate/spielstand-original.md</c>, „Managerkader im Spielerpool“). Der Schritt verbraucht keinen Zufall.
/// </summary>
public static partial class SaveGame
{
    /// <summary>Erste Schemaversion, in der die Managerkader im Pool stehen.</summary>
    private const int PoolSeatedSchema = 48;

    private static JsonObject MigratePool(JsonObject root) => root; // nur der Schritt nach dem Laden (AfterLoadPool)

    /// <summary>Nach dem Laden eines Spielstands vor <see cref="PoolSeatedSchema"/>: Managerkader in den Pool.</summary>
    private static void AfterLoadPool(GameState game, int loadedSchema)
    {
        if (loadedSchema < PoolSeatedSchema)
            game.TransferMarket.SeatManagerSquads(game);
    }
}
