using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.SaveLoad;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Laden/Sichern-Dialog und automatisches Speichern (Spur speichern-dialog). Dateinamen und Ordner sind synthetisch,
/// die Spielstände entstehen aus dem Spiel mit Originaldaten.
/// </summary>
public class SaveLoadScreenTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "em-dialogtest-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    private ScreenDriver NewDriver()
    {
        var d = FlowTests.CreateDriver();
        var context = SaveContext.For(d.Host);
        context.Directory = _directory;
        context.AutoSaveOverride = false; // Dialogtests sollen keine Nebenwirkung des Hauptmenüs sehen
        d.StartNewGame();
        d.PlayMatchDay();
        return d;
    }

    private static string Fingerprint(ScreenDriver d) => StateFingerprint.Of(d.Host.Game!);

    /// <summary>
    /// Abdruck eines Stands im Format des Spielstands (wie <c>SaveGameTests</c>): vergleichbar auch zwischen einem
    /// geladenen und dem laufenden Spiel, <see cref="StateFingerprint"/> sieht dort Listen und Felder unterschiedlich.
    /// </summary>
    private static string Saved(GameState game) => Convert.ToBase64String(SaveGame.Save(game));

    // ---- Dateiregeln (ohne Originaldaten) ----------------------------------------------------------

    [Theory]
    [InlineData("test1", "TEST1")]
    [InlineData(" Test1.ems ", "TEST1")]
    [InlineData("a-b_c", "A-B_C")]
    [InlineData("neunzeichen", null)]
    [InlineData("", null)]
    [InlineData("a b", null)]
    [InlineData("a/b", null)]
    [InlineData("..", null)]
    [InlineData("con", null)]
    public void NormalizeName_FollowsDosRules(string typed, string? expected) =>
        Assert.Equal(expected, SaveFiles.NormalizeName(typed));

    [Fact]
    public void WriteAtomic_ReplacesFile_AndLeavesNoTempFile()
    {
        var path = Path.Combine(_directory, "X.ems");
        SaveFiles.WriteAtomic(path, [1, 2, 3]);
        SaveFiles.WriteAtomic(path, [4, 5]);
        Assert.Equal([4, 5], File.ReadAllBytes(path));
        Assert.Equal([path], Directory.GetFiles(_directory));
    }

    [Fact]
    public void List_ShowsDirectoriesFirst_AndMarksOnlySavesAsSelectable()
    {
        Directory.CreateDirectory(Path.Combine(_directory, "ZED"));
        File.WriteAllBytes(Path.Combine(_directory, "B.ems"), []);
        File.WriteAllBytes(Path.Combine(_directory, "A.txt"), []);
        File.WriteAllBytes(Path.Combine(_directory, "C.ems.tmp"), []);
        var list = SaveFiles.List(_directory);
        Assert.Equal(["ZED", "A.txt", "B.ems"], list.Select(e => e.FileName));
        Assert.Equal([SaveEntryKind.Directory, SaveEntryKind.Other, SaveEntryKind.Save], list.Select(e => e.Kind));
    }

    [Fact]
    public void AutoSave_IsOnByDefault() => Assert.True(new GameSettings().AutoSave);

    // ---- Dialog ------------------------------------------------------------------------------------

    [GameDataFact]
    public void OpenAndCancel_ChangesNoState()
    {
        var d = NewDriver();
        var game = d.Host.Game;
        var before = Fingerprint(d);
        foreach (var tile in new[] { "Laden", "Sichern" })
        {
            d.OpenTile(tile);
            Assert.IsType<SaveLoadScreen>(d.Host.Current);
            d.Frames(3);
            d.Press(Key.Escape);
            Assert.IsType<MainMenuScreen>(d.Host.Current);
        }
        Assert.Same(game, d.Host.Game);
        Assert.Equal(before, Fingerprint(d));
        Assert.False(Directory.Exists(_directory) && Directory.EnumerateFileSystemEntries(_directory).Any());
    }

    [GameDataFact]
    public void SaveThenLoad_ThroughDialog_RestoresSameState()
    {
        var d = NewDriver();
        var saved = Saved(d.Host.Game!);
        d.OpenTile("Sichern");
        d.Type("probe1");
        d.Press(Key.Enter);
        Assert.IsType<MainMenuScreen>(d.Host.Current);
        Assert.True(File.Exists(Path.Combine(_directory, "PROBE1.ems")));

        d.PlayMatchDay();
        Assert.NotEqual(saved, Saved(d.Host.Game!));

        d.OpenTile("Laden");
        d.Type("probe1");
        d.Press(Key.Enter);
        Assert.IsType<MainMenuScreen>(d.Host.Current);
        Assert.Equal(saved, Saved(d.Host.Game!));
    }

    [GameDataFact]
    public void LoadByClick_SelectsFirst_ThenLoads()
    {
        var d = NewDriver();
        d.OpenTile("Sichern");
        d.Type("klick");
        d.Press(Key.Enter);
        var saved = Saved(d.Host.Game!);
        d.PlayMatchDay();

        d.OpenTile("Laden");
        var index = SaveFiles.List(_directory).ToList().FindIndex(e => e.FileName == "KLICK.ems");
        var rect = SaveLoadScreen.EntryRect(index / SaveLoadScreen.RowsPerColumn, index % SaveLoadScreen.RowsPerColumn);
        d.Click(rect.X + 3, rect.Y + 3);
        var screen = Assert.IsType<SaveLoadScreen>(d.Host.Current);
        Assert.Equal("KLICK", screen.FileName);
        d.Click(rect.X + 3, rect.Y + 3);
        Assert.IsType<MainMenuScreen>(d.Host.Current);
        Assert.Equal(saved, Saved(d.Host.Game!));
    }

    // em.exe 0x5DBE5 (Datei 0x5E5ED..0x5E667): Rechtsklick auf einen Eintrag bricht nicht ab, er übernimmt den Namen nicht (Namensfeld und Auswahl bleiben).
    [GameDataFact]
    public void RightClickOnEntry_KeepsNameAndDoesNotLeaveOrLoad()
    {
        var d = NewDriver();
        d.OpenTile("Sichern");
        d.Type("rechts");
        d.Press(Key.Enter);
        d.OpenTile("Laden");
        var before = Assert.IsType<SaveLoadScreen>(d.Host.Current).FileName;
        var index = SaveFiles.List(_directory).ToList().FindIndex(e => e.FileName == "RECHTS.ems");
        var rect = SaveLoadScreen.EntryRect(index / SaveLoadScreen.RowsPerColumn, index % SaveLoadScreen.RowsPerColumn);
        d.RightClick(rect.X + 3, rect.Y + 3);
        // em.exe Datei 0x5E5ED–0x5E667: Taste 3 auf einem Eintrag zeigt nur die Spielstand-Info (0x5B661),
        // das Namensfeld [bp+6] bleibt unverändert, der Dialog bleibt offen (R9I-F1, #326).
        var screen = Assert.IsType<SaveLoadScreen>(d.Host.Current);
        Assert.Equal(before, screen.FileName);
        Assert.NotEqual("RECHTS", screen.FileName);
    }

    // em.exe 0x5DBE5 (Datei 0x5E66A..0x5E68C): Linksklick auf einen Eintrag führt aus, wenn der Name im Feld schon gleich ist.
    [GameDataFact]
    public void LeftClickOnEntry_WithSameNameTyped_LoadsAtOnce()
    {
        var d = NewDriver();
        d.OpenTile("Sichern");
        d.Type("gleich");
        d.Press(Key.Enter);
        d.PlayMatchDay();
        d.OpenTile("Laden");
        d.Type("gleich");
        var index = SaveFiles.List(_directory).ToList().FindIndex(e => e.FileName == "GLEICH.ems");
        var rect = SaveLoadScreen.EntryRect(index / SaveLoadScreen.RowsPerColumn, index % SaveLoadScreen.RowsPerColumn);
        d.Click(rect.X + 3, rect.Y + 3);
        Assert.IsType<MainMenuScreen>(d.Host.Current);
    }

    [GameDataFact]
    public void ForeignFile_AndNewerVersion_ShowMessage_AndKeepRunningState()
    {
        var d = NewDriver();
        Directory.CreateDirectory(_directory);
        File.WriteAllText(Path.Combine(_directory, "FREMD.ems"), "das ist kein Spielstand");
        File.WriteAllText(Path.Combine(_directory, "NEUER.ems"),
            $"{{\"kennung\":\"{SaveGame.Identifier}\",\"schema\":{SaveGame.SchemaVersion + 1}}}");
        var game = d.Host.Game;
        var before = Fingerprint(d);

        foreach (var name in new[] { "fremd", "neuer" })
        {
            d.OpenTile("Laden");
            d.Type(name);
            d.Press(Key.Enter);
            var screen = Assert.IsType<SaveLoadScreen>(d.Host.Current);
            Assert.NotEmpty(screen.MessageLines);
            Assert.Same(game, d.Host.Game);
            Assert.Equal(before, Fingerprint(d));
            d.Press(Key.Enter); // Enter schließt die Meldung nicht (nur Linksklick, Datei 0x4C246, E3 Runde 12)
            Assert.NotEmpty(screen.MessageLines);
            if (name == "neuer") // fremde Version: Kurzmeldung 0x5C559, schließt von selbst (Integration R12, UM-4)
                d.Frames(ShortMessage.FramesFor(ShortMessage.Duration(ShortMessageTexts.WrongVersion.Line1)));
            else
                for (var page = 0; page < 3 && screen.MessageLines.Count > 0; page++)
                    d.CloseMessage(); // mehr als vier Zeilen: Seiten zu je vier Zeilen (UM-3, Runde 12)
            Assert.Empty(screen.MessageLines);
            d.Press(Key.Escape);
            Assert.IsType<MainMenuScreen>(d.Host.Current);
        }
    }

    [GameDataFact]
    public void Save_ToExistingName_AsksFirst_AndNoKeepsFile()
    {
        var d = NewDriver();
        d.OpenTile("Sichern");
        d.Type("alt");
        d.Press(Key.Enter);
        var path = Path.Combine(_directory, "ALT.ems");
        var old = File.ReadAllBytes(path);
        d.PlayMatchDay();

        d.OpenTile("Sichern");
        d.Type("alt");
        d.Press(Key.Enter);
        var screen = Assert.IsType<SaveLoadScreen>(d.Host.Current);
        Assert.True(screen.IsConfirming);
        d.Press(Key.Escape);
        Assert.False(screen.IsConfirming);
        Assert.Equal(old, File.ReadAllBytes(path));

        d.Press(Key.Enter);
        d.Press(Key.Enter); // Ja
        Assert.IsType<MainMenuScreen>(d.Host.Current);
        Assert.NotEqual(old, File.ReadAllBytes(path));
    }

    [GameDataFact]
    public void Delete_AsksFirst_AndRemovesFile()
    {
        var d = NewDriver();
        d.OpenTile("Sichern");
        d.Type("weg");
        d.Press(Key.Enter);
        d.OpenTile("Laden");
        d.Type("weg");
        d.Click(SaveLoadScreen.DeleteButton.X + 5, SaveLoadScreen.DeleteButton.Y + 5);
        var screen = Assert.IsType<SaveLoadScreen>(d.Host.Current);
        Assert.True(screen.IsConfirming);
        d.Press(Key.Enter);
        Assert.False(File.Exists(Path.Combine(_directory, "WEG.ems")));
    }

    // ---- Automatisch speichern ---------------------------------------------------------------------

    private static string AutoFile(string directory) => Path.Combine(directory, "AUTOSAVE.ems");

    /// <summary>Geht Zug um Zug wie das Hauptmenü: ein Tag spielen (Continue), dann „Hauptmenü sichtbar“.</summary>
    private static List<int> Walk(ScreenDriver d, string directory, int turns, Action<GameState>? each = null)
    {
        var host = d.Host;
        var context = SaveContext.For(host);
        var writes = new List<int>();
        var game = host.Game!;
        for (var turn = 1; turn <= turns; turn++)
        {
            if (game.Phase == SeasonPhase.SeasonOver)
                game.StartNextSeason();
            var before = context.AutoSaveWrites;
            var state = Saved(game);
            if (AutoSave.OnMainMenu(host))
            {
                writes.Add(turn);
                // Zustand unverändert, Datei enthält genau diesen Stand
                Assert.Equal(state, Saved(game));
                var loaded = SaveGame.Load(File.ReadAllBytes(AutoFile(directory)));
                Assert.Equal(state, Saved(loaded.Game!));
                Assert.Equal(before + 1, context.AutoSaveWrites);
                Assert.Equal([AutoFile(directory)], Directory.GetFiles(directory));
            }
            each?.Invoke(game);
            var writesBefore = context.AutoSaveWrites;
            game.Continue();
            Assert.Equal(writesBefore, context.AutoSaveWrites); // nie während Continue()
        }
        return writes;
    }

    [GameDataFact]
    public void AutoSave_WritesExactlyOneFile_OnEveryTenthMainMenu()
    {
        var d = FlowTests.CreateDriver();
        var context = SaveContext.For(d.Host);
        context.Directory = _directory;
        context.AutoSaveOverride = true;
        d.StartNewGame(); // das erste Hauptmenü ist schon gezählt
        var writes = Walk(d, _directory, 45);
        Assert.Equal(4, writes.Count);
        Assert.All(writes.Zip(writes.Skip(1)), p => Assert.Equal(10, p.Second - p.First));
    }

    [GameDataFact]
    public void AutoSave_Off_WritesNothing()
    {
        var d = FlowTests.CreateDriver();
        var context = SaveContext.For(d.Host);
        context.Directory = _directory;
        context.AutoSaveOverride = false;
        d.StartNewGame();
        Assert.Empty(Walk(d, _directory, 25));
        Assert.False(File.Exists(AutoFile(_directory)));
    }

    [GameDataFact]
    public void AutoSave_ReEnteringSameTurn_DoesNotCount()
    {
        var d = FlowTests.CreateDriver();
        var context = SaveContext.For(d.Host);
        context.Directory = _directory;
        context.AutoSaveOverride = true;
        d.StartNewGame();
        d.PlayMatchDay();
        for (var i = 0; i < 30; i++)
        {
            d.OpenTile("Laden");
            d.Press(Key.Escape);
        }
        Assert.Equal(0, context.AutoSaveWrites);
    }

    [GameDataFact]
    public void AutoSave_ThroughScreens_WritesLoadableFile()
    {
        var d = FlowTests.CreateDriver();
        var context = SaveContext.For(d.Host);
        context.Directory = _directory;
        context.AutoSaveOverride = true;
        d.StartNewGame();
        for (var i = 0; i < 25; i++)
            d.PlayMatchDay();
        Assert.True(context.AutoSaveWrites >= 2);
        Assert.Null(context.LastAutoSaveError);
        Assert.NotNull(SaveGame.Load(File.ReadAllBytes(AutoFile(_directory))).Game);
        Assert.Equal([AutoFile(_directory)], Directory.GetFiles(_directory));
    }

    /// <summary>Mehrsaisonprobe: fünf Saisonwechsel, der Spielstand vom Haltepunkt läuft nach dem Laden gleich weiter.</summary>
    [GameDataFact]
    public void AutoSave_OverFiveSeasons_LoadedStateContinuesIdentically()
    {
        var d = FlowTests.CreateDriver();
        var context = SaveContext.For(d.Host);
        context.Directory = _directory;
        context.AutoSaveOverride = true;
        d.StartNewGame();
        var game = d.Host.Game!;
        var startSeason = game.Season;
        var writes = 0;
        var checkedTwin = false;
        for (var turn = 0; game.Season < startSeason + 5 && turn < 3000; turn++)
        {
            if (game.Phase == SeasonPhase.SeasonOver)
                game.StartNextSeason();
            KeepSquadHelper.KeepSquads(game); // Spur heimspiel-statistik (#136): sonst ab Saison 3 entlassen
            if (AutoSave.OnMainMenu(d.Host))
            {
                writes++;
                if (!checkedTwin && writes == 20)
                {
                    checkedTwin = true;
                    var twin = SaveGame.Load(File.ReadAllBytes(AutoFile(_directory))).Game!;
                    for (var i = 0; i < 12 && twin.Phase != SeasonPhase.SeasonOver; i++)
                    {
                        game.Continue();
                        twin.Continue();
                    }
                    Assert.Equal(Saved(game), Saved(twin));
                    continue;
                }
            }
            game.Continue();
        }
        Assert.True(game.Season >= startSeason + 5);
        Assert.True(writes >= 20, $"Nur {writes} automatische Spielstände.");
    }
}
