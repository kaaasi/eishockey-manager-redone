using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur re-reste (Runde 23, Teil 2): #925 (b) im DEB-Pokal. Lesung und ausführbare Belege in <c>docs/archiv/analyse/re-reste-23.md</c> §1.
/// </summary>
public class DebCupFastPathTests
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
        // C07 (Runde 25, wettbewerbe-anzeige, R15-E2 = C): [ORIGINAL] Verlängerungstor eine Marke, Penaltyschießen und Gleichstand im schnellen Weg zwei (Beleg c07_marken.py, re-abweichungen C07); vorher umgekehrt.
        Assert.Equal((6, 5, (int?)home.Index, 2), (minute.Match.HomeGoals, minute.Match.AwayGoals, minute.Match.PenaltyWinner, minute.Marks));

        var playoff = CupMatch.FromReport(1, home, away, Fast((1, 1), false));
        Assert.Equal(((int?)away.Index, 2), (playoff.Match.PenaltyWinner, playoff.Marks)); // C07: schneller Weg zwei Marken
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

    /// <summary>
    /// RR-01 (Review #965, Fixlauf; rot vorher: kein Wurf bei ungleichem Ergebnis): [ORIGINAL] Steht hinter der gerechneten
    /// DEB-Pokalpaarung noch eine offene Paarung des Termins (0:0 in DGROUP 0x5C4B), meldet 0x32AA3 „Gleichstand“ (0x32B23 bzw.
    /// 0x32B3F–0x32B5A) und 0x376F0 würfelt auch für ein ungleiches Ergebnis random(0, 2) je Seite, bis Heim + x ≠ Gast + y
    /// (0x37724–0x37748); eine Marke (0x3775D), Sieger nach der Summe (0x34C76). Ohne offene Paarung dahinter bleibt das ungleiche
    /// Ergebnis ohne Wurf; der eigene Gleichstand (ein Wurf, zwei Marken) bleibt unverändert.
    /// </summary>
    [Fact]
    public void FastPath_DebCupExtraRoll_OnClearResult()
    {
        var side = new MatchSide(50);
        int clear = 0, changed = 0;
        for (var seed = 0; seed < 2000; seed++)
        {
            var plain = OriginalMinutes.FastCupResultWithTie(side, side, true, true, false, false, knockOut: true, new Random(seed),
                debCup: true);
            if (plain.Shootout)
            {
                var tieRoll = OriginalMinutes.FastCupResultWithTie(side, side, true, true, false, false, knockOut: true,
                    new Random(seed), debCup: true, debExtraRoll: true);
                Assert.Equal(plain, tieRoll); // eigener Gleichstand: wie ohne offene Paarung dahinter
                continue;
            }
            clear++;
            Assert.NotEqual(plain.Home, plain.Away);
            var extra = OriginalMinutes.FastCupResultWithTie(side, side, true, true, false, false, knockOut: true, new Random(seed),
                debCup: true, debExtraRoll: true);
            Assert.True(extra.Shootout);
            Assert.Equal((plain.Home, plain.Away), (extra.Tie, extra.AwayBeforeRoll));
            Assert.NotEqual(extra.Home, extra.Away);
            Assert.InRange(extra.Home - plain.Home, 0, 2);
            Assert.InRange(extra.Away - plain.Away, 0, 2);
            if ((extra.Home, extra.Away) != (plain.Home, plain.Away))
                changed++;
        }
        Assert.True(clear > 500);
        Assert.True(changed > clear / 2);
    }

    /// <summary>
    /// RR-01: <see cref="CupMatch.FromReport"/> trägt im DEB-Pokal (schneller Weg) auch ein ungleiches Ergebnis mit Zusatztoren:
    /// Endstand = Ergebnis + Wurf, eine Marke (0x3775D), Sieger nach der Summe (0x34C76; 3:1 mit Wurf 0:2 → 3:3 zählt für den
    /// Heimverein, 2:1 mit Wurf 0:2 → 2:3 kippt zum Gast). Ohne <c>debCup</c> bleibt ein ungleiches Ergebnis ohne Marke.
    /// </summary>
    [Fact]
    public void FromReport_DebCupExtraRoll_ClearResultCarriesExtraGoals_OneMark()
    {
        var game = TestGames.Create(3);
        var (home, away) = (game.Clubs[4], game.Clubs[5]);
        static MatchReport Fast(int h, int g, (int, int) goals) => new()
        {
            HomeGoals = h, AwayGoals = g, ShootoutHomeWins = h + goals.Item1 > g + goals.Item2, ShootoutGoals = goals, FastCupPath = true,
        };

        var homeWon = CupMatch.FromReport(1, home, away, Fast(3, 1, (2, 0)), debCup: true);
        Assert.Equal((5, 1, home.Index, 1), (homeWon.Match.HomeGoals, homeWon.Match.AwayGoals, homeWon.Match.Winner, homeWon.Marks));

        var flipped = CupMatch.FromReport(1, home, away, Fast(2, 1, (0, 2)), debCup: true);
        Assert.Equal((2, 3, away.Index, 1), (flipped.Match.HomeGoals, flipped.Match.AwayGoals, flipped.Match.Winner, flipped.Marks));
        Assert.Equal(away.Index, flipped.Match.PenaltyWinner);

        var playoff = CupMatch.FromReport(1, home, away, Fast(3, 1, (2, 0)));
        Assert.Equal((3, 1, 0), (playoff.Match.HomeGoals, playoff.Match.AwayGoals, playoff.Marks));
    }

    /// <summary>
    /// RR-01, Anschluss: Zwei (und mehr) Paarungen an einem DEB-Pokaltermin. [ORIGINAL] Die Markierung 0x32AA3 erkennt eine offene
    /// Paarung nur zweimal (0:0 → 30:0 → 60:0, Vergleiche 0x32B23 und 0x32B3F): Zusatzwurf für die erste und zweite gerechnete
    /// Paarung mit offener Paarung dahinter, nicht für die dritte und nicht für die letzte offene. Eine entschiedene Paarung
    /// (D014-Byte 100, 0x32B71) zählt nicht als offen.
    /// </summary>
    [Fact]
    public void DebCupExtraRoll_FirstTwoPairingsBeforeAnOpenOne()
    {
        var game = TestGames.Create(3);
        var cup = Assert.IsType<DebCup>(game.DebCup);
        cup.StartSeason(game);
        var round = cup.Rounds[cup.CurrentRound];
        Assert.Equal(DebCup.Teams / 2, round.Count);
        Club Home(CupTie t) => game.Clubs[CupTie.Pairing(t, t.Games.Count).Home];
        Club Away(CupTie t) => game.Clubs[CupTie.Pairing(t, t.Games.Count).Away];
        void Played(CupTie t, int homeGoals, int awayGoals)
        {
            var (h, a) = CupTie.Pairing(t, t.Games.Count);
            t.Add(new KnockoutMatch(1, h, a, homeGoals, awayGoals), 0);
        }

        Assert.True(MatchEngine.DebCupExtraRoll(game, Home(round[0]), Away(round[0])));
        Assert.True(MatchEngine.DebCupExtraRoll(game, Home(round[1]), Away(round[1])));
        Played(round[0], 3, 1);
        Assert.True(MatchEngine.DebCupExtraRoll(game, Home(round[1]), Away(round[1])));
        Played(round[1], 2, 2);
        Assert.False(MatchEngine.DebCupExtraRoll(game, Home(round[2]), Away(round[2]))); // 60:0 wird nicht mehr erkannt
        for (var k = 2; k < round.Count - 1; k++)
            Played(round[k], 1, 0);
        Assert.False(MatchEngine.DebCupExtraRoll(game, Home(round[^1]), Away(round[^1]))); // letzte offene Paarung
        Played(round[^1], 0, 1);

        // Zweiter Termin: alle offen, Paarung 0 und 1 würfeln mit; dritter Termin: entschiedene Paarungen zählen nicht als offen.
        Assert.True(MatchEngine.DebCupExtraRoll(game, Home(round[0]), Away(round[0])));
        foreach (var t in round.Take(round.Count - 1))
            Played(t, 0, 2); // Spiel 2: First ist Gast und gewinnt erneut → entschieden
        Played(round[^1], 0, 2); // Spiel 1 an Second, Spiel 2 an First → offen
        Assert.Equal(1, round.Count(t => !t.IsDecided));
        Assert.False(MatchEngine.DebCupExtraRoll(game, Home(round[^1]), Away(round[^1]))); // einzige offene Paarung
        Assert.False(MatchEngine.DebCupExtraRoll(game, Home(round[0]), Away(round[0]))); // entschieden

        // Spiel über den schnellen Weg: Paarung 0 oder 1 eines frischen Termins liefert auch bei ungleichem Ergebnis Zusatztore.
        cup.StartSeason(game);
        game.Options.ShowDebCup = false;
        MatchEngine.EnsureTeamValues(game);
        var first = cup.Rounds[cup.CurrentRound].Take(2).First(t => !game.ActiveManagers.Any(m => t.Involves(m.ClubIndex)));
        Assert.True(MatchEngine.DebCupExtraRoll(game, Home(first), Away(first)));
        var rolledClear = 0;
        for (var seed = 0; seed < 200 && rolledClear < 3; seed++)
        {
            var report = MatchEngine.PlayWithEvents(game, Home(first), Away(first), MatchCompetition.DebCup, knockOut: true, new Random(seed));
            Assert.True(report.FastCupPath);
            if (report.HomeGoals == report.AwayGoals || report.ShootoutGoals is not { } goals)
                continue;
            rolledClear++;
            var (match, marks) = CupMatch.FromReport(1, Home(first), Away(first), report, debCup: true);
            Assert.Equal(1, marks);
            Assert.Equal((report.HomeGoals + goals.Home, report.AwayGoals + goals.Away), (match.HomeGoals, match.AwayGoals));
            Assert.NotEqual(match.HomeGoals, match.AwayGoals);
        }
        Assert.Equal(3, rolledClear);
    }
}
