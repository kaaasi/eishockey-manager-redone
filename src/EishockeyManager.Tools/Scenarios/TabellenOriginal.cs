using EishockeyManager.UI;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.Tools.Scenarios;

/// <summary>
/// Spur tabellen-original-2: Tabellenansicht (em.exe 0x4F78D) zum Pixelvergleich mit dem Video „Die Oberfläche von EM“
/// (13:07, <c>oberflaeche_1307</c>: „Tabelle 1. Liga“ beim Spielstart, Berliner SC in der Oberliga) und nach drei
/// Spieltagen (Bilanz mit „-“ aufgefüllt, Heim klein).
/// </summary>
internal static class TabellenOriginal
{
    [Scenario("tabelle-original")]
    private static void Table(ScreenDriver d)
    {
        d.StartNewGame(); // Berliner SC wie im Video
        d.OpenTile("Tabelle 1. Liga");
        d.Move(118, 214); d.Save("00_tabelle_1liga_video_13_07");
        d.Press(Key.Escape);
        d.OpenTile("Tabelle Oberliga");
        d.Move(118, 214); d.Save("01_tabelle_oberliga_spielbeginn");
        d.Press(Key.Escape);
        for (var md = 0; md < 3; md++)
            d.PlayMatchDay();
        d.OpenTile("Tabelle Oberliga");
        d.Move(240, 230); d.Save("02_tabelle_oberliga_3_spieltage");
        d.Press(Key.Escape);
    }
}
