using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur livespiel-kern, Teil A1 (L1): Anzeigezähler der Live-Anzeige (<c>docs/analyse/re-livespiel.md</c> §2, §12).
/// Vor dem Teil rot: es gab keine Reihenliste und keine Drittelzähler (<c>GameState.LiveScoreboard</c> fehlte, Build-Fehler).
/// </summary>
public sealed class LivespielKernScoreboardTests
{
    private static GameState Game(int seed, params int[] managerClubs)
    {
        var setup = new GameSetup();
        foreach (var club in managerClubs)
            setup.Managers.Add(new ManagerSetup { Name = $"M{club}", Head = 0, ClubIndex = club });
        return NewGame.Create(TestGames.CreateDatabase(), setup, seed);
    }

    /// <summary>
    /// Abnahme A1: Spieltag mit Managerpaarung – Summe der Drittelzähler = Tore der Ereignisliste bis Minute 60, Chancen ≥ Tore,
    /// Strafminuten = Strafminuten des Vereins ([ORIGINAL] 0x53CF6/0x53D1D, 0x359DC).
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void ManagerMatch_PeriodCountersMatchEvents(int seed)
    {
        var game = Game(seed, 2);
        var home = game.Clubs[2];
        var away = game.Clubs.First(c => c.Index != 2 && c.League == home.League);
        var report = MatchEngine.PlayWithEvents(game, home, away, MatchCompetition.League, knockOut: false, new Random(seed));

        var board = Assert.IsType<LiveScoreboard>(game.LiveScoreboard);
        var row = Assert.Single(board.Rows);
        Assert.True(board.Active);
        Assert.True(row.Finished);
        Assert.Equal((home.Index, away.Index), (row.HomeClub, row.AwayClub));
        for (var side = 0; side < 2; side++)
        {
            var goals = Enumerable.Range(0, 3).Sum(k => row.Goals(k, side));
            var chances = Enumerable.Range(0, 3).Sum(k => row.Chances(k, side));
            var eventGoals = report.Events.Count(e => e.Kind == MatchEventKind.Goal && e.Side == side && e.Minute <= 60);
            Assert.Equal(eventGoals, goals);
            Assert.Equal(side == 0 ? report.HomeGoals : report.AwayGoals, goals);
            Assert.True(chances >= goals);
            Assert.Equal(row.ChanceList.Count(c => c.Side == side), chances);
            for (var k = 0; k < 3; k++)
                Assert.Equal(report.Events.Count(e => e.Kind == MatchEventKind.Goal && e.Side == side
                    && (e.Minute - 1) / 20 == k), row.Goals(k, side));
            Assert.Equal((side == 0 ? report.HomePenaltyMinutes : report.AwayPenaltyMinutes) & 0xFF, row.PenaltyMinutes(side));
            Assert.Equal(row.PenaltyMinutes(side), row.PenaltyMinutes(side, 70));
        }
        // Die Zähler bis Minute m wachsen monoton und erreichen bei 60 den Endwert.
        for (var k = 0; k < 3; k++)
            Assert.Equal(row.Chances(k, 0, 60), Enumerable.Range(0, 61).Max(m => row.Chances(k, 0, m)));
    }

    /// <summary>
    /// Die Zähler sind reine Anzeige: dieselbe Managerpaarung mit demselben Zufall ergibt mit und ohne Reihe dasselbe Spiel
    /// (A1 zieht nichts; die Züge der Anzeige nach R18-D3 kommen erst mit A4).
    /// </summary>
    [Fact]
    public void Scoreboard_DrawsNoRandom()
    {
        static (MatchReport Report, LiveScoreboardRow Row) Run(bool withRow)
        {
            var game = Game(7, 2);
            var home = game.Clubs[2];
            var away = game.Clubs.First(c => c.Index != 2 && c.League == home.League);
            var homeTeam = MatchEngine.LiveTeamOf(game, home, away, managed: true, managerMatch: true);
            var awayTeam = MatchEngine.LiveTeamOf(game, away, home, managed: false, managerMatch: true);
            var row = new LiveScoreboard(game.Season, game.Day, MatchCompetition.League).Add(home.Index, away.Index)!;
            var report = MatchEngine.PlayWithScoreboard(withRow ? row : null,
                () => OriginalMinutes.Play(homeTeam, awayTeam, new MatchRules(ManagerMatch: true), new Random(11)));
            return (report, row);
        }

        var (with, row) = Run(withRow: true);
        var (without, empty) = Run(withRow: false);
        Assert.Equal((with.HomeGoals, with.AwayGoals), (without.HomeGoals, without.AwayGoals));
        Assert.Equal(with.Events.Select(e => (e.Minute, e.Side, e.Kind, e.PenaltyMinutes)),
            without.Events.Select(e => (e.Minute, e.Side, e.Kind, e.PenaltyMinutes)));
        Assert.NotEmpty(row.ChanceList);
        Assert.Empty(empty.ChanceList);
        Assert.Null(OriginalMinutes.CurrentRow);
    }

