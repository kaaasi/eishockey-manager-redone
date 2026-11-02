namespace EishockeyManager.Core;

/// <summary>Bankverbindung eines Managers: Kredit und Sparbuch (Bildschirm Finanzen, Kasten „BANK“).</summary>
public sealed class BankAccount
{
    /// <summary>Offener Kredit (positiv = Schulden bei der Bank).</summary>
    public long Credit { get; set; }

    /// <summary>Guthaben auf dem Sparbuch.</summary>
    public long Savings { get; set; }

    /// <summary>Saldo bei der Bank: Sparbuch minus Kredit (Anzeige unter dem Bankbild).</summary>
    public long Balance => Savings - Credit;

    /// <summary>
    /// Zinsen pro Monat: positiv = Einnahme (Sparbuch), negativ = Ausgabe (Kredit). Gebucht werden sie über das
    /// Zinskonto des Tagesabschlusses (<c>FinanceMonthly.cs</c>) aufs Konto, nicht aufs Sparbuch.
    /// </summary>
    public long MonthlyInterest =>
        (long)Math.Round(Savings * FinanceRules.SavingsRate) - (long)Math.Round(Credit * FinanceRules.CreditRate);
}

/// <summary>Kredit zwischen zwei Managern („Untereinander“, nur mit mehreren Managern).</summary>
public sealed class ManagerLoan
{
    public required int Lender { get; init; }
    public required int Borrower { get; init; }
    public long Amount { get; set; }
}

public sealed partial class GameState
{
    /// <summary>Alle Kredite zwischen Managern.</summary>
    public List<ManagerLoan> ManagerLoans { get; } = [];

    /// <summary>
    /// Geld von der Bank holen: zuerst vom Sparbuch abheben, den Rest als Kredit (höchstens <see cref="MaxWithdrawal"/>).
    /// Liefert den tatsächlich gutgeschriebenen Betrag.
    /// </summary>
    public long WithdrawFromBank(Manager manager, long amount)
    {
        amount = Math.Min(amount, MaxWithdrawal(manager));
        if (amount <= 0)
            return 0;
        var bank = Finance(manager).Bank;
        var fromSavings = Math.Min(amount, bank.Savings);
        var fromCredit = Math.Max(0, Math.Min(amount - fromSavings, FinanceRules.CreditLimit - bank.Credit));
        bank.Savings -= fromSavings;
        bank.Credit += fromCredit;
        manager.Money += fromSavings + fromCredit;
        return fromSavings + fromCredit;
    }

    /// <summary>
    /// Geld zur Bank bringen: zuerst den Kredit tilgen, den Rest aufs Sparbuch. Nur vorhandenes Geld (kein Dispo).
    /// Liefert den tatsächlich eingezahlten Betrag.
    /// </summary>
    public long DepositToBank(Manager manager, long amount)
    {
        amount = Math.Min(amount, Math.Max(0, manager.Money));
        if (amount <= 0)
            return 0;
        var bank = Finance(manager).Bank;
        var repay = Math.Min(amount, bank.Credit);
        bank.Credit -= repay;
        bank.Savings += amount - repay;
        manager.Money -= amount;
        return amount;
    }

    /// <summary>
    /// Höchstbetrag, der in einer Buchung von der Bank geholt werden kann.
    /// [ORIGINAL] em.exe Datei 0x5977C–0x598DC (Auswahl Bank, links): 6.000.000 Drittel-DM minus Schuld bei der Bank, mindestens
    /// das Sparbuch – also max(2 Mio. DM − Kredit, Sparbuch), nicht Sparbuch plus freier Kreditrahmen (Teil 5, G4).
    /// </summary>
    public long MaxWithdrawal(Manager manager)
    {
        var balance = Finance(manager).Bank.Balance;
        return Math.Max(0, Math.Max(FinanceRules.CreditLimit - Math.Max(0, -balance), balance));
    }

    /// <summary>
    /// Vorgabe des Betragsbalkens für „von der Bank holen“: das Sparbuch, ohne Sparbuch keine Vorgabe.
    /// [ORIGINAL] em.exe Datei 0x59872–0x598DC (Darlehensfeld gegenüber der Bank &gt; 0).
    /// </summary>
    public long? WithdrawalDefault(Manager manager) => Finance(manager).Bank.Balance is > 0 and var savings ? savings : null;

