using EishockeyManager.Core;
using Xunit;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur spielberechnung, Teil 2: Minutenrechnung des Originals (<see cref="OriginalMinutes"/>, <see cref="PenaltyBox"/>),
/// Fundstellen in docs/spielberechnung.md §4, §6 und §9.
/// </summary>
public class SpielberechnungMinutenTests
{
    private sealed class ScriptedRandom(params int[] values) : Random
    {
        private readonly Queue<int> _values = new(values);
        public List<(int Min, int Max)> Calls { get; } = [];

        public override int Next(int minValue, int maxValue)
        {
            Calls.Add((minValue, maxValue - 1));
            Assert.True(_values.Count > 0, $"Kein Zug mehr vorgegeben für random({minValue}, {maxValue - 1}).");
            var v = _values.Dequeue();
            Assert.InRange(v, minValue, maxValue - 1);
            return v;
        }

        public bool Exhausted => _values.Count == 0;
    }

    private static LiveTeam Team(int value = 60, int effort = 60) => new()
    {
        Defense = new PartValues(value, value, value),
        Attack = new PartValues(value, value, value),
        ClubForm = 50,
        Effort = effort,
    };

    [Fact]
    public void PenaltyBox_TwoMinutes_EndAfterTwoTicks()
    {
        var box = new PenaltyBox();
        box.Add(0, 2);
        Assert.Equal(4, box.Skaters);
        Assert.Equal(1, box.Count);
        Assert.Equal(1, box.Tick());
        Assert.Equal(4, box.Skaters);
        Assert.Equal(0, box.Tick());
        Assert.Equal(5, box.Skaters);
        Assert.Equal(0, box.State);
    }

    [Fact]
    public void PenaltyBox_GoalEndsTwoMinutesButNotFive()
    {
        var box = new PenaltyBox();
        box.Add(0, 2);
        box.Release(0, force: false);
        Assert.Equal((0, 5), (box.State, box.Skaters));

        box.Add(0, 13);
        box.Release(0, force: false);
        Assert.Equal((13, 4), (box.State, box.Skaters));

        // 5 Minuten auf Platz 0, 2 Minuten auf Platz 1: das Gegentor beendet die zweite.
        box.Add(1, 2);
        Assert.Equal(3, box.Skaters);
        box.Release(0, force: false);
        Assert.Equal((13, 4), (box.State, box.Skaters));

        // 5 Minuten laufen fünf Minuten.
        for (var i = 0; i < 4; i++)
            Assert.Equal(1, box.Tick());
        Assert.Equal(0, box.Tick());
        Assert.Equal((0, 5), (box.State, box.Skaters));
    }

    [Fact]
    public void PenaltyBox_SecondSlotMovesUpWhenTheFirstEnds()
    {
        var box = new PenaltyBox();
        box.Add(0, 2);
        box.Tick(); // Platz 0: 1
        box.Add(1, 2);
        Assert.Equal(3, box.Skaters);
        // Platz 0 läuft ab (Spieler zurück), Platz 1 (2 → 1) rückt nach Platz 0.
        Assert.Equal(1, box.Tick());
        Assert.Equal((1, 4), (box.State, box.Skaters));
        Assert.Equal(0, box.Tick());
        Assert.Equal((0, 5), (box.State, box.Skaters));
    }

    [Fact]
    public void PenaltyBox_GoalWithTwoOrdinaryPenalties_FreesTheSecondSlotLikeTheOriginal()
    {
        var box = new PenaltyBox();
        box.Add(0, 2);
        box.Add(1, 2);
        box.Release(0, force: false);
        Assert.Equal((2, 4), (box.State, box.Skaters));
    }

    [Fact]
    public void Shot_HomeFollowsTheOriginalDrawOrder()
    {
        // Summe 600 < lrandom(550, 850) = 700 → schwach (w = 1); lrandom(0, 6) = 2 > 1; d = (210 − 150) / 3 = 20 →
        // lrandom(0, 20/4 + 1 + 2) = 3 ≠ 0 → Tor.
        var random = new ScriptedRandom(700, 2, 3);
        Assert.True(OriginalMinutes.Shot(150, 210, 150, 90, awayShoots: false, 0, 0, 10, false, false, random));
        Assert.Equal([(550, 850), (0, 6), (0, 8)], random.Calls);

        // lrandom(0, 4) = 1 → daneben (stark, w = 0).
        random = new ScriptedRandom(550, 1);
        Assert.False(OriginalMinutes.Shot(150, 210, 150, 90, awayShoots: false, 0, 0, 10, false, false, random));
        Assert.True(random.Exhausted);
    }

