using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Befund R2-06 (A03): Hot-Seat-Zugwechsel. Jeder aktive Manager kommt je Tag genau einmal dran;
/// simuliert wird erst nach der Bestätigung aller, und zwar genau einmal.
/// </summary>
public class HotSeatTests
{
    // Je ein Verein aus Bundesliga, 2. Liga, Oberliga und noch einer aus der Bundesliga
    private static readonly int[] ClubsOf = [2, 14, 26, 7];

    private static GameState Create(int managers, int seed = 1)
    {
        // Ohne DEB-Pokal: jeder gemeinsame Tag ist dann ein Ligaspieltag (Pokaltage prüft DebCupTests im Hot-Seat).
        var setup = new GameSetup { DebCup = false };
        for (var i = 0; i < managers; i++)
            setup.Managers.Add(new ManagerSetup { Name = $"M{i}", Head = i, ClubIndex = ClubsOf[i] });
        return NewGame.Create(TestGames.CreateDatabase(), setup, seed);
    }

    /// <summary>Spielt einen Tag im Hot-Seat: Reihenfolge der Manager am Zug und Anzahl der Simulationen.</summary>
    private static (List<int> Turns, int Simulations) PlayDay(GameState game)
    {
        var turns = new List<int>();
        var simulations = 0;
        for (var guard = 0; guard < 10; guard++)
        {
            turns.Add(game.CurrentManager);
            var day = game.CurrentDay;
            var played = game.PlayedMatchDays;
            var results = game.Results.Count;
            var simulate = game.IsLastTurnOfDay;
            var steps = game.Continue();
            var simulated = game.CurrentDay != day || game.PlayedMatchDays != played || game.Results.Count != results;
            Assert.Equal(simulate, simulated);
            if (!simulated)
            {
                Assert.Empty(steps);
                continue;
            }
            simulations++;
            return (turns, simulations);
        }
        throw new Xunit.Sdk.XunitException("Tag wurde nie simuliert.");
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void EveryManagerOncePerDay_DaySimulatedOnce(int count)
    {
        var game = Create(count);
        for (var day = 0; day < 12; day++)
        {
            var played = game.PlayedMatchDays;
            var (turns, simulations) = PlayDay(game);
            Assert.Equal(Enumerable.Range(0, count), turns);
            Assert.Equal(1, simulations);
            Assert.Equal(played + 1, game.PlayedMatchDays); // Ligaspieltage: genau einer je gemeinsamem Tag
            Assert.Equal(0, game.CurrentManager);           // neuer Tag beginnt beim ersten aktiven Manager
            Assert.All(game.Managers, m => Assert.False(game.IsReady(m)));
        }
    }

    /// <summary>Gegenbeispiel aus dem Review: zwei Manager, fünfmal Continue() → aktiver Manager blieb immer 0.</summary>
    [Fact]
    public void ReviewCounterexample_TwoManagers_FiveContinues()
    {
        var game = Create(2);
        var seen = new List<int>();
        for (var i = 0; i < 5; i++)
        {
            seen.Add(game.CurrentManager);
            game.Continue();
        }
        seen.Add(game.CurrentManager);
        Assert.Equal([0, 1, 0, 1, 0, 1], seen);
        Assert.Equal(2, game.PlayedMatchDays); // fünf Bestätigungen = zwei volle Tage + ein halber
    }

    [Fact]
    public void ReadyState_ShowsWhoHasConfirmed()
    {
        var game = Create(3);
        game.Continue();
        Assert.True(game.IsReady(game.Managers[0]));
        Assert.False(game.IsReady(game.Managers[1]));
        Assert.Equal(1, game.CurrentManager);
        game.Continue();
        Assert.True(game.IsReady(game.Managers[1]));
        Assert.True(game.IsLastTurnOfDay);
    }

    [Fact]
    public void DismissedManagers_AreSkipped()
    {
        var game = Create(4);
        game.Managers[0].Dismissed = true;
        game.Managers[2].Dismissed = true;
        game.StartTurns();
        for (var day = 0; day < 5; day++)
        {
            var (turns, simulations) = PlayDay(game);
            Assert.Equal([1, 3], turns);
            Assert.Equal(1, simulations);
        }
    }

    /// <summary>
    /// Scheidet ein Manager während eines Tages aus (Kennzeichen gesetzt, nachdem er seinen Zug bestätigt hat; #688, R688S-03: kein
    /// Weg der Zeitleiste setzt es mehr, es kommt aus älteren Spielständen), ist er ab dem nächsten Tag nicht mehr am Zug.
    /// </summary>
    [Fact]
    public void DismissalDuringDay_ExcludedFromNextDay()
    {
        var game = Create(3);
        var (first, _) = PlayDay(game); // Tag 0 mit allen drei Managern
        Assert.Equal([0, 1, 2], first);
        game.Continue(); // Manager 0 bestätigt, Manager 1 am Zug
        game.Continue(); // Manager 1 bestätigt, Manager 2 am Zug
        game.Managers[1].Dismissed = true;
        game.Continue(); // Manager 2 bestätigt: Tag läuft, CheckDismissed überspringt Manager 1 fortan
        Assert.False(game.IsGameOver);
        for (var day = 0; day < 3; day++)
        {
            var (turns, simulations) = PlayDay(game);
            Assert.Equal([0, 2], turns);
            Assert.Equal(1, simulations);
        }
    }

    [Fact]
    public void SingleManager_SimulatesImmediately()
    {
        var game = Create(1);
        Assert.True(game.IsLastTurnOfDay);
        var (turns, simulations) = PlayDay(game);
        Assert.Equal([0], turns);
        Assert.Equal(1, simulations);
    }
}
