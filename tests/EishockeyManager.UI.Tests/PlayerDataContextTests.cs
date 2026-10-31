using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.UI.Screens.Team;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Integration Runde 8, Befund R8-D01 (#253): Das Spielerdaten-Fenster blendet Gehalt und Marktwert nach dem Verein aus,
/// nicht nach der Spielklasse. [ORIGINAL] em.exe 0x041AC, Versatz 0x7D6–0x801: Verein des Managers (Manager +0x14)
/// gegen Verein im Spielerpool (+0x1D); Transfermarkt-Spieler gehören nie zum Verein des Managers.
/// </summary>
public class PlayerDataContextTests
{
    private static GameState NewGame1()
    {
        var data = new byte[ClubDatabase.FileLength];
        for (var i = 0; i < ClubDatabase.ClubsWithSquad; i++)
        {
            System.Text.Encoding.ASCII.GetBytes($"VEREIN {i}").CopyTo(data, ClubDatabase.SquadOffset + i * ClubDatabase.SquadRecordLength);
            data[ClubDatabase.StrengthOffset + i * 3] = (byte)(100 - i % 12 * 5);
        }
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Test", Head = 0, ClubIndex = 2, StartLeague = League.Bundesliga });
        return NewGame.Create(ClubDatabase.Parse(data), setup, 3);
    }

    /// <summary>Eigener Verein: Gehalt und Marktwert sichtbar.</summary>
    [Fact]
    public void OwnClub_ShowsSalaryAndMarketValue()
    {
        var game = NewGame1();
        var own = game.Clubs[game.Managers[0].ClubIndex];
        var ctx = PlayerDataContext.Create(game, own.Players[0], own, own, 3);
        Assert.True(ctx.ShowSalary);
        Assert.True(ctx.ShowMarketValue);
    }

    /// <summary>Fremder Verein derselben Liga: Gehalt nie, Marktwert nur bei „5 − Level“ ≥ 3.</summary>
    [Theory]
    [InlineData(3, false)] // Level 3 → „5 − Level“ = 2 < 3
    [InlineData(1, true)]  // Level 1 → 4 ≥ 3
    public void OtherClubSameLeague_HidesSalary(int level, bool marketVisible)
    {
        var game = NewGame1();
        var own = game.Clubs[game.Managers[0].ClubIndex];
        var other = game.LeagueClubs(League.Bundesliga).First(c => c != own);
        Assert.Equal(CupClass.Of(own), CupClass.Of(other));
        var ctx = PlayerDataContext.Create(game, other.Players[0], other, own, level);
        Assert.False(ctx.ShowSalary);
        Assert.Equal(marketVisible, ctx.ShowMarketValue);
    }

    /// <summary>Transfermarkt (kein Verein im Remake): wie ein fremder Verein.</summary>
    [Fact]
    public void MarketPlayer_HidesSalary()
    {
        var game = NewGame1();
        var own = game.Clubs[game.Managers[0].ClubIndex];
        var ctx = PlayerDataContext.Create(game, own.Players[0], null, own, 3);
        Assert.False(ctx.ShowSalary);
        Assert.False(ctx.ShowMarketValue);
    }
}
