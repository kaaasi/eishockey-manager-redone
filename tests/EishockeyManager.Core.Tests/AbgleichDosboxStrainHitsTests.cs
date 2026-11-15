using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;
using EishockeyManager.Tools;
using Xunit.Abstractions;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// #730 (Spur abgleich-dosbox-19, Teil 1, Fortsetzung 1; R19-E4 = A): Belastungstreffer des Managervereins an Spieltag 2 gegen
/// fünf unabhängige Läufe des Originals. DOSBox `runde-19` Sitzung 4: FILE50 fünfmal geladen, Spieltag 2 (EC RATINGEN – KÖLNER
/// EC) ohne Mausklick gespielt, gesichert als FILE66 … FILE70. Bericht: <c>docs/analyse/abgleich-dosbox.md</c> §4.5.
/// </summary>
/// <remarks>
/// Maß je Lauf: Summe über die Kölner Spieler des Zuwachses der Belastung (FILE50 → Stand danach); ein Spieler, der über 127
/// kippt (danach ≤ 60, vorher &gt; 60, Sägezahn 0x4BD56), zählt mit der Mindestzahl bis 128. Die Erholung im Training ist für
/// Original und Remake gleich (Stürmer 1 je Training, Abwehr 0) und fällt im Vergleich heraus. Gemessen (400 Saaten,
/// <c>belastung-staerke/hits.py</c>, mit Erholung): Original 64, 58, 57, 60, 53 (Mittel 58,4), FILE61 aus Sitzung 1 49; Remake
/// 60,9 ± 6,7.
/// </remarks>
public class AbgleichDosboxStrainHitsTests(ITestOutputHelper output)
{
    private const string Club = "KÖLNER EC";
    private static readonly string[] Runs = ["FILE66", "FILE67", "FILE68", "FILE69", "FILE70"];

    /// <summary>Kippgrenze: danach ≤ 60 bei vorher &gt; 60 gilt als über 127 gekippt (wie <c>hits.py</c>).</summary>
    private const int Tipped = 60;

    internal static int Hits(IReadOnlyDictionary<string, int> before, IReadOnlyDictionary<string, int> after) =>
        after.Where(p => before.ContainsKey(p.Key))
            .Sum(p => p.Value <= Tipped && before[p.Key] > Tipped ? 128 - before[p.Key] : p.Value - before[p.Key]);

    private static Dictionary<string, int> Strain(GameState game) =>
        game.Clubs.Single(c => c.Name == Club).Players.ToDictionary(p => p.Name, p => p.Strain);

    /// <summary>
    /// Mittel der fünf Original-Läufe gegen das Mittel des Remakes über 30 Saaten: Abstand höchstens zwei Standardfehler
    /// (s · √(1/5 + 1/30), s = Streuung des Remakes). Eine um ein Fünftel kleinere Trefferrate (Mittel um 49 wie FILE61, Vorschlag
    /// §4.4) fiele heraus; gemessen liegt der Abstand bei rund 2,5 Treffern, die Grenze bei rund 6,5.
    /// </summary>
    [GameDataFact]
    public void File50_FiveRuns_StrainHitsMeanWithinRemakeSpread()
    {
        var database = ClubDatabase.Load(TestGameData.Load());
        var start = Strain(HokCompare.Import(OriginalImportTests.OriginalSave("FILE50.HOK"), database, HokCompare.DefaultSeed));
        var original = Runs
            .Select(f => Hits(start, Strain(HokCompare.Import(OriginalImportTests.OriginalSave(f + ".HOK"), database,
                HokCompare.DefaultSeed))))
            .ToArray();

        var remake = new List<int>();
        for (var seed = 1; seed <= 30; seed++)
        {
            var game = HokCompare.Import(OriginalImportTests.OriginalSave("FILE50.HOK"), database, seed);
            var target = HokCompare.Import(OriginalImportTests.OriginalSave(Runs[seed % Runs.Length] + ".HOK"), database, seed);
            Assert.Null(HokCompare.PlayTo(game, target));
            remake.Add(Hits(start, Strain(game)));
        }

        var mean = remake.Average();
        var spread = Math.Sqrt(remake.Sum(h => (h - mean) * (h - mean)) / (remake.Count - 1));
        var limit = 2 * spread * Math.Sqrt(1.0 / original.Length + 1.0 / remake.Count);
        output.WriteLine($"Original {string.Join(", ", original)} (Mittel {original.Average():F1}); Remake {mean:F1} ± {spread:F1}" +
                         $" ({remake.Min()}–{remake.Max()}); Grenze ±{limit:F1}");
        Assert.All(original, h => Assert.InRange(h, remake.Min() - 5, remake.Max() + 5));
        Assert.InRange(original.Average() - mean, -limit, limit);
    }

    /// <summary>Maß ohne Originaldaten: Zuwachs, gekippte Spieler mit der Mindestzahl bis 128, Erholung negativ.</summary>
    [Fact]
    public void Hits_CountsGainAndTippedPlayersToTheBound()
    {
        var before = new Dictionary<string, int> { ["A"] = 100, ["B"] = 123, ["C"] = 70, ["D"] = 50 };
        var after = new Dictionary<string, int> { ["A"] = 104, ["B"] = 50, ["C"] = 69, ["D"] = 52 };
        // A +4, B gekippt (mindestens 128 − 123 = 5), C −1 (Training, ohne Einsatz), D +2 (unter der Kippgrenze gestartet).
        Assert.Equal(10, Hits(before, after));
    }
}
