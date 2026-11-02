namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur <c>kader-transfer</c>, Teil 6: Proben aus den Reviews der Runde 9 (#347, R9T-P3-01). Mutationen, die bisher ohne roten Test
/// blieben, bekommen eine Probe mit festem Zufall.
/// </summary>
public class KaderTransferTeil6Tests
{
    /// <summary>random(a, b) liefert der Reihe nach die vorgegebenen Werte und merkt sich die Grenzen.</summary>
    private sealed class Sequence(params int[] values) : Random
    {
        private int _next;
        public List<(int Min, int Max)> Calls { get; } = [];
        public override int Next(int minValue, int maxValue)
        {
            Calls.Add((minValue, maxValue - 1));
            return values[_next++];
        }
        public override int Next(int maxValue) => Next(0, maxValue);
        public override int Next() => Next(0, int.MaxValue);
        public override double NextDouble() => 0;
    }

    /// <summary>
    /// [ORIGINAL] Datei 0x495D8–0x49628: Index random(1, 179), mit random(0, 5) = 0 stattdessen random(180, 199) – ein Sechstel
    /// Nationalspieler. Vorher grün auch mit random(0, 4) (Mutation aus R9T-P3-01): kein Test hielt die Grenzen fest.
    /// </summary>
    [Fact]
    public void DrawIndex_NationalShareIsOneInSix()
    {
        var league = new Sequence(17, 1);
        Assert.Equal(17, TransferPoolRules.DrawIndex(league));
        Assert.Equal([(1, 179), (0, 5)], league.Calls);

        var national = new Sequence(17, 0, 185);
        Assert.Equal(185, TransferPoolRules.DrawIndex(national));
        Assert.Equal([(1, 179), (0, 5), (180, 199)], national.Calls);
    }
}
