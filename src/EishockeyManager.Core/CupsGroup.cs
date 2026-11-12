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
/// [ORIGINAL] Rückrunde: gleiche Paarungen mit getauschtem Heimrecht. Die Gruppenpläne entstehen in Datei 0x30427 über die
/// Spielplanfunktion Datei 0x4E7C7 (Aufruf Datei 0x3068B mit n = 4 bzw. 6, Spieltag 1 … DGROUP 0x4DD8 + 3 · (Wettbewerb > 0),
/// Schleife Datei 0x3071D–0x30744); für Spieltag d ≥ n zieht sie n − 1 ab und tauscht das Heimrecht (re-nachtrag.md §4,
/// Spur wettbewerbe-rest Teil 3). Video: zweiter Termin in derselben Zeile.
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
    /// Tabelle der Gruppe nach den bisher gespielten Runden (je Spieltag eine Runde).
    /// </summary>
    /// <remarks>
    /// [ORIGINAL] em.exe 0x30852 (Wettbewerb 0 = WM, 2 = Europapokal-Gruppen, 3 = Endrunde): nach jedem Spieltag trägt
    /// 0x303D9 Punkte (Sieg 2, Unentschieden 1), Tore und Gegentore je Gruppenplatz ein (DGROUP 0xCD58, 0xCD28, 0xCCF8) und
    /// sortiert danach die Platzliste der Gruppe (DGROUP 0xCD88) an Ort und Stelle; siehe <see cref="Standings"/>.
    /// Ausgangsreihenfolge = Plätze der Auslosung (0x30427, Datei 0x30831). Angezeigt wird diese Liste (WM 0x2F5B6,
    /// Europapokal 0x31CD5); die Endrunde nimmt Erster und Zweiter je Gruppe daraus (0x305D4).
    /// </remarks>
    public IReadOnlyList<TableRow> Table(IReadOnlyList<Club> clubs) =>
        Standings(clubs, Teams, Results.GroupBy(r => r.Day)
            .Select(day => day.Select(r => (r.Home, r.Away, r.HomeGoals, r.AwayGoals))));

    /// <summary>
    /// [ORIGINAL] Gruppentabelle wie em.exe 0x30852 (Datei 0x30C36–0x30E73): je Runde Ergebnisse eintragen, dann
    /// Tauschsortierung der Platzliste (für jeden Platz i alle späteren j). j verdrängt i bei mehr Punkten, bei gleichen
    /// Punkten bei höherer Tordifferenz und bei gleicher Tordifferenz, wenn i <b>nicht mehr</b> Tore hat (Datei 0x30D89:
    /// „jna“ – voller Gleichstand tauscht also, anders als in der Liga). Spiele zählen nicht (alle Vereine einer Gruppe
    /// spielen je Runde einmal).
    /// </summary>
    /// <param name="slots">Vereine in der Ausgangsreihenfolge (Plätze der Auslosung).</param>
    /// <param name="rounds">Ergebnisse je Runde, in Spielfolge.</param>
    public static IReadOnlyList<TableRow> Standings(IReadOnlyList<Club> clubs, IReadOnlyList<int> slots,
        IEnumerable<IEnumerable<(int Home, int Away, int HomeGoals, int AwayGoals)>> rounds)
    {
        var rows = slots.ToDictionary(c => c, c => new TableRow { Club = clubs[c] });
        var order = slots.ToList();
        foreach (var round in rounds)
        {
            foreach (var (home, away, homeGoals, awayGoals) in round)
            {
                if (!rows.TryGetValue(home, out var h) || !rows.TryGetValue(away, out var a))
                    continue;
                Add(h, homeGoals, awayGoals, true);
                Add(a, awayGoals, homeGoals, false);
            }
            LeagueTable.ExchangeSort(order, (b, a) => Displaces(rows[b], rows[a]));
        }
        var list = order.Select(c => rows[c]).ToList();
        for (var i = 0; i < list.Count; i++)
            list[i].Rank = i + 1;
        return list;
    }

    /// <summary>[ORIGINAL] Vergleich von 0x30852 (Datei 0x30CB6–0x30D91): verdrängt <paramref name="b"/> (späterer Platz) <paramref name="a"/>?</summary>
    private static bool Displaces(TableRow b, TableRow a)
    {
        if (b.PointsPlus != a.PointsPlus)
            return b.PointsPlus > a.PointsPlus;
        if (b.GoalDifference != a.GoalDifference)
            return b.GoalDifference > a.GoalDifference;
        return a.GoalsFor <= b.GoalsFor;
    }

    private static void Add(TableRow row, int goalsFor, int goalsAgainst, bool home)
    {
        row.GoalsFor += goalsFor;
        row.GoalsAgainst += goalsAgainst;
        char c;
        if (goalsFor > goalsAgainst) { row.Wins++; c = 'S'; }
        else if (goalsFor == goalsAgainst) { row.Draws++; c = 'U'; }
        else { row.Losses++; c = 'N'; }
        // [ORIGINAL] wie 0x50283 (DGROUP 0x7CE „sSnNuU“): Heimverein klein, Gast groß. Eine Bilanz führt das Original nur
        // für Liga und WM (Code 7/8); die Gruppenansichten zeigen sie nicht.
        row.Form.Add(home ? char.ToLowerInvariant(c) : c);
    }

    /// <summary>Spielt Runde <paramref name="round"/>: Unentschieden sind möglich (Gruppenspiel, kein Penaltyschießen).</summary>
    public IReadOnlyList<KnockoutMatch> Play(int round, int day, GameState game, Random random)
    {
        var clubs = game.Clubs;
        var played = Pairings(round).Select(p =>
        {
            var report = MatchEngine.PlayWithEvents(game, clubs[p.Home], clubs[p.Away], MatchCompetition.EuropeanCup, knockOut: false, random);
            var (hg, ag) = (report.HomeGoals, report.AwayGoals);
            return new KnockoutMatch(day, p.Home, p.Away, hg, ag);
        }).ToList();
        Results.AddRange(played);
        return played;
    }
}
