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
}
