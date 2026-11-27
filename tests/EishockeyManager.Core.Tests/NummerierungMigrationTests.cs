using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Fix Runde 13 (R13G-01, #613; R13KI-04, #606): A091 Nr. = Kaderplatz + 1. Das Hauptmenü nummeriert nicht mehr um (AGENTS.md §4:
/// dort nur die Neuaufstellung mit „automatisch aufstellen“). Die Umnummerierung älterer Stände beim Laden (Schema 38 → 39) entfiel
/// in Runde 27 mit allen Migrationen (aufraeumen-migration, R27-D2 = B).
/// </summary>
public class NummerierungMigrationTests
{
    private static GameState Create()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "M", ClubIndex = 5 });
        return NewGame.Create(TestGames.CreateDatabase(), setup, 1);
    }

    [Fact]
    public void MainMenu_WithAutoLineupOff_KeepsNumbers()
    {
        var game = Create();
        var manager = game.Managers[0];
        var club = game.Clubs[manager.ClubIndex];
        club.AutoLineupEnabled = false;
        club.Players[0].Number = 77;
        var before = SaveGame.Save(game);

        game.OnMainMenu(manager, isReturn: false);
        game.OnMainMenu(manager, isReturn: true);

        Assert.Equal(77, club.Players[0].Number);
        Assert.Equal(before, SaveGame.Save(game));
    }

    [Fact]
    public void MainMenu_WithAutoLineupOn_DoesNotRenumber()
    {
        // Die Neuaufstellung 0x426BC ändert keine Nr.; die Nr. folgt dem Kaderplatz nur bei Kauf und Abgang (und beim Laden).
        var game = Create();
        var manager = game.Managers[0];
        var club = game.Clubs[manager.ClubIndex];
        Assert.True(club.AutoLineupEnabled);
        club.Players[0].Number = 77;

        game.OnMainMenu(manager, isReturn: false);

        Assert.Equal(77, club.Players[0].Number);
    }

    /// <summary>
    /// Laden nummeriert nicht um. #620 (R13GN-03): Nr. = Platz + 1 ist ohnehin erfüllt, daher eine abweichende Nr. vor dem
    /// Sichern: Sie muss das Laden überstehen.
    /// </summary>
    [Fact]
    public void CurrentSave_RoundTrip_Unchanged()
    {
        var game = Create();
        var player = game.Clubs[game.ActiveManager.ClubIndex].Players[0];
        player.Number = 77;
        var data = SaveGame.Save(game);
        var loaded = SaveGame.Load(data);
        Assert.True(loaded.Success, loaded.Error);
        Assert.Equal(data, SaveGame.Save(loaded.Game!));
        Assert.Equal(77, loaded.Game!.FindPlayer(player.Id)!.Number);
    }
}
