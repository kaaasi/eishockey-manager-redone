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
            Stadium = Stadium.ForLeague(Clubs[manager.ClubIndex].League, Setup.Duration == GameDuration.EineSaison),
            CostLeague = Clubs[manager.ClubIndex].League ?? League.Oberliga,
            Youth = YouthTeam.StartTeams(StartLeagueOf(manager)), // Spur geld Teil 3 (FinanceYouth.cs, #327)
        };

    /// <summary>
    /// Start-Liga eines Managers aus der Spielvorbereitung ([ORIGINAL] DGROUP 0x11A; im Remake je Manager,
    /// <see cref="ManagerSetup.StartLeague"/>). [APPROX] Ohne Eintrag in <see cref="Setup"/> (Tests, alte Spielstände)
    /// die Liga des Vereins, sonst Oberliga.
    /// </summary>
    private League StartLeagueOf(Manager manager)
    {
        var i = Managers.ToList().IndexOf(manager);
        if (i >= 0 && i < Setup.Managers.Count)
            return Setup.Managers[i].StartLeague;
        return Clubs[manager.ClubIndex].League ?? League.Oberliga;
    }

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
    /// Anzeige „Ausgaben“: Grundausgaben, Gehälter, Versicherungen, Stadionunterhalt, Jugendförderung, 8,5 Trainingstage
    /// (<see cref="FinanceRules.MonthlyTrainingExpenseThirds"/>), Kreditzinsen und 10 % eines negativen Kontostands.
    /// [ORIGINAL] 0x3E811 mit Flags 1, 1.
    /// </summary>
    public long MonthlyExpenses(Manager manager)
    {
        var club = Clubs[manager.ClubIndex];
        var training = FinanceRules.ToMarks(FinanceRules.MonthlyTrainingExpenseThirds(manager.Training.RawCostPerTrainingDay));
        var expenses = MonthlyBaseExpenses(manager) + FinanceRules.MonthlyWages(this, club) + FinanceRules.MonthlyInsurance(this, club)
            + Finance(manager).Stadium.MonthlyUpkeep + RecurringExpenses(manager)
            + training + MonthlyDebtInterest(manager)
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
            SettleHomeMatch(result.Home, result.Away, result.MatchDay, leagueMatch: true);
    }

    /// <summary>
    /// Gemeinsame Spielabrechnung für alle Wettbewerbe (Liga, Play-Offs, Aufstiegsrunde, Pokale über
    /// <see cref="ISeasonCompetition"/>): Zuschauer und Eintritt für jeden Manager des Heimvereins, genau einmal je
    /// ausgetragenem Spiel. <paramref name="tableMatchDay"/> = Ligaspieltag, dessen Vortabelle die Nachfrage
    /// bestimmt; nach der Hauptrunde die Abschlusstabelle. <paramref name="importance"/> = Wettbewerbsbonus der
    /// Zuschauerformel (0 Liga, 1 DEB-Pokal und Play-Off/Aufstieg, 2 Europapokal).
    /// [ORIGINAL] em.exe 0x3F7DE: Konto += Zuschauer × Eintritt; eine Gastronomie-Einnahme gibt es im Original nicht.
    /// Spur heimspiel-statistik: „Weniger als neun Spieler“ für beide Vereine und die Zuschauerstatistik der
    /// Ligaheimspiele (<paramref name="leagueMatch"/>) in <see cref="SettleMatchGate"/>.
    /// </summary>
    public void SettleHomeMatch(int home, int away, int tableMatchDay, int importance = 0, bool leagueMatch = false) =>
        SettleMatchGate(home, away, tableMatchDay, importance, leagueMatch);

    /// <summary>Spiele der Play-Offs und Aufstiegsrunden eines Termins abrechnen (nach der Hauptrunde, Bonus 1).</summary>
    public void SettlePostseasonMatches(IEnumerable<(int Home, int Away)> matches)
    {
        foreach (var (home, away) in matches)
            SettleHomeMatch(home, away, PlayedMatchDays + 1, importance: 1);
    }

    /// <summary>Rechnet alle Tage bis einschließlich <paramref name="day"/> ab (Monats-, Wochen- und Tageswechsel).</summary>
    public void AdvanceFinanceTo(int day) => AdvanceFinance(day, clubDay: true);

    /// <summary>
    /// Gemeinsamer Kern des Tageswechsels der Finanzen. <paramref name="clubDay"/>: Tageslauf des Hauptwegs mit Jugendteams
    /// (0x39DEC, Datei 0x5501C) und Talentsuche (0x06FA5, Datei 0x5528A); false für die WM-Schleife (Datei 0x55570–0x55740 ruft
    /// beide nicht; Spur nationalteam, Review #674 R14NT4-02/-07). Sonst gleich, damit beide Wege nicht auseinanderlaufen.
    /// </summary>
    internal void AdvanceFinance(int day, bool clubDay)
    {
        // Ladeeigenheit des Markierungstags (GameState.LoadQuirk.cs): Der Finanztag läuft dem Tag um den Ladeversatz voraus.
        var target = FinanceTarget(day);
        while (FinanceDay < target)
        {
            FinanceDay++;
            var date = SeasonCalendar.LayoutDate(FinanceDay);
            ExpireMessages();
            // [ORIGINAL] Tagesablauf 0x55008: an Ligaspieltagen die Jugendteams (0x39DEC) vor den Spielen (Spur geld Teil 3).
            if (clubDay)
                DailyYouth(FinanceDay);
            // [ORIGINAL] Tageslauf 0x54C9F, Datei 0x5528A: Talentsuche an Tagen mit Zähler 0x3B7 ≡ 0, 4 (mod 7) (Spur geld Teil 6).
            if (clubDay && FinanceDay > 0)
                DailyTalentSearch(FinanceDay);
            if (FinanceDay > 0)
            {
                // [ORIGINAL] Tagesabschluss 0x3ED01: Zinskonto, Werbeangebote (Monatsletzter, Monatsmitte), am 1.
                // Restlaufzeiten der Werbeverträge (0x3EF52), am Monatsletzten der Monatsabschluss (0x3F02C, #134);
                // danach die Werbebewertung neu (0x5569F). Zuerst die „beleidigten“ Baufirmen (0x3ED19, Spur geld).
                DailyStadium();
                AccrueDailyInterest(date);
                DailySponsorOffers(date);
                if (date.Day == 1)
                    foreach (var manager in Managers)
                        CountDownSponsorContracts(manager);
            }
            // #679: [ORIGINAL] Stadionbau 0x3D316 am Tageszähler 0x3B7 ≡ 0 (mod 7) nach dem Weiterzählen (Hauptweg Datei 0x552AB–0x552C3,
            // WM-Schleife Datei 0x556B5–0x556CD), im Tageslauf also Zähler DayCounter.Of(Tag) – samstags, vorher montags.
            if (FinanceDay > 0 && DayCounter.Of(FinanceDay) % 7 == 0)
                WeeklyFinances();
            if (FinanceDay > 0 && date.Day == DateTime.DaysInMonth(date.Year, date.Month))
                MonthlyFinances();
            if (FinanceDay > 0)
                DailySponsorRatings();
            // Börsentag (em.exe 0x3ED4A, Tagesrechnung 0x5784A). Das Original rechnet ihn im Tagesabschluss jedes
            // menschlichen Managers, also n-mal je Kalendertag. Entscheidung Nutzer (30.10.2026, #188): einmal je
            // Kalendertag, nach dem Tagesabschluss des letzten Managers – kein Manager hat einen Vorteil.
            AdvanceStocks();
        }
    }

    /// <summary>Saisonwechsel für das Paket „saison“: Finanzbuch umstellen, Tageszähler zurücksetzen.</summary>
    public void StartFinanceSeason()
    {
        foreach (var manager in Managers)
        {
            Finance(manager).Book.NewSeason();
            // [ORIGINAL] 0x563C0: Kostenstufe = Liga der neuen Saison (Spur finanzen-2).
            Finance(manager).CostLeague = Clubs[manager.ClubIndex].League ?? League.Oberliga;
            // [ORIGINAL] Saisonwechsel 0x55889: Auf-/Abstieg und Neustart der Jugendteams (0x3A13B, Spur geld Teil 3).
            YouthSeasonChange(manager);
        }
        FinanceDay = -1;
    }

    /// <summary>
    /// [ORIGINAL] 0x5664D: zu Saisonbeginn neue Werbeangebote (gewürfelt, gespeicherte Bewertung; #133), nach der Sommerpause
    /// (Datei 0x56650 hinter der Schleife 0x564D2–0x56593; #688, vorher in <see cref="StartFinanceSeason"/>).
    /// </summary>
    internal void StartSponsorSeason()
    {
        foreach (var manager in Managers)
            RefreshSponsorOffers(manager, noDice: false);
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
            Book(manager, FinanceCategory.Gehaelter, -FinanceRules.MonthlyWages(this, club)); // A380: samt eigener Transferliste
            var insurance = FinanceRules.MonthlyInsurance(this, club);
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
        foreach (var done in AdvanceConstruction(manager))
            ConstructionCompleted(manager, done);
    }

    private void MonthlyManagerFinances(Manager manager)
    {
        BookSponsoring(manager);
        SettleYouth(manager);
    }
}
