using EishockeyManager.Core;
using Xunit;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur tabellen-original: Ligatabelle nach em.exe 0x50283 (Ergebnis eintragen) und 0x50B45 (Tauschsortierung DGROUP 0x5CCB).
/// Konstruierte Tabellen: Spieltag 1 gewinnt c[5] daheim 3:1 gegen c[0], alle anderen spielen 1:1; Spieltag 2 umgekehrt.
/// Danach sind c[0] und c[5] in allen Kriterien gleich (2 Punkte, 2 Spiele, Tordifferenz 0, 4 Tore).
/// </summary>
public class TabellenOriginalTests
{
    private static (GameState Game, List<int> C) Create()
    {
        var game = TestGames.Create();
        game.Results.Clear();
        var c = game.LeagueClubs(League.Bundesliga).Select(x => x.Index).ToList();
        return (game, c);
    }

    private static void Day(GameState game, List<int> c, int matchDay, (int Home, int Away, int HomeGoals, int AwayGoals) first)
    {
        game.Results.Add(new MatchResult(League.Bundesliga, matchDay, c[first.Home], c[first.Away], first.HomeGoals, first.AwayGoals));
        foreach (var (h, a) in new[] { (1, 2), (3, 4), (6, 7), (8, 9), (10, 11) })
            game.Results.Add(new MatchResult(League.Bundesliga, matchDay, c[h], c[a], 1, 1));
    }

    private static (GameState Game, List<int> C) TwoDays()
    {
        var (game, c) = Create();
        Day(game, c, 1, (5, 0, 3, 1));
        Day(game, c, 2, (0, 5, 3, 1));
        return (game, c);
    }

    [Fact]
    public void CompleteTie_KeepsOrderOfPreviousMatchDay()
    {
        // [ORIGINAL] 0x50B45: Tauschsortierung an Ort und Stelle; bei vollem Gleichstand bleibt die Reihenfolge des Vortags.
        var (game, c) = TwoDays();
        var table = LeagueTable.Compute(game, League.Bundesliga, TableView.Gesamt, 2);
        Assert.Equal(c[5], table[0].Club.Index);
        Assert.Equal(c[0], table[1].Club.Index);
    }

    [Fact]
    public void HomeView_TieKeepsOverallOrder()
    {
        // [ORIGINAL] 0x4F78D sortiert 0x5CCB für „Heim“ ausgehend von der Gesamtreihenfolge.
        var (game, c) = TwoDays();
        var home = LeagueTable.Compute(game, League.Bundesliga, TableView.Heim, 2);
        Assert.Equal(c[5], home[0].Club.Index);
        Assert.Equal(c[0], home[1].Club.Index);
    }

    [Fact]
    public void Balance_HomeLowercase_AwayUppercase()
    {
        // [ORIGINAL] 0x50283: Heimverein bekommt DGROUP 0x7CE/0x7D2/0x7D0 („s“, „u“, „n“), Gast 0x7D1/0x7D3/0x7CF („N“, „U“, „S“).
        var (game, c) = TwoDays();
        var rows = LeagueTable.Compute(game, League.Bundesliga, TableView.Gesamt, 2).ToDictionary(r => r.Club.Index);
        Assert.Equal("sN", rows[c[5]].Balance);
        Assert.Equal("Ns", rows[c[0]].Balance);
        Assert.Equal("uu", rows[c[1]].Balance);
        Assert.Equal("UU", rows[c[2]].Balance);
    }

    [Fact]
    public void Balance_SameInEveryView()
    {
        // [ORIGINAL] 0x4F78D zeichnet die Bilanz DGROUP 0x825E + 0x50 · Verein unabhängig von der Ansicht.
        var (game, c) = TwoDays();
        foreach (var view in new[] { TableView.Heim, TableView.Auswaerts })
        {
            var rows = LeagueTable.Compute(game, League.Bundesliga, view, 2).ToDictionary(r => r.Club.Index);
            Assert.Equal("sN", rows[c[5]].Balance);
        }
    }

