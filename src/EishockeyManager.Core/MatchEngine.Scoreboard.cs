using System.Runtime.CompilerServices;

namespace EishockeyManager.Core;

/// <summary>Eine Torchance einer Anzeigereihe: Minute (1 … 70), Seite (0 Heim, 1 Gast), Tor oder vergeben.</summary>
public readonly record struct ScoreboardChance(int Minute, int Side, bool Goal);

/// <summary>Eine Strafe einer Anzeigereihe: Minute, Seite und Minuten (2 oder 5).</summary>
public readonly record struct ScoreboardPenalty(int Minute, int Side, int Minutes);

/// <summary>
/// Eine Reihe der Live-Anzeige (Spur livespiel-kern, Teil A1; <c>docs/analyse/re-livespiel.md</c> §2): eine Paarung mit
/// ihren Drittelzählern.
/// <list type="bullet">
/// <item>[ORIGINAL] Chancen je Drittel und Seite DGROUP 0xA839 + 14p + 2k + s: die Torszene 0x53C57 zählt bei Art 0
///   (vergeben) und Art 1 (Tor) hoch (Datei 0x53CF6), Tore DGROUP 0xA83F + 14p + 2k + s nur bei Art 1 (Datei 0x53D1D);
///   beides nur bis Minute 60 (Datei 0x53CCE) und nur für eine Reihe p &lt; 4 (Datei 0x53CC8). Drittel k = DGROUP 0xA838,
///   vom Spieltagsablauf vor den Dritteln auf 0, 1, 2 gesetzt (Datei 0x37023, 0x3719D, 0x371C4). Bytezähler.</item>
/// <item>[ORIGINAL] Strafminuten im Spiel DGROUP 0xA845 + 14p + s: die Strafe 0x35818 addiert die Belegung des
///   Strafbankplatzes &amp; 7 (2 bzw. 13 &amp; 7 = 5) für jede Reihe, deren Verein in DGROUP 0x233 steht (Datei
///   0x3598A–0x359EE), vor der Spielerwahl – also wie die Strafminuten des Vereins
///   (<see cref="LiveTeam.PenaltyMinutes"/>). Bytezähler.</item>
/// </list>
/// Die Zähler werden je Spieltagsablauf auf 0 gelöscht (Datei 0x36BCC–0x36BDB: 0x54 Byte ab DGROUP 0xA839).
/// </summary>
public sealed class LiveScoreboardRow
{
    /// <summary>[ORIGINAL] Datei 0x53CCE: Chancen und Tore zählen nur bis Minute 60 (keine Verlängerung).</summary>
    public const int LastCountedMinute = 60;

    /// <summary>Minuten je Drittel (Drittel k = DGROUP 0xA838, Minuten 1–20, 21–40, 41–60).</summary>
    public const int PeriodLength = 20;

    private readonly List<ScoreboardChance> chances = [];
    private readonly List<ScoreboardPenalty> penalties = [];
    private readonly int[] penaltyMinutes = new int[2];

    internal LiveScoreboardRow(int homeClub, int awayClub)
    {
        HomeClub = homeClub;
        AwayClub = awayClub;
    }

    /// <summary>Heimverein (DGROUP 0x233 + 2p).</summary>
    public int HomeClub { get; }

    /// <summary>Gastverein (DGROUP 0x234 + 2p).</summary>
    public int AwayClub { get; }

    /// <summary>Alle Chancen der Reihe in Spielreihenfolge (auch die nicht gezählten nach Minute 60).</summary>
    public IReadOnlyList<ScoreboardChance> ChanceList => chances;

    /// <summary>
    /// Strafen der Reihe aus der Ereignisliste (für die Anzeige Minute für Minute). [APPROX] Eine Strafe ohne wählbaren
    /// Spieler (0x378C5 liefert keinen, Rückgabe ohne Ereignis, <c>OriginalMinutes.Penalty</c>) zählt im Original schon in
    /// 0xA845 mit; sie steht nur in <see cref="PenaltyMinutes(int)"/>, nicht hier.
    /// </summary>
    public IReadOnlyList<ScoreboardPenalty> PenaltyList => penalties;

    /// <summary>Ist das Spiel dieser Reihe fertig gerechnet?</summary>
    public bool Finished { get; private set; }

    /// <summary>
    /// Chancen im Drittel <paramref name="period"/> (0–2) für Seite <paramref name="side"/> bis einschließlich Minute
    /// <paramref name="upToMinute"/>; wie das Byte DGROUP 0xA839 + 14p + 2k + s (Anzeige begrenzt erst auf 9, Datei 0x52E8A).
    /// </summary>
    public int Chances(int period, int side, int upToMinute = LastCountedMinute) =>
        Count(period, side, upToMinute, goalsOnly: false);

    /// <summary>Tore im Drittel <paramref name="period"/> für Seite <paramref name="side"/> (DGROUP 0xA83F + 14p + 2k + s).</summary>
    public int Goals(int period, int side, int upToMinute = LastCountedMinute) =>
        Count(period, side, upToMinute, goalsOnly: true);

