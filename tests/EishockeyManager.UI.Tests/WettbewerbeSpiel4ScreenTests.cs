using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.UI.Screens.Statistics;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur wettbewerbe-spiel, Teil 4 (Inventur P2 Nr. 16, A349): Die Über-/Unterzahl-Zähler der Vereinsstatistik kommen aus
/// der Minutenrechnung (0xA939–0xA9A2, 0xAA2C–0xAAEF, 0x35A84/0x35B18; <c>docs/spielberechnung.md</c> §18) über die
/// Zeitleiste. Anschlussprüfung: schon vor Teil 4 grün, veraltet war nur der Kommentar des Bildschirms.
/// </summary>
public class WettbewerbeSpiel4ScreenTests
{
    private static GameState NewGame()
    {
        var data = new byte[ClubDatabase.FileLength];
        for (var i = 0; i < ClubDatabase.ClubsWithSquad; i++)
        {
            System.Text.Encoding.ASCII.GetBytes($"VEREIN {i}").CopyTo(data, ClubDatabase.SquadOffset + i * ClubDatabase.SquadRecordLength);
            data[ClubDatabase.StrengthOffset + i * 3] = (byte)(100 - i % 12 * 5);
        }
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Test", Head = 0, ClubIndex = 5, StartLeague = League.Bundesliga });
        return Core.NewGame.Create(ClubDatabase.Parse(data), setup, 7);
    }

    [Fact]
    public void PowerPlayCounts_FromPlayedMatches_ShownOnClubStatistics()
    {
        var game = NewGame();
        var manager = game.ActiveManager;
        for (var day = 0; day < 200 && manager.PowerPlayCounts.Take(4).Sum() == 0; day++)
            game.Continue();
        var counts = manager.PowerPlayCounts;
        Assert.True(counts.Take(4).Sum() > 0, "nach 200 Tagen kein Tor in Über- oder Unterzahl gezählt");
        Assert.True(counts[(int)PowerPlayCounter.PowerPlays] + counts[(int)PowerPlayCounter.ShortHanded] > 0);
        var items = ClubStatisticsScreen.Layout(game, manager);
        var cells = items.Where(i => i.X == ClubStatisticsScreen.PowerPlayX).Select(i => i.Text).ToList();
        Assert.Equal(6, cells.Count);
        for (var k = 0; k < 4; k++)
            Assert.Equal(ClubStatisticsScreen.Number(Math.Clamp(counts[k], 0, 100), 3, false), cells[k]);
        Assert.Equal(ClubStatisticsScreen.Number(PowerPlay.PowerPlaySuccess(counts), 3, false) + "#", cells[4]);
    }
}
