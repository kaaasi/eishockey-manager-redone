namespace EishockeyManager.Core;

/// <summary>Abschnitt der laufenden Saison.</summary>
public enum SeasonPhase
{
    /// <summary>Hauptrunde, Play-Offs, Pokale.</summary>
    Running,

    /// <summary>Vereinssaison beendet (ab 14. April): Infoleiste zeigt „Saisonende“, nur noch WM-Termine.</summary>
    ClubSeasonOver,

    /// <summary>Saisonende erreicht (29. April): Bildschirm „Saisonende“, danach <see cref="GameState.StartNextSeason"/>.</summary>
    SeasonOver,

    /// <summary>Gewählte Spieldauer abgelaufen oder alle Manager entlassen.</summary>
    GameOver,
}

/// <summary>
/// Saisonablauf über mehrere Jahre: Zeitleiste Tag für Tag nach dem Kalender des Originals, Play-Offs,
/// Aufstiegsrunden, Auf- und Abstieg, neue Saison, Spielende.
/// </summary>
/// <remarks>
/// „und tschüß…“ (<see cref="Continue"/>) spielt den aktuellen Tag und läuft dann Tag für Tag weiter – auch über
/// spielfreie Tage –, bis wieder ein Verein eines Managers spielt. Das Hauptmenü zeigt also immer das Datum des
/// <b>nächsten</b> eigenen Spiels (Beleg: Video, Saison 1995/96 springt vom 44. Spieltag direkt zum 14. April,
/// weil BK Oldenburg weder Play-Offs noch Aufstiegsrunde spielt).
/// </remarks>
public sealed partial class GameState
{
    /// <summary>Tag seit Saisonbeginn, den das Hauptmenü zeigt (Tag des nächsten eigenen Spiels).</summary>
    public int CurrentDay { get; private set; }

    public SeasonPhase Phase { get; private set; } = SeasonPhase.Running;

    public bool IsGameOver => Phase == SeasonPhase.GameOver;

    /// <summary>Play-Offs der laufenden Saison (ab Ende der Hauptrunde).</summary>
    public Playoffs? Playoffs { get; private set; }

    /// <summary>Aufstiegsrunden Bundesliga/2. Liga und 2. Liga/Oberliga (ab Ende der Hauptrunde).</summary>
    public List<RelegationGroup> RelegationGroups { get; } = [];

    /// <summary>Alle Spiele der Play-Offs dieser Saison.</summary>
    public List<KnockoutMatch> PlayoffResults { get; } = [];

    /// <summary>Abgeschlossene Saisons, älteste zuerst.</summary>
    public List<SeasonRecord> History { get; } = [];

    /// <summary>Ergebnis der gerade beendeten Saison (ab Ende der Vereinssaison bis zum Start der nächsten).</summary>
    public SeasonRecord? CurrentRecord { get; private set; }

    /// <summary>Weitere Wettbewerbe (DEB-Pokal, Europapokal, WM), siehe <see cref="ISeasonCompetition"/>.</summary>
    public List<ISeasonCompetition> Competitions { get; } = [];

    /// <summary>Manager, die noch im Spiel sind.</summary>
    public IEnumerable<Manager> ActiveManagers => Managers.Where(m => !m.Dismissed);

    /// <summary>Anzahl der Saisons laut Spielvorbereitung oder null (unbegrenzt).</summary>
    public int? SeasonLimit => Setup.Duration switch
    {
        GameDuration.EineSaison => 1,
        GameDuration.DreiSaisons => 3,
        GameDuration.FuenfSaisons => 5,
        _ => null,
    };

    /// <summary>Nummer des nächsten Ligaspieltags, wenn heute ein Ligaspieltag ist (Infoleiste „n.Spieltag“).</summary>
    public int? UpcomingLeagueMatchDay =>
        Phase == SeasonPhase.Running && !RegularSeasonOver && SeasonCalendar.Events(CurrentDay).HasFlag(DayEvents.League)
            ? PlayedMatchDays + 1
            : null;

