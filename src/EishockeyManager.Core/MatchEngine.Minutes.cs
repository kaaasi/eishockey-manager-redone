namespace EishockeyManager.Core;

/// <summary>
/// Strafbank einer Mannschaft im Spiel (Vereinsfeld 0x33) und Feldspieler auf dem Eis (Vereinsfeld 0x32).
/// [ORIGINAL] Datei 0x35540 (zählen/herunterzählen), 0x35691 (Spieler zurück aufs Eis), 0x35941–0x35963 (Strafe eintragen):
/// zwei Plätze mit je 3 Bit Restminuten (Bits 0–2 Platz 0, Bits 4–6 Platz 1) und je einer Marke (Bit 3 bzw. Bit 7) für
/// Strafen, die ein Gegentor nicht beendet (5 Minuten: Code 13 = Marke + 5).
/// </summary>
public sealed class PenaltyBox
{
    /// <summary>Gepackter Zustand wie Vereinsfeld 0x33.</summary>
    public int State { get; set; }

    /// <summary>Feldspieler auf dem Eis (Vereinsfeld 0x32, beim Anpfiff 5).</summary>
    public int Skaters { get; set; } = OriginalMatch.FullStrength;

    /// <summary>Belegte Plätze ohne Herunterzählen (0x35540 mit Schalter 0).</summary>
    public int Count => ((State & 7) != 0 ? 1 : 0) + (((State >> 4) & 7) != 0 ? 1 : 0);

    /// <summary>
    /// [ORIGINAL] 0x35540(Verein, 1): beide Uhren um eine Minute herunterzählen. Eine Uhr, die von 1 auf 0 geht, gibt den
    /// Spieler frei (0x35691 mit Zwang). Ist Platz 0 frei und läuft Platz 1 weiter, rückt Platz 1 nach Platz 0 (mit Marke).
    /// </summary>
    /// <returns>Danach noch belegte Plätze (so zählt das Original: ein in dieser Minute nachgerückter Platz zählt mit).</returns>
    public int Tick()
    {
        var count = 0;
        var slot0Free = true;
        var t0 = State & 7;
        if (t0 != 0)
        {
            slot0Free = false;
            count++;
            State = (State & 0xF8) | (t0 - 1);
            if (t0 == 1)
            {
                Release(0, force: true);
                slot0Free = true;
                count = 0;
            }
        }
        var t1 = (State >> 4) & 7;
        if (t1 == 0)
            return count;
        count++;
        State = (State & 0x8F) | ((t1 - 1) << 4);
        if (t1 == 1)
        {
            Release(1, force: true);
            return count - 1;
        }
        if (slot0Free)
        {
            State = ((State & 0x80) != 0 ? 8 : 0) | (t1 - 1);
            MoveOccupant();
        }
        return count;
    }

    /// <summary>
    /// [ORIGINAL] 0x35691(Verein, Platz, Zwang): Spieler zurück aufs Eis (Feldspieler + 1). Ohne Zwang (Gegentor) bleibt
    /// eine markierte Strafe stehen; ist Platz 0 markiert oder Platz 1 belegt, wird stattdessen Platz 1 frei (so im
    /// Original – auch wenn Platz 0 eine gewöhnliche Strafe ist).
    /// </summary>
    public void Release(int slot, bool force)
    {
        if (slot == 0)
        {
            if ((State & 0xF8) != 0 && !force)
            {
                if ((State & 0xF0) == 0)
                    return;
                if (!ReleaseSlot1(force))
                    return;
                Skaters++;
                return;
            }
            State &= 0xF0;
            Return(0);
            if ((State & 0x80) != 0)
            {
                State = ((State >> 4) & 7) | 8;
                MoveOccupant();
            }
            Skaters++;
            return;
        }
        if (ReleaseSlot1(force))
            Skaters++;
    }

    private bool ReleaseSlot1(bool force)
    {
        if ((State & 0x80) != 0 && !force)
            return false;
        State &= 0x0F;
        Return(1);
        return true;
    }

    /// <summary>
    /// Spieler eines Managervereins auf den Plätzen (DGROUP 0xD095 + 6 · Manager + 3 · Platz: Kaderplatz + 1, +0x0A,
    /// Art). [ORIGINAL] 0x35691 setzt ihn beim Freiwerden wieder ein (+0x0A aus dem Satz).
    /// </summary>
    public LivePlayer?[] Occupants { get; } = new LivePlayer?[2];

    private void Return(int slot)
    {
        if (Occupants[slot] is { } p)
            p.Dressed = true;
        Occupants[slot] = null;
    }

    private void MoveOccupant()
    {
        Occupants[0] = Occupants[1];
        Occupants[1] = null;
    }

