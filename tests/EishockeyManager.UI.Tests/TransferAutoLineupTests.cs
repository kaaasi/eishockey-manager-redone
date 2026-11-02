using EishockeyManager.Tests.Support;
using EishockeyManager.UI;
using EishockeyManager.UI.Screens.Team;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Integration Runde 11 (Anschluss aus wettbewerbe-spiel Teil 8): Beim Verlassen der Transferliste stellt das Original mit
/// der Einstellung „automatisch aufstellen“ neu auf (em.exe Datei 0x47BB5 in 0x465F1); Öffnen allein ändert nichts.
/// </summary>
public class TransferAutoLineupTests
{
    [GameDataFact]
    public void LeavingTransfer_WithSetting_LinesUpAgain()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        var club = game.Clubs[game.ActiveManager.ClubIndex];
        club.AutoLineupEnabled = true;
        club.Lineup.ClearAll();

        d.OpenTile("Transfer");
        Assert.IsType<TransferScreen>(d.Host.Current);
        Assert.Empty(club.Lineup.Players);
        d.Press(Key.Escape);
        Assert.IsNotType<TransferScreen>(d.Host.Current);
        Assert.NotEmpty(club.Lineup.Players);
    }
}
