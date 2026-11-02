namespace EishockeyManager.Core;

/// <summary>
/// Monatliche Grundeinnahmen und Grundausgaben nach dem Original (Spur <c>finanzen-2</c>, Belege in
/// <c>docs/finanzen-rekonstruktion.md</c>). Das Original rechnet in Drittel-DM; hier wird jede Position in
/// Drittel-DM gebildet und erst zum Buchen durch 3 geteilt (abgeschnitten wie die Anzeige des Originals).
/// </summary>
public static partial class FinanceRules
{
    /// <summary>Interne Geldeinheit des Originals: ein Drittel DM (Ausgabefunktion 99F:02D8).</summary>
    public const int ThirdsPerMark = 3;

    /// <summary>
    /// Startkonto in DM. [ORIGINAL] em.exe 0x29BD5: 1.200.000 + 150.000 · (5 − Level) Drittel
    /// = 400.000 + 50.000 · (5 − Level) DM (Karriere-Video Level 3: 500.000 DM, UI-Video Level 5: 400.000 DM).
    /// </summary>
    public static long StartMoney(int level) =>
        ToMarks(1_200_000L + 150_000L * (ManagerSetup.LevelCount - 1 - level));

    /// <summary>
    /// [ORIGINAL] em.exe Datei 0x3E819 ff.: Sockel der Monatsausgaben 130.000 Drittel minus 10.000 je Stufe
    /// „5 − Level“ minus 20 × Ligawert (DGROUP 0x52C2: 2400, 1800, 990 für Bundesliga, 2. Liga, Oberliga).
    /// </summary>
    private static readonly long[] LeagueExpenseFactor = [2400, 1800, 990];

    /// <summary>
    /// [ORIGINAL] 0x3E9A1 ff.: 175.000 × (Preisindex − 25) / 100 × (2 − Start-Liga) Drittel; Preisindex DGROUP 0x2BC = 100.
    /// In der 2. Liga werden zusätzlich 60.000 Drittel abgezogen (0x3E9F0).
    /// </summary>
    public const int PriceIndex = 100;

    /// <summary>Grundausgaben pro Monat in Drittel-DM (ohne Gehälter, Stadion, Jugend). [ORIGINAL] 0x3E811.</summary>
    public static long MonthlyBaseExpenseThirds(int level, League league)
    {
        var tier = (int)league;
        var thirds = 130_000L - 10_000L * (ManagerSetup.LevelCount - 1 - level) - 20L * LeagueExpenseFactor[tier];
        thirds += 175_000L * (PriceIndex - 25) / 100 * (2 - tier);
        if (league == League.ZweiteLiga)
            thirds -= 60_000;
        return thirds;
    }

    /// <summary>
    /// Grundeinnahmen pro Monat in Drittel-DM. [ORIGINAL] 0x3E657: 5 × Spielerzahl × Summe der vier Teamwerte
    /// (Kondition und Technik, Abwehr und Sturm; Vereinsfelder 0x18–0x1B). Belegt am Video: ESC Wolfsburg
    /// 20 Spieler, Teamwerte 122 → 12.200 Drittel = 4.066 DM; Berliner SC 119 → 3.966 DM.
    /// </summary>
    public static long MonthlyBaseIncomeThirds(int players, int teamValueSum) => 5L * players * teamValueSum;

    /// <summary>
    /// Summe der vier Teamwerte eines Vereins. [ORIGINAL] 0x3E657 liest die Vereinsfelder 0x18–0x1B (Kondition und
    /// Technik je Abwehr und Sturm), nicht die Spieler (Spur geld Teil 2, A035); im Remake Datenbankstärke + Abweichung
    /// (<see cref="MatchEngine.TeamValuesOf"/>, Lader 0x2EF1B, Wanderung 0x9D1B). [APPROX] Vor dem ersten Spiel eines
    /// Spielstands sind die Abweichungen noch nicht gezogen (siehe <see cref="MatchEngine.LoadTeamValues"/>) und zählen 0;
    /// für den Sturm gilt wie dort die Vereinsstärke (Datenbankbyte 0).
    /// </summary>
    public static int TeamValueSum(GameState game, Club club)
    {
        _ = game;
        var v = MatchEngine.TeamValuesOf(club);
        return 4 * club.Strength + v.DefenseCondition + v.AttackCondition + v.DefenseTechnique + v.AttackTechnique;
    }

    /// <summary>Drittel-DM → DM wie die Anzeige des Originals (abgeschnitten, Vorzeichen bleibt).</summary>
    public static long ToMarks(long thirds) => thirds / ThirdsPerMark;

    /// <summary>
    /// Trainingsanteil der Anzeige „Ausgaben“ in Drittel-DM aus den Kosten je Trainingstag X (Managerfeld 0x97EE).
    /// [ORIGINAL] em.exe 0x3E86B–0x3E8B5 (nur mit Flag 1): X um 3 nach links geschoben (0:0x52A) plus X/2, also
    /// 8,5 Trainingstage = 4,25 Wochen à zwei Trainingstage (unabhängige Nachprüfung, Runde 5).
    /// </summary>
    public static long MonthlyTrainingExpenseThirds(long costPerTrainingDayThirds) =>
        8 * costPerTrainingDayThirds + costPerTrainingDayThirds / 2;

    /// <summary>
    /// Grenze des Kontostands in DM. [ORIGINAL] em.exe 0x3F10E–0x3F165: am Monatsabschluss auf ±500.000.000 Drittel
    /// begrenzt (unabhängige Nachprüfung, Runde 5).
    /// </summary>
    public const long BalanceLimit = 500_000_000L / ThirdsPerMark;
}

