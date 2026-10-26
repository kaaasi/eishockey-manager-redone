using EishockeyManager.Tests.Support;
using EishockeyManager.Core;
using EishockeyManager.UI;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Befund R2-06 über die Oberfläche (Originaldateien, übersprungen ohne „gamedata“): „und tschüß…“ gibt den Zug
/// weiter, ohne den Tag zu spielen; die Lampen der Infoleiste zeigen rot = noch nicht bereit, grün = bereit.
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

    /// <summary>Lampe i der Infoleiste rot?</summary>
    private static bool LampRed(ScreenDriver d, int i)
    {
        var red = new Surface();
        Widgets.Lamp(red, d.Host.Assets, 226, 31 + i * 7, red: true);
        var green = new Surface();
        Widgets.Lamp(green, d.Host.Assets, 226, 31 + i * 7, red: false);
        int redHits = 0, greenHits = 0;
        for (var y = 31 + i * 7; y < 37 + i * 7; y++)
        for (var x = 226; x < 232; x++)
        {
            var p = y * Surface.Width + x;
            if (red.Pixels[p] == green.Pixels[p])
                continue;
            if (d.Surface.Pixels[p] == red.Pixels[p]) redHits++;
            if (d.Surface.Pixels[p] == green.Pixels[p]) greenHits++;
        }
        Assert.True(redHits != greenHits, $"Lampe {i} weder eindeutig rot noch grün");
        return redHits > greenHits;
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
            for (var i = 0; i < managers; i++)
                Assert.Equal(i >= turn, LampRed(d, i)); // bereits bestätigt = grün
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
        for (var i = 0; i < managers; i++)
            Assert.True(LampRed(d, i));
    }
}