    /// <summary>[ORIGINAL] 0x35941–0x35963: Feldspieler − 1, Code auf Platz <paramref name="slot"/> (= belegte Plätze).</summary>
    public void Add(int slot, int code)
    {
        Skaters--;
        State |= code << (4 * slot);
    }
}

/// <summary>Art eines Ereignisses der Minutenrechnung.</summary>
public enum MatchEventKind
{
    Goal,
    Penalty,

    /// <summary>Verletzung im Spiel (0x38B8F, Ereignis 4 der Szene 0x53C57).</summary>
    Injury,
}

/// <summary>
/// Ein Ereignis: Minute (1 … 70), Seite (0 Heim, 1 Gast), bei Strafen die Minuten (2 oder 5). Spieler: bestrafter bzw.
/// verletzter Spieler, beim Tor einer gemanagten Mannschaft der Schütze (0x386D1) mit <paramref name="Assist"/>.
/// </summary>
public readonly record struct MatchEvent(int Minute, int Side, MatchEventKind Kind, int PenaltyMinutes = 0, Player? Player = null,
    Player? Assist = null);

/// <summary>
/// Eine Mannschaft während der Minutenrechnung (Vereinsfelder des Originals, die die Rechnung liest und schreibt).
/// </summary>
public sealed class LiveTeam
{
    /// <summary>Teamwerte Abwehr (Vereinsfelder 0x18, 0x1A, 0x1C) für <see cref="OriginalMatch.LineValues"/>.</summary>
    public PartValues Defense { get; init; }

    /// <summary>Teamwerte Sturm (Vereinsfelder 0x19, 0x1B, 0x1D).</summary>
    public PartValues Attack { get; init; }

    /// <summary>Tagesform (Vereinsfeld 0x17, beim Spielbeginn random(47, 53)).</summary>
    public int ClubForm { get; init; } = 50;

    /// <summary>Einsatz 0–100 (Vereinsfeld 0x26).</summary>
    public int Effort { get; set; } = 60;

    /// <summary>Angriff 0–7 (Vereinsfeld 0x2C, Bits 4–6).</summary>
    public int AttackLevel { get; set; }

    /// <summary>
    /// Taktik, nach der der Minutenablauf Einsatz und Angriff dem Spielstand anpasst, oder null (fest wie bei der
    /// Spielvorbereitung). Nur in Managerpaarungen (siehe <see cref="TacticsParity"/>).
    /// </summary>
    public Tactics? Tactics { get; init; }

    /// <summary>
    /// Früher: Minuten, in denen die Taktik nachgeführt wurde. [ORIGINAL] 0x939A läuft in geraden Minuten der
    /// Managerpaarung für beide Vereine (Datei 0x3803F ff.); 0x80CB in ungeraden setzt nur Linienwechsel-Modus und
    /// Torhüter. Wird nicht mehr ausgewertet.
    /// </summary>
    public int TacticsParity { get; init; }

    /// <summary>Eigene und gegnerische Stärke für die Automatik der Taktik (Datei 0x8365, doppelte Differenz).</summary>
    public int TacticsStrength { get; init; }

    public int TacticsOpponentStrength { get; init; }

    /// <summary>Reihe auf dem Eis 0–2 je Teil (Vereinsfelder 0x2E/0x30). Computervereine wechseln nur in Managerpaarungen.</summary>
    public int DefenseLine { get; set; }

    public int AttackLine { get; set; }

    /// <summary>Energie der Reihen beim Anpfiff (Vereinsfeld 0x1E + 4 · Teil + Reihe) 0–100; Startwert von <see cref="LineEnergy"/>.</summary>
    public int Energy { get; init; } = 100;

    private int[,]? _lineEnergy;

    /// <summary>Energie einer Reihe (Vereinsfeld 0x1E + 4 · Teil + Reihe); ändert sich nur in Managerpaarungen (0x87F7).</summary>
    public int LineEnergy(int part, int line) => _lineEnergy?[part, line] ?? Energy;

    public void SetLineEnergy(int part, int line, int value)
    {
        if (_lineEnergy is null)
        {
            _lineEnergy = new int[2, 4];
            for (var p = 0; p < 2; p++)
                for (var l = 0; l < 4; l++)
                    _lineEnergy[p, l] = Energy;
        }
        _lineEnergy[part, line] = value;
    }

    /// <summary>Reihe auf dem Eis eines Teils (0 Abwehr, 1 Sturm).</summary>
    public int Line(int part) => part == 0 ? DefenseLine : AttackLine;

    public void SetLine(int part, int line)
    {
        if (part == 0)
            DefenseLine = line;
        else
            AttackLine = line;
    }

