namespace EishockeyManager.Core;

/// <summary>Was an einem Tag der Saison stattfindet (Bitmaske wie in der Kalendertabelle von em.exe).</summary>
[Flags]
public enum DayEvents : byte
{
    None = 0,

    /// <summary>Ligaspieltag. Das Original setzt drei Bits (vermutlich je Liga eins); alle Ligen spielen gemeinsam.</summary>
    League = 7,
    DebCup = 8,
    WorldCup = 16,
    EuropeanCup = 32,

    /// <summary>Play-Off und Aufstiegsrunden.</summary>
    Playoffs = 64,

    /// <summary>Markierung 255: Ende der Vereinssaison (13. April) bzw. Saisonende (29. April).</summary>
    SeasonEnd = 255,
}

/// <summary>
/// Kalender einer Saison. [ORIGINAL] Die Termine stammen aus der Kalendertabelle in em.exe
/// (Dateioffset 0x175F9, ein Byte je Tag ab dem 10. September, Werte siehe <see cref="DayEvents"/>).
/// Belegt in den Videos: 17. Spieltag = Sonntag, 5. Nov.; 37. Spieltag = Mittwoch, 17. Januar;
/// 43. Spieltag = Freitag, 2. Feb.; 44. Spieltag = Sonntag, 4. Feb.; Saisonende = Montag, 29. April.
/// <list type="bullet">
/// <item>Jede Saison beginnt am „Sonntag, 10. Sept.“ – unabhängig vom echten Wochentag des Jahres.</item>
/// <item>Das Original kennt keinen 29. Februar: „Donnerstag, 22. März“ und „Samstag, 14. April“ (Video) passen
///   nur zu einem Kalender ohne Schalttag. Wochentage folgen daher dem Kalender 1989/90.</item>
/// </list>
/// </summary>
public static class SeasonCalendar
{
    /// <summary>Tag 0 einer Saison im Kalenderlayout (Sonntag, 10.09.1989; 1990 ist kein Schaltjahr).</summary>
    public static readonly DateOnly Layout = new(1989, 9, 10);

    /// <summary>Letzter Tag der Vereinssaison (Freitag, 13. April). Danach zeigt das Hauptmenü „Saisonende“.</summary>
    public const int ClubSeasonEnd = 215;

    /// <summary>Saisonende (Montag, 29. April): Bildschirm „Saisonende“, danach beginnt die neue Saison.</summary>
    public const int SeasonEnd = 231;

    public const int Length = SeasonEnd + 1;

    private static readonly int[] LeagueDays =
    [
        0, 3, 7, 10, 17, 21, 24, 26, 28, 31, 35, 38, 45, 49, 52, 54, 56, 63, 66, 70, 80, 84,
        87, 89, 91, 94, 98, 101, 105, 108, 112, 115, 117, 119, 122, 126, 129, 133, 136, 138, 140, 143, 145, 147,
    ];
    private static readonly int[] DebCupDays = [12, 14, 19, 40, 42, 47, 96, 103, 110, 124, 125, 132, 198, 202, 209];
    private static readonly int[] EuropeanCupDays = [55, 58, 60, 62, 65, 67, 69, 72, 74, 76, 78, 81];
    private static readonly int[] PlayoffDays = [149, 151, 153, 156, 158, 163, 165, 168, 182, 188, 193, 195, 200, 205, 207];
    private static readonly int[] WorldCupDays = [216, 218, 220, 222, 224, 226, 228, 230];

    private static readonly DayEvents[] Table = BuildTable();

    private static DayEvents[] BuildTable()
    {
        var table = new DayEvents[Length];
        foreach (var d in LeagueDays) table[d] |= DayEvents.League;
        foreach (var d in DebCupDays) table[d] |= DayEvents.DebCup;
        foreach (var d in EuropeanCupDays) table[d] |= DayEvents.EuropeanCup;
        foreach (var d in PlayoffDays) table[d] |= DayEvents.Playoffs;
        foreach (var d in WorldCupDays) table[d] |= DayEvents.WorldCup;
        table[ClubSeasonEnd] = DayEvents.SeasonEnd;
        table[SeasonEnd] = DayEvents.SeasonEnd;
        return table;
    }

    /// <summary>Kalendertabelle wie im Original (ein Byte je Tag, Tag 0 = 10. September).</summary>
    public static ReadOnlySpan<DayEvents> Days => Table;

