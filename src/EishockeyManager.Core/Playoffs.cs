namespace EishockeyManager.Core;

/// <summary>Spiel außerhalb der Ligatabelle (Play-Off, Pokal …). Bei Gleichstand entscheidet ein Penaltyschießen.</summary>
public sealed record KnockoutMatch(int Day, int Home, int Away, int HomeGoals, int AwayGoals, int? PenaltyWinner = null)
{
    public int Winner => HomeGoals > AwayGoals ? Home : HomeGoals < AwayGoals ? Away : PenaltyWinner ?? Home;
    public int Loser => Winner == Home ? Away : Home;
    public bool Involves(int club) => Home == club || Away == club;

    /// <summary>
    /// Spiel mit Sieger. [APPROX] Unentschieden nach regulärer Spielzeit → Penaltyschießen („n.P.“ im Original),
    /// Sieger zufällig mit leichtem Vorteil für den stärkeren Verein.
    /// </summary>
    public static KnockoutMatch Play(int day, Club home, Club away, Random random)
    {
        var (hg, ag) = MatchEngine.Play(home, away, random);
        int? penalty = null;
        if (hg == ag)
        {
            var pHome = Math.Clamp(0.5 + (home.Strength - away.Strength) / 200.0, 0.3, 0.7);
            penalty = random.NextDouble() < pHome ? home.Index : away.Index;
        }
        return new KnockoutMatch(day, home.Index, away.Index, hg, ag, penalty);
    }

    /// <summary>
    /// Spiel mit Sieger nach der Berechnung des Originals (<see cref="MatchEngine.PlayWithEvents"/>, mit Verlängerung).
    /// Bleibt es danach unentschieden, entscheidet das Penaltyschießen wie in <see cref="Play(int, Club, Club, Random)"/>
    /// ([APPROX]); der schnelle Weg der Pokale bringt seinen Sieger mit ([ORIGINAL] 0x376F0).
    /// </summary>
    public static KnockoutMatch Play(GameState game, int day, Club home, Club away, MatchCompetition competition, Random random)
    {
        var report = MatchEngine.PlayWithEvents(game, home, away, competition, knockOut: true, random);
        int? penalty = null;
        if (report.HomeGoals == report.AwayGoals)
        {
            if (report.ShootoutHomeWins is { } homeWins)
            {
                penalty = homeWins ? home.Index : away.Index;
            }
            else
            {
                var pHome = Math.Clamp(0.5 + (home.Strength - away.Strength) / 200.0, 0.3, 0.7);
                penalty = random.NextDouble() < pHome ? home.Index : away.Index;
            }
        }
        return new KnockoutMatch(day, home.Index, away.Index, report.HomeGoals, report.AwayGoals, penalty);
    }
}

/// <summary>Play-Off-Serie zweier Vereine: wer zuerst <see cref="WinsNeeded"/> Spiele gewinnt.</summary>
public sealed class PlayoffSeries(int higherSeed, int lowerSeed)
{
    /// <summary>„Best of five“: drei Siege entscheiden die Serie. [ORIGINAL] Datei 0x315E9 (Siege DGROUP 0xCCA4, nach drei Siegen Ergebnis 100 = nicht ausgetragen; spielberechnung.md §14). Die 15 Termine im Kalender sind drei Runden à höchstens fünf Spiele.</summary>
    public const int WinsNeeded = 3;

    public int HigherSeed { get; } = higherSeed;
    public int LowerSeed { get; } = lowerSeed;
    public List<KnockoutMatch> Games { get; } = [];

    public int Wins(int club) => Games.Count(g => g.Winner == club);
    public bool IsDecided => Wins(HigherSeed) >= WinsNeeded || Wins(LowerSeed) >= WinsNeeded;
    public int? Winner => Wins(HigherSeed) >= WinsNeeded ? HigherSeed : Wins(LowerSeed) >= WinsNeeded ? LowerSeed : null;
    public bool Involves(int club) => HigherSeed == club || LowerSeed == club;

    /// <summary>Heimrecht: Heim und Gast tauschen nach jedem Spiel. [ORIGINAL] Datei 0x31505 (spielberechnung.md §14). [APPROX] Spiel 1 beim besser platzierten Verein (erste Stelle der Paarung) ist nicht gesondert belegt.</summary>
    public (int Home, int Away) NextPairing => Games.Count % 2 == 0 ? (HigherSeed, LowerSeed) : (LowerSeed, HigherSeed);
}

