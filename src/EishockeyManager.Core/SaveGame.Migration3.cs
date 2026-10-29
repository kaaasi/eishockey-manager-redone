using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration Schema 3 → 4 (Spur nationalteam-2): Aufstellung (<c>Formation</c>), Taktik und eingesetzte Spieler
/// (<c>_dressed</c>) des Bundestrainers. Ein Bundestrainer aus Schema 3 bekommt eine leere Aufstellung, die
/// Grundtaktik und niemanden eingesetzt – damit spielt er wie bisher mit der Automatik (<see cref="NationalCoach.Lineup"/>).
/// Ohne Bundestrainer (null) ändert sich nichts. Unerwartete Formen bleiben unverändert; der Reader meldet sie.
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigrateFrom3(JsonObject root)
    {
        if (root["spiel"] is not JsonObject game || game["NationalCoach"] is not JsonObject coach)
            return root;

        coach["Formation"] ??= new JsonObject
        {
            ["$id"] = NewObjectId,
            ["_defenders"] = EmptyGrid(Lineup.Blocks, Lineup.DefendersPerBlock, null),
            ["_forwards"] = EmptyGrid(Lineup.Blocks, Lineup.ForwardsPerBlock, null),
            ["_goalkeeper"] = null,
        };
        coach["Tactics"] ??= new JsonObject
        {
            ["$id"] = NewObjectId,
            ["AttackMode"] = nameof(TacticMode.Auto),
            ["EffortMode"] = nameof(TacticMode.Auto),
            ["Goalie"] = nameof(GoalieMode.Auto),
            ["LineChange"] = nameof(LineChangeMode.Automatic),
            ["_attack"] = EmptyGrid(Tactics.Blocks, Tactics.Situations, 0),
            ["_effort"] = EmptyGrid(Tactics.Blocks, Tactics.Situations, 0),
            ["_minutes"] = EmptyGrid(Tactics.Blocks, 2, 0),
        };
        coach["_dressed"] ??= new JsonObject { ["$id"] = NewObjectId, ["items"] = new JsonArray() };
        SortFields(coach);

        Renumber(root, new Dictionary<int, int>(), [0]);
        return root;
    }

    /// <summary>Zweidimensionales Feld wie der Writer es schreibt (<c>dims</c>, <c>items</c> zeilenweise).</summary>
    private static JsonObject EmptyGrid(int rows, int columns, int? value)
    {
        var items = new JsonArray();
        for (var i = 0; i < rows * columns; i++)
            items.Add(value is { } v ? JsonValue.Create(v) : null);
        return new JsonObject { ["$id"] = NewObjectId, ["dims"] = new JsonArray(rows, columns), ["items"] = items };
    }
}