    /// <summary>
    /// Nächstes Spiel eines Vereins für die Infoleiste: heute, sonst das nächste Ligaspiel; null nach der Vereinssaison
    /// oder ohne weiteres Spiel.
    /// </summary>
    public (Club Opponent, bool Home, Competition Competition)? UpcomingMatch(int club)
    {
        // Spur bundestrainer-modus4: Das Nationalteam des Bundestrainers spielt die WM nach der Vereinssaison.
        if (Phase != SeasonPhase.Running && !(Phase == SeasonPhase.ClubSeasonOver && NationalCoach?.Nation == club))
            return null;
        if (MatchOn(CurrentDay, club) is { } today)
            return (Clubs[today.Opponent], today.Home, today.Competition);
        return NextLeagueMatch(club) is { } league ? (league.Opponent, league.Home, Competition.League) : null;
    }

    /// <summary>Spiel eines Vereins an einem Tag (nur für heute und Tage, deren Paarungen schon feststehen).</summary>
    public (int Opponent, bool Home, Competition Competition)? MatchOn(int day, int club)
    {
        var events = SeasonCalendar.Events(day);
        if (events == DayEvents.SeasonEnd)
            return null;

        if (events.HasFlag(DayEvents.League) && SeasonCalendar.LeagueMatchDays(LeagueMatchDays).ToList().IndexOf(day) == PlayedMatchDays
            && NextLeagueMatch(club) is { } league)
            return (league.Opponent.Index, league.Home, Competition.League);

        if (events.HasFlag(DayEvents.Playoffs))
        {
            var index = PlayoffDateIndex(day);
            if (Playoffs?.SeriesOn(index, club) is { } series)
            {
                var (home, away) = series.NextPairing;
                return (home == club ? away : home, home == club, Competition.Playoffs);
            }
            foreach (var group in RelegationGroups)
                if (group.MatchOn(index, club) is { } m)
                    return (m.Home == club ? m.Away : m.Home, m.Home == club, Competition.Relegation);
        }

        foreach (var competition in Competitions)
            if ((events & competition.Days) != 0 && competition.MatchOn(this, day, club) is { } c)
                return (c.Opponent, c.Home, competition.Kind);
        return null;
    }

    private static int PlayoffDateIndex(int day) => SeasonCalendar.DaysOf(DayEvents.Playoffs).ToList().IndexOf(day);

    internal bool ManagerPlaysOn(int day) =>
        ActiveManagers.Any(m => MatchOn(day, m.ClubIndex) is not null) || NationalCoachPlaysOn(day); // Bundestrainer: Spur wettbewerbe-weltkugel

    /// <summary>
    /// „und tschüß…“ des Managers am Zug. Im Hot-Seat gibt es zunächst nur den Zug an den nächsten aktiven Manager
    /// weiter (leere Liste, siehe <c>GameState.HotSeat.cs</c>). Bestätigt der letzte offene Manager, spielt es den
    /// aktuellen Tag und alle folgenden Tage bis zum nächsten Spiel eines Managers, bis zum Ende der Vereinssaison
    /// oder bis zum Saisonende, und beginnt eine neue Bereitschaftsphase. Liefert, was die Oberfläche zeigen soll.
    /// </summary>
    public IReadOnlyList<SeasonStep> Continue()
    {
        if (Phase is SeasonPhase.SeasonOver or SeasonPhase.GameOver)
            throw new InvalidOperationException("Die Saison ist zu Ende – zuerst StartNextSeason() aufrufen.");
        if (PassTurn() || PassNationalTurn()) // Zug des Bundestrainers im Modus 4: Spur bundestrainer-modus4
            return [];

        var steps = RunDays();
        StartTurns();
        return steps;
    }

