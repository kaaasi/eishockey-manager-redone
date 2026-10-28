using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur „speichern“, Teil „rng“, Abnahme 1: <see cref="GameRandom"/> liefert dieselbe Folge wie
/// <see cref="System.Random"/> mit Seed und lässt sich sichern und fortsetzen.
/// </summary>
public class RngTests
{
    private const int Count = 10_000;

    public static TheoryData<int> Seeds => [0, 1, 3, 11, 42, -7, 161_803_398, int.MaxValue, int.MinValue, 2026];

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Next_MatchesSystemRandom(int seed)
    {
        var expected = new Random(seed);
        var actual = new GameRandom(seed);
        for (var i = 0; i < Count; i++)
            Assert.Equal(expected.Next(), actual.Next());
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void NextRange_MatchesSystemRandom(int seed)
    {
        var expected = new Random(seed);
        var actual = new GameRandom(seed);
        (int Min, int Max)[] ranges = [(0, 1), (0, 6), (1, 101), (-50, 50), (17, 17), (int.MinValue, int.MaxValue), (-1, int.MaxValue)];
        for (var i = 0; i < Count; i++)
        {
            var (min, max) = ranges[i % ranges.Length];
            Assert.Equal(expected.Next(min, max), actual.Next(min, max));
            Assert.Equal(expected.Next(max - min < 0 ? 100 : (int)Math.Min(max - (long)min, int.MaxValue)),
                         actual.Next(max - min < 0 ? 100 : (int)Math.Min(max - (long)min, int.MaxValue)));
        }
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void NextDouble_MatchesSystemRandom(int seed)
    {
        var expected = new Random(seed);
        var actual = new GameRandom(seed);
        for (var i = 0; i < Count; i++)
            Assert.Equal(expected.NextDouble(), actual.NextDouble());
    }

    /// <summary>Selten genutzte Methoden: auch sie dürfen die Folge nicht verschieben.</summary>
    [Theory]
    [MemberData(nameof(Seeds))]
    public void OtherMethods_MatchSystemRandom(int seed)
    {
        var expected = new Random(seed);
        var actual = new GameRandom(seed);
        for (var i = 0; i < 500; i++)
        {
            Assert.Equal(expected.NextSingle(), actual.NextSingle());
            Assert.Equal(expected.NextInt64(), actual.NextInt64());
            Assert.Equal(expected.NextInt64(1_000_000_007L), actual.NextInt64(1_000_000_007L));
            Assert.Equal(expected.NextInt64(-5L, long.MaxValue), actual.NextInt64(-5L, long.MaxValue));
            var a = new byte[7];
            var b = new byte[7];
            expected.NextBytes(a);
            actual.NextBytes(b);
            Assert.Equal(a, b);
            int[] shuffledA = [1, 2, 3, 4, 5, 6, 7, 8];
            int[] shuffledB = [1, 2, 3, 4, 5, 6, 7, 8];
            expected.Shuffle(shuffledA);
            actual.Shuffle(shuffledB);
            Assert.Equal(shuffledA, shuffledB);
        }
        Assert.Equal(expected.Next(), actual.Next());
    }

    [Fact]
    public void InvalidArguments_ThrowLikeSystemRandom()
    {
        var random = new GameRandom(1);
        Assert.Throws<ArgumentOutOfRangeException>(() => random.Next(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => random.Next(5, 4));
        Assert.Throws<ArgumentOutOfRangeException>(() => random.NextInt64(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => random.NextInt64(5, 4));
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void CaptureRestore_ContinuesIdentically(int seed)
    {
        var reference = new Random(seed);
        var random = new GameRandom(seed);
        for (var i = 0; i < 1234; i++)
            Assert.Equal(reference.Next(), random.Next());

        var state = random.Capture();
        var copy = GameRandom.FromState(state);
        var values = Enumerable.Range(0, Count).Select(_ => random.Next(0, 1000)).ToArray();

        Assert.Equal(values, Enumerable.Range(0, Count).Select(_ => copy.Next(0, 1000)));
        Assert.Equal(values, Enumerable.Range(0, Count).Select(_ => reference.Next(0, 1000)));

        random.Restore(state); // dieselbe Instanz zurücksetzen
        Assert.Equal(values, Enumerable.Range(0, Count).Select(_ => random.Next(0, 1000)));
        Assert.Equal(state, GameRandom.FromState(state).Capture());
    }

    [Fact]
    public void Capture_IsACopy()
    {
        var random = new GameRandom(5);
        var state = random.Capture();
        random.Next();
        Assert.NotEqual(state, random.Capture());
        Assert.Equal(state, GameRandom.FromState(state).Capture());
    }

    [Fact]
    public void InvalidState_IsRejected()
    {
        Assert.Throws<ArgumentException>(() => new RngState(new int[55], 0, 21));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RngState(new int[56], 56, 21));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RngState(new int[56], 0, -1));
    }

    /// <summary>
    /// Spielstand: Abbild aller vier Quellen sichern, weiterspielen, zurücksetzen – die Folgen wiederholen sich.
    /// Noch nicht erzeugte Quellen bleiben „nicht erzeugt“ und entstehen danach wie bisher aus dem Saison-Seed.
    /// </summary>
    [Fact]
    public void GameState_CaptureRestore_RepeatsAllSources_AndKeepsLazyCreation()
    {
        // „Noch nicht erzeugt“ (je nach Spielstart schon belegt) wird über ein Abbild ohne die drei Quellen hergestellt.
        var fresh = TestGames.Create(7);
        var full = fresh.CaptureRng();
        Assert.Equal(full, fresh.CaptureRng()); // Abbild verbraucht nichts
        var lazy = full with { Finance = null, Cup = null, Contract = null };
        fresh.RestoreRng(lazy);
        Assert.Equal(lazy, fresh.CaptureRng()); // bleibt „nicht erzeugt“, bis darauf zugegriffen wird

        var expected = Draw(fresh); // erzeugt die drei Quellen neu aus dem Saison-Seed
        var twin = TestGames.Create(7);
        twin.RestoreRng(lazy);
        Assert.Equal(expected, Draw(twin));

        var game = TestGames.Create(7);
        for (var i = 0; i < 3; i++)
            TestSeason.PlayMatchDay(game);
        var snapshot = game.CaptureRng();
        Assert.NotNull(snapshot.Finance);
        var first = Draw(game);
        game.RestoreRng(snapshot);
        Assert.Equal(first, Draw(game));
    }

    /// <summary>Zwei Spiele: eines am Spieltag 5 auf das Zufallsabbild eines Zwillings gesetzt, danach gleicher Verlauf.</summary>
    [Fact]
    public void GameState_RestoredRng_TwinPlaysIdentically()
    {
        var a = TestGames.Create(5);
        var b = TestGames.Create(5);
        for (var i = 0; i < 5; i++)
        {
            TestSeason.PlayMatchDay(a);
            TestSeason.PlayMatchDay(b);
        }
        var snapshot = a.CaptureRng();
        a.Random.Next(); // a weicht ab …
        a.RestoreRng(snapshot); // … und wird zurückgesetzt
        for (var i = 0; i < 10; i++)
        {
            TestSeason.PlayMatchDay(a);
            TestSeason.PlayMatchDay(b);
        }
        Assert.Equal(b.CaptureRng(), a.CaptureRng());
        Assert.Equal(b.Managers.Select(m => m.Money), a.Managers.Select(m => m.Money));
    }

    private static int[] Draw(GameState game) =>
        [game.Random.Next(), game.FinanceRandom.Next(), game.CupRandom.Next(), game.ContractRandom.Next(), game.Random.Next()];
}
