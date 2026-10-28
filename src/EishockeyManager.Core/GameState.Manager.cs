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
    private GameRandom? _financeRandom;

    /// <summary>Letzter abgerechneter Tag (−1 = noch keiner).</summary>
    public int FinanceDay { get; set; } = -1;

    /// <summary>
    /// Eigene Zufallsquelle der Finanzen, damit das Öffnen von Bildschirmen die Spielergebnisse nicht verändert.
    /// </summary>
    public GameRandom FinanceRandom
    {
        get => _financeRandom ??= new GameRandom(unchecked(Season * 7919 + Managers.Sum(m => m.ClubIndex * 31 + m.Head)));
        init => _financeRandom = value;
    }

    public StockMarket Stocks
    {
        get => _stocks ??= StockMarket.Create(FinanceRandom);
        set => _stocks = value;
    }

    /// <summary>Finanzdaten eines Managers (beim ersten Zugriff angelegt).</summary>
    public ManagerFinance Finance(Manager manager) =>
        manager.FinanceData ??= new ManagerFinance
        {
            Stadium = Stadium.ForLeague(Clubs[manager.ClubIndex].League),
            StartLeague = Clubs[manager.ClubIndex].League ?? League.Oberliga,
        };

    /// <summary>Bucht einen Betrag auf das Konto und ins Finanzbuch.</summary>
    public void Book(Manager manager, FinanceCategory category, long amount)
    {
        manager.Money += amount;
        Finance(manager).Book.Add(category, amount);
    }

    /// <summary>
    /// Anzeige „Einnahmen“ (Werbung, Statistik): Grundeinnahmen, Werbeverträge, Zinsertrag auf Forderungen.
    /// [ORIGINAL] em.exe 0x3E657 mit Flag 1 (Spur finanzen-2).
    /// </summary>
    public long MonthlyIncome(Manager manager) =>
        MonthlyBaseIncome(manager) + SponsorIncome(manager) + MonthlyReceivableInterest(manager);

    /// <summary>
    /// Anzeige „Ausgaben“: Grundausgaben, Gehälter, Versicherungen, Stadionunterhalt, Jugendförderung, 5/8 der
    /// wöchentlichen Trainingskosten, Kreditzinsen und 10 % eines negativen Kontostands. [ORIGINAL] 0x3E811 mit Flags 1, 1.
    /// </summary>
    public long MonthlyExpenses(Manager manager)
    {
        var club = Clubs[manager.ClubIndex];
        var training = manager.Training.WeeklyCost;
        var expenses = MonthlyBaseExpenses(manager) + FinanceRules.MonthlyWages(club) + FinanceRules.MonthlyInsurance(club)
            + Finance(manager).Stadium.MonthlyUpkeep + RecurringExpenses(manager)
            + training / 8 + training / 2 + MonthlyDebtInterest(manager)
            + (manager.Money < 0 ? -manager.Money / 10 : 0);
        return Math.Max(0, expenses);
    }

    /// <summary>
    /// Nach einem Ligaspieltag: alle Tage seit der letzten Abrechnung bis heute abrechnen und die Heimspiele
    /// der Managervereine auswerten. Wird von <see cref="PlayNextMatchDay"/> aufgerufen.
    /// </summary>
    public void SettleFinances(IEnumerable<MatchResult> results)
    {
        AdvanceFinanceTo(Day);
        foreach (var result in results)
            SettleHomeMatch(result.Home, result.Away, result.MatchDay);
    }

    /// <summary>
    /// Gemeinsame Spielabrechnung für alle Wettbewerbe (Liga, Play-Offs, Aufstiegsrunde, Pokale über
    /// <see cref="ISeasonCompetition"/>): Zuschauer und Eintritt für jeden Manager des Heimvereins, genau einmal je
    /// ausgetragenem Spiel. <paramref name="tableMatchDay"/> = Ligaspieltag, dessen Vortabelle die Nachfrage
    /// bestimmt; nach der Hauptrunde die Abschlusstabelle. <paramref name="importance"/> = Wettbewerbsbonus der
    /// Zuschauerformel (0 Liga, 1 DEB-Pokal und Play-Off/Aufstieg, 2 Europapokal).
    /// [ORIGINAL] em.exe 0x3F7DE: Konto += Zuschauer × Eintritt; eine Gastronomie-Einnahme gibt es im Original nicht.
    /// </summary>
    public void SettleHomeMatch(int home, int away, int tableMatchDay, int importance = 0)
    {
        foreach (var manager in Managers.Where(m => m.ClubIndex == home))
        {
            var finance = Finance(manager);
            var stadium = finance.Stadium;
            var spectators = Spectators(Clubs[home], Clubs[away], stadium, tableMatchDay, FinanceRandom, importance, manager.Level);
            var tickets = (long)spectators * stadium.TicketPrice;
            Book(manager, FinanceCategory.Eintritt, tickets);
            finance.LastHomeGame = (spectators, tickets);
        }
    }

    /// <summary>Spiele der Play-Offs und Aufstiegsrunden eines Termins abrechnen (nach der Hauptrunde, Bonus 1).</summary>
    public void SettlePostseasonMatches(IEnumerable<(int Home, int Away)> matches)
    {
        foreach (var (home, away) in matches)
            SettleHomeMatch(home, away, PlayedMatchDays + 1, importance: 1);
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
            if (FinanceDay > 0)
                AccrueDailyInterest(date);
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
            // [ORIGINAL] Monatsabschluss 0x3F02C: Grundausgaben und Stadionunterhalt (Finanzbuch „Unterhalt“),
            // Grundeinnahmen („Sponsoren“), danach das Zinskonto des Tagesabschlusses.
            Book(manager, FinanceCategory.Unterhalt, -(MonthlyBaseExpenses(manager) + Finance(manager).Stadium.MonthlyUpkeep));
            Book(manager, FinanceCategory.Sponsoren, MonthlyBaseIncome(manager));
            MonthlyManagerFinances(manager);
            BookAccruedInterest(manager);
        }
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
