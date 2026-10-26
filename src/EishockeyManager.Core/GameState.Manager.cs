namespace EishockeyManager.Core;

/// <summary>Finanzdaten eines Managers (Paket „manager“). Weitere Teile in eigenen Finance*-Dateien.</summary>
public sealed partial class ManagerFinance
{
    public FinanceBook Book { get; } = new();
    public BankAccount Bank { get; } = new();

    /// <summary>Aktienbesitz, Index wie <see cref="StockMarket.Stocks"/>.</summary>
    public StockHolding[] Holdings { get; } = [new(), new(), new(), new()];

    public required Stadium Stadium { get; init; }

    /// <summary>Zuschauer und Einnahmen des letzten Heimspiels (für Statistik und Anzeige).</summary>
    public (int Spectators, long Income)? LastHomeGame { get; set; }
}

public sealed partial class Manager
{
    /// <summary>Finanzdaten; über <see cref="GameState.Finance"/> angelegt.</summary>
    public ManagerFinance? FinanceData { get; set; }
}

/// <summary>
/// Spielstand – Teil des Pakets „manager“: Finanzen, Börse und die tägliche Abrechnung.
/// </summary>
public sealed partial class GameState
{
    private StockMarket? _stocks;
    private Random? _financeRandom;

    /// <summary>Letzter abgerechneter Tag (−1 = noch keiner).</summary>
    public int FinanceDay { get; set; } = -1;

    /// <summary>
    /// Eigene Zufallsquelle der Finanzen, damit das Öffnen von Bildschirmen die Spielergebnisse nicht verändert.
    /// </summary>
    public Random FinanceRandom
    {
        get => _financeRandom ??= new Random(unchecked(Season * 7919 + Managers.Sum(m => m.ClubIndex * 31 + m.Head)));
        init => _financeRandom = value;
    }

    public StockMarket Stocks
    {
        get => _stocks ??= StockMarket.Create(FinanceRandom);
        set => _stocks = value;
    }

    /// <summary>Finanzdaten eines Managers (beim ersten Zugriff angelegt).</summary>
    public ManagerFinance Finance(Manager manager) =>
        manager.FinanceData ??= new ManagerFinance { Stadium = Stadium.ForLeague(Clubs[manager.ClubIndex].League) };

    /// <summary>Bucht einen Betrag auf das Konto und ins Finanzbuch.</summary>
    public void Book(Manager manager, FinanceCategory category, long amount)
    {
        manager.Money += amount;
        Finance(manager).Book.Add(category, amount);
    }

    /// <summary>[APPROX] Regelmäßige Einnahmen pro Monat (Sponsoren, Grundeinnahmen) – Anzeige „Einnahmen“.</summary>
    public long MonthlyIncome(Manager manager) =>
        FinanceRules.BaseIncome(Clubs[manager.ClubIndex].League) + SponsorIncome(manager);

    /// <summary>
    /// Regelmäßige Ausgaben pro Monat – Anzeige „Ausgaben“: Vertragsgehälter, Versicherungen, Unterhalt, Jugendförderung.
    /// </summary>
    public long MonthlyExpenses(Manager manager)
    {
        var club = Clubs[manager.ClubIndex];
        return FinanceRules.MonthlyWages(club) + FinanceRules.MonthlyInsurance(club)
            + Finance(manager).Stadium.MonthlyUpkeep + RecurringExpenses(manager);
    }

    /// <summary>
    /// Nach einem Spieltag: alle Tage seit der letzten Abrechnung bis heute abrechnen und die Heimspiele
    /// der Managervereine auswerten. Wird von <see cref="PlayNextMatchDay"/> aufgerufen.
    /// </summary>
    public void SettleFinances(IEnumerable<MatchResult> results)
    {
        AdvanceFinanceTo(Day);
        foreach (var result in results)
            SettleHomeGame(result);
    }

    /// <summary>Rechnet alle Tage bis einschließlich <paramref name="day"/> ab (Monats-, Wochen- und Tageswechsel).</summary>
    public void AdvanceFinanceTo(int day)
    {
        while (FinanceDay < day)
        {
            FinanceDay++;
            var date = SeasonCalendar.LayoutDate(FinanceDay);
            Stocks.Advance(FinanceRandom);
            ExpireMessages();
            if (FinanceDay > 0 && date.DayOfWeek == DayOfWeek.Monday)
                WeeklyFinances();
            if (FinanceDay > 0 && date.Day == 1)
                MonthlyFinances();
        }
    }

    /// <summary>Saisonwechsel für das Paket „saison“: Finanzbuch umstellen, Tageszähler zurücksetzen.</summary>
    public void StartFinanceSeason()
    {
        foreach (var manager in Managers)
            Finance(manager).Book.NewSeason();
        FinanceDay = -1;
    }

    private void SettleHomeGame(MatchResult result)
    {
        foreach (var manager in Managers.Where(m => m.ClubIndex == result.Home))
        {
            var finance = Finance(manager);
            var stadium = finance.Stadium;
            var spectators = Spectators(Clubs[result.Home], Clubs[result.Away], stadium, result.MatchDay, FinanceRandom);
            var tickets = (long)spectators * stadium.TicketPrice;
            var gastronomy = (long)spectators * stadium.GastronomyPerSpectator;
            Book(manager, FinanceCategory.Eintritt, tickets);
            if (gastronomy > 0)
                Book(manager, FinanceCategory.Gastronomie, gastronomy);
            finance.LastHomeGame = (spectators, tickets + gastronomy);
        }
    }

    private void WeeklyFinances()
    {
        foreach (var manager in Managers)
            WeeklyManagerFinances(manager);
    }

    private void MonthlyFinances()
    {
        foreach (var manager in Managers)
        {
            var club = Clubs[manager.ClubIndex];
            Book(manager, FinanceCategory.Gehaelter, -FinanceRules.MonthlyWages(club));
            var insurance = FinanceRules.MonthlyInsurance(club);
            if (insurance > 0)
                Book(manager, FinanceCategory.Versicherung, -insurance);
            Book(manager, FinanceCategory.Unterhalt, -Finance(manager).Stadium.MonthlyUpkeep);
            Book(manager, FinanceCategory.Sponsoren, FinanceRules.BaseIncome(club.League));
            MonthlyManagerFinances(manager);
        }
        PayInterest();
    }

    // Erweiterungspunkte für weitere Teile des Pakets (Werbung, Stadionbau, Jugend …).
    private long SponsorIncome(Manager manager) => Finance(manager).Sponsoring.MonthlyIncome;
    private long RecurringExpenses(Manager manager) => Finance(manager).YouthFunding;
    private void WeeklyManagerFinances(Manager manager)
    {
        if (AdvanceConstruction(manager) is { } done)
            ConstructionCompleted(manager, done);
        AdvanceTalentSearch(manager);
    }

    private void MonthlyManagerFinances(Manager manager)
    {
        SettleSponsoring(manager);
        SettleYouth(manager);
    }
}
