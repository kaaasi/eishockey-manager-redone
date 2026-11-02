namespace EishockeyManager.Core;

/// <summary>Bilanz aus Ligaspielen (2-Punkte-Regel).</summary>
public sealed class MatchBalance
{
    public int PointsPlus { get; set; }
    public int PointsMinus { get; set; }
    public int GoalsFor { get; set; }
    public int GoalsAgainst { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Draws { get; set; }

    public void Add(int goalsFor, int goalsAgainst)
    {
        GoalsFor += goalsFor;
        GoalsAgainst += goalsAgainst;
        if (goalsFor > goalsAgainst) { Wins++; PointsPlus += 2; }
        else if (goalsFor < goalsAgainst) { Losses++; PointsMinus += 2; }
        else { Draws++; PointsPlus++; PointsMinus++; }
    }

    public MatchBalance Plus(MatchBalance other) => new()
    {
        PointsPlus = PointsPlus + other.PointsPlus, PointsMinus = PointsMinus + other.PointsMinus,
        GoalsFor = GoalsFor + other.GoalsFor, GoalsAgainst = GoalsAgainst + other.GoalsAgainst,
        Wins = Wins + other.Wins, Losses = Losses + other.Losses, Draws = Draws + other.Draws,
    };
}

/// <summary>Eintrag der Ewigen Tabelle eines Vereins.</summary>
public sealed class EternalEntry
{
    public required int Club { get; init; }
    public long Points { get; set; }
    public int Championships { get; set; }
    public int CupWins { get; set; }
    public int EuropeanCupWins { get; set; }
}

/// <summary>Abschneiden in der Liga für „Früher“ (em.exe kodiert Platz &lt; 50, 0x33–0x35 = Play-Off-Aus, 0x80 = Meister).</summary>
public enum LeagueOutcome
{
    Rank,
    PlayoffQuarterFinal,
    PlayoffSemiFinal,
    PlayoffFinal,
    GermanChampion,
}

/// <summary>Ergebnis eines Pokals für „Früher“.</summary>
public enum CupOutcome
{
    NotParticipating,
    Eliminated,
    Winner,
}

/// <summary>Eine Saison eines Managers für „Früher“: Verein, Liga, Platz, DEB-Pokal, Europapokal.</summary>
public sealed record ManagerSeasonHistory(
    int Season, int Club, League League, int Rank, LeagueOutcome LeagueOutcome,
    CupOutcome Deb, int DebRound, CupOutcome Europe, bool EuropeFinalRound);

/// <summary>
/// Statistik über Saisons (Paket „wettbewerbe“): Ewige Tabelle der Vereine, Ewige Bilanz und Saisonverlauf je Manager.
/// Fortgeschrieben genau einmal je Saison am Saisonende (<see cref="RecordSeasonStatistics"/>).
/// </summary>
/// <remarks>
/// [ORIGINAL] Punkte der Ewigen Tabelle je Saison (em.exe Datei 0x5667A):
/// Ligaplatz p (0-basiert) → 12 − p, dazu 40 in der Bundesliga bzw. 20 in der 2. Liga; Vereine ohne Liga 0. Die Liga ist die
/// der <b>nächsten</b> Saison (nach Auf- und Abstieg), der Platz der aus der gespielten Saison (Spur saisonwechsel Teil 3, S-K4).
/// Deutscher Meister +15 und als Bundesligist +10; Erster einer unteren Liga +3; DEB-Pokalsieger +25;
/// Europacupsieger +40. Belegt am Video (16:38, nach der ersten Saison): Berliner SC (8. der Oberliga) 5 Punkte,
/// Mannheimer ERC (1. der Bundesliga) 52, Düsseldorfer EG (2., Meister, Europacupsieger) 116.
/// [ORIGINAL] Das Original führt 42 Vereine (Strukturindex 0–41, A197): Die Vereine auf den Plätzen 42–47 rücken nie in die
/// Oberliga nach (re-saisonwechsel.md §3, DOSBox-Beleg #389) und fehlen in der Ewigen Tabelle (<see cref="GameState.OutsidePlaces"/>).
/// </remarks>
public sealed partial class GameState
{
    private readonly Dictionary<int, EternalEntry> _eternal = [];
    private readonly Dictionary<int, (MatchBalance Home, MatchBalance Away)> _managerBalance = [];
    private readonly Dictionary<int, List<ManagerSeasonHistory>> _managerHistory = [];

