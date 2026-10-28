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
/// Ligaplatz p (0-basiert) → 12 − p, dazu 40 in der Bundesliga bzw. 20 in der 2. Liga; Vereine ohne Liga 0.
/// Deutscher Meister +15 und als Bundesligist +10; Erster einer unteren Liga +3; DEB-Pokalsieger +25;
/// Europacupsieger +40. Belegt am Video (16:38, nach der ersten Saison): Berliner SC (8. der Oberliga) 5 Punkte,
/// Mannheimer ERC (1. der Bundesliga) 52, Düsseldorfer EG (2., Meister, Europacupsieger) 116.
/// [APPROX] Das Original führt 42 Vereine (Strukturindex 0–41), das Remake alle 48 wählbaren Vereine.
/// </remarks>
public sealed partial class GameState
{
    private readonly Dictionary<int, EternalEntry> _eternal = [];
    private readonly Dictionary<int, (MatchBalance Home, MatchBalance Away)> _managerBalance = [];
    private readonly Dictionary<int, List<ManagerSeasonHistory>> _managerHistory = [];

    /// <summary>Saison, für die die Statistik zuletzt fortgeschrieben wurde (Schutz gegen doppelte Buchung).</summary>
    public int StatisticsRecordedSeason { get; private set; } = -1;

    /// <summary>Ewige Tabelle aller wählbaren Vereine, sortiert nach Punkten (stabil nach Vereinsindex).</summary>
    public IReadOnlyList<EternalEntry> EternalTable =>
        Enumerable.Range(0, NewGame.SelectableClubs).Select(Eternal)
            .OrderByDescending(e => e.Points).ThenBy(e => e.Club).ToList();

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
    /// Saisonende (29. April, vor der Titelvergabe): Ewige Tabelle, Ewige Bilanz der Manager und „Früher“ fortschreiben.
    /// Wird aus <c>GameState.Integration.cs</c> genau für diesen Tag aufgerufen; ein zweiter Aufruf in derselben
    /// Saison ändert nichts.
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
            var league = Clubs[club].League;
            var rank0 = league is { } l ? record.FinalTables[l].ToList().IndexOf(club) : -1;
            var points = EternalPoints(rank0 < 0 ? null : league, Math.Max(0, rank0), champion == club, cupWinner == club, europeWinner == club);
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