    private List<SeasonStep> RunDays()
    {
        var steps = new List<SeasonStep>();
        // Spur wettbewerbe-weltkugel: Im Modus „nur Weltkugel“ springt der erste Tag der Saison zur WM.
        if (SkipToWorldCup())
            return steps;
        var turnsTaken = true; // der erste Tag ist der des Halts: Seine Managerzüge sind gelaufen
        while (true)
        {
            ProcessDay(CurrentDay, steps, turnsTaken);
            turnsTaken = false;
            if (Phase is SeasonPhase.SeasonOver or SeasonPhase.GameOver)
                return steps;
            CurrentDay++;
            // #688 (R688-04): Der Tageswechsel nach den Spielen des Vortags (Tagesabschluss, Takt, Listenwechsel) liegt im Original
            // vor den Managermenüs des neuen Tages (Datei 0x55204–0x552D3, dann 0x54E3F–0x54EF4) – also vor dem Halt.
            OpenDay(CurrentDay);
            // Nach der Vereinssaison hält das Original am 14. April an („Saisonende“ in der Infoleiste)
            if (Phase == SeasonPhase.ClubSeasonOver && CurrentDay == SeasonCalendar.ClubSeasonEnd + 1)
                return steps;
            if (CurrentDay != SeasonCalendar.SeasonEnd && ManagerPlaysOn(CurrentDay))
                return steps;
        }
    }

    // ---- Anschlusspunkte für andere Pakete (in deren eigener partial-Datei implementieren) -------------

    /// <summary>
    /// Paket „manager“: jeder Tag der Zeitleiste nach den Managermenüs, bevor dessen Spiele ausgetragen werden. Der Tageswechsel
    /// davor läuft in <see cref="OnDayOpen"/>.
    /// </summary>
    partial void OnDayManager(int day);

    /// <summary>
    /// #688 (R688-04): Tageswechsel zum Tag <paramref name="day"/> vor dessen Managermenüs (Tagesabschluss, Takt 0x07169,
    /// Listenwechsel). [ORIGINAL] Datei 0x55204–0x552D3 nach den Spielen des Vortags, Züge Datei 0x54E3F–0x54EF4 vor dem Menü 0x44E22.
    /// </summary>
    partial void OnDayOpen(int day);

    /// <summary>
    /// Ist der Tageswechsel zu <see cref="CurrentDay"/> schon gerechnet (<see cref="OpenDay"/>)? Die Zeitleiste rechnet ihn vor dem
    /// Halt; Spielstände vor Schema 45, ein neues Spiel und der erste Tag einer Saison holen ihn vor den Spielen des Tages nach.
    /// [ORIGINAL] Tag 0 (#699 Punkt 2, Spur tagesablauf Runde 15): Spiel und Saison beginnen mit Index und Zähler 0
    /// (Datei 0x565A1–0x565B0, 0x43A93), der erste Tageswechsel läuft vor dem ersten Menü (Datei 0x54D41 → 0x55073 → 0x55204,
    /// Zähler 1 = 10. September; FILE01: 1/1) mit Tagesabschluss 0x3ED01/0x3F833 je Manager (<see cref="OpenStartDay"/>).
    /// </summary>
    public bool DayOpened { get; private set; }

    private void OpenDay(int day)
    {
        if (DayOpened)
            return;
        DayOpened = true;
        OnDayOpen(day);
    }

    /// <summary>
    /// Tageswechsel zu Tag 0 vor dem ersten Menü eines neuen Spiels bzw. einer neuen Saison (#699 Punkt 2). [ORIGINAL] Spiel- und
    /// Saisonstart setzen Zähler DGROUP 0x3B7 und Index 0xA969 auf 0 (Datei 0x43A93, 0x565A7); das Tagesbyte des Index 0 ist 0, der
    /// Hauptweg springt bei Datei 0x54D5C nach 0x55073 und von dort ohne Menü zum Tageswechsel Datei 0x55204 (Zähler 1, Tagesabschluss
    /// je Manager, Takt nicht: Zähler 1 ≢ 0, 4), danach Neuaufstellungen und Listenwechsel-Wurf von Manager 0 vor dessen Menü.
    /// Im Modus „nur Weltkugel“ nicht (Datei 0x54CEE: eigener Weg zur WM, <see cref="SkipToWorldCup"/>).
    /// </summary>
    internal void OpenStartDay()
    {
        if (!WorldOnly && CurrentDay == 0 && Phase == SeasonPhase.Running)
            OpenDay(0);
    }

    /// <summary>Paket „manager“: neue Saison hat begonnen (Finanzbuch, Stadion …).</summary>
    partial void OnNewSeasonManager();

