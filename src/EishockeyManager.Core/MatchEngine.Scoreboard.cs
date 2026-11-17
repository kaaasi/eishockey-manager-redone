using System.Runtime.CompilerServices;

namespace EishockeyManager.Core;

/// <summary>Eine Torchance einer Anzeigereihe: Minute (1 … 70), Seite (0 Heim, 1 Gast), Tor oder vergeben.</summary>
public readonly record struct ScoreboardChance(int Minute, int Side, bool Goal);

/// <summary>
/// Tormeldung einer Paarung ohne Anzeigereihe (Spur zusehen-rest, A832): Minute, Vereine und Spielstand nach dem Tor.
/// </summary>
public readonly record struct GoalNotice(int Minute, int HomeClub, int AwayClub, int HomeGoals, int AwayGoals);

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
public sealed partial class LiveScoreboardRow
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
/// <item>[ORIGINAL] Zum Zusehen markiert (Bit 7) ist ein Verein im Modus 2 der Markierung <see cref="Club.WatchMode"/>
///   (Schalter 0x0AE2B, <see cref="GameState.SetWatchMode"/>): Spielstart und neuer Manager markieren den Managerverein, die
///   Ernennung zum Bundestrainer das eigene Land, der dritte Knopf des Vereinsfensters schaltet um (Spur zusehen, Teil 1).</item>
/// <item>[ORIGINAL] A840 (Spur zusehen-rest, Teil 2): Die Regeln der Managerpaarung (<see cref="MatchRules.ManagerMatch"/>:
///   Energie 0x87F7, Seitentausch, 0x80CB, Reihenwechsel 0x8EF2, Taktik 0x939A, Eingriff, Torschützen 0x541D9) hängen an
///   der Reihe: 0x52B91 sucht den Verein in den fünf Paaren 0x233/0x234 (Merker [bp-0xF] Datei 0x37E7B Liga, 0x38277
///   Pokal). Ein markierter Verein ohne Manager bekommt also mit der Reihe auch die Managerregeln (beide Vereine der
///   Paarung), ein Managerverein ohne Reihe (Markierung aus, mehr als vier Reihen) spielt mit den Vereinsfeldern (0x977F
///   Datei 0x097E6) und behält nur, was an Vereinsfeld 0x16 hängt: Verletzungen 0x38B8F (Datei 0x37ECF), Strafspieler
///   0x378C5 (dort ohne Reihe aus dem ganzen Kader, Datei 0x379A3) und die Neuzählung 0x38A36.</item>
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
    private readonly List<GoalNotice> notices = [];

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

    /// <summary>
    /// Tormeldungen der markierten Paarungen ohne Reihe in der Reihenfolge der Tore (Spur zusehen-rest, A832; siehe
    /// <see cref="MatchEngine.NoticeTarget"/>); gelten nur mit <see cref="Active"/>. Darstellungszustand wie die Reihen.
    /// </summary>
    public IReadOnlyList<GoalNotice> Notices => notices;

    internal void AddNotice(GoalNotice notice) => notices.Add(notice);

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
        if (!WatchedLive(game, home) && !WatchedLive(game, away))
            return null;
        return board.Add(home.Index, away.Index);
    }

    /// <summary>
    /// Markierung zum Zusehen (Vereinsfeld 0x34 Bit 7) des Vereinssatzes. Länder spielen im Remake mit einem eigenen
    /// Vereinsobjekt ohne Kader (<c>GameState.RatedNation</c>); die Markierung steht am Vereinsplatz 52–63 in
    /// <see cref="GameState.Clubs"/> (A837: [ORIGINAL] 0x544C0 liest Feld 0x34 des Satzes, Datei 0x54BF7–0x54C23).
    /// </summary>
    internal static bool WatchedLive(GameState game, Club club) => Watched(game, club).WatchedLive;

    private static Club Watched(GameState game, Club club) =>
        club.Index is >= NationalTeams.First and <= NationalTeams.Last && club.Index < game.Clubs.Count
            ? game.Clubs[club.Index]
            : club;

    /// <summary>
    /// Spiel mit Anzeigezustand: Chancen über <see cref="OriginalMinutes.GoalScene"/> zählen, Züge der Anzeige nach R18-D3
    /// (<c>MatchEngine.GoalScene.cs</c>), am Ende Strafminuten übernehmen.
    /// </summary>
    internal static MatchReport PlayWithScoreboard(LiveScoreboardRow? row, Func<MatchReport> play, GameOptions? options = null,
        bool dayActive = false, GoalNoticeTarget? notice = null)
    {
        var previous = OriginalMinutes.Display;
        OriginalMinutes.Display = new LiveDisplay(row, dayActive || row is not null, options, notice);
        try
        {
            var report = play();
            row?.Finish(report);
            return report;
        }
        finally
        {
            OriginalMinutes.Display = previous;
        }
    }
}