    /// <summary>Saison, für die die Statistik zuletzt fortgeschrieben wurde (Schutz gegen doppelte Buchung).</summary>
    public int StatisticsRecordedSeason { get; private set; } = -1;

    /// <summary>Ewige Tabelle der Vereine auf den Plätzen 0–41, sortiert nach Punkten (stabil nach Vereinsindex).</summary>
    public IReadOnlyList<EternalEntry> EternalTable
    {
        get
        {
            var neverPromoted = OutsidePlaces().Skip(OutsideDrawPlaces).ToHashSet();
            return Enumerable.Range(0, NewGame.SelectableClubs).Where(c => !neverPromoted.Contains(c)).Select(Eternal)
                .OrderByDescending(e => e.Points).ThenBy(e => e.Club).ToList();
        }
    }

    /// <summary>Eintrag eines Vereins in der Ewigen Tabelle (0 Punkte, solange er noch keinen hat).</summary>
    public EternalEntry Eternal(int club) =>
        _eternal.TryGetValue(club, out var entry) ? entry : new EternalEntry { Club = club };

    /// <summary>Ewige Bilanz eines Managers (Ligaspiele seines jeweiligen Vereins): zu Hause und auswärts.</summary>
    public (MatchBalance Home, MatchBalance Away) ManagerBalance(int manager) =>
        _managerBalance.TryGetValue(manager, out var b) ? b : (new MatchBalance(), new MatchBalance());

    /// <summary>Abgeschlossene Saisons eines Managers (älteste zuerst).</summary>
    public IReadOnlyList<ManagerSeasonHistory> ManagerHistory(int manager) =>
        _managerHistory.TryGetValue(manager, out var list) ? list : [];

    /// <summary>Punkte der Ewigen Tabelle für eine Saison (siehe Klassenbeschreibung).</summary>
    public static int EternalPoints(League? league, int rank0, bool champion, bool cupWinner, bool europeWinner)
    {
        var points = league switch
        {
            League.Bundesliga => 40 + 12 - rank0,
            League.ZweiteLiga => 20 + 12 - rank0,
            League.Oberliga => 12 - rank0,
            _ => 0,
        };
        if (champion)
            points += 15 + (league == League.Bundesliga ? 10 : 0);
        else if (league is League.ZweiteLiga or League.Oberliga && rank0 == 0)
            points += 3;
        if (cupWinner)
            points += 25;
        if (europeWinner)
            points += 40;
        return points;
    }