    /// <summary>Paket „mannschaft“: neue Saison hat begonnen (Alter, Verträge, Talente …).</summary>
    partial void OnNewSeasonTeam();

    /// <summary>
    /// [ORIGINAL] Merker „Managerverein spielt“ (em.exe Datei 0x36FEA–0x3700B, geprüft bei 0x37028; Sätze DGROUP 0xA729 + m
    /// gelöscht 0x32AA3, gesetzt 0x545A6/0x547C7, zurückgesetzt 0x54FF3): wird <b>einmal je Tag</b> vor den ersten Spielen
    /// gebildet und gilt nur innerhalb dieses Tages (statisch je Thread, damit nicht im Spielstand: SaveGame speichert alle Instanzfelder). Nur gesetzt, wenn
    /// <see cref="GameOptions.ManagerDayMinuteLoop"/> an ist; <c>MatchEngine.PlayWithEvents</c> liest nur diesen Wert
    /// (R14S-01, nicht je Paarung neu über <see cref="ManagerPlaysOn"/>, das mit dem Spielfortschritt kippt).
    /// </summary>
    [ThreadStatic]
    internal static bool ManagerDayFlag;

    private void ProcessDay(int day, List<SeasonStep> steps) => ProcessDay(day, steps, turnsTaken: false);

    /// <param name="turnsTaken">Hat die Zeitleiste an diesem Tag gehalten und sind die Managerzüge gelaufen (Würfe des
    /// Listenwechsels bis zum letzten aktiven Manager schon beim Weitergeben, <see cref="RollListChangesThrough"/>)?</param>
    private void ProcessDay(int day, List<SeasonStep> steps, bool turnsTaken)
    {
        ManagerDayFlag = false;
        try
        {
            OpenDay(day);
            DayOpened = false;
            MainPathRemainingListChanges(day, turnsTaken);
            ProcessDayCore(day, steps);
        }
        finally
        {
            ManagerDayFlag = false;
        }
    }

    private void ProcessDayCore(int day, List<SeasonStep> steps)
    {
        OnDayManager(day);
        CleanLineups();
        CheckDismissed();
        if (Phase == SeasonPhase.GameOver)
        {
            // [APPROX] (Review #441, SW-02; nur noch mit ausgeschiedenen Managern aus älteren Spielständen, R688S-03): Sind am
            // 29. April alle Manager ausgeschieden, schließt die Saison noch ab (Titel, Austausch, Ewige Tabelle, Chronik) – wie
            // beim Ausscheiden eines Teils der Manager; ausgeschiedene Manager erhalten weder Platz noch Prämie. Kein
            // SeasonEndStep: die Oberfläche zeigt direkt das Spielende.
            if (day == SeasonCalendar.SeasonEnd)
            {
                FinishSeason();
                Phase = SeasonPhase.GameOver;
            }
            return;
        }
        // [ORIGINAL] Jugendteams von Tag 0 (Datei 0x55008–0x5502C, 0x39DEC) nach dem ersten Menü vor den Spielen (#699 Punkt 2):
        // Der Tageswechsel zu Tag 0 vor dem Menü rechnet sie nicht (AdvanceFinance), die übrigen Tage dort ([APPROX]).
        if (day == 0 && !WorldOnly)
            DailyYouth(0);
        MarkShortHandedClubs(); // Spur heimspiel-statistik (#165): Wertung 0:2/2:0 vor den Spielen des Tages
        ManagerDayFlag = Options.ManagerDayMinuteLoop && ManagerPlaysOn(day); // Merker vor den ersten Spielen des Tages (R14S-01)

        var events = SeasonCalendar.Events(day);
        if (day == SeasonCalendar.ClubSeasonEnd)
        {
            FinishClubSeason();
            return;
        }
        if (day == SeasonCalendar.SeasonEnd)
        {
            FinishSeason();
            steps.Add(new SeasonEndStep(day));
            return;
        }

        if (events.HasFlag(DayEvents.League) && !RegularSeasonOver)
        {
            PlayNextMatchDay();
            steps.Add(new MatchesStep(day, Competition.League, ManagerInvolved: true));
            if (RegularSeasonOver)
                StartPlayoffs();
        }

        if (events.HasFlag(DayEvents.Playoffs))
            PlayPlayoffDay(day, steps);

        foreach (var competition in Competitions)
            if ((events & competition.Days) != 0)
                competition.PlayDay(this, day, steps);
    }

