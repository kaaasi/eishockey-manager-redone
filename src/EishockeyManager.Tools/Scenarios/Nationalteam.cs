using EishockeyManager.UI;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Team;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.Tools.Scenarios;

/// <summary>Spur nationalteam (Runde 8): Taktik des Nationalteams ohne Startwert (#192).</summary>
internal static class Nationalteam
{
    /// <summary>
    /// Modus 1 („nur Weltkugel“): Taktik nach „auto“ vor dem ersten WM-Spiel (alle Werte 0, wie DOSBox
    /// `nur-weltkugel/26`) und nach dem ersten WM-Spiel (Werte der Automatik dieses Spiels).
    /// </summary>
    [Scenario("nationalteam-taktik")]
    private static void Tactics(ScreenDriver d)
    {
        d.Host.Show(new TitleScreen(d.Host));
        d.Frame();
        d.Click(160, 120);
        d.Click(20, 60);
        var cell = SetupScreen.HeadCell(3);
        d.Click(cell.X + 5, cell.Y + 5);
        d.Type("Joerg");
        d.Press(Key.Enter);
        d.Click(SetupScreen.ModeFlag.X + 5, SetupScreen.ModeFlag.Y + 5);
        d.Click(SetupScreen.StartButton.X + 10, SetupScreen.StartButton.Y + 10);
        d.PlayMatchDay();
        var game = d.Host.Game!;
        game.AutoNominate();
        game.NationalCoach!.AutoFormation();
        d.OpenTile("Aufstellung");
        d.Click(TeamUi.SwitchButton.X + 5, TeamUi.SwitchButton.Y + 5);
        d.Move(300, 235); d.Save("01_taktik_vor_dem_ersten_spiel_wie_dosbox_26");
        d.Press(Key.Escape);

        d.PlayMatchDay();
        d.OpenTile("Aufstellung");
        d.Click(TeamUi.SwitchButton.X + 5, TeamUi.SwitchButton.Y + 5);
        d.Move(300, 235); d.Save("02_taktik_nach_dem_ersten_wm_spiel");
    }
}
