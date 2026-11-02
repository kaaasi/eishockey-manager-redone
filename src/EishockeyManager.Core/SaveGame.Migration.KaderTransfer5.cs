using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration der Spur kader-transfer, Teil 5 (Schema 24 → 25, vorläufig; die Leitstelle nummeriert in der Integration):
/// Übertrag der Spiele beim jetzigen Verein (<see cref="Player.ClubGamesCarry"/>, Kaderplatz +0x1C) und die Zähler je Liga der
/// Poolumverteilung (<c>_poolTurn…</c>, DGROUP 0x48FB). [APPROX] Ältere Stände kennen frühere Saisons nicht: 0, es zählen die Spiele
/// der laufenden Saison wie bisher; die Zähler beginnen wie beim Spielstart bei 0.
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigrateKaderTransfer5(JsonObject root)
    {
        foreach (var market in MarketObjects(root))
        {
            market["_poolTurnBundesliga"] ??= 0;
            market["_poolTurnZweiteLiga"] ??= 0;
            market["_poolTurnOberliga"] ??= 0;
            SortFields(market);
        }
        if (root["spieler"] is not JsonArray players)
            return root;
        foreach (var player in players.OfType<JsonObject>())
        {
            player["ClubGamesCarry"] ??= 0;
            SortFields(player);
        }
        return root;
    }
}
