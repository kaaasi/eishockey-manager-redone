using System.Buffers.Binary;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur hok-import, Befunde des Reviews zu PR #670 (<c>docs/reviews/2026-11-09-integration-hok-import-r14.md</c>): R14HI-02
/// (laufende Stadionausbauten), R14HI-03 (Werbevertrag mit Betrag, der nicht durch 3 teilbar ist), R14HI-04 (Zeilen der Feldkarte
/// mit Status B/A, die der Import nicht übernahm).
/// </summary>
public sealed class OriginalImportReviewR14Tests
{
    private const int Seed = 4711;

    /// <summary>R14HI-02: FILE43 – Sitzkomfort Stufe 0 mit Ziel 1 und 8 Restwochen (Manager-Satz +0x117 + 2 · 3, +0x128 + 3); die 370.000 DM sind schon abgebucht.</summary>
    [GameDataFact]
    public void Import_StadiumConstruction_File43()
    {
        var (_, game, _) = Import("FILE43.HOK");
        var stadium = game.Finance(game.Managers[0]).Stadium;
        var construction = Assert.Single(stadium.Constructions);
        Assert.Equal((StadiumProject.Feature, StadiumFeature.Sitzkomfort, 1, 8), (construction.Project, construction.Feature, construction.Target, construction.WeeksLeft));
        Assert.Equal(0, stadium.Level(StadiumFeature.Sitzkomfort));
        Assert.True(stadium.IsUnderConstruction(StadiumProject.Feature, StadiumFeature.Sitzkomfort));
        Assert.Equal(668_860, game.Managers[0].Money);
    }

    /// <summary>R14HI-02: FILE44A – Gastronomie Stufe 2 mit Ziel 3 (Restaurant), noch 1 Woche; das Kino ist fertig (Stufe 2, kein Ziel).</summary>
    [GameDataFact]
    public void Import_StadiumConstruction_File44A()
    {
        var (_, game, _) = Import("FILE44A.HOK");
        var stadium = game.Finance(game.Managers[0]).Stadium;
        var construction = Assert.Single(stadium.Constructions);
        Assert.Equal((StadiumProject.Feature, StadiumFeature.Gastronomie, 3, 1), (construction.Project, construction.Feature, construction.Target, construction.WeeksLeft));
        Assert.Equal(2, stadium.Level(StadiumFeature.Gastronomie));
        Assert.Equal(2, stadium.Level(StadiumFeature.Kino));
        Assert.False(stadium.IsUnderConstruction(StadiumProject.Feature, StadiumFeature.Kino));
    }

    /// <summary>Ohne Baustelle (FILE41, FILE44) bleibt die Liste leer – auch nach dem Nachspielen.</summary>
    [GameDataTheory]
    [InlineData("FILE41.HOK")]
    [InlineData("FILE44.HOK")]
    public void Import_NoConstruction_WhenOriginalHasNone(string file)
    {
        var (_, game, _) = Import(file);
        Assert.Empty(game.Finance(game.Managers[0]).Stadium.Constructions);
    }

    /// <summary>
    /// R14HI-03: Werbevertrag mit 1.000.001 Drittel-DM (nicht durch 3 teilbar), gültige Prüfsummen (im Test über
    /// <see cref="OriginalSavePatcher.Encode"/> erzeugt). Vorher warf <c>ApplySponsoring</c> eine <c>KeyNotFoundException</c>, die
    /// <c>TryImport</c> nicht fing; jetzt lädt der Stand mit dem Vertrag (Betrag / 3 abgerundet).
    /// </summary>
    [GameDataFact]
    public void TryImport_SponsorContractAmountNotDivisibleByThree_Loads()
    {
        var original = File.ReadAllBytes(OriginalImportTests.OriginalSave("FILE05.HOK"));
        var save = OriginalSaveGame.Read(original);
        var contracts = save.Block("C15B");
        BinaryPrimitives.WriteInt32LittleEndian(contracts.AsSpan(0), 1_000_001); // Fläche 0 (Bande), Manager 0
        contracts[4] = 0; // Partner
        contracts[5] = 6; // Restmonate
        var patched = OriginalSavePatcher.Encode(original.AsSpan(0, OriginalSaveGame.PlainLength), save);
        var database = ClubDatabase.Load(TestGameData.Load());
        Assert.True(OriginalSaveGame.Read(patched).ChecksumsValid);

        var result = OriginalImport.TryImport(patched, database, Seed);
        Assert.True(result.Success, result.Error);
        var contract = Assert.Single(game(result).Finance(game(result).Managers[0]).Sponsoring.Contracts);
        Assert.Equal((AdSlotKind.Bande, 0, 333_333, 6), (contract.Kind, contract.Sponsor, contract.MonthlyAmount, contract.MonthsLeft));
        Assert.Equal(new[] { 0 }, contract.Boards);
        static GameState game(OriginalImportResult r) => r.Game!;
    }

    /// <summary>R14HI-03: Ein Importfehler mit gültigen Prüfsummen ist ein Ladefehler ohne Prüfsummen-Kennzeichen (Zahl der Manager 0, DGROUP 0x386).</summary>
    [GameDataFact]
    public void TryImport_ImportError_IsReportedWithoutChecksumFlag()
    {
        var original = File.ReadAllBytes(OriginalImportTests.OriginalSave("FILE05.HOK"));
        var save = OriginalSaveGame.Read(original);
        save.Block("0386")[0] = 0;
        var result = OriginalImport.TryImport(OriginalSavePatcher.Encode(original.AsSpan(0, OriginalSaveGame.PlainLength), save),
            ClubDatabase.Load(TestGameData.Load()), Seed);
        Assert.False(result.Success);
        Assert.False(result.ChecksumError);
        Assert.Contains("0x386", result.Error);
    }

    internal static (OriginalSaveGame Save, GameState Game, int[] ClubOf) Import(string file)
    {
        var save = OriginalSaveGame.Read(File.ReadAllBytes(OriginalImportTests.OriginalSave(file)));
        var database = ClubDatabase.Load(TestGameData.Load());
        return (save, GameState.ImportOriginal(save, database, Seed), OriginalImport.ClubIndices(save, database));
    }
}