    /// <summary>Minuten der Reihe auf dem Eis je Teil (Vereinsfelder 0x2F/0x31).</summary>
    public int[] OnIceMinutes { get; } = new int[2];

    /// <summary>Linienwechsel-Modus (Vereinsfeld 0x2C Bits 0–2): 0 kein Wechsel (Spielvorbereitung 0x84D7), sonst 1–4.</summary>
    public int LineMode { get; set; }

    /// <summary>Verein mit Manager (Vereinsfeld 0x16 ≠ 0), auch das Nationalteam des Bundestrainers.</summary>
    public bool Managed { get; init; }

    /// <summary>Linienwechsel-Einstellung des Managers (DGROUP 0xA700 + Manager); null bei Computervereinen.</summary>
    public LineChangeMode? LineChange { get; init; }

    /// <summary>Kader des Managervereins für die Reihenwerte aus den Spielern (0x98F8); null bei Computervereinen.</summary>
    public LiveSquad? Squad { get; init; }

    /// <summary>
    /// Kader nur für die Spielerwahl der Strafen (0x378C5), wenn <see cref="Squad"/> fehlt: Nationalteam des Bundestrainers
    /// (R8W3I-06). [ORIGINAL] 0x35AB8–0x35B2C ruft 0x378C5 für jeden Verein mit Vereinsfeld 0x16 ≠ 0, also auch für das
    /// Nationalteam (Manager 5, Kaderplätze DGROUP 0x5DC3 + 5 · 0x618).
    /// </summary>
    public LiveSquad? PenaltySquad { get; init; }

    /// <summary>Kader der Strafen: <see cref="Squad"/>, sonst <see cref="PenaltySquad"/>.</summary>
    public LiveSquad? PenaltyPlayers => Squad ?? PenaltySquad;

    /// <summary>
    /// Managerwert, der auf den Einsatz bei Strafen wirkt (DGROUP 0x974B + 0x1AF · Vereinsfeld 0x16, Wert − 20). Computer: 20.
    /// </summary>
    public int PenaltyManagerValue { get; init; } = 20;

    /// <summary>Torhüter draußen (Vereinsfeld 0x2C, Bit 3).</summary>
    public bool GoaliePulled { get; set; }

    /// <summary>
    /// Nur Managervereine: im Original setzt ein Gegner jeden Abschluss ins Tor, wenn DGROUP 0xDE15 + Manager = 0
    /// (Datei 0xA83E–0xA883; Bedeutung noch offen, vermutlich „kein Torhüter aufgestellt“).
    /// </summary>
    public bool OpenNet { get; set; }

    public PenaltyBox Box { get; } = new();

    /// <summary>Managerverein mit weniger als 8 fitten Spielern (0x120:0x66): Strafen werden gezogen, aber nicht verhängt.</summary>
    public bool FewFitPlayers { get; init; }

    /// <summary>Strafminuten dieses Spiels (im Original nur in der Liga gezählt: DGROUP 0x82A1 + 0x50 · Verein).</summary>
    public int PenaltyMinutes { get; set; }

    /// <summary>
    /// Managersatz (0 … Zahl der Manager − 1) für die Verletzungen im Spiel (0x38B8F), nur Managervereine mit Kader in der
    /// Managerpaarung; null sonst (auch das Nationalteam des Bundestrainers, siehe <see cref="OriginalMinutes.Injuries"/>).
    /// </summary>
    public int? InjuryManager { get; init; }

    /// <summary>Managerwert des eigenen Managers (DGROUP 0x974B + 0x1AF · Manager, 0x952C) für die Verletzungen.</summary>
    public int ManagerValue { get; init; } = 20;

    /// <summary>
    /// Über-/Unterzahl-Zähler des Managers (DGROUP 0x97D2 + 0x1AF · Manager, <see cref="Manager.PowerPlayCounts"/>), die
    /// die Minutenrechnung fortschreibt (<see cref="OriginalMinutes.GoalScored"/>, <see cref="OriginalMinutes.Penalty"/>);
    /// null bei Computervereinen. [APPROX] Auch beim Nationalteam null: das Original zählt dort (Vereinsfeld 0x16 = 6) in
    /// den Satz des Bundestrainers (Manager 5), den das Remake nicht führt.
    /// </summary>
    public int[]? PowerPlayCounts { get; init; }
}

