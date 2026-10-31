using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.Tools.Scenarios;

/// <summary>
/// Spur credits (#207): Klick auf die Titelleiste „EISHOCKEY MANAGER“ im Hauptmenü zeigt das Credits-Fenster kb\15 bei
/// (51 | 61), verlängert um die Zeile „Remake“ / „Jörg Klinge“. Vergleich mit dem DOSBox-Bild des Nutzers.
/// </summary>
internal static class Credits
{
    [Scenario("credits")]
    private static void Open(ScreenDriver d)
    {
        d.StartNewGame();
        d.Move(1, 230); d.Save("01_hauptmenue");
        d.Click(160, 10);
        d.Move(1, 230); d.Save("02_credits");
        d.Click(160, 120);
        d.Move(1, 230); d.Save("03_geschlossen");
    }
}
