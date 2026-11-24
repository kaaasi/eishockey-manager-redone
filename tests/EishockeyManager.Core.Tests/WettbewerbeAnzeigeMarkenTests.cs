using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Runde 25, Spur wettbewerbe-anzeige Teil 3: C07 (A098, A482, A924, A925) – Ergebnismarken der K.-o.-Spiele wie im Original.
/// Erwartete Werte aus <c>c07_marken.py</c> (docs/analyse/re-abweichungen.md C07): Minutenweg, 3:3 nach 60, Heimtor in der
/// Verlängerung → 4:3 mit einer Marke; 3:3 ohne Tor, Penaltyschießen 3:2 → 6:5 mit zwei Marken – in DEB-Pokal (Zeile 0),
/// Play-offs (Zeile 2) und WM-Endrunde (Zeile 3) gleich. Vorher umgekehrt (Pokal: zwei bzw. eine; Play-offs: zwei in beiden Fällen).
/// </summary>
public class WettbewerbeAnzeigeMarkenTests
{
    private static readonly Club Home = new() { Index = 10, Name = "HEIM", Strength = 70 };
    private static readonly Club Away = new() { Index = 40, Name = "GAST", Strength = 70 };

    private static MatchReport OvertimeGoal() => new() { HomeGoals = 4, AwayGoals = 3, Overtime = true };

    private static MatchReport Shootout() => new()
    {
        HomeGoals = 3, AwayGoals = 3, Overtime = true, ShootoutGoals = (3, 2), ShootoutHomeWins = true,
    };

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Cup_OvertimeGoal_OneMark(bool debCup)
    {
        var (match, marks) = CupMatch.FromReport(100, Home, Away, OvertimeGoal(), debCup);
        Assert.Equal((4, 3), (match.HomeGoals, match.AwayGoals));
        Assert.Equal(1, marks);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Cup_Shootout_TwoMarks(bool debCup)
    {
        var (match, marks) = CupMatch.FromReport(100, Home, Away, Shootout(), debCup);
        Assert.Equal((6, 5), (match.HomeGoals, match.AwayGoals));
        Assert.Equal(Home.Index, match.PenaltyWinner);
        Assert.Equal(2, marks);
    }

    [Fact]
    public void Playoff_OvertimeGoalOne_ShootoutTwo_RegularNone()
    {
        Assert.Equal(1, PlayoffSeries.MarksFromReport(OvertimeGoal()));
        Assert.Equal(2, PlayoffSeries.MarksFromReport(Shootout()));
        Assert.Equal(0, PlayoffSeries.MarksFromReport(new MatchReport { HomeGoals = 4, AwayGoals = 2 }));
    }

    /// <summary>
    /// C07 (A925, Lesart re-abweichungen C07): [ORIGINAL] Schneller Weg der WM-Endrunde bei Gleichstand – 0x32AA3 markiert Zeile 3
    /// vor dem Zusatzwurf (Datei 0x32C8E–0x32D84), 0x3775D setzt die zweite Marke. Vorher eine Marke.
    /// </summary>
    [Fact]
    public void WorldCup_FastPathTie_TwoMarks()
    {
        var report = new MatchReport { HomeGoals = 2, AwayGoals = 2, FastCupPath = true, ShootoutGoals = (1, 0), ShootoutHomeWins = true };
        var (match, marks) = CupMatch.FromReport(100, Home, Away, report);
        Assert.Equal((3, 2), (match.HomeGoals, match.AwayGoals));
        Assert.Equal(2, marks);
    }
}
