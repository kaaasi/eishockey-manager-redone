using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Competitions;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur nationalteam-2: Aufstellung und Taktik des Nationalteams im Menü des Bundestrainers.
/// </summary>
public class NationalLineupScreenTests
{
    /// <summary>Spielmodus „nur Weltkugel“ starten und bis zur Ernennung springen (wie WorldOnlyMenuTests).</summary>
    internal static (ScreenDriver Driver, GameState Game) StartWorldOnly()
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
        d.Click(SetupScreen.ModeFlag.X + 5, SetupScreen.ModeFlag.Y + 5);
        d.Click(SetupScreen.StartButton.X + 5, SetupScreen.StartButton.Y + 5);
        Assert.IsType<MainMenuScreen>(d.Host.Current);
        var game = d.Host.Game!;
        Assert.Equal(GameMode.WorldOnly, game.Mode);
        d.PlayMatchDay();
        Assert.NotNull(game.NationalCoach);
        return (d, game);
    }

    /// <summary>
    /// Befund: Die Kacheln „Aufstellung“ und „Taktik“ des Bundestrainers zeigten nur den Platzhalter.
    /// Jetzt öffnen sie die Bildschirme des Nationalteams; Öffnen, Überfahren und Umschalten ändern nichts.
    /// </summary>
    [GameDataFact]
    public void CoachMenu_OpensNationalLineupAndTactics_WithoutChangingState()
    {
        var (d, game) = StartWorldOnly();
        game.AutoNominate();
        var before = StateFingerprint.Of(game);

        d.OpenTile("Aufstellung");
        Assert.IsNotType<PlaceholderScreen>(d.Host.Current);
        Assert.IsType<NationalLineupScreen>(d.Host.Current);
        d.Move(100, 60);
        d.Frame();
        d.Press(Key.Escape);
        Assert.IsType<MainMenuScreen>(d.Host.Current);

        d.OpenTile("Taktik");
        Assert.IsNotType<PlaceholderScreen>(d.Host.Current);
        d.Frame();
        d.Press(Key.Escape);
        Assert.IsType<MainMenuScreen>(d.Host.Current);
        Assert.Equal(before, StateFingerprint.Of(game));
    }
}
