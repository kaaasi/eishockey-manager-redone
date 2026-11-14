using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;

namespace EishockeyManager.UI.Tests;

/// <summary>Spur nutzer-befunde, #835: Managerkopf im Hauptmenü ohne schwarzen Rand.</summary>
public class NutzerBefundeMainMenuTests
{
    /// <summary>
    /// Nutzerprobe: der Kopf wurde als ganze 40 × 40-Zelle aus kb\0 samt dunklem Rand (Farbe 8) nach (232 | 33) gesetzt.
    /// Original Datei 0x44431–0x4445F: 38 × 38 nach (233 | 34); Videobild oberflaeche_0410 ohne dunklen Rand
    /// (mittlere Abweichung im Kasten 20,5 → 12,1).
    /// </summary>
    [GameDataFact]
    public void ManagerHead_IsDrawnWithoutCellBorder()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        Assert.IsType<MainMenuScreen>(d.Host.Current);
        var pixels = d.Surface.Pixels;
        var manager = d.Host.Game!.ActiveManager;
        var sheet = d.Host.Assets.Image(0);
        var cell = Widgets.HeadSource(manager.Head);
        // Rand der Zelle ist dunkel (8), Inhalt liegt um 1 Pixel versetzt.
        Assert.Equal(8, sheet.Pixels[(cell.Y + 10) * sheet.Width + cell.X]);
        for (var y = 0; y < MainMenuScreen.HeadSize; y++)
            for (var x = 0; x < MainMenuScreen.HeadSize; x++)
                Assert.Equal(sheet.Pixels[(cell.Y + 1 + y) * sheet.Width + cell.X + 1 + x],
                    pixels[(MainMenuScreen.HeadPosition.Y + y) * 320 + MainMenuScreen.HeadPosition.X + x]);
        // Zwischen hellem Kastenrand (x 231, y 32) und Kopf liegt keine dunkle Linie mehr.
        for (var i = 0; i < 40; i++)
        {
            Assert.NotEqual(8, pixels[33 * 320 + 232 + i]);
            Assert.NotEqual(8, pixels[(33 + i) * 320 + 232]);
            Assert.NotEqual(8, pixels[(33 + i) * 320 + 271]);
        }
    }
}