    [Fact]
    public void Shot_AwayAgainstStrongerDefense_HasTwoWaysIn()
    {
        // Gast: lrandom(0, 3 + 0 + 1) = 2 > WM 1; d = (90 − 150) / 3 = −20 → lrandom(0, 6) = 1 ≠ 0, dann lrandom(0, 6) = 0 → Tor.
        var random = new ScriptedRandom(550, 2, 1, 0);
        Assert.True(OriginalMinutes.Shot(150, 210, 150, 90, awayShoots: true, 0, 0, 10, true, false, random));
        Assert.Equal([(550, 850), (0, 4), (0, 6), (0, 6)], random.Calls);

        // lrandom(0, 4) = 1 ≤ WM → daneben, aber leeres Tor des Gegners.
        random = new ScriptedRandom(550, 1);
        Assert.True(OriginalMinutes.Shot(150, 210, 150, 90, awayShoots: true, 0, 0, 10, true, defenderOpenNet: true, random));
    }

    [Fact]
    public void Shot_OvertimeOnlyWithMarkAndCapAt29()
    {
        Assert.False(OriginalMinutes.Shot(150, 150, 150, 150, false, 3, 3, 62, false, false, new ScriptedRandom(600)));
        Assert.False(OriginalMinutes.Shot(150, 150, 150, 150, false, 29, 0, 20, false, false, new ScriptedRandom(600)));
    }

    [Fact]
    public void Penalty_ChainsToTheOpponentWithOneInFour()
    {
        var club = Team();
        var opponent = Team();
        var events = new List<MatchEvent>();
        // Eigene Strafe: lrandom(0, 12) = 0, Platz 0 frei: lrandom(0, 15) = 3 → 2 Minuten; Kette lrandom(0, 3) = 0;
        // Gegner: lrandom(0, 12) = 5, lrandom(0, 50) = 0 → Strafe, lrandom(0, 15) = 0 → 5 Minuten (kein weiteres Glied).
        var random = new ScriptedRandom(0, 3, 0, 5, 0, 0);
        Assert.Equal(2, OriginalMinutes.Penalty(club, opponent, 0, 7, chained: false, events, random));
        Assert.True(random.Exhausted);
        Assert.Equal([new MatchEvent(7, 0, MatchEventKind.Penalty, 2), new MatchEvent(7, 1, MatchEventKind.Penalty, 5)], events);
        Assert.Equal((2, 13), (club.Box.State, opponent.Box.State));
        Assert.Equal((4, 4), (club.Box.Skaters, opponent.Box.Skaters));
    }

    [Fact]
    public void Penalty_FullBox_NoFurtherPenalty()
    {
        var club = Team();
        club.Box.Add(0, 13);
        club.Box.Add(1, 2);
        // Nach dem Herunterzählen noch zwei Plätze belegt → kein Zug.
        var random = new ScriptedRandom();
        Assert.Equal(0, OriginalMinutes.Penalty(club, Team(), 0, 7, false, [], random));
        Assert.Empty(random.Calls);
    }

