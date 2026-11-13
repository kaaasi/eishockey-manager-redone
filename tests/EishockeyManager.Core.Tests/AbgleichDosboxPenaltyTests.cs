using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;
using EishockeyManager.Tools;
using Xunit.Abstractions;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// #773 (Spur abgleich-dosbox, Teil 2): Einsatz (Vereinsfeld 0x26) und Liga-Strafminuten nach dem ersten Spieltag eines neuen
/// Spiels, DOSBox-Aufnahme `runde-17` FILE52 → FILE53 (ES WEIßWASSER, Bundesliga). Bericht: <c>docs/analyse/abgleich-dosbox.md</c> §2.
/// </summary>
public class AbgleichDosboxPenaltyTests(ITestOutputHelper output)
{
    /// <summary>
    /// [ORIGINAL] FILE53, Vereinssätze 0–51 (<c>emtool hok dump … far:9657</c>, Feld 0x26) ohne den Managerverein und ohne
    /// dessen Gegner: 40 × 20, 60 × 14, 0 × 16 (kein Spiel). Der Gegner EC HEDOS MÜNCHEN steht auf 0x26 = 60, Grundwert 0x2D = 40,
    /// Angriff 4 – den Einsatz der Managerpaarung ändert 0x939A je Minute nach dem Spielstand (im Remake je Saat 40 … 60).
    /// </summary>
    private static readonly Dictionary<int, int> OriginalEffort = new() { [0] = 16, [40] = 20, [60] = 14 };

    /// <summary>[ORIGINAL] FILE53: Summe der Liga-Strafminuten aller Vereine nach dem 1. Spieltag.</summary>
    private const int OriginalPenaltyMinutes = 456;

    /// <summary>
    /// Regressionstest Ursache trennen (Einsatz oder Zahl der Strafwürfe): Nach dem ersten Spieltag haben die Computervereine im
    /// Remake genau die Einsatzverteilung des Originals (Spielvorbereitung 0x8365, #772) – der Einsatz der ersten Spieltage ist
    /// nicht die Ursache von #773.
    /// </summary>
    [GameDataTheory]
    [InlineData(1993)]
    [InlineData(1)]
    [InlineData(2)]
    public void File52To53_ComputerClubEffort_LikeOriginal(int seed)
    {
        var game = PlayFirstMatchDay(seed);
        var managed = game.Managers.Select(m => m.ClubIndex).ToHashSet();
        var opponent = game.Clubs.Single(c => c.Name == "EC HEDOS MÜNCHEN");
        var effort = game.Clubs.Where(c => c.Index < 52 && !managed.Contains(c.Index) && c != opponent)
            .GroupBy(c => c.Tactics.MatchEffort).ToDictionary(g => g.Key, g => g.Count());
        output.WriteLine(string.Join(", ", effort.OrderBy(e => e.Key).Select(e => $"{e.Key} × {e.Value}")) +
            $"; Gegner {opponent.Tactics.MatchEffort}");
        Assert.Equal(OriginalEffort.OrderBy(e => e.Key), effort.OrderBy(e => e.Key));
        Assert.InRange(opponent.Tactics.MatchEffort, 40, 60);
    }

    /// <summary>
    /// Beleg #773: Die Liga-Strafminuten des ersten Spieltags (Original 456) liegen in der Verteilung des Remakes über 20 Saaten
    /// (mindestens zwei Saaten auf jeder Seite) – der Überschuss aus FILE02 → FILE03 (1447 zu 1218–1377) entsteht nicht am ersten
    /// Spieltag.
    /// </summary>
    [GameDataFact]
    public void File52To53_LeaguePenaltyMinutes_WithinRemakeSpread()
    {
        var sums = new List<int>();
        for (var seed = 1; seed <= 20; seed++)
            sums.Add(PlayFirstMatchDay(seed).Clubs.Sum(c => c.LeaguePenaltyMinutes));
        sums.Sort();
        output.WriteLine($"Original {OriginalPenaltyMinutes}, Remake {string.Join(" ", sums)}, Mittel {sums.Average():F1}");
        Assert.True(sums.Count(s => s <= OriginalPenaltyMinutes) >= 2 && sums.Count(s => s >= OriginalPenaltyMinutes) >= 2,
            $"Original {OriginalPenaltyMinutes}, Remake {sums[0]}…{sums[^1]}");
    }

    private static GameState PlayFirstMatchDay(int seed)
    {
        var database = ClubDatabase.Load(TestGameData.Load());
        var game = HokCompare.Import(OriginalImportTests.OriginalSave("FILE52.HOK"), database, seed);
        var target = HokCompare.Import(OriginalImportTests.OriginalSave("FILE53.HOK"), database, seed);
        Assert.Null(HokCompare.PlayTo(game, target));
        return game;
    }
}
