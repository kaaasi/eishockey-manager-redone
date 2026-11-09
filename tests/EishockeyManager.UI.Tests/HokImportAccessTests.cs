using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.SaveLoad;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur hok-import, Teil 3 (Zugang): Der Laden-Dialog listet Original-Spielstände <c>*.HOK</c> und importiert sie beim Laden
/// (<see cref="OriginalImport.TryImport"/>); eine falsche Prüfsumme zeigt „Der kleine Mogelguru“ und lädt nichts.
/// Die Spielstände kommen aus dem Referenz-Repo (<c>original/spielstaende</c>, nie im Code-Repo).
/// </summary>
public sealed class HokImportAccessTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "em-hoktest-" + Guid.NewGuid().ToString("N"));

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
        context.AutoSaveOverride = false;
        d.StartNewGame();
        return d;
    }

    private void Copy(string name, string? target = null)
    {
        Directory.CreateDirectory(_directory);
        File.Copy(OriginalSave(name), Path.Combine(_directory, target ?? name));
    }

    private static string Saved(GameState game) => Convert.ToBase64String(SaveGame.Save(game));

    private static Rect EntryRectOf(string directory, string fileName)
    {
        var index = SaveFiles.List(directory).ToList().FindIndex(e => e.FileName == fileName);
        Assert.True(index >= 0, $"{fileName} fehlt in der Liste.");
        return SaveLoadScreen.EntryRect(index / SaveLoadScreen.RowsPerColumn, index % SaveLoadScreen.RowsPerColumn);
    }

    // ---- Dateiliste (ohne Originaldaten) ------------------------------------------------------------

    [Fact]
    public void List_MarksHokFilesAsOriginal()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllBytes(Path.Combine(_directory, "B.ems"), []);
        File.WriteAllBytes(Path.Combine(_directory, "FILE05.HOK"), []);
        File.WriteAllBytes(Path.Combine(_directory, "file16a.hok"), []);
        File.WriteAllBytes(Path.Combine(_directory, "ZUVIELEZEICHEN.HOK"), []);
        var list = SaveFiles.List(_directory);
        Assert.Equal(["B.ems", "FILE05.HOK", "file16a.hok", "ZUVIELEZEICHEN.HOK"], list.Select(e => e.FileName));
        Assert.Equal([SaveEntryKind.Save, SaveEntryKind.Original, SaveEntryKind.Original, SaveEntryKind.Other], list.Select(e => e.Kind));
        Assert.Equal("FILE16A", list[2].BaseName);
    }

    [Fact]
    public void TryImport_RejectsNonOriginalData_WithoutChecksumFlag()
    {
        var result = OriginalImport.TryImport(new byte[100], ClubDatabaseStub, 1);
        Assert.False(result.Success);
        Assert.False(result.ChecksumError);
        Assert.False(string.IsNullOrEmpty(result.Error));
    }

    // Die Datenbank wird bei einer zu kurzen Datei nicht gebraucht.
    private static ClubDatabase ClubDatabaseStub => null!;

    // ---- Laden-Dialog mit Originaldaten -------------------------------------------------------------

    [GameDataFact]
    public void LoadDialog_ImportsHok_ByTypedName()
    {
        Copy("FILE05.HOK");
        var d = NewDriver();
        var expected = OriginalImport.Load(OriginalSave("FILE05.HOK"), d.Host.Assets.Database, d.Host.Seed!.Value);
        expected.OnMainMenu(expected.ActiveManager, isReturn: false); // Aufbau des Hauptmenüs (E4 = B)

        d.OpenTile("Laden");
        d.Type("file05");
        d.Press(Key.Enter);

        Assert.IsType<MainMenuScreen>(d.Host.Current);
        Assert.Equal(Saved(expected), Saved(d.Host.Game!));
        Assert.Contains("20. Sept.  1993", d.Host.Game!.DateText);
    }

    [GameDataFact]
    public void LoadDialog_ImportsHok_ByClickTwice()
    {
        Copy("FILE29.HOK");
        var d = NewDriver();
        d.OpenTile("Laden");
        var rect = EntryRectOf(_directory, "FILE29.HOK");
        d.Click(rect.X + 3, rect.Y + 3);
        Assert.Equal("FILE29", Assert.IsType<SaveLoadScreen>(d.Host.Current).FileName);
        d.Click(rect.X + 3, rect.Y + 3);

        Assert.IsType<MainMenuScreen>(d.Host.Current);
        Assert.Equal("dsfs", d.Host.Game!.Managers[0].Name);
    }

    /// <summary>Gleicher Name als Remake- und Original-Spielstand: Tippen lädt den Remake-Stand, der Klick auf den HOK-Eintrag den HOK.</summary>
    [GameDataFact]
    public void SameName_TypedLoadsRemakeSave_ClickOnHokImports()
    {
        var d = NewDriver();
        d.OpenTile("Sichern");
        d.Type("file05");
        d.Press(Key.Enter);
        var remake = Saved(d.Host.Game!);
        Copy("FILE05.HOK");

        d.OpenTile("Laden");
        d.Type("file05");
        d.Press(Key.Enter);
        Assert.Equal(remake, Saved(d.Host.Game!));

        d.OpenTile("Laden");
        var rect = EntryRectOf(_directory, "FILE05.HOK");
        d.Click(rect.X + 3, rect.Y + 3);
        d.Click(rect.X + 3, rect.Y + 3);
        Assert.IsType<MainMenuScreen>(d.Host.Current);
        Assert.NotEqual(remake, Saved(d.Host.Game!));
        Assert.Contains("20. Sept.  1993", d.Host.Game!.DateText);
    }

    /// <summary>
    /// Review R14HI-05 (M45): Name getippt, dann **ein** Klick auf den gleichnamigen HOK-Eintrag → der angeklickte HOK wird importiert,
    /// nicht der gleichnamige Remake-Stand (<c>_selected</c> muss beim Ausführen auf den Eintrag zeigen).
    /// </summary>
    [GameDataFact]
    public void SameName_TypedThenSingleClickOnHok_Imports()
    {
        var d = NewDriver();
        d.OpenTile("Sichern");
        d.Type("file05");
        d.Press(Key.Enter);
        var remake = Saved(d.Host.Game!);
        Copy("FILE05.HOK");

        d.OpenTile("Laden");
        d.Type("file05");
        var rect = EntryRectOf(_directory, "FILE05.HOK");
        d.Click(rect.X + 3, rect.Y + 3);
        Assert.IsType<MainMenuScreen>(d.Host.Current);
        Assert.NotEqual(remake, Saved(d.Host.Game!));
        Assert.Contains("20. Sept.  1993", d.Host.Game!.DateText);
    }

    /// <summary>[ORIGINAL] KAPUTT.HOK zeigt im Original „Der kleine Mogelguru“ (Bild kaputt_mogelguru.png); geladen wird nichts.</summary>
    [GameDataFact]
    public void ChecksumError_ShowsMogelguru_AndKeepsRunningGame()
    {
        Copy("KAPUTT.HOK");
        var d = NewDriver();
        var before = Saved(d.Host.Game!);

        d.OpenTile("Laden");
        d.Type("kaputt");
        d.Press(Key.Enter);
        Assert.IsType<MogelguruScreen>(d.Host.Current);
        Assert.Equal(before, Saved(d.Host.Game!));

        d.Click(160, 120);
        Assert.IsType<SaveLoadScreen>(d.Host.Current);
        Assert.Equal(before, Saved(d.Host.Game!));
    }

    [GameDataFact]
    public void TryImport_FlagsChecksumError_AndImportsValidSave()
    {
        var database = ClubDatabase.Load(TestGameData.Load());
        var broken = OriginalImport.TryImport(File.ReadAllBytes(OriginalSave("KAPUTT.HOK")), database, 1);
        Assert.True(broken.ChecksumError);
        Assert.Null(broken.Game);

        var ok = OriginalImport.TryImport(File.ReadAllBytes(OriginalSave("FILE11.HOK")), database, 1);
        Assert.True(ok.Success, ok.Error);
        Assert.Null(ok.Error);
    }

    /// <summary>Im Speichern-Dialog sind Original-Spielstände nur zur Ansicht: ein Klick übernimmt den Namen nicht.</summary>
    [GameDataFact]
    public void SaveDialog_DoesNotSelectHok()
    {
        Copy("FILE05.HOK");
        var d = NewDriver();
        d.OpenTile("Sichern");
        var rect = EntryRectOf(_directory, "FILE05.HOK");
        d.Click(rect.X + 3, rect.Y + 3);
        d.Click(rect.X + 3, rect.Y + 3);
        var screen = Assert.IsType<SaveLoadScreen>(d.Host.Current);
        Assert.Equal("", screen.FileName);
        Assert.Equal([Path.Combine(_directory, "FILE05.HOK")], Directory.GetFiles(_directory));
    }

    /// <summary>
    /// Ordner mit den Original-Spielständen: <c>EM_SAVEGAMES</c> oder <c>spielstaende</c> neben dem (aufgelösten) Ordner
    /// <c>gamedata</c> (wie <c>OriginalImportTests</c>).
    /// </summary>
    private static string OriginalSave(string name)
    {
        var folder = Environment.GetEnvironmentVariable("EM_SAVEGAMES");
        if (string.IsNullOrEmpty(folder))
        {
            var root = TestGameData.Load().RootDirectory;
            var real = Directory.ResolveLinkTarget(root.TrimEnd('/', '\\'), true)?.FullName ?? root;
            folder = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(real).TrimEnd('/', '\\'))!, "spielstaende");
        }
        var path = Path.Combine(folder, name);
        return File.Exists(path) ? path
            : throw new FileNotFoundException($"Original-Spielstand {name} fehlt in {folder} (Ordner per EM_SAVEGAMES angeben).");
    }
}
