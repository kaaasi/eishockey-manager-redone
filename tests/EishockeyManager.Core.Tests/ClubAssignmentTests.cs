using EishockeyManager.Tests.Support;
using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Befund R2-07 (A04): Feste und zufällige Vereinswahl dürfen keinen Verein doppelt vergeben.
/// </summary>
public class ClubAssignmentTests
{
    /// <summary>Art der Wahl eines Managers in den Testfällen.</summary>
    public enum Pick { Fixed, Random, NoLeague }

    private static ManagerSetup Manager(string name, Pick pick, int fixedClub, League league, StartStrength strength) =>
        pick switch
        {
            Pick.Fixed => new ManagerSetup { Name = name, ClubIndex = fixedClub },
            Pick.NoLeague => new ManagerSetup { Name = name, ClubIndex = fixedClub },
            _ => new ManagerSetup { Name = name, StartLeague = league, StartStrength = strength },
        };

    private static void AssertExclusive(GameState game, GameSetup setup)
    {
        Assert.Equal(setup.Managers.Count, game.Managers.Count);
        Assert.Equal(game.Managers.Count, game.Managers.Select(m => m.ClubIndex).Distinct().Count());

        // Feste Wahlen bleiben erhalten, jeder Managerverein spielt in einer Liga, jede Liga hat 12 Vereine.
        for (var i = 0; i < setup.Managers.Count; i++)
        {
            if (setup.Managers[i].ClubIndex is { } chosen)
                Assert.Equal(chosen, game.Managers[i].ClubIndex);
            Assert.NotNull(game.Clubs[game.Managers[i].ClubIndex].League);
        }
        foreach (var league in Enum.GetValues<League>())
            Assert.Equal(12, game.LeagueClubs(league).Count());
    }

    /// <summary>Gegenbeispiel aus dem Review, synthetisch: zufällig aus dem Mittelfeld, danach fest auf jeden Mittelfeldverein.</summary>
    [Fact]
    public void RandomThenFixed_SameBand_NeverShared()
    {
        var database = TestGames.CreateDatabase();
        for (var seed = 0; seed < 50; seed++)
        for (var fixedClub = 4; fixedClub <= 7; fixedClub++)
        {
            var setup = new GameSetup();
            setup.Managers.Add(new ManagerSetup { Name = "A", StartLeague = League.Bundesliga, StartStrength = StartStrength.Mittelfeld });
            setup.Managers.Add(new ManagerSetup { Name = "B", ClubIndex = fixedClub });
            AssertExclusive(NewGame.Create(database, setup, seed), setup);
        }
    }

    /// <summary>Alle Reihenfolgen von zwei bis vier Managern mit gemischten Wahlarten, mehrere Seeds.</summary>
    [Theory]
    [InlineData(new[] { Pick.Random, Pick.Fixed })]
    [InlineData(new[] { Pick.Fixed, Pick.Random })]
    [InlineData(new[] { Pick.Random, Pick.Random, Pick.Fixed })]
    [InlineData(new[] { Pick.Random, Pick.Fixed, Pick.Random, Pick.Fixed })]
    [InlineData(new[] { Pick.NoLeague, Pick.Random, Pick.Fixed })]
    [InlineData(new[] { Pick.Random, Pick.NoLeague, Pick.Fixed, Pick.NoLeague })]
    [InlineData(new[] { Pick.Fixed, Pick.NoLeague, Pick.Random, Pick.Random })]
    public void MixedPicks_EveryOrderAndSeed_ExclusiveClubs(Pick[] picks)
    {
        var database = TestGames.CreateDatabase();
        foreach (var order in Permutations(Enumerable.Range(0, picks.Length).ToArray()))
        for (var seed = 0; seed < 15; seed++)
        {
            var setup = new GameSetup();
            foreach (var slot in order)
            {
                // Feste Wahlen zielen auf das Band, aus dem die Zufallswahl zieht (Oberliga-Mittelfeld = 28–31),
                // NoLeague auf Vereine ohne Liga (36–47). So entsteht die Kollisionsgefahr gezielt.
                var fixedClub = picks[slot] == Pick.NoLeague ? 40 + slot : 28 + slot;
                setup.Managers.Add(Manager($"M{slot}", picks[slot], fixedClub, League.Oberliga, StartStrength.Mittelfeld));
            }
            AssertExclusive(NewGame.Create(database, setup, seed), setup);
        }
    }

    /// <summary>
    /// Absicherung (war vor dem Fix nicht rot, weil die alte Kettenverdrängung zufällig ebenfalls exklusiv endete):
    /// Ein ligaloser Verein vorne verdrängt nie einen später fest gewählten Oberligaverein.
    /// </summary>
    [Fact]
    public void NoLeagueClubFirst_DoesNotDisplaceLaterFixedOberligaClub()
    {
        var database = TestGames.CreateDatabase();
        for (var seed = 0; seed < 100; seed++)
        {
            var setup = new GameSetup();
            setup.Managers.Add(new ManagerSetup { Name = "A", ClubIndex = 45 });
            for (var c = 24; c < 27; c++)
                setup.Managers.Add(new ManagerSetup { Name = $"M{c}", ClubIndex = c });
            var game = NewGame.Create(database, setup, seed);
            AssertExclusive(game, setup);
            Assert.Equal(1, Enumerable.Range(27, 9).Count(c => game.Clubs[c].League is null));
        }
    }

    [Fact]
    public void DuplicateFixedChoice_IsRejected()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "A", ClubIndex = 4 });
        setup.Managers.Add(new ManagerSetup { Name = "B", ClubIndex = 4 });
        Assert.Throws<ArgumentException>(() => NewGame.Create(TestGames.CreateDatabase(), setup, 0));
    }

    /// <summary>Gegenbeispiel aus dem Review mit der Originaldatenbank (übersprungen ohne Originaldateien).</summary>
    [GameDataFact]
    public void ReviewCounterexample_OriginalDatabase_Seed0()
    {
        var data = TestGameData.Load();
        var database = ClubDatabase.Load(data);

        for (var seed = 0; seed < 20; seed++)
        {
            var setup = new GameSetup();
            setup.Managers.Add(new ManagerSetup { Name = "A", StartLeague = League.Bundesliga, StartStrength = StartStrength.Mittelfeld });
            setup.Managers.Add(new ManagerSetup { Name = "B", ClubIndex = 4 });
            var game = NewGame.Create(database, setup, seed);
            AssertExclusive(game, setup);
        }
    }

    private static IEnumerable<int[]> Permutations(int[] items)
    {
        if (items.Length <= 1)
        {
            yield return items;
            yield break;
        }
        for (var i = 0; i < items.Length; i++)
        {
            var rest = items.Where((_, j) => j != i).ToArray();
            foreach (var p in Permutations(rest))
                yield return [items[i], .. p];
        }
    }
}