    public static DayEvents Events(int day) => (uint)day < Length ? Table[day] : DayEvents.None;

    /// <summary>Tage (seit Saisonbeginn) der Termine eines Wettbewerbs, aufsteigend.</summary>
    public static IReadOnlyList<int> DaysOf(DayEvents kind) => kind switch
    {
        DayEvents.League => LeagueDays,
        DayEvents.DebCup => DebCupDays,
        DayEvents.EuropeanCup => EuropeanCupDays,
        DayEvents.Playoffs => PlayoffDays,
        DayEvents.WorldCup => WorldCupDays,
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    private static readonly string[] Weekdays = ["Sonntag", "Montag", "Dienstag", "Mittwoch", "Donnerstag", "Freitag", "Samstag"];

    /// <summary>[APPROX] Monatsnamen: kurze ausgeschrieben, lange abgekürzt (belegt: „Sept.“, „Okt.“, „Nov.“, „Dez.“, „Januar“, „Februar“, „März“, „April“).</summary>
    private static readonly string[] Months = ["Januar", "Februar", "März", "April", "Mai", "Juni", "Juli", "Aug.", "Sept.", "Okt.", "Nov.", "Dez."];

    public static DateOnly LayoutDate(int day) => Layout.AddDays(day);

    /// <summary>Kalenderjahr eines Saisontages (ab Januar das Folgejahr).</summary>
    public static int Year(int day, int season) => LayoutDate(day).Month >= 9 ? season : season + 1;

    /// <summary>Datumszeile der Infoleiste, z. B. „Sonntag, 10. Sept.  1993“.</summary>
    public static string Format(int day, int season)
    {
        var d = LayoutDate(day);
        // [APPROX] Ursache unbekannt: Das Original zeigt am Saisonende „Montag, 29. April“, obwohl der Kalender sonst einen Sonntag ergibt
        // (beide Saisonenden im Video). Der Wochentag wird dort wie im Original um eins weitergezählt.
        var weekday = ((int)d.DayOfWeek + (day == SeasonEnd ? 1 : 0)) % 7;
        return $"{Weekdays[weekday]}, {d.Day}. {Months[d.Month - 1]}  {Year(day, season)}";
    }

    /// <summary>Kurzes Datum für Meldungen, z. B. „1. Nov. 1995“.</summary>
    public static string FormatShort(int day, int season)
    {
        var d = LayoutDate(day);
        return $"{d.Day}. {Months[d.Month - 1]} {Year(day, season)}";
    }

    /// <summary>Tage (seit Saisonbeginn) der ersten <paramref name="count"/> Ligaspieltage.</summary>
    public static IReadOnlyList<int> LeagueMatchDays(int count) =>
        count <= LeagueDays.Length ? LeagueDays[..count] : throw new ArgumentOutOfRangeException(nameof(count));
}

public sealed record MatchResult(League League, int MatchDay, int Home, int Away, int HomeGoals, int AwayGoals)
{
    public bool Involves(int club) => Home == club || Away == club;

    /// <summary>Ergebnis aus Sicht eines Vereins: (eigene Tore, Gegentore, Heimspiel).</summary>
    public (int For, int Against, bool Home) For(int club) =>
        club == Home ? (HomeGoals, AwayGoals, true) : (AwayGoals, HomeGoals, false);
}

/// <summary>
/// Spielberechnung ohne Live-Spiel.
/// [APPROX] Die Formel des Originals (Overlay 5E73) ist noch nicht rekonstruiert. Torerwartung aus der
/// Stärkedifferenz, leichter Heimvorteil, Tore poissonverteilt; kalibriert auf rund 6,5 Tore je Spiel.
/// Unentschieden sind möglich (im Original in der Liga belegt).
/// </summary>
public static class MatchEngine
{
    public static (int Home, int Away) Play(Club home, Club away, Random random)
    {
        // Spur heimspiel-statistik (#165): [ORIGINAL] 0x36DC5 – nicht spielfähiger Managerverein, Wertung ohne Spielberechnung
        // (kein Zufallszug). Heimverein zuerst, siehe GameState.MarkShortHandedClubs.
        if (home.ForfeitsMatch)
            return (0, 2);
        if (away.ForfeitsMatch)
            return (2, 0);
        var diff = (home.MatchStrength - away.MatchStrength) / 30.0; // Stärke aus der Aufstellung (Paket „mannschaft“)
        var lambdaHome = 3.45 * Math.Exp(0.45 * diff);
        var lambdaAway = 3.05 * Math.Exp(-0.45 * diff);
        return (Poisson(lambdaHome, random), Poisson(lambdaAway, random));
    }

