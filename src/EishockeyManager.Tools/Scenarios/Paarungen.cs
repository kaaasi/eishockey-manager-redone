using EishockeyManager.UI.Screens.Competitions;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.Tools.Scenarios;

/// <summary>Bildschirm „Paarungen Liga“ (Kacheln unter „Paarungen“). Kein Videobild, daher nur Sichtprüfung.</summary>
internal static class Paarungen
{
    [Scenario("paarungen-liga")]
    private static void LeaguePairings(ScreenDriver d)
    {
        d.StartNewGame();
        d.OpenTile("Paarungen 1. Liga"); d.Move(10, 230); d.Save("01_bundesliga_vor_dem_spieltag");
        d.Press(UI.Key.Escape);
        d.PlayMatchDay();
        d.OpenTile("Paarungen 1. Liga"); d.Move(10, 230); d.Save("02_bundesliga_naechster_tag");
        var left = LeaguePairingsScreen.Buttons[0];
        d.Click(left.X + 5, left.Y + 5); d.Move(10, 230); d.Save("03_bundesliga_gespielter_tag");
        d.Press(UI.Key.Escape);
        d.OpenTile("Paarungen 2. Liga"); d.Move(10, 230); d.Save("04_zweite_liga");
        d.Press(UI.Key.Escape);
        d.OpenTile("Paarungen Oberliga"); d.Move(10, 230); d.Save("05_oberliga");
    }
}
