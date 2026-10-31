using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration Schema 11 → 12 (Spur nationalteam, #192; Nummer bestätigt in der Integration Runde 8): Taktik des
/// Nationalteams ohne Startwert.
/// <list type="bullet">
/// <item>Jede Taktik bekommt die Felder der Anzeige unter „AUTOM.“ (<c>_shownAutoEffort</c>, <c>_shownAutoAttack</c>):
///   bei Vereinen null (Anzeige wie bisher), beim Bundestrainer je sechs Nullen.</item>
/// <item>Der Spielstand bekommt die Taktik des Nationalteams ohne Bundestrainer (<c>_vacantNationalTactics</c>): ist
///   ein Bundestrainer im Amt, hält er sie (null); sonst eine neue Taktik mit allen Werten 0 und Modi „AUTOM.“/
///   „Automatisch“ wie <see cref="Tactics.National"/>.</item>
/// <item>[APPROX] Ein alter Spielstand kennt die Werte des letzten WM-Spiels nicht: Die Anzeige unter „AUTOM.“ beginnt
///   bei 0 und zeigt ab dem nächsten WM-Spiel des Landes dessen Werte.</item>
/// <item>[APPROX] Eigene Werte eines amtierenden Bundestrainers: Stehen sie in allen vier Blöcken noch auf dem alten
///   Startwert (Zeile eines neuen Vereins, <c>new Tactics()</c>), gelten sie als nie geändert und bekommen den
///   Startwert des Nationalteams (0); von Hand geänderte Werte bleiben.</item>
/// </list>
/// Unerwartete Formen bleiben unverändert; der Reader meldet sie.
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigrateFromNationalteam(JsonObject root)
    {
        if (root["spiel"] is not JsonObject game)
            return root;

        var coachTactics = (game["NationalCoach"] as JsonObject)?["Tactics"] as JsonObject;
        foreach (var tactics in TacticsObjects(root))
        {
            var national = ReferenceEquals(tactics, coachTactics);
            if (national)
                ResetUntouchedStartValues(tactics);
            tactics["_shownAutoEffort"] ??= national ? ZeroLine() : null;
            tactics["_shownAutoAttack"] ??= national ? ZeroLine() : null;
            SortFields(tactics);
        }

        if (!game.ContainsKey("_vacantNationalTactics"))
        {
            game["_vacantNationalTactics"] = coachTactics is not null ? null : new JsonObject
            {
                ["$id"] = NewObjectId,
                ["AttackMode"] = nameof(TacticMode.Auto),
                ["EffortMode"] = nameof(TacticMode.Auto),
                ["Goalie"] = nameof(GoalieMode.Auto),
                ["LineChange"] = nameof(LineChangeMode.Automatic),
                ["_attack"] = EmptyGrid(Tactics.Blocks, Tactics.Situations, 0),
                ["_effort"] = EmptyGrid(Tactics.Blocks, Tactics.Situations, 0),
                ["_minutes"] = EmptyGrid(Tactics.Blocks, 2, 0),
                ["_shownAutoAttack"] = ZeroLine(),
                ["_shownAutoEffort"] = ZeroLine(),
            };
            SortFields(game);
        }

        Renumber(root, new Dictionary<int, int>(), [0]);
        return root;
    }

    /// <summary>Alte Startwerte (alle Blöcke wie ein neuer Verein) durch den Startwert des Nationalteams (0) ersetzen.</summary>
    private static void ResetUntouchedStartValues(JsonObject tactics)
    {
        var start = new Tactics();
        if (!IsGrid(tactics["_effort"], (b, i) => start.Effort(b, (ScoreSituation)i))
            || !IsGrid(tactics["_attack"], (b, i) => start.Attack(b, (ScoreSituation)i)))
            return;
        tactics["_effort"] = EmptyGrid(Tactics.Blocks, Tactics.Situations, 0);
        tactics["_attack"] = EmptyGrid(Tactics.Blocks, Tactics.Situations, 0);
    }

    private static bool IsGrid(JsonNode? node, Func<int, int, int> value)
    {
        if (node is not JsonObject grid || grid["items"] is not JsonArray items
            || items.Count != Tactics.Blocks * Tactics.Situations)
            return false;
        for (var b = 0; b < Tactics.Blocks; b++)
            for (var i = 0; i < Tactics.Situations; i++)
                if (items[b * Tactics.Situations + i] is not JsonValue v || !v.TryGetValue<int>(out var n) || n != value(b, i))
                    return false;
        return true;
    }

    /// <summary>Sechs Nullen (Grundwert und fünf Zuschläge) wie der Writer ein <c>int[]</c> schreibt.</summary>
    private static JsonObject ZeroLine()
    {
        var items = new JsonArray();
        for (var i = 0; i < Tactics.Situations; i++)
            items.Add(0);
        return new JsonObject { ["$id"] = NewObjectId, ["items"] = items };
    }

    /// <summary>Alle ausgeschriebenen Taktiken (Objekte mit Einsatz, Angriff, Minuten und Modus).</summary>
    private static List<JsonObject> TacticsObjects(JsonNode? node)
    {
        var found = new List<JsonObject>();
        Collect(node);
        return found;

        void Collect(JsonNode? current)
        {
            switch (current)
            {
                case JsonObject obj:
                    if (obj.ContainsKey("_effort") && obj.ContainsKey("_attack") && obj.ContainsKey("_minutes")
                        && obj.ContainsKey("EffortMode"))
                        found.Add(obj);
                    foreach (var (_, child) in obj.ToArray())
                        Collect(child);
                    break;
                case JsonArray array:
                    foreach (var child in array.ToArray())
                        Collect(child);
                    break;
            }
        }
    }
}
