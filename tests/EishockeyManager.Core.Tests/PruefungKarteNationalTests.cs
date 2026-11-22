using EishockeyManager.Core;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur pruefung-karte (Runde 22, #938 P3-2, Review #937): Gegenproben zur Spur national, die bei den Mutationen M6–M8 grün blieben.
/// </summary>
public class PruefungKarteNationalTests
{
    /// <summary>
    /// M6: Der Kaderplatz des Satzes 5 trägt die Form des Pools (Pool +0x17, A066/0x42EFF), nicht die des Vereinsplatzes; ohne
    /// Poolform bleibt die Platzform. Mutation <c>slot.Form = Form</c> in <c>CupsNational.Slot.cs</c> muss rot werden.
    /// </summary>
    [Fact]
    public void CopyForNationalSlot_TakesPoolForm_ElseSlotForm()
    {
        var game = TestGames.Create(5);
        var player = game.Clubs.First(c => c.Players.Count > 0).Players[0];
        player.Form = 61;
        player.PoolForm = 47;
        var withPool = player.CopyForNationalSlot();
        Assert.Equal(47, withPool.Form);
        Assert.Null(withPool.PoolForm);

        player.PoolForm = null;
        Assert.Equal(61, player.CopyForNationalSlot().Form);
    }

    /// <summary>
    /// M7: Die Abrechnung des WM-Spiels begrenzt die Belastung aller Plätze des Satzes 5 auf 50 … 150 (Datei 0x549A2). Mutation:
    /// <c>ClampStrain</c> in <c>CreditWorldCupSquad</c> entfernt → rot.
    /// </summary>
    [Fact]
    public void CreditWorldCupSquad_ClampsStrainOfAllSquadSlots()
    {
        var game = GameModeTests.Create(7, flag: false, globe: true, 5);
        game.Continue();
        game.AutoNominate();
        var coach = game.NationalCoach!;
        Assert.True(coach.Squad.Count >= 2);
        coach.Squad[0].Player.Strain = 400;
        coach.Squad[1].Player.Strain = 3;

        MatchEngine.CreditWorldCupSquad(coach, against: 0, side: 0, new MatchReport { HomeGoals = 1, AwayGoals = 0 });

        Assert.Equal(150, coach.Squad[0].Player.Strain);
        Assert.Equal(50, coach.Squad[1].Player.Strain);
    }

    /// <summary>
    /// M8: Der Import übernimmt die Zähler der Über-/Unterzahl des Satzes 5 (965B + 5 · 0x1AF + 0x177, sechs Wörter): FILE19
    /// `1,0,0,1,3,5`, FILE20 `1,0,0,90,3,5` (Beleg Review #937 P3-1). Mutation: Import der Zähler verworfen → rot.
    /// </summary>
    [GameDataTheory]
    [InlineData("FILE19.HOK", new[] { 1, 0, 0, 1, 3, 5 })]
    [InlineData("FILE20.HOK", new[] { 1, 0, 0, 90, 3, 5 })]
    public void Import_NationalCoachPowerPlayCounts_FromRecordFive(string file, int[] expected)
    {
        var game = GameState.ImportOriginal(
            Formats.OriginalSaveGame.Read(File.ReadAllBytes(OriginalImportTests.OriginalSave(file))),
            Formats.ClubDatabase.Load(TestGameData.Load()), 1993);

        Assert.NotNull(game.NationalCoach);
        Assert.Equal(expected, game.NationalCoach!.PowerPlayCounts);
    }
}
