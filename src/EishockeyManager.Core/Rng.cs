namespace EishockeyManager.Core;

/// <summary>
/// Gesicherter Zustand eines <see cref="GameRandom"/>: die 56 Felder der subtraktiven Folge und die beiden Zeiger.
/// Unveränderlich; <see cref="GameRandom.Capture"/> legt eine Kopie an.
/// </summary>
public sealed record RngState
{
    public const int Length = 56;

    public RngState(IReadOnlyList<int> seedArray, int next, int nextP)
    {
        ArgumentNullException.ThrowIfNull(seedArray);
        if (seedArray.Count != Length)
            throw new ArgumentException($"Der Zufallszustand braucht {Length} Werte, nicht {seedArray.Count}.", nameof(seedArray));
        ArgumentOutOfRangeException.ThrowIfNegative(next);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(next, Length);
        ArgumentOutOfRangeException.ThrowIfNegative(nextP);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(nextP, Length);
        SeedArray = [.. seedArray];
        Next = next;
        NextP = nextP;
    }

    /// <summary>Die 56 Felder (Index 0 bleibt wie bei <see cref="System.Random"/> unbenutzt).</summary>
    public IReadOnlyList<int> SeedArray { get; }
    public int Next { get; }
    public int NextP { get; }

    public bool Equals(RngState? other) =>
        other is not null && Next == other.Next && NextP == other.NextP && SeedArray.SequenceEqual(other.SeedArray);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Next);
        hash.Add(NextP);
        foreach (var value in SeedArray)
            hash.Add(value);
        return hash.ToHashCode();
    }
}

/// <summary>
/// Serialisierbarer Zufallsgenerator des Spielstands (Spur „speichern“, Teil „rng“).
/// Liefert für denselben Seed <b>dieselbe Zahlenfolge</b> wie <see cref="System.Random"/> mit Seed – dessen
/// subtraktiven Generator nach Knuth (in .NET „Net5CompatSeedImpl“) –, damit alle bisherigen Spielverläufe und
/// Mehrsaison-Referenzwerte gleich bleiben. Anders als <see cref="System.Random"/> lässt sich der Zustand mit
/// <see cref="Capture"/> sichern und mit <see cref="Restore"/> bzw. <see cref="FromState"/> wiederherstellen.
/// Abgeleitet von <see cref="System.Random"/>, damit alle Regeln, die einen <see cref="System.Random"/> erwarten,
/// unverändert bleiben; jede öffentliche Methode ist überschrieben, der Basiszustand wird nie benutzt.
/// </summary>
public sealed class GameRandom : Random
{
    private const int Seed = 161_803_398;

    private readonly int[] _seedArray = new int[RngState.Length];
    private int _next;
    private int _nextP;

    /// <summary>
    /// Ohne Seed: zufälliger Seed. [APPROX] Ein ungeseedeter <see cref="System.Random"/> nutzt in .NET einen anderen
    /// Algorithmus; da dessen Folge ohnehin nicht wiederholbar ist, genügt hier ein zufälliger Seed.
    /// </summary>
    public GameRandom() : this(Shared.Next())
    {
    }

    public GameRandom(int seed) : base(0)
    {
        // Wie System.Random (Net5CompatSeedImpl / CompatPrng): Knuth, subtraktive Methode.
        var subtraction = seed == int.MinValue ? int.MaxValue : Math.Abs(seed);
        var mj = Seed - subtraction;
        _seedArray[55] = mj;
        var mk = 1;
        var ii = 0;
        for (var i = 1; i < 55; i++)
        {
            if ((ii += 21) >= 55)
                ii -= 55;
            _seedArray[ii] = mk;
            mk = mj - mk;
            if (mk < 0)
                mk += int.MaxValue;
            mj = _seedArray[ii];
        }

        for (var k = 1; k < 5; k++)
        {
            for (var i = 1; i < 56; i++)
            {
                var n = i + 30;
                if (n >= 55)
                    n -= 55;
                _seedArray[i] -= _seedArray[1 + n];
                if (_seedArray[i] < 0)
                    _seedArray[i] += int.MaxValue;
            }
        }

        _next = 0;
        _nextP = 21;
    }

    private GameRandom(RngState state) : base(0) => Restore(state);

    /// <summary>Neuer Generator mit dem gesicherten Zustand.</summary>
    public static GameRandom FromState(RngState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return new GameRandom(state);
    }

    /// <summary>Kopie des aktuellen Zustands.</summary>
    public RngState Capture() => new(_seedArray, _next, _nextP);

    /// <summary>Setzt diesen Generator auf den gesicherten Zustand; die Folge setzt sich danach identisch fort.</summary>
    public void Restore(RngState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        for (var i = 0; i < RngState.Length; i++)
            _seedArray[i] = state.SeedArray[i];
        _next = state.Next;
        _nextP = state.NextP;
    }

    private int InternalSample()
    {
        var next = _next;
        if (++next >= 56)
            next = 1;
        var nextP = _nextP;
        if (++nextP >= 56)
            nextP = 1;

        var value = _seedArray[next] - _seedArray[nextP];
        if (value == int.MaxValue)
            value--;
        if (value < 0)
            value += int.MaxValue;

        _seedArray[next] = value;
        _next = next;
        _nextP = nextP;
        return value;
    }

    protected override double Sample() => InternalSample() * (1.0 / int.MaxValue);

    private double SampleForLargeRange()
    {
        var result = InternalSample();
        if (InternalSample() % 2 == 0)
            result = -result;
        double d = result;
        d += int.MaxValue - 1;
        d /= 2 * (uint)int.MaxValue - 1;
        return d;
    }

    public override int Next() => InternalSample();

    public override int Next(int maxValue)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxValue);
        return (int)(Sample() * maxValue);
    }

    public override int Next(int minValue, int maxValue)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(minValue, maxValue);
        var range = (long)maxValue - minValue;
        return range <= int.MaxValue
            ? (int)(Sample() * range) + minValue
            : (int)((long)(SampleForLargeRange() * range) + minValue);
    }

    public override double NextDouble() => Sample();

    public override float NextSingle() => (float)Sample();

    public override long NextInt64()
    {
        while (true)
        {
            var result = NextUInt64() >> 1;
            if (result != long.MaxValue)
                return (long)result;
        }
    }

    public override long NextInt64(long maxValue)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxValue);
        if (maxValue > 1)
        {
            var bits = Log2Ceiling((ulong)maxValue);
            while (true)
            {
                var result = NextUInt64() >> (64 - bits);
                if (result < (ulong)maxValue)
                    return (long)result;
            }
        }
        return 0;
    }

    public override long NextInt64(long minValue, long maxValue)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(minValue, maxValue);
        var range = (ulong)(maxValue - minValue);
        if (range > 1)
        {
            var bits = Log2Ceiling(range);
            while (true)
            {
                var result = NextUInt64() >> (64 - bits);
                if (result < range)
                    return (long)result + minValue;
            }
        }
        return minValue;
    }

    public override void NextBytes(byte[] buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        NextBytes(buffer.AsSpan());
    }

    public override void NextBytes(Span<byte> buffer)
    {
        for (var i = 0; i < buffer.Length; i++)
            buffer[i] = (byte)InternalSample();
    }

    private ulong NextUInt64() =>
        (ulong)(uint)Next(1 << 22) | ((ulong)(uint)Next(1 << 22) << 22) | ((ulong)(uint)Next(1 << 20) << 44);

    private static int Log2Ceiling(ulong value)
    {
        var result = System.Numerics.BitOperations.Log2(value);
        return System.Numerics.BitOperations.PopCount(value) != 1 ? result + 1 : result;
    }
}
