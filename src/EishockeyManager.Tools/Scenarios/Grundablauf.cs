using EishockeyManager.UI;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.Tools.Scenarios;

/// <summary>Titel, Spielvorbereitung, Hauptmenü, Spieltag und Tabellen.</summary>
internal static class Grundablauf
{
    [Scenario("setup")]
    private static void Setup(ScreenDriver d)
    {
        d.Host.Show(new TitleScreen(d.Host));
        d.Frame(); d.Save("01_titel");
        d.Click(160, 120); d.Move(150, 20); d.Save("02_setup_leer");
        d.Click(20, 60); d.Move(200, 140); d.Save("03_setup_kopfwahl");
        d.Click(200, 100); d.Type("LetsShow"); d.Move(100, 70); d.Save("04_setup_name");
        d.Press(Key.Enter); d.Click(150, 60);
        for (var p = 0; p < 3; p++) d.Wheel(-1);
        d.Move(209, 140); d.Save("05_setup_vereinswahl");
        d.Click(209, 140); d.Move(100, 150); d.Save("06_setup_fertig");
        d.Click(286, 212); d.Move(10, 230); d.Save("07_hauptmenue");
        d.Click(190, 110); d.Move(10, 230); d.Save("08_hauptmenue_statistiken");
        d.Click(45, 110); d.Move(10, 230); d.Save("09_hauptmenue_manager");
        d.Click(27, 220); d.Save("10_platzhalter");
    }

    [Scenario("spieltag")]
    private static void MatchDay(ScreenDriver d)
    {
        d.StartNewGame();
        d.NextDay(); d.Frame(); d.Save("01_banner"); // Tag weiter über das Vereinslogo (#205)
        d.Frames(MatchDayScreen.BannerFrames + 40 * MatchDayScreen.FramesPerMinute);
        d.Save("02_spiele_laufen");
        d.Press(Key.Space); d.Move(10, 230); d.Save("03_nach_spieltag");
        for (var md = 0; md < 16; md++) d.PlayMatchDay();
        d.Move(10, 230); d.Save("04_nach_17_spieltagen");
    }

    [Scenario("tabellen")]
    private static void Tables(ScreenDriver d)
    {
        d.StartNewGame();
        for (var md = 0; md < 17; md++) d.PlayMatchDay();
        d.OpenTile("Tabelle Oberliga"); d.Save("01_oberliga_gesamt");
        d.Click(60, 170); d.Save("02_oberliga_heim");
        d.Press(Key.Escape);
        d.OpenTile("Tabelle 1. Liga"); d.Save("03_bundesliga");
    }
}
