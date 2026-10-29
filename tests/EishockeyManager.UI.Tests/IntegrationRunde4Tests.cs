using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Competitions;
using EishockeyManager.UI.Screens.SaveLoad;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Integration Runde 4: Aufstellung und Taktik des Nationalteams (Spur nationalteam-2) zusammen mit dem
/// Laden/Sichern-Dialog im Menü des Bundestrainers und den Kachelsperren (Spur tabellen-highscore).
/// </summary>
public class IntegrationRunde4Tests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "em-integration4-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    private ScreenDriver StartWorldOnly()
    {
        var d = FlowTests.CreateDriver();
        var context = SaveContext.For(d.Host);
        context.Directory = _directory;
        context.AutoSaveOverride = false;
        d.Host.Show(new TitleScreen(d.Host));
        d.Frame();
        d.Click(160, 120);
        d.Click(20, 60);
        var cell = SetupScreen.HeadCell(3);
        d.Click(cell.X + 5, cell.Y + 5);
        d.Type("LetsShow");
        d.Press(Key.Enter);
        d.Click(SetupScreen.ModeFlag.X + 5, SetupScreen.ModeFlag.Y + 5); // nur Weltkugel
        d.Click(SetupScreen.StartButton.X + 5, SetupScreen.StartButton.Y + 5);
        Assert.Equal(GameMode.WorldOnly, d.Host.Game!.Mode);
        d.PlayMatchDay(); // Sprung zur WM, Ernennung
        Assert.NotNull(d.Host.Game.NationalCoach);
        return d;
    }

    /// <summary>
    /// Eine selbst gesetzte Aufstellung und Taktik des Nationalteams übersteht Sichern und Laden über die Kacheln
    /// im Menü des Bundestrainers (Schema 4); die Bildschirme Aufstellung/Taktik öffnen danach ohne Zustandsänderung.
    /// </summary>
    [GameDataFact]
    public void NationalLineup_SurvivesSaveLoadThroughDialog_InCoachMenu()
    {
        var d = StartWorldOnly();
        var game = d.Host.Game!;
        var coach = game.NationalCoach!;
        game.AutoNominate(); // WM-Kader wie mit „A“ im Bildschirm „Spieler in WM-Kader“
        Assert.NotEmpty(coach.Squad);
        var keeper = coach.Squad.Select(m => m.Player).First(p => p.InjuredGames == 0 && p.SuspendedGames == 0);
        var slot = new LineupSlot(0, keeper.Role, 0);
        if (coach.Formation[slot] is not null)
            coach.ClearSlot(slot);
        Assert.Equal(LineupResult.Assigned, coach.Assign(slot, keeper));
        coach.Tactics.AttackMode = TacticMode.Semi;
        var strength = game.NationalMatchStrength(NationalTeams.Germany);
        var saved = Convert.ToBase64String(SaveGame.Save(game));

        d.OpenTile("Sichern");
        d.Type("nt4");
        d.Press(Key.Enter);
        d.PlayMatchDay();
        d.OpenTile("Laden");
        d.Type("nt4");
        d.Press(Key.Enter);
        Assert.IsType<MainMenuScreen>(d.Host.Current);

        var loaded = d.Host.Game!;
        Assert.Equal(saved, Convert.ToBase64String(SaveGame.Save(loaded)));
        Assert.Same(loaded.NationalCoach!.Formation[slot],
            loaded.NationalCoach.Squad.Select(m => m.Player).Single(p => p.Id == keeper.Id));
        Assert.Equal(TacticMode.Semi, loaded.NationalCoach.Tactics.AttackMode);
        Assert.Equal(strength, loaded.NationalMatchStrength(NationalTeams.Germany));

        var before = StateFingerprint.Of(loaded);
        foreach (var tile in new[] { "Aufstellung", "Taktik" })
        {
            d.OpenTile(tile);
            Assert.IsNotType<MainMenuScreen>(d.Host.Current);
            d.Frames(2);
            d.Press(Key.Escape);
            Assert.IsType<MainMenuScreen>(d.Host.Current);
        }
        Assert.Equal(before, StateFingerprint.Of(loaded));
    }

    /// <summary>
    /// Die neuen Sperren der Spielvorbereitung („Werbung“, „Neuer“) ändern das Menü des Bundestrainers nicht:
    /// dort ist die Reihe Manager ohnehin vollständig gesperrt (DGROUP 0x5159 + 42), Laden/Sichern bleiben frei.
    /// </summary>
    [GameDataFact]
    public void CoachMenu_ManagerRowLocked_SaveLoadFree_WithSetupLocks()
    {
        var d = StartWorldOnly();
        var game = d.Host.Game!;
        foreach (var tile in MenuTiles.Small[0].Where(t => !t.IsEmpty))
            Assert.True(NationalMenu.IsLocked(game, tile), tile.Name);
        foreach (var name in new[] { "Laden", "Sichern", "Einstellungen", "Aufstellung", "Taktik" })
            Assert.False(NationalMenu.IsLocked(game, MenuTiles.Small.SelectMany(r => r).Single(t => t.Name == name)), name);
    }
}
