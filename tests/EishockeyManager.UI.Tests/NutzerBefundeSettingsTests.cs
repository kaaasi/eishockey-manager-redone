using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur nutzer-befunde, #836 (R18-E4 = wie im Original): Die Kachel „Einstellungen“ öffnet das Optionsfenster des Originals
/// (em.exe Datei 0x2CD1C); Nutzerprobe main 01ed8a5 öffnete stattdessen die Geräteeinstellungen des Remakes.
/// </summary>
public class NutzerBefundeSettingsTests
{
    [GameDataFact]
    public void SettingsTile_OpensOriginalOptions_DeviceButtonOpensDeviceSettings()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var money = d.Host.Game!.ActiveManager.Money;
        d.OpenTile("Einstellungen");
        var options = Assert.IsType<GameOptionsScreen>(d.Host.Current);
        d.Click(GameOptionsScreen.DeviceButton.X + 5, GameOptionsScreen.DeviceButton.Y + 5);
        Assert.IsType<SettingsScreen>(d.Host.Current);
        d.RightClick(10, 10);
        Assert.Same(options, d.Host.Current);
        d.RightClick(10, 10);
        Assert.IsType<MainMenuScreen>(d.Host.Current);
        Assert.Equal(money, d.Host.Game.ActiveManager.Money);
    }

    /// <summary>Der Zusatzknopf liegt außerhalb der drei Fenster und Schalter des Originals.</summary>
    [Fact]
    public void DeviceButton_DoesNotOverlapOriginalWindows()
    {
        foreach (var window in new[] { GameOptionsScreen.TopWindow, GameOptionsScreen.ReportWindow, GameOptionsScreen.CupWindow })
            Assert.False(Overlaps(window, GameOptionsScreen.DeviceButton));
        for (var i = 0; i < GameOptionsScreen.SwitchCount; i++)
            Assert.False(Overlaps(GameOptionsScreen.SwitchRect(i), GameOptionsScreen.DeviceButton));
    }

    private static bool Overlaps(Rect a, Rect b) => a.X < b.Right && b.X < a.Right && a.Y < b.Bottom && b.Y < a.Bottom;
}
