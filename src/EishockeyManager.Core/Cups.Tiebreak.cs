namespace EishockeyManager.Core;

/// <summary>
/// K.-o.-Spiel der Pokale (DEB-Pokal, WM-Endrunde) mit Endstand und Ergebnismarken wie im Original (Spur wettbewerbe-spiel,
/// Teil 1; Inventur P2 Nr. 3, A003, A032). Fundstellen: <c>docs/spielberechnung.md</c> §3 und §19.
/// </summary>
/// <remarks>
/// Das Original kodiert den Ausgang im Ergebnisbyte des Heimvereins (Zeilen DGROUP 0x5C4B): + 30 je Marke, die Anzeige
/// 0x32E23 zieht je 30 ab und setzt dafür einen Punkt hinter das Ergebnis (Datei 0x338AB–0x338E3, höchstens zwei).
/// <list type="bullet">
/// <item>[ORIGINAL] Schneller Weg, Gleichstand (WM-Endrunde, C07/A925): 0x32AA3 markiert Zeile 3 vor dem Zusatzwurf (Datei
///   0x376E1 → 0x32C8E–0x32D84, Heim + 30), dann Heim = h + random(0, 2) + 30, Gast = g + random(0, 2), bis ungleich (Datei
///   0x376F0–0x377A3, zweite Marke 0x3775D) – Endstand mit zwei Marken (Lesart re-abweichungen C07).</item>
/// <item>[ORIGINAL] Schneller Weg im DEB-Pokal (#925 (b), Spur re-reste Teil 2, <c>docs/analyse/re-reste-23.md</c> §1):
///   0x32AA3 markiert die Zeile 0 vor dem Wurf (0x32AF4, Heimbyte + 30 bei 0x32B9D), 0x3775D setzt die zweite Marke – genau
///   ein Wurf, Endstand h + x : g + y mit zwei Marken; bleibt es gleich (x = y), zählt die Wertung 0x34C76
///   (<c>cmp al,[bx+0x5c4c]; jnc</c>: Heim ≥ Gast → Sieg Platz 2k = Heimverein) das Spiel für den Heimverein. Beleg FILE44
///   D014: Endspiel 4:4 und 3:3 mit je zwei Marken ohne Managerverein.</item>
/// <item>[ORIGINAL] Schneller Weg im DEB-Pokal, ungleiches Ergebnis mit offener Paarung dahinter (RR-01, Review #965,
///   <see cref="MatchEngine.DebCupExtraRoll"/>): 0x32AA3 meldet die offene Paarung (0:0 → 30:0 → 60:0, 0x32B23/0x32B3F) als
///   Gleichstand, 0x376F0 würfelt bis Heim + x ≠ Gast + y (0x37724–0x37748), 0x3775D eine Marke; Sieger nach der Summe (0x34C76).
///   Nur die ersten beiden gerechneten Paarungen eines Termins (60:0 erkennt die Markierung nicht mehr).</item>
/// <item>[ORIGINAL] Minutenweg (C07, A098/A482; Beleg <c>c07_marken.py</c>, re-abweichungen C07): 0x32AA3 markiert nach Minute 60 bei
///   Gleichstand (Heim + 30, DEB Datei 0x32B9D, Zeilen 2/3 Datei 0x32D72). Die Verlängerung liest den Stand im Pokalzweig (Datei
///   0x380D0 ff.) und zieht die Marken ab (Datei 0x381FF–0x3824B); ein Tor schreibt 0x36AAA(Stand ohne Marken + Tor, Heim + 30)
///   (Datei 0x38402) – Tor in der Verlängerung: <b>eine</b> Marke. Bleibt es gleich, setzt 0x32AA3 (Schalter 1) die zweite Marke,
///   das Penaltyschießen 0x3901B nimmt eine ab (0x39088) und schreibt Heim = Stand mit einer Marke + Treffer + 30 (Datei
///   0x39770–0x397B3) – Penaltyschießen: <b>zwei</b> Marken. In DEB-Pokal, Play-offs und WM-Endrunde gleich. Vorher umgekehrt
///   (zwei nach einem Verlängerungstor nach dem Videobild eisbaeren_f44_01330, das nicht widerspricht: „4: 3..“ stammt dort aus
///   einem Penaltyschießen bzw. dem schnellen Weg).</item>
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
    /// Endstand und Marken aus dem Spielbericht (C07): 0 = reguläre Spielzeit (oder Spielwertung), 1 = in der Verlängerung
    /// entschieden (Minutenweg), 2 = Penaltyschießen (Endstand = Gleichstand + Penaltytore) bzw. Zusatzwurf des schnellen Wegs
    /// (<see cref="MatchReport.FastCupPath"/>) bei eigenem Gleichstand; im DEB-Pokal (<paramref name="debCup"/>) zählt dort ein
    /// stehender Gleichstand für den Heimverein (0x34C76, #925 (b)). Zusatzwurf des DEB-Pokals bei ungleichem Ergebnis (RR-01,
    /// <see cref="MatchEngine.DebCupExtraRoll"/>): Endstand = Ergebnis + Wurf, eine Marke, Sieger nach der Summe.
    /// </summary>
    public static (KnockoutMatch Match, int Marks) FromReport(int day, Club home, Club away, MatchReport report,
        bool debCup = false)
    {
        var debFastPath = debCup && report.FastCupPath;
        var (hg, ag) = (report.HomeGoals, report.AwayGoals);
        if (debFastPath && !report.Forfeit && hg != ag && report.ShootoutGoals is { } roll)
        {
            // RR-01 (Review #965): [ORIGINAL] Zusatzwurf für eine ungleiche Paarung, hinter der am Termin noch eine offene steht
            // (0x32AA3 meldet 0:0 bzw. 30:0 als Gleichstand, 0x376F0 würfelt bis Heim + x ≠ Gast + y, 0x3775D eine Marke); der
            // Sieger folgt der Summe (0x34C76 nach Abzug der Marke, 0x34C06–0x34C2A) und kann kippen.
            var (sumHome, sumAway) = (hg + roll.Home, ag + roll.Away);
            var rollWinner = sumHome > sumAway ? home.Index : away.Index;
            return (new KnockoutMatch(day, home.Index, away.Index, sumHome, sumAway, rollWinner), 1);
        }
        if (report.Forfeit || hg != ag)
        {
            var marks = !report.Forfeit && report.Overtime ? 1 : 0; // C07: [ORIGINAL] Verlängerungstor eine Marke (Datei 0x38402)
            return (new KnockoutMatch(day, home.Index, away.Index, hg, ag), marks);
        }
        if (report.ShootoutGoals is { } goals && goals.Home != goals.Away)
        {
            var winner = goals.Home > goals.Away ? home.Index : away.Index;
            // C07: [ORIGINAL] Penaltyschießen (0x32AA3 + 0x3901B) und Zusatzwurf des schnellen Wegs (0x32AA3 + 0x3775D) zwei Marken.
            return (new KnockoutMatch(day, home.Index, away.Index, hg + goals.Home, ag + goals.Away, winner), MaxMarks);
        }
        // Beide Wege liefern bei Gleichstand immer Penaltytore; bleibt nur der Sieger, gilt er mit Gleichstand.
        // #945 (Review #944 P3-1): [ORIGINAL] Bleibt der Gleichstand nach dem einen Zusatzwurf der Play-offs (x = y), stehen die
        // Zusatztore im Ergebnisbyte (Heim + x + 30, Gast + y, Datei 0x3775D–0x3779F); das Spiel zählt für den Gast (0x3166E).
        // #925 (b): im DEB-Pokal zählt der stehende Gleichstand des schnellen Wegs für den Heimverein (0x34C76 jnc), zwei Marken.
        var fallback = report.ShootoutHomeWins is false && !debFastPath ? away.Index : home.Index;
        var (extraHome, extraAway) = report.ShootoutGoals ?? (0, 0);
        return (new KnockoutMatch(day, home.Index, away.Index, hg + extraHome, ag + extraAway, fallback), MaxMarks);
    }
}