    [Fact]
    public void EqualPoints_FewerGamesRanksHigher()
    {
        // [ORIGINAL] 0x50B45: nach den Punkten entscheiden die Spiele (weniger = besser), dann Tordifferenz, dann Tore.
        var (game, c) = Create();
        Day(game, c, 1, (5, 0, 3, 1));
        // Spieltag 2: c[0] spielt nicht, c[5] verliert 0:1 gegen c[1]; die übrigen 1:1.
        game.Results.Add(new MatchResult(League.Bundesliga, 2, c[1], c[5], 1, 0));
        var table = LeagueTable.Compute(game, League.Bundesliga, TableView.Gesamt, 2).ToDictionary(r => r.Club.Index);
        // c[1]: 3 Punkte; c[5]: 2 Punkte aus 2 Spielen, Tordifferenz +1; c[2], c[3] …: 1 Punkt aus 1 Spiel.
        Assert.True(table[c[1]].Rank < table[c[5]].Rank);
        Assert.Equal(3, table[c[1]].PointsPlus);
    }

    [Fact]
    public void GoalDifferenceThenGoals()
    {
        var (game, c) = Create();
        game.Results.Add(new MatchResult(League.Bundesliga, 1, c[0], c[1], 5, 4)); // c[0]: +1, 5 Tore
        game.Results.Add(new MatchResult(League.Bundesliga, 1, c[2], c[3], 2, 0)); // c[2]: +2, 2 Tore
        game.Results.Add(new MatchResult(League.Bundesliga, 1, c[4], c[5], 2, 1)); // c[4]: +1, 2 Tore
        var t = LeagueTable.Compute(game, League.Bundesliga, TableView.Gesamt, 1).Select(r => r.Club.Index).Take(3).ToList();
        Assert.Equal([c[2], c[0], c[4]], t);
    }

    [Fact]
    public void ExchangeSort_IsNotStable_LikeOriginal()
    {
        // Tauschsortierung 0x50B45: für jeden Platz i alle späteren Plätze j, Tausch, wenn j besser ist. Ausgangsreihenfolge
        // [0, 1, 2] mit Punkten 0, 0, 1: Platz 0 tauscht mit Platz 2 → [2, 1, 0]; die gleichen Vereine 0 und 1 drehen sich um.
        var order = new List<int> { 0, 1, 2 };
        int[] points = [0, 0, 1];
        LeagueTable.ExchangeSort(order, (b, a) => points[b] > points[a]);
        Assert.Equal([2, 1, 0], order);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MultiSeason_FourChanges_FinalTablesSortedLikeOriginal(bool active)
    {
        // Mehrsaisonprobe: Platzierungen bestimmen Auf-/Abstieg, Play-offs und Prämien. Am Ende jeder Hauptrunde muss jede
        // Abschlusstabelle nach dem Vergleich von 0x50B45 geordnet sein (kein späterer Platz besser als ein früherer).
        var game = TestGames.Create(3);
        var checks = 0;
        RngTwinProbeTests.Run(game, active, beforeChange: g =>
        {
            foreach (var league in Enum.GetValues<League>())
            {
                var table = LeagueTable.Compute(g, league, TableView.Gesamt, g.PlayedMatchDays);
                Assert.Equal(12, table.Select(r => r.Club.Index).Distinct().Count());
                Assert.Equal(g.FinalTables()[league], table.Select(r => r.Club.Index));
                for (var i = 0; i + 1 < table.Count; i++)
                {
                    var (a, b) = (table[i], table[i + 1]);
                    Assert.True(a.PointsPlus > b.PointsPlus
                                || a.PointsPlus == b.PointsPlus && (a.Games < b.Games
                                || a.Games == b.Games && (a.GoalDifference > b.GoalDifference
                                || a.GoalDifference == b.GoalDifference && a.GoalsFor >= b.GoalsFor)),
                        $"{g.Season} {league}: Platz {i + 2} vor Platz {i + 1}");
                }
                checks++;
            }
        });
        Assert.Equal(12, checks);
        Assert.Equal(1997, game.Season);
    }
}
