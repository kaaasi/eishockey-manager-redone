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

    private bool ManagerPlaysOn(int day) =>
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
        while (true)
        {
            ProcessDay(CurrentDay, steps);
            if (Phase is SeasonPhase.SeasonOver or SeasonPhase.GameOver)
                return steps;
            CurrentDay++;
            // Nach der Vereinssaison hält das Original am 14. April an („Saisonende“ in der Infoleiste)
            if (Phase == SeasonPhase.ClubSeasonOver && CurrentDay == SeasonCalendar.ClubSeasonEnd + 1)
                return steps;
            if (CurrentDay != SeasonCalendar.SeasonEnd && ManagerPlaysOn(CurrentDay))
                return steps;
        }
    }

    // ---- Anschlusspunkte für andere Pakete (in deren eigener partial-Datei implementieren) -------------

    /// <summary>Paket „manager“: jeder Tag der Zeitleiste, bevor dessen Spiele ausgetragen werden (Tageswechsel der Finanzen).</summary>
    partial void OnDayManager(int day);

    /// <summary>Paket „manager“: neue Saison hat begonnen (Finanzbuch, Stadion …).</summary>
    partial void OnNewSeasonManager();

    /// <summary>Paket „mannschaft“: neue Saison hat begonnen (Alter, Verträge, Talente …).</summary>
    partial void OnNewSeasonTeam();

    private void ProcessDay(int day, List<SeasonStep> steps)
    {
        OnDayManager(day);
        CleanLineups();
        CheckDebts(day, steps);
        if (Phase == SeasonPhase.GameOver)
            return;
        MarkShortHandedClubs(); // Spur heimspiel-statistik (#165): Wertung 0:2/2:0 vor den Spielen des Tages

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
        // [APPROX] Im Modus „nur Weltkugel“ wird keine Liga gespielt, also steigt auch niemand ab (Spur wettbewerbe-weltkugel).
        if (!WorldOnly)
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
            // [APPROX] (K3, Disassembly ohne DOSBox-Beleg) Bit 0x04 unterdrückt beide Pfeile (Datei 0x56287–0x56334), Symbol 0.
            if (forced.Contains(i))
                events.Add(SeasonEvent.ForcedRelegation);
            else if (record.PromotedInto.Values.Any(l => l.Contains(club.Index)))
                events.Add(SeasonEvent.Promotion);
            if (!forced.Contains(i) && record.RelegatedInto.Values.Any(l => l.Contains(club.Index)))
                events.Add(SeasonEvent.Relegation);
            // [ORIGINAL] Datei 0x562C1–0x56334 (S-K1, DOSBox-Beleg #389, `file13_15`): Bleibt der Verein als Letzter oder
            // Vorletzter in der Oberliga, setzt em.exe Bit 0x02 und Bit 0x01 (Pfeil hoch, Pfeil runter) und das Konto auf
            // 400.000 DM – gesetzt, nicht gebucht (neutral, kein Finanzbuch), vor den Prämien des Fensters (Datei 0x5645D).
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
            events.AddRange(SinglePremiumEvents(manager, league)); // Spur statistiken (#135): Einzelprämien (StatisticsClub.cs)
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
    }

    /// <summary>
    /// Entlassung: Wer 8 Wochen am Stück mit mehr als 1,5 Mio. DM im Minus steht, wird entlassen
    /// (laut Anforderungen, nur lokale Spiele). Im Hot-Seat spielen die übrigen Manager weiter.
    /// </summary>
    private void CheckDebts(int day, List<SeasonStep> steps)
    {
        for (var i = 0; i < Managers.Count; i++)
        {
            var m = Managers[i];
            if (m.Dismissed)
                continue;
            m.DebtDays = m.Money < Manager.DebtLimit ? m.DebtDays + 1 : 0;
            if (m.DebtDays < Manager.DebtDaysUntilDismissal)
                continue;
            m.Dismissed = true;
            steps.Add(new DismissalStep(day, i));
        }
        if (Managers.Count > 0 && Managers.All(m => m.Dismissed))
        {
            Phase = SeasonPhase.GameOver;
            return;
        }
        while (ActiveManager.Dismissed)
            CurrentManager = (CurrentManager + 1) % Managers.Count;
    }
}