    /// <summary>
    /// Reihenfolge der Reihen wie die Paarungen im Spieltagsablauf ([ORIGINAL] 0x544C0 hängt je Paarung an, 0x5353C zeichnet in
    /// dieser Reihenfolge); nur Paarungen mit Managerverein; ein Ligaspieltag ist eine Liste.
    /// </summary>
    [Fact]
    public void MatchDay_RowsInPairingOrder_OnlyManagerPairings()
    {
        var game = Game(3, 2, 5, 14);
        var managed = new HashSet<int> { 2, 5, 14 };
        var expected = game.Schedules.OrderBy(s => s.Key)
            .SelectMany(s => s.Value[game.PlayedMatchDays].Matches)
            .Where(m => managed.Contains(m.Home) || managed.Contains(m.Away))
            .ToList();
        TestSeason.PlayMatchDay(game);

        var board = game.LiveScoreboard!;
        Assert.Equal(MatchCompetition.League, board.Competition);
        Assert.Equal(expected, board.Rows.Select(r => (r.HomeClub, r.AwayClub)).ToList());
        Assert.All(board.Rows, r => Assert.True(r.Finished));
        Assert.Null(board.RowOf(game.Clubs.First(c => !managed.Contains(c.Index)
            && !expected.Any(m => m.Home == c.Index || m.Away == c.Index)).Index));
    }

    /// <summary>Der nächste Spieltag beginnt eine neue Liste ([ORIGINAL] 0x36B5D löscht Reihen und Zähler vor den Spielen).</summary>
    [Fact]
    public void NextMatchDay_StartsNewList()
    {
        var game = Game(4, 2);
        TestSeason.PlayMatchDay(game);
        var first = game.LiveScoreboard!;
        TestSeason.PlayMatchDay(game);
        var second = game.LiveScoreboard!;
        Assert.NotSame(first, second);
        var row = Assert.Single(second.Rows);
        Assert.True(row.HomeClub == 2 || row.AwayClub == 2);
    }

    /// <summary>Ohne Managerverein keine Reihe; die Liste bleibt leer, die Live-Anzeige aus (DGROUP 0x5A56 = 0).</summary>
    [Fact]
    public void ComputerPairing_NoRow()
    {
        var game = Game(5, 2);
        var clubs = game.Clubs.Where(c => c.Index != 2 && c.League == game.Clubs[2].League).Take(2).ToList();
        MatchEngine.PlayWithEvents(game, clubs[0], clubs[1], MatchCompetition.League, knockOut: false, new Random(1));
        Assert.False(game.LiveScoreboard!.Active);
        Assert.Empty(game.LiveScoreboard.Rows);
    }

    /// <summary>[APPROX] Höchstens vier Reihen (Anzeige und Zähler kennen nur p &lt; 4, Datei 0x53CC8).</summary>
    [Fact]
    public void Board_HoldsAtMostFourRows()
    {
        var board = new LiveScoreboard(1993, 0, MatchCompetition.League);
        for (var i = 0; i < LiveScoreboard.MaxRows; i++)
            Assert.NotNull(board.Add(2 * i, 2 * i + 1));
        Assert.Null(board.Add(20, 21));
        Assert.Equal(LiveScoreboard.MaxRows, board.Rows.Count);
    }

    /// <summary>[ORIGINAL] Drittel nach DGROUP 0xA838: Minute 20 zählt im ersten, 21 im zweiten, nach 60 gar nicht.</summary>
    [Theory]
    [InlineData(1, 0)]
    [InlineData(20, 0)]
    [InlineData(21, 1)]
    [InlineData(40, 1)]
    [InlineData(41, 2)]
    [InlineData(60, 2)]
    [InlineData(61, -1)]
    [InlineData(70, -1)]
    public void PeriodOf_Minute(int minute, int period) => Assert.Equal(period, LiveScoreboardRow.PeriodOf(minute));
}
