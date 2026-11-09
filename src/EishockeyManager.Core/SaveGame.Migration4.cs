using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration Schema 4 → 5 (Spur taktik-verein):
/// <list type="bullet">
/// <item>Eigene Taktikwerte haben eine neue Bedeutung (Einsatz 0–100 %, Angriff 0–7 statt −3…+3, siehe
///   <see cref="Tactics"/>). Alte Werte lassen sich nicht umrechnen; Verein und Bundestrainer bekommen die Startwerte
///   eines neuen Teams (seit Runde 14 alle 0, A180: <c>new Tactics()</c>). Modi, Torwart- und Linienwechsel und Minuten
///   bleiben. [APPROX] Wer „SEMI“ oder „MANUELL“ eingestellt hatte, spielt danach mit den Startwerten.</item>
/// <item>Vereine bekommen „Eingesetzt“ (<c>_dressed</c>, <c>_dressedByHand</c>): niemand von Hand eingesetzt, es gilt
///   die automatische Auswahl (<see cref="Club.Dressed"/>).</item>
/// </list>
/// Unerwartete Formen bleiben unverändert; der Reader meldet sie.
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigrateFrom4(JsonObject root)
    {
        var start = new Tactics();
        if (root["vereine"] is JsonArray clubs)
        {
            foreach (var club in clubs.OfType<JsonObject>())
            {
                ResetTacticValues(club["Tactics"] as JsonObject, start);
                club["_dressed"] ??= new JsonObject { ["$id"] = NewObjectId, ["items"] = new JsonArray() };
                club["_dressedByHand"] ??= false;
                SortFields(club);
            }
        }
        if (root["spiel"] is JsonObject game && game["NationalCoach"] is JsonObject coach)
            ResetTacticValues(coach["Tactics"] as JsonObject, start);

        Renumber(root, new Dictionary<int, int>(), [0]);
        return root;
    }

    private static void ResetTacticValues(JsonObject? tactics, Tactics start)
    {
        if (tactics is null)
            return;
        tactics["_effort"] = Grid(Tactics.Blocks, Tactics.Situations, (b, i) => start.Effort(b, (ScoreSituation)i));
        tactics["_attack"] = Grid(Tactics.Blocks, Tactics.Situations, (b, i) => start.Attack(b, (ScoreSituation)i));
    }

    /// <summary>Zweidimensionales Feld wie der Writer es schreibt (<c>dims</c>, <c>items</c> zeilenweise).</summary>
    private static JsonObject Grid(int rows, int columns, Func<int, int, int> value)
    {
        var items = new JsonArray();
        for (var r = 0; r < rows; r++)
            for (var c = 0; c < columns; c++)
                items.Add(JsonValue.Create(value(r, c)));
        return new JsonObject { ["$id"] = NewObjectId, ["dims"] = new JsonArray(rows, columns), ["items"] = items };
    }
}
