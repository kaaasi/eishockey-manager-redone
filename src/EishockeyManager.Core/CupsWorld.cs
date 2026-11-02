namespace EishockeyManager.Core;

/// <summary>
/// Spielwerte der zwölf Nationalmannschaften (Datenbank 52–63) aus em.exe.
/// </summary>
/// <remarks>
/// [ORIGINAL] DGROUP 0x4C4E: je Land vier Bytes Kondition Abwehr, Technik Abwehr, Kondition Sturm, Technik Sturm,
/// vom Lader um 2 vermindert (Datei 0x2EFDE ff.); die Datenbankwerte der Nationalteams wirken nicht.
/// Belegt am Video „Stärken Länderteams – Gesamt“ (13:18), dessen Spalten das Mittel aus Abwehr und Sturm zeigen
/// (Bildschirm 0x51206, auf 99 begrenzt): Russland Kondition 99 / Technik 99, Kanada 98 / 93, Schweden 91 / 99,
/// Deutschland 88 / 91.
/// </remarks>
public static class NationalTeamRatings
{
    private static readonly byte[] Raw =
    [
        99, 105, 104, 108, 97, 102, 90, 100, 98, 93, 103, 98, 92, 92, 97, 95, 86, 93, 95, 93, 89, 91, 84, 96,
        90, 87, 80, 80, 69, 73, 84, 88, 89, 87, 92, 88, 73, 84, 75, 78, 82, 84, 66, 82, 102, 99, 94, 97,
    ];

    /// <summary>Kondition und Technik von Abwehr und Sturm der Nationalmannschaft <paramref name="club"/> (52–63).</summary>
    public static (int ConditionDefense, int TechniqueDefense, int ConditionAttack, int TechniqueAttack) Of(int club)
    {
        var i = club - NationalTeams.First;
        if ((uint)i >= 12)
            throw new ArgumentOutOfRangeException(nameof(club));
        return (Raw[4 * i] - 2, Raw[4 * i + 1] - 2, Raw[4 * i + 2] - 2, Raw[4 * i + 3] - 2);
    }

    /// <summary>Kondition und Technik als Mittel aus Abwehr und Sturm (ungerundet, über 99 möglich).</summary>
    public static (double Condition, double Technique) Average(int club)
    {
        var (cd, td, ca, ta) = Of(club);
        return ((cd + ca) / 2.0, (td + ta) / 2.0);
    }

    /// <summary>[APPROX] Spielstärke für die Spielberechnung: Mittel aus Kondition und Technik (das Remake rechnet mit einem Wert).</summary>
    public static int MatchStrength(int club)
    {
        var (condition, technique) = Average(club);
        return (int)((condition + technique) / 2);
    }
}

/// <summary>
/// Weltmeisterschaft: zwei Gruppen à sechs Länder (jeder gegen jeden), danach Viertelfinale, Halbfinale und
/// Platzierungsspiele. Acht Termine vom 14. bis 28. April, nach der Vereinssaison.
/// </summary>
/// <remarks>
/// Belege aus em.exe (Datei 0x30E7B, Zähler DGROUP 0x7AE):
/// <list type="bullet">
/// <item>[ORIGINAL] Gruppen 0x30427 (Wettbewerb 0): Gruppe g = sechs aufeinanderfolgende Nationalteams ab DGROUP 0x6AD + 6g;
///   Spielplan „abcdef / dafcbe / afcebd / cafbed / eabcdf“ (<see cref="CupGroup"/>), Termine 1–5.</item>
/// <item>[ORIGINAL] Nach dem fünften Termin Viertelfinale (0x30F8B): k-ter der Gruppe 1 gegen (5−k)-ten der Gruppe 2,
///   Gruppe 1 mit Heimrecht – A1–B4, A2–B3, A3–B2, A4–B1.</item>
/// <item>[ORIGINAL] Nach jedem K.-o.-Termin steht der Sieger vorn; Weiterverteilung nach DGROUP 0x12C
///   (9, 13, 10, 14, 11, 15, 12, 16, 17, 19, 18, 20, 21, 23, 22, 24): Termin 7 = Halbfinale (Sieger VF1–VF2, VF3–VF4)
///   und „SPIELE 5.-8.“ (Verlierer VF1–VF2, VF3–VF4); Termin 8 = „ENDSPIEL“, „SPIEL UM 3.PL.“, „SPIEL UM 5.PL.“,
///   „SPIEL UM 7.PL.“. Platz = Spiel · 2 + 1 (Sieger) bzw. + 2 (Verlierer).</item>
/// <item>[ORIGINAL] Der Sieger des Endspiels ist Weltmeister (DGROUP 0xD0F3); das Meisterfoto erscheint für jedes Land
///   (Aufruf 0x31378 ohne Bedingung).</item>
/// <item>[ORIGINAL] Reihenfolge der Länder: Beim Spielstart und zu jeder neuen Saison tauscht das Original 20-mal zwei
///   zufällige Nationalteams (Datei 0x35EDD, je random(52, 63); Aufrufe Datei 0x43A17 und 0x5644E). Die Tausche wirken
///   fort, die Gruppen sind die Plätze 1–6 und 7–12 dieser Reihenfolge.</item>
/// <item>[ORIGINAL] Spielwerte: <see cref="GameState.NationalMatchStrength"/> – ohne Bundestrainer werden die Werte
///   Deutschlands vor der WM neu ausgewürfelt, mit Bundestrainer zählt dessen Aufstellung ([APPROX] Stärkeformel, siehe
///   <see cref="NationalLineup.Strength"/>).</item>
/// </list>
/// [ORIGINAL] Unentschieden im K.-o.: schneller Weg random(0, 2) je Seite bis ungleich, Endstand mit Marke + 30 (0x376F0;
/// <see cref="CupMatch"/>, <see cref="MarksOf"/>). [APPROX] Plätze 9–12 nach Gruppenplatz (WM-Tabelle nicht gelesen).
/// </remarks>
public sealed class WorldCup : ISeasonCompetition
{
    public const int Teams = 12;
    public const int GroupSize = 6;
    public const int GroupDates = 5;

