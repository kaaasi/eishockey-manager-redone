namespace EishockeyManager.Core;

/// <summary>
/// Penaltyschießen nach der Verlängerung (Spur spielberechnung-komplett, Teil 5). Fundstellen:
/// <c>docs/spielberechnung.md</c> §19.
/// </summary>
public static partial class OriginalMinutes
{
    /// <summary>
    /// Penaltyschießen ohne Anzeige. [ORIGINAL] 0x3901B (Stub 0xF0:0x9D, gerufen aus 0x32AA3 für K.-o.-Spiele, die nach der
    /// Verlängerung noch unentschieden sind – Heimbyte − 30 = Gastbyte):
    /// <list type="number">
    /// <item>Datei 0x39021–0x3902F: random(0, 1) immer zuerst (nur die Anzeige wertet ihn aus).</item>
    /// <item>Ist keiner der beiden Vereine zum Zusehen markiert (Vereinsfeld 0x34 Bit 7, Datei 0x390CB/0x390FA), Treffer je
    ///   Seite random(2, 5), Heim zuerst, wiederholt bis ungleich (0x39105–0x39179).</item>
    /// <item>Datei 0x39770–0x397B3: Die Treffer kommen auf den Spielstand (Heim + Marke 30 bleibt, also Endstand
    ///   Gleichstand + Penaltytore).</item>
    /// </list>
    /// [APPROX] Das Remake hat keine Live-Anzeige (Zweig mit Bit 7, 0x3917E–0x3976F, nicht umgesetzt) und führt die
    /// Penaltytore getrennt (<see cref="MatchReport.ShootoutGoals"/>), der Spielstand bleibt der Gleichstand.
    /// </summary>
    public static (int Home, int Away) Shootout(Random random)
    {
        _ = OriginalMatch.Rnd(random, 0, 1);
        int home, away;
        do
        {
            home = OriginalMatch.Rnd(random, 2, 5);
            away = OriginalMatch.Rnd(random, 2, 5);
        }
        while (home == away);
        return (home, away);
    }
}
