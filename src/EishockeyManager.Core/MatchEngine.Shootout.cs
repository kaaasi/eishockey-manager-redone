using System.Runtime.CompilerServices;

namespace EishockeyManager.Core;

/// <summary>
/// Ein Schuss des Penaltyschießens mit Anzeige (A101, T7): Seite des Schützen (0 Heim, 1 Gast), Treffer und – mit Darstellung
/// ≠ 0 – die Szene der Torszene 0x53C57 (sonst null).
/// </summary>
public readonly record struct ShootoutShot(int Side, bool Goal, GoalSceneChoice? Scene);

/// <summary>
/// Penaltyschießen nach der Verlängerung (Spur spielberechnung-komplett, Teil 5). Fundstellen:
/// <c>docs/spielberechnung.md</c> §19.
/// </summary>
public static partial class OriginalMinutes
{
    /// <summary>
    /// Penaltyschießen. [ORIGINAL] 0x3901B (Stub 0xF0:0x9D, gerufen aus 0x32AA3 für K.-o.-Spiele, die nach der Verlängerung
    /// noch unentschieden sind – Heimbyte − 30 = Gastbyte):
    /// <list type="number">
    /// <item>Datei 0x39021–0x3902F: random(0, 1) immer zuerst – die Seite des ersten Schützen (0 Heim, 1 Gast; Datei 0x390A4–0x390A7
    ///   hält ihn als Rundenanfang fest).</item>
    /// <item>Ist keiner der beiden Vereine zum Zusehen markiert (Vereinsfeld 0x34 Bit 7, Datei 0x390CB/0x390FA), Treffer je
    ///   Seite random(2, 5), Heim zuerst, wiederholt bis ungleich (0x39105–0x39179).</item>
    /// <item>A101: Sonst Schuss für Schuss ab 0 : 0 (Datei 0x39280–0x3929A): Treffer bei random(0, 10) &gt; 6 (Datei
    ///   0x39429–0x39443, 0x39475), Seite wechselt nach jedem Schuss (0x394EA), eine Runde ist fertig, wenn wieder der erste
    ///   Schütze dran ist (0x394EE–0x394F6). Nach fünf Runden geht es weiter, solange es gleich steht oder die Runde nicht fertig
    ///   ist (Datei 0x39507–0x39547) – das Schießen endet nie vorzeitig.</item>
    /// <item>Datei 0x39770–0x397B3: Die Treffer kommen auf den Spielstand (Heim + Marke 30 bleibt, also Endstand
    ///   Gleichstand + Penaltytore).</item>
    /// </list>
    /// Im Zweig mit Bit 7 je Schuss die Torszene 0x130:0x7F (Datei 0x394C5; Szenenwahl und Schüsse siehe
    /// <see cref="Shootout(Random, bool, MatchReport, GameOptions)"/>). [APPROX] Das Remake führt die Penaltytore getrennt
    /// (<see cref="MatchReport.ShootoutGoals"/>), der Spielstand bleibt der Gleichstand.
    /// </summary>
    /// <param name="watched">Heim- oder Gastverein zum Zusehen markiert (<see cref="Club.WatchedLive"/>).</param>
    public static (int Home, int Away) Shootout(Random random, bool watched = false) => Shootout(random, watched, null, null);

    /// <summary>
    /// Penaltyschießen wie <see cref="Shootout(Random, bool)"/>; im Zweig mit Anzeige die Schüsse an <paramref name="report"/>
    /// (<see cref="ShootoutShots"/>). A472/T7 (Spur torszene-dauer Teil 2, <c>docs/analyse/torszene-dauer.md</c> §6):
    /// [ORIGINAL] Je Schuss ruft 0x3901B die Torszene 0x53C57 mit Art = Treffer, Penalty-Kennzeichen 1 und Minute 0x78
    /// (Datei 0x39479–0x394C5). Mit Darstellung <c>[0x226]</c> ≠ 0 läuft sie auf dem eigenen Eis des Penaltyschießens
    /// (Datei 0x3937F–0x393D8: <c>0x5A55</c> = 1) und spielt die Szene; deren Wahl 0x575C3 zieht wie im Spiel aus dem
    /// Spielzufall (R18-D3 = A, <see cref="GoalSceneSelection.Choose"/> mit Penalty, also nach den beiden ersten Zügen
    /// random(2, Penalty)). [APPROX] Der Torschütze 0x541D9 (Art 0/1, ohne Bedingung) zieht im Original danach weiter; das
    /// Remake zieht dort wie bisher nichts.
    /// </summary>
    public static (int Home, int Away) Shootout(Random random, bool watched, MatchReport? report, GameOptions? options)
    {
        var first = OriginalMatch.Rnd(random, 0, 1);
        int home, away;
        if (watched)
        {
            var shots = new List<ShootoutShot>();
            var result = ShotByShot(random, first, shots, options is { LiveMode: not 0 });
            if (report is not null)
                Shots.AddOrUpdate(report, shots);
            return result;
        }
        do
        {
            home = OriginalMatch.Rnd(random, 2, 5);
            away = OriginalMatch.Rnd(random, 2, 5);
        }
        while (home == away);
        return (home, away);
    }

    /// <summary>Schussfolge des Zweigs mit Anzeige (A101, siehe <see cref="Shootout"/>).</summary>
    private static (int Home, int Away) ShotByShot(Random random, int first, List<ShootoutShot> shots, bool scenes)
    {
        var goals = new int[2];
        var side = first;
        var rounds = 0;
        do
        {
            var goal = OriginalMatch.Rnd(random, 0, 10) > 6;
            if (goal)
                goals[side]++;
            // [ORIGINAL] Datei 0x394C5: Torszene je Schuss; Szenenwahl nur mit Darstellung ≠ 0 (Datei 0x53DD2–0x53DE3).
            var scene = scenes
                ? GoalSceneSelection.Choose(goal, penalty: true, side, sixSkaters: false, GoalSceneSelection.OriginalCounts, random)
                : (GoalSceneChoice?)null;
            shots.Add(new ShootoutShot(side, goal, scene));
            side ^= 1;
            if (side == first)
                rounds++;
        }
        while (rounds < ShootoutRounds || goals[0] == goals[1] || side != first);
        return (goals[0], goals[1]);
    }

    private static readonly ConditionalWeakTable<MatchReport, List<ShootoutShot>> Shots = new();

    /// <summary>Schüsse des Penaltyschießens mit Anzeige in ihrer Reihenfolge; leer ohne Anzeigezweig (Darstellungszustand).</summary>
    public static IReadOnlyList<ShootoutShot> ShootoutShots(MatchReport report) =>
        Shots.TryGetValue(report, out var shots) ? shots : [];

    /// <summary>[ORIGINAL] Mindestens fünf Runden (Datei 0x39507: <c>cmp byte [bp-0x6],0x5</c>).</summary>
    public const int ShootoutRounds = 5;
}
