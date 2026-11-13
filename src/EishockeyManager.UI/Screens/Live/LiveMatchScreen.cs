using System.Runtime.CompilerServices;
using EishockeyManager.Core;
using EishockeyManager.UI.Screens.Competitions;

namespace EishockeyManager.UI.Screens.Live;

/// <summary>
/// Live-Bildschirm (Spur livespiel-ui-2, B2; L2/L4/L5; <c>docs/analyse/re-livespiel.md</c> §1, §4, §5, §15): Aufbau 0x5353C,
/// Anzeigetafel 0x52BD6 je Reihe, Spieluhr 0x51E90, Lampe und Reporter 0x52520. Der Bildschirm treibt die Minuten nur über
/// den Minutenschritt des Spielkerns (<see cref="LiveSession.Step"/> → <see cref="LiveMatchDay.Step"/>); Zeit vergeht damit
/// innerhalb von <see cref="GameState.Continue"/>, das auf dem Thread der Sitzung wartet.
/// <list type="bullet">
/// <item>[ORIGINAL] Ablauf je Minute (Datei 0x37C6B–0x37CBD, <see cref="LiveClock.Minute"/>): Uhr auf die Minute, Zwinkern
///   und Pause, dann die Rechnung der Minute (Reporter-Meldungen der Chancen und Strafen), am Minutenende die Anzeigetafel.
///   Nach Minute 60 bzw. 70 unter Geschwindigkeit 70 auf einen Mausklick warten (Datei 0x3855F–0x38587).</item>
/// <item>[APPROX] Pause n ≤ 0 (Geschwindigkeit 62 … 69 mit Zwinkern: 30 − 31 … 30 − 34, #823) = keine Pause:
///   <c>50h:19Eh</c> teilt n (Langwort mit Vorzeichen, 0x227C) durch 3 und wartet auf den Tickzähler (0xBBF:0x2C08); ein
///   negativer Wert wäre im Original eine sehr lange Wartezeit oder sofort vorbei – nicht nachgeprüft, die Geschwindigkeit
///   steht im Original ohnehin nur auf Zehnern (Regler 0x2CD1C, <see cref="GameOptions.SetMatchSpeed"/>), 62 … 69 kommen
///   also nicht vor.</item>
/// <item>[APPROX] Enter oder Escape rechnen die restlichen Minuten ohne Anzeige (wie die Vorspultaste in
///   <see cref="MatchDayScreen"/>, Entscheidung Nutzer 31.10.2026, #205); die Leertaste (Spielmodus-Optionen 0x2CD1C) ist
///   nicht angeschlossen.</item>
/// <item>[APPROX] Reihen: die Paarungen mit Anzeigereihe des Spieltags, die schon begonnen haben (<see cref="LiveSession.Shown"/>);
///   das Remake rechnet sie nacheinander (<c>MatchEngine.LiveDriver.cs</c>), spätere Managerpaarungen erscheinen erst mit
///   ihrem Spiel.</item>
/// <item>[APPROX] Zuschauerzahl: Das Remake rechnet die Zuschauer erst nach dem Spieltag (Finanzen); im Heimspiel steht die
///   Zeile deshalb leer, sonst „(ausw.)“ wie im Original ohne Heimspiel bzw. in der WM (Datei 0x53A11–0x53B37).</item>
/// </list>
/// </summary>
public sealed class LiveMatchScreen : Screen
{
    /// <summary>Hintergrund hinter den Reihen ([ORIGINAL] <c>110h:7Ah</c>, Datei 0x535C4: kb\22).</summary>
    public const int BackgroundGraphic = 22;

    /// <summary>Kästen und Reihenhintergrund ([ORIGINAL] kb\19, Datei 0x536AA, 0x535F9, 0x53627, 0x53658, 0x53B5E).</summary>
    public const int FrameGraphic = 19;

    public static readonly Rect RowSource = new(0, 0, 313, 44);
    public static readonly Rect ClockBoxSource = new(0, 45, 122, 38);
    public static readonly Rect ReporterBoxSource = new(190, 60, 122, 38);
    public static readonly Rect ReporterSource = new(123, 45, 63, 53);
    public static readonly Rect LampFieldSource = new(187, 45, 21, 12);

