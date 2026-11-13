using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.Tools.Scenarios;

/// <summary>Kachel „Einstellungen“ (Optionsfenster) und Geräteeinstellungen über den Knopf „Gerät“ (#836).</summary>
internal static class Einstellungen
{
    [Scenario("einstellungen")]
    private static void Settings(ScreenDriver d)
    {
        GameSettings.StorePath = null; // Bildschirmfotos ändern keine echten Einstellungen
        GameSettings.Current = new GameSettings();
        d.StartNewGame();
        d.OpenTile("Einstellungen"); d.Move(10, 230); d.Save("00_optionen"); // Optionsfenster des Originals (#836)
        var device = GameOptionsScreen.DeviceButton;
        d.Click(device.X + 5, device.Y + 5); d.Move(10, 230); d.Save("01_einstellungen");
        var down = SettingsScreen.ArrowRect(SettingsScreen.Row.EffectsVolume);
        for (var i = 0; i < 4; i++) d.Click(down.X + 4, down.Bottom - 3);
        var row = SettingsScreen.RowRect(SettingsScreen.Row.Fullscreen);
        d.Click(row.X + 10, row.Y + 5);
        d.Move(10, 230); d.Save("02_geaendert");
        d.Click(SettingsScreen.BackButton.X + 5, SettingsScreen.BackButton.Y + 5);
        d.Move(10, 230); d.Save("03_zurueck_optionen");
    }
}
