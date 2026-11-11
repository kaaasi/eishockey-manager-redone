using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;
using EishockeyManager.Tools;
using Xunit.Abstractions;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// #773 (Spur abgleich-rest, Fortsetzung 3): Liga-Strafminuten nach #772. [ORIGINAL] Strafwurf Datei 0x35818–0x35CBD wie
/// <see cref="OriginalMinutes.Penalty"/> (lrandom(0, 12) = 0 oder lrandom(0, 110 − Einsatz 0x26) = 0, 5 Minuten mit 1/16 auf
/// Platz 0, Ligazähler nur bei Wettbewerb 7, Datei 0x35967–0x35986); Einsatz der Computervereine im Spiel ohne Manager ist der
/// Grundwert 40/60 aus 0x8365 (einzige Schreiber von 0x26 im Spiel: 0x8479, 0x9523 nur Managerpaarung, 0x38EFE Mausweg).
/// Abgleich (Bericht <c>docs/analyse/belastung-staerke.md</c> §8): FILE05 → FILE24 (4. Spieltag) Original +441, Remake
/// 390–497 (Mittel 445, Saaten 1–8); FILE16A → FILE22 Original +430, Remake 399–467 (Mittel 437). Nur FILE02 → FILE03
/// (1.–3. Spieltag, Original 1447, Remake 1218–1377) liegt darüber – Zufall bzw. Zufallsquelle (lrandom, <c>[APPROX]</c>).
/// </summary>
public class AbgleichRestPenaltyTests(ITestOutputHelper output)
{
    /// <summary>
    /// Strafminuten eines Spieltags: Summe der Liga-Strafminuten aller Vereine FILE05 → FILE24 im Mittel über vier Saaten
    /// höchstens 8 % neben dem Original (+441). Rot mit dem Einsatz vor #772 (Grundwert + 20/15, Mittel rund 490).
    /// </summary>
    [GameDataFact]
    public void File05To24_LeaguePenaltyMinutes_LikeOriginal()
    {
        var before = Import("FILE05", 1).Clubs.Sum(c => c.LeaguePenaltyMinutes);
        var original = Import("FILE24", 1).Clubs.Sum(c => c.LeaguePenaltyMinutes) - before;
        Assert.Equal(441, original);

        var sums = new List<int>();
        foreach (var seed in new[] { 1, 2, 3, 4 })
        {
            var game = Import("FILE05", seed);
            Assert.Null(HokCompare.PlayTo(game, Import("FILE24", seed)));
            sums.Add(game.Clubs.Sum(c => c.LeaguePenaltyMinutes) - before);
        }
        var mean = sums.Average();
        output.WriteLine($"Original +{original}, Remake {string.Join(", ", sums)} (Mittel {mean:0.0})");
        Assert.InRange(mean, original * 0.92, original * 1.08);
    }

    private static GameState Import(string file, int seed) =>
        HokCompare.Import(OriginalImportTests.OriginalSave(file + ".HOK"), ClubDatabase.Load(TestGameData.Load()), seed);
}
