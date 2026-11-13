using EishockeyManager.Core;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.Tools.Scenarios;

/// <summary>
/// Optionsbildschirm des Originals (Spur optionen-wirkung, Teil 2) für den Pixelvergleich mit den DOSBox-Bildern 48–51
/// (<c>docs/dosbox-befunde.md</c>). Die Schalter werden auf die Stände der Bilder gestellt.
/// </summary>
internal static class OptionenWirkungScenarios
{
    [Scenario("optionen")]
    private static void Options(ScreenDriver d)
    {
        GameSettings.StorePath = null;
        GameSettings.Current = new GameSettings();
        d.StartNewGame();
        d.OpenTile("Einstellungen");
        // Bild 48: DEB-Pokal und WM-Spiele an (Modus „nur Weltkugel“, Bundestrainer-Abschnitt), Torszenen aus
        Click(d, 12);
        d.Move(319, 239); d.Save("48_anfang");
        // Bild 49: Ergebnisse 1. Liga, Tabelle 1./2. Liga an, DEB-Pokal aus (Hintergrundgrafik dort aus – im Remake fest an)
        Click(d, GameOptionsScreen.ReportSwitch(ReportKind.Results, League.Bundesliga));
        Click(d, GameOptionsScreen.ReportSwitch(ReportKind.Table, League.Bundesliga));
        Click(d, GameOptionsScreen.ReportSwitch(ReportKind.Table, League.ZweiteLiga));
        Click(d, 10);
        d.Move(319, 239); d.Save("49_geaendert");
        Click(d, GameOptionsScreen.GoalScenesSwitch);
        d.Move(319, 239); d.Save("50_torszenen_pinguin");
        Click(d, GameOptionsScreen.GoalScenesSwitch);
        d.Move(319, 239); d.Save("51a_meldung");
        d.Click(319, 239);
        d.Move(319, 239); d.Save("51_torszenen_roter_punkt");
    }

    private static void Click(ScreenDriver d, int index)
    {
        var r = GameOptionsScreen.SwitchRect(index);
        d.Click(r.X + r.W / 2, r.Y + r.H / 2);
    }
}
