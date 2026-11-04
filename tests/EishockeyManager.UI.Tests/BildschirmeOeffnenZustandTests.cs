using EishockeyManager.Core;
using EishockeyManager.UI;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Team;
using EishockeyManager.UI.Testing;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// R12U-02 (#538, AGENTS.md §4): Bildschirme verändern den Spielzustand nicht durch bloßes Öffnen oder Zeichnen.
/// Verglichen wird der gespeicherte Spielstand vor und nach <c>Host.Show</c> plus <c>Draw</c>, mit zwei Managern und
/// <c>CurrentManager = 1</c>.
/// </summary>
public class BildschirmeOeffnenZustandTests
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

    private static void ShowAndDraw(ScreenDriver d, Screen screen)
    {
        d.Host.Show(screen);
        d.Host.Draw(d.Surface);
    }

    [GameDataFact]
    public void MenuTileScreens_OpeningAndDrawingDoNotChangeGameState()
    {
        var (d, game) = Start();
        game.CurrentManager = 1;
        var menu = (MainMenuScreen)d.Host.Current!;
        var changed = new List<string>();
        foreach (var (tile, factory) in MenuEntries.All.OrderBy(e => e.Key))
        {
            var before = SaveGame.Save(game);
            ShowAndDraw(d, factory(d.Host, menu));
            if (!before.AsSpan().SequenceEqual(SaveGame.Save(game)))
                changed.Add(tile);
            d.Host.Show(menu);
            game.CurrentManager = 1;
        }
        Assert.Empty(changed);
    }

    [GameDataFact]
    public void SeasonEndScreens_OpeningAndDrawingDoNotChangeGameState()
    {
        var (d, game) = Start();
        var guard = 0;
        while (d.Host.Current is not SeasonEndScreen && guard++ < 20000)
        {
            KeepSquadHelper.KeepSquads(game);
            d.Advance();
        }
        var seasonEnd = Assert.IsType<SeasonEndScreen>(d.Host.Current);
        game.CurrentManager = 1;
        var menu = new MainMenuScreen(d.Host);

        // Fenster „Saisonende“ je Manager: Öffnen und Zeichnen ändern nichts.
        var before = SaveGame.Save(game);
        ShowAndDraw(d, new SeasonEndSummaryScreen(d.Host, menu));
        Assert.True(before.AsSpan().SequenceEqual(SaveGame.Save(game)), "SeasonEndSummaryScreen ändert beim Öffnen den Spielzustand");

        // Verhandlungen am Saisonende (Aufruf wie in der Eingabe des Fensters davor).
        game.CurrentManager = 1;
        game.StartNextSeasonWithRenewals();
        var screen = ContractsScreen.ForSeasonEnd(d.Host, () => menu);
        var contracts = Assert.IsType<ContractsScreen>(screen);
        before = SaveGame.Save(game);
        ShowAndDraw(d, contracts);
        Assert.True(before.AsSpan().SequenceEqual(SaveGame.Save(game)), "ContractsScreen (Saisonende) ändert beim Öffnen den Spielzustand");
        Assert.NotNull(seasonEnd);
    }
}