    public static readonly (int X, int Y) ClockBox = (3, 182);
    public static readonly (int X, int Y) ReporterBox = (194, 180);
    public static readonly (int X, int Y) Reporter = (129, 187);
    public static readonly (int X, int Y) LampField = (3, 226);

    /// <summary>[ORIGINAL] Lampe „Schnelldurchlauf“ bei (10 | 228) (Datei 0x53B6B, 0x38E0C).</summary>
    public static readonly (int X, int Y) FastLamp = (10, 228);

    /// <summary>[ORIGINAL] Reihe p ab y = 4 + 45p (Datei 0x536A9), Anzeigetafel y0 = 15 + 45p, x0 = 13 bzw. 240 (Datei 0x52C3A).</summary>
    public const int RowStep = 45;

    public static int RowY(int p) => 4 + RowStep * p;

    public static int BoardY(int p) => 15 + RowStep * p;

    public static int BoardX(int side) => side == 0 ? 13 : 240;

    /// <summary>[ORIGINAL] Kopf bzw. Logo bei (96 | y) und (182 | y), 38 × 38 (Datei 0x53749, 0x538F4).</summary>
    public static int CrestX(int side) => side == 0 ? 96 : 182;

    private static readonly ConditionalWeakTable<GameState, LRandom> WinkRandoms = new();

    private readonly LiveSession _session;
    private readonly Screen _back;
    private readonly LiveClock _clock;
    private readonly LiveReporter _reporter;
    private IEnumerator<bool>? _script;
    private InputState _input = new();
    private bool _skip;

    public LiveMatchScreen(ScreenHost host, LiveSession session, Screen back) : base(host)
    {
        _session = session;
        _back = back;
        // [APPROX] lrandom lebt im Original über das ganze Programm (DGROUP 0xE017); hier je Spielstand (Darstellungszustand).
        var wink = WinkRandoms.GetValue(host.Game!, _ => new LRandom());
        _clock = new LiveClock(host.Game!.Options, wink);
        _reporter = new LiveReporter(host.Game!.Options, wink);
    }

    private GameState Game => Host.Game!;

    /// <summary>Angezeigte Minute (DGROUP 0xE19E).</summary>
    public int DisplayMinute { get; private set; }

    /// <summary>Wartet der Bildschirm nach Minute 60/70 auf einen Klick?</summary>
    public bool WaitingForClick { get; private set; }

    /// <summary>Ist das Spiel gerechnet und der Bildschirm fertig?</summary>
    public bool Done { get; private set; }

    public LiveClock Clock => _clock;

    public LiveReporter ReporterState => _reporter;

    public override void Enter() => _script ??= Script().GetEnumerator();

    public override void Update(InputState input)
    {
        _input = input;
        if (Done)
            return;
        if (input.Pressed(Key.Enter) || input.Pressed(Key.Escape))
            _skip = true;
        _script ??= Script().GetEnumerator();
        if (_skip || !_script.MoveNext())
            Finish();
    }

    private void Finish()
    {
        if (Done)
            return;
        Done = true;
        WaitingForClick = false;
        // [APPROX] Enter/Escape: restliche Minuten ohne Anzeige (siehe Klasse).
        while (_session.Step())
            DisplayMinute = _session.Day?.Minute ?? DisplayMinute;
        _clock.EndMatchDay();
        _session.Release();
        Host.Show(_back);
    }

