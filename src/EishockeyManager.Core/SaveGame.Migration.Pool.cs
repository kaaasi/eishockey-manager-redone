using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration der Spur pool (Runde 16, #743; Schema 48 → 49): Managerkader im Spielerpool.
/// <list type="number">
/// <item>Feld <see cref="PoolSlot.Manager"/> (Teil 3): −1 an jedem Poolplatz; nach dem Laden bekommt ein vergebener Platz ohne Spieler
///   im Spiel (nach Vertragsende verloren) den einzigen Manager, bei mehreren bleibt er unbekannt ([APPROX], Export dann 6).</item>
/// <item>Ältere Remake-Spielstände führen die Startkader außerhalb der 199 Poolplätze: Nach dem Laden setzt
///   <see cref="TransferMarket.SeatManagerSquads"/> jeden Kaderspieler eines Managers ohne Platz auf einen freien Ligaplatz seiner Art
///   ([APPROX], dessen Poolspieler entfällt; im Original stehen sie seit dem Spielstart auf den Plätzen 2…, 22…, 98…,
///   <c>docs/formate/spielstand-original.md</c>, „Managerkader im Spielerpool“). Verbraucht keinen Zufall.</item>
/// </list>
/// </summary>
public static partial class SaveGame
{
    /// <summary>Erste Schemaversion, in der die Managerkader im Pool stehen.</summary>
    private const int PoolSeatedSchema = 49;

    private static JsonObject MigratePool(JsonObject root)
    {
        foreach (var market in MarketObjects(root))
        {
            if (market["_pool"]?["items"] is not JsonArray items)
                continue;
            foreach (var slot in items.OfType<JsonObject>())
            {
                slot["Manager"] ??= -1;
                SortFields(slot);
            }
        }
        return root;
    }

    /// <summary>Nach dem Laden eines Spielstands vor <see cref="PoolSeatedSchema"/>: Managerkader in den Pool, verlorene Plätze.</summary>
    private static void AfterLoadPool(GameState game, int loadedSchema)
    {
        if (loadedSchema >= PoolSeatedSchema)
            return;
        game.TransferMarket.SeatManagerSquads(game);
        if (game.Managers.Count != 1 || game.WorldOnly)
            return;
        var inGame = game.AllPlayers.ToHashSet(ReferenceEqualityComparer.Instance);
        foreach (var slot in game.TransferMarket.Pool.Where(s => s.Owner == PoolOwner.Taken && s.Manager < 0 && !inGame.Contains(s.Player)))
            slot.Manager = 0;
    }
}