    /// <summary>Nach dem letzten Spieltag der Hauptrunde: Play-Offs und Aufstiegsrunden aus den Abschlusstabellen.</summary>
    private void StartPlayoffs()
    {
        var tables = FinalTables();
        Playoffs = new Playoffs(tables[League.Bundesliga].Take(Playoffs.Teams).ToList());
        RelegationGroups.Clear();
        foreach (var upper in new[] { League.Bundesliga, League.ZweiteLiga })
        {
            var top = tables[upper];
            var bottom = tables[upper + 1];
            RelegationGroups.Add(new RelegationGroup(upper, [top[^2], top[^1], bottom[0], bottom[1]], Random));
        }
    }

    /// <summary>Abschlusstabellen der Hauptrunde (Vereinsindizes, Platz 1 zuerst).</summary>
    public Dictionary<League, IReadOnlyList<int>> FinalTables() =>
        Enum.GetValues<League>().ToDictionary(l => l,
            l => (IReadOnlyList<int>)LeagueTable.Compute(this, l, TableView.Gesamt, PlayedMatchDays).Select(r => r.Club.Index).ToList());

    private void PlayPlayoffDay(int day, List<SeasonStep> steps)
    {
        var index = PlayoffDateIndex(day);
        if (Playoffs is null || index < 0)
            return;
        if (index == 0)
            steps.Add(new BannerStep(day, "Play-Off / Aufstiegsrunden"));

        var managerClubs = ActiveManagers.Select(m => m.ClubIndex).ToHashSet();
        bool Involves(int home, int away) => managerClubs.Contains(home) || managerClubs.Contains(away);

        var hadChampion = Playoffs.Champion is not null;
        var playoffGames = Playoffs.PlayDate(index, day, this, Random);
        PlayoffResults.AddRange(playoffGames);
        var relegationGames = RelegationGroups.SelectMany(g => g.PlayDate(index, this, Random)).ToList();
        SettlePostseasonMatches(playoffGames.Select(g => (g.Home, g.Away)) // gemeinsame Spielabrechnung (Paket „wirtschaft“)
            .Concat(relegationGames.Select(g => (g.Home, g.Away))));
        // [ORIGINAL] Begegnungstabellen auch für Play-offs (Datei 0x3165B, nur gespielte Spiele) und Aufstiegsrunden (0x30942,
        // Wettbewerb 1); Anschluss aus wettbewerbe-spiel Teil 2 (A111, #425), Integration Runde 11.
        RecordMeetings(playoffGames);
        foreach (var g in relegationGames)
            RecordMeeting(g.Home, g.Away, g.HomeGoals, g.AwayGoals);

        var playoffInvolved = playoffGames.Any(g => Involves(g.Home, g.Away));
        var relegationInvolved = relegationGames.Any(g => Involves(g.Home, g.Away));
        if (playoffInvolved)
            steps.Add(new MatchesStep(day, Competition.Playoffs, true));
        if (relegationInvolved)
            steps.Add(new MatchesStep(day, Competition.Relegation, true));
        if (!playoffInvolved && !relegationInvolved && playoffGames.Count + relegationGames.Count > 0)
            steps.Add(new MatchesStep(day, Competition.Playoffs, false));
        if (!hadChampion && Playoffs.Champion is { } champion)
            steps.Add(new TrophyStep(day, Title.GermanChampion, champion));
    }

