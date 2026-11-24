using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur aufstellung (Runde 25), Teil 3 S07 (A898): Spiele eines Managervereins zählen wie im Original vor den Minuten (Spielabrechnung
/// 0x544C0, Datei 0x54910–0x5499E) und beim Aufstellen im Spiel sofort (Datei 0x415B7–0x41622), nicht am Spielende.
/// </summary>
public class AufstellungS07Tests
{
    private static (Club Club, LiveMatchDay Day, LiveTeam Team, Dictionary<Player, int> Before) LiveMatch(MatchCompetition competition)
    {
        const int managed = 2;
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "M", Head = 0, ClubIndex = managed });
        var game = Core.NewGame.Create(TestGames.CreateDatabase(), setup, 5);
        var club = game.Clubs[managed];
        var before = club.Players.ToDictionary(p => p, p => p.Games);
        var league = club.League!.Value;
        var (home, away) = game.Schedules[league][0].Matches.First(m => m.Home == managed || m.Away == managed);
        var day = LiveMatchDay.Begin(game, [new LiveFixture(game.Clubs[home], game.Clubs[away], competition,
            KnockOut: false, new GameRandom(7))]);
        for (var i = 0; i < 10; i++)
            day.Step();
        club.AutoLineupEnabled = false;
        return (club, day, day.MatchOf(managed)!.TeamOf(managed)!, before);
    }

    /// <summary>
    /// [ORIGINAL] Die Aufgestellten zählen ihr Spiel vor den Minuten (Datei 0x54910–0x5499E): in Minute 10 steht es schon in der Spalte SP;
    /// am Spielende zählt es nicht noch einmal. Vorher (rot): erst am Spielende.
    /// </summary>
    [Fact]
    public void StartLineup_CountsBeforeMinutes_Once()
    {
        var (club, day, _, before) = LiveMatch(MatchCompetition.League);
        var lineup = club.Lineup.Players.ToList();
        Assert.All(lineup, p => Assert.Equal(before[p] + 1, p.Games));
        day.Finish();
        Assert.All(lineup, p => Assert.Equal(before[p] + 1, p.Games));
    }

    /// <summary>
    /// [ORIGINAL] Das Aufstellen im Spiel zählt sofort (Datei 0x415B7–0x41622); wer im selben Eingriff wieder herauskommt, behält das Spiel.
    /// Vorher (rot): gezählt erst am Spielende, ein Hinein und Hinaus im selben Eingriff gar nicht.
    /// </summary>
    [Fact]
    public void PlacedInMatch_CountsAtOnce_EvenIfTakenOutAgain()
    {
        var (club, day, team, before) = LiveMatch(MatchCompetition.DebCup);
        var bench = club.Players.Where(p => p.Available && p.Role == PlayerRole.Forward && !club.Lineup.Contains(p)).Take(2).ToList();
        Assert.Equal(2, bench.Count);
        var cupBefore = bench.Select(p => p.CupGames).ToList();
        club.ClearLineupSlotInMatch(LineupSlot.Forward(2, 0));
        int? line = 2;
        Assert.Equal(LineupResult.Assigned, club.ClickPlayerInMatch(bench[0], ref line, right: false));
        Assert.Equal(before[bench[0]] + 1, bench[0].Games);
        Assert.Equal(cupBefore[0] + 1, bench[0].CupGames);

        // Hinein und wieder hinaus im selben Eingriff, dann der zweite hinein.
        Assert.Equal(LineupResult.TakenOut, club.ClickPlayerInMatch(bench[0], ref line, right: false));
        Assert.Equal(LineupResult.Assigned, club.ClickPlayerInMatch(bench[1], ref line, right: false));
        LiveLineup.Apply(team, club);
        day.Finish();

        Assert.Equal(before[bench[0]] + 1, bench[0].Games);
        Assert.Equal(before[bench[1]] + 1, bench[1].Games);
        Assert.Equal(cupBefore[1] + 1, bench[1].CupGames);
    }
}
