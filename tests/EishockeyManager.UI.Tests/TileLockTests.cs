using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Competitions;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur tabellen-highscore: Kachelsperren des Hauptmenüs, die das Original aus der Spielvorbereitung ableitet
/// (em.exe Datei 0x45057–0x4509C, Sperrtabelle DGROUP 0x5159, Reihe „Manager“).
/// </summary>
public class TileLockTests
{
    /// <summary>Spiel auf synthetischer Datenbank (ohne Originaldaten) mit <paramref name="managers"/> Managern.</summary>
    private static GameState Synthetic(int managers, GameDuration duration)
    {
        var data = new byte[Formats.ClubDatabase.FileLength];
        for (var i = 0; i < Formats.ClubDatabase.ClubsWithSquad; i++)
        {
            System.Text.Encoding.ASCII.GetBytes($"VEREIN {i}").CopyTo(data, Formats.ClubDatabase.SquadOffset + i * Formats.ClubDatabase.SquadRecordLength);
            data[Formats.ClubDatabase.StrengthOffset + i * 3] = (byte)(100 - i % 12 * 5);
        }
        var setup = new GameSetup { Duration = duration };
        for (var m = 0; m < managers; m++)
            setup.Managers.Add(new ManagerSetup { Name = $"M{m}", Head = m, ClubIndex = 2 + m });
        return NewGame.Create(Formats.ClubDatabase.Parse(data), setup, 3);
    }

    private static MenuTile Tile(string name) => MenuTiles.Small.SelectMany(t => t).First(t => t.Name == name);

    /// <summary>
    /// [ORIGINAL] Datei 0x45079: „Werbung“ (Platz 2 der Reihe Manager, DGROUP 0x515A) ist gesperrt, solange das
    /// Spielende (DGROUP 0x5A14 = 1994 + 2 · Dauer, Datei 0x2987C) schon 1994 ist – also nur bei der Spieldauer „eine Saison“.
    /// </summary>
    [Theory]
    [InlineData(GameDuration.EineSaison, true)]
    [InlineData(GameDuration.DreiSaisons, false)]
    [InlineData(GameDuration.FuenfSaisons, false)]
    [InlineData(GameDuration.Unbegrenzt, false)]
    public void Advertising_IsLocked_OnlyInOneSeasonGame(GameDuration duration, bool locked)
    {
        var game = Synthetic(1, duration);
        Assert.Equal(locked, NationalMenu.IsLocked(game, Tile("Werbung")));
    }

    /// <summary>
    /// [ORIGINAL] Datei 0x45057: „Neuer“ (Platz 5 der Reihe Manager, DGROUP 0x515D) ist gesperrt, wenn vier Manager
    /// spielen (DGROUP 0x386 = Anzahl der Manager, Höchstzahl 4).
    /// </summary>
    [Theory]
    [InlineData(1, false)]
    [InlineData(3, false)]
    [InlineData(4, true)]
    public void New_IsLocked_OnlyWithFourManagers(int managers, bool locked)
    {
        var game = Synthetic(managers, GameDuration.Unbegrenzt);
        Assert.Equal(locked, NationalMenu.IsLocked(game, Tile("Neuer")));
    }

    /// <summary>Die übrigen Kacheln der Reihe Manager und die anderen Reihen bleiben frei (Sperrtabelle des Vereins: alle 0).</summary>
    [Fact]
    public void OtherTiles_StayFree_InOneSeasonGameWithFourManagers()
    {
        var game = Synthetic(4, GameDuration.EineSaison);
        var locked = MenuTiles.Small.SelectMany(t => t).Where(t => !t.IsEmpty && NationalMenu.IsLocked(game, t)).Select(t => t.Name).ToList();
        // Highscore: Sperre aus der Spielvorbereitung, siehe HighscoreLockTests (Standard-Regler → frei).
        Assert.Equal(["Werbung", "Neuer"], locked);
    }

    /// <summary>Gesperrte Kacheln nehmen im Hauptmenü keinen Klick an (Datei 0x45B63) und zeigen das Schlosssymbol.</summary>
    [GameDataFact]
    public void LockedTile_TakesNoClick_InMainMenu()
    {
        var d = FlowTests.CreateDriver();
        var data = TestGameData.Load();
        var setup = new GameSetup { Duration = GameDuration.EineSaison };
        setup.Managers.Add(new ManagerSetup { Name = "LetsShow", Head = 3, ClubIndex = 42 });
        d.Host.Game = NewGame.Create(Formats.ClubDatabase.Load(data), setup, 1993);
        d.Host.Show(new MainMenuScreen(d.Host));
        d.Frame();

        var menu = d.Host.Current;
        d.OpenTile("Werbung");
        Assert.Same(menu, d.Host.Current);
        d.OpenTile("Finanzen");
        Assert.NotSame(menu, d.Host.Current);
    }
}
