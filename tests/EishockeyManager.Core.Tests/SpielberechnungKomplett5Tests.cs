using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur spielberechnung-komplett, Teil 5: Penaltyschießen nach der Verlängerung (0x3901B). Fundstellen:
/// <c>docs/spielberechnung.md</c> §19.
/// </summary>
public class SpielberechnungKomplett5Tests
{
    /// <summary>Liefert vorgegebene Werte (geprüft gegen die Grenzen) und merkt sich die Grenzen jedes Zugs.</summary>
    private sealed class ScriptedRandom(params int[] values) : Random(0)
    {
        private readonly Queue<int> _values = new(values);
        public List<(int Min, int Max)> Calls { get; } = [];

        public override int Next(int minValue, int maxValue)
        {
            Calls.Add((minValue, maxValue - 1));
            var v = _values.Count > 0 ? _values.Dequeue() : minValue;
            Assert.InRange(v, minValue, maxValue - 1);
            return v;
        }
    }

    /// <summary>
    /// [ORIGINAL] 0x3901B ohne Anzeige: random(0, 1) zuerst (0x39028), dann je Seite random(2, 5), Heim zuerst, bis
    /// ungleich (0x39105–0x39179).
    /// </summary>
    [Fact]
    public void Shootout_FollowsOriginalDraws()
    {
        var random = new ScriptedRandom(1, 3, 3, 2, 4);
        Assert.Equal((2, 4), OriginalMinutes.Shootout(random));
        Assert.Equal([(0, 1), (2, 5), (2, 5), (2, 5), (2, 5)], random.Calls);
    }

    /// <summary>
    /// Anschluss: Ein K.-o.-Spiel des Minutenwegs, das nach der Verlängerung unentschieden steht, entscheidet das
    /// Penaltyschießen 0x3901B (Tore 2 … 5, ungleich, Sieger = mehr Tore); der Spielstand bleibt der Gleichstand.
    /// Vorher: kein Sieger im Bericht, der Aufrufer zog ihn mit <c>NextDouble</c> nach der Vereinsstärke ([APPROX]).
    /// </summary>
    [Fact]
    public void ManagedKnockoutTie_DecidedByOriginalShootout()
    {
        var game = TestGames.Create(3);
        MatchEngine.EnsureTeamValues(game);
        var club = game.Clubs[game.Managers[0].ClubIndex];
        var ties = 0;
        for (var seed = 0; seed < 400 && ties < 3; seed++)
        {
            var opponent = game.Clubs[(club.Index + 1 + seed % 11) % 12];
            var report = MatchEngine.PlayWithEvents(game, club, opponent, MatchCompetition.DebCup, knockOut: true,
                new Random(seed));
            if (report.HomeGoals != report.AwayGoals)
            {
                Assert.Null(report.ShootoutGoals);
                continue;
            }
            ties++;
            Assert.True(report.Overtime);
            var (h, a) = Assert.IsType<(int, int)>(report.ShootoutGoals);
            Assert.InRange(h, 2, 5);
            Assert.InRange(a, 2, 5);
            Assert.NotEqual(h, a);
            Assert.Equal(h > a, report.ShootoutHomeWins);
        }
        Assert.True(ties > 0);
    }

    /// <summary>Ohne K.-o. (Liga, Gruppen) kein Penaltyschießen.</summary>
    [Fact]
    public void NoKnockout_NoShootout()
    {
        var game = TestGames.Create(3);
        MatchEngine.EnsureTeamValues(game);
        var club = game.Clubs[game.Managers[0].ClubIndex];
        for (var seed = 0; seed < 40; seed++)
        {
            var report = MatchEngine.PlayWithEvents(game, club, game.Clubs[(club.Index + 1) % 12], MatchCompetition.League,
                knockOut: false, new Random(seed));
            Assert.Null(report.ShootoutGoals);
            Assert.Null(report.ShootoutHomeWins);
        }
    }
}
