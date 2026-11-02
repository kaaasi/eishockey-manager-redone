namespace EishockeyManager.Core;

/// <summary>
/// K.-o.-Spiel der Pokale (DEB-Pokal, WM-Endrunde) mit Endstand und Ergebnismarken wie im Original (Spur wettbewerbe-spiel,
/// Teil 1; Inventur P2 Nr. 3, A003, A032). Fundstellen: <c>docs/spielberechnung.md</c> §3 und §19.
/// </summary>
/// <remarks>
/// Das Original kodiert den Ausgang im Ergebnisbyte des Heimvereins (Zeilen DGROUP 0x5C4B): + 30 je Marke, die Anzeige
/// 0x32E23 zieht je 30 ab und setzt dafür einen Punkt hinter das Ergebnis (Datei 0x338AB–0x338E3, höchstens zwei).
/// <list type="bullet">
/// <item>[ORIGINAL] Schneller Weg, Gleichstand: Heim = h + random(0, 2) + 30, Gast = g + random(0, 2), bis ungleich
///   (Datei 0x376F0–0x377A3, Marke 0x37761) – Endstand mit einer Marke.</item>
/// <item>[ORIGINAL] Minutenweg, nach der Verlängerung noch Gleichstand: Penaltyschießen 0x3901B nimmt die Marke ab
///   (0x39088: Heimbyte − 30), Endstand = Gleichstand + Treffer, Heim + 30 (0x39789, 0x397B3) – eine Marke.</item>
/// <item>Minutenweg, Tor in der Verlängerung: [ORIGINAL] 0x38402 schreibt den Spielstand mit Heim + 30 auf den schon
///   markierten Stand; im Video zwei Punkte („4: 3..“, eisbaeren_f44_01330, neben „4: 3.“ nach Penaltyschießen).
///   [APPROX] Woher der Stand vor 0x38402 kommt (Tabelle DGROUP 0xA0BF, 0x37E2D), ist nicht verfolgt; die zwei Marken
///   stützen sich auf das Videobild.</item>
/// </list>
/// Der Spielbericht (<see cref="MatchReport"/>) hält weiter den Gleichstand und die Penaltytore getrennt, damit die
/// Spielerzähler nur echte Tore zählen; <see cref="KnockoutMatch.PenaltyWinner"/> nennt nach einem Penaltyschießen den
/// Sieger. Die Play-offs (<c>Playoffs.cs</c>, Spur saisonwechsel) bleiben bei Gleichstand + Sieger.
/// </remarks>
public static class CupMatch
{
    /// <summary>Höchstzahl der Marken, die die Anzeige auswertet (Datei 0x338AB–0x338E3).</summary>
    public const int MaxMarks = 2;

    /// <summary>Spielt ein K.-o.-Spiel (<see cref="MatchEngine.PlayWithEvents"/>) und bildet Endstand und Marken.</summary>
    public static (KnockoutMatch Match, int Marks) Play(GameState game, int day, Club home, Club away,
        MatchCompetition competition, Random random)
    {
        var report = MatchEngine.PlayWithEvents(game, home, away, competition, knockOut: true, random);
        return FromReport(day, home, away, report);
    }

    /// <summary>
    /// Endstand und Marken aus dem Spielbericht: 0 = reguläre Spielzeit (oder Spielwertung), 1 = Penaltyschießen (Endstand
    /// = Gleichstand + Penaltytore), 2 = in der Verlängerung entschieden.
    /// </summary>
    public static (KnockoutMatch Match, int Marks) FromReport(int day, Club home, Club away, MatchReport report)
    {
        var (hg, ag) = (report.HomeGoals, report.AwayGoals);
        if (report.Forfeit || hg != ag)
        {
            var marks = !report.Forfeit && report.Overtime ? 2 : 0;
            return (new KnockoutMatch(day, home.Index, away.Index, hg, ag), marks);
        }
        if (report.ShootoutGoals is { } goals && goals.Home != goals.Away)
        {
            var winner = goals.Home > goals.Away ? home.Index : away.Index;
            return (new KnockoutMatch(day, home.Index, away.Index, hg + goals.Home, ag + goals.Away, winner), 1);
        }
        // Beide Wege liefern bei Gleichstand immer Penaltytore; bleibt nur der Sieger, gilt er mit Gleichstand.
        var fallback = report.ShootoutHomeWins is false ? away.Index : home.Index;
        return (new KnockoutMatch(day, home.Index, away.Index, hg, ag, fallback), 1);
    }
}
