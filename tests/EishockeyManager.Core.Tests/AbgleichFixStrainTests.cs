using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;
using EishockeyManager.Tools;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur abgleich-fix, Teil 2 (#730): Belastung nach dem Training. [ORIGINAL] Datei 0x6B08–0x6B25: Belastung − Erholung als Byte,
/// dann Begrenzung 50 … 150 über Datei 0x4BD56, die das Byte mit <c>cbw</c> vorzeichenbehaftet liest: Ein Ergebnis ab 128 gilt als
/// negativ und wird 50. Belege FILE02→FILE03: MENDE 120, HOCK 115, SIKORA 117 (Reihen 1/2, viele Spielminuten) stehen danach auf 50,
/// BRANDL (Reihe 3) 118 → 119. Vorher rot: Das Remake begrenzte vorzeichenlos (Ergebnis bis 150).
/// </summary>
public class AbgleichFixStrainTests
{
    private static Player Defender(int strain) => new()
    {
        Name = "TEST", Role = PlayerRole.Defender, Condition = 50, Technique = 50, Form = 50, Age = 30, Energy = 80, Strain = strain,
    };

    private static int StrainAfterWeights(int strain)
    {
        var plan = new TrainingPlan();
        plan.Fill(TrainingKind.Weights); // Erholung −20 (TrainingTests.HardTraining_CostsEnergyAndBuildsCondition)
        var p = Defender(strain);
        Assert.Equal(-20, TrainingRules.Apply(p, plan, 3, new Random(1)).Recovery);
        return p.Strain;
    }

    [Theory]
    [InlineData(80, 100)]
    [InlineData(107, 127)]
    [InlineData(108, 50)] // 128 = −128 → 50
    [InlineData(120, 50)] // 140 = −116 → 50
    [InlineData(150, 50)]
    public void Training_StrainByteIsClampedSigned(int before, int after) => Assert.Equal(after, StrainAfterWeights(before));

    /// <summary>
    /// Abnahme #730: FILE02 → drei Spieltage. MENDE (120, Reihe 1) fällt wie in FILE03 bei jeder Saat auf höchstens 70 (Original 50);
    /// vorher blieb er über 120. Ob auch HOCK und SIKORA (115/117) die Grenze 128 vor einem Training erreichen, hängt vom Zufall
    /// der Spiele ab (Original ja, Remake je Saat).
    /// </summary>
    [GameDataTheory]
    [InlineData(1993)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(77)]
    public void File02_ThreeMatchDays_MendeFallsTo50(int seed)
    {
        var game = GameState.ImportOriginal(OriginalSaveGame.Read(File.ReadAllBytes(OriginalImportTests.OriginalSave("FILE02.HOK"))),
            ClubDatabase.Load(TestGameData.Load()), seed);
        for (var i = 0; i < 3; i++)
            TestSeason.PlayMatchDay(game);
        var club = game.Clubs[game.Managers[0].ClubIndex];
        Assert.InRange(club.Players.Single(p => p.Name == "MENDE").Strain, TrainingRules.MinStrain, 70);
        Assert.All(club.Players, p => Assert.InRange(p.Strain, TrainingRules.MinStrain, TrainingRules.MaxStrain));
    }

    public static TheoryData<string, string> Pairs() => AbgleichOriginalPaareTests.Pairs();

    /// <summary>
    /// Ersatz für den Vergleich je Spieler (Sägezahn, Phase zufällig): Mittel der Belastung je Managerverein nach dem Nachspielen
    /// höchstens 10 vom Original, bei den Saaten 1993, 1, 2, 3, 77 (gemessen: Abstand 0–7,8, FILE38→39).
    /// </summary>
    [GameDataTheory]
    [MemberData(nameof(Pairs))]
    public void Pairs_ManagerClubStrainMean_LikeOriginal(string a, string b)
    {
        var database = ClubDatabase.Load(TestGameData.Load());
        foreach (var seed in new[] { 1993, 1, 2, 3, 77 })
        {
            var start = HokCompare.Import(OriginalImportTests.OriginalSave(a + ".HOK"), database, seed);
            var target = HokCompare.Import(OriginalImportTests.OriginalSave(b + ".HOK"), database, seed);
            Assert.Null(HokCompare.PlayTo(start, target));
            foreach (var manager in target.Managers)
            {
                var original = target.Clubs[manager.ClubIndex].Players;
                if (original.Count == 0)
                    continue;
                var remake = start.Clubs[manager.ClubIndex].Players;
                Assert.InRange(remake.Average(p => p.Strain), original.Average(p => p.Strain) - 10, original.Average(p => p.Strain) + 10);
            }
        }
    }
}
