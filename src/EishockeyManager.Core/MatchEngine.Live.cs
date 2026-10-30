using EishockeyManager.Formats;

namespace EishockeyManager.Core;

/// <summary>Wettbewerb eines Spiels für die Spielberechnung (Regeln und Zähler hängen davon ab).</summary>
public enum MatchCompetition
{
    League,
    Playoff,
    Relegation,
    DebCup,
    EuropeanCup,
    WorldCup,
}

/// <summary>
/// Anschluss der Spielberechnung des Originals an die Vereine des Remakes (Spur spielberechnung, Teil 3).
/// Fundstellen: <c>docs/spielberechnung.md</c> §11.
/// </summary>
public static partial class MatchEngine
{
    /// <summary>[ORIGINAL] Energie der Reihen bei der Spielvorbereitung (Datei 0x84D7: Vereinsfeld 0x1E = 0x5B).</summary>
    public const int PreparedEnergy = 0x5B;

    /// <summary>[ORIGINAL] Statistik nach dem Spieltag nur für Vereine mit Index &lt; 0x40 (Datei 0x52CF).</summary>
    public const int FirstForeignClub = 0x40;

    /// <summary>[ORIGINAL] Unter 8 fitten Spielern verhängt das Original gegen Managervereine keine Strafen (0x120:0x66).</summary>
    public const int MinimumFitForPenalties = 8;

    /// <summary>
    /// Spiel mit der Berechnung des Originals, Ereignissen und Spielerzählern.
    /// <list type="number">
    /// <item>[ORIGINAL] Spielwertung 0:2/2:0 ohne Berechnung bei nicht spielfähigem Managerverein (0x36DC5, #165).</item>
    /// <item>[ORIGINAL] Pokale (DEB-Pokal, Europapokal, WM) ohne Managerverein: schneller Weg 0x3731F
    ///   (<see cref="OriginalMinutes.FastCupResult"/>) – das Remake hat keine Anzeige, die den Minutenweg erzwingen würde
    ///   ([APPROX] Schalter DGROUP 0x227–0x229 gelten als aus). Sonst Minute für Minute (<see cref="OriginalMinutes.Play"/>).</item>
    /// <item>Zähler der Spieler (<see cref="CreditPlayers"/>).</item>
    /// </list>
    /// Ein Unentschieden im K.-o.-Spiel nach Verlängerung bleibt im Bericht stehen; das Penaltyschießen entscheidet der
    /// Aufrufer (<see cref="KnockoutMatch.Play(GameState, int, Club, Club, MatchCompetition, Random)"/>), der schnelle Weg
    /// liefert seinen Sieger in <see cref="MatchReport.ShootoutHomeWins"/>.
    /// </summary>
    public static MatchReport PlayWithEvents(GameState game, Club home, Club away, MatchCompetition competition,
        bool knockOut, Random random)
    {
        if (home.ForfeitsMatch || away.ForfeitsMatch)
        {
            var forfeit = home.ForfeitsMatch
                ? new MatchReport { HomeGoals = 0, AwayGoals = 2, Forfeit = true }
                : new MatchReport { HomeGoals = 2, AwayGoals = 0, Forfeit = true };
            CreditPlayers(game, home, away, competition, forfeit, random);
            return forfeit;
        }
        var homeManaged = IsManaged(game, home);
        var awayManaged = IsManaged(game, away);
        var rules = new MatchRules(
            WorldCup: competition == MatchCompetition.WorldCup,
            Playoff: competition is MatchCompetition.Playoff or MatchCompetition.Relegation,
            KnockOut: knockOut);
        var managerMatch = homeManaged || awayManaged;
        var homeTeam = LiveTeamOf(game, home, away, homeManaged, managerMatch);
        var awayTeam = LiveTeamOf(game, away, home, awayManaged, managerMatch);
        MatchReport report;
        if (!managerMatch && competition is MatchCompetition.DebCup or MatchCompetition.EuropeanCup or MatchCompetition.WorldCup)
        {
            var h = OriginalMinutes.Strengths(homeTeam, awayTeam, 0);
            var a = OriginalMinutes.Strengths(awayTeam, homeTeam, 0);
            var (hg, ag, shootout, tie) = OriginalMinutes.FastCupResultWithTie(
                new MatchSide(h.Normal, homeTeam.Box.Skaters, homeTeam.AttackLevel),
                new MatchSide(a.Normal, awayTeam.Box.Skaters, awayTeam.AttackLevel),
                home.Index < FirstForeignClub, away.Index < FirstForeignClub,
                rules.WorldCup, rules.Playoff, knockOut, random);
            report = new MatchReport { HomeGoals = hg, AwayGoals = ag };
            if (shootout)
            {
                // [ORIGINAL] 0x376F0–0x377A3: random(0, 2) je Seite bis ungleich, Heim + 30 als Penaltymarke.
                // [APPROX] Das Remake führt das Penaltyschießen wie bisher getrennt: Ergebnis = Gleichstand, Sieger hier.
                report.ShootoutHomeWins = hg > ag;
                report.HomeGoals = report.AwayGoals = tie;
            }
        }
        else
        {
            report = OriginalMinutes.Play(homeTeam, awayTeam, rules, random);
        }
        CreditPlayers(game, home, away, competition, report, random);
        return report;
    }