/// <summary>
/// Play-Offs der Bundesliga um die Deutsche Meisterschaft.
/// [ORIGINAL] Datei 0x31934 (Setzung aus 0x5CCB), 0x31BB0 (drei Runden 8 → 4 → 2), 0x31CBF (Meister); spielberechnung.md §14:
/// die ersten acht der Hauptrunde, Viertelfinale 1–8, 2–7, 3–6, 4–5, Halbfinale, Finale.
/// [ORIGINAL] Halbfinale: Sieger 1–8 gegen Sieger 3–6, Sieger 2–7 gegen Sieger 4–5 – Tabelle DGROUP 0x144 beim Rundenwechsel
/// (Datei 0x31BED–0x31C4D) führt die Sieger der Plätze 0/4 auf die Plätze 8/9 und die der Plätze 2/6 auf 10/11; im Endspiel die
/// Sieger der Plätze 8/10 auf 12/13. Heimrecht im ersten Spiel der nächsten Runde hat der Sieger der Paarung mit dem kleineren
/// Platz (FILE44: Halbfinale SB Rosenheim – ESV Kaufbeuren, Berichtigung des Imports Runde 14, R14HI-01). Je Runde fünf Termine
/// (Texte im Original: „VIERTELFINALE“, „HALBFINALE“, „Play-Off Spiele“, „Deutsche Meisterschaft“).
/// </summary>
public sealed partial class Playoffs
{
    public const int Teams = 8;
    public const int DatesPerRound = 5;
    public static readonly string[] RoundNames = ["Viertelfinale", "Halbfinale", "Endspiel"];

    private readonly List<List<PlayoffSeries>> _rounds = [];

    /// <param name="seeds">Die acht Teilnehmer nach Platzierung der Hauptrunde (Platz 1 zuerst).</param>
    public Playoffs(IReadOnlyList<int> seeds)
    {
        if (seeds.Count != Teams)
            throw new ArgumentException("Die Play-Offs brauchen acht Vereine.", nameof(seeds));
        Seeds = seeds;
        // Reihenfolge der Serien = Reihenfolge der Sieger in der nächsten Runde (Tabelle DGROUP 0x144): 1–8, 3–6, 2–7, 4–5.
        _rounds.Add([
            new(seeds[0], seeds[7]), new(seeds[2], seeds[5]),
            new(seeds[1], seeds[6]), new(seeds[3], seeds[4]),
        ]);
    }

    public IReadOnlyList<int> Seeds { get; }
    public IReadOnlyList<IReadOnlyList<PlayoffSeries>> Rounds => _rounds;
    public int? Champion => _rounds.Count == 3 ? _rounds[2][0].Winner : null;

    /// <summary>Legt die nächste Runde an, sobald alle Serien der vorigen entschieden sind.</summary>
    private void Prepare(int round)
    {
        while (_rounds.Count <= round && _rounds[^1].All(s => s.IsDecided) && _rounds.Count < 3)
        {
            var winners = _rounds[^1].Select(s => s.Winner!.Value).ToList();
            var next = new List<PlayoffSeries>();
            for (var i = 0; i < winners.Count; i += 2)
                next.Add(new(winners[i], winners[i + 1])); // [ORIGINAL] Sieger der Paarung mit dem kleineren Platz zuerst (Heimrecht Spiel 1)
            _rounds.Add(next);
        }
    }

    /// <summary>Serie, in der ein Verein am Play-Off-Termin <paramref name="dateIndex"/> (0–14) spielt, oder null.</summary>
    public PlayoffSeries? SeriesOn(int dateIndex, int club)
    {
        var round = dateIndex / DatesPerRound;
        Prepare(round);
        if (round >= _rounds.Count)
            return null;
        return _rounds[round].FirstOrDefault(s => s.Involves(club) && !s.IsDecided);
    }

    /// <summary>Spielt alle offenen Serien der Runde dieses Termins je ein Spiel weiter.</summary>
    public IReadOnlyList<KnockoutMatch> PlayDate(int dateIndex, int day, GameState game, Random random)
    {
        var clubs = game.Clubs;
        var round = dateIndex / DatesPerRound;
        Prepare(round);
        var played = new List<KnockoutMatch>();
        if (round >= _rounds.Count)
            return played;
        foreach (var series in _rounds[round].Where(s => !s.IsDecided))
        {
            var (home, away) = series.NextPairing;
            var match = KnockoutMatch.Play(game, day, clubs[home], clubs[away], MatchCompetition.Playoff, random);
            series.Games.Add(match);
            played.Add(match);
        }
        return played;
    }
}

