using EishockeyManager.Core;
using EishockeyManager.UI.Screens.Team;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.Tools.Scenarios;

/// <summary>
/// Spur kader-start Runde 14 Teil 6: Vereinsaufstellung mit nicht eingesetzten Spielern (A364, NR leer; Vergleich DOSBox
/// <c>file34_3</c>) und gewählter Reihe (A010, Nummer in Farbe 0x10).
/// </summary>
internal static class KaderStartRunde14Teil6
{
    [Scenario("aufstellung-reihen")]
    private static void Lineup(ScreenDriver d)
    {
        d.StartNewGame();
        d.OpenTile("Aufstellung");
        var club = d.Host.Game!.Clubs[d.Host.Game.ActiveManager.ClubIndex];
        club.AutoLineupEnabled = false;
        var rows = TeamUi.Rows(club).ToList();
        foreach (var p in rows.Where(p => club.IsDressed(p) && !club.Lineup.Contains(p)).Take(3).ToList())
            d.RightClick(150, LineupScreen.FirstRowY + rows.IndexOf(p) * LineupScreen.RowPitch + 2);
        var box = LineupScreen.BlockBox(2);
        d.Click(box.X + 3, box.Y + 5);
        d.Move(300, 236);
        d.Save("01_nicht_eingesetzt_reihe3");
    }
}