    /// <summary>
    /// Höchstbetrag, der zur Bank gebracht werden kann: das eigene Konto, nur wenn es positiv ist.
    /// [ORIGINAL] em.exe Datei 0x598DF–0x5997B (Auswahl Bank, rechts).
    /// </summary>
    public static long MaxDeposit(Manager manager) => Math.Max(0, manager.Money);

    /// <summary>
    /// Vorgabe für „zur Bank bringen“: der Kredit, höchstens das Konto; ohne Kredit oder ohne Guthaben keine Vorgabe.
    /// [ORIGINAL] em.exe Datei 0x59994–0x59A12.
    /// </summary>
    public long? DepositDefault(Manager manager)
    {
        var credit = -Finance(manager).Bank.Balance;
        return manager.Money > 0 && credit > 0 ? Math.Min(credit, manager.Money) : null;
    }

    /// <summary>Forderung von <paramref name="self"/> an <paramref name="other"/> minus Schuld bei ihm (Darlehensfeld im Original).</summary>
    public long ManagerLoanNet(int self, int other) => LoanAmount(self, other) - LoanAmount(other, self);

    /// <summary>
    /// Höchstbetrag, den <paramref name="self"/> an <paramref name="other"/> zahlen kann (Tilgung und neue Forderung).
    /// [ORIGINAL] em.exe Datei 0x598DF–0x5997B, rechts: nur bei eigenem Konto &gt; 0, min(1 Mio. DM − was der andere mir
    /// schuldet, eigenes Konto); eine eigene Schuld beim anderen erhöht die Grenze nicht (Abzug nur, wenn das Darlehensfeld
    /// &gt; 0 ist, Datei 0x59924).
    /// </summary>
    public long MaxLendToManager(int self, int other)
    {
        var money = Managers[self].Money;
        if (self == other || money <= 0)
            return 0;
        return Math.Max(0, Math.Min(FinanceRules.ManagerLoanLimit - Math.Max(0, ManagerLoanNet(self, other)), money));
    }

    /// <summary>
    /// Höchstbetrag, den <paramref name="self"/> von <paramref name="other"/> holen kann (Forderung einziehen und leihen).
    /// [ORIGINAL] em.exe Datei 0x5977C–0x598DC, links: nur wenn das Konto des anderen &gt; 0 ist, min(1 Mio. DM − was ich ihm
    /// schulde, sein Konto); mindestens die eigene Forderung an ihn (die auch bei leerem Konto des anderen eingezogen wird).
    /// </summary>
    public long MaxBorrowFromManager(int self, int other)
    {
        if (self == other)
            return 0;
        var money = Managers[other].Money;
        var limit = money > 0 ? Math.Min(FinanceRules.ManagerLoanLimit - Math.Max(0, ManagerLoanNet(other, self)), money) : 0;
        return Math.Max(0, Math.Max(limit, ManagerLoanNet(self, other)));
    }

    /// <summary>
    /// Vorgabe für „von einem Mitmanager holen“: die eigene Forderung an ihn, sonst keine.
    /// [ORIGINAL] em.exe Datei 0x59872–0x598DC.
    /// </summary>
    public long? BorrowDefault(int self, int other) => ManagerLoanNet(self, other) is > 0 and var claim ? claim : null;

    /// <summary>
    /// Vorgabe für „an einen Mitmanager zahlen“: die eigene Schuld bei ihm, höchstens das eigene Konto; ohne Schuld oder
    /// ohne Guthaben keine. [ORIGINAL] em.exe Datei 0x59994–0x59A12.
    /// </summary>
    public long? LendDefault(int self, int other)
    {
        var debt = ManagerLoanNet(other, self);
        var money = Managers[self].Money;
        return money > 0 && debt > 0 ? Math.Min(debt, money) : null;
    }

    /// <summary>Offener Betrag, den <paramref name="borrower"/> dem <paramref name="lender"/> schuldet.</summary>
    public long LoanAmount(int lender, int borrower) =>
        ManagerLoans.Where(l => l.Lender == lender && l.Borrower == borrower).Sum(l => l.Amount);

    /// <summary>Forderungen eines Managers an andere Manager.</summary>
    public long ManagerReceivables(Manager manager)
    {
        var index = IndexOf(manager);
        return ManagerLoans.Where(l => l.Lender == index).Sum(l => l.Amount);
    }