    /// <summary>13. April: Auf- und Abstieg stehen fest.</summary>
    private void FinishClubSeason()
    {
        var tables = FinalTables();
        var record = new SeasonRecord { Season = Season, FinalTables = tables };
        if (Playoffs?.Champion is { } champion)
            record.Titles[Title.GermanChampion] = champion;

        // [ORIGINAL] Reihenfolge der Gruppentabelle (Datei 0x56157–0x5636D): em.exe paart den nächsten Aufsteiger unter
        // den ersten beiden mit dem nächsten Absteiger unter den Plätzen 3 und 4 (Tausch 0x36169) – Grundlage der
        // Startordnung der Folgesaison (LeagueTable.SuccessorOrder, Spur wettbewerbe-komplett). Zwangsabstieg: 29. April
        // (SeasonFlow.Zwangsabstieg.cs).
        foreach (var group in RelegationGroups)
            RecordRelegationGroup(record, group, group.Table(Clubs).Select(r => r.Club.Index).ToList());

        // Oberliga-Austausch wie em.exe (Spur saisonwechsel, S-K1/S-K2, SeasonFlow.Oberliga.cs).
        // [ORIGINAL] auch im Modus „nur Weltkugel“ (A163, Spur saison Teil 4; docs/analyse/re-runde-13.md): Datei 0x56238–0x56284
        // ohne Modusprüfung über die ungespielte Platzliste DGROUP 0x5CCB (= Startordnung, LeagueTable.StartOrder). Ohne Ligaspiele
        // gibt es keine Aufstiegsrunden, zwischen 1. Liga, 2. Liga und Oberliga wechselt niemand. Spielstände FILE20 → FILE21:
        // HEC HEILBRONN und EC BAD TÖLZ (Plätze 34/35) gehen, EHC STRAUBING und ESV KÖNIGSBRUNN (Plätze 37/41) rücken nach;
        // 1. und 2. Liga mit denselben zwölf Namen. [APPROX] Steht im Modus 1 der (nie geführte) Verein des Managers auf den
        // letzten beiden Plätzen, bleibt er wie in den anderen Modi (Doppeltausch); ob em.exe dort das Vereinsfeld +0x16 setzt, ist
        // nicht gelesen (in FILE20/21 nicht betroffen).
        ExchangeOberliga(tables[League.Oberliga], record);

        CurrentRecord = record;
        Phase = SeasonPhase.ClubSeasonOver;
    }