    private IEnumerable<bool> Script()
    {
        _clock.BeginMatchDay();
        yield return true; // [ORIGINAL] 0x5353C: Bild steht (einblenden)
        while (_session.Day is { } day && day.Running && day.Minute < LiveMatchDay.LastMinute)
        {
            var minute = day.Minute + 1;
            DisplayMinute = minute;
            var info = _clock.Minute(display: true);
            if (info.Wink)
                foreach (var f in _reporter.Wink())
                    yield return f;
            for (var i = 0; i < LiveReporter.PauseFrames(info.PauseUnits); i++)
                yield return true;
            if (!_session.Step())
                break;
            foreach (var message in Messages(day.Matches[0], minute))
                foreach (var f in message)
                    yield return f;
            yield return true; // [ORIGINAL] 0x3854A: Anzeigetafel am Minutenende
            if (minute is 60 or 70 && _clock.WaitsForClick(minute, display: true))
            {
                WaitingForClick = true;
                yield return true;
                while (!_input.LeftClick && !_input.RightClick)
                    yield return true;
                WaitingForClick = false;
            }
        }
    }

    /// <summary>
    /// Meldungen einer Minute (§5): Chancen der Reihe (Torszene 0x53C57 → 0x52520 Ereignis 0/1), Strafen (0x35818 → Ereignis
    /// 2/3 mit Strafgrund aus dem Kern), Verletzungen (Ereignis 4). [APPROX] Reihenfolge innerhalb der Minute: Chancen, Strafen,
    /// Verletzungen; vergebene Chancen ohne Schützen (der Kern führt ihn nicht).
    /// </summary>
    private IEnumerable<IEnumerable<bool>> Messages(LiveMatch match, int minute)
    {
        if (match.Row is not { } row)
            yield break;
        var events = match.Report.Events.Where(e => e.Minute == minute).ToList();
        var goals = new Queue<MatchEvent>(events.Where(e => e.Kind == MatchEventKind.Goal));
        foreach (var c in row.ChanceList.Where(c => c.Minute == minute))
        {
            MatchEvent? goal = null;
            if (c.Goal && goals.Count > 0)
                goal = goals.Dequeue();
            yield return _reporter.Message(ClubName(match, c.Side), c.Goal ? ReporterEvent.Goal : ReporterEvent.ChanceMissed,
                goal?.Player?.Name, goal?.Assist?.Name, null);
        }
        var reasons = new Queue<ScoreboardPenaltyReason>(row.PenaltyReasons.Where(r => r.Minute == minute));
        foreach (var e in events.Where(e => e.Kind == MatchEventKind.Penalty))
        {
            var reason = reasons.Count > 0 ? reasons.Dequeue().Reason : GoalSceneSelection.MajorPenaltyReason;
            yield return _reporter.Message(ClubName(match, e.Side), e.PenaltyMinutes == 5 ? ReporterEvent.Major : ReporterEvent.Minor,
                e.Player?.Name, null, GoalSceneSelection.Reasons[reason]);
        }
        foreach (var e in events.Where(e => e.Kind == MatchEventKind.Injury))
            yield return _reporter.Message(ClubName(match, e.Side), ReporterEvent.Injury, e.Player?.Name, null, null);
    }

    private string ClubName(LiveMatch match, int side) => (side == 0 ? match.Home : match.Away).Name;

    public override void Draw(Surface s)
    {
        s.Palette = Assets.Palette;
        DrawBuild(s, Assets, Game, _session.Shown, _clock.Lamp);
        for (var p = 0; p < Math.Min(_session.Shown.Count, LiveScoreboard.MaxRows); p++)
            DrawBoard(s, Assets, _session.Shown[p], p, MinuteOf(_session.Shown[p]));
        LiveWidgets.Clock(s, Assets, 0, DisplayMinute);
        DrawReporter(s, Assets, _reporter);
    }

    private int MinuteOf(LiveMatch match) => match.Running ? DisplayMinute : match.LastMinute;

