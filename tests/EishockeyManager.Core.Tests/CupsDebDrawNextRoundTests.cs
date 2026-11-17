using System.Reflection;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Review #909 P3-2 (#902): gezielte Probe für die Auslosung der DEB-Pokalrunden 2–5 (<c>CupsDeb.DrawNextRound</c>, em.exe Datei
/// 0x3489D): Sieger in Paarungsreihenfolge, dann <see cref="DebCup.DrawSwaps"/> Vertauschungen mit <c>random(1, n) − 1</c> aus dem
/// gemeinsamen Strom. Vorher schützten nur Abdrücke (<c>RngTwinProbeTests</c> u. a.) die Quelle: Die Mutation <c>new
/// GameRandom(1)</c> statt <c>game.Random</c> ließ <c>ZufallsstromTeil4Tests</c> grün.
/// </summary>
public class CupsDebDrawNextRoundTests
{
    [Fact]
    public void DrawNextRound_SwapsWinnersWithRollsOfSharedStream()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Trainer 0", Head = 0, ClubIndex = 2 });
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, 3);
        var deb = game.DebCup!;
        for (var guard = 0; deb.Rounds.Count < 2 && guard < 400; guard++)
            game.Continue();
        Assert.Equal(2, deb.Rounds.Count);

        // Zustand vor der Auslosung der zweiten Runde herstellen: Runde 2 verwerfen, Strom auf einen bekannten Stand setzen.
        var rounds = (List<List<CupTie>>)typeof(DebCup).GetField("_rounds", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(deb)!;
        rounds.RemoveAt(1);
        var state = game.Random.Capture();
        var winners = rounds[^1].Select(t => t.Winner!.Value).ToList();
        var sim = GameRandom.FromState(state);
        for (var i = 0; i < DebCup.DrawSwaps; i++)
        {
            var a = sim.Next(1, winners.Count + 1) - 1;
            var b = sim.Next(1, winners.Count + 1) - 1;
            (winners[a], winners[b]) = (winners[b], winners[a]);
        }

        typeof(DebCup).GetMethod("DrawNextRound", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(deb, [game]);

        Assert.Equal(2, deb.Rounds.Count);
        Assert.Equal(winners, deb.Draw);
        Assert.Equal(sim.Capture(), game.Random.Capture());
    }
}
