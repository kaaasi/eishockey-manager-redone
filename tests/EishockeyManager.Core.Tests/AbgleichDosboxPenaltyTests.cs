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

    /// <summary>
    /// Regressionstest #773 (rot vor dem Fix: Anteil 6,25 %): Die 5-Minuten-Strafe zieht lrandom(0, 15) direkt nach dem
    /// Strafwurf (Datei 0x358B7–0x3592D, keine Züge dazwischen). [ORIGINAL] lrandom (Datei 0xBFD5) läuft auf einem Zyklus von
    /// 15.625 Zuständen (x = (16 · x + 167) mod 4.000.000); aufeinanderfolgende Züge hängen zusammen. Über den Zyklus ist jede
    /// Strafe bei Einsatz 40 mit 12,55 %, bei 60 mit 11,90 % eine 5-Minuten-Strafe (Strafwahrscheinlichkeit 8,92 % bzw. 9,41 %
    /// wie mit unabhängigen Zügen).
    /// </summary>
    [Theory]
    [InlineData(40, 0.1255)]
    [InlineData(60, 0.1190)]
    public void PenaltyMinutes_FiveMinuteShare_LikeLrandomCycle(int effort, double expected)
    {
        var random = new Random(773);
        int penalties = 0, five = 0;
        for (var i = 0; i < 2_000_000; i++)
        {
            var minutes = OriginalMatch.PenaltyMinutes(effort, firstBoxSlotFree: true, random);
            if (minutes > 0)
                penalties++;
            if (minutes == 5)
                five++;
        }
        output.WriteLine($"Einsatz {effort}: Strafen {penalties / 2e6:P2}, davon 5 Minuten {(double)five / penalties:P2}");
        Assert.InRange((double)five / penalties, expected - 0.004, expected + 0.004);
        Assert.InRange(penalties / 2e6, effort == 40 ? 0.0880 : 0.0930, effort == 40 ? 0.0905 : 0.0955);
    }

    /// <summary>
    /// Regressionstest #773 (rot vor dem Fix: Original 1447 über allen Saaten, Remake 1218–1377): Liga-Strafminuten aller Vereine
    /// FILE02 → FILE03 (drei Spieltage) liegen in der Verteilung des Remakes über 12 Saaten.
    /// </summary>
    [GameDataFact]
    public void File02To03_LeaguePenaltyMinutes_WithinRemakeSpread()
    {
        const int original = 1447; // [ORIGINAL] FILE03, Summe LeaguePenaltyMinutes (docs/analyse/belastung-staerke.md §8)
        var database = ClubDatabase.Load(TestGameData.Load());
        var sums = new List<int>();
        for (var seed = 1; seed <= 12; seed++)
        {
            var game = HokCompare.Import(OriginalImportTests.OriginalSave("FILE02.HOK"), database, seed);
            var target = HokCompare.Import(OriginalImportTests.OriginalSave("FILE03.HOK"), database, seed);
            Assert.Null(HokCompare.PlayTo(game, target));
            sums.Add(game.Clubs.Sum(c => c.LeaguePenaltyMinutes));
        }
        sums.Sort();
        output.WriteLine($"Original {original}, Remake {string.Join(" ", sums)}, Mittel {sums.Average():F1}");
        Assert.True(sums.Count(s => s <= original) >= 1 && sums.Count(s => s >= original) >= 1,
            $"Original {original}, Remake {sums[0]}…{sums[^1]}");
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
