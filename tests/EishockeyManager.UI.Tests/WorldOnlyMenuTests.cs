using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Competitions;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur wettbewerbe-weltkugel: Spielmodus in der Spielvorbereitung und Hauptmenü des Bundestrainers.
/// </summary>
public class WorldOnlyMenuTests
{
    /// <summary>Spiel auf synthetischer Datenbank (ohne Originaldaten), Fahne gedrückt.</summary>
    private static GameState Synthetic(bool globe)
    {
        var data = new byte[Formats.ClubDatabase.FileLength];
        for (var i = 0; i < Formats.ClubDatabase.ClubsWithSquad; i++)
        {
            System.Text.Encoding.ASCII.GetBytes($"VEREIN {i}").CopyTo(data, Formats.ClubDatabase.SquadOffset + i * Formats.ClubDatabase.SquadRecordLength);
            data[Formats.ClubDatabase.StrengthOffset + i * 3] = (byte)(100 - i % 12 * 5);
        }
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "A", Head = 0, ClubIndex = 2 });
        if (!globe)
            setup.ToggleEuropeAndWorldCup();
        return NewGame.Create(Formats.ClubDatabase.Parse(data), setup, 3);
    }

    private static void ClickRect(ScreenDriver d, Rect r) => d.Click(r.X + 5, r.Y + 5);

    /// <summary>Spielvorbereitung bis kurz vor „Los jetzt!“ wie <see cref="ScreenDriver.StartNewGame"/>.</summary>
    private static (ScreenDriver Driver, SetupScreen Setup) OpenSetup()
    {
        var d = FlowTests.CreateDriver();
        d.Host.Show(new TitleScreen(d.Host));
        d.Frame();
        d.Click(160, 120);
        var setup = Assert.IsType<SetupScreen>(d.Host.Current);
        d.Click(20, 60);
        var cell = SetupScreen.HeadCell(3);
        d.Click(cell.X + 5, cell.Y + 5);
        d.Type("LetsShow");
        d.Press(Key.Enter);
        return (d, setup);
    }

    /// <summary>(1) Knöpfe → Bitwerte → Modus wie im Original (Datei 0x286F6 ff., 0x29838).</summary>
    [GameDataFact]
    public void Setup_Buttons_GiveOriginalModes()
    {
        var (d, screen) = OpenSetup();
        Assert.Equal(3, screen.Setup.ModeBits);
        ClickRect(d, SetupScreen.ModeFlag);
        Assert.Equal(2, screen.Setup.ModeBits);
        Assert.Equal(GameMode.WorldOnly, screen.Setup.Mode);
        ClickRect(d, SetupScreen.ModeGlobe);
        Assert.Equal(0, screen.Setup.ModeBits);
        // Ohne Modus nimmt „Los jetzt!“ keinen Klick an.
        ClickRect(d, SetupScreen.StartButton);
        Assert.Same(screen, d.Host.Current);
        ClickRect(d, SetupScreen.ModeFlag);
        Assert.Equal(GameMode.ClubOnly, screen.Setup.Mode);
        ClickRect(d, SetupScreen.ModeGlobe);
        Assert.Equal(GameMode.Full, screen.Setup.Mode);
    }

    /// <summary>Modus 1 starten: Sprung zur WM schon mit „Los jetzt!“ (#195), Hauptmenü des Bundestrainers, WM-Kader und „WM Vorbereitung“ erreichbar.</summary>
    [GameDataFact]
    public void WorldOnly_CoachMenu_LocksAndRoutes_AndOpeningChangesNothing()
    {
        var (d, setup) = OpenSetup();
        ClickRect(d, SetupScreen.ModeFlag);
        ClickRect(d, SetupScreen.StartButton);
        Assert.IsType<MainMenuScreen>(d.Host.Current);
        var game = d.Host.Game!;
        Assert.Equal(GameMode.WorldOnly, game.Mode);
        Assert.True(game.NationalCoachMenu);

        // #195 (DOSBox 06): „Los jetzt!“ springt sofort zum ersten WM-Termin und ernennt den Bundestrainer; ein Menü
        // vor der Ernennung gibt es im Modus 1 nicht mehr.
        Assert.Equal(GameState.FirstWorldCupDay, game.CurrentDay);
        Assert.NotNull(game.NationalCoach);
        Assert.Equal(0, game.PlayedMatchDays);

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
        foreach (var locked in new[] { "Finanzen", "Verträge", "Totale", "Tabelle 1. Liga", "DEB-Pokal", "Stärken 1. Liga" })
        {
            var current = d.Host.Current;
            d.OpenTile(locked);
            Assert.Same(current, d.Host.Current);
        }
        d.OpenTile("WM");
        Assert.IsType<CupPairingsScreen>(d.Host.Current); // #194: Paarungen → WM = Paarungsansicht
        Assert.Equal(before, StateFingerprint.Of(game));
        _ = setup;
    }

    /// <summary>Modus 2: WM-Kacheln gesperrt (Datei 0x44FE8 ff.), alle übrigen wie bisher.</summary>
    [Fact]
    public void ClubOnly_LocksWorldTiles()
    {
        var game = Synthetic(globe: false);
        foreach (var tile in MenuTiles.Small.SelectMany(t => t).Where(t => !t.IsEmpty))
            Assert.Equal(NationalMenu.WorldTiles.Contains(tile.Name), NationalMenu.IsLocked(game, tile));
        // [ORIGINAL] Datei 0x44FE8 ff.: Platz 6 der Reihen 2–4 und Platz 4 der Stärken.
        Assert.Equal(["WM-Statistik", "Tabelle WM", "WM", "Stärken Länderteams"],
            new[] { MenuTiles.Small[2][5], MenuTiles.Small[3][5], MenuTiles.Small[4][5], MenuTiles.Small[5][3] }.Select(t => t.Name));
        var full = Synthetic(globe: true);
        Assert.All(MenuTiles.Small.SelectMany(t => t), t => Assert.False(NationalMenu.IsLocked(full, t)));
    }

    /// <summary>[ORIGINAL] Sperrtabelle des Nationalteams (DGROUP 0x5159 + 42) auf die Kachelnamen des Remakes übertragen.</summary>
    [Fact]
    public void CoachTable_MatchesOriginalBytes()
    {
        byte[][] table =
        [
            [1, 1, 1, 1, 1, 1], [0, 0, 1, 0, 0, 0], [1, 1, 1, 1, 1, 0], [1, 1, 1, 1, 1, 0],
            [1, 1, 1, 1, 1, 0], [1, 1, 1, 0, 0, 0], [0, 0, 0, 0, 0, 0],
        ];
        for (var row = 0; row < 7; row++)
            for (var slot = 0; slot < MenuTiles.Small[row].Length; slot++)
            {
                var tile = MenuTiles.Small[row][slot];
                if (tile.IsEmpty)
                    continue;
                Assert.Equal(table[row][slot] == 0, NationalMenu.CoachTiles.Contains(tile.Name));
            }
    }
}
