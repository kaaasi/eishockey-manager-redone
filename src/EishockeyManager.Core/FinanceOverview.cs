namespace EishockeyManager.Core;

/// <summary>
/// Werte der Übersicht rechts im Bildschirm Finanzen, Zeilen „Untereinander“ und „Bank“ (A-Nummer #897).
/// <paramref name="ManagerLoans"/> / <paramref name="ManagerInterest"/> = Zeilen 3/4, <paramref name="BankLoan"/> /
/// <paramref name="BankInterest"/> = Zeilen 6/7.
/// </summary>
public readonly record struct FinanceOverviewValues(long ManagerLoans, long ManagerInterest, long BankLoan, long BankInterest);

public sealed partial class GameState
{
    /// <summary>
    /// Zeilen 3/4 und 6/7 der Finanzübersicht wie im Original ([ORIGINAL] em.exe Datei 0x57F96, Schleife über die Sätze i = 0–4 in
    /// 0x57ECF + 0x90 ff.): Zeile 3 = Σ der Darlehen mit den <b>anderen</b> Managern (DGROUP 0x97F6 + 4 · i, vorzeichenbehaftet:
    /// negativ = ich schulde i; Satz des eigenen Managers und nicht vorhandener Manager entfallen), Zeile 4 = Σ ihrer Monatszinsen
    /// (Datei 0x57C64: Darlehen · 25 / 1000, je Satz abgeschnitten), Zeile 6 = Darlehen Bank (Satz 4 = DGROUP 0x9806, ein Saldo),
    /// Zeile 7 = dessen Monatszins (0x57C64, Satz 4: Saldo · 1 / 100, bei negativem Saldo · 4 / 100, abgeschnitten).
    /// [APPROX] Das Remake führt Sparbuch und Kredit getrennt; als Saldo gilt <see cref="BankAccount.Balance"/> (Sparbuch − Kredit).
    /// </summary>
    public FinanceOverviewValues FinanceOverviewLoans(Manager manager)
    {
        var me = IndexOf(manager);
        long sum = 0, interest = 0;
        for (var i = 0; i < Managers.Count; i++)
        {
            if (i == me)
                continue;
            var loan = LoanAmount(me, i) - LoanAmount(i, me);
            sum += loan;
            interest += loan * 25 / 1000;
        }
        var balance = Finance(manager).Bank.Balance;
        return new FinanceOverviewValues(sum, interest, balance, balance * (balance < 0 ? 4 : 1) / 100);
    }
}
