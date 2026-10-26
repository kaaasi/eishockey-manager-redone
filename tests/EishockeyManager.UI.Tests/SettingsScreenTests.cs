using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Einstellungen: Modell (synthetisch) und Bildschirm (mit Originaldateien, ohne „gamedata“ übersprungen).
/// Alle Tests in einer Klasse, weil <see cref="GameSettings.Current"/> statisch ist.
/// </summary>
public class SettingsScreenTests : IDisposable
{
    private readonly GameSettings _previous = GameSettings.Current;
    private readonly string? _previousPath = GameSettings.StorePath;
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "em-einstellungen-" + Guid.NewGuid().ToString("N"));

    public SettingsScreenTests()
    {
        GameSettings.Current = new GameSettings();
        GameSettings.StorePath = null;
    }

    public void Dispose()
    {
        GameSettings.Current = _previous;
        GameSettings.StorePath = _previousPath;
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    [Fact]
    public void Defaults_MatchRequirements()
    {
        var s = new GameSettings();
        Assert.Equal(10, s.EffectsVolume);
        Assert.Equal(10, s.MusicVolume);
        Assert.True(s.MusicInMatch);   // „Musik auch im Spiel (Standard: an)“
        Assert.True(s.GoalScenes);
        Assert.False(s.Fullscreen);
    }

    [Theory]
    [InlineData(-3, 0)]
    [InlineData(4, 4)]
    [InlineData(11, 10)]
    public void Volumes_AreClamped(int value, int expected)
    {
        var s = new GameSettings { EffectsVolume = value, MusicVolume = value };
        Assert.Equal(expected, s.EffectsVolume);
        Assert.Equal(expected, s.MusicVolume);
    }

    [Fact]
    public void SaveAndLoad_RoundTrip()
    {
        var path = Path.Combine(_directory, "unter", "einstellungen.json");
        new GameSettings { EffectsVolume = 3, MusicVolume = 7, MusicInMatch = false, GoalScenes = false, Fullscreen = true }.Save(path);
        var loaded = GameSettings.Load(path);
        Assert.Equal((3, 7, false, false, true),
            (loaded.EffectsVolume, loaded.MusicVolume, loaded.MusicInMatch, loaded.GoalScenes, loaded.Fullscreen));
    }

    [Fact]
    public void Load_MissingOrBrokenFileGivesDefaults()
    {
        Assert.Equal(10, GameSettings.Load(Path.Combine(_directory, "fehlt.json")).MusicVolume);
        Directory.CreateDirectory(_directory);
        var broken = Path.Combine(_directory, "kaputt.json");
        File.WriteAllText(broken, "{ kein json");
        Assert.True(GameSettings.Load(broken).MusicInMatch);
    }

    [Fact]
    public void SaveCurrent_WithoutPathWritesNothing()
    {
        GameSettings.StorePath = null;
        GameSettings.SaveCurrent();
        Assert.False(Directory.Exists(_directory));

        GameSettings.StorePath = Path.Combine(_directory, "e.json");
        GameSettings.Current.MusicVolume = 2;
        GameSettings.SaveCurrent();
        Assert.Equal(2, GameSettings.Load(GameSettings.StorePath).MusicVolume);
    }

    private static ScreenDriver? Open()
    {
        var d = FlowTests.CreateDriver();
        if (d is null)
            return null;
        d.StartNewGame();
        d.OpenTile("Einstellungen");
        Assert.IsType<SettingsScreen>(d.Host.Current);
        return d;
    }

    [Fact]
    public void Tile_OpensSettingsAndEscapeReturns()
    {
        if (Open() is not { } d)
            return;
        var money = d.Host.Game!.ActiveManager.Money;
        d.Press(Key.Escape);
        Assert.IsType<MainMenuScreen>(d.Host.Current);
        Assert.Equal(money, d.Host.Game.ActiveManager.Money); // Öffnen ändert keinen Spielzustand

        d.OpenTile("Einstellungen");
        var back = SettingsScreen.BackButton;
        d.Click(back.X + 5, back.Y + 5);
        Assert.IsType<MainMenuScreen>(d.Host.Current);
    }

    [Fact]
    public void Arrows_ChangeVolume()
    {
        if (Open() is not { } d)
            return;
        var arrows = SettingsScreen.ArrowRect(SettingsScreen.Row.EffectsVolume);
        d.Click(arrows.X + 4, arrows.Bottom - 3);   // leiser
        d.Click(arrows.X + 4, arrows.Bottom - 3);
        Assert.Equal(8, GameSettings.Current.EffectsVolume);
        d.Click(arrows.X + 4, arrows.Y + 3);        // lauter
        Assert.Equal(9, GameSettings.Current.EffectsVolume);
        Assert.Equal(10, GameSettings.Current.MusicVolume);
    }

    [Fact]
    public void Cells_SetVolumeDirectly()
    {
        if (Open() is not { } d)
            return;
        var cell = SettingsScreen.CellRect(SettingsScreen.Row.MusicVolume, 4);
        d.Click(cell.X + 2, cell.Y + 2);
        Assert.Equal(4, GameSettings.Current.MusicVolume);
        var first = SettingsScreen.CellRect(SettingsScreen.Row.MusicVolume, 1);
        d.Click(first.X + 2, first.Y + 2);
        Assert.Equal(1, GameSettings.Current.MusicVolume);
        d.Click(first.X + 2, first.Y + 2);            // nochmal auf die einzige volle Zelle = aus
        Assert.Equal(0, GameSettings.Current.MusicVolume);
    }

    [Fact]
    public void Rows_ToggleSwitchesAndSave()
    {
        if (Open() is not { } d)
            return;
        GameSettings.StorePath = Path.Combine(_directory, "e.json");
        foreach (var row in new[] { SettingsScreen.Row.MusicInMatch, SettingsScreen.Row.GoalScenes, SettingsScreen.Row.Fullscreen })
        {
            var r = SettingsScreen.RowRect(row);
            d.Click(r.X + 10, r.Y + 5);
        }
        var s = GameSettings.Current;
        Assert.Equal((false, false, true), (s.MusicInMatch, s.GoalScenes, s.Fullscreen));
        var saved = GameSettings.Load(GameSettings.StorePath);
        Assert.Equal((false, false, true), (saved.MusicInMatch, saved.GoalScenes, saved.Fullscreen));
    }
}
