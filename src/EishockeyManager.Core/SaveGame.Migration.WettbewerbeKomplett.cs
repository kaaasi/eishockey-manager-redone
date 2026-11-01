using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration der Spur wettbewerbe-komplett (Teil 2; Schema 17 → 18, vorläufig, Nummer in der Integration): Rekorde
/// (<c>_teamRecords</c>, <c>_teamRecordOpponents</c>), Kopf-an-Kopf-Liste (<c>_headToHead</c>, leer = 255) und Bilanz der
/// Länder (<c>_nationalForm</c>) für das Gegner-Fenster (<see cref="GameState.TeamRecord"/>). [APPROX] Ältere Spielstände
/// haben nichts gezählt und beginnen leer; ab dem nächsten Länderspiel wird eingetragen. Unerwartete Formen bleiben
/// unverändert; der Reader meldet sie.
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigrateWettbewerbeKomplett(JsonObject root)
    {
        if (root["spiel"] is not JsonObject game)
            return root;
        var records = TeamRecords.Teams * TeamRecords.Kinds;
        game["_teamRecords"] ??= Filled(records, 0);
        game["_teamRecordOpponents"] ??= Filled(records, 0);
        game["_headToHead"] ??= Filled(GameState.HeadToHeadSlots * TeamRecords.Teams * GameState.HeadToHeadLength, GameState.HeadToHeadEmpty);
        game["_nationalForm"] ??= Filled((NationalTeams.Last - NationalTeams.First + 1) * GameState.NationalFormLength, 0);
        SortFields(game);
        Renumber(root, new Dictionary<int, int>(), [0]);
        return root;
    }

    /// <summary>Neues Feld <c>int[]</c> wie der Writer, alle Werte <paramref name="value"/>.</summary>
    private static JsonObject Filled(int count, int value)
    {
        var items = new JsonArray();
        for (var i = 0; i < count; i++)
            items.Add(value);
        return new JsonObject { ["$id"] = NewObjectId, ["items"] = items };
    }
}
