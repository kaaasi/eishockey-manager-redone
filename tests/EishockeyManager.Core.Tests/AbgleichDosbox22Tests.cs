using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur abgleich-dosbox-22 (Runde 23): DOSBox-Aufnahme `runde-22` Sitzung 1 (Referenz-Repo, FILE75/FILE76). Teil 1 belegt, was
/// Original und Remake schon gleich behandeln; Teil 2 behebt die Abweichungen aus dem Bericht
/// <c>docs/analyse/abgleich-dosbox-22.md</c> (je ein Regressionstest unten).
/// </summary>
public class AbgleichDosbox22Tests
{
    private const int Seed = 4711;

    private static (OriginalSaveGame Original, OriginalSaveGame Exported, GameState Game) RoundTrip(string file)
    {
        var raw = OriginalExportTests.RawDatabase();
        var original = OriginalSaveGame.Read(File.ReadAllBytes(OriginalImportTests.OriginalSave(file)));
        var game = GameState.ImportOriginal(original, ClubDatabase.Parse(raw), Seed);
        return (original, OriginalSaveGame.Read(OriginalExport.Export(game, raw)), game);
    }

    private static int Int32(OriginalSaveGame save, string block, int offset) =>
        System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(save.Block(block).AsSpan(offset));

    private static (OriginalSaveGame Save, GameState Game) Import(string file)
    {
        var save = OriginalSaveGame.Read(File.ReadAllBytes(OriginalImportTests.OriginalSave(file)));
        var game = GameState.ImportOriginal(save, ClubDatabase.Load(TestGameData.Load()), Seed);
        return (save, game);
    }

    /// <summary>
    /// A613 (Import Sparbuch und Managerdarlehen): FILE76 – Spieler1 legte 100.000 DM aufs Sparbuch und lieh Spieler2
    /// 105.000 DM (Angabe Nutzer, Bild <c>file76_01</c>). [ORIGINAL] Satz 0 +0x19B: Eintrag 1 = +315.000, Eintrag 4 (Bank) =
    /// +300.000; Satz 1 Eintrag 0 = −315.000 (Drittel-DM). Der Import liest den positiven Bankeintrag als Sparbuch und den
    /// negativen Eintrag i im Satz m als Darlehen von i an m.
    /// </summary>
    [GameDataFact]
    public void File76_SavingsAndManagerLoan_ImportLikeOriginal()
    {
        var (_, game) = Import("FILE76.HOK");
        var bank = game.Finance(game.Managers[0]).Bank;
        Assert.Equal(100_000, bank.Savings);
        Assert.Equal(0, bank.Credit);
        Assert.Equal(0, game.Finance(game.Managers[1]).Bank.Balance);
        var loan = Assert.Single(game.ManagerLoans);
        Assert.Equal((0, 1, 105_000L), (loan.Lender, loan.Borrower, loan.Amount));
    }

    /// <summary>
    /// A629 (Leihe zwischen Managern, nur der Stand nach der Leihe): FILE76 – LOTH von Spieler1 (EV LANDSHUT) an Spieler2
    /// geliehen. [ORIGINAL] Kaderplatz Satz 1 Platz 22: +0x0B = 1, +0x0C = 0x80 | Satz des Verleihers, Gehalt 7.622 DM.
    /// </summary>
    [GameDataFact]
    public void File76_LoanBetweenManagers_ImportsLender()
    {
        var (_, game) = Import("FILE76.HOK");
        var lender = game.Clubs[game.Managers[0].ClubIndex];
        var borrower = game.Clubs[game.Managers[1].ClubIndex];
        var loth = Assert.Single(borrower.Players, p => p.Name == "LOTH");
        Assert.True(loth.OnLoan);
        Assert.Same(lender, loth.Loan!.Origin);
        Assert.Equal((1, 7622), (loth.ContractYears, loth.MonthlySalary));
        Assert.DoesNotContain(lender.Players, p => p.Name == "LOTH");
        Assert.Equal((21, 23), (lender.Players.Count, borrower.Players.Count));
    }

    /// <summary>
    /// A065 (Grundlage für Teil 2): Vor dem ersten Spieltag steht das Tabellenfeld DGROUP 0x8288 + 0x50 · Satz (das die
    /// Bestenliste 0x5D2DA liest) bei allen 36 Ligavereinen auf 0 (FILE75, neues Spiel); der Import übernimmt es.
    /// </summary>
    [GameDataFact]
    public void File75_TableFieldBeforeFirstMatchDay_IsZero()
    {
        var (save, game) = Import("FILE75.HOK");
        var table = save.Block("8257");
        for (var slot = 0; slot < 36; slot++)
            Assert.Equal(0, table[OriginalImport.TableField(0x8288, slot)]);
        Assert.Equal(0, game.PlayedMatchDays);
        Assert.All(game.Managers, m => Assert.Equal(0, game.Clubs[m.ClubIndex].StoredTableRank));
    }

