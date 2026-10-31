using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur nationalteam-2 (Runde 8, Welle 3). Bildnamen beziehen sich auf `dosbox/nur-weltkugel/` im Referenz-Repo.
/// </summary>
public class Nationalteam2Tests
{
    /// <summary>Spielvorbereitung mit einem Manager im Modus „nur Weltkugel“ bis „Los jetzt!“.</summary>
    internal static ScreenDriver StartWorldOnlyThroughSetup()
    {
        var d = FlowTests.CreateDriver();
        d.Host.Show(new TitleScreen(d.Host));
        d.Frame();
        d.Click(160, 120);
        Assert.IsType<SetupScreen>(d.Host.Current);
        d.Click(20, 60);
        var cell = SetupScreen.HeadCell(3);
        d.Click(cell.X + 5, cell.Y + 5);
        d.Type("LetsShow");
        d.Press(Key.Enter);
        d.Click(SetupScreen.ModeFlag.X + 5, SetupScreen.ModeFlag.Y + 5); // Fahne aus → nur Weltkugel
        d.Click(SetupScreen.StartButton.X + 5, SetupScreen.StartButton.Y + 5);
        return d;
    }

    /// <summary>
    /// #195, DOSBox <c>06_hauptmenue_bundestrainer_14_april.png</c>: Nach „Los jetzt!“ hält das Original im Modus 1 sofort am
    /// Samstag, 14. April, im Menü des Bundestrainers (Gegner Russland, Konto 500.000 DM). [ORIGINAL] Der Halt liegt in der
    /// Tagesfunktion Datei 0x54C9F: Sie springt am ersten Tag (0x54CEE) zum WM-Termin, ernennt den Bundestrainer (0xD0BA)
    /// und ruft am Tagesende das Hauptmenü 0x44E22 für Manager 5 (0x555A3, `call 0x110:0x66`) – ein Menü am 10. Sept.
    /// davor gibt es nicht. Vorher zeigte das Remake das Menü am Tag 0; gesprungen wurde erst mit „und tschüß“.
    /// </summary>
    [GameDataFact]
    public void Mode1_FirstHaltAfterStart_IsFirstWorldCupDay()
    {
        var d = StartWorldOnlyThroughSetup();
        Assert.IsType<MainMenuScreen>(d.Host.Current);
        var game = d.Host.Game!;
        Assert.Equal(GameMode.WorldOnly, game.Mode);
        Assert.Equal(0, game.PlayedMatchDays);
        Assert.Equal(GameState.FirstWorldCupDay, game.CurrentDay);
        Assert.NotNull(game.NationalCoach);
        Assert.True(game.NationalCoachMenu);
        Assert.Equal(500_000, game.Managers[0].Money);
    }
}
