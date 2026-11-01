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
    ///   der Taktik). Nationalteam des Bundestrainers: [APPROX] ohne Kader wie ein Computerverein (Energie, Reihen);
    ///   [ORIGINAL] Strafspieler 0x378C5 aus dem Kader des Bundestrainers (<see cref="NationalPenaltySquad"/>, R8W3I-06).</item>
    /// </list>
    /// </summary>
    public static LiveTeam LiveTeamOf(GameState game, Club club, Club opponent, bool managed, bool managerMatch)
    {
        var value = TeamValue(club);
        var opponentValue = TeamValue(opponent);
        var loaded = TeamValuesOf(club);
        // Managervereine bilden ihre Reihenwerte im Original aus den Spielern (0x98F8); die Streuung und Form des Laders
        // wirken dort nicht ([APPROX] Teamwert ohne Streuung, Form 50). Die Tagesform 0x17 gilt für alle Vereine.
        var spread = !managed;
        // [ORIGINAL] Der Lader begrenzt nicht (Byte bis v + v/10, also bis 101); begrenzt wird erst in 0x977F
        // (OriginalMatch.LineValues). Untergrenze: Streuung ≥ −v/10, also nie negativ (Review R8-A03).
        var defense = new PartValues(
            value + (spread ? loaded.DefenseCondition : 0),
            value + (spread ? loaded.DefenseTechnique : 0),
            spread ? loaded.DefenseForm : 50);
        var attack = new PartValues(
            value + (spread ? loaded.AttackCondition : 0),
            value + (spread ? loaded.AttackTechnique : 0),
            spread ? loaded.AttackForm : 50);
        var manager = game.ActiveManagers.FirstOrDefault(m => m.ClubIndex == club.Index);
        LiveSquad? squad = null;
        var penaltyValue = 20;
        if (managed && manager is not null && club.Players.Count > 0)
        {
            squad = LiveSquad.Create(club, TrainingRules.LevelValue(manager.Level), club.Tactics);
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
        LiveSquad? penaltySquad = null;
        var fewFit = managed && club.Players.Count > 0 && club.Players.Count(p => p.Available) < MinimumFitForPenalties;
        if (managed && managerMatch && game.NationalCoach is { } coach && club.Index == coach.Nation)
        {
            // R8W3I-06: [ORIGINAL] Das Nationalteam (Vereinsfeld 0x16 = 6) zieht den Strafspieler aus dem Kader des
            // Bundestrainers (0x378C5 mit Manager 5); unter 8 fitten Kaderspielern keine Strafe (0x358FF, 0x120:0x66(5, 1)).
            // [APPROX] Reihenwerte und Energie bleiben wie beim Computerverein (kein Squad); der Managerwert der Strafen
            // bleibt 20 (das Original liest DGROUP 0x974B + 6 · 0x1AF hinter den sechs Managersätzen, nicht geprüft).
            penaltySquad = NationalPenaltySquad(coach);
            fewFit = coach.Squad.Count(m => m.Player.Available) < MinimumFitForPenalties;
        }
        return new LiveTeam
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
            PenaltySquad = penaltySquad,
            PenaltyManagerValue = penaltyValue,
            TacticsStrength = value,
            TacticsOpponentStrength = opponentValue,
            FewFitPlayers = fewFit,
        };
    }

    /// <summary>
    /// Kader des Nationalteams für die Spielerwahl der Strafen (R8W3I-06): Kaderplätze in der Reihenfolge des Kaders des
    /// Bundestrainers, Position aus dessen Aufstellung (<see cref="NationalCoach.Lineup"/>; Block b = Reihe b wie
    /// <see cref="LiveSquad.PositionOf"/>), eingesetzt = verfügbar. [APPROX] Reihenfolge der Kaderplätze im Original
    /// (DGROUP 0x5DC3 + 5 · 0x618) nicht gelesen.
    /// </summary>
    public static LiveSquad NationalPenaltySquad(NationalCoach coach)
    {
        var lineup = coach.Lineup();
        var positions = new Dictionary<Player, int>();
        if (lineup.Goalkeeper is { } g)
            positions[g] = 1;
        for (var i = 0; i < lineup.Defenders.Count; i++)
            if (lineup.Defenders[i] is { } d)
                positions[d] = i / Lineup.DefendersPerBlock + 1;
        for (var i = 0; i < lineup.Forwards.Count; i++)
            if (lineup.Forwards[i] is { } f)
                positions[f] = 0x80 | (i / Lineup.ForwardsPerBlock + 1);
        var squad = new LiveSquad();
        foreach (var member in coach.Squad)
        {
            var p = member.Player;
            squad.Players.Add(new LivePlayer
            {
                Player = p,
                Position = positions.GetValueOrDefault(p),
                Dressed = p.Available,
                Energy = PreparedPlayerEnergy(p),
            });
        }
        return squad;
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
    ///   gelesen); die Strafe bekommt der Spieler aus 0x378C5 (Managerverein und Nationalteam des Bundestrainers).
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
            CreditLineup(national.Players.ToList(), national.Goalkeeper, goals, against, side, report, random);
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
        CreditLineup(club.Lineup.Players.ToList(), club.Lineup.Goalkeeper, goals, against, side, report, random);
    }

    /// <summary>Zähler einer gemanagten Mannschaft aus ihrer Aufstellung (Managerverein oder Nationalteam des Bundestrainers).</summary>
    private static void CreditLineup(List<Player> lineup, Player? keeper, int goals, int against, int side, MatchReport report,
        Random random)
    {
        foreach (var p in lineup)
            p.Games++;
        var skaters = lineup.Where(p => p.Role != PlayerRole.Goalkeeper).ToList();
        MatchStatistics.CreditSkaters(skaters, goals, random, countGames: false);
        if (keeper is { } goalie)
        {
            goalie.GoalsAgainst += against;
            goalie.GoalieMinutes += report.Overtime ? 70 : 60;
        }
        foreach (var e in report.Events)
        {
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
