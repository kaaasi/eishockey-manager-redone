using System.Text;
using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

public class ScheduleTests
{
    [Fact]
    public void DoubleRoundRobin_EveryPairMeetsTwiceWithSwappedHomeRight()
    {
        var teams = Enumerable.Range(0, 12).ToList();
        var days = Schedule.DoubleRoundRobin(teams);

        Assert.Equal(22, days.Count);
        Assert.All(days, d => Assert.Equal(6, d.Matches.Count));
        Assert.All(days, d => Assert.Equal(12, d.Matches.SelectMany(m => new[] { m.Home, m.Away }).Distinct().Count()));

        var pairs = days.SelectMany(d => d.Matches).ToList();
        Assert.Equal(132, pairs.Count);
        Assert.Equal(132, pairs.Distinct().Count()); // jede Heim/Gast-Kombination genau einmal
    }
}

public static class TestGames
{
    /// <summary>Synthetische Datenbank: Vereine 0–47 mit Namen „VEREIN n“ und absteigender Stärke je Liga.</summary>
    public static ClubDatabase CreateDatabase()
    {
        var data = new byte[ClubDatabase.FileLength];
        for (var i = 0; i < ClubDatabase.ClubsWithSquad; i++)
        {
            Encoding.ASCII.GetBytes($"VEREIN {i}").CopyTo(data, ClubDatabase.SquadOffset + i * ClubDatabase.SquadRecordLength);
            data[ClubDatabase.StrengthOffset + i * 3] = (byte)(100 - i % 12 * 5);
        }
        return ClubDatabase.Parse(data);
    }

    public static GameState Create(int seed = 3)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Test", Head = 0, ClubIndex = 2 });
        return NewGame.Create(CreateDatabase(), setup, seed);
    }
}

public class NewGameTests
{
    private static ClubDatabase CreateDatabase() => TestGames.CreateDatabase();

    [Fact]
    public void Create_AssignsChosenClubAndBuildsSchedules()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Test", Head = 3, ClubIndex = 5 });

        var game = NewGame.Create(CreateDatabase(), setup, seed: 1);

        Assert.Single(game.Managers);
        Assert.Equal(5, game.ActiveManager.ClubIndex);
        Assert.Equal(FinanceRules.StartMoney(game.ActiveManager.Level), game.ActiveManager.Money);
        foreach (var league in Enum.GetValues<League>())
        {
            Assert.Equal(12, game.LeagueClubs(league).Count());
            Assert.Equal(44, game.Schedules[league].Count);
        }
        Assert.NotNull(game.NextLeagueMatch(5));
    }

    [Fact]
    public void Create_ClubWithoutLeagueMovesIntoOberliga()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Test", ClubIndex = 42 });

        var game = NewGame.Create(CreateDatabase(), setup, seed: 7);

        Assert.Equal(League.Oberliga, game.Clubs[42].League);
        Assert.Equal(12, game.LeagueClubs(League.Oberliga).Count());
    }

    [Theory]
    [InlineData(StartStrength.OberesDrittel, 0, 3)]
    [InlineData(StartStrength.Mittelfeld, 4, 7)]
    [InlineData(StartStrength.Abstiegskampf, 8, 11)]
    public void Create_RandomClubComesFromChosenThird(StartStrength strength, int min, int max)
    {
        for (var seed = 0; seed < 20; seed++)
        {
            var setup = new GameSetup();
            setup.Managers.Add(new ManagerSetup { Name = "Test", StartLeague = League.ZweiteLiga, StartStrength = strength });
            var game = NewGame.Create(CreateDatabase(), setup, seed);
            var rank = game.ActiveManager.ClubIndex - 12; // Stärke fällt mit dem Index
            Assert.InRange(rank, min, max);
        }
    }

    [Theory]
    [InlineData(400000, "400.000 DM")]
    [InlineData(-239321, "-239.321 DM")]
    [InlineData(0, "0 DM")]
    public void FormatMoney_LikeOriginal(long amount, string expected) =>
        Assert.Equal(expected, GameState.FormatMoney(amount));
}