    /// <summary>Hat der Verein einen (nicht entlassenen) Manager oder ist er das Team des Bundestrainers?</summary>
    public static bool IsManaged(GameState game, Club club) =>
        game.ActiveManagers.Any(m => m.ClubIndex == club.Index)
        || (club.Index is >= NationalTeams.First and <= NationalTeams.Last && game.NationalCoach?.Nation == club.Index);

    /// <summary>
    /// Mannschaft eines Vereins für die Minutenrechnung.
    /// <list type="bullet">
    /// <item>Teamwerte: Kondition und Technik beider Teile = <see cref="TeamValue"/>. [ORIGINAL] Der Lader 0x2EF1B setzt
    ///   die Vereinsfelder 0x18–0x1B aus den Stärkebytes der Datenbank (± random(0, Wert/10)). [APPROX] ohne Streuung und
    ///   mit Byte 0 auch für den Sturm (Byte 1 weicht nur bei den Europapokal-Gegnern ab und wird im Remake nicht
    ///   geführt); die Abweichung der Aufstellung von der Startaufstellung wirkt wie bisher über
    ///   <see cref="TeamValue"/>.</item>
    /// <item>Form 0x1C/0x1D: [ORIGINAL] random(45, 55), Europapokal-Gegner (Index &gt; 64) 5 weniger; [APPROX] Mitte 50
    ///   bzw. 45. Tagesform 0x17: [ORIGINAL] random(47, 53), [APPROX] 50.</item>
    /// <item>Einsatz 0x26 und Angriff 0x2C: Automatik bzw. eigene Taktik beim Stand 0:0 (Spielvorbereitung, Datei 0x8365;
    ///   <see cref="Tactics.EffectiveEffort"/>). In Managerpaarungen führt der Minutenablauf sie nach dem Spielstand nach.</item>
    /// <item>Energie 0x1E = <see cref="PreparedEnergy"/>. Reihe 0 bleibt auf dem Eis: [ORIGINAL] für Paarungen ohne
    ///   Manager (Reihenwechsel 0x8EF2 nur in Managerpaarungen); [APPROX] auch in Managerpaarungen (0x8EF2 nicht gelesen).</item>
    /// <item>Managervereine: [APPROX] Teamwerte statt der Werte der eingesetzten Spieler (0x98F8–0x9C63, nur teilweise
    ///   gelesen: Mittel über drei Spieler, Energie wirkt mit Energie/2 + 50 % auf die Technik).</item>
    /// </list>
    /// </summary>
    public static LiveTeam LiveTeamOf(GameState game, Club club, Club opponent, bool managed, bool managerMatch)
    {
        var value = TeamValue(club);
        var opponentValue = TeamValue(opponent);
        var form = club.Index > 64 ? 45 : 50;
        var part = new PartValues(value, value, form);
        return new LiveTeam
        {
            Defense = part,
            Attack = part,
            ClubForm = 50,
            Effort = club.Tactics.EffectiveEffort(0, 0, value, opponentValue),
            AttackLevel = club.Tactics.EffectiveAttack(0, 0, value, opponentValue),
            Energy = PreparedEnergy,
            Tactics = managerMatch ? club.Tactics : null,
            TacticsParity = managed ? 0 : 1,
            TacticsStrength = value,
            TacticsOpponentStrength = opponentValue,
            FewFitPlayers = managed && club.Players.Count > 0
                && club.Players.Count(p => p.Available) < MinimumFitForPenalties,
        };
    }

    /// <summary>
    /// Teamwert eines Vereins (Maßstab der Vereinsfelder 0x18–0x1B, also der Datenbankstärke): Stärke aus der Datenbank,
    /// verschoben um die Veränderung der Aufstellung gegenüber dem Spielbeginn wie <see cref="TeamStrength.MatchStrength"/>,
    /// aber ohne dessen Einsatzzuschlag (der Einsatz wirkt in der Rechnung des Originals selbst). Vereine ohne Kader
    /// (Nationalteams aus <c>RatedNation</c>, Europapokal-Gegner) spielen mit ihrer Stärke. Begrenzt auf 0 … 100.
    /// [APPROX] Abbildung der Remake-Kader auf die Teamwerte (das Original führt für Computervereine keine Spieler).
    /// </summary>
    public static int TeamValue(Club club)
    {
        if (club.Players.Count == 0)
            return Math.Clamp(club.Strength, 0, 100);
        var delta = (TeamStrength.Rating(club.Lineup) - club.ReferenceRating) * TeamStrength.StrengthPerRatingPoint;
        return Math.Clamp((int)Math.Round(club.Strength + delta), 0, 100);
    }

