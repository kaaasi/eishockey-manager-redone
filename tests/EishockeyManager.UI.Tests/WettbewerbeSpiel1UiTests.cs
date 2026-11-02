using EishockeyManager.Core;
using EishockeyManager.UI.Screens.Competitions;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur wettbewerbe-spiel, Teil 1 (A275, A277): Punkte hinter dem Pokalergebnis = Marken des Spiels. [ORIGINAL] Anzeige
/// 0x32E23, je + 30 ein Punkt (Datei 0x338AB–0x338E3); zwei Punkte nach einem Tor in der Verlängerung (0x38402,
/// eisbaeren_f44_01330 „4: 3..“). Vorher: ein Punkt nur bei Penaltysieger, nach Verlängerung keiner.
/// </summary>
public class WettbewerbeSpiel1UiTests
{
    [Fact]
    public void DebCupRow_DotsFollowMarks()
    {
        var tie = new CupTie(3, 7);
        tie.Add(new KnockoutMatch(10, 3, 7, 4, 3), 2);              // Verlängerung
        tie.Add(new KnockoutMatch(12, 7, 3, 5, 6, PenaltyWinner: 3), 1); // Penaltyschießen, Endstand
        var row = DebCupOverview.Row(tie, [10, 12, 14], played: 2);
        Assert.Equal([new DebCupResult(4, 3, 2), new DebCupResult(6, 5, 1)], row.Results.Select(r => r!.Value));
    }

    [Fact]
    public void DebCupRow_OldGamesWithoutMarks_PenaltyWinnerGivesOneDot()
    {
        var tie = new CupTie(3, 7);
        tie.Games.Add(new KnockoutMatch(10, 3, 7, 2, 2, PenaltyWinner: 7));
        var row = DebCupOverview.Row(tie, [10, 12, 14], played: 1);
        Assert.Equal(1, row.Results[0]!.Value.Dots);
    }
}