    /// <summary>29. April: Titel, Prämien, Ereignisse; Spielende nach der gewählten Dauer.</summary>
    private void FinishSeason()
    {
        if (CurrentRecord is null)
            FinishClubSeason();
        var record = CurrentRecord!;
        foreach (var competition in Competitions)
            if (competition is { Title: { } title, Winner: { } winner })
                record.Titles[title] = winner;
        var forced = ApplyForcedRelegation(record); // Spur saisonwechsel Teil 2 (S-K3): Statusbit 0x04, nach den Titelbits
        // [ORIGINAL] Ewige Tabelle nach dem Austausch (Datei 0x563AF ruft 0x5667A), mit der Liga der nächsten Saison
        // (Spur saisonwechsel Teil 3, S-K4; StatisticsHistory.cs). Ewige Bilanz und „Früher“ hängen nicht am Austausch.
        RecordSeasonStatistics();
        // [ORIGINAL] Ligamischung am 29. April nach Austausch und Ewiger Tabelle, vor dem Fenster mit den Prämien (Datei 0x5644E vor
        // 0x5645D; Spur saison Teil 3, S-K6-a, SeasonFlow.LeagueSlots.cs). Verbraucht keinen Zufall.
        PrepareNextLeagueSlots(record);

        // Spur wettbewerbe-weltkugel: Im Modus „nur Weltkugel“ führt kein Manager einen Verein durch die Saison –
        // kein Platz, keine Prämie (Prämien gibt es nur aus der Vereinssaison).
        for (var i = 0; i < Managers.Count && !WorldOnly; i++)
        {
            var manager = Managers[i];
            if (manager.Dismissed)
                continue;
            var club = Clubs[manager.ClubIndex];
            var league = club.League ?? League.Oberliga;
            var rank = record.FinalTables[league].ToList().IndexOf(club.Index) + 1;
            var events = new List<SeasonEvent>();
            // [ORIGINAL] (K3, Datei 0x56287–0x56334; DOSBox-Beleg `file16_9`, Teil 5 D3) Bit 0x04 unterdrückt beide Pfeile, Symbol 0.
            if (forced.Contains(i))
                events.Add(SeasonEvent.ForcedRelegation);
            else if (record.PromotedInto.Values.Any(l => l.Contains(club.Index)))
                events.Add(SeasonEvent.Promotion);
            if (!forced.Contains(i) && record.RelegatedInto.Values.Any(l => l.Contains(club.Index)))
                events.Add(SeasonEvent.Relegation);
            // [ORIGINAL] Datei 0x562C1–0x56334 (S-K1, DOSBox-Beleg #389, `file13_15`): Bleibt der Verein als Letzter oder
            // Vorletzter in der Oberliga, setzt em.exe Bit 0x02 und Bit 0x01 (Pfeil hoch, Pfeil runter) und das Konto auf
            // 400.000 DM – gesetzt, nicht gebucht (neutral, kein Finanzbuch), vor den Prämien des Fensters (Datei 0x5645D).
            // [ORIGINAL] E5 (re-runde-12.md, Datei 0x5629D–0x562DE): ohne Kontoprüfung – Guthaben wird gekürzt, Schulden
            // gehoben; nur Gruppe 2 (Datei 0x562C6), nie Bundesliga/2. Liga. Bit 0x04 (Zwangsabstieg) prüft das Remake hier
            // nicht: in der Oberliga nicht erreichbar (re-saisonwechsel.md Abschnitt 2). Tests: SaisonwechselKontoTests.
            if (KeptInOberliga(record, club.Index))
            {
                events.Add(SeasonEvent.Relegation);
                manager.Money = OberligaKeptMoney;
            }
            foreach (var (title, winner) in record.Titles)
            {
                if (winner != club.Index)
                    continue;
                // Ausdrückliche Zuordnung (Spur finanzen-2): Der WM-Titel geht an ein Nationalteam, nie an einen
                // Managerverein, und bringt dem Verein keine Prämie.
                switch (title)
                {
                    case Title.GermanChampion: events.Add(SeasonEvent.GermanChampion); break;
                    case Title.CupWinner: events.Add(SeasonEvent.CupWinner); break;
                    case Title.EuropeanCupWinner: events.Add(SeasonEvent.EuropeanCupWinner); break;
                    case Title.WorldChampion: break;
                }
            }
            // Spur statistiken (#135): Einzelprämien; Fair-Play über die Liga der nächsten Saison (S-K6-c, SeasonFlow.FairPlay.cs)
            events.AddRange(SeasonEndSinglePremiums(record, manager, league));
            var premium = SeasonPremium(league, rank, PremiumEvents(record, club.Index, events)); // ohne Aufstieg beim Doppeltausch
            if (premium > 0)
                Book(manager, FinanceCategory.Praemien, premium); // Finanzbuch „Prämien“ (Paket „wirtschaft“)
            record.Managers.Add(new ManagerSeasonResult(i, club.Index, league, rank, premium, events));
        }

        History.Add(record);
        Phase = SeasonLimit is { } limit && History.Count >= limit ? SeasonPhase.GameOver : SeasonPhase.SeasonOver;
    }

    /// <summary>
    /// Statusbit 0x40 „Verein unter den ersten fünf der 1. Liga“ eines Managers am Saisonende (Symbol 8, Fenster „Saisonende“).
    /// </summary>
    /// <remarks>
    /// [ORIGINAL] Datei 0x553A2–0x553F2 (Befund #125): An den Tagen mit Markierung 255 (13. und 29. April) ruft das Original
    /// <c>0xE8:0x93(0x7F)</c> (Datei 0x34769), löscht dann das Statusbyte jedes Managers (DGROUP 0x974E + 0x1AF · Manager) und setzt
    /// Bit 0x40, wenn sein Verein auf den Plätzen 1–5 der Tabelle der 1. Liga steht (DGROUP 0x5CCB) – nicht im Spielmodus 2 (0x553DB).
    /// Das gilt für jeden Manager, nicht nur für den höchstplatzierten (der wird Bundestrainer). Das Bit wirkt nur in der Anzeige
    /// (Datei 0x2714F, Symbol 8, ohne Prämie). Das Remake hält kein Statusbyte: Das Bit leitet sich aus der Abschlusstabelle, dem Verein und
    /// dem Modus ab, die schon im Spielstand liegen – es ist kein neuer Zustand und braucht keine Schemaänderung. Das Original leitet es an
    /// beiden Markertagen neu ab; zwischen 13. und 29. April ändert sich die Tabelle nicht (Ligaspiele ruhen).
    /// [ORIGINAL] 0x5CCB steht an den Markertagen nach „Gesamt“: Die Ansicht Heim/Auswärts sortiert beim Verlassen wieder nach Gesamt
    /// (Datei 0x50256, Spur tabellen-original). [APPROX] Nur die Reihenfolge vollständig gleichstehender Vereine kann davon abhängen, wie
    /// oft die Ansicht gewechselt wurde; das Remake nimmt die Abschlusstabelle „Gesamt“.
    /// </remarks>
    public bool HasTopFiveStatus(SeasonRecord record, ManagerSeasonResult result) =>
        Mode != GameMode.ClubOnly && record.IsTopFive(result.Club);