    /// <summary>
    /// Zähler der Spieler nach einem Spiel.
    /// <list type="bullet">
    /// <item>[ORIGINAL] Nur Vereine mit Index &lt; 0x40 (keine Europapokal-Gegner; Nationalteams spielen ohne Kader).</item>
    /// <item>Vereine ohne Manager: [ORIGINAL] 0x5028 aus dem Endstand (Tore, Vorlagen, Spiele), in der Liga Torhüter
    ///   0x4DB6 (<see cref="MatchStatistics"/>); keine Strafen je Spieler. Gilt auch für eine Spielwertung 0:2 (das
    ///   Original wertet das gespeicherte Ergebnis).</item>
    /// <item>Managervereine: Spiele + 1 für die Aufstellung; Torhüter der Aufstellung bekommt die Gegentore
    ///   ([ORIGINAL] 0xA3F7: Torhüter auf dem Eis) und die Spielminuten 60/70 ([ORIGINAL] 0x874D zählt je Minute).
    ///   [APPROX] Torschützen und Vorlagen nach 0x5028 unter den Feldspielern der Aufstellung (Auswahl 0x386D1 nicht
    ///   gelesen); jede Strafe bekommt ein zufälliger Feldspieler der Aufstellung (Spielerwahl 0x378C5 nicht gelesen).
    ///   Bei einer Spielwertung zählt nichts.</item>
    /// </list>
    /// </summary>
    public static void CreditPlayers(GameState game, Club home, Club away, MatchCompetition competition, MatchReport report, Random random)
    {
        Credit(game, home, report.HomeGoals, report.AwayGoals, 0, competition, report, random);
        Credit(game, away, report.AwayGoals, report.HomeGoals, 1, competition, report, random);
    }

    private static void Credit(GameState game, Club club, int goals, int against, int side, MatchCompetition competition,
        MatchReport report, Random random)
    {
        if (club.Index >= FirstForeignClub || club.Players.Count == 0)
            return;
        if (!IsManaged(game, club))
        {
            MatchStatistics.CreditSkaters(club.Players, goals, random);
            if (competition == MatchCompetition.League)
                MatchStatistics.CreditGoalies(GoaliesOf(club), against, random);
            return;
        }
        if (report.Forfeit)
            return;
        var lineup = club.Lineup.Players.ToList();
        foreach (var p in lineup)
            p.Games++;
        var skaters = lineup.Where(p => p.Role != PlayerRole.Goalkeeper).ToList();
        MatchStatistics.CreditSkaters(skaters, goals, random, countGames: false);
        if (club.Lineup.Goalkeeper is { } goalie)
        {
            goalie.GoalsAgainst += against;
            goalie.GoalieMinutes += report.Overtime ? 70 : 60;
        }
        if (skaters.Count == 0)
            return;
        foreach (var e in report.Events)
        {
            if (e.Kind != MatchEventKind.Penalty || e.Side != side)
                continue;
            var p = skaters[OriginalMatch.Rnd(random, 0, skaters.Count - 1)];
            if (e.PenaltyMinutes == 5)
                p.Penalties5++;
            else
                p.Penalties2++;
        }
    }

    /// <summary>Torhüter eines Vereins: der aufgestellte zuerst, dann die übrigen in Kaderreihenfolge.</summary>
    private static List<Player> GoaliesOf(Club club)
    {
        var list = new List<Player>();
        if (club.Lineup.Goalkeeper is { } g)
            list.Add(g);
        list.AddRange(club.Players.Where(p => p.Role == PlayerRole.Goalkeeper && p != club.Lineup.Goalkeeper));
        return list;
    }
}

public sealed partial class GameState
{
    /// <summary>
    /// Neue Saison: Spielerzähler der Spielberechnung (Spiele, Tore, Vorlagen, Strafen, Gegentore, Torhüterminuten)
    /// auf 0. [APPROX] Das Zurücksetzen im Original ist nicht gesucht; die Zähler gelten im Remake je Saison.
    /// Länderspiele (<see cref="Player.Caps"/>) bleiben.
    /// </summary>
    internal void ResetMatchCounters()
    {
        foreach (var p in Clubs.SelectMany(c => c.Players).Concat(TransferMarket.FreeAgents))
            p.Games = p.Goals = p.Assists = p.Penalties2 = p.Penalties5 = p.GoalsAgainst = p.GoalieMinutes = 0;
    }
}
