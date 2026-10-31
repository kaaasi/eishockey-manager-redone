using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.Tools.Scenarios;

/// <summary>Bildschirm Einstellungen (Kachel unter „Gemischtes“).</summary>
internal static class Einstellungen
{
    [Scenario("einstellungen")]
    private static void Settings(ScreenDriver d)
    {
        GameSettings.StorePath = null; // Bildschirmfotos ändern keine echten Einstellungen
        GameSettings.Current = new GameSettings();
        d.StartNewGame();
        d.OpenTile("Einstellungen"); d.Move(10, 230); d.Save("01_einstellungen");
        var down = SettingsScreen.ArrowRect(SettingsScreen.Row.EffectsVolume);
        for (var i = 0; i < 4; i++) d.Click(down.X + 4, down.Bottom - 3);
        var row = SettingsScreen.RowRect(SettingsScreen.Row.GoalScenes);
        d.Click(row.X + 10, row.Y + 5);
        d.Move(10, 230); d.Save("02_geaendert");
        d.Click(SettingsScreen.OptionsButton.X + 5, SettingsScreen.OptionsButton.Y + 5);
        d.Move(10, 230); d.Save("03_spielstand_optionen");
    }
}
