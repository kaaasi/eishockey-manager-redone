using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.UI;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Testing;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur zufallsstrom, Teil 3 (N3 = B, N6 = A; AGENTS.md §4): Seit die Würfe des Spielkerns aus dem gemeinsamen Strom
/// <see cref="GameState.Random"/> kommen, würde jeder Wurf beim bloßen Öffnen eines Bildschirms den Spielverlauf verschieben.
/// Die Probe öffnet und zeichnet jeden Bildschirm einer Menükachel für beide Manager, am Spielstart und nach einigen Spieltagen,
/// und baut das Hauptmenü mehrfach auf: Der Strom steht danach unverändert. Der Zufall der Darstellung (A061, A271, A274, A438,
/// A452) zieht nicht aus dem Strom.
/// </summary>
public class ZufallsstromBildschirmeTests
{
    private static (ScreenDriver Driver, GameState Game) Start()
    {
        var d = FlowTests.CreateDriver();
        var setup = new GameSetup();
        int[] clubs = [2, 14];
        for (var i = 0; i < clubs.Length; i++)
            setup.Managers.Add(new ManagerSetup { Name = $"M{i}", Head = i, ClubIndex = clubs[i] });
        var game = NewGame.Create(d.Host.Assets.Database, setup, 5);
        d.Host.Game = game;
        d.Host.Show(new MainMenuScreen(d.Host));
        d.Frame();
        return (d, game);
    }

    private static List<string> OpenAll(ScreenDriver d, GameState game)
    {
        var drawn = new List<string>();
        for (var manager = 0; manager < game.Managers.Count; manager++)
        {
            game.CurrentManager = manager;
            var menu = new MainMenuScreen(d.Host);
            d.Host.Show(menu);
            d.Host.Draw(d.Surface);
            foreach (var (tile, factory) in MenuEntries.All.OrderBy(e => e.Key))
            {
                var before = game.Random.Capture();
                d.Host.Show(factory(d.Host, menu));
                d.Host.Draw(d.Surface);
                d.Frames(2);
                if (before != game.Random.Capture())
                    drawn.Add($"{tile} (Manager {manager})");
                d.Host.Show(menu);
                game.CurrentManager = manager;
            }
        }
        return drawn;
    }

    [GameDataFact]
    public void OpeningAllMenuTileScreens_DrawsNothingFromSharedStream()
    {
        var (d, game) = Start();
        var start = game.Random.Capture();
        Assert.Empty(OpenAll(d, game));
        Assert.Equal(start, game.Random.Capture()); // auch der wiederholte Aufbau des Hauptmenüs (OnMainMenu, Datei 0x44FBD) zieht nichts

        game.CurrentManager = 0;
        d.Host.Show(new MainMenuScreen(d.Host));
        for (var i = 0; i < 6; i++)
        {
            KeepSquadHelper.KeepSquads(game);
            d.Advance();
        }
        var later = game.Random.Capture();
        Assert.NotEqual(start, later); // die Zeitleiste zieht – nur sie
        Assert.Empty(OpenAll(d, game));
        Assert.Equal(later, game.Random.Capture());
    }
}