    /// <summary>[ORIGINAL] Spielnamen der K.-o.-Phase (Texte DGROUP 0x1D2A ff.).</summary>
    public static readonly string[][] KnockoutNames =
    [
        ["VIERTELFINALE", "VIERTELFINALE", "VIERTELFINALE", "VIERTELFINALE"],
        ["HALBFINALE", "HALBFINALE", "SPIELE 5.-8.", "SPIELE 5.-8."],
        ["ENDSPIEL", "SPIEL UM 3.PL.", "SPIEL UM 5.PL.", "SPIEL UM 7.PL."],
    ];

    /// <summary>[ORIGINAL] DGROUP 0x12C: neuer Platz (1-basiert) von Platz i nach einem K.-o.-Termin (Sieger gerade, Verlierer ungerade).</summary>
    private static readonly int[] Advance = [9, 13, 10, 14, 11, 15, 12, 16, 17, 19, 18, 20, 21, 23, 22, 24];

    /// <summary>[ORIGINAL] Zahl der Tausche je Saison (Datei 0x35FC9: Schleife bis 0x14).</summary>
    public const int Swaps = 20;

    private readonly List<int> _order = Enumerable.Range(NationalTeams.First, Teams).ToList();
    private readonly List<CupGroup> _groups = [];
    private readonly List<List<KnockoutMatch>> _knockout = [];
    private readonly List<List<int>> _knockoutMarks = [];
    private readonly int?[] _slots = new int?[24];

    public Competition Kind => Competition.WorldCup;
    public DayEvents Days => DayEvents.WorldCup;
    public Title? Title => Core.Title.WorldChampion;

    public IReadOnlyList<CupGroup> Groups => _groups;

    /// <summary>Gespielte K.-o.-Termine: [0] Viertelfinale, [1] Halbfinale + Spiele 5.–8., [2] Endspiel + Platzierungsspiele.</summary>
    public IReadOnlyList<IReadOnlyList<KnockoutMatch>> Knockout => _knockout;

    /// <summary>
    /// Ergebnismarken des Spiels <paramref name="index"/> am K.-o.-Termin <paramref name="stage"/> (0 regulär, 1 Penaltyschießen,
    /// 2 Verlängerung; <see cref="CupMatch"/>). Fehlt der Eintrag, 1 bei Penaltysieger, sonst 0.
    /// </summary>
    public int MarksOf(int stage, int index) =>
        stage < _knockoutMarks.Count && index < _knockoutMarks[stage].Count
            ? _knockoutMarks[stage][index]
            : _knockout[stage][index].PenaltyWinner is null ? 0 : 1;

    public int? Winner { get; private set; }

    /// <summary>Endstand: Platz 1–12 (Vereinsindizes), erst nach dem letzten Termin vollständig.</summary>
    public IReadOnlyList<int> Ranking { get; private set; } = [];

    public IEnumerable<KnockoutMatch> Matches =>
        _groups.SelectMany(g => g.Results).Concat(_knockout.SelectMany(k => k)).OrderBy(m => m.Day);

    public static int DateIndex(int day) => SeasonCalendar.DaysOf(DayEvents.WorldCup).ToList().IndexOf(day);

    public void StartSeason(GameState game)
    {
        _groups.Clear();
        _knockout.Clear();
        _knockoutMarks.Clear();
        Array.Fill(_slots, null);
        Winner = null;
        Ranking = [];
        Mix(_order, game.CupRandom);
        _groups.Add(new CupGroup(1, _order.Take(GroupSize).ToList()));
        _groups.Add(new CupGroup(2, _order.Skip(GroupSize).ToList()));
        game.StartNationalSeason();
    }

    /// <summary>Paarungen (Heim, Gast) eines K.-o.-Termins 0–2 oder leer, solange sie nicht feststehen.</summary>
    public IReadOnlyList<(int Home, int Away)> KnockoutPairings(int stage)
    {
        var list = new List<(int, int)>();
        for (var k = 0; k < 4; k++)
        {
            var at = 8 * stage + 2 * k;
            if (_slots[at] is not { } home || _slots[at + 1] is not { } away)
                return [];
            list.Add((home, away));
        }
        return list;
    }