    /// <summary>Nach dem Bildschirm „Saisonende“: Auf-/Abstieg anwenden, neue Spielpläne, Kalender auf den 10. September.</summary>
    public void StartNextSeason()
    {
        if (Phase != SeasonPhase.SeasonOver || CurrentRecord is not { } record)
            throw new InvalidOperationException("Die Saison ist noch nicht zu Ende.");

        ValidateSeasonSquads();

        foreach (var club in Clubs)
            if (club.Index < NewGame.SelectableClubs)
                club.League = record.NextLeague(club.Index, club.League);
        AdvanceLeagueSlots(record); // S-K6: am 29. April gebildete Satzfolge übernehmen, Datei 0x5644E (SeasonFlow.LeagueSlots.cs)

        foreach (var league in Enum.GetValues<League>())
        {
            var members = LeagueClubs(league).Select(c => c.Index).ToList();
            for (var i = members.Count - 1; i > 0; i--)
            {
                var j = Random.Next(i + 1);
                (members[i], members[j]) = (members[j], members[i]);
            }
            Schedules[league] = Schedule.Rounds(members, rounds: 4);
        }

        Season++;
        ClearNationalForm(); // Bilanzzeichen leeren, em.exe 0x55EBA (CupsNational.TeamInfo.cs)
        PlayedMatchDays = 0;
        Results.Clear();
        PlayoffResults.Clear();
        Playoffs = null;
        RelegationGroups.Clear();
        CurrentRecord = null;
        CurrentDay = 0;
        Phase = SeasonPhase.Running;
        foreach (var competition in Competitions)
            competition.StartSeason(this);
        OnNewSeasonManager();
        OnNewSeasonTeam();
        // #699 Punkt 2: Tag 0 vor dem ersten Menü – mit Verhandlungen aus der Oberfläche erst nach diesen
        // (StartNextSeasonWithRenewals, dann ReturnToFirstActiveManager), weil der Tageswechsel offene Verhandlungen beendet.
        if (!s_deferRenewals)
            OpenStartDay();
    }

    /// <summary>
    /// Ausgeschiedene Manager (<see cref="Manager.Dismissed"/>, nur aus älteren Spielständen): Sind alle ausgeschieden, endet das
    /// Spiel; sonst geht der Zug an den nächsten aktiven Manager. [ORIGINAL] Keine Entlassung wegen Schulden (#688, R688S-03,
    /// <c>SeasonDismissal.cs</c>): Die Remake-Regel „acht Wochen unter −1,5 Mio. DM“ ist entfernt; Schulden wirken wie im Original
    /// über den Zwangsabstieg am 29. April (<c>SeasonFlow.Zwangsabstieg.cs</c>) und die Kontogrenze im Monatsabschluss.
    /// </summary>
    private void CheckDismissed()
    {
        if (Managers.Count > 0 && Managers.All(m => m.Dismissed))
        {
            Phase = SeasonPhase.GameOver;
            return;
        }
        while (ActiveManager.Dismissed)
            CurrentManager = (CurrentManager + 1) % Managers.Count;
    }
}

