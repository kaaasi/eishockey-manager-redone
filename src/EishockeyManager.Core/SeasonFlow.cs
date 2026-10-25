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
        if (Phase != SeasonPhase.Running)
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

    private bool ManagerPlaysOn(int day) => ActiveManagers.Any(m => MatchOn(day, m.ClubIndex) is not null);

    /// <summary>
    /// „und tschüß…“: spielt den aktuellen Tag und alle folgenden Tage bis zum nächsten Spiel eines Managers,
    /// bis zum Ende der Vereinssaison oder bis zum Saisonende. Liefert, was die Oberfläche zeigen soll.
    /// </summary>
    public IReadOnlyList<SeasonStep> Continue()
    {
        if (Phase is SeasonPhase.SeasonOver or SeasonPhase.GameOver)
            throw new InvalidOperationException("Die Saison ist zu Ende – zuerst StartNextSeason() aufrufen.");

        var steps = new List<SeasonStep>();
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

    private void ProcessDay(int day, List<SeasonStep> steps)
    {
        CheckDebts(day, steps);
        if (Phase == SeasonPhase.GameOver)
            return;

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
        var playoffGames = Playoffs.PlayDate(index, day, Clubs, Random);
        PlayoffResults.AddRange(playoffGames);
        var relegationGames = RelegationGroups.SelectMany(g => g.PlayDate(index, Clubs, Random)).ToList();

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

        foreach (var group in RelegationGroups)
        {
            var up = group.Qualified(Clubs);
            foreach (var club in group.Clubs)
            {
                var from = Clubs[club].League;
                if (up.Contains(club) && from == group.Lower)
                    record.PromotedInto[group.Upper].Add(club);
                else if (!up.Contains(club) && from == group.Upper)
                    record.RelegatedInto[group.Lower].Add(club);
            }
        }

        // [APPROX] Die beiden Letzten der Oberliga verlassen die Liga (Vereine von Managern ausgenommen),
        // zwei zufällige Vereine ohne Liga (Datenbank 36–47) rücken nach – im Video grün unter „Oberliga“.
        var managerClubs = Managers.Select(m => m.ClubIndex).ToHashSet();
        var leaving = tables[League.Oberliga].Reverse().Where(c => !managerClubs.Contains(c)).Take(2).ToList();
        record.LeftOberliga.AddRange(leaving);
        var pool = Clubs.Where(c => c.Index < NewGame.SelectableClubs && c.League is null).Select(c => c.Index).ToList();
        for (var i = 0; i < leaving.Count && pool.Count > 0; i++)
        {
            var pick = pool[Random.Next(pool.Count)];
            pool.Remove(pick);
            record.PromotedInto[League.Oberliga].Add(pick);
        }

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

        for (var i = 0; i < Managers.Count; i++)
        {
            var manager = Managers[i];
            if (manager.Dismissed)
                continue;
            var club = Clubs[manager.ClubIndex];
            var league = club.League ?? League.Oberliga;
            var rank = record.FinalTables[league].ToList().IndexOf(club.Index) + 1;
            var events = new List<SeasonEvent>();
            if (record.PromotedInto.Values.Any(l => l.Contains(club.Index)))
                events.Add(SeasonEvent.Promotion);
            if (record.RelegatedInto.Values.Any(l => l.Contains(club.Index)))
                events.Add(SeasonEvent.Relegation);
            foreach (var (title, winner) in record.Titles)
            {
                if (winner != club.Index)
                    continue;
                events.Add(title switch
                {
                    Title.GermanChampion => SeasonEvent.GermanChampion,
                    Title.CupWinner => SeasonEvent.CupWinner,
                    _ => SeasonEvent.EuropeanCupWinner,
                });
            }
            var premium = Premium(league, rank) + (events.Contains(SeasonEvent.GermanChampion) ? 1_000_000 : 0);
            manager.Money += premium;
            record.Managers.Add(new ManagerSeasonResult(i, club.Index, league, rank, premium, events));
        }

        History.Add(record);
        Phase = SeasonLimit is { } limit && History.Count >= limit ? SeasonPhase.GameOver : SeasonPhase.SeasonOver;
    }

    /// <summary>
    /// Prämie für die Platzierung in der Hauptrunde. [APPROX] Belegt ist nur: 1. Platz der Oberliga = 400.000 DM,
    /// 4. Platz der Oberliga = keine (Video). Übrige Werte geschätzt; Deutscher Meister zusätzlich 1 Mio. DM.
    /// </summary>
    public static long Premium(League league, int rank) => (league, rank) switch
    {
        (League.Bundesliga, 1) => 1_000_000,
        (League.Bundesliga, 2) => 700_000,
        (League.Bundesliga, 3) => 500_000,
        (League.ZweiteLiga, 1) => 600_000,
        (League.ZweiteLiga, 2) => 400_000,
        (League.ZweiteLiga, 3) => 200_000,
        (League.Oberliga, 1) => 400_000,
        (League.Oberliga, 2) => 200_000,
        (League.Oberliga, 3) => 100_000,
        _ => 0,
    };

    /// <summary>Nach dem Bildschirm „Saisonende“: Auf-/Abstieg anwenden, neue Spielpläne, Kalender auf den 10. September.</summary>
    public void StartNextSeason()
    {
        if (Phase != SeasonPhase.SeasonOver || CurrentRecord is not { } record)
            throw new InvalidOperationException("Die Saison ist noch nicht zu Ende.");

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
