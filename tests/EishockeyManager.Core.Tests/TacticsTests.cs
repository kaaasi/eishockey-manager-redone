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
    public void Defaults_AreAutomaticAndZero()
    {
        var t = new Tactics();
        Assert.Equal(GoalieMode.Auto, t.Goalie);
        Assert.Equal(LineChangeMode.Automatic, t.LineChange);
        Assert.Equal(TacticMode.Auto, t.EffortMode);
        Assert.Equal(0, t.Effort(0, ScoreSituation.Base));
        Assert.Equal(0, t.Minutes(3, forwards: true));
        Assert.False(t.MinutesActive);
        Assert.Equal(0, t.EffortStrengthBonus());
    }

    [Fact]
    public void Values_AreClamped()
    {
        var t = new Tactics();
        t.SetEffort(1, ScoreSituation.Tied, 99);
        t.SetAttack(2, ScoreSituation.HighLead, -99);
        t.SetMinutes(0, false, 100);
        Assert.Equal(Tactics.MaxValue, t.Effort(1, ScoreSituation.Tied));
        Assert.Equal(Tactics.MinValue, t.Attack(2, ScoreSituation.HighLead));
        Assert.Equal(Tactics.MaxMinutes, t.Minutes(0, false));
    }

    [Fact]
    public void Manual_AddsSituationToBase_Auto_IgnoresOwnValues()
    {
        var t = new Tactics { AttackMode = TacticMode.Manual };
        t.SetAttack(0, ScoreSituation.Base, 1);
        t.SetAttack(0, ScoreSituation.HighDeficit, 2);
        Assert.Equal(1, t.EffectiveAttack(0, 0));
        Assert.Equal(3, t.EffectiveAttack(0, -4));

        t.AttackMode = TacticMode.Auto;
        Assert.Equal(Tactics.AutoValue(ScoreSituation.HighDeficit), t.EffectiveAttack(0, -4));
    }

    [Fact]
    public void Semi_MovesOwnValueOneStepTowardsAutomatic()
    {
        var t = new Tactics { EffortMode = TacticMode.Semi };
        t.SetEffort(0, ScoreSituation.Base, -2);
        Assert.Equal(-1, t.EffectiveEffort(0, 0)); // Automatik will 0
        t.SetEffort(0, ScoreSituation.Base, 0);
        Assert.Equal(0, t.EffectiveEffort(0, 0));
    }

    [Fact]
    public void Effort_RaisesMatchStrength()
    {
        var game = TestGames.Create();
        var club = game.Clubs[2];
        var before = club.MatchStrength;
        club.Tactics.EffortMode = TacticMode.Manual;
        for (var b = 0; b < Tactics.Blocks; b++)
            club.Tactics.SetEffort(b, ScoreSituation.Base, Tactics.MaxValue);
        Assert.Equal(before + Tactics.MaxValue, club.MatchStrength);
    }
}