    /// <summary>Strafminuten der Seite im ganzen Spiel (DGROUP 0xA845 + 14p + s, Byte).</summary>
    public int PenaltyMinutes(int side) => penaltyMinutes[side] & 0xFF;

    /// <summary>Strafminuten der Seite bis einschließlich Minute <paramref name="upToMinute"/> (aus <see cref="PenaltyList"/>).</summary>
    public int PenaltyMinutes(int side, int upToMinute) =>
        penalties.Where(p => p.Side == side && p.Minute <= upToMinute).Sum(p => p.Minutes) & 0xFF;

    /// <summary>Drittel (0–2) einer Minute bis 60; Minuten danach haben kein gezähltes Drittel (−1).</summary>
    public static int PeriodOf(int minute) => minute is >= 1 and <= LastCountedMinute ? (minute - 1) / PeriodLength : -1;

    private int Count(int period, int side, int upToMinute, bool goalsOnly) =>
        chances.Count(c => c.Side == side && (!goalsOnly || c.Goal) && c.Minute <= upToMinute && PeriodOf(c.Minute) == period)
        & 0xFF;

    internal void AddChance(int minute, int side, bool goal) => chances.Add(new ScoreboardChance(minute, side, goal));

    internal void Finish(MatchReport report)
    {
        penaltyMinutes[0] = report.HomePenaltyMinutes;
        penaltyMinutes[1] = report.AwayPenaltyMinutes;
        foreach (var e in report.Events)
        {
            if (e.Kind == MatchEventKind.Penalty)
                penalties.Add(new ScoreboardPenalty(e.Minute, e.Side, e.PenaltyMinutes));
        }
        Finished = true;
    }
}

/// <summary>
/// Reihenliste der Live-Anzeige eines Spieltagsablaufs (Spur livespiel-kern, Teil A1).
/// <list type="bullet">
/// <item>[ORIGINAL] Der Spieltagsablauf 0x36B5D löscht vor den Spielen die Reihenliste (DGROUP 0xA75C + p = 0, 0x233 + 2p =
///   0xFF, 0xA88D + p = 0xFF, Datei 0x36B8E–0x36BC6), die Zahl der Reihen DGROUP 0xA762 = 0 und die Drittelzähler.</item>
/// <item>[ORIGINAL] Die Spielabrechnung 0x544C0 (Aufruf je Paarung Datei 0x36DBD bzw. 0x36F9C, vor dem Spiel) hängt eine
///   Reihe an, wenn Heim- oder Gastverein zum Zusehen markiert ist (Vereinsfeld 0x34 Bit 7, Datei 0x54BF7–0x54C23), und
///   setzt dann DGROUP 0x5A56 = 1 (Live-Anzeige an diesem Spieltag, Datei 0x54C63–0x54C94). Eine Spielwertung 0:2/2:0 kehrt
///   vorher zurück (Datei 0x54599, 0x547BA) und bekommt keine Reihe. Die Reihenfolge der Reihen ist also die der Paarungen
///   im Spieltagsablauf; 0x5353C zeichnet sie in dieser Reihenfolge (y = 4 + 45p, Datei 0x536A9).</item>
/// <item>[APPROX] Zum Zusehen markiert sind im Remake genau die Vereine mit Manager und das Land des Bundestrainers
///   (<see cref="MatchEngine.IsManaged"/>). Im Original markiert der Spielstart 0x28165 (Aufrufe Datei 0x298EA, 0x29C1C) je Manager
///   den Managerverein (Modus 2, mit Verdrängen), die Ernennung zum Bundestrainer das eigene Land (Datei 0x55457); Klicks im Vereinsfenster (Datei 0x4E46A)
///   und die WM-Option 12 (Datei 0x2D3FD) schalten Vereine um (0xAE2B, höchstens vier im Modus 2). Diese Schalter hat das Remake nicht.</item>
/// <item>[APPROX] Höchstens <see cref="MaxRows"/> Reihen: 0x544C0 prüft die Grenze nicht, die Anzeige (0x233, fünf Wörter mit
///   Endmarke DGROUP 0x23B) und die Zähler (Datei 0x53CC8) kennen nur vier; mehr markierte Paarungen verhindert im Original
///   die Grenze in 0xAE2B.</item>
/// <item>[APPROX] Das Remake hat keinen gemeinsamen Einstieg „Spieltagsablauf“: Eine neue Liste beginnt, wenn Saison, Tag oder
///   Wettbewerb wechseln oder ein Verein einer vorhandenen Reihe noch einmal spielt (je Spieltagsablauf spielt jeder Verein
///   höchstens einmal).</item>
/// <item>Das Penaltyschießen 0x3901B baut die Reihen für seine Anzeige neu auf (Datei 0x3929E–0x39345); das gehört zur Anzeige
///   des Penaltyschießens und ist hier nicht nachgebaut.</item>
/// </list>
/// Die Liste ist Darstellungszustand (DGROUP, nicht im Spielstand) und wird nicht gesichert.
/// </summary>
public sealed class LiveScoreboard
{
    /// <summary>[ORIGINAL] Höchstens vier Anzeigereihen (Datei 0x53CB6, 0x36BB9; 0x233 mit Endmarke 0x23B).</summary>
    public const int MaxRows = 4;