/// <summary>
/// Anzeigezustand der laufenden Paarung für den Spielkern: Reihe (oder null), Live-Anzeige am Spieltag (DGROUP 0x5A56),
/// die Optionen (Geschwindigkeit DGROUP 0x262, Darstellung 0x226) und das Ziel der Tormeldung (A832, sonst null).
/// </summary>
internal sealed record LiveDisplay(LiveScoreboardRow? Row, bool DayActive, GameOptions? Options,
    GoalNoticeTarget? Notice = null);

/// <summary>Reihenliste und Vereine, für die eine Paarung ohne Reihe ihre Tore meldet (A832).</summary>
internal sealed record GoalNoticeTarget(LiveScoreboard Board, int HomeClub, int AwayClub);

public static partial class MatchEngine
{
    /// <summary>
    /// Ziel der Tormeldung einer Paarung (Spur zusehen-rest, A832). [ORIGINAL] Abschluss 0xA484, Datei 0xAB47–0xABF5: Steht die
    /// Paarung nicht in der Reihenliste (Byte [bp−9] = 0x1E), meldet sie ein Tor nur, wenn an diesem Spieltag die Live-Anzeige
    /// läuft (DGROUP 0x5A56 ≠ 0, Datei 0xABB4) und Heim- oder Gastverein zum Zusehen markiert ist (Vereinsfeld 0x34
    /// <c>test 0xC0</c>, Modus ≠ 0, Datei 0xABD7/0xABEE). 0x5A56 setzt die Spielabrechnung 0x544C0, die für alle Paarungen vor
    /// der Minutenschleife läuft (Datei 0x54C63–0x54C94): Es gilt also für den ganzen Ablauf, sobald eine Paarung eine Reihe hat.
    /// [APPROX] Das Remake rechnet die Paarungen nacheinander (<c>MatchEngine.LiveDriver.cs</c>) und weiß bei früheren Paarungen
    /// noch nicht, ob eine Reihe folgt: Es legt die Meldung für jede markierte Paarung ohne Reihe in der Reihenliste des Ablaufs
    /// ab; angezeigt wird sie nur mit Live-Bildschirm, also nur mit Reihe (<see cref="LiveScoreboard.Active"/>). Die Markierung
    /// gilt zu Beginn der Paarung.
    /// </summary>
    internal static GoalNoticeTarget? NoticeTarget(MatchSetup match)
    {
        if (match.Row is not null || (Watched(match.Game, match.Home).WatchMode == 0 && Watched(match.Game, match.Away).WatchMode == 0)
            || CurrentScoreboard(match.Game) is not { } board)
            return null;
        return new GoalNoticeTarget(board, match.Home.Index, match.Away.Index);
    }
}

public static partial class OriginalMinutes
{
    /// <summary>Anzeigezustand der laufenden Paarung (nur während <see cref="MatchEngine.PlayWithScoreboard"/>).</summary>
    [ThreadStatic]
    internal static LiveDisplay? Display;

    /// <summary>Reihe der Live-Anzeige der laufenden Paarung oder null.</summary>
    internal static LiveScoreboardRow? CurrentRow => Display?.Row;
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
