using EishockeyManager.Core;
using EishockeyManager.UI;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.Tools.Scenarios;

/// <summary>
/// Spur statistiken (Runde 8, #135): Statistiken → Spieler (em.exe 0x25E30) und Fairplay (0x2A1E0) zu Saisonbeginn und
/// nach zehn Spieltagen. Vergleich mit den Fundstellen in <c>docs/referenz-videos.md</c> (Spielerstatistik, Fairplay).
/// </summary>
internal static class Statistiken
{
    [Scenario("statistiken")]
    private static void Run(ScreenDriver d)
    {
        d.StartNewGame();
        var game = d.Host.Game!;
        Shots(d, "00_saisonbeginn");
        for (var i = 0; i < 10 && game.Phase == SeasonPhase.Running; i++)
            d.PlayMatchDay();
        if (d.Host.Current is not MainMenuScreen)
            d.Host.Show(new MainMenuScreen(d.Host));
        Shots(d, "01_spieltag10");
    }

    private static void Shots(ScreenDriver d, string prefix)
    {
        d.OpenTile("Spieler");
        d.Move(300, 230); d.Save(prefix + "_spieler_scorer_liga");
        d.Click(54 + 5, 182 + 5);
        d.Move(300, 230); d.Save(prefix + "_spieler_torschuetzen_liga");
        d.Click(212 + 5, 195 + 5);
        d.Move(300, 230); d.Save(prefix + "_spieler_torschuetzen_manager");
        d.Press(Key.Escape);

        d.OpenTile("Fairplay");
        d.Move(300, 230); d.Save(prefix + "_fairplay");
        d.Press(Key.Escape);
    }
}
