using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Review-Befunde der Integration Runde 7 (<c>docs/reviews/2026-10-30-integration-runde-7.md</c>), unabhängig von den
/// Konstanten des Produktcodes.
/// </summary>
public class IntegrationRunde7ReviewTests
{
    /// <summary>Gibt je Aufruf die untere Grenze zurück und merkt sich die angeforderten inklusiven Grenzen.</summary>
    private sealed class LowerBoundRandom : Random
    {
        public List<(int Min, int Max)> Calls { get; } = [];

        public override int Next(int minValue, int maxValue)
        {
            Calls.Add((minValue, maxValue - 1));
            return minValue;
        }

        public override int Next(int maxValue) => Next(0, maxValue);
        public override int Next() => 0;
        public override double NextDouble() => 0;
    }

    /// <summary>
    /// R7-02 (#200): [ORIGINAL] em.exe Datei 0x9F92–0xA019 ruft lrandom (Datei 0xBFD5, 0x99F:0x7E5) mit
    /// (Angriff, 7) – das Angriffsbitfeld ist die untere, 7 die obere Grenze. Vorher zog das Remake random(0, Angriff).
    /// </summary>
    [Fact]
    public void AttackBonus_DrawsFromAttackLevelToSeven()
    {
        var random = new LowerBoundRandom();
        OriginalMatch.FullResult(new MatchSide(150, AttackLevel: 7), new MatchSide(150, AttackLevel: 3), false, false, random);
        Assert.Equal((7, 7), random.Calls[0]);
        Assert.Equal((3, 7), random.Calls[1]);
    }

    /// <summary>R7-02: Bei Angriff 7 ist der Zuschlag immer 2 (lrandom(7, 7) − 5), bei Angriff 0 höchstens 2.</summary>
    [Theory]
    [InlineData(7, 2, 2)]
    [InlineData(5, 0, 2)]
    [InlineData(0, 0, 2)]
    public void AttackBonus_RangeMatchesOriginal(int attack, int min, int max)
    {
        for (var seed = 0; seed < 200; seed++)
        {
            var random = new Random(seed);
            var draw = random.Next(attack, 8);
            var bonus = Math.Max(0, draw - 5);
            Assert.InRange(bonus, min, max);
        }
        var low = new LowerBoundRandom();
        OriginalMatch.FullResult(new MatchSide(150, AttackLevel: attack), new MatchSide(150), false, false, low);
        Assert.Equal(attack, low.Calls[0].Min);
        Assert.Equal(7, low.Calls[0].Max);
    }
}
