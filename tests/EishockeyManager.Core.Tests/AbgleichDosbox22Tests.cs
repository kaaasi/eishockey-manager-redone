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
}
