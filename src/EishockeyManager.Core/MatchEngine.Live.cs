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
    /// <summary>
    /// [ORIGINAL] Energie der Reihen bei der Spielvorbereitung: Datei 0x85B4 setzt Vereinsfeld 0x1E = 0x5B, die Schleife
    /// 0x860B–0x8637 zählt danach einmal herunter (solange der alte Wert über 0x5B liegt) – also 0x5A. Auf die Technik
    /// wirkt sie mit Energie / 4 + 75 % (für 90 und 91 gleich).
    /// </summary>
    public const int PreparedEnergy = 0x5A;

    /// <summary>[ORIGINAL] Statistik nach dem Spieltag nur für Vereine mit Index &lt; 0x40 (Datei 0x52CF).</summary>
    public const int FirstForeignClub = 0x40;

    /// <summary>[ORIGINAL] Unter 8 fitten Spielern verhängt das Original gegen Managervereine keine Strafen (0x120:0x66).</summary>
    public const int MinimumFitForPenalties = 8;

    /// <summary>
    /// Spiel mit der Berechnung des Originals, Ereignissen und Spielerzählern.
    /// <list type="number">
    /// <item>[ORIGINAL] Spielwertung 0:2/2:0 ohne Berechnung bei nicht spielfähigem Managerverein (0x36DC5, #165).</item>
    /// <item>[ORIGINAL] Pokale (DEB-Pokal, Europapokal, WM) ohne Managerverein: schneller Weg 0x3731F
    ///   (<see cref="OriginalMinutes.FastCupResult"/>), wenn der Anzeigeschalter des Wettbewerbs aus ist (DGROUP 0x227–0x229,
    ///   <see cref="GameOptions.CupMatchUsesMinuteLoop"/>). Sonst Minute für Minute (<see cref="OriginalMinutes.Play"/>).</item>
    /// <item>Zähler der Spieler (<see cref="CreditPlayers"/>).</item>
    /// </list>
    /// Ein Unentschieden im K.-o.-Spiel nach Verlängerung bleibt im Bericht stehen; den Sieger des Penaltyschießens liefern
    /// beide Wege in <see cref="MatchReport.ShootoutHomeWins"/> (schneller Weg 0x376F0, Minutenweg 0x3901B,
    /// <see cref="OriginalMinutes.Shootout"/>); <see cref="KnockoutMatch.Play(GameState, int, Club, Club, MatchCompetition, Random)"/>
    /// übernimmt ihn.
    /// </summary>
    public static MatchReport PlayWithEvents(GameState game, Club home, Club away, MatchCompetition competition,
        bool knockOut, Random random)
    {
        EnsureTeamValues(game);
        if (home.ForfeitsMatch || away.ForfeitsMatch)
        {
            var forfeit = home.ForfeitsMatch
                ? new MatchReport { HomeGoals = 0, AwayGoals = 2, Forfeit = true }
                : new MatchReport { HomeGoals = 2, AwayGoals = 0, Forfeit = true };
            CreditPlayers(game, home, away, competition, forfeit, random);
            AfterMatch(home, away, competition, forfeit, random);
            return forfeit;
        }
        var homeManaged = IsManaged(game, home);
        var awayManaged = IsManaged(game, away);
        var managerMatch = homeManaged || awayManaged;
        var rules = new MatchRules(
            WorldCup: competition == MatchCompetition.WorldCup,
            Playoff: competition is MatchCompetition.Playoff or MatchCompetition.Relegation,
            KnockOut: knockOut,
            ManagerMatch: managerMatch);
        var homeTeam = LiveTeamOf(game, home, away, homeManaged, managerMatch);
        var awayTeam = LiveTeamOf(game, away, home, awayManaged, managerMatch);
        MatchReport report;
        // [ORIGINAL] Datei 0x3702E–0x37085: Pokalspiel mit eingeschaltetem Anzeigeschalter (DGROUP 0x227 DEB-Pokal, Code 6;
        // 0x228 Europapokal, Code 3/5; 0x229 WM-Gruppe, Code 8; GameOptions.CupMatchUsesMinuteLoop) läuft durch die
        // Minutenschleife 0x37088, sonst der schnelle Weg 0x3731F. Die WM-Endrunde (Code 10, 0x31017) prüft keinen Schalter
        // und nimmt immer den schnellen Weg (R9S-02, #350; Erreichen von 0x36B5D aus 0x31017 aus dem Aufrufmuster geschlossen,
        // Überlagerungssprung 0xF0:0x70 nicht aufgelöst – R9S-07, #358). Anschluss optionen-wirkung, Integration Runde 9.
        // Play-offs/Aufstiegsrunde (Code 1) ebenso, Schalter = Feld „Ergebnisse“ der Meldungsmatrix (0x37058–0x37085, #351).
        // [APPROX] Das Original entscheidet je Spieltag (Merker 0x36FEA „ein Managerverein spielt“: dann laufen alle Paarungen
        // des Spieltags durch die Minutenschleife), das Remake je Paarung – Entscheidung Nutzer 01.11.2026: so lassen, das
        // Verhalten des Originals als Einstellung nach v1.0 (#349). Das Setzen des WM-Schalters im Bundestrainer-Abschnitt
        // (0x5541F/0x5575A, Sicherung 0x22C) ist nicht nachgebaut.
        var minuteLoop = competition == MatchCompetition.WorldCup && knockOut
            ? false
            : game.Options.CupMatchUsesMinuteLoop(competition);
        if (!managerMatch && competition is MatchCompetition.DebCup or MatchCompetition.EuropeanCup or MatchCompetition.WorldCup
                or MatchCompetition.Playoff or MatchCompetition.Relegation
            && !minuteLoop)
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
                // Der Bericht hält den Gleichstand (Spielerzähler nur für echte Tore) und die Zusatztore getrennt; den
                // Endstand mit Marke bildet CupMatch.FromReport (Spur wettbewerbe-spiel, Teil 1).
                report.ShootoutHomeWins = hg > ag;
                report.ShootoutGoals = (hg - tie, ag - tie);
                report.HomeGoals = report.AwayGoals = tie;
            }
        }
        else
        {
            report = OriginalMinutes.Play(homeTeam, awayTeam, rules, random);
            if (knockOut && report.HomeGoals == report.AwayGoals)
            {
                // Teil 5: [ORIGINAL] nach der Verlängerung noch unentschieden → Penaltyschießen 0x3901B (aus 0x32AA3).
                // [APPROX] Die Zuordnung der Wettbewerbszeilen DGROUP 0x5C4B ist nicht gelesen; das Remake schießt in jedem
                // K.-o.-Spiel des Minutenwegs.
                var goals = OriginalMinutes.Shootout(random);
                report.ShootoutGoals = goals;
                report.ShootoutHomeWins = goals.Home > goals.Away;
            }
        }
        CreditPlayers(game, home, away, competition, report, random);
        AfterMatch(home, away, competition, report, random);
        return report;
    }

    /// <summary>[ORIGINAL] Tabellenrechnung 0x50283 → 0x4F527: Tagesform beider Vereine nach einem Ligaspiel.</summary>
    private static void AfterMatch(Club home, Club away, MatchCompetition competition, MatchReport report, Random random)
    {
        if (competition != MatchCompetition.League)
            return;
        UpdateDayForm(home, report.HomeGoals - report.AwayGoals, random);
        UpdateDayForm(away, report.AwayGoals - report.HomeGoals, random);
    }

    /// <summary>Hat der Verein einen (nicht entlassenen) Manager oder ist er das Team des Bundestrainers?</summary>
    public static bool IsManaged(GameState game, Club club) =>
        game.ActiveManagers.Any(m => m.ClubIndex == club.Index)
        || (club.Index is >= NationalTeams.First and <= NationalTeams.Last && game.NationalCoach?.Nation == club.Index);

    /// <summary>
    /// Mannschaft eines Vereins für die Minutenrechnung.
    /// <list type="bullet">
    /// <item>Teamwerte: Kondition und Technik je Teil = <see cref="TeamValue"/> + Streuung des Laders. [ORIGINAL] Der
    ///   Lader 0x2EF1B setzt die Vereinsfelder 0x18–0x1B einmal je Spiel aus den Stärkebytes der Datenbank
    ///   (v − random(0, v/10) + random(0, v/10), <see cref="LoadTeamValues"/>). [APPROX] Byte 0 auch für den Sturm (Byte 1
    ///   weicht nur bei den Europapokal-Gegnern ab und wird im Remake nicht geführt); die Abweichung der Aufstellung von
    ///   der Startaufstellung wirkt wie bisher über <see cref="TeamValue"/>.</item>
    /// <item>Form 0x1C/0x1D: [ORIGINAL] random(45, 55), Europapokal-Gegner (Index &gt; 64) 5 weniger, aus dem Lader.
    ///   Tagesform 0x17: [ORIGINAL] random(47, 53) aus dem Lader, nach Ligaspielen nachgeführt (<see cref="UpdateDayForm"/>).</item>
    /// <item>Einsatz 0x26 und Angriff 0x2C: Automatik bzw. eigene Taktik beim Stand 0:0 (Spielvorbereitung, Datei 0x8365;
    ///   <see cref="Tactics.EffectiveEffort"/>). In Managerpaarungen führt der Minutenablauf sie nach dem Spielstand nach.</item>
    /// <item>Energie 0x1E = <see cref="PreparedEnergy"/>. Reihe 0 bleibt auf dem Eis: [ORIGINAL] für Paarungen ohne
    ///   Manager; in Managerpaarungen Energie und Reihenwechsel je Minute (0x87F7, 0x8EF2, <c>docs/spielberechnung.md</c> §13).</item>
    /// <item>Managervereine in der Managerpaarung: [ORIGINAL] Reihenwerte aus den eingesetzten Spielern
    ///   (<see cref="LiveSquad"/>, <see cref="ManagedLineValues"/>, 0x98F8); Strafen mit dem Managerwert des nächsten
    ///   Managersatzes (0x35890, <see cref="ManagedTeamValues"/>). Defense/Attack bleiben die Teamwerte (für die Automatik
    ///   der Taktik).</item>
    /// <item>Nationalteams (Vereinsplätze 52–63 ohne Kader, auch das Land des Bundestrainers): [ORIGINAL] eigene Teamwerte,
    ///   beim Bundestrainer Kader, Taktik und Strafspieler – <see cref="NationalLiveTeam"/> (Spur wettbewerbe-spiel, Teil 3).</item>
    /// </list>
    /// </summary>
    public static LiveTeam LiveTeamOf(GameState game, Club club, Club opponent, bool managed, bool managerMatch)
    {
        if (IsNation(club))
            return NationalLiveTeam(game, club.Index, game.NationalMatchStrength(club.Index),
                IsNation(opponent) ? game.NationalMatchStrength(opponent.Index) : TeamValue(opponent), managerMatch);
        var value = TeamValue(club);
        var opponentValue = TeamValue(opponent);
        var loaded = TeamValuesOf(club);
        // Managervereine bilden ihre Reihenwerte im Original aus den Spielern (0x98F8); die Streuung und Form des Laders
        // wirken dort nicht ([APPROX] Teamwert ohne Streuung, Form 50). Die Tagesform 0x17 gilt für alle Vereine.
        var spread = !managed;
        // [ORIGINAL] Der Lader begrenzt nicht (Byte bis v + v/10, also bis 101); begrenzt wird erst in 0x977F
        // (OriginalMatch.LineValues). Untergrenze: Streuung ≥ −v/10, also nie negativ (Review R8-A03).
        var (defense, attack) = spread ? ClubFieldValues(club) : (new PartValues(value, value, 50), new PartValues(value, value, 50));
        var manager = game.ActiveManagers.FirstOrDefault(m => m.ClubIndex == club.Index);
        LiveSquad? squad = null;
        var penaltyValue = 20;
        var ownValue = 20;
        int? injuryManager = null;
        if (managed && manager is not null && club.Players.Count > 0)
        {
            squad = LiveSquad.Create(club, TrainingRules.LevelValue(manager.Level), club.Tactics);
            // Teil 3: eigener Managerwert und Managersatz für die Verletzungen 0x38B8F ([APPROX] aus demselben Stand wie
            // der Wert der Strafen, R8W3I-05).
            ownValue = ManagedTeamValues(squad, nineOrMore: !game.IsShortHanded(club)).ManagerValue;
            injuryManager = game.Managers.ToList().IndexOf(manager);
            // [ORIGINAL] Strafen lesen den Managerwert über Vereinsfeld 0x16 = Manager + 1 (Datei 0x35890), also den Satz
            // des nächsten Managers (übernommen). [APPROX] Ohne nächsten Manager 0 (nicht belegter Satz, nicht geprüft).
            // R8W3I-05: [ORIGINAL] gelesen wird der gespeicherte Wert DGROUP 0x974B, den nur 0x952C schreibt – mit Schalter 1
            // im Tageslauf 0x54C9F für jeden Manager vor den Spielen des Tages (0x54F9C–0x54FBF), bei weniger als neun
            // aufgestellten und eingesetzten Spielern 100 (0x9698–0x96A8). [APPROX] Das Remake rechnet ihn hier aus
            // demselben Stand: zwischen Tageslauf und Spiel ändert sich die Aufstellung nicht (wie bei
            // GameState.MarkShortHandedClubs); weniger als neun = GameState.IsShortHanded.
            var index = game.Managers.ToList().IndexOf(manager);
            var next = index + 1 < game.Managers.Count ? game.Managers[index + 1] : null;
            penaltyValue = next is not null && game.Clubs.FirstOrDefault(c => c.Index == next.ClubIndex) is { Players.Count: > 0 } nextClub
                ? ManagedTeamValues(LiveSquad.Create(nextClub, 0, null), nineOrMore: !game.IsShortHanded(nextClub)).ManagerValue
                : 0;
        }
        var fewFit = managed && club.Players.Count > 0 && club.Players.Count(p => p.Available) < MinimumFitForPenalties;
        var team = new LiveTeam
        {
            Defense = defense,
            Attack = attack,
            ClubForm = loaded.DayForm,
            Effort = club.Tactics.EffectiveEffort(0, 0, value, opponentValue),
            AttackLevel = club.Tactics.EffectiveAttack(0, 0, value, opponentValue),
            Energy = PreparedEnergy,
            Tactics = managerMatch ? club.Tactics : null,
            Managed = managed,
            LineChange = managed ? club.Tactics.LineChange : null,
            Squad = managerMatch ? squad : null,
            PenaltyManagerValue = penaltyValue,
            TacticsStrength = value,
            TacticsOpponentStrength = opponentValue,
            FewFitPlayers = fewFit,
            InjuryManager = managerMatch && squad is not null ? injuryManager : null,
            ManagerValue = ownValue,
            // Teil 4: [ORIGINAL] Zähler des Managers (Vereinsfeld 0x16 = Manager + 1 → DGROUP 0x97D2 + 0x1AF · Manager).
            PowerPlayCounts = managed ? manager?.PowerPlayCounts : null,
        };
        // Teil 8 (A089/A070): [ORIGINAL] 0x35691 zählt beim Freiwerden neu (0x38A36); nur Managervereine (siehe Methode).
        team.Box.Released = () => RecountSkaters(team);
        return team;
    }

    /// <summary>Vereinsplatz eines Nationalteams ohne eigenen Kader (Spielpartner aus <see cref="GameState.RatedNation"/>).</summary>
    private static bool IsNation(Club club) =>
        club.Index is >= NationalTeams.First and <= NationalTeams.Last && club.Players.Count == 0;

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
    ///   [ORIGINAL] Torschütze und Vorlage aus 0x541D9/0x386D1 im Spiel (<see cref="OriginalMinutes.ScoringPlayers"/>,
    ///   Teil 3); die Strafe bekommt der Spieler aus 0x378C5 (Managerverein und Nationalteam des Bundestrainers).
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
        // Review-Befund R7-03 (#201): Das Nationalteam des Bundestrainers (RatedNation ohne Kader) zählt über die
        // Aufstellung des Bundestrainers wie ein Managerverein ([ORIGINAL] Ereignisse im Spiel 0x541D9/0x35818; die
        // Statistik nach dem Spieltag 0x52CF überspringt Index ≥ 0x40 und Managervereine).
        if (game.NationalCoach is { } coach && club.Index == coach.Nation)
        {
            if (report.Forfeit)
                return;
            var national = coach.Lineup();
            // R8W3I-06: Die Strafe zählt beim Spieler aus 0x378C5 (wie beim Managerverein), kein eigener Zufallszug mehr.
            CreditLineup(national.Players.ToList(), national.Goalkeeper, against, side, report);
            return;
        }
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
        CreditLineup(club.Lineup.Players.ToList(), club.Lineup.Goalkeeper, against, side, report);
        if (report.Events.Any(e => e.Kind == MatchEventKind.Injury && e.Side == side))
            PlayerInjuries.RefillLineup(club);
    }

    /// <summary>Zähler einer gemanagten Mannschaft aus ihrer Aufstellung (Managerverein oder Nationalteam des Bundestrainers).</summary>
    private static void CreditLineup(List<Player> lineup, Player? keeper, int against, int side, MatchReport report)
    {
        foreach (var p in lineup)
            p.Games++;
        if (keeper is { } goalie)
        {
            goalie.GoalsAgainst += against;
            goalie.GoalieMinutes += report.Overtime ? 70 : 60;
        }
        foreach (var e in report.Events)
        {
            // [ORIGINAL] 0x54248–0x54337 (Teil 3): Vorlage Spielerfeld 0x1F + 1, Tor Kaderplatz +0x22 + 1 (Liga und Pokal).
            // [APPROX] Spielerfeld 0x1B (nur Wettbewerb 0, DGROUP 0xD0BF) und die Grenze 255 führt das Remake nicht getrennt.
            if (e.Kind == MatchEventKind.Goal && e.Side == side && e.Player is { } scorer)
            {
                scorer.Goals++;
                if (e.Assist is { } assist)
                    assist.Assists++;
                continue;
            }
            // [ORIGINAL] 0x35B7D–0x35B8E: Kaderplatz +0 (5 Minuten) bzw. +1 (2 Minuten) des Spielers aus 0x378C5;
            // beim Torhüter zählt keiner (ohne Spieler gibt es seit R8W3I-04 kein Ereignis).
            if (e.Kind != MatchEventKind.Penalty || e.Side != side || e.Player is not { } penalized)
                continue;
            if (e.PenaltyMinutes == 5)
                penalized.Penalties5++;
            else
                penalized.Penalties2++;
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