/// <summary>
/// Aufstiegsrunde zwischen zwei Ligen: die zwei Letzten der oberen und die zwei Ersten der unteren Liga
/// spielen in einer Gruppe je zweimal gegeneinander; die beiden Ersten spielen in der nächsten Saison oben.
/// Belegt im Video für 2. Liga/Oberliga (1994/95: BK Oldenburg und EC Harz Braunlage steigen auf, SC Riessersee
/// und SC Memmingen ab; 1995/96 bleiben beide Zweitligisten). Die Saisonende-Bildschirme zeigen auch zwischen
/// Bundesliga und 2. Liga mal zwei, mal einen Wechsel – daher hier dasselbe Verfahren.
/// </summary>
public sealed partial class RelegationGroup
{
    /// <summary>
    /// [APPROX] Play-Off-Termine (Index 0–14) der sechs Gruppenspieltage. Belegt: Spiele am 6.2., 10.2., 20.2.,
    /// 25.2. und eines zwischen 25.2. und 22.3., das letzte nach dem 22.3. (Infoleiste im Video).
    /// </summary>
    public static readonly int[] DateIndices = [0, 2, 5, 7, 9, 10];

    public RelegationGroup(League upper, IReadOnlyList<int> clubs, Random random)
    {
        Upper = upper;
        Clubs = clubs;
        var order = clubs.ToList();
        for (var i = order.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (order[i], order[j]) = (order[j], order[i]);
        }
        Rounds = Schedule.DoubleRoundRobin(order);
    }

    /// <summary>Obere Liga (Bundesliga oder 2. Liga).</summary>
    public League Upper { get; }
    public League Lower => Upper + 1;

    /// <summary>Teilnehmer: zuerst die beiden Letzten der oberen Liga, dann die beiden Ersten der unteren.</summary>
    public IReadOnlyList<int> Clubs { get; }
    public IReadOnlyList<MatchDay> Rounds { get; private set; }
    public List<MatchResult> Results { get; } = [];

    public int PlayedRounds { get; private set; }
    public bool IsFinished => PlayedRounds >= Rounds.Count;

    public static int RoundOn(int dateIndex) => Array.IndexOf(DateIndices, dateIndex);

    public (int Home, int Away)? MatchOn(int dateIndex, int club)
    {
        var round = RoundOn(dateIndex);
        if (round < 0 || round != PlayedRounds)
            return null;
        foreach (var m in Rounds[round].Matches)
            if (m.Home == club || m.Away == club)
                return m;
        return null;
    }

    public IReadOnlyList<MatchResult> PlayDate(int dateIndex, GameState game, Random random)
    {
        var clubs = game.Clubs;
        var round = RoundOn(dateIndex);
        if (round < 0 || round != PlayedRounds)
            return [];
        var played = Rounds[round].Matches.Select(m =>
        {
            var report = MatchEngine.PlayWithEvents(game, clubs[m.Home], clubs[m.Away], MatchCompetition.Relegation, knockOut: false, random);
            var (hg, ag) = (report.HomeGoals, report.AwayGoals);
            return new MatchResult(Upper, round + 1, m.Home, m.Away, hg, ag);
        }).ToList();
        Results.AddRange(played);
        PlayedRounds++;
        return played;
    }

    /// <summary>
    /// Tabelle der Gruppe. [ORIGINAL] em.exe 0x30852 mit Wettbewerb 1 (Aufruf 0x38691 nach jedem Spieltag, Code
    /// DGROUP 0xD0C2 = 1): Punkte, Tore und Sortierung wie die Pokalgruppen (<see cref="CupGroup.Standings"/>).
    /// Ausgangsreihenfolge = Plätze 0x30427 (Datei 0x30511): die beiden Letzten der oberen Liga, dann die beiden Ersten
    /// der unteren (DGROUP 0x5CCB + 10/11 bzw. + 12/13 je Liga) – wie <see cref="Clubs"/>.
    /// </summary>
    public IReadOnlyList<TableRow> Table(IReadOnlyList<Club> clubs) =>
        CupGroup.Standings(clubs, Clubs, Results.GroupBy(r => r.MatchDay)
            .Select(day => day.Select(r => (r.Home, r.Away, r.HomeGoals, r.AwayGoals))));

    /// <summary>Die beiden Vereine, die in der nächsten Saison in der oberen Liga spielen.</summary>
    public IReadOnlyList<int> Qualified(IReadOnlyList<Club> clubs) => Table(clubs).Take(2).Select(r => r.Club.Index).ToList();
}
