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

    /// <summary>Zinsen pro Monat: positiv = Einnahme (Sparbuch), negativ = Ausgabe (Kredit).</summary>
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
    /// Geld von der Bank holen: zuerst vom Sparbuch abheben, den Rest als Kredit (bis zum Kreditrahmen).
    /// Liefert den tatsächlich gutgeschriebenen Betrag.
    /// </summary>
    public long WithdrawFromBank(Manager manager, long amount)
    {
        if (amount <= 0)
            return 0;
        var bank = Finance(manager).Bank;
        var fromSavings = Math.Min(amount, bank.Savings);
        var fromCredit = Math.Min(amount - fromSavings, FinanceRules.CreditLimit - bank.Credit);
        fromCredit = Math.Max(0, fromCredit);
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

    /// <summary>Höchstbetrag, der von der Bank geholt werden kann (Sparbuch plus freier Kreditrahmen).</summary>
    public long MaxWithdrawal(Manager manager)
    {
        var bank = Finance(manager).Bank;
        return bank.Savings + Math.Max(0, FinanceRules.CreditLimit - bank.Credit);
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
    /// Liefert den bewegten Betrag.
    /// </summary>
    public long PayToManager(int from, int to, long amount)
    {
        var repaid = RepayManagerLoan(from, to, amount);
        return repaid + LendBetweenManagers(from, to, amount - repaid);
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

    /// <summary>Verleiht Geld von einem Manager an einen anderen (nach Annahme des Angebots).</summary>
    public long LendBetweenManagers(int lender, int borrower, long amount)
    {
        var from = Managers[lender];
        amount = Math.Min(amount, Math.Max(0, from.Money));
        if (amount <= 0 || lender == borrower)
            return 0;
        var loan = ManagerLoans.FirstOrDefault(l => l.Lender == lender && l.Borrower == borrower);
        if (loan is null)
            ManagerLoans.Add(loan = new ManagerLoan { Lender = lender, Borrower = borrower });
        loan.Amount += amount;
        from.Money -= amount;
        Managers[borrower].Money += amount;
        return amount;
    }

    /// <summary>Tilgt Schulden bei einem anderen Manager.</summary>
    public long RepayManagerLoan(int borrower, int lender, long amount)
    {
        var loan = ManagerLoans.FirstOrDefault(l => l.Lender == lender && l.Borrower == borrower);
        if (loan is null)
            return 0;
        var debtor = Managers[borrower];
        amount = Math.Min(Math.Min(amount, loan.Amount), Math.Max(0, debtor.Money));
        if (amount <= 0)
            return 0;
        loan.Amount -= amount;
        debtor.Money -= amount;
        Managers[lender].Money += amount;
        if (loan.Amount == 0)
            ManagerLoans.Remove(loan);
        return amount;
    }

    public int IndexOf(Manager manager)
    {
        for (var i = 0; i < Managers.Count; i++)
            if (ReferenceEquals(Managers[i], manager))
                return i;
        throw new ArgumentException("Manager gehört nicht zu diesem Spiel.", nameof(manager));
    }

    /// <summary>Monatliche Zinsen für Bank und Manager-Kredite.</summary>
    private void PayInterest()
    {
        foreach (var manager in Managers)
        {
            var bank = Finance(manager).Bank;
            var savingsInterest = (long)Math.Round(bank.Savings * FinanceRules.SavingsRate);
            bank.Savings += savingsInterest;
            Finance(manager).Book.Add(FinanceCategory.Zinsen, savingsInterest);
            Book(manager, FinanceCategory.Zinsen, -(long)Math.Round(bank.Credit * FinanceRules.CreditRate));
        }
        foreach (var loan in ManagerLoans)
        {
            var interest = (long)Math.Round(loan.Amount * FinanceRules.ManagerLoanRate);
            Book(Managers[loan.Borrower], FinanceCategory.ManagerKredite, -interest);
            Book(Managers[loan.Lender], FinanceCategory.ManagerKredite, interest);
        }
    }
}
