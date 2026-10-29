using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Competitions;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur bundestrainer-modus4: Menü des Bundestrainers im Modus 4 (Zug „Manager 5“ an WM-Tagen).
/// </summary>
public class NationalCoachMode4MenuTests
{
    /// <summary>Modus 4 auf synthetischer Datenbank, Manager 0 (Verein 8) und Manager 1 (Verein 2, stark).</summary>
    private static GameState Synthetic()
    {
        var data = new byte[Formats.ClubDatabase.FileLength];
        for (var i = 0; i < Formats.ClubDatabase.ClubsWithSquad; i++)
        {
            System.Text.Encoding.ASCII.GetBytes($"VEREIN {i}").CopyTo(data, Formats.ClubDatabase.SquadOffset + i * Formats.ClubDatabase.SquadRecordLength);
            data[Formats.ClubDatabase.StrengthOffset + i * 3] = (byte)(100 - i % 12 * 5);
        }
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "A", Head = 0, ClubIndex = 8 });
        setup.Managers.Add(new ManagerSetup { Name = "B", Head = 1, ClubIndex = 2 });
        return NewGame.Create(Formats.ClubDatabase.Parse(data), setup, 3);
    }

    /// <summary>Spielt bis zum Zug des Bundestrainers am ersten WM-Tag.</summary>
    private static void PlayToNationalTurn(GameState game)
    {
        for (var guard = 0; !game.NationalTurn; guard++)
        {
            Assert.True(guard < 3000, "Kein Zug des Bundestrainers");
            Assert.True(game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver, "Saisonende ohne Bundestrainer");
            foreach (var m in game.Managers)
                m.Money = Math.Max(m.Money, 10_000_000);
            game.Continue();
        }
    }

    /// <summary>
    /// [ORIGINAL] Datei 0x44FBD–0x4504A: Im Zug „Manager 5“ gilt die Sperrtabelle des Nationalteams (DGROUP 0x5159 + 42);
    /// Platz 6 der Reihe Manager („Highscore“) wird im Modus 4 ausdrücklich gesperrt (0x4502C, Wert = DGROUP 0x256 = 0
    /// oder Bundestrainer im Modus 4). Der Verein des ernannten Managers behält seine eigenen Sperren.
    /// </summary>
    [Fact]
    public void Mode4_NationalTurn_UsesCoachTable_HighscoreLocked()
    {
        var game = Synthetic();
        PlayToNationalTurn(game);
        Assert.Equal(GameMode.Full, game.Mode);
        Assert.Equal(1, game.CurrentManager);
        Assert.True(game.NationalCoachMenu);

        var highscore = MenuTiles.Small[0][5];
        Assert.Equal("Highscore", highscore.Name);
        Assert.True(NationalMenu.IsLocked(game, highscore));
        foreach (var tile in MenuTiles.Small.SelectMany(t => t).Where(t => !t.IsEmpty))
            Assert.Equal(!NationalMenu.CoachTiles.Contains(tile.Name), NationalMenu.IsLocked(game, tile));
        Assert.Equal(NationalTeams.Germany, NationalMenu.Team(game));
        Assert.Equal(game.NationalAccount, NationalMenu.Money(game));
        Assert.Equal(NationalCoach.FirstBudget, NationalMenu.Money(game));

        // Nach dem Zug des Bundestrainers: wieder das Vereinsmenü.
        game.Continue();
        Assert.False(game.NationalCoachMenu);
        Assert.Equal(game.ActiveManager.ClubIndex, NationalMenu.Team(game));
        Assert.Equal(game.ActiveManager.Money, NationalMenu.Money(game));
        Assert.Equal(TileLocks.IsLocked(game, highscore), NationalMenu.IsLocked(game, highscore));
    }

    /// <summary>Originaldaten: Hauptmenü im Zug des Bundestrainers öffnet WM-Kader und „WM Vorbereitung“, ohne den Zustand zu ändern.</summary>
    [GameDataFact]
    public void Mode4_NationalTurn_MainMenu_RoutesAndOpeningChangesNothing()
    {
        var database = Formats.ClubDatabase.Load(TestGameData.Load());
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "LetsShow", Head = 3, ClubIndex = StrongestBundesligaClub(database) });
        var game = NewGame.Create(database, setup, 1993);
        PlayToNationalTurn(game);

        var d = FlowTests.CreateDriver();
        d.Host.Game = game;
        d.Host.Show(new MainMenuScreen(d.Host));
        d.Frame();
        var menu = d.Host.Current;
        var before = StateFingerprint.Of(game);

        d.OpenTile("Transfer");
        Assert.IsType<NationalSquadScreen>(d.Host.Current);
        d.Frame();
        d.Press(Key.Escape);
        d.OpenTile("Training");
        Assert.IsType<FriendliesScreen>(d.Host.Current);
        d.Frame();
        d.Press(Key.Escape);
        Assert.IsType<MainMenuScreen>(d.Host.Current);
        foreach (var locked in new[] { "Finanzen", "Highscore", "Verträge", "Tabelle 1. Liga" })
        {
            var current = d.Host.Current;
            d.OpenTile(locked);
            Assert.Same(current, d.Host.Current);
        }
        Assert.Equal(before, StateFingerprint.Of(game));
        _ = menu;

        // „und tschüß…“ des Bundestrainers spielt den WM-Tag.
        var day = game.CurrentDay;
        d.PlayMatchDay();
        Assert.True(game.CurrentDay > day);
        Assert.False(game.NationalTurn);
        Assert.Contains(game.WorldCup!.Matches, m => m.Day == day);
    }

    private static int StrongestBundesligaClub(Formats.ClubDatabase database)
    {
        var game = NewGame.Create(database, new GameSetup { Managers = { new ManagerSetup { Name = "X", ClubIndex = 0 } } }, 1);
        return game.Clubs.Where(c => c.League == League.Bundesliga).MaxBy(c => c.Strength)!.Index;
    }
}
