using System.Text.Json.Nodes;
using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Fix Runde 13 (R13G-01, #613; R13KI-04, #606): A091 Nr. = Kaderplatz + 1. Das Hauptmenü nummeriert nicht mehr um (AGENTS.md §4:
/// dort nur die Neuaufstellung mit „automatisch aufstellen“); Spielstände aus der Zeit der freien Rückennummern werden einmalig
/// beim Laden für alle Vereine umnummeriert (Schema 38 → 39, <c>SaveGame.Migration.Nummern.cs</c>).
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

    [Theory]
    [InlineData(36)]
    [InlineData(38)]
    public void OldSave_WithFreeNumbers_RenumbersAllClubsOnLoad(int schema)
    {
        var game = Create();
        var managerClub = game.Managers[0].ClubIndex;
        var computerClub = game.Clubs.First(c => c.Index != managerClub && c.Players.Count > 0).Index;
        var root = JsonNode.Parse(SaveGame.Save(game))!.AsObject();
        root["schema"] = schema;
        var players = root["spieler"]!.AsArray().Select(p => p!.AsObject()).ToDictionary(p => (int)p["Id"]!);
        foreach (var index in new[] { managerClub, computerClub })
        {
            var items = root["vereine"]![index]!["Players"]!["items"]!.AsArray();
            for (var i = 0; i < items.Count; i++)
                players[(int)items[i]!["$spieler"]!]["Number"] = 99 - 3 * i; // freie Rückennummern wie vor A091
        }

        var loaded = SaveGame.Load(System.Text.Encoding.UTF8.GetBytes(root.ToJsonString()));

        Assert.True(loaded.Success, loaded.Error);
        foreach (var club in loaded.Game!.Clubs)
            for (var i = 0; i < club.Players.Count; i++)
                Assert.Equal(i + 1, club.Players[i].Number);
        Assert.Equal(SaveGame.Save(game), SaveGame.Save(loaded.Game));
    }

    [Fact]
    public void CurrentSave_RoundTrip_Unchanged()
    {
        var game = Create();
        var data = SaveGame.Save(game);
        var loaded = SaveGame.Load(data);
        Assert.True(loaded.Success, loaded.Error);
        Assert.Equal(data, SaveGame.Save(loaded.Game!));
    }
}
