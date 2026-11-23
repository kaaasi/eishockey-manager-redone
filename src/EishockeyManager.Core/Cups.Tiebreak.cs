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
///   (Datei 0x376F0–0x377A3, Marke 0x37761) – Endstand mit einer Marke (WM-Endrunde, Zeile 3 in 0x32AA3 nicht gelesen).</item>
/// <item>[ORIGINAL] Schneller Weg im DEB-Pokal (#925 (b), Spur re-reste Teil 2, <c>docs/analyse/re-reste-23.md</c> §1):
///   0x32AA3 markiert die Zeile 0 vor dem Wurf (0x32AF4, Heimbyte + 30 bei 0x32B9D), 0x3775D setzt die zweite Marke – genau
///   ein Wurf, Endstand h + x : g + y mit zwei Marken; bleibt es gleich (x = y), zählt die Wertung 0x34C76
///   (<c>cmp al,[bx+0x5c4c]; jnc</c>: Heim ≥ Gast → Sieg Platz 2k = Heimverein) das Spiel für den Heimverein. Beleg FILE44
///   D014: Endspiel 4:4 und 3:3 mit je zwei Marken ohne Managerverein.</item>
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
        return FromReport(day, home, away, report, debCup: competition == MatchCompetition.DebCup);
    }

    /// <summary>
    /// Endstand und Marken aus dem Spielbericht: 0 = reguläre Spielzeit (oder Spielwertung), 1 = Penaltyschießen (Endstand
    /// = Gleichstand + Penaltytore), 2 = in der Verlängerung entschieden oder (<paramref name="debCup"/>, schneller Weg
    /// <see cref="MatchReport.FastCupPath"/>) Zusatzwurf des DEB-Pokals; dort zählt ein stehender Gleichstand für den Heimverein
    /// (0x34C76, #925 (b)).
    /// </summary>
    public static (KnockoutMatch Match, int Marks) FromReport(int day, Club home, Club away, MatchReport report,
        bool debCup = false)
    {
        var debFastPath = debCup && report.FastCupPath;
        var (hg, ag) = (report.HomeGoals, report.AwayGoals);
        if (report.Forfeit || hg != ag)
        {
            var marks = !report.Forfeit && report.Overtime ? 2 : 0;
            return (new KnockoutMatch(day, home.Index, away.Index, hg, ag), marks);
        }
        if (report.ShootoutGoals is { } goals && goals.Home != goals.Away)
        {
            var winner = goals.Home > goals.Away ? home.Index : away.Index;
            return (new KnockoutMatch(day, home.Index, away.Index, hg + goals.Home, ag + goals.Away, winner), debFastPath ? 2 : 1);
        }
        // Beide Wege liefern bei Gleichstand immer Penaltytore; bleibt nur der Sieger, gilt er mit Gleichstand.
        // #945 (Review #944 P3-1): [ORIGINAL] Bleibt der Gleichstand nach dem einen Zusatzwurf der Play-offs (x = y), stehen die
        // Zusatztore im Ergebnisbyte (Heim + x + 30, Gast + y, Datei 0x3775D–0x3779F); das Spiel zählt für den Gast (0x3166E).
        // #925 (b): im DEB-Pokal zählt der stehende Gleichstand des schnellen Wegs für den Heimverein (0x34C76 jnc), zwei Marken.
        var fallback = report.ShootoutHomeWins is false && !debFastPath ? away.Index : home.Index;
        var (extraHome, extraAway) = report.ShootoutGoals ?? (0, 0);
        return (new KnockoutMatch(day, home.Index, away.Index, hg + extraHome, ag + extraAway, fallback), debFastPath ? 2 : 1);
    }
}
