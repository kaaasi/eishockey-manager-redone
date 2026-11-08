using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration Schema 40 → 41 (Runde 14, Spur kader-start Teil 2b; die Leitstelle nummeriert das Schema bei der Integration):
/// <list type="bullet">
/// <item>A046: Tabellenfeld je Verein (<c>Club._tableRank</c>, DGROUP 0x8288). [APPROX] Ältere Stände: das Platzfeld des
///   zuletzt gespeicherten Ligaspieltags (<c>_rankSlots</c>[<see cref="GameState.RankSlotsRecorded"/> − 1], derselbe
///   Schreiber 0x51049), ohne gespeicherten Spieltag 0 wie beim Spielstart.</item>
/// <item>A135: Tore und Spiele je Wettbewerb (<see cref="Player.CupGames"/> usw.). [APPROX] Ältere Stände zählten alles in der
///   Liga: Pokal und Europapokal 0, Überträge der Tore beim Verein 0 (Tore beim Verein = Saisontore).</item>
/// </list>
/// Unerwartete Formen bleiben unverändert; der Reader meldet sie.
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigrateKaderStart2(JsonObject root)
    {
        var recorded = root["spiel"]?["RankSlotsRecorded"] is JsonValue value && value.TryGetValue<int>(out var n) ? n : 0;
        var slot = Math.Min(recorded, GameState.RankSlotCount) - 1;
        if (root["vereine"] is JsonArray clubs)
            foreach (var club in clubs.OfType<JsonObject>())
            {
                if (club.ContainsKey("_tableRank"))
                    continue;
                club["_tableRank"] = slot >= 0 && club["_rankSlots"]?["items"] is JsonArray slots && slot < slots.Count
                                     && slots[slot] is JsonValue place && place.TryGetValue<int>(out var p) ? p : 0;
                SortFields(club);
            }
        if (root["spieler"] is JsonArray players)
            foreach (var player in players.OfType<JsonObject>())
            {
                foreach (var name in (string[])["CupGames", "EuropeGames", "CupGoals", "EuropeGoals", "ClubCupGamesCarry",
                             "ClubEuropeGamesCarry", "ClubGoalsCarry", "ClubCupGoalsCarry", "ClubEuropeGoalsCarry"])
                    player[name] ??= 0;
                SortFields(player);
            }
        return root;
    }
}