    /// <summary>[ORIGINAL] Aufbau 0x5353C (§4): Hintergrund, Kästen, je Reihe Hintergrund, Köpfe bzw. Logos, Namen, Zuschauer, Lampe.</summary>
    public static void DrawBuild(Surface s, Assets a, GameState game, IReadOnlyList<LiveMatch> rows, bool lamp)
    {
        s.Blit(a.Image(BackgroundGraphic), 0, 0);
        var frame = a.Image(FrameGraphic);
        s.Blit(frame, ClockBoxSource, ClockBox.X, ClockBox.Y);
        s.Blit(frame, ReporterBoxSource, ReporterBox.X, ReporterBox.Y);
        s.Blit(frame, ReporterSource, Reporter.X, Reporter.Y, transparent: 0); // [APPROX] Durchsichtigkeit aus dem Videobild (Farbe 0)
        for (var p = 0; p < Math.Min(rows.Count, LiveScoreboard.MaxRows); p++)
        {
            var y = RowY(p);
            s.Blit(frame, RowSource, 3, y - 3);
            var match = rows[p];
            for (var side = 0; side < 2; side++)
            {
                var club = side == 0 ? match.Home : match.Away;
                if (game.ActiveManagers.FirstOrDefault(m => m.ClubIndex == club.Index) is { } manager)
                    s.Blit(a.Image(0), new Rect(manager.Head % 3 * 41, manager.Head / 3 * 41, 38, 38), CrestX(side), y);
                else
                    Widgets.Logo(s, a, club.LogoGraphic, CrestX(side), y);
                // [ORIGINAL] Datei 0x53958–0x539A6: Schrift 3, Farbe 7, Schatten 0, zentriert 5–96 bzw. 224–317, Grundlinie y + 4.
                var (x1, x2) = side == 0 ? (5, 96) : (224, 317);
                CompetitionWidgets.Centered(s, a.Mini, x1, x2, y + 4, club.Name, Colors.White, 0);
            }
            // [ORIGINAL] Datei 0x53A11–0x53B37: Zuschauer des Heimspiels eines Managers, sonst bzw. in der WM „(ausw.)“.
            if (!MatchEngine.IsManaged(game, match.Home) || match.Competition == MatchCompetition.WorldCup)
                CompetitionWidgets.Centered(s, a.Mini, 48, 82, y + 38, "(ausw.)", 0, null);
        }
        s.Blit(frame, LampFieldSource, LampField.X, LampField.Y);
        LiveWidgets.Lamp(s, a, FastLamp.X, FastLamp.Y, lamp);
    }

