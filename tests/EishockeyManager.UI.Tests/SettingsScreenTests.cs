using EishockeyManager.Tests.Support;
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
        Assert.True(s.GoalScenes);
        Assert.False(s.Fullscreen);
    }

    [Theory]
    [InlineData(-3, 0)]
    [InlineData(4, 4)]
    [InlineData(11, 10)]
    public void Volume_IsClamped(int value, int expected) =>
        Assert.Equal(expected, new GameSettings { EffectsVolume = value }.EffectsVolume);

    [Fact]
    public void SaveAndLoad_RoundTrip()
    {
        var path = Path.Combine(_directory, "unter", "einstellungen.json");
        new GameSettings { EffectsVolume = 3, GoalScenes = false, Fullscreen = true }.Save(path);
        var loaded = GameSettings.Load(path);
        Assert.Equal((3, false, true), (loaded.EffectsVolume, loaded.GoalScenes, loaded.Fullscreen));
    }

    /// <summary>#221: Dateien aus früheren Ständen enthalten noch MusicVolume/MusicInMatch; sie müssen weiter laden.</summary>
    [Fact]
    public void Load_OldFileWithMusicFields_StillLoads()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "alt.json");
        File.WriteAllText(path,
            "{ \"EffectsVolume\": 4, \"MusicVolume\": 2, \"MusicInMatch\": false, \"GoalScenes\": false, \"Fullscreen\": true, \"AutoSave\": false }");
        var loaded = GameSettings.Load(path);
        Assert.Equal((4, false, true, false), (loaded.EffectsVolume, loaded.GoalScenes, loaded.Fullscreen, loaded.AutoSave));
    }

    /// <summary>#221: Es gibt keine Musikeinstellungen mehr (Musik läuft nur im Titelbildschirm).</summary>
    [Fact]
    public void NoMusicSettings()
    {
        Assert.Null(typeof(GameSettings).GetProperty("MusicVolume"));
        Assert.Null(typeof(GameSettings).GetProperty("MusicInMatch"));
        Assert.DoesNotContain(Enum.GetNames<SettingsScreen.Row>(), n => n.StartsWith("Music"));
    }

    [Fact]
    public void Load_MissingOrBrokenFileGivesDefaults()
    {
        Assert.Equal(10, GameSettings.Load(Path.Combine(_directory, "fehlt.json")).EffectsVolume);
        Directory.CreateDirectory(_directory);
        var broken = Path.Combine(_directory, "kaputt.json");
        File.WriteAllText(broken, "{ kein json");
        Assert.True(GameSettings.Load(broken).GoalScenes);
    }

    [Fact]
    public void SaveCurrent_WithoutPathWritesNothing()
    {
        GameSettings.StorePath = null;
        GameSettings.SaveCurrent();
        Assert.False(Directory.Exists(_directory));

        GameSettings.StorePath = Path.Combine(_directory, "e.json");
        GameSettings.Current.EffectsVolume = 2;
        GameSettings.SaveCurrent();
        Assert.Equal(2, GameSettings.Load(GameSettings.StorePath).EffectsVolume);
    }

    private static ScreenDriver Open()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        d.OpenTile("Einstellungen");
        Assert.IsType<SettingsScreen>(d.Host.Current);
        return d;
    }

    [GameDataFact]
    public void Tile_OpensSettingsAndEscapeReturns()
    {
        var d = Open();
        var money = d.Host.Game!.ActiveManager.Money;
        d.Press(Key.Escape);
        Assert.IsType<MainMenuScreen>(d.Host.Current);
        Assert.Equal(money, d.Host.Game.ActiveManager.Money); // Öffnen ändert keinen Spielzustand

        d.OpenTile("Einstellungen");
        var back = SettingsScreen.BackButton;
        d.Click(back.X + 5, back.Y + 5);
        Assert.IsType<MainMenuScreen>(d.Host.Current);
    }

    [GameDataFact]
    public void Arrows_ChangeVolume()
    {
        var d = Open();
        var arrows = SettingsScreen.ArrowRect(SettingsScreen.Row.EffectsVolume);
        d.Click(arrows.X + 4, arrows.Bottom - 3);   // leiser
        d.Click(arrows.X + 4, arrows.Bottom - 3);
        Assert.Equal(8, GameSettings.Current.EffectsVolume);
        d.Click(arrows.X + 4, arrows.Y + 3);        // lauter
        Assert.Equal(9, GameSettings.Current.EffectsVolume);
    }

    [GameDataFact]
    public void Cells_SetVolumeDirectly()
    {
        var d = Open();
        var cell = SettingsScreen.CellRect(SettingsScreen.Row.EffectsVolume, 4);
        d.Click(cell.X + 2, cell.Y + 2);
        Assert.Equal(4, GameSettings.Current.EffectsVolume);
        var first = SettingsScreen.CellRect(SettingsScreen.Row.EffectsVolume, 1);
        d.Click(first.X + 2, first.Y + 2);
        Assert.Equal(1, GameSettings.Current.EffectsVolume);
        d.Click(first.X + 2, first.Y + 2);            // nochmal auf die einzige volle Zelle = aus
        Assert.Equal(0, GameSettings.Current.EffectsVolume);
    }

    [GameDataFact]
    public void Rows_ToggleSwitchesAndSave()
    {
        var d = Open();
        GameSettings.StorePath = Path.Combine(_directory, "e.json");
        foreach (var row in new[] { SettingsScreen.Row.GoalScenes, SettingsScreen.Row.Fullscreen })
        {
            var r = SettingsScreen.RowRect(row);
            d.Click(r.X + 10, r.Y + 5);
        }
        var s = GameSettings.Current;
        Assert.Equal((false, true), (s.GoalScenes, s.Fullscreen));
        var saved = GameSettings.Load(GameSettings.StorePath);
        Assert.Equal((false, true), (saved.GoalScenes, saved.Fullscreen));
    }
}