/// <summary>Rahmen eines Spiels für die Minutenrechnung.</summary>
/// <param name="WorldCup">WM-Spiel (<c>[0x12B]</c> = 1).</param>
/// <param name="Playoff">Wettbewerbscode 1 (Play-off/Aufstieg): Torbudget halbiert, je Minute + 1.</param>
/// <param name="KnockOut">Bei Gleichstand nach 60 Minuten Verlängerung 61–65 und 66–70 (Prüfung 0xE8:0x70).</param>
/// <param name="ManagerMatch">Managerpaarung (0x130:0x4D): Energie, Reihenwechsel, Taktik je Minute (0x37F9B ff.).</param>
public readonly record struct MatchRules(bool WorldCup = false, bool Playoff = false, bool KnockOut = false, bool ManagerMatch = false);

/// <summary>Ergebnis der Minutenrechnung mit Ereignissen.</summary>
public sealed class MatchReport
{
    public int HomeGoals { get; set; }
    public int AwayGoals { get; set; }
    public bool Overtime { get; set; }
    public List<MatchEvent> Events { get; } = [];
    public int HomePenaltyMinutes { get; set; }
    public int AwayPenaltyMinutes { get; set; }

    /// <summary>Spielwertung ohne Berechnung (0:2/2:0, nicht spielfähiger Managerverein).</summary>
    public bool Forfeit { get; set; }

    /// <summary>Penaltyschießen: true Heim, false Gast, null keins (siehe <see cref="MatchEngine.PlayWithEvents"/>).</summary>
    public bool? ShootoutHomeWins { get; set; }

    /// <summary>Tore des Penaltyschießens nach der Verlängerung (<see cref="OriginalMinutes.Shootout"/>), sonst null.</summary>
    public (int Home, int Away)? ShootoutGoals { get; set; }
}

/// <summary>Minutenrechnung des Originals (Spur spielberechnung, Teil 2): Abschluss, Strafbank, Strafen, Ablauf je Minute.</summary>
public static partial class OriginalMinutes
{
    /// <summary>[ORIGINAL] Datei 0xA6DA/0xA78F: ab 29 bzw. 59 (mit Penaltymarke +30) Toren zählt kein Treffer mehr.</summary>
    private static bool GoalCap(int score) => score == 29 || score >= 59;

    /// <summary>
    /// Abschluss einer Torchance. [ORIGINAL] Datei 0xA484–0xA839:
    /// <list type="number">
    /// <item>Schwäche w = 1, wenn lrandom(550, 850) größer ist als die Summe der vier Stärken (beide Mannschaften in Modus 0
    ///   und 2, 0x368B1), sonst 0.</item>
    /// <item>Verlängerung (Minute &gt; 60): kein Tor, wenn das Heimergebnis kleiner als 10 ist (die Marke der Spiele in der
    ///   Verlängerung, siehe <c>docs/spielberechnung.md</c> §4).</item>
    /// <item>Heim schießt: lrandom(0, 2w + 4) ≤ 1 → kein Tor. Gast schießt: lrandom(0, 3·WM + 2w + 1) ≤ WM → kein Tor.</item>
    /// <item>d = (Angriff Schütze (Modus 2) − Abwehr Gegner (Modus 0)) / 3. d ≥ 0: Tor, wenn lrandom(0, d/4 + w + 2) ≠ 0.
    ///   d &lt; 0: Tor, wenn lrandom(0, −d/4 + 1) = 0 oder danach lrandom(0, 6 − w) = 0.</item>
    /// <item>Hat der Gegner <see cref="LiveTeam.OpenNet"/>, ist jeder Abschluss ein Tor (auch nach Nein).</item>
    /// </list>
    /// </summary>
    public static bool Shot(int homeDefensive, int homeOffensive, int awayDefensive, int awayOffensive, bool awayShoots,
        int homeScore, int awayScore, int minute, bool worldCup, bool defenderOpenNet, Random random)
    {
        var wm = worldCup ? 1 : 0;
        var sum = homeDefensive + awayOffensive + awayDefensive + homeOffensive;
        var weak = OriginalMatch.Rnd(random, 550, 850) > sum ? 1 : 0;
        if (minute > 60 && homeScore < 10)
            return false;
        var goal = false;
        if (!awayShoots)
        {
            if (!GoalCap(homeScore) && OriginalMatch.Rnd(random, 0, 2 * weak + 4) > 1)
                goal = Finish((homeOffensive - awayDefensive) / 3, weak, random);
        }
        else if (!GoalCap(awayScore) && OriginalMatch.Rnd(random, 0, 3 * wm + 2 * weak + 1) > wm)
        {
            goal = Finish((awayOffensive - homeDefensive) / 3, weak, random);
        }
        return goal || defenderOpenNet;
    }