    /// <summary>
    /// A065 (Teil 2): Vor dem ersten Spieltag rechnet die Bestenliste mit dem Tabellenfeld (0 im ersten Spieljahr), nicht mit
    /// Platzterm 0. [ORIGINAL] Datei 0x5D2CA–0x5D303: 25 · (36 − (Feld + Versatz + 1)); FILE75: beide Manager in der 2. Liga
    /// (Versatz 12) → 25 · 23 = 575 Rohpunkte.
    /// </summary>
    [GameDataFact]
    public void File75_HighscoreBeforeFirstMatchDay_UsesTableField()
    {
        var (_, game) = Import("FILE75.HOK");
        for (var manager = 0; manager < game.Managers.Count; manager++)
        {
            var input = HighscoreRules.Inputs(game, manager);
            Assert.Equal((0, 12), (input.Rank0, input.LeagueOffset));
        }
    }

    /// <summary>
    /// A402 (Export Bankseite): Satz 4 +0x18F (Konto der Bank) trägt den Bankeintrag des Managers mit gleichem Vorzeichen –
    /// FILE76 Sparbuch +300.000, FILE08 Kredit −2.103.000 Drittel-DM. Vorher schrieb der Export 0.
    /// </summary>
    [GameDataTheory]
    [InlineData("FILE76.HOK", 300_000)]
    [InlineData("FILE08.HOK", -2_103_000)]
    public void Export_BankRecordAccount_LikeOriginal(string file, int expected)
    {
        var (original, exported, _) = RoundTrip(file);
        var at = OriginalImport.MarketRecord * OriginalSaveGame.ManagerRecordLength + 0x18F;
        Assert.Equal(expected, Int32(original, "965B", at));
        Assert.Equal(expected, Int32(exported, "965B", at));
    }

    /// <summary>
    /// A614 (Bandenwerbung): FILE76 Spieler1 – Flächen 4 und 5 Vertrag Nr. 2 von JWE Service (Partner 0), Fläche 6 Nr. 1 von CCH
    /// (Partner 1); DGROUP 0x17C = 3. Der Import liest den Partner aus +7 (vorher +4: JWE-Banden als Partner 2), der Export schreibt
    /// +4 = Nummer, +7 = Partner und 0x17C (vorher +4 = +7 = Partner, 0x17C = 1).
    /// </summary>
    [GameDataFact]
    public void File76_BoardContracts_NumberAndPartnerLikeOriginal()
    {
        var (original, exported, game) = RoundTrip("FILE76.HOK");
        var contracts = game.Finance(game.Managers[0]).Sponsoring.Contracts;
        Assert.Equal([(1, new[] { 6 }), (0, new[] { 4, 5 })], contracts.Select(c => (c.Sponsor, c.Boards)).ToArray());
        Assert.Equal(3, original.Block("017C")[0]);
        Assert.Equal(3, exported.Block("017C")[0]);
        foreach (var area in new[] { 4, 5, 6 })
            foreach (var field in new[] { 4, 5, 6, 7, 8 })
                Assert.Equal(original.Block("C15B")[10 * area + field], exported.Block("C15B")[10 * area + field]);
    }

    /// <summary>
    /// A629 (Export der Leihe): LOTH (Pool 100) gehört im Original weiter dem Verleiher Spieler1 (+0x1A = 0), steht aber beim
    /// Verein des Entleihers (+0x1D). Vorher schrieb der Export den Entleiher (1). Ein zweiter Import behält die Leihe.
    /// </summary>
    [GameDataFact]
    public void File76_LoanedPlayer_PoolOwnerIsLender()
    {
        var (original, exported, _) = RoundTrip("FILE76.HOK");
        Assert.Equal(0, OriginalImport.PoolEntry(original, 100)[0x1A]);
        Assert.Equal(0, OriginalImport.PoolEntry(exported, 100)[0x1A]);
        Assert.Equal(OriginalImport.PoolEntry(original, 100)[0x1D], OriginalImport.PoolEntry(exported, 100)[0x1D]);

        var again = GameState.ImportOriginal(exported, ClubDatabase.Parse(OriginalExportTests.RawDatabase()), Seed);
        var loth = Assert.Single(again.Clubs[again.Managers[1].ClubIndex].Players, p => p.Name == "LOTH");
        Assert.Same(again.Clubs[again.Managers[0].ClubIndex], loth.Loan!.Origin);
    }

    /// <summary>
    /// Zugfolge im Export (Entscheidung Leitstelle, PR #964): DGROUP 0xA70D = 0xA70E an allen Originalständen; FILE75 (zwei
    /// Manager, Spieler1 am Zug) 0. Vorher Managerzahl − 1 = 1.
    /// </summary>
    [GameDataFact]
    public void File75_Export_TurnByteA70D_EqualsA70E()
    {
        var (original, exported, _) = RoundTrip("FILE75.HOK");
        Assert.Equal(0, original.Block("A70D")[0]);
        Assert.Equal(original.Block("A70D")[0], exported.Block("A70D")[0]);
        Assert.Equal(exported.Block("A70E")[0], exported.Block("A70D")[0]);
    }
}
