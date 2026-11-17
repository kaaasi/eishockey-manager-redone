using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>Spur <c>einzelpunkte</c> (Runde 22, Welle 2): kleine Befunde der Liste V, je Befund eine Fundstelle in em.exe.</summary>
public class EinzelpunkteTests
{
    /// <summary>
    /// A104 (Review #884 P2-2), Datei 0x4DB6: Gegentore und Minuten gehen an die ersten Torhüter des Vereins in
    /// <b>Poolreihenfolge</b>, nicht an den aufgestellten. Vorher bekam der aufgestellte Torhüter die Zähler zuerst.
    /// </summary>
    private static int PoolIndex(TransferMarket market, Player p) =>
        market.Pool.FirstOrDefault(s => ReferenceEquals(s.Player, p))?.Index ?? int.MaxValue;

    [Fact]
    public void ComputerClub_GoalsAgainstGoToFirstGoalieInPoolOrder()
    {
        var game = TestGames.Create();
        var club = game.Clubs.First(c => c.Index < MatchEngine.FirstForeignClub
            && !game.Managers.Any(m => m.ClubIndex == c.Index) && c.Players.Count(p => p.Role == PlayerRole.Goalkeeper) >= 2);
        var goalies = club.Players.Where(p => p.Role == PlayerRole.Goalkeeper).ToList();
        var market = game.TransferMarket;
        var inPool = goalies.OrderBy(p => PoolIndex(market, p)).ToList();
        // Der Aufgestellte (hier: ein anderer als der erste im Pool) darf nicht bevorzugt werden.
        var other = goalies.First(p => !ReferenceEquals(p, inPool[0]));
        club.Lineup.Place(LineupSlot.Goalkeeper, other);
        var opponent = game.Clubs.First(c => c.League == club.League && c != club);

        var random = new Random(3);
        for (var i = 0; i < 200; i++)
            MatchEngine.CreditPlayers(game, club, opponent, MatchCompetition.League, new MatchReport { HomeGoals = 1, AwayGoals = 3 }, random);

        Assert.True(inPool[0].GoalieMinutes > other.GoalieMinutes, $"{inPool[0].GoalieMinutes} / {other.GoalieMinutes}");
        Assert.True(inPool[0].GoalsAgainst > other.GoalsAgainst);
    }
}
