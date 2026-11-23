using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur re-reste (Runde 23, Teil 2): #925 (b) im DEB-Pokal. Lesung und ausführbare Belege in <c>docs/analyse/re-reste-23.md</c> §1.
/// </summary>
public class ReResteTests
{
    /// <summary>
    /// #925 (b), schneller Weg im DEB-Pokal (rot vor dem Fix: nie ein Gleichstand): [ORIGINAL] 0x32AA3 markiert auch die Zeile 0
    /// (0x32AF4 <c>cmp byte [bp+0x6],0x1</c>, Heimbyte + 30 bei 0x32B9D), der Vergleich 0x37748 ist deshalb nie gleich – genau ein
    /// Wurf random(0, 2) je Seite, bei x = y (1/3) bleibt der Gleichstand stehen. Ohne <c>debCup</c> (WM-Endrunde) wie bisher bis
    /// ungleich.
    /// </summary>
    [Fact]
    public void FastPath_DebCupTie_OneRollAndTieCanStay()
    {
        var side = new MatchSide(50);
        int stayed = 0, rolled = 0;
        for (var seed = 0; seed < 3000; seed++)
        {
            var plain = OriginalMinutes.FastCupResultWithTie(side, side, true, true, false, false, knockOut: false, new Random(seed));
            if (plain.Home != plain.Away)
                continue;
            var deb = OriginalMinutes.FastCupResultWithTie(side, side, true, true, false, false, knockOut: true, new Random(seed),
                debCup: true);
            Assert.True(deb.Shootout);
            Assert.Equal(plain.Home, deb.Tie);
            Assert.InRange(deb.Home - plain.Home, 0, 2);
            Assert.InRange(deb.Away - plain.Away, 0, 2);
            rolled++;
            if (deb.Home == deb.Away)
                stayed++;
            var other = OriginalMinutes.FastCupResultWithTie(side, side, true, true, false, false, knockOut: true, new Random(seed));
            Assert.NotEqual(other.Home, other.Away);
        }
        Assert.True(rolled > 50);
        Assert.InRange(stayed, rolled / 6, rolled / 2); // x = y mit 1/3
    }

    /// <summary>
    /// #925 (b) (rot vor dem Fix: Sieger Gast, eine Marke): [ORIGINAL] Nach dem Wurf des schnellen Wegs trägt das Heimbyte zwei
    /// Marken (0x32B9D und 0x3775D, FILE44 D014 „40 04“ = 4:4 mit zwei Marken); die Wertung 0x34C76 (<c>cmp al,[bx+0x5c4c]; jnc</c>)
    /// zählt Heim ≥ Gast für den Platz 2k, den Heimverein des Spiels. Wurf 1:1 nach 3:3 → 4:4, Sieger Heim, zwei Marken; Wurf 2:0 →
    /// 5:3, Sieger Heim, zwei Marken; Wurf 0:1 → 3:4, Sieger Gast, zwei Marken. Der Minutenweg (Penaltyschießen 0x3901B) behält eine
    /// Marke, die Play-offs (ohne <c>debCup</c>) den Gast.
    /// </summary>
    [Fact]
    public void FromReport_DebCupFastPath_TieStaysForHome_TwoMarks()
    {
        var game = TestGames.Create(3);
        var (home, away) = (game.Clubs[4], game.Clubs[5]);
        static MatchReport Fast((int, int) goals, bool? homeWins) => new()
        {
            HomeGoals = 3, AwayGoals = 3, ShootoutHomeWins = homeWins, ShootoutGoals = goals, FastCupPath = true,
        };

        var tie = CupMatch.FromReport(1, home, away, Fast((1, 1), false), debCup: true);
        Assert.Equal((4, 4, (int?)home.Index, 2), (tie.Match.HomeGoals, tie.Match.AwayGoals, tie.Match.PenaltyWinner, tie.Marks));
        Assert.Equal(home.Index, tie.Match.Winner);

        var homeWon = CupMatch.FromReport(1, home, away, Fast((2, 0), true), debCup: true);
        Assert.Equal((5, 3, (int?)home.Index, 2), (homeWon.Match.HomeGoals, homeWon.Match.AwayGoals, homeWon.Match.PenaltyWinner, homeWon.Marks));

        var awayWon = CupMatch.FromReport(1, home, away, Fast((0, 1), false), debCup: true);
        Assert.Equal((3, 4, (int?)away.Index, 2), (awayWon.Match.HomeGoals, awayWon.Match.AwayGoals, awayWon.Match.PenaltyWinner, awayWon.Marks));

        var minute = CupMatch.FromReport(1, home, away,
            new MatchReport { HomeGoals = 3, AwayGoals = 3, ShootoutHomeWins = true, ShootoutGoals = (3, 2) }, debCup: true);
        Assert.Equal((6, 5, (int?)home.Index, 1), (minute.Match.HomeGoals, minute.Match.AwayGoals, minute.Match.PenaltyWinner, minute.Marks));

        var playoff = CupMatch.FromReport(1, home, away, Fast((1, 1), false));
        Assert.Equal(((int?)away.Index, 1), (playoff.Match.PenaltyWinner, playoff.Marks));
    }

    /// <summary>
    /// #925 (b), Anschluss: Ein DEB-Pokalspiel ohne Managerverein über den schnellen Weg (Anzeigeschalter DGROUP 0x227 aus) liefert
    /// einen Bericht mit <see cref="MatchReport.FastCupPath"/>, Zusatztoren 0 … 2 je Seite und – über die Saaten – auch stehende
    /// Gleichstände; <see cref="CupMatch.FromReport"/> (Weg von <see cref="CupMatch.Play"/>) wertet sie für den Heimverein mit zwei
    /// Marken (rot vor dem Fix: nie ein Gleichstand).
    /// </summary>
    [Fact]
    public void Play_DebCupFastPath_TieStaysForHome()
    {
        var game = TestGames.Create(3);
        game.Options.ShowDebCup = false;
        MatchEngine.EnsureTeamValues(game);
        var (home, away) = (game.Clubs[4], game.Clubs[5]);
        int shootouts = 0, stayed = 0;
        for (var seed = 0; seed < 400 && stayed < 3; seed++)
        {
            var report = MatchEngine.PlayWithEvents(game, home, away, MatchCompetition.DebCup, knockOut: true, new Random(seed));
            Assert.True(report.FastCupPath);
            if (report.ShootoutGoals is not { } goals)
                continue;
            shootouts++;
            Assert.Equal(report.HomeGoals, report.AwayGoals);
            Assert.InRange(goals.Home, 0, 2);
            Assert.InRange(goals.Away, 0, 2);
            var (match, marks) = CupMatch.FromReport(1, home, away, report, debCup: true);
            Assert.Equal(2, marks);
            Assert.Equal((report.HomeGoals + goals.Home, report.AwayGoals + goals.Away), (match.HomeGoals, match.AwayGoals));
            if (goals.Home != goals.Away)
                continue;
            stayed++;
            Assert.Equal(home.Index, match.Winner);
        }
        Assert.True(shootouts > 0);
        Assert.Equal(3, stayed);
    }
}
