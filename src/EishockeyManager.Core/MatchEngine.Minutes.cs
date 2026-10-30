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
            State = ((State & 0x80) != 0 ? 8 : 0) | (t1 - 1);
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
            if ((State & 0x80) != 0)
                State = ((State >> 4) & 7) | 8;
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
        return true;
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
}

/// <summary>Ein Ereignis: Minute (1 … 70), Seite (0 Heim, 1 Gast), bei Strafen die Minuten (2 oder 5).</summary>
public readonly record struct MatchEvent(int Minute, int Side, MatchEventKind Kind, int PenaltyMinutes = 0);

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
    /// Minuten, in denen die Taktik nachgeführt wird: 0 gerade (Managerverein, Spielstandtaktik 0x939A), 1 ungerade
    /// (Computerverein, Taktik 0x80CB). [ORIGINAL] Takt aus 0x37DCF ff.; die Werte liefert [APPROX]
    /// <see cref="Core.Tactics.EffectiveEffort"/>/<see cref="Core.Tactics.EffectiveAttack"/> des Blocks 0 (0x939A und
    /// 0x80CB sind nicht gelesen).
    /// </summary>
    public int TacticsParity { get; init; }

    /// <summary>Eigene und gegnerische Stärke für die Automatik der Taktik (Datei 0x8365, doppelte Differenz).</summary>
    public int TacticsStrength { get; init; }

    public int TacticsOpponentStrength { get; init; }

    /// <summary>Reihe auf dem Eis 0–2 je Teil (Vereinsfelder 0x2E/0x30). Computervereine wechseln nur in Managerpaarungen.</summary>
    public int DefenseLine { get; set; }

    public int AttackLine { get; set; }

    /// <summary>Energie der Reihen (Vereinsfeld 0x1E + 4 · Teil + Reihe) 0–100.</summary>
    public int Energy { get; init; } = 100;

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
}

/// <summary>Rahmen eines Spiels für die Minutenrechnung.</summary>
/// <param name="WorldCup">WM-Spiel (<c>[0x12B]</c> = 1).</param>
/// <param name="Playoff">Wettbewerbscode 1 (Play-off/Aufstieg): Torbudget halbiert, je Minute + 1.</param>
/// <param name="KnockOut">Bei Gleichstand nach 60 Minuten Verlängerung 61–65 und 66–70 (Prüfung 0xE8:0x70).</param>
public readonly record struct MatchRules(bool WorldCup = false, bool Playoff = false, bool KnockOut = false);

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

    /// <summary>Penaltyschießen des schnellen Wegs: true Heim, false Gast, null keins (siehe <see cref="MatchEngine.PlayWithEvents"/>).</summary>
    public bool? ShootoutHomeWins { get; set; }
}

