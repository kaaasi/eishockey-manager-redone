namespace EishockeyManager.Core;

/// <summary>
/// Gruppe eines Pokalwettbewerbs (Europapokal: vier Vereine, WM: sechs Länder), jeder gegen jeden.
/// </summary>
/// <remarks>
/// [ORIGINAL] Spielpläne aus em.exe (Texte DGROUP 0x450 ff. und 0x474 ff., Zeiger 0x30C und 0x324): je Runde eine
/// Buchstabenfolge, je zwei Buchstaben eine Paarung (a = erster Verein der Gruppe). Vier Vereine: „abcd“, „dabc“,
/// „acbd“ – belegt im Video (Europacup Gruppe 1: JYP–DEG, Vitkovice–Lion am 4.11.; Lion–JYP, DEG–Vitkovice am
/// 7.11.; JYP–Vitkovice, DEG–Lion am 9.11.). Sechs Vereine: „abcdef“, „dafcbe“, „afcebd“, „cafbed“, „eabcdf“.
/// Rundenzahl je Wettbewerb DGROUP 0x4DD8 (5 bzw. 3), bei Vierergruppen doppelt (Hin- und Rückrunde).
/// [APPROX] Rückrunde: gleiche Paarungen mit getauschtem Heimrecht (Video: zweiter Termin in derselben Zeile).
/// </remarks>
public sealed class CupGroup(int number, IReadOnlyList<int> teams)
{
    public static readonly string[] FourTeamRounds = ["abcd", "dabc", "acbd"];
    public static readonly string[] SixTeamRounds = ["abcdef", "dafcbe", "afcebd", "cafbed", "eabcdf"];

    /// <summary>Nummer der Gruppe (1, 2) oder 0 für die Endrunde.</summary>
    public int Number { get; } = number;

    /// <summary>Vereine in Auslosungsreihenfolge (a, b, c, …).</summary>
    public IReadOnlyList<int> Teams { get; } = teams;

    public List<KnockoutMatch> Results { get; } = [];

    private string[] BaseRounds => Teams.Count == 6 ? SixTeamRounds : FourTeamRounds;

    /// <summary>Spieltage der Gruppe: Vierergruppe Hin- und Rückrunde (6), Sechsergruppe einfach (5).</summary>
    public int RoundCount => Teams.Count == 6 ? SixTeamRounds.Length : 2 * FourTeamRounds.Length;

    /// <summary>Paarungen (Heim, Gast) der Runde <paramref name="round"/> (0-basiert).</summary>
    public IReadOnlyList<(int Home, int Away)> Pairings(int round)
    {
        var rounds = BaseRounds;
        var letters = rounds[round % rounds.Length];
        var swap = round >= rounds.Length;
        var list = new List<(int, int)>();
        for (var i = 0; i + 1 < letters.Length; i += 2)
        {
            var a = Teams[letters[i] - 'a'];
            var b = Teams[letters[i + 1] - 'a'];
            list.Add(swap ? (b, a) : (a, b));
        }
        return list;
    }

    public bool Contains(int club) => Teams.Contains(club);

    /// <summary>
    /// Tabelle der Gruppe. [APPROX] Reihenfolge wie in der Liga (Pluspunkte, Minuspunkte, Tordifferenz, Tore),
    /// bei Gleichstand die Auslosungsreihenfolge. Das Original zeigt Punkte und Tore („0: 0  0: 0“, Video 13:11).
    /// </summary>
    public IReadOnlyList<TableRow> Table(IReadOnlyList<Club> clubs)
    {
        var rows = Teams.ToDictionary(c => c, c => new TableRow { Club = clubs[c] });
        foreach (var r in Results)
        {
            Add(rows[r.Home], r.HomeGoals, r.AwayGoals, true);
            Add(rows[r.Away], r.AwayGoals, r.HomeGoals, false);
        }
        var order = Teams.ToList();
        var list = rows.Values
            .OrderByDescending(r => r.PointsPlus)
            .ThenBy(r => r.PointsMinus)
            .ThenByDescending(r => r.GoalDifference)
            .ThenByDescending(r => r.GoalsFor)
            .ThenBy(r => order.IndexOf(r.Club.Index))
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

    /// <summary>Spielt Runde <paramref name="round"/>: Unentschieden sind möglich (Gruppenspiel, kein Penaltyschießen).</summary>
    public IReadOnlyList<KnockoutMatch> Play(int round, int day, IReadOnlyList<Club> clubs, Random random)
    {
        var played = Pairings(round).Select(p =>
        {
            var (hg, ag) = MatchEngine.Play(clubs[p.Home], clubs[p.Away], random);
            return new KnockoutMatch(day, p.Home, p.Away, hg, ag);
        }).ToList();
        Results.AddRange(played);
        return played;
    }
}