    /// <summary>Schulden eines Managers bei anderen Managern.</summary>
    public long ManagerDebts(Manager manager)
    {
        var index = IndexOf(manager);
        return ManagerLoans.Where(l => l.Borrower == index).Sum(l => l.Amount);
    }

    /// <summary>
    /// Geld an einen anderen Manager geben: zuerst eigene Schulden bei ihm tilgen, den Rest verleihen.
    /// Liefert den bewegten Betrag. [ORIGINAL] em.exe Datei 0x59184–0x59266 (rechts): Konto −= v, Konto des anderen += v,
    /// eigenes Darlehensfeld += v, seines −= v.
    /// </summary>
    public long PayToManager(int from, int to, long amount)
    {
        amount = Math.Min(amount, MaxLendToManager(from, to));
        if (amount <= 0)
            return 0;
        ShiftLoan(from, to, amount);
        return amount;
    }

    /// <summary>
    /// Geld von einem Mitmanager holen: zuerst die eigene Forderung an ihn einziehen, den Rest leihen – im lokalen Spiel
    /// sofort und ohne seine Zustimmung (Entscheidung Nutzer E2, docs/analyse/re-geld.md). Liefert den bewegten Betrag.
    /// [ORIGINAL] em.exe Datei 0x59269–0x5932B (links): Konto += v, Konto des anderen −= v (darf negativ werden),
    /// eigenes Darlehensfeld −= v, seines += v; der Mitmanager wird nicht gefragt.
    /// </summary>
    public long BorrowFromManager(int self, int other, long amount)
    {
        amount = Math.Min(amount, MaxBorrowFromManager(self, other));
        if (amount <= 0)
            return 0;
        ShiftLoan(other, self, amount);
        return amount;
    }

    /// <summary>
    /// <paramref name="payer"/> zahlt <paramref name="amount"/> an <paramref name="payee"/>: zuerst sinkt die Schuld des
    /// Zahlers beim Empfänger, der Rest wird Forderung des Zahlers. Neutrale Umbuchung (kein Finanzbuch).
    /// </summary>
    private void ShiftLoan(int payer, int payee, long amount)
    {
        Managers[payer].Money -= amount;
        Managers[payee].Money += amount;
        var debt = ManagerLoans.FirstOrDefault(l => l.Lender == payee && l.Borrower == payer);
        if (debt is not null)
        {
            var repaid = Math.Min(amount, debt.Amount);
            debt.Amount -= repaid;
            amount -= repaid;
            if (debt.Amount == 0)
                ManagerLoans.Remove(debt);
        }
        if (amount <= 0)
            return;
        var claim = ManagerLoans.FirstOrDefault(l => l.Lender == payer && l.Borrower == payee);
        if (claim is null)
            ManagerLoans.Add(claim = new ManagerLoan { Lender = payer, Borrower = payee });
        claim.Amount += amount;
    }

    /// <summary>Zinsen pro Monat zwischen zwei Managern aus Sicht von <paramref name="self"/> (positiv = Einnahme).</summary>
    public long ManagerLoanInterest(int self, int other) =>
        (long)Math.Round(LoanAmount(self, other) * FinanceRules.ManagerLoanRate)
        - (long)Math.Round(LoanAmount(other, self) * FinanceRules.ManagerLoanRate);

    /// <summary>Forderungen des Managers an andere Manager minus seine Schulden bei ihnen.</summary>
    public long ManagerLoanBalance(Manager manager)
    {
        var index = IndexOf(manager);
        return ManagerLoans.Where(l => l.Lender == index).Sum(l => l.Amount)
             - ManagerLoans.Where(l => l.Borrower == index).Sum(l => l.Amount);
    }

    /// <summary>
    /// Verleiht Geld nach Annahme einer Kreditanfrage (<see cref="RequestLoan"/>; nur noch für den späteren Netzwerkmodus,
    /// Entscheidung E2). Wie <see cref="PayToManager"/>: eine Schuld des Verleihers beim anderen wird zuerst getilgt.
    /// </summary>
    public long LendBetweenManagers(int lender, int borrower, long amount) => PayToManager(lender, borrower, amount);

    public int IndexOf(Manager manager)
    {
        for (var i = 0; i < Managers.Count; i++)
            if (ReferenceEquals(Managers[i], manager))
                return i;
        throw new ArgumentException("Manager gehört nicht zu diesem Spiel.", nameof(manager));
    }
}
