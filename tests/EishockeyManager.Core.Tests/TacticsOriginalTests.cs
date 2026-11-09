using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur taktik-verein, Befund (a) aus #103: Wertebereich, Bedeutung und Vorbelegung der Taktik nach em.exe
/// (Belege in <see cref="Tactics"/>). Vor dem Fix: Werte −3…+3, alles 0.
/// </summary>
public class TacticsOriginalTests
{
    private static readonly ScoreSituation[] Rows = Enum.GetValues<ScoreSituation>();

    /// <summary>
    /// Video `bundestrainer_00058`/`00059` (AUTOM.): Einsatz 60/0/25/20/30/0, Angriff 0/4/5/6/7/4 – die Startwerte vor
    /// Runde 14 (<see cref="Tactics.LegacyStart"/>). Seit A180 (kader-start Runde 14) startet ein Verein mit 0 (FILE29).
    /// </summary>
    [Fact]
    public void Defaults_ShowEvenMatchRowOfOriginal()
    {
        Assert.All(Enumerable.Range(0, Tactics.Blocks), b => Assert.All(Rows, r =>
        {
            Assert.Equal(0, new Tactics().Effort(b, r));
            Assert.Equal(0, new Tactics().Attack(b, r));
        }));
        var t = Tactics.LegacyStart();
        int[] effort = [60, 0, 25, 20, 30, 0];
        int[] attack = [0, 4, 5, 6, 7, 4];
        for (var b = 0; b < Tactics.Blocks; b++)
        {
            Assert.Equal(effort, Rows.Select(r => t.Effort(b, r)));
            Assert.Equal(attack, Rows.Select(r => t.Attack(b, r)));
        }
    }

    [Fact]
    public void EffortIsPercent_AttackUpToSeven_BaseAndSurchargeShareTheLimit()
    {
        var t = Tactics.LegacyStart();
        t.SetEffort(0, ScoreSituation.Base, 100);
        Assert.Equal(70, t.Effort(0, ScoreSituation.Base)); // 100 − größter Zuschlag (30)
        t.SetEffort(0, ScoreSituation.Tied, 100);
        Assert.Equal(30, t.Effort(0, ScoreSituation.Tied)); // 100 − Grundwert 70
        t.SetEffort(0, ScoreSituation.Tied, -5);
        Assert.Equal(0, t.Effort(0, ScoreSituation.Tied));

        t.SetAttack(1, ScoreSituation.HighLead, 9);
        Assert.Equal(7, t.Attack(1, ScoreSituation.HighLead)); // Grundwert 0
        t.SetAttack(1, ScoreSituation.Base, 7);
        Assert.Equal(0, t.Attack(1, ScoreSituation.Base)); // größter Zuschlag 7
    }

    /// <summary>Datei 0x4896C: Der Grundwert prüft nur „hohe Führung“ bis „knapper Rückstand“.</summary>
    [Fact]
    public void BaseLimit_IgnoresHighDeficit_LikeOriginal()
    {
        var t = new Tactics();
        for (var r = 1; r < 5; r++)
            t.SetEffort(2, Rows[r], 0);
        t.SetEffort(2, ScoreSituation.HighDeficit, 0);
        t.SetEffort(2, ScoreSituation.Base, 100);
        t.SetEffort(2, ScoreSituation.HighDeficit, 50);
        Assert.Equal(0, t.Effort(2, ScoreSituation.HighDeficit));
        t.SetEffort(2, ScoreSituation.Base, 40);
        t.SetEffort(2, ScoreSituation.HighDeficit, 60);
        t.SetEffort(2, ScoreSituation.Base, 100);
        Assert.Equal(100, t.Effort(2, ScoreSituation.Base));
        t.EffortMode = TacticMode.Semi;
        Assert.Equal(100, t.EffectiveEffort(2, -5)); // 160 → auf 100 begrenzt
    }

    [Theory]
    [InlineData(50, 50, 60, 0)] // gleich stark
    [InlineData(50, 62, 60, 0)] // 2 · 12 = 24 < 25
    [InlineData(50, 63, 40, 20)] // 26
    [InlineData(80, 50, 40, 20)]
    public void AutoEffort_DependsOnStrengthGap(int own, int opponent, int expectedBase, int expectedHighLead)
    {
        Assert.Equal(expectedBase, Tactics.AutoEffort(own, opponent, ScoreSituation.Base));
        Assert.Equal(expectedHighLead, Tactics.AutoEffort(own, opponent, ScoreSituation.HighLead));
    }

    [Fact]
    public void AutoAttack_UnderdogAndFavourite()
    {
        Assert.Equal([0, 5, 6, 4, 4, 0], Rows.Select(r => Tactics.AutoAttack(40, 60, r)));
        Assert.Equal([0, 4, 5, 6, 7, 4], Rows.Select(r => Tactics.AutoAttack(60, 60, r)));
        Assert.Equal([0, 4, 5, 6, 7, 4], Rows.Select(r => Tactics.AutoAttack(70, 60, r)));
    }

    [Theory]
    [InlineData(4, 60)] // hohe Führung: 60 + 0
    [InlineData(1, 85)]
    [InlineData(0, 80)]
    [InlineData(-2, 90)]
    [InlineData(-3, 60)]
    public void EffectiveEffort_IsBasePlusSituation(int goalDifference, int expected)
    {
        var t = Tactics.LegacyStart();
        t.EffortMode = TacticMode.Semi;
        Assert.Equal(expected, t.EffectiveEffort(3, goalDifference));
        t.EffortMode = TacticMode.Auto;
        Assert.Equal(expected, t.EffectiveEffort(3, goalDifference, 50, 50));
    }

    [Fact]
    public void EffectiveAttack_KeepsThreeBits()
    {
        var t = Tactics.LegacyStart();
        t.AttackMode = TacticMode.Semi;
        Assert.Equal(6, t.EffectiveAttack(0, 0));
        for (var r = 1; r < 5; r++)
            t.SetAttack(0, Rows[r], 0);
        t.SetAttack(0, ScoreSituation.HighDeficit, 7); // begrenzt nur durch 7 − Grundwert
        t.SetAttack(0, ScoreSituation.Base, 3); // Grenze ignoriert „hoher Rückstand“ (Datei 0x4896C)
        Assert.Equal(3, t.Attack(0, ScoreSituation.Base));
        Assert.Equal((3 + 7) & 7, t.EffectiveAttack(0, -4));
        t.AttackMode = TacticMode.Auto;
        Assert.Equal(4, t.EffectiveAttack(0, -2, 40, 60)); // Schwächerer, knapper Rückstand: 0 + 4
    }

    [Fact]
    public void OnlySemiIsEditable()
    {
        Assert.False(Tactics.Editable(TacticMode.Auto));
        Assert.True(Tactics.Editable(TacticMode.Semi));
        Assert.False(Tactics.Editable(TacticMode.Manual));
    }

    // Abnahme (3) „Einsatz verschiebt die Stärke um höchstens ±3“ entfällt (A178/A182, Runde 11): der Einsatz wirkt nur in
    // der Spielberechnung, siehe TaktikTeil5Tests.Effort_DoesNotShiftMatchStrength.
}

