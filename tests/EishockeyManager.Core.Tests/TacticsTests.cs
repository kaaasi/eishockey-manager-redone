using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

public class TacticsTests
{
    [Theory]
    [InlineData(5, ScoreSituation.HighLead)]
    [InlineData(3, ScoreSituation.HighLead)]
    [InlineData(2, ScoreSituation.NarrowLead)]
    [InlineData(0, ScoreSituation.Tied)]
    [InlineData(-1, ScoreSituation.NarrowDeficit)]
    [InlineData(-3, ScoreSituation.HighDeficit)]
    public void Situation_FromGoalDifference(int difference, ScoreSituation expected) =>
        Assert.Equal(expected, Tactics.Situation(difference));

    [Fact]
    public void Defaults_AreAutomatic()
    {
        var t = new Tactics();
        Assert.Equal(GoalieMode.Auto, t.Goalie);
        Assert.Equal(LineChangeMode.Automatic, t.LineChange);
        Assert.Equal(TacticMode.Auto, t.EffortMode);
        Assert.Equal(TacticMode.Auto, t.AttackMode);
        Assert.Equal(0, t.Minutes(3, forwards: true));
        Assert.False(t.MinutesActive);
    }

    [Fact]
    public void Minutes_AreClamped()
    {
        var t = new Tactics();
        t.SetMinutes(0, false, 100);
        Assert.Equal(Tactics.MaxMinutesPerPart, t.Minutes(0, false));
        t.SetMinutes(1, true, -1);
        Assert.Equal(0, t.Minutes(1, true));
    }
}
