using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Competitions;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur wettbewerbe-komplett Teil 3 (R8W3I-10, Testlücke): Seit #195 springt Modus 1 schon mit „Los jetzt!“ zum ersten
/// WM-Termin; der Weg „Hauptmenü vor der Ernennung“ (<see cref="WorldOnlyMenuTests"/> vor 50495d6) gibt es nur noch bei
/// alten Spielständen von Tag 0. Dort bleiben WM-Kader und „WM Vorbereitung“ gesperrt ([APPROX] in
/// <see cref="NationalMenu.IsLocked"/>), Öffnen und Überfahren ändern nichts, „und tschüß…“ holt den Sprung nach.
/// </summary>
public class WorldOnlyDayZeroMenuTests
{
    [GameDataFact]
    public void WorldOnly_OldSaveFromDayZero_LocksSquadUntilAppointment()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "LetsShow", Head = 3, ClubIndex = 2 });
        Assert.True(setup.ToggleClubCompetitions()); // Fahne aus: Modus 1 („nur Weltkugel“)
        var created = NewGame.Create(Formats.ClubDatabase.Load(TestGameData.Load()), setup, 1993);
        // Spielstand von Tag 0 wie vor #195 (ohne den Sprung aus der Spielvorbereitung).
        var game = SaveGame.Load(SaveGame.Save(created)).Game!;
        Assert.Equal(GameMode.WorldOnly, game.Mode);
        Assert.True(game.NationalCoachMenu);
        Assert.Null(game.NationalCoach);
        Assert.Equal(0, game.CurrentDay);

        var d = FlowTests.CreateDriver();
        d.Host.Game = game;
        d.Host.Show(new MainMenuScreen(d.Host));
        d.Frame();
        var menu = Assert.IsType<MainMenuScreen>(d.Host.Current);

        // Vor der Ernennung: gesperrte Kacheln öffnen nichts, Überfahren und Öffnen ändern nichts.
        var before = StateFingerprint.Of(game);
        foreach (var locked in new[] { "Transfer", "Training", "Verträge", "Finanzen" })
        {
            d.OpenTile(locked);
            Assert.Same(menu, d.Host.Current);
        }
        d.Move(30, 220);
        d.Frame();
        Assert.Equal(before, StateFingerprint.Of(game));

        // „und tschüß…“: Sprung zum ersten WM-Termin, Ernennung, danach ist der WM-Kader erreichbar.
        d.PlayMatchDay();
        Assert.Equal(GameState.FirstWorldCupDay, game.CurrentDay);
        Assert.NotNull(game.NationalCoach);
        Assert.Equal(0, game.PlayedMatchDays);
        d.OpenTile("Transfer");
        Assert.IsType<NationalSquadScreen>(d.Host.Current);
    }
}