    [Fact]
    public void Play_EventsAreConsistent_AndInTheRangeOfIceHockey()
    {
        var random = new Random(17);
        const int matches = 2000;
        int goals = 0, home = 0, penaltyMinutes = 0, draws = 0;
        for (var i = 0; i < matches; i++)
        {
            var h = Team();
            var a = Team();
            var report = OriginalMinutes.Play(h, a, new MatchRules(), random);
            Assert.False(report.Overtime);
            Assert.Equal(report.HomeGoals, report.Events.Count(e => e.Kind == MatchEventKind.Goal && e.Side == 0));
            Assert.Equal(report.AwayGoals, report.Events.Count(e => e.Kind == MatchEventKind.Goal && e.Side == 1));
            Assert.All(report.Events, e => Assert.InRange(e.Minute, 1, 60));
            Assert.DoesNotContain(report.Events, e => e.Kind == MatchEventKind.Penalty && e.Minute == 60);
            Assert.Equal(report.HomePenaltyMinutes, report.Events.Where(e => e.Side == 0).Sum(e => e.PenaltyMinutes));
            Assert.InRange(h.Box.Skaters, 3, 5);
            Assert.InRange(a.Box.Skaters, 3, 5);
            goals += report.HomeGoals + report.AwayGoals;
            home += report.HomeGoals;
            penaltyMinutes += report.HomePenaltyMinutes + report.AwayPenaltyMinutes;
            draws += report.HomeGoals == report.AwayGoals ? 1 : 0;
        }
        var perGame = goals / (double)matches;
        Assert.InRange(perGame, 4.0, 9.0);
        Assert.True(home > goals - home, "Heimvorteil des Originals");
        Assert.InRange(penaltyMinutes / (double)matches, 10.0, 40.0);
        Assert.InRange(draws / (double)matches, 0.03, 0.25);
    }

    [Fact]
    public void Play_KnockOut_OvertimeOnlyAfterADraw()
    {
        var random = new Random(23);
        var overtimes = 0;
        for (var i = 0; i < 500; i++)
        {
            var report = OriginalMinutes.Play(Team(), Team(), new MatchRules(KnockOut: true), random);
            var regular = report.Events.Where(e => e.Kind == MatchEventKind.Goal && e.Minute <= 60).ToList();
            var regularHome = regular.Count(e => e.Side == 0);
            Assert.Equal(report.Overtime, regularHome == regular.Count - regularHome);
            if (!report.Overtime)
                Assert.DoesNotContain(report.Events, e => e.Minute > 60);
            else
            {
                overtimes++;
                Assert.DoesNotContain(report.Events, e => e.Kind == MatchEventKind.Penalty && e.Minute == 70);
            }
        }
        Assert.True(overtimes > 0);
    }

    [Fact]
    public void Play_StrongerTeamWinsMoreOften()
    {
        var random = new Random(29);
        int strongWins = 0, weakWins = 0;
        for (var i = 0; i < 1000; i++)
        {
            var r = OriginalMinutes.Play(Team(40), Team(80), new MatchRules(), random);
            strongWins += r.AwayGoals > r.HomeGoals ? 1 : 0;
            weakWins += r.HomeGoals > r.AwayGoals ? 1 : 0;
        }
        Assert.True(strongWins > 2 * weakWins, $"{strongWins} : {weakWins}");
    }

    [Fact]
    public void FastCupResult_FollowsTheOriginalDrawOrder()
    {
        // 0x9F5C wie in FullResult_EqualTeams (9 Züge) → 4:2; Glättung: lrandom(0, 3) = 1 (Heim bleibt), lrandom(0, 3) = 0 →
        // Gast + lrandom(0, 4) = 2; k = 0 → lrandom(0, 0); Gleichstand 4:4 → random(0, 2) je Seite bis ungleich.
        var random = new ScriptedRandom(0, 0, 1, 2, 1, 2, 0, 1, 0, 1, 0, 2, 0, 1, 1, 2, 0);
        var result = OriginalMinutes.FastCupResult(new MatchSide(150), new MatchSide(150), true, true, false, false, knockOut: true, random);
        Assert.Equal((6, 4, true), result);
        Assert.True(random.Exhausted);
        Assert.Equal([(0, 3), (0, 3), (0, 4), (0, 0), (0, 2), (0, 2), (0, 2), (0, 2)], random.Calls[9..]);
    }

    [Fact]
    public void FastCupResult_StrongerTeamUsuallyAhead_NoDrawInKnockOut()
    {
        var random = new Random(31);
        int strong = 0, weak = 0;
        for (var i = 0; i < 2000; i++)
        {
            var (h, a, shootout) = OriginalMinutes.FastCupResult(new MatchSide(120), new MatchSide(200), true, true, false, false, true, random);
            Assert.NotEqual(h, a);
            Assert.InRange(h, 0, 29);
            strong += a > h ? 1 : 0;
            weak += h > a ? 1 : 0;
            _ = shootout;
        }
        Assert.True(strong > 4 * weak, $"{strong} : {weak}");
    }
}
