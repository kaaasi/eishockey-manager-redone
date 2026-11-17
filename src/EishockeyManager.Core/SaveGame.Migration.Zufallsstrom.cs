using System.Text.Json.Nodes;

namespace EishockeyManager.Core;

/// <summary>
/// Migration Schema 52 → 53 (Runde 21, Spur zufallsstrom, N3 = B): Die eigenen Zufallsquellen der Tendenz
/// (<c>GameState._trendRandom</c>, A725) und des Faktors je Managerzug (<c>GameState._turnRandom</c>, A533) entfallen; beide
/// Stellen ziehen aus dem gemeinsamen Strom <c>GameState.Random</c>, der schon im Spielstand steht. [APPROX] Ein älterer Stand
/// setzt seine Folge deshalb mit anderen Würfen fort als vor der Migration (der Zustand der entfallenen Quellen wird verworfen);
/// der gespeicherte Faktor des laufenden Zugs (<c>_contractTurnFactor</c>) bleibt. Die Objektnummern werden neu vergeben.
/// </summary>
public static partial class SaveGame
{
    private static JsonObject MigrateZufallsstrom(JsonObject root)
    {
        if (root["spiel"] is JsonObject game)
        {
            var removed = game.Remove("_trendRandom");
            removed |= game.Remove("_turnRandom");
            if (removed)
                Renumber(root, new Dictionary<int, int>(), [0]); // die Quellen waren Objekte mit eigener Nummer
        }
        return root;
    }

    /// <summary>
    /// Migration Schema 53 → 54 (Runde 21, Spur zufallsstrom, Teil 4, N3 = B): Die übrigen eigenen Zufallsquellen entfallen –
    /// Pokale (<c>GameState._cupRandom</c>) und Finanzen (<c>GameState._financeRandom</c>). Alle Stellen ziehen aus dem gemeinsamen Strom <c>GameState.Random</c>. [APPROX] wie
    /// 52 → 53: Ein älterer Stand setzt seine Folge mit anderen Würfen fort (der Zustand der entfallenen Quellen wird verworfen).
    /// </summary>
    private static JsonObject MigrateZufallsstromQuellen(JsonObject root)
    {
        if (root["spiel"] is JsonObject game)
        {
            var removed = false;
            foreach (var field in RemovedSources)
                removed |= game.Remove(field);
            if (removed)
                Renumber(root, new Dictionary<int, int>(), [0]); // die Quellen waren Objekte mit eigener Nummer
        }
        return root;
    }

    /// <summary>Felder der mit Schema 54 entfallenen Quellen.</summary>
    private static readonly string[] RemovedSources = ["_cupRandom", "_financeRandom"];
}