    /// <summary>
    /// [ORIGINAL] Anzeigetafel 0x52BD6 einer Reihe (§4): Spielstand 0x51FEA, je Seite Lampen und Energiebalken je Teil, Einsatz
    /// und Angriff, Feldspieler und Strafbank, Chancen je Drittel, Strafminuten; nur Heimseite die Tore je Drittel.
    /// </summary>
    public static void DrawBoard(Surface s, Assets a, LiveMatch match, int p, int minute)
    {
        var report = match.Report;
        // [ORIGINAL] 0x52190(0, Heim, Gast, 139, 45p + 6): Zehner/Einer bei x, x + 9, x + 21, x + 30.
        var sy = 45 * p + 6;
        LiveWidgets.Digit(s, a, 0, report.HomeGoals / 10 % 10, 139, sy);
        LiveWidgets.Digit(s, a, 0, report.HomeGoals % 10, 148, sy);
        LiveWidgets.Digit(s, a, 0, report.AwayGoals / 10 % 10, 160, sy);
        LiveWidgets.Digit(s, a, 0, report.AwayGoals % 10, 169, sy);
        var y0 = BoardY(p);
        var row = match.Row;
        for (var side = 0; side < 2; side++)
        {
            var x0 = BoardX(side);
            if ((side == 0 ? match.HomeTeam : match.AwayTeam) is { } team)
            {
                for (var t = 0; t < 2; t++)
                {
                    var line = team.Line(t);
                    for (var k = 0; k < MatchIntervention.LineCount; k++)
                        LiveWidgets.Lamp(s, a, x0 + 8 + 8 * t, y0 + 7 * k - 1, k == line);
                    // [ORIGINAL] Datei 0x52D3C–0x52D5D: Energie der Reihe auf dem Eis, max(0, (Energie − 50) / 2).
                    LiveWidgets.VerticalBar(s, x0 + 24 * t, y0, 25, Math.Max(0, (team.LineEnergy(t, line) - 50) / 2));
                }
                // [ORIGINAL] Spalte A (Datei 0x52DB2–0x52E4C): Feldspieler, Strafbank Platz 0 und 1 → (139 + 34s | y0 + 6).
                var colA = 139 + 34 * side;
                Column(s, a, colA, y0 + 6, 0, team.Box.Skaters);
                Column(s, a, colA, y0 + 6, 1, team.Box.State & 7);
                Column(s, a, colA, y0 + 6, 2, (team.Box.State >> 4) & 7);
                // [ORIGINAL] Datei 0x53060–0x5308E: Einsatz · 25 / 100 und Angriff · 25 / 7 bei (x0 + 39 | y0 + 9) und (… | y0 + 16).
                LiveWidgets.HorizontalBar(s, x0 + 39, y0 + 9, 25, team.Effort * 25 / 100);
                LiveWidgets.HorizontalBar(s, x0 + 39, y0 + 16, 25, team.AttackLevel * 25 / 7);
            }
            if (row is null)
                continue;
            // [ORIGINAL] Spalte B (Datei 0x52E6E–0x52EC2): Chancen je Drittel, höchstens 9 → (88 + 136s | y0 + 5).
            for (var k = 0; k < 3; k++)
                Column(s, a, 88 + 136 * side, y0 + 5, k, Math.Min(9, row.Chances(k, side, minute)), step: 8);
            // [ORIGINAL] Datei 0x52F2F–0x52F4E: „n min.“ Farbe 0x10, ohne Schatten, zentriert 40–74, Grundlinie 5 → (48 + 227s | y0 − 3).
            var pm = row.Finished ? row.PenaltyMinutes(side) : (side == 0 ? match.HomeTeam : match.AwayTeam)?.PenaltyMinutes ?? 0;
            CompetitionWidgets.Centered(s, a.Mini, 48 + 227 * side, 82 + 227 * side, y0 + 2, $"{pm & 0xFF} min.", 0x10, null);
            if (side == 0)
            {
                // [ORIGINAL] Datei 0x52F77–0x53032: Tore je Drittel „H:G“ (je höchstens 9) → (153 | y0 + 4).
                for (var k = 0; k < 3; k++)
                {
                    var text = $"{Math.Min(9, row.Goals(k, 0, minute))}:{Math.Min(9, row.Goals(k, 1, minute))}";
                    CompetitionWidgets.Centered(s, a.Mini, 153, 0, y0 + 4 + 5 + 8 * k, text, Colors.White, null, left: true);
                }
            }
        }
    }

    /// <summary>Eine Ziffer der Spalten A/B (Hilfsfläche: Schrift 3, Farbe 7, Grundlinie 5 + Schritt · k, linksbündig).</summary>
    private static void Column(Surface s, Assets a, int x, int y, int k, int value, int step = 7) =>
        CompetitionWidgets.Centered(s, a.Mini, x, 0, y + 5 + step * k, $"{value % 10}", Colors.White, null, left: true);

    /// <summary>Reporterkasten (Hilfsfläche nach (199 | 183)), Mund und Augen (§5).</summary>
    public static void DrawReporter(Surface s, Assets a, LiveReporter reporter)
    {
        var box = LiveReporter.Box;
        foreach (var line in reporter.Lines)
        {
            var font = line.Large ? a.Bold : a.Mini;
            CompetitionWidgets.Centered(s, font, box.X + line.X1, box.X + line.X2, box.Y + line.Baseline, line.Text, line.Color,
                line.Shadow);
        }
        var digits = a.Image(LiveWidgets.DigitsGraphic);
        var mouth = reporter.Mouth >= 0 ? LiveReporter.MouthOpenSource(reporter.Mouth) : LiveReporter.MouthClosedSource;
        s.Blit(digits, mouth, LiveReporter.MouthTarget.X, LiveReporter.MouthTarget.Y);
        s.Blit(digits, reporter.EyesClosed ? LiveReporter.EyesClosedSource : LiveReporter.EyesOpenSource,
            LiveReporter.EyesTarget.X, LiveReporter.EyesTarget.Y);
    }
}