    private static bool Finish(int d, int weak, Random random)
    {
        if (d >= 0)
            return OriginalMatch.Rnd(random, 0, (d >> 2) + weak + 2) != 0;
        return OriginalMatch.Rnd(random, 0, -d / 4 + 1) == 0 || OriginalMatch.Rnd(random, 0, 6 - weak) == 0;
    }

    /// <summary>
    /// Strafwurf einer Mannschaft in einer Minute. [ORIGINAL] Datei 0x35818–0x35CBD für Vereine ohne Manager:
    /// <list type="number">
    /// <item>Strafbank herunterzählen (<see cref="PenaltyBox.Tick"/>); sind danach beide Plätze belegt, keine Strafe.</item>
    /// <item>Strafe nach <see cref="OriginalMatch.PenaltyMinutes"/> (5 Minuten nur auf Platz 0), Feldspieler − 1.</item>
    /// <item>Mit 1/4 (lrandom(0, 3) = 0, nur auf Platz 0 und nicht im Folgeaufruf) gleich danach ein Wurf für den Gegner –
    ///   der zählt dessen Strafbank ein zweites Mal herunter (übernommen).</item>
    /// </list>
    /// Managervereine: [ORIGINAL] mit weniger als 8 fitten Spielern (0x120:0x66) keine Strafe, der Zug findet trotzdem
    /// statt (<see cref="LiveTeam.FewFitPlayers"/>). Einsatz + Managerwert − 20 (<see cref="LiveTeam.PenaltyManagerValue"/>,
    /// Datei 0x35873–0x358B4); Spieler aus 0x378C5 (<see cref="PenalizedPlayer"/>), Spielstrecke §13.
    /// </summary>
    /// <returns>Minuten der eigenen Strafe (0, 2 oder 5).</returns>
    public static int Penalty(LiveTeam club, LiveTeam opponent, int side, int minute, bool chained, List<MatchEvent> events, Random random)
    {
        var count = club.Box.Tick();
        if (count >= 2)
            return 0;
        var effort = club.Managed ? club.Effort + club.PenaltyManagerValue - 20 : club.Effort;
        var minutes = OriginalMatch.PenaltyMinutes(effort, count == 0, random);
        if (minutes == 0 || club.FewFitPlayers)
            return 0;
        club.Box.Add(count, minutes == 5 ? 13 : 2);
        club.PenaltyMinutes += minutes;
        var chain = !chained && OriginalMatch.Rnd(random, 0, 3) == 0 && count == 0;
        // [ORIGINAL] 0x35A26–0x35B22: Über-/Unterzahl zählen, vor der Spielerwahl (Teil 4, docs/spielberechnung.md §18).
        var counted = CountPowerPlayStart(club, opponent);
        LivePlayer? player = null;
        if (club.PenaltyPlayers is not null)
        {
            // [ORIGINAL] 0x35B26–0x35C17: Spieler 0x378C5; keiner (100) → 0x35B33 `cmp [bp-0x1],0x64` → 0x3583B: Rückgabe 0
            // ohne Anzeige (0x130:0x48 an 0x35C73) und ohne Kettenstrafe (R8W3I-04). Strafbank, Feldspieler und
            // Strafminuten des Vereins sind da schon gesetzt (0x35941–0x35986). Ein Torhüter bleibt auf dem Eis und bekommt
            // keine Strafe angerechnet; ein Feldspieler geht vom Eis (+0x0A = 0) bis zum Freiwerden.
            player = PenalizedPlayer(club, random);
            if (player is null)
                return 0;
            if (!player.Goalie)
            {
                player.Dressed = false;
                club.Box.Occupants[count] = player;
            }
        }
        events.Add(new MatchEvent(minute, side, MatchEventKind.Penalty, minutes,
            player is { Goalie: false } ? player.Player : null));
        // [ORIGINAL] 0x35C7B–0x35CB2: bleibt die Kettenstrafe aus (Rückgabe 0), wird der zuletzt gezählte Wert zurückgenommen.
        if (chain && Penalty(opponent, club, 1 - side, minute, chained: true, events, random) == 0 && counted is { } last)
            CountPowerPlay(last.Team, last.Counter, -1);
        return minutes;
    }