/// <summary>Minutenrechnung des Originals (Spur spielberechnung, Teil 2): Abschluss, Strafbank, Strafen, Ablauf je Minute.</summary>
public static class OriginalMinutes
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
    /// statt (<see cref="LiveTeam.FewFitPlayers"/>). Einsatzzuschlag (DGROUP 0x974B) und Spielerwahl 0x378C5 sind nicht
    /// gelesen; den bestraften Spieler wählt <see cref="MatchEngine.PlayWithEvents"/> ([APPROX]).
    /// </summary>
    /// <returns>Minuten der eigenen Strafe (0, 2 oder 5).</returns>
    public static int Penalty(LiveTeam club, LiveTeam opponent, int side, int minute, bool chained, List<MatchEvent> events, Random random)
    {
        var count = club.Box.Tick();
        if (count >= 2)
            return 0;
        var minutes = OriginalMatch.PenaltyMinutes(club.Effort, count == 0, random);
        if (minutes == 0 || club.FewFitPlayers)
            return 0;
        club.Box.Add(count, minutes == 5 ? 13 : 2);
        club.PenaltyMinutes += minutes;
        events.Add(new MatchEvent(minute, side, MatchEventKind.Penalty, minutes));
        if (!chained && OriginalMatch.Rnd(random, 0, 3) == 0 && count == 0)
            Penalty(opponent, club, 1 - side, minute, chained: true, events, random);
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
        for (var minute = from; minute <= to; minute++)
            PlayMinute(home, away, rules, minute, to, report, random);
    }

    /// <summary>Eine Spielminute einer Paarung ohne Manager (Reihenfolge der Züge wie im Original).</summary>
    public static void PlayMinute(LiveTeam home, LiveTeam away, MatchRules rules, int minute, int periodEnd, MatchReport report, Random random)
    {
        var h = Strengths(home, away, minute);
        var a = Strengths(away, home, minute);
        var flags = OriginalMatch.MinuteChances(
            new MatchSide(h.Normal, home.Box.Skaters, home.AttackLevel, home.GoaliePulled),
            new MatchSide(a.Normal, away.Box.Skaters, away.AttackLevel, away.GoaliePulled),
            minute, periodEnd, rules.WorldCup, rules.Playoff, random);
        // Verlängerung: Heimergebnis mit Marke ≥ 10 (siehe Shot).
        var mark = minute > 60 ? 30 : 0;
        if ((flags & 1) != 0)
            Score(home, away, 0, h, a, minute, rules, report, mark, random);
        if ((flags & 2) != 0)
            Score(home, away, 1, h, a, minute, rules, report, mark, random);
        FollowTactics(home, report.HomeGoals - report.AwayGoals, minute);
        FollowTactics(away, report.AwayGoals - report.HomeGoals, minute);
        if (minute != 60 && minute != 70)
        {
            Penalty(home, away, 0, minute, chained: false, report.Events, random);
            Penalty(away, home, 1, minute, chained: false, report.Events, random);
        }
    }

    /// <summary>Einsatz und Angriff nach dem Spielstand (nur mit <see cref="LiveTeam.Tactics"/>, siehe dort).</summary>
    private static void FollowTactics(LiveTeam team, int goalDifference, int minute)
    {
        if (team.Tactics is not { } tactics || minute % 2 != team.TacticsParity)
            return;
        team.Effort = tactics.EffectiveEffort(0, goalDifference, team.TacticsStrength, team.TacticsOpponentStrength);
        team.AttackLevel = tactics.EffectiveAttack(0, goalDifference, team.TacticsStrength, team.TacticsOpponentStrength);
    }

    private static void Score(LiveTeam home, LiveTeam away, int side, (int Normal, int Defensive, int Offensive) h,
        (int Normal, int Defensive, int Offensive) a, int minute, MatchRules rules, MatchReport report, int mark, Random random)
    {
        var defender = side == 0 ? away : home;
        if (!Shot(h.Defensive, h.Offensive, a.Defensive, a.Offensive, side == 1, report.HomeGoals + mark,
                report.AwayGoals + mark, minute, rules.WorldCup, defender.OpenNet, random))
            return;
        if (side == 0)
            report.HomeGoals++;
        else
            report.AwayGoals++;
        report.Events.Add(new MatchEvent(minute, side, MatchEventKind.Goal));
        // [ORIGINAL] Datei 0xA8AC–0xA933: Gegentor in Unterzahl beendet eine Strafe (0x35540(…, 0), dann 0x35691(…, 0, 0)).
        if (defender.Box.Count > 0)
            defender.Box.Release(0, force: false);
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
        var defense = OriginalMatch.LineValues(team.Defense, team.DefenseLine, attack: false, advantage,
            team.Energy, team.Effort, inMatch: true);
        var attack = OriginalMatch.LineValues(team.Attack, team.AttackLine, attack: true, advantage,
            team.Energy, team.Effort, inMatch: true);
        return (OriginalMatch.TeamStrength(defense, attack, 1, minute, team.ClubForm),
            OriginalMatch.TeamStrength(defense, attack, 0, minute, team.ClubForm),
            OriginalMatch.TeamStrength(defense, attack, 2, minute, team.ClubForm));
    }
}