    private IReadOnlyList<(int Home, int Away)> PairingsOn(int date) =>
        date < GroupDates ? _groups.SelectMany(g => g.Pairings(date)).ToList() : KnockoutPairings(date - GroupDates);

    public (int Opponent, bool Home)? MatchOn(GameState game, int day, int club)
    {
        var date = DateIndex(day);
        if (date < 0 || Winner is not null)
            return null;
        foreach (var (home, away) in PairingsOn(date))
            if (home == club || away == club)
                return (home == club ? away : home, home == club);
        return null;
    }

    /// <summary>[ORIGINAL] Datei 0x35EDD: 20-mal zwei Plätze random(0, 11) tauschen (auch denselben Platz).</summary>
    public static void Mix(IList<int> order, Random random)
    {
        for (var n = 0; n < Swaps; n++)
        {
            var a = random.Next(0, order.Count);
            var b = random.Next(0, order.Count);
            (order[a], order[b]) = (order[b], order[a]);
        }
    }

    /// <summary>Reihenfolge der Länder nach den bisherigen Tauschen (Plätze 1–6 = Gruppe 1).</summary>
    public IReadOnlyList<int> Order => _order;

    public void PlayDay(GameState game, int day, List<SeasonStep> steps)
    {
        var date = DateIndex(day);
        if (date < 0 || Winner is not null)
            return;
        var random = game.CupRandom;
        List<KnockoutMatch> played;
        if (date < GroupDates)
        {
            played = [];
            foreach (var group in _groups)
                foreach (var (home, away) in group.Pairings(date))
                {
                    game.PrepareNationalTactics(home, away); // Spielvorbereitung 0x8365 je Paarung (#192)
                    var report = MatchEngine.PlayWithEvents(game, game.RatedNation(home), game.RatedNation(away),
                        MatchCompetition.WorldCup, knockOut: false, random);
                    var (hg, ag) = (report.HomeGoals, report.AwayGoals);
                    var match = new KnockoutMatch(day, home, away, hg, ag);
                    group.Results.Add(match);
                    played.Add(match);
                    game.EnterNationalResult(home, away, hg, ag, NationalMatchKind.WorldCupGroup); // 0x50283, Code 8
                }
            if (date == GroupDates - 1)
            {
                var a = _groups[0].Table(game.Clubs).Select(r => r.Club.Index).ToList();
                var b = _groups[1].Table(game.Clubs).Select(r => r.Club.Index).ToList();
                for (var k = 0; k < 4; k++)
                {
                    _slots[2 * k] = a[k];
                    _slots[2 * k + 1] = b[3 - k];
                }
            }
        }
        else
        {
            var stage = date - GroupDates;
            var results = KnockoutPairings(stage)
                .Select(p =>
                {
                    game.PrepareNationalTactics(p.Home, p.Away); // Spielvorbereitung 0x8365 je Paarung (#192)
                    return CupMatch.Play(game, day, game.RatedNation(p.Home), game.RatedNation(p.Away), MatchCompetition.WorldCup, random);
                }).ToList();
            played = results.Select(r => r.Match).ToList();
            while (_knockoutMarks.Count < _knockout.Count)
                _knockoutMarks.Add(_knockout[_knockoutMarks.Count].Select(m => m.PenaltyWinner is null ? 0 : 1).ToList());
            _knockout.Add(played);
            _knockoutMarks.Add(results.Select(r => r.Marks).ToList());
            foreach (var m in played)
                game.EnterNationalResult(m.Home, m.Away, m.HomeGoals, m.AwayGoals, NationalMatchKind.WorldCupKnockout); // Code 10
            for (var k = 0; k < 4; k++)
            {
                _slots[8 * stage + 2 * k] = played[k].Winner;
                _slots[8 * stage + 2 * k + 1] = played[k].Loser;
            }
            if (stage < 2)
                for (var i = 8 * stage; i < 8 * stage + 8; i++)
                    _slots[Advance[i] - 1] = _slots[i];
            else
            {
                Winner = played[0].Winner;
                var a = _groups[0].Table(game.Clubs);
                var b = _groups[1].Table(game.Clubs);
                Ranking = Enumerable.Range(16, 8).Select(i => _slots[i]!.Value)
                    .Concat(new[] { a[4], b[4], a[5], b[5] }.Select(r => r.Club.Index)).ToList();
                steps.Add(new TrophyStep(day, Core.Title.WorldChampion, Winner.Value));
                game.SetNationalBudget(Ranking);
                game.RecordWorldCupRanking(Ranking); // WM-Statistik 0x3ADE1 (Spur nationalteam-2)
            }
        }
        game.CountCaps(played);
        game.SettleCupMatches(played); // gemeinsame Spielabrechnung (Nationalteams haben keinen Manager)
        // Eigene Spiele: Vereine der Manager, im Modus „nur Weltkugel“ das Nationalteam des Bundestrainers.
        var managerTeams = game.ManagerTeams();
        steps.Add(new MatchesStep(day, Competition.WorldCup, played.Any(m => managerTeams.Contains(m.Home) || managerTeams.Contains(m.Away))));
    }
}
