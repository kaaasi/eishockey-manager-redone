using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.SaveLoad;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Integration Runde 3, Welle C: Laden/Sichern-Dialog und automatisches Speichern (Spur speichern-dialog) im
/// Spielmodus „nur Weltkugel“ mit Menü des Bundestrainers (Spur wettbewerbe-weltkugel). Keine der beiden Spuren
/// kannte die andere; geprüft wird das Zusammenspiel über die Oberfläche.
/// </summary>
public class IntegrationRunde3cTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "em-integration3c-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    private static string Saved(GameState game) => Convert.ToBase64String(SaveGame.Save(game));

    /// <summary>Spielvorbereitung mit einem Manager, Modus „nur Weltkugel“, Start ins Menü des Bundestrainers.</summary>
    private (ScreenDriver Driver, SaveContext Context) StartWorldOnly(bool autoSave)
    {
        var d = FlowTests.CreateDriver();
        var context = SaveContext.For(d.Host);
        context.Directory = _directory;
        context.AutoSaveOverride = autoSave;
        d.Host.Show(new TitleScreen(d.Host));
        d.Frame();
        d.Click(160, 120);
        Assert.IsType<SetupScreen>(d.Host.Current);
        d.Click(20, 60);
        var cell = SetupScreen.HeadCell(3);
        d.Click(cell.X + 5, cell.Y + 5);
        d.Type("LetsShow");
        d.Press(Key.Enter);
        d.Click(SetupScreen.ModeFlag.X + 5, SetupScreen.ModeFlag.Y + 5); // Fahne aus → nur Weltkugel
        d.Click(SetupScreen.StartButton.X + 5, SetupScreen.StartButton.Y + 5);
        Assert.IsType<MainMenuScreen>(d.Host.Current);
        Assert.Equal(GameMode.WorldOnly, d.Host.Game!.Mode);
        Assert.True(d.Host.Game.NationalCoachMenu);
        return (d, context);
    }

    /// <summary>Sichern und Laden über die Kacheln im Menü des Bundestrainers stellen Modus, Ernennung und Konten her.</summary>
    [GameDataFact]
    public void WorldOnly_SaveThenLoad_ThroughDialog_InCoachMenu()
    {
        var (d, _) = StartWorldOnly(autoSave: false);
        // #195: „Los jetzt!“ springt schon zum ersten WM-Termin (Ernennung)
        var game = d.Host.Game!;
        Assert.NotNull(game.NationalCoach);
        var saved = Saved(game);
        var before = StateFingerprint.Of(game);

        d.OpenTile("Sichern");
        Assert.IsType<SaveLoadScreen>(d.Host.Current);
        d.Type("bt1");
        d.Press(Key.Enter);
        Assert.IsType<MainMenuScreen>(d.Host.Current);
        Assert.Equal(before, StateFingerprint.Of(game)); // Sichern ändert den Zustand nicht
        Assert.True(File.Exists(Path.Combine(_directory, "BT1.ems")));

        d.PlayMatchDay();
        Assert.NotEqual(saved, Saved(d.Host.Game!));

        d.OpenTile("Laden");
        d.Type("bt1");
        d.Press(Key.Enter);
        Assert.IsType<MainMenuScreen>(d.Host.Current);
        var loaded = d.Host.Game!;
        Assert.Equal(saved, Saved(loaded));
        Assert.Equal(GameMode.WorldOnly, loaded.Mode);
        Assert.True(loaded.NationalCoachMenu);
        Assert.NotNull(loaded.NationalCoach);
    }

    /// <summary>
    /// Das automatische Speichern zählt auch die Hauptmenüs des Bundestrainers; die Datei lädt im Modus „nur Weltkugel“
    /// und läuft mit dem ununterbrochen gespielten Stand gleich weiter.
    /// </summary>
    [GameDataFact]
    public void WorldOnly_AutoSave_WritesLoadableFile_ThatContinuesIdentically()
    {
        var (d, context) = StartWorldOnly(autoSave: true);
        var game = d.Host.Game!;
        for (var i = 0; i < 40 && context.AutoSaveWrites == 0; i++)
            d.PlayMatchDay();
        Assert.True(context.AutoSaveWrites >= 1, "Kein automatischer Spielstand im Modus „nur Weltkugel“.");
        Assert.Null(context.LastAutoSaveError);

        var file = Directory.GetFiles(_directory).Single();
        var twin = SaveGame.Load(File.ReadAllBytes(file)).Game!;
        Assert.Equal(GameMode.WorldOnly, twin.Mode);
        Assert.True(twin.NationalCoachMenu);
        Assert.Equal(Saved(game), Saved(twin));
        for (var i = 0; i < 6 && game.Phase != SeasonPhase.SeasonOver; i++)
        {
            game.Continue();
            twin.Continue();
        }
        Assert.Equal(Saved(game), Saved(twin));
    }

    /// <summary>
    /// R3C-01 (Review Integration Welle C, Issue siehe PR #89): Im Hot-Seat bleibt beim Weitergeben an den nächsten
    /// Manager das Hauptmenü stehen. Auch dieser neue Zug ist ein Hauptmenü-Aufruf (em.exe 0x44E49–0x44E69: Prüfung auf 10
    /// vor der Erhöhung, für jeden Manager). Zwei sonst gleiche Abläufe – einmal mit Kachelbesuch nach jedem Zug, einmal
    /// ohne – müssen zur selben Zeit dieselben automatischen Spielstände schreiben.
    /// </summary>
    [GameDataFact]
    public void HotSeat_AutoSave_DoesNotDependOnTileVisits()
    {
        var plain = HotSeatAutoSaves(visitTile: false);
        var visited = HotSeatAutoSaves(visitTile: true);
        Assert.True(plain.Count(w => w.Bytes is not null) >= 2, "Zu wenige automatische Spielstände.");
        Assert.Equal(plain.Select(w => (w.Day, w.Manager, w.Bytes)), visited.Select(w => (w.Day, w.Manager, w.Bytes)));
    }

    private List<(int Day, int Manager, string? Bytes)> HotSeatAutoSaves(bool visitTile)
    {
        var directory = Path.Combine(_directory, visitTile ? "mit" : "ohne");
        var d = FlowTests.CreateDriver();
        var context = SaveContext.For(d.Host);
        context.Directory = directory;
        context.AutoSaveOverride = true;
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "A", Head = 0, ClubIndex = 2 });
        setup.Managers.Add(new ManagerSetup { Name = "B", Head = 1, ClubIndex = 30 });
        var game = NewGame.Create(d.Host.Assets.Database, setup, 3);
        d.Host.Game = game;
        d.Host.Show(new MainMenuScreen(d.Host));
        d.Frame();

        var steps = new List<(int, int, string?)>();
        for (var step = 0; step < 50; step++)
        {
            var writes = context.AutoSaveWrites;
            d.PlayMatchDay();
            if (visitTile)
            {
                d.OpenTile("Laden");
                d.Press(Key.Escape);
                Assert.IsType<MainMenuScreen>(d.Host.Current);
            }
            var file = SaveFiles.PathOf(directory, SaveFiles.AutoSaveBaseName);
            var bytes = context.AutoSaveWrites != writes ? Convert.ToBase64String(File.ReadAllBytes(file)) : null;
            steps.Add((game.Day, game.CurrentManager, bytes));
        }
        return steps;
    }
}
