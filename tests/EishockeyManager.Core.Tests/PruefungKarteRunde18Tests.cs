namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur pruefung-karte Teil 2 (Runde 18): gesammelte P3 #824 (lrandom an einer Stelle) und #829 (weitere aufeinanderfolgende
/// lrandom-Züge, <c>docs/analyse/abgleich-dosbox.md</c> §2.5).
/// </summary>
public class PruefungKarteRunde18Tests
{
    /// <summary>
    /// #824 (R814 P3-2): Der Generator der Live-Anzeige (<see cref="LRandom"/>) und der Zyklus der Strafwürfe
    /// (<see cref="LRandomCycle"/>) sind dieselbe Folge (Datei 0xBFD5); nach zwei Schritten liegt der Zustand im Zyklus.
    /// </summary>
    [Fact]
    public void LRandom_FollowsCycleStepAndValue()
    {
        var r = new LRandom();
        var x = 0;
        var ranges = new[] { (0, 12), (0, 15), (1, 20), (550, 850), (0, 3) };
        for (var i = 0; i < 2000; i++)
        {
            var (min, max) = ranges[i % ranges.Length];
            x = LRandomCycle.Step(x);
            Assert.Equal(LRandomCycle.Value(x, min, max), r.Next(min, max));
            Assert.Equal(x, r.State);
        }
        Assert.Equal(LRandomCycle.Modulus, LRandom.Modulus);
        Assert.Contains(r.State, LRandomCycle.Cycle);
    }

    private static (int Hit, int Total) CountOverCycle(Func<Func<int, int, int>, bool?> trial)
    {
        int hit = 0, total = 0;
        foreach (var start in LRandomCycle.Cycle)
        {
            var x = start;
            var result = trial((min, max) =>
            {
                x = LRandomCycle.Step(x);
                return LRandomCycle.Value(x, min, max);
            });
            if (result is null)
                continue;
            total++;
            hit += result.Value ? 1 : 0;
        }
        return (hit, total);
    }

    /// <summary>
    /// #829 (R812 P3-3), Kette: [ORIGINAL] Datei 0x358B7–0x35A0D: nach der Strafe d3 = lrandom(0, 15), dann ohne Zug dazwischen
    /// der Kettenzug lrandom(0, 3) = 0 und im Aufruf 0x35C8B für den Gegner wieder d1 = lrandom(0, 12), d2 = lrandom(0, 110 − Einsatz).
    /// Über den Zyklus folgt einer Kette bei Einsatz 40 in 41 von 360 Lagen eine Strafe (11,4 % statt 9,0 % unabhängig).
    /// Je Strafe sind das 2,9 statt 2,25 % Kettenstrafen, also etwa +0,7 % Strafen insgesamt – das Remake zieht unabhängig
    /// ([APPROX], kein Fix, Begründung im Bericht §2.5).
    /// </summary>
    [Fact]
    public void Chain_PenaltyAfterChainDraw_OverCycle()
    {
        const int effort = 40;
        var (chained, penalties) = CountOverCycle(draw =>
        {
            if (draw(0, 12) != 0 && draw(0, 110 - effort) != 0)
                return null;
            draw(0, 15);
            return draw(0, 3) == 0;
        });
        Assert.Equal(1394, penalties);
        Assert.Equal(360, chained);
        var (second, chains) = CountOverCycle(draw =>
        {
            if (draw(0, 12) != 0 && draw(0, 110 - effort) != 0)
                return null;
            draw(0, 15);
            if (draw(0, 3) != 0)
                return null;
            return draw(0, 12) == 0 || draw(0, 110 - effort) == 0;
        });
        Assert.Equal((41, 360), (second, chains));
    }

    /// <summary>
    /// #829, Chancen: [ORIGINAL] Datei 0xA387–0xA3EC: je Punkt von tH, dann je Punkt von tG lrandom(1, 20) = 1, ohne andere Züge
    /// dazwischen. Über den Zyklus weicht die Wahrscheinlichkeit einer Heim- bzw. Gastchance für tH = tG = 1 … 10 höchstens um
    /// 1,1 % (relativ) von der unabhängigen ab – keine Änderung nötig.
    /// </summary>
    [Fact]
    public void ChanceLoop_CouplingBelowOnePointTwoPercent()
    {
        for (var points = 1; points <= 10; points++)
        {
            var n = points;
            var (home, total) = CountOverCycle(draw =>
            {
                var hit = false;
                for (var i = 0; i < n; i++)
                    hit |= draw(1, 20) == 1;
                return hit;
            });
            var independent = 1 - Math.Pow(19.0 / 20, n);
            var share = (double)home / total;
            Assert.InRange(share / independent, 0.988, 1.012);
        }
    }
}