public sealed partial class ManagerFinance
{
    /// <summary>
    /// Liga des Vereins als „Stufe“ der Kosten ([ORIGINAL] Manager-Feld 0x9746): bei Spielbeginn gesetzt und am
    /// Saisonende aus dem Vereinsindex neu bestimmt (Datei 0x563C0), also die Liga der kommenden Saison.
    /// </summary>
    public League CostLeague { get; set; } = League.Oberliga;

    /// <summary>
    /// Zinskonto in Drittel-DM: täglich aufgelaufene Zinsen (Sparbuch, Kredite, Überziehung), gebucht am
    /// Monatsletzten (#134). [ORIGINAL] DGROUP 0x2A0 + 4 × Manager, Tagesabschluss 0x3ED01, Monatsabschluss 0x3F0DA.
    /// </summary>
    public long InterestAccrualThirds { get; set; }

    /// <summary>Anteil des Zinskontos, der auf Manager-Kredite entfällt (nur für das Finanzbuch).</summary>
    public long ManagerLoanAccrualThirds { get; set; }
}

public sealed partial class GameState
{
    /// <summary>Spieler, für die der Manager Gehalt zahlt: Kader (Spieler auf dem Transfermarkt bleiben im Kader).</summary>
    private static int PaidPlayers(Club club) => club.Players.Count;

    /// <summary>Grundeinnahmen des Monats in DM (Teamwerte × Spielerzahl). [ORIGINAL] 0x3E657.</summary>
    public long MonthlyBaseIncome(Manager manager)
    {
        var club = Clubs[manager.ClubIndex];
        return FinanceRules.ToMarks(FinanceRules.MonthlyBaseIncomeThirds(PaidPlayers(club), FinanceRules.TeamValueSum(this, club)));
    }

    /// <summary>Grundausgaben des Monats in DM (Level, Start-Liga). [ORIGINAL] 0x3E811.</summary>
    public long MonthlyBaseExpenses(Manager manager) =>
        FinanceRules.ToMarks(FinanceRules.MonthlyBaseExpenseThirds(manager.Level, Finance(manager).CostLeague));

    /// <summary>
    /// Zinsertrag auf Forderungen pro Monat in DM, wie die Anzeige „Einnahmen“: 2,5 % auf Manager-Kredite,
    /// 1 % auf das Sparbuch. [ORIGINAL] 0x3E73B ff. (Flag 1).
    /// </summary>
    public long MonthlyReceivableInterest(Manager manager)
    {
        var index = IndexOf(manager);
        var managers = ManagerLoans.Where(l => l.Lender == index).Sum(l => l.Amount);
        return managers * 25 / 1000 + Finance(manager).Bank.Savings * 10 / 1000;
    }

    /// <summary>
    /// Kreditzinsen pro Monat in DM, wie die Anzeige „Ausgaben“: 4 % Bankkredit, 2,5 % Manager-Kredite.
    /// [ORIGINAL] 0x57768 (Modus 1) mit 0x57C64.
    /// </summary>
    public long MonthlyDebtInterest(Manager manager)
    {
        var index = IndexOf(manager);
        var managers = ManagerLoans.Where(l => l.Borrower == index).Sum(l => l.Amount);
        return managers * 25 / 1000 + Finance(manager).Bank.Credit * 4 / 100;
    }

    /// <summary>
    /// Täglicher Zinslauf: (Zinsertrag [2 % Manager, 1 % Sparbuch] − Kreditzinsen − 10 % eines negativen
    /// Kontostands) / (Tage des Monats + 1) in Drittel-DM aufs Zinskonto. [ORIGINAL] 0x3ED4F–0x3EE6B.
    /// </summary>
    private void AccrueDailyInterest(DateOnly date)
    {
        var days = DateTime.DaysInMonth(date.Year, date.Month) + 1;
        for (var i = 0; i < Managers.Count; i++)
        {
            var manager = Managers[i];
            var finance = Finance(manager);
            var receivables = ManagerLoans.Where(l => l.Lender == i).Sum(l => l.Amount) * 2 / 100;
            var managerDebts = ManagerLoans.Where(l => l.Borrower == i).Sum(l => l.Amount) * 25 / 1000;
            var bank = finance.Bank.Savings * 1 / 100 - finance.Bank.Credit * 4 / 100;
            var overdraft = manager.Money < 0 ? manager.Money / 10 : 0;
            var managerPart = (receivables - managerDebts) * FinanceRules.ThirdsPerMark / days;
            var bankPart = (bank + overdraft) * FinanceRules.ThirdsPerMark / days;
            finance.ManagerLoanAccrualThirds += managerPart;
            finance.InterestAccrualThirds += managerPart + bankPart;
        }
    }

    /// <summary>Monatsletzter (#134, [ORIGINAL] 0x3F02C bei Tag = Monatslänge): Zinskonto buchen (Finanzbuch „Zinsen“ bzw. „Manager-Kredite“) und leeren.</summary>
    private void BookAccruedInterest(Manager manager)
    {
        var finance = Finance(manager);
        var total = FinanceRules.ToMarks(finance.InterestAccrualThirds);
        var managers = FinanceRules.ToMarks(finance.ManagerLoanAccrualThirds);
        if (managers != 0)
            Book(manager, FinanceCategory.ManagerKredite, managers);
        if (total - managers != 0)
            Book(manager, FinanceCategory.Zinsen, total - managers);
        finance.InterestAccrualThirds = 0;
        finance.ManagerLoanAccrualThirds = 0;
        // [ORIGINAL] 0x3F10E: Begrenzung nach Einnahmen, Ausgaben und Zinsen; keine Buchung (kein Aufwand/Ertrag).
        manager.Money = Math.Clamp(manager.Money, -FinanceRules.BalanceLimit, FinanceRules.BalanceLimit);
    }
}
