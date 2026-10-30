using EishockeyManager.Tests.Support;
using EishockeyManager.Core;
using EishockeyManager.UI;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Befund R2-06 über die Oberfläche (Originaldateien, übersprungen ohne „gamedata“): „und tschüß…“ gibt den Zug
/// weiter, ohne den Tag zu spielen. Die Lampen der Infoleiste sind laut em.exe Warnlampen des Managers am Zug, keine
/// Bereitschaftsanzeige (Spur pixel-verein, PV-03, <see cref="Screens.MenuLamps"/>): bei jedem Zug zeigen sie die
/// Lage des Managers am Zug – zu Spielbeginn nur Lampe 4 (freie Werbeflächen) rot.
/// </summary>
public class HotSeatUiTests
{
    private static (ScreenDriver Driver, GameState Game) Start(int managers)
    {
        var d = FlowTests.CreateDriver();
        var setup = new GameSetup();
        int[] clubs = [2, 14, 26, 7];
        for (var i = 0; i < managers; i++)
            setup.Managers.Add(new ManagerSetup { Name = $"M{i}", Head = i, ClubIndex = clubs[i] });
        var game = NewGame.Create(d.Host.Assets.Database, setup, 5);
        d.Host.Game = game;
        d.Host.Show(new MainMenuScreen(d.Host));
        d.Frame();
        return (d, game);
    }

    [GameDataTheory]
    [InlineData(2)]
    [InlineData(4)]
    public void Continue_PassesTurnThenPlaysDayOnce(int managers)
    {
        var (d, game) = Start(managers);

        for (var turn = 0; turn < managers; turn++)
        {
            Assert.Equal(turn, game.CurrentManager);
            AssertStartLamps(d);
            Assert.Equal(0, game.PlayedMatchDays);
            if (turn == managers - 1)
                break;
            d.Press(Key.Space);
            Assert.IsType<MainMenuScreen>(d.Host.Current); // nur Zugwechsel, kein Spieltag
        }

        d.PlayMatchDay(); // letzter Manager bestätigt → Tag läuft genau einmal
        Assert.IsType<MainMenuScreen>(d.Host.Current);
        Assert.Equal(1, game.PlayedMatchDays);
        Assert.Equal(0, game.CurrentManager);
        AssertStartLamps(d);
    }

    /// <summary>Warnlampen des Managers am Zug: Aufstellung vollständig, nur Werbeflächen frei.</summary>
    private static void AssertStartLamps(ScreenDriver d)
    {
        for (var i = 0; i < Screens.MenuLamps.Count; i++)
            Assert.Equal(i == 4, PixelVereinTests.LampRed(d, i));
    }
}
