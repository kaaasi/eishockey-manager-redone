using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Integration Runde 11 (Anschluss aus wettbewerbe-spiel Teil 8): Zu Beginn jedes Managerzugs stellt das Original mit der
/// Einstellung „automatisch aufstellen“ neu auf (em.exe Datei 0x44E22, Aufruf Datei 0x450C6 von 0x426BC).
/// </summary>
public class AutoLineupTurnTests
{
    private static GameState TwoManagers()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Anna", Head = 0, ClubIndex = 2 });
        setup.Managers.Add(new ManagerSetup { Name = "Bernd", Head = 1, ClubIndex = 5 });
        return NewGame.Create(TestGames.CreateDatabase(), setup, 3);
    }

    [Fact]
    public void TurnStart_WithSetting_LinesUpAgain()
    {
        var game = TwoManagers();
        Assert.Equal(0, game.CurrentManager);
        var club = game.Clubs[5];
        club.AutoLineupEnabled = true;
        club.Lineup.ClearAll();

        game.Continue(); // Anna gibt den Zug an Bernd weiter
        Assert.Equal(1, game.CurrentManager);
        Assert.NotEmpty(club.Lineup.Players);
    }

    [Fact]
    public void TurnStart_WithoutSetting_KeepsLineup()
    {
        var game = TwoManagers();
        var club = game.Clubs[5];
        club.AutoLineupEnabled = false;
        club.Lineup.ClearAll();

        game.Continue();
        Assert.Equal(1, game.CurrentManager);
        Assert.Empty(club.Lineup.Players);
    }
}