    /// <summary>
    /// Saisonende (29. April): Ewige Tabelle, Ewige Bilanz der Manager und „Früher“ fortschreiben. Wird aus
    /// <see cref="FinishSeason"/> nach dem Zwangsabstieg aufgerufen (Spur saisonwechsel Teil 3; vorher aus
    /// <c>GameState.Integration.cs</c> davor); ein zweiter Aufruf in derselben Saison ändert nichts.
    /// </summary>
    internal void RecordSeasonStatistics()
    {
        if (StatisticsRecordedSeason == Season || CurrentRecord is not { } record)
            return;
        StatisticsRecordedSeason = Season;

        int? champion = record.Titles.TryGetValue(Title.GermanChampion, out var c) ? c : null;
        var cupWinner = DebCup?.Winner;
        var europeWinner = EuropeanCup?.Winner;

        for (var club = 0; club < NewGame.SelectableClubs; club++)
        {
            var played = Clubs[club].League;
            var rank0 = played is { } l ? record.FinalTables[l].ToList().IndexOf(club) : -1;
            // [ORIGINAL] Spur saisonwechsel Teil 3 (S-K4): em.exe ruft Datei 0x5667A erst nach dem Austausch (Datei 0x563AF). Die
            // Ligapunkte hängen am neuen Vereinsplatz (Index < 12 → 40, < 24 → 20, > 35 → 0, Datei 0x566B8–0x56718), der Platz am
            // Tabellensatz, der mit dem Verein getauscht wurde (gespielte Saison). DOSBox-Beleg `file14_3`: ES Weißwasser,
            // Aufsteiger als Zweiter der 2. Liga, 51 = 40 + 12 − 1; Krefelder EV (Letzter, in der Oberliga geblieben) 1.
            // [APPROX] Nachrücker in die Oberliga (vorher ohne Liga): Ihr Platzfeld ist nicht gelesen; 0 Punkte wie bisher
            // (`file14_3`: TEV Miesbach höchstens 1 Punkt, hinter Krefeld auf Platz 35).
            var league = rank0 < 0 ? null : record.NextLeague(club, played);
            var points = EternalPoints(league, Math.Max(0, rank0), champion == club, cupWinner == club, europeWinner == club);
            if (points == 0 && champion != club && cupWinner != club && europeWinner != club)
                continue;
            if (!_eternal.TryGetValue(club, out var entry))
                _eternal[club] = entry = new EternalEntry { Club = club };
            entry.Points += points;
            if (champion == club) entry.Championships++;
            if (cupWinner == club) entry.CupWins++;
            if (europeWinner == club) entry.EuropeanCupWins++;
        }

        for (var m = 0; m < Managers.Count; m++)
        {
            var manager = Managers[m];
            if (manager.Dismissed)
                continue;
            var club = manager.ClubIndex;
            if (!_managerBalance.TryGetValue(m, out var balance))
                _managerBalance[m] = balance = (new MatchBalance(), new MatchBalance());
            foreach (var r in Results.Where(r => r.Involves(club)))
            {
                var (goalsFor, against, home) = r.For(club);
                (home ? balance.Home : balance.Away).Add(goalsFor, against);
            }

            if (!_managerHistory.TryGetValue(m, out var list))
                _managerHistory[m] = list = [];
            var league = Clubs[club].League ?? League.Oberliga;
            var rank = record.FinalTables[league].ToList().IndexOf(club) + 1;
            list.Add(new ManagerSeasonHistory(Season, club, league, rank, LeagueOutcomeOf(club, champion),
                DebCup is { } deb && deb.Participants.Contains(club) ? (deb.Winner == club ? CupOutcome.Winner : CupOutcome.Eliminated) : CupOutcome.NotParticipating,
                DebCup?.Rounds.Select((r, i) => (r, i)).LastOrDefault(x => x.r.Any(t => t.Involves(club))).i ?? 0,
                EuropeanCup is { } ec && ec.Draw.Contains(club) ? (ec.Winner == club ? CupOutcome.Winner : CupOutcome.Eliminated) : CupOutcome.NotParticipating,
                EuropeanCup?.FinalRound?.Contains(club) ?? false));
        }
    }

    private LeagueOutcome LeagueOutcomeOf(int club, int? champion)
    {
        if (champion == club)
            return LeagueOutcome.GermanChampion;
        if (Playoffs is not { } playoffs || !playoffs.Seeds.Contains(club))
            return LeagueOutcome.Rank;
        var round = playoffs.Rounds.Select((r, i) => (r, i)).Last(x => x.r.Any(s => s.Involves(club))).i;
        return round switch
        {
            0 => LeagueOutcome.PlayoffQuarterFinal,
            1 => LeagueOutcome.PlayoffSemiFinal,
            _ => LeagueOutcome.PlayoffFinal,
        };
    }
}
