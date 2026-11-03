using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur ui-meldungen, Teil 4 (E4 = B): Das Hauptmenü meldet jeden Aufbau dem Spielkern (<see cref="GameState.OnMainMenu"/>) und
/// zeichnet nur (Hauptmenü em.exe Datei 0x44E22, Aufbau ab 0x44FBD).
/// </summary>
public class MainMenuEventUiTests
{
    private static (ScreenDriver Driver, GameState Game, MainMenuScreen Menu) Start()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var menu = Assert.IsType<MainMenuScreen>(d.Host.Current);
        return (d, d.Host.Game!, menu);
    }

    /// <summary>Öffnet eine Kachel und kehrt mit Escape bzw. Rechtsklick ins Hauptmenü zurück.</summary>
    private static void BackFrom(ScreenDriver d, string tile)
    {
        d.OpenTile(tile);
        Assert.IsNotType<MainMenuScreen>(d.Host.Current);
        for (var guard = 0; d.Host.Current is not MainMenuScreen; guard++)
        {
            Assert.True(guard < 10, "keine Rückkehr ins Hauptmenü");
            if (guard % 2 == 0)
                d.Press(Key.Escape);
            else
                d.RightClick(5, 5);
        }
    }

    /// <summary>
    /// [ORIGINAL] 0x4527C–0x45291: Nachricht ablegen → der erste Menüaufbau zeigt die Leiste und löscht das Kennzeichen, der zweite
    /// nicht – auch wenn die Nachricht noch ungelesen ist. Zeichnen ändert den Spielzustand nicht.
    /// </summary>
    [GameDataFact]
    public void NewMessage_FirstBuildShowsBar_SecondDoesNot_DrawingChangesNothing()
    {
        var (d, game, menu) = Start();
        Assert.False(menu.NewsBarVisible);
        game.Notify(game.ActiveManager, "Der Stadion-Ausbau (Test)", "ist abgeschlossen.");
        menu.Enter(); // Aufbau nach dem Ablegen (im Spiel: nach dem Tagesablauf)
        Assert.True(menu.NewsBarVisible);
        Assert.False(game.ActiveManager.HasNewMessage);

        var before = StateFingerprint.Of(game);
        d.Frame(); // erstes Zeichnen der Leiste (setzt seit Integration R12 auch „gelesen“ nicht mehr, NewsBar.Draw)
        for (var i = 0; i < 3; i++)
            d.Move(10 + i, 10);
        var after = StateFingerprint.Of(game);
        Assert.True(before == after, StateFingerprint.FirstDifference(before, after));

        // Rückkehr aus einem Unterbildschirm: zweiter Aufbau ohne Leiste.
        BackFrom(d, "Tabelle 1. Liga");
        Assert.Same(menu, d.Host.Current);
        Assert.False(menu.NewsBarVisible);
    }

    /// <summary>
    /// [ORIGINAL] Neuaufstellung 0x426BC bei jeder Rückkehr ins Menü (Datei 0x450C6). Regressionstest: Vorher stellte nur der
    /// Zuganfang neu auf; eine Lücke in der Aufstellung blieb bis zum nächsten Tag stehen.
    /// </summary>
    [GameDataFact]
    public void ReturnFromSubScreen_WithAutoLineup_LinesUpAgain()
    {
        var (d, game, menu) = Start();
        var club = game.Clubs[game.ActiveManager.ClubIndex];
        Assert.True(club.AutoLineupEnabled);
        var full = club.Lineup.Count;
        club.Lineup.Remove(club.Lineup.Players.First());

        BackFrom(d, "Tabelle 1. Liga");
        Assert.Same(menu, d.Host.Current);
        Assert.Equal(full, club.Lineup.Count);
    }

    /// <summary>
    /// Hot-Seat: Gibt der erste Manager ab, ist das stehende Menü der erste Aufbau des nächsten Zugs – dessen Kennzeichen zeigt
    /// die Leiste sofort (Datei 0x44E22 je Managerzug).
    /// </summary>
    [GameDataFact]
    public void PassTurn_NextManagersFlag_ShowsBarAtOnce()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "A", Head = 0, ClubIndex = 42 });
        setup.Managers.Add(new ManagerSetup { Name = "B", Head = 1, ClubIndex = 2 });
        var game = NewGame.Create(Formats.ClubDatabase.Load(TestGameData.Load()), setup, seed: 3);
        var d = FlowTests.CreateDriver();
        d.Host.Game = game;
        var menu = new MainMenuScreen(d.Host);
        d.Host.Show(menu);
        d.Frame();
        var second = game.Managers[1];
        game.Notify(second, "Testmeldung");
        Assert.False(menu.NewsBarVisible);

        d.Click(MainMenuScreen.ContinueLogo.X + 5, MainMenuScreen.ContinueLogo.Y + 5);
        Assert.Same(second, game.ActiveManager);
        Assert.Same(menu, d.Host.Current);
        Assert.True(menu.NewsBarVisible);
        Assert.False(second.HasNewMessage);
    }
}
