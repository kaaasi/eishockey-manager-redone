namespace EishockeyManager.Core;

/// <summary>
/// Kalender einer Saison. Beobachtungen aus Aufnahmen des Originals:
/// <list type="bullet">
/// <item>Jede Saison beginnt am „Sonntag, 10. Sept.“ – unabhängig vom echten Wochentag des Jahres.
///   Die Wochentage folgen dem Kalender 1995/96 (dort war der 10.09. ein Sonntag).</item>
/// <item>Ligaspiele finden sonntags und mittwochs statt: 17. Spieltag = Sonntag, 5. Nov.;
///   37. Spieltag = Mittwoch, 17. Januar. Dazu passt genau ein ausgelassener Termin (Heiligabend).</item>
/// </list>
/// </summary>
public static class SeasonCalendar
{
    /// <summary>Tag 0 einer Saison im Kalenderlayout.</summary>
    public static readonly DateOnly Layout = new(1995, 9, 10);

    private static readonly string[] Weekdays = ["Sonntag", "Montag", "Dienstag", "Mittwoch", "Donnerstag", "Freitag", "Samstag"];

    /// <summary>[APPROX] Monatsnamen: kurze ausgeschrieben, lange abgekürzt (belegt: „Sept.“, „Nov.“, „Januar“).</summary>
    private static readonly string[] Months = ["Januar", "Februar", "März", "April", "Mai", "Juni", "Juli", "Aug.", "Sept.", "Okt.", "Nov.", "Dez."];

    public static DateOnly LayoutDate(int day) => Layout.AddDays(day);

    /// <summary>Kalenderjahr eines Saisontages (ab Januar das Folgejahr).</summary>
    public static int Year(int day, int season) => LayoutDate(day).Month >= 9 ? season : season + 1;

    /// <summary>Datumszeile der Infoleiste, z. B. „Sonntag, 10. Sept.  1993“.</summary>
    public static string Format(int day, int season)
    {
        var d = LayoutDate(day);
        return $"{Weekdays[(int)d.DayOfWeek]}, {d.Day}. {Months[d.Month - 1]}  {Year(day, season)}";
    }

    /// <summary>Kurzes Datum für Meldungen, z. B. „1. Nov. 1995“.</summary>
    public static string FormatShort(int day, int season)
    {
        var d = LayoutDate(day);
        return $"{d.Day}. {Months[d.Month - 1]} {Year(day, season)}";
    }

    /// <summary>Tage (seit Saisonbeginn) der Ligaspieltage: sonntags und mittwochs ab Tag 0, ohne Heiligabend.</summary>
    public static IReadOnlyList<int> LeagueMatchDays(int count)
    {
        var days = new List<int>(count);
        for (var day = 0; days.Count < count; day++)
        {
            var d = LayoutDate(day);
            if (d.DayOfWeek is not (DayOfWeek.Sunday or DayOfWeek.Wednesday))
                continue;
            if (d is { Month: 12, Day: 24 })
                continue;
            days.Add(day);
        }
        return days;
    }
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