    private static int Poisson(double lambda, Random random)
    {
        var limit = Math.Exp(-lambda);
        var k = 0;
        var p = random.NextDouble();
        while (p > limit)
        {
            k++;
            p *= random.NextDouble();
        }
        return k;
    }
}

public enum TableView
{
    Heim,
    Gesamt,
    Auswaerts,
}

/// <summary>Eine Tabellenzeile.</summary>
public sealed class TableRow
{
    public required Club Club { get; init; }
    public int Rank { get; set; }

    /// <summary>Platz nach dem vorigen Spieltag (für die Tendenz), 0 = unbekannt.</summary>
    public int PreviousRank { get; set; }

    public int Games => Wins + Draws + Losses;
    public int Wins { get; set; }
    public int Draws { get; set; }
    public int Losses { get; set; }
    public int GoalsFor { get; set; }
    public int GoalsAgainst { get; set; }
    public int GoalDifference => GoalsFor - GoalsAgainst;

    /// <summary>Pluspunkte nach der 2-Punkte-Regel.</summary>
    public int PointsPlus => 2 * Wins + Draws;

    /// <summary>Minuspunkte nach der 2-Punkte-Regel.</summary>
    public int PointsMinus => 2 * Losses + Draws;

    /// <summary>Letzte Spiele, älteste zuerst: S/U/N, groß = Heimspiel, klein = Auswärtsspiel.</summary>
    public List<char> Form { get; } = [];

    /// <summary>Bilanz der letzten acht Spiele wie im Original.</summary>
    public string Balance => new(Form.TakeLast(8).ToArray());
}

public static class LeagueTable
{
    public const int BalanceLength = 8;

    /// <summary>Tabelle nach Spieltag <paramref name="upToMatchDay"/> (1-basiert) mit Tendenz zum Vortag.</summary>
    public static IReadOnlyList<TableRow> Compute(GameState game, League league, TableView view, int upToMatchDay)
    {
        var sorted = ComputeRanks(game, league, view, upToMatchDay);
        if (upToMatchDay > 1)
        {
            var before = ComputeRanks(game, league, view, upToMatchDay - 1).ToDictionary(r => r.Club.Index, r => r.Rank);
            foreach (var row in sorted)
                row.PreviousRank = before.GetValueOrDefault(row.Club.Index);
        }
        return sorted;
    }

    private static List<TableRow> ComputeRanks(GameState game, League league, TableView view, int upToMatchDay)
    {
        var rows = game.LeagueClubs(league).ToDictionary(c => c.Index, c => new TableRow { Club = c });
        foreach (var r in game.Results)
        {
            if (r.League != league || r.MatchDay > upToMatchDay)
                continue;
            if (view != TableView.Auswaerts && rows.TryGetValue(r.Home, out var home))
                Add(home, r.HomeGoals, r.AwayGoals, true);
            if (view != TableView.Heim && rows.TryGetValue(r.Away, out var away))
                Add(away, r.AwayGoals, r.HomeGoals, false);
        }

        return Sort(rows.Values);
    }

    /// <summary>[APPROX] Reihenfolge: Pluspunkte, Minuspunkte, Tordifferenz, erzielte Tore.</summary>
    private static List<TableRow> Sort(IEnumerable<TableRow> rows)
    {
        var list = rows
            .OrderByDescending(r => r.PointsPlus)
            .ThenBy(r => r.PointsMinus)
            .ThenByDescending(r => r.GoalDifference)
            .ThenByDescending(r => r.GoalsFor)
            .ThenBy(r => r.Club.Index)
            .ToList();
        for (var i = 0; i < list.Count; i++)
            list[i].Rank = i + 1;
        return list;
    }

    private static void Add(TableRow row, int goalsFor, int goalsAgainst, bool home)
    {
        row.GoalsFor += goalsFor;
        row.GoalsAgainst += goalsAgainst;
        char c;
        if (goalsFor > goalsAgainst) { row.Wins++; c = 'S'; }
        else if (goalsFor == goalsAgainst) { row.Draws++; c = 'U'; }
        else { row.Losses++; c = 'N'; }
        row.Form.Add(home ? c : char.ToLowerInvariant(c));
    }
}
