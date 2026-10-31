using EishockeyManager.Core;
using EishockeyManager.UI;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.Tools.Scenarios;

/// <summary>
/// Spur statistiken-verein (Runde 8, Welle 3): Statistiken → Verein (em.exe 0x2D5AE) zu Spielbeginn und nach drei
/// Spieltagen, dazu Spieler und Fairplay nach zehn Spieltagen. Vergleich mit <c>folge01_00522</c>, <c>folge01_01022</c>,
/// <c>folge15_01001</c> (Fairplay) und <c>folge15_01007</c> (Spieler).
/// </summary>
internal static class StatistikenVerein
{
    [Scenario("statistiken-verein")]
    private static void Run(ScreenDriver d)
    {
        d.StartNewGame();
        var game = d.Host.Game!;
        Verein(d, "00_spielbeginn");
        for (var i = 0; i < 3 && game.Phase == SeasonPhase.Running; i++)
            d.PlayMatchDay();
        Menu(d);
        Verein(d, "01_spieltag3");
        for (var i = 0; i < 7 && game.Phase == SeasonPhase.Running; i++)
            d.PlayMatchDay();
        Menu(d);
        Verein(d, "02_spieltag10");
        d.OpenTile("Spieler");
        d.Move(300, 230); d.Save("02_spieltag10_spieler");
        d.Press(Key.Escape);
        d.OpenTile("Fairplay");
        d.Move(300, 230); d.Save("02_spieltag10_fairplay");
        d.Press(Key.Escape);
    }

    private static void Menu(ScreenDriver d)
    {
        if (d.Host.Current is not MainMenuScreen)
            d.Host.Show(new MainMenuScreen(d.Host));
    }

    private static void Verein(ScreenDriver d, string prefix)
    {
        d.OpenTile("Verein");
        d.Move(300, 230); d.Save(prefix + "_verein");
        d.Press(Key.Escape);
    }
}
