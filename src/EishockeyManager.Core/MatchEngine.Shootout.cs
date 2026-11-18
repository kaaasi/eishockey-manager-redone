namespace EishockeyManager.Core;

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
    /// [APPROX] Das Remake hat für den Zweig mit Bit 7 keine Anzeige (Szene 0x130:0x7F und Meldung 0x130:0x2A je Schuss, Datei
    /// 0x394C5/0x394E3; ob sie Zufallszahlen ziehen, ist nicht gelesen – das Remake zieht dort keine) und führt die
    /// Penaltytore getrennt (<see cref="MatchReport.ShootoutGoals"/>), der Spielstand bleibt der Gleichstand.
    /// </summary>
    /// <param name="watched">Heim- oder Gastverein zum Zusehen markiert (<see cref="Club.WatchedLive"/>).</param>
    public static (int Home, int Away) Shootout(Random random, bool watched = false)
    {
        var first = OriginalMatch.Rnd(random, 0, 1);
        int home, away;
        if (watched)
            return ShotByShot(random, first);
        do
        {
            home = OriginalMatch.Rnd(random, 2, 5);
            away = OriginalMatch.Rnd(random, 2, 5);
        }
        while (home == away);
        return (home, away);
    }

    /// <summary>Schussfolge des Zweigs mit Anzeige (A101, siehe <see cref="Shootout"/>).</summary>
    private static (int Home, int Away) ShotByShot(Random random, int first)
    {
        var goals = new int[2];
        var side = first;
        var rounds = 0;
        do
        {
            if (OriginalMatch.Rnd(random, 0, 10) > 6)
                goals[side]++;
            side ^= 1;
            if (side == first)
                rounds++;
        }
        while (rounds < ShootoutRounds || goals[0] == goals[1] || side != first);
        return (goals[0], goals[1]);
    }

    /// <summary>[ORIGINAL] Mindestens fünf Runden (Datei 0x39507: <c>cmp byte [bp-0x6],0x5</c>).</summary>
    public const int ShootoutRounds = 5;
}
