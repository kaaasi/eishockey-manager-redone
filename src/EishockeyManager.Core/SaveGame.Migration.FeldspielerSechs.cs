using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration der Spur feldspieler-sechs (Runde 23, #939; Schema 62 → 63): Der Feldspieler im Tor (<c>_goalieStandIn</c>, A672) steht
/// jetzt auf dem sechsten Feldspielerplatz (<see cref="LineupSlot.SixthSkater"/>, Feld <c>_sixthSkater</c>). [ORIGINAL] Beide sind im
/// Original derselbe Fall: ein dritter Spieler der Abwehrhälfte in Reihe 1 (+0x15 = 1, Gitter Datei 0x40478–0x404D7). Ältere Stände
/// kennen den sechsten Platz sonst nicht; der Wert wandert unverändert (auch ein Verweis <c>$ref</c>).
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigrateFeldspielerSechs(JsonObject root)
    {
        RenameStandIn(root);
        return root;
    }

    /// <summary>Jede Aufstellung (Objekt mit <c>_goalkeeper</c>): <c>_goalieStandIn</c> → <c>_sixthSkater</c>; Stände ohne beides bleiben
    /// unverändert (Schema 59 legt <c>_goalieStandIn</c> an).</summary>
    private static void RenameStandIn(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject obj:
                if (obj.ContainsKey("_goalkeeper") && obj.ContainsKey("_goalieStandIn"))
                {
                    // Ältere Migrationen legen _goalieStandIn auch in Ständen an, die _sixthSkater schon haben (Testaufbauten aus dem
                    // aktuellen Schema); dann gilt der vorhandene sechste Platz.
                    var value = obj["_goalieStandIn"]?.DeepClone();
                    obj.Remove("_goalieStandIn");
                    if (!obj.ContainsKey("_sixthSkater"))
                        obj["_sixthSkater"] = value;
                    SortFields(obj);
                }
                foreach (var child in obj.Select(kv => kv.Value).ToList())
                    RenameStandIn(child);
                break;
            case JsonArray array:
                foreach (var child in array)
                    RenameStandIn(child);
                break;
        }
    }
}