    /// <summary>
    /// Ein ganzes Spiel Minute für Minute (Paarung ohne Manager). [ORIGINAL] Ablauf 0x36B5D/0x37A62 (Liga 0x37DCF–0x380A4):
    /// drittelweise 1–20, 21–40, 41–60; je Minute Torchancen 0x9F5C, Abschluss je Chance (Heim zuerst), dann Strafen für
    /// Heim und Gast (außer in Minute 60 und 70). Bei <see cref="MatchRules.KnockOut"/> und Gleichstand Verlängerung 61–65
    /// und 66–70.
    /// [APPROX] Die Verlängerung läuft wie im Original ohne „Sudden Death“ über beide Abschnitte; die Ergebnismarken
    /// (+10/+30) des Originals bildet das Remake über <see cref="MatchReport.Overtime"/> ab.
    /// </summary>
    public static MatchReport Play(LiveTeam home, LiveTeam away, MatchRules rules, Random random)
    {
        var report = new MatchReport();
        PlayPeriod(home, away, rules, 1, 20, report, random);
        PlayPeriod(home, away, rules, 21, 40, report, random);
        PlayPeriod(home, away, rules, 41, 60, report, random);
        if (rules.KnockOut && report.HomeGoals == report.AwayGoals)
        {
            report.Overtime = true;
            PlayPeriod(home, away, rules, 61, 65, report, random);
            PlayPeriod(home, away, rules, 66, 70, report, random);
        }
        report.HomePenaltyMinutes = home.PenaltyMinutes;
        report.AwayPenaltyMinutes = away.PenaltyMinutes;
        return report;
    }

    private static void PlayPeriod(LiveTeam home, LiveTeam away, MatchRules rules, int from, int to, MatchReport report, Random random)
    {
        if (from != 1)
        {
            // [ORIGINAL] 0x37A86: in jeder Drittelpause (auch vor der Verlängerung) 0x84D7(1).
            RecoverEnergy(home);
            RecoverEnergy(away);
        }
        for (var minute = from; minute <= to; minute++)
            PlayMinute(home, away, rules, minute, to, report, random);
    }

    /// <summary>Eine Spielminute einer Paarung ohne Manager (Reihenfolge der Züge wie im Original).</summary>
    public static void PlayMinute(LiveTeam home, LiveTeam away, MatchRules rules, int minute, int periodEnd, MatchReport report, Random random)
    {
        // [ORIGINAL] 0x37ECF–0x37F06: hat einer der Vereine einen Manager, vor den Torchancen Verletzungen 0x38B8F.
        if (rules.ManagerMatch)
            Injuries(home, away, minute, report, random);
        var h = Strengths(home, away, minute);
        var a = Strengths(away, home, minute);
        var flags = OriginalMatch.MinuteChances(
            new MatchSide(h.Normal, home.Box.Skaters, home.AttackLevel, home.GoaliePulled),
            new MatchSide(a.Normal, away.Box.Skaters, away.AttackLevel, away.GoaliePulled),
            minute, periodEnd, rules.WorldCup, rules.Playoff, random);
        // Verlängerung: Heimergebnis mit Marke ≥ 10 (siehe Shot).
        var mark = minute > 60 ? 30 : 0;
        // [ORIGINAL] 0x37E2D/0x37E62: Spielstand einmal je Minute in lokale Variablen ([bp-0xd]/[bp-0xe]); die Tore dieser
        // Minute ändern sie nicht, 0x80CB und 0x939A bekommen den Stand davor (R8W3I-01, #269).
        var (homeBefore, awayBefore) = (report.HomeGoals, report.AwayGoals);
        if ((flags & 1) != 0)
            Score(home, away, 0, h, a, minute, rules, report, mark, random);
        if ((flags & 2) != 0)
            Score(home, away, 1, h, a, minute, rules, report, mark, random);
        var (x, y, sx, sy) = (home, away, 0, 1);
        if (rules.ManagerMatch)
        {
            // [ORIGINAL] 0x37F9B–0x38073, nur Managerpaarungen: Energie (0x87F7), Seiten zufällig tauschen (random(0, 1)),
            // ungerade Minute Modus/Torhüter der Computer (0x80CB), Reihenwechsel (0x8EF2), gerade Minute Taktik (0x939A).
            DrainEnergy(home, random);
            DrainEnergy(away, random);
            if (OriginalMatch.Rnd(random, 0, 1) != 0)
                (x, y, sx, sy) = (away, home, 1, 0);
            var gx = sx == 0 ? homeBefore : awayBefore;
            var gy = sx == 0 ? awayBefore : homeBefore;
            if (minute % 2 != 0)
                ComputerLineModes(x, y, gx, gy, minute, sx == 0 ? h.Normal : a.Normal, sx == 0 ? a.Normal : h.Normal);
            ChangeLines(x, y);
            ChangeLines(y, x);
            if (minute % 2 == 0)
            {
                FollowTactics(x, gx - gy);
                FollowTactics(y, gy - gx);
            }
        }
        if (minute != 60 && minute != 70)
        {
            Penalty(x, y, sx, minute, chained: false, report.Events, random);
            Penalty(y, x, sy, minute, chained: false, report.Events, random);
        }
    }