    private readonly List<LiveScoreboardRow> rows = [];

    internal LiveScoreboard(int season, int day, MatchCompetition competition)
    {
        Season = season;
        Day = day;
        Competition = competition;
    }

    public int Season { get; }
    public int Day { get; }
    public MatchCompetition Competition { get; }

    /// <summary>Reihen in der Reihenfolge der Paarungen (DGROUP 0xA75C, Zahl 0xA762).</summary>
    public IReadOnlyList<LiveScoreboardRow> Rows => rows;

    /// <summary>Live-Anzeige an diesem Spieltag (DGROUP 0x5A56): mindestens eine Reihe.</summary>
    public bool Active => rows.Count > 0;

    /// <summary>Reihe eines Vereins (Heim oder Gast) oder null.</summary>
    public LiveScoreboardRow? RowOf(int club) => rows.FirstOrDefault(r => r.HomeClub == club || r.AwayClub == club);

    internal bool Matches(int season, int day, MatchCompetition competition, int home, int away) =>
        Season == season && Day == day && Competition == competition && RowOf(home) is null && RowOf(away) is null;

    internal LiveScoreboardRow? Add(int home, int away)
    {
        if (rows.Count >= MaxRows)
            return null;
        var row = new LiveScoreboardRow(home, away);
        rows.Add(row);
        return row;
    }
}

public static partial class MatchEngine
{
    private static readonly ConditionalWeakTable<GameState, LiveScoreboard> Scoreboards = new();

    /// <summary>
    /// Anschluss in <see cref="PlayWithEvents"/> nach der Prüfung auf Spielwertung: Reihe der Paarung anlegen (siehe
    /// <see cref="LiveScoreboard"/>), null ohne zum Zusehen markierten Verein.
    /// </summary>
    internal static LiveScoreboardRow? ScoreboardRow(GameState game, Club home, Club away, MatchCompetition competition)
    {
        if (!Scoreboards.TryGetValue(game, out var board)
            || !board.Matches(game.Season, game.Day, competition, home.Index, away.Index))
        {
            board = new LiveScoreboard(game.Season, game.Day, competition);
            Scoreboards.AddOrUpdate(game, board);
        }
        if (!IsManaged(game, home) && !IsManaged(game, away))
            return null;
        return board.Add(home.Index, away.Index);
    }

    /// <summary>Spiel mit Reihe: Chancen über <see cref="OriginalMinutes.GoalScene"/> zählen, am Ende Strafminuten übernehmen.</summary>
    internal static MatchReport PlayWithScoreboard(LiveScoreboardRow? row, Func<MatchReport> play)
    {
        var previous = OriginalMinutes.CurrentRow;
        OriginalMinutes.CurrentRow = row;
        try
        {
            var report = play();
            row?.Finish(report);
            return report;
        }
        finally
        {
            OriginalMinutes.CurrentRow = previous;
        }
    }
}

public static partial class OriginalMinutes
{
    /// <summary>Reihe der Live-Anzeige der laufenden Paarung (nur während <see cref="MatchEngine.PlayWithScoreboard"/>).</summary>
    [ThreadStatic]
    internal static LiveScoreboardRow? CurrentRow;

    /// <summary>
    /// [ORIGINAL] Aufruf der Torszene 0x53C57 aus dem Abschluss 0xA484 (Datei 0xAB90) für eine Paarung mit Anzeigereihe, nach
    /// jedem Abschluss mit Art = Tor (1) bzw. vergeben (0), vor dem Torschützen 0x541D9; nicht nach dem Abbruch in der
    /// Verlängerung (0xA642–0xA650). Teil A1: Drittelzähler (<see cref="LiveScoreboardRow.Chances"/>).
    /// </summary>
    internal static void GoalScene(int minute, int side, bool goal) => CurrentRow?.AddChance(minute, side, goal);
}

public sealed partial class GameState
{
    /// <summary>
    /// Reihenliste der Live-Anzeige des letzten Spieltagsablaufs (Spur livespiel-kern, A1); null vor dem ersten Spiel.
    /// Darstellungszustand, nicht im Spielstand.
    /// </summary>
    public LiveScoreboard? LiveScoreboard => MatchEngine.CurrentScoreboard(this);
}

public static partial class MatchEngine
{
    internal static LiveScoreboard? CurrentScoreboard(GameState game) =>
        Scoreboards.TryGetValue(game, out var board) ? board : null;
}
