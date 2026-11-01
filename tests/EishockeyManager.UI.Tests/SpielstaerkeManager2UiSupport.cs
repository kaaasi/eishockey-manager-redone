using EishockeyManager.Core;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Testaufbau (spielstaerke-manager-2): Managervereine spielen seit 0x98F8 mit den Werten ihrer Spieler. Tests, die einen
/// Managerverein mit der Spielstärke seiner Datenbank voraussetzen (Tabellenplatz als Vorbedingung), setzen die
/// Spielerwerte darauf (wie <c>ManagedSquads</c> in den Core-Tests).
/// </summary>
internal static class ManagedSquadsUi
{
    public static void PlayAtDatabaseStrength(GameState game)
    {
        foreach (var m in game.Managers)
        {
            var club = game.Clubs[m.ClubIndex];
            var value = Math.Min(Player.MaxValue, club.Strength);
            foreach (var p in club.Players)
            {
                p.Condition = p.Technique = value;
                p.Form = 50;
            }
        }
    }

    /// <summary>
    /// Vorbedingung „Managerverein wird Bundestrainer“ (unter den ersten fünf der Bundesliga): Spielerwerte 99. Seit der
    /// Wanderung der Teamwerte (0x9D1B, spielberechnung-komplett, #204) liegen die Computervereine der Bundesliga zwischen
    /// 69 und 94 (in der Saison bis 99); mit Datenbankstärke erreichte Verein 2 die ersten fünf in 10 von 12 Seeds, mit
    /// 99 in 12 von 12. Nur als Vorbedingung erlaubt.
    /// </summary>
    public static void PlayAtTopStrength(GameState game) => PlayAtTopStrength(game, [.. Enumerable.Range(0, game.Managers.Count)]);

    /// <summary>
    /// Nur die Vereine der genannten Manager (Spielberechnung Teil 5: mit dem Penaltyschießen 0x3901B ändert sich der
    /// Zufallsverbrauch, zwei gleich starke Managervereine tauschen dann die Tabellenplätze).
    /// </summary>
    public static void PlayAtTopStrength(GameState game, params int[] managers)
    {
        foreach (var m in managers.Select(i => game.Managers[i]))
            foreach (var p in game.Clubs[m.ClubIndex].Players)
            {
                p.Condition = p.Technique = Player.MaxValue;
                p.Form = 50;
            }
    }
}