    /// <summary>
    /// Einsatz und Angriff nach dem Spielstand (nur mit <see cref="LiveTeam.Tactics"/>, siehe dort). [ORIGINAL] 0x939A,
    /// gerade Minuten der Managerpaarung für beide Vereine: Grundwert + Zuschlag des Spielstands, dazu bei einer
    /// Energie der Reihen auf dem Eis ((Abwehr + Sturm) / 2) unter 50 deren Differenz zu 50; begrenzt auf 0 … 100. Bei
    /// „MANUELL“ schreibt das Original keinen Einsatz ([APPROX] das Remake nimmt dann die eigenen Werte ohne Energieabzug).
    /// </summary>
    private static void FollowTactics(LiveTeam team, int goalDifference)
    {
        if (team.Tactics is not { } tactics)
            return;
        var effort = tactics.EffectiveEffort(0, goalDifference, team.TacticsStrength, team.TacticsOpponentStrength);
        var energy = (team.LineEnergy(0, team.DefenseLine) + team.LineEnergy(1, team.AttackLine)) >> 1;
        if (energy < 50 && tactics.EffortMode != TacticMode.Manual)
            effort += energy - 50;
        team.Effort = Math.Clamp(effort, 0, 100);
        team.AttackLevel = tactics.EffectiveAttack(0, goalDifference, team.TacticsStrength, team.TacticsOpponentStrength);
    }

    private static void Score(LiveTeam home, LiveTeam away, int side, (int Normal, int Defensive, int Offensive) h,
        (int Normal, int Defensive, int Offensive) a, int minute, MatchRules rules, MatchReport report, int mark, Random random)
    {
        var defender = side == 0 ? away : home;
        var shooter = side == 0 ? home : away;
        // [ORIGINAL] 0xA642–0xA650: Abbruch in der Verlängerung vor jeder Wertung (ohne Szene, siehe Shot).
        var aborted = minute > 60 && report.HomeGoals + mark < 10;
        // [ORIGINAL] 0xA491–0xA4BC: Feldspieler beider Vereine (Vereinsfeld 0x32) zu Beginn des Abschlusses.
        var (homeSkaters, awaySkaters) = (home.Box.Skaters, away.Box.Skaters);
        var goal = Shot(h.Defensive, h.Offensive, a.Defensive, a.Offensive, side == 1, report.HomeGoals + mark,
            report.AwayGoals + mark, minute, rules.WorldCup, defender.OpenNet, random);
        if (goal)
        {
            if (side == 0)
                report.HomeGoals++;
            else
                report.AwayGoals++;
            GoalScored(home, away, side, homeSkaters, awaySkaters);
        }
        // [ORIGINAL] 0xAB47–0xAB90: in der Managerpaarung nach jedem Abschluss Szene/Torschütze 0x541D9 (ScoringPlayers).
        var players = rules.ManagerMatch && !aborted ? ScoringPlayers(shooter, goal, random) : null;
        if (!goal)
            return;
        if (players is { } scoring)
            scoring.Scorer.MatchGoals++;
        report.Events.Add(new MatchEvent(minute, side, MatchEventKind.Goal, Player: players?.Scorer.Player,
            Assist: players?.Assist.Player));
    }

    /// <summary>
    /// Schneller Weg der Pokale ohne Manager und ohne Anzeige. [ORIGINAL] Datei 0x3731F–0x377A3:
    /// <list type="number">
    /// <item>s = Stärke (0x368B1, Gewichtung 1, Minute 0) + 6 für Vereine (Index &lt; 0x40), k = min(4, |s_H/3 − s_G/3|).</item>
    /// <item>Endstand (h, g) aus 0x9F5C(…, 0, 60) (<see cref="OriginalMatch.FullResult"/>).</item>
    /// <item>Glättung ohne WM: mit 1/4 – oder, wenn der Gast stärker ist, mit 2/3 – h −= lrandom(0, h); dann mit 1/4 – oder
    ///   bei stärkerem Gast mit 1/2 – g += lrandom(0, 4). In der WM statt dessen h −= lrandom(0, h), h += lrandom(0, h)
    ///   bzw. h += lrandom(0, h), h −= lrandom(0, h) (der Gast bleibt unverändert, übernommen).</item>
    /// <item>Ist lrandom(0, k) ≠ 0 und passt die Torreihenfolge nicht zur Stärke (h ≤ g bei stärkerem Heim, h ≥ g bei
    ///   stärkerem Gast), alles noch einmal ab Schritt 2.</item>
    /// <item>K.-o.-Gleichstand: random(0, 2) je Seite dazu, bis ungleich; Heim + 30 als Penaltymarke (hier
    ///   <c>Shootout</c>).</item>
    /// </list>
    /// </summary>
    public static (int Home, int Away, bool Shootout) FastCupResult(MatchSide home, MatchSide away, bool homeIsClub,
        bool awayIsClub, bool worldCup, bool playoff, bool knockOut, Random random)
    {
        var (h, g, shootout, _) = FastCupResultWithTie(home, away, homeIsClub, awayIsClub, worldCup, playoff, knockOut, random);
        return (h, g, shootout);
    }

    /// <summary>Wie <see cref="FastCupResult"/>, zusätzlich der Gleichstand vor dem Penaltyschießen.</summary>
    public static (int Home, int Away, bool Shootout, int Tie) FastCupResultWithTie(MatchSide home, MatchSide away,
        bool homeIsClub, bool awayIsClub, bool worldCup, bool playoff, bool knockOut, Random random)
    {
        var si = home.Strength + (homeIsClub ? 6 : 0);
        var di = away.Strength + (awayIsClub ? 6 : 0);
        var k = Math.Min(4, Math.Abs(si / 3 - di / 3));
        int h, g;
        while (true)
        {
            (h, g) = OriginalMatch.FullResult(home, away, worldCup, playoff, random);
            if (!worldCup)
            {
                if (OriginalMatch.Rnd(random, 0, 3) == 0 || (si < di && OriginalMatch.Rnd(random, 0, 2) != 0))
                    h -= OriginalMatch.Rnd(random, 0, h);
                if (OriginalMatch.Rnd(random, 0, 3) == 0 || (si < di && OriginalMatch.Rnd(random, 0, 1) != 0))
                    g += OriginalMatch.Rnd(random, 0, 4);
            }
            else
            {
                if (OriginalMatch.Rnd(random, 0, 3) == 0 || (si < di && OriginalMatch.Rnd(random, 0, 2) != 0))
                {
                    h -= OriginalMatch.Rnd(random, 0, h);
                    h += OriginalMatch.Rnd(random, 0, h);
                }
                if (OriginalMatch.Rnd(random, 0, 3) == 0 || (si > di && OriginalMatch.Rnd(random, 0, 1) != 0))
                {
                    h += OriginalMatch.Rnd(random, 0, h);
                    h -= OriginalMatch.Rnd(random, 0, h);
                }
            }
            if (OriginalMatch.Rnd(random, 0, k) == 0)
                break;
            if (h <= g && si > di)
                continue;
            if (h >= g && si < di)
                continue;
            break;
        }
        if (!knockOut || h != g)
            return (h, g, false, 0);
        int x, y;
        do
        {
            x = OriginalMatch.Rnd(random, 0, 2);
            y = OriginalMatch.Rnd(random, 0, 2);
        }
        while (h + x == g + y);
        return (h + x, g + y, true, h);
    }

    /// <summary>
    /// Stärken einer Mannschaft in dieser Minute: Gewichtung 1 (Torchancen 0x9F5C), 0 (Abwehr) und 2 (Angriff) für den
    /// Abschluss 0xA484 – alle aus denselben Reihenwerten (0xA484 ruft 0x368B1 mit „neu“ = 0, also aus dem Puffer).
    /// </summary>
    public static (int Normal, int Defensive, int Offensive) Strengths(LiveTeam team, LiveTeam opponent, int minute)
    {
        var advantage = team.Box.Skaters - opponent.Box.Skaters;
        PartValues defense, attack;
        if (team.Squad is { } squad)
        {
            // [ORIGINAL] 0x98F8: Managervereine bilden die Werte aus den Spielern der Reihen auf dem Eis.
            defense = MatchEngine.ManagedLineValues(squad, team.DefenseLine, 0, advantage, team.Effort, minute, inMatch: true);
            attack = MatchEngine.ManagedLineValues(squad, team.AttackLine, 1, advantage, team.Effort, minute, inMatch: true);
            team.OpenNet = !squad.HasGoalie;
        }
        else
        {
            defense = OriginalMatch.LineValues(team.Defense, team.DefenseLine, attack: false, advantage,
                team.LineEnergy(0, team.DefenseLine), team.Effort, inMatch: true);
            attack = OriginalMatch.LineValues(team.Attack, team.AttackLine, attack: true, advantage,
                team.LineEnergy(1, team.AttackLine), team.Effort, inMatch: true);
        }
        return (OriginalMatch.TeamStrength(defense, attack, 1, minute, team.ClubForm),
            OriginalMatch.TeamStrength(defense, attack, 0, minute, team.ClubForm),
            OriginalMatch.TeamStrength(defense, attack, 2, minute, team.ClubForm));
    }
}
