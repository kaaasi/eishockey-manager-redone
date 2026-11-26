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
/// <item>[ORIGINAL] Pause n ≤ 0 (Geschwindigkeit 62 … 69 mit Zwinkern: 30 − 31 … 30 − 34, #823; A448, S05) wie im Original
///   (<see cref="LiveReporter.PauseFrames"/>): <c>50h:19Eh</c> (Datei 0x130EE) teilt n mit Vorzeichen durch 3 (0x0:0x47C) und übergibt das Wort an 0xBBF:0x2C08: Ziel =
///   BIOS-Tickzähler 0040:006C + Wort (vorzeichenlos, <c>adc dx, 0</c>); 0xBBF:0x2C4E meldet fertig, sobald der Zähler das Ziel
///   erreicht. n = −1, −2 ergeben 0 (keine Pause), n = −3, −4 ergeben 0xFFFF Ticks (≈ 1 Stunde). Solche Werte kommen im
///   Original nicht vor: Der Regler setzt nur Zehner (Datei 0x2CD1C, <see cref="GameOptions.SetMatchSpeed"/>), die Lampe 70.</item>
/// <item>[APPROX] Enter oder Escape rechnen die restlichen Minuten ohne Anzeige (wie die Vorspultaste in
///   <see cref="MatchDayScreen"/>, Entscheidung Nutzer 31.10.2026, #205).</item>
/// <item>[ORIGINAL] Leertaste (S05, Datei 0x37CFA–0x37D89): nach Zwinkern und Pause der Minute n die Spielmodus-Optionen
///   0x2CD1C (<see cref="GameOptionsScreen"/>; davor Sicherung zurück, danach neu sichern und Lampe aus,
///   <see cref="LiveClock.BeforeOptions"/>/<see cref="LiveClock.AfterOptions"/>), dann ist Minute n für keine Paarung gerechnet
///   (<see cref="LiveMatchDay.Skip"/>); weiter mit Minute n + 1. Abgefragt nur mit Live-Anzeige, also auch im Schnelldurchlauf
///   (Datei 0x37D02 prüft nur das Anzeigeargument). [APPROX] Die Tafel zeigt in Minute n den Stand von n − 1 weiter (das
///   Original zeichnet nach dem Neuaufbau 0x5353C bis zum Ende von Minute n + 1 keine Tafel; das Remake zeichnet sie je Bild).</item>
/// <item>[ORIGINAL] Reihen: die Paarungen mit Anzeigereihe des Termins (<see cref="LiveSession.Shown"/>), am Ligaspieltag alle von
///   Minute 1 an (S01, <c>MatchEngine.PlayMatchDay</c>), an Pokal-, Play-off- und WM-Terminen ebenso (<c>MatchEngine.PlayDate</c>,
///   Pokalzeilen Datei 0x380D0–0x3853E).</item>
/// <item>[ORIGINAL] Nach jedem Drittel (Minute 20, 40, 60, 65, 70) und nach dem Penaltyschießen die Seiten des Seitenwählers
///   0x3A88A mit Minute und laufenden Ständen (S04, <see cref="LivePages"/>, <see cref="LivePageScreens"/>).</item>
/// <item>[ORIGINAL] Torszene je Chance vor der Reporter-Meldung (0x53C57 → 0x567C8 vor 0x541D9/0x52520, Spur torszenen C3,
///   <see cref="GoalSceneCall"/>): Pause 30, Abspieler <see cref="GoalSceneScreen"/> mit der Szene des Spielkerns, Pause 20,
///   „r“/„w“ wiederholt bis zu dreimal.</item>
/// <item>[ORIGINAL] Darstellung 2 „Dauer-Eisfeld“ (A472, Spur torszene-dauer Teil 2, <c>docs/analyse/torszene-dauer.md</c> §10
///   T1–T8): statt des Live-Bildschirms das Eisfeld für den ganzen Spieltag (<see cref="RinkScript"/>,
///   <see cref="GoalSceneRinkView"/>); Penaltyschießen mit Darstellung 1 und 2 auf eigenem Eis (<see cref="Shootouts"/>).</item>
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
    private bool _space;

    public LiveMatchScreen(ScreenHost host, LiveSession session, Screen back) : base(host)
    {
        _session = session;
        _back = back;
        // [APPROX] lrandom lebt im Original über das ganze Programm (DGROUP 0xE017); hier je Spielstand (Darstellungszustand).
        var wink = WinkRandoms.GetValue(host.Game!, _ => new LRandom());
        _clock = new LiveClock(host.Game!.Options, wink);
        _reporter = new LiveReporter(host.Game!.Options, wink);
        _clock.BeginMatchDay(); // [ORIGINAL] Datei 0x36B65–0x36B77 (vor dem ersten Bild, auch wenn sofort übersprungen wird)
        // A472: [ORIGINAL] Datei 0x3710B: Darstellung 2 beim Beginn des Spieltagsablaufs (Optionen sind dann nicht erreichbar).
        PermanentRink = OriginalMinutes.PermanentRink(host.Game!.Options) && GoalSceneCall.Available(Assets);
        // T1: [ORIGINAL] Aufbau vor dem ersten Drittel, also vor dem ersten Bild (Datei 0x3710B–0x37171).
        if (PermanentRink && session.Day is { } day && GoalSceneRinkView.ForMatchDay(Assets, host.Game!, day) is { } rink)
        {
            _rink = rink;
            RinksBuilt.Add(rink);
        }
    }

    /// <summary>
    /// Läuft der Spieltag auf dem Dauer-Eisfeld (Darstellung 2, A472)? [APPROX] Ohne Szenen-Dateien (<c>tore\</c>) zeigt das
    /// Remake den Live-Bildschirm wie bei Darstellung 1; das Original kennt nur den Rückfall bei zu wenig Speicher.
    /// [APPROX] Der Spielkern (<see cref="OriginalMinutes.PermanentRink"/>) prüft dabei nur die Option: ohne <c>tore\</c> zieht er
    /// trotz Reporter und Eingriffsmenü keinen Strafgrund und setzt den Merker „Manager am Zug“ wie ohne Live-Anzeige; das Original
    /// setzt beim gescheiterten Aufbau 0x5A56 = 1 (<c>torszene_dauer.py</c> Ausgabe A „Aufbau scheitert“). Mit vollständigen
    /// Originaldaten nicht erreichbar (Review torszene-dauer R3).
    /// </summary>
    public bool PermanentRink { get; }

    private GoalSceneRinkView? _rink;
    private bool _backgroundOnly;

    /// <summary>Stehendes Eisfeld (Dauer-Eisfeld des Spieltags oder Eis des Penaltyschießens) oder null.</summary>
    public GoalSceneRinkView? Rink => _rink;

    /// <summary>Alle aufgebauten Eisfelder in ihrer Reihenfolge (für Tests).</summary>
    public List<GoalSceneRinkView> RinksBuilt { get; } = [];

    /// <summary>Wird nach einem Abbau nur der Hintergrund <c>110h:7Ah</c> gezeigt (Datei 0x56E9A)?</summary>
    public bool ShowingBackgroundOnly => _backgroundOnly;

    private GameState Game => Host.Game!;

    /// <summary>Angezeigte Minute (DGROUP 0xE19E).</summary>
    public int DisplayMinute { get; private set; }

    /// <summary>Wartet der Bildschirm nach Minute 60/70 auf einen Klick?</summary>
    public bool WaitingForClick { get; private set; }

    /// <summary>Ist das Spiel gerechnet und der Bildschirm fertig?</summary>
    public bool Done { get; private set; }

    public LiveClock Clock => _clock;

    public LiveSession Session => _session;

    public LiveReporter ReporterState => _reporter;

    public override void Enter()
    {
        EndIntervention();
        _script ??= Script().GetEnumerator();
    }

    public override void Update(InputState input)
    {
        _input = input;
        if (Done)
            return;
        if (input.Pressed(Key.Enter) || input.Pressed(Key.Escape))
            _skip = true;
        if (input.Pressed(Key.Space))
            _space = true; // [ORIGINAL] Tastenpuffer, gelesen einmal je Minute nach der Pause (Datei 0x37CFA)
        // T4: [ORIGINAL] Eingriffsmenü 0x38D96 kehrt mit Dauer-Eisfeld sofort zurück (Datei 0x38DA2–0x38DAA).
        if (!WaitingForClick && !PermanentRink && (input.LeftClick || input.RightClick))
        {
            Click(input.MouseX, input.MouseY, input.RightClick);
            if (_intervention is not null)
                return; // Aufstellung bzw. Taktik ist offen; die Minute läuft erst nach der Rückkehr weiter.
        }
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
        // Enter/Escape: restliche Minuten ohne Anzeige (Näherung, siehe Klasse).
        while (_session.Step())
            DisplayMinute = _session.Day?.Minute ?? DisplayMinute;
        _rink = null; // T6: Abbau (Modus 3)
        _clock.EndMatchDay();
        _session.Release();
        Host.Show(_back);
    }

    private IEnumerable<bool> Script()
    {
        if (PermanentRink)
        {
            foreach (var f in RinkScript())
                yield return f;
            yield break;
        }
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
            if (_space)
            {
                // S05: [ORIGINAL] Leertaste → Spielmodus-Optionen, Minute ohne Rechnung (Datei 0x37D0B–0x37D89, siehe Klasse).
                _space = false;
                foreach (var f in Options())
                    yield return f;
                if (!_session.Skip())
                    break;
                SkippedMinutes.Add(minute);
            }
            else if (!_session.Step())
                break;
            else
            {
                foreach (var f in MinuteMessages(day, minute))
                    yield return f;
            }
            yield return true; // [ORIGINAL] 0x3854A: Anzeigetafel am Minutenende
            if (minute is 60 or 70 && _clock.WaitsForClick(minute, display: true))
            {
                WaitingForClick = true;
                yield return true;
                while (!_input.LeftClick && !_input.RightClick)
                    yield return true;
                WaitingForClick = false;
            }
            // S04: [ORIGINAL] Ende des Aufrufs von 0x37A62 (Datei 0x3858C–0x386CD, nach dem Warten auf den Klick 0x3855F): Seiten
            // des Seitenwählers 0x3A88A mit der Minute; nach Minute 70 und Penaltyschießen noch einmal (Datei 0x37297–0x372AC).
            if (LivePages.IsPeriodEnd(minute))
            {
                foreach (var f in Pages(day, minute, afterShootout: false))
                    yield return f;
                if (minute == LiveMatchDay.LastMinute && day.Matches.Any(m => m.Stepped && m.Report.ShootoutHomeWins is not null))
                {
                    // F076: [ORIGINAL] 0x32AA3(Schalter 1) nach den Seiten der Minute 70 (Datei 0x3728B): je Paarung mit
                    // Penaltyschießen in Paarungsreihenfolge der Penalty-Bildschirm (LivePenaltyScreen), dann die Seiten.
                    foreach (var f in Shootouts(day))
                        yield return f;
                    foreach (var f in Pages(day, minute, afterShootout: true))
                        yield return f;
                }
            }
        }
    }

    /// <summary>Meldungen einer gerechneten Minute aller Paarungen.</summary>
    private IEnumerable<bool> MinuteMessages(LiveMatchDay day, int minute)
    {
        // S01/A456/A909: [ORIGINAL] je Minute die Paarungen in ihrer Reihenfolge (0x37A62): Meldungen der Paarung mit Reihe,
        // Tormeldung der Paarung ohne Reihe an ihrer Stelle (Abschluss 0xA484, Datei 0xABB4–0xAD71).
        foreach (var match in day.Matches)
        {
            var parts = match.Row is not null
                ? Messages(match, minute)
                : [Notices(minute, n => n.HomeClub == match.Home.Index && n.AwayClub == match.Away.Index)];
            foreach (var part in parts)
                foreach (var f in part)
                    yield return f;
        }
        // Spur zusehen-rest, A832: Meldungen früher gerechneter Paarungen (Pokaltermine, je Paarung gerechnet).
        foreach (var f in Notices(minute, n => !day.Matches.Any(m => m.Home.Index == n.HomeClub && m.Away.Index == n.AwayClub)))
            yield return f;
    }

    /// <summary>Wird gerade der Penalty-Bildschirm gezeigt (F076)?</summary>
    public bool ShowingShootout { get; private set; }

    /// <summary>
    /// [ORIGINAL] Anzeigeweg von 0x3901B nur, wenn ein Verein der Paarung live zugesehen wird (Datei 0x390B8–0x390D6): Ankündigung,
    /// Schüsse, Pause 50, Endergebnis (<see cref="LivePenaltyScreen"/>).
    /// <list type="bullet">
    /// <item>[ORIGINAL] T7 (Datei 0x39036–0x3906D): läuft das Dauer-Eisfeld, zuerst dessen Abbau (Modus 3, Hintergrund).</item>
    /// <item>[ORIGINAL] T7 (Datei 0x3937F–0x393D8): mit Darstellung 1 **und** 2 nach der Ankündigung eigenes Eis
    ///   (<see cref="GoalSceneRinkView.ForShootout"/>), je Schuss die Torszene 0x53C57 darauf (Modus 2, Pause 30 und 20,
    ///   Kopfzeile mit den Treffern vor dem Schuss: 0x53C57 liest das Ergebnisbyte der Zeile + 10 = <c>DGROUP 0x5C4B</c>, Datei
    ///   0x3941D/0x394BB), dann Pause 80 (Datei 0x394FB); am Ende Abbau (Datei 0x3954A–0x39588).</item>
    /// <item>[APPROX] Mit Darstellung 0 (Live-Bildschirm, Spielstand 0x51FEA je Treffer, Pause 80; Datei 0x3941A, 0x394D3) zeigt das
    ///   Remake die Schüsse weiterhin nicht; zwischen Ankündigung und Endergebnis nur die Pause 50.</item>
    /// </list>
    /// </summary>
    private IEnumerable<bool> Shootouts(LiveMatchDay day)
    {
        foreach (var m in day.Matches.Where(m => m.Stepped && m.Report.ShootoutGoals is not null))
        {
            if (!m.Home.WatchedLive && !m.Away.WatchedLive)
                continue;
            if (_rink is not null)
            {
                _rink = null; // [ORIGINAL] Datei 0x39036: Abbau Modus 3 des Eises der Verlängerung
                _backgroundOnly = true;
            }
            ShowingShootout = true;
            Host.Show(new LivePenaltyScreen(Host, this, m.Home, m.Away, null));
            yield return true; // weiter, sobald der Bildschirm zurückkehrt
            ShowingShootout = false;
            if (Game.Options.LiveMode != 0 && GoalSceneCall.Available(Assets))
            {
                foreach (var f in ShootoutShots(m))
                    yield return f;
            }
            for (var i = 0; i < LiveReporter.PauseFrames(LivePenaltyScreen.PauseBeforeResult); i++)
                yield return true;
            ShowingShootout = true;
            Host.Show(new LivePenaltyScreen(Host, this, m.Home, m.Away, m.Report.ShootoutGoals));
            yield return true;
            ShowingShootout = false;
            _backgroundOnly = false;
        }
    }

    /// <summary>[ORIGINAL] Pause nach jedem Schuss (Datei 0x394FB: <c>50h:19Eh(80)</c>).</summary>
    public const int ShotPause = 80;

    /// <summary>T7: eigenes Eis, Schüsse mit Torszene, Abbau (siehe <see cref="Shootouts"/>).</summary>
    private IEnumerable<bool> ShootoutShots(LiveMatch match)
    {
        _backgroundOnly = false;
        _rink = GoalSceneRinkView.ForShootout(Assets, Game, match.Home, match.Away);
        RinksBuilt.Add(_rink);
        yield return true; // [ORIGINAL] Modus 1: Eis steht (einblenden)
        var score = (Home: 0, Away: 0);
        foreach (var shot in OriginalMinutes.ShootoutShots(match.Report))
        {
            if (shot.Scene is { } choice)
            {
                var scene = new ScoreboardScene(LiveMatchDay.LastMinute, shot.Side, shot.Goal, choice);
                foreach (var f in GoalScene(match, scene, score, LiveMatchDay.LastMinute))
                    yield return f;
            }
            if (shot.Goal)
                score = shot.Side == 0 ? (score.Home + 1, score.Away) : (score.Home, score.Away + 1);
            for (var i = 0; i < LiveReporter.PauseFrames(ShotPause); i++)
                yield return true;
        }
        _rink = null; // [ORIGINAL] Datei 0x3954A–0x39588: Abbau Modus 3, Hintergrund 110h:7Ah
        _backgroundOnly = true;
    }

    /// <summary>
    /// Spieltag auf dem Dauer-Eisfeld (A472, <c>docs/analyse/torszene-dauer.md</c> §2–§7):
    /// <list type="bullet">
    /// <item>[ORIGINAL] T1 Aufbau einmal vor dem ersten Drittel (Datei 0x3710B–0x37171, <see cref="GoalSceneRinkView.ForMatchDay"/>).</item>
    /// <item>[ORIGINAL] T2 Je Minute ohne Pause, Zwinkern und Leertaste nur die Minutenzeile (Datei 0x37C6B–0x37CEA); keine
    ///   Spieluhr, Anzeigetafel (Datei 0x52BEB), Reporter (Datei 0x5252A), Tormeldung (Datei 0xABB4).</item>
    /// <item>[ORIGINAL] T3 Torszenen der Managerpaarungen in Rechenreihenfolge auf dem stehenden Eis (Modus 2, Halle des
    ///   Aufbaus, ohne Pause 6, danach kein Live-Bildschirm; Datei 0x56B4D–0x56E3F).</item>
    /// <item>[ORIGINAL] T4 Kein Eingriffsmenü (Datei 0x38DA2); nach einer Verletzung die automatische Aufstellung wie ohne
    ///   Live-Anzeige (Datei 0x54134–0x5419D, Spielkern <c>MatchEngine.Injuries.cs</c>).</item>
    /// <item>[ORIGINAL] T5 Verlängerung auf demselben Eis (Datei 0x37220); nach Minute 60/70 kein Klick, keine Liga- oder
    ///   Pokalseiten (Datei 0x3855F, 0x385E7, 0x3A9E5); nach einem angezeigten Penaltyschießen ist das Eis abgebaut und die
    ///   Seiten nach dem Penaltyschießen erscheinen normal (§6).</item>
    /// <item>[ORIGINAL] T6 Abbau am Ende (Datei 0x372C2, Modus 3).</item>
    /// </list>
    /// </summary>
    private IEnumerable<bool> RinkScript()
    {
        yield return true; // [ORIGINAL] Modus 1: Eis steht (einblenden; Aufbau im Konstruktor)
        while (_session.Day is { } day && day.Running && day.Minute < LiveMatchDay.LastMinute)
        {
            var minute = day.Minute + 1;
            DisplayMinute = minute;
            _clock.Minute(display: false); // [ORIGINAL] Anzeige-Argument 0 (Datei 0x37176–0x371E8)
            if (_rink is not null)
                _rink.Minute = minute; // [ORIGINAL] 0xE19E = Minute, Minutenzeile 0x570E8 (Datei 0x37CD5–0x37CEA)
            if (!_session.Step())
                break;
            foreach (var match in day.Matches)
            {
                foreach (var (scene, score) in ScenesOfMinute(match, minute))
                {
                    foreach (var f in GoalScene(match, scene, score, minute))
                        yield return f;
                }
            }
            yield return true; // Minutenende
            if (minute == LiveMatchDay.LastMinute && day.Matches.Any(m => m.Stepped && m.Report.ShootoutHomeWins is not null))
            {
                foreach (var f in Shootouts(day))
                    yield return f;
                // [ORIGINAL] Datei 0x37297–0x372AC: Seitenwähler nach dem Penaltyschießen; mit noch stehendem Eis (kein angezeigtes
                // Penaltyschießen) keine Seiten (Datei 0x3A9E5).
                foreach (var f in Pages(day, minute, afterShootout: true))
                    yield return f;
            }
        }
        _rink = null; // [ORIGINAL] T6: Abbau Modus 3 (Datei 0x372C2–0x372F9)
    }

    /// <summary>Minuten, die per Leertaste ohne Rechnung vergangen sind (für Tests).</summary>
    public List<int> SkippedMinutes { get; } = [];

    /// <summary>Sind gerade die Spielmodus-Optionen der Leertaste offen?</summary>
    public bool ShowingOptions { get; private set; }

    /// <summary>[ORIGINAL] Datei 0x37D29–0x37D89: Sicherung zurück, Optionen 0x2CD1C, neu sichern, Lampe aus, Neuaufbau.</summary>
    private IEnumerable<bool> Options()
    {
        _clock.BeforeOptions();
        ShowingOptions = true;
        Host.Show(new GameOptionsScreen(Host, this));
        yield return true; // weiter, sobald die Optionen zu diesem Bildschirm zurückkehren
        ShowingOptions = false;
        _clock.AfterOptions();
        _input = new InputState();
    }

    /// <summary>Wird gerade eine Seite nach dem Drittel gezeigt (S04)?</summary>
    public bool ShowingPage { get; private set; }

    /// <summary>Zeigt die Seiten nach dem Drittel nacheinander (<see cref="LivePageScreens"/>); jede kehrt hierher zurück.</summary>
    private IEnumerable<bool> Pages(LiveMatchDay day, int minute, bool afterShootout)
    {
        // A472: [ORIGINAL] Seiten nur ohne stehendes Eis (DGROUP 0x5A55, Datei 0x3A9E5); das Eis bildet _rink ab.
        foreach (var page in LivePageScreens.For(Game, day, minute, afterShootout, permanentRink: _rink is not null))
        {
            foreach (var screen in LivePageScreens.Create(Host, this, day, page, minute, afterShootout))
            {
                ShowingPage = true;
                Host.Show(screen);
                yield return true; // weiter, sobald die Seite zu diesem Bildschirm zurückkehrt
                ShowingPage = false;
            }
        }
    }

    /// <summary>
    /// [ORIGINAL] Treffer-Test Schieber 0x534BC (Datei 0x534D7–0x534FC): x ≤ mx ≤ x + Breite und y ≤ my ≤ y + 6 →
    /// (mx − x) · n / Breite + 1, sonst 0.
    /// </summary>
    public static int SliderHit(int mx, int my, int x, int y, int width, int n) =>
        mx >= x && mx <= x + width && my >= y && my <= y + 6 ? (mx - x) * n / width + 1 : 0;

    /// <summary>[ORIGINAL] Treffer-Test Lampe 0x53508 (Datei 0x53518–0x53535): x ≤ mx ≤ x + 5 und y ≤ my ≤ y + 5.</summary>
    public static bool LampHit(int mx, int my, int x, int y) => mx >= x && mx <= x + 5 && my >= y && my <= y + 5;

    /// <summary>[ORIGINAL] Lampe „Schnelldurchlauf“ (Datei 0x38DBB–0x38DD9): 2 &lt; x &lt; 21 und 225 &lt; y &lt; 239.</summary>
    public static bool FastLampHit(int mx, int my) => mx is > 2 and < 21 && my is > 225 and < 239;

    /// <summary>Was ein Klick im Eingriffsmenü auslöst (für Tests und den Kopf/Logo-Weg).</summary>
    public enum ClickResult { None, Lamp, Effort, Attack, Line, Crest }

    /// <summary>Letzter Klick im Eingriffsmenü.</summary>
    public ClickResult LastClick { get; private set; }

    /// <summary>
    /// Eingriffsmenü Datei 0x38D96 (§10, B3): Lampe „Schnelldurchlauf“, dann je Reihe p (45p &lt; y &lt; 45p + 43) mit
    /// Managerverein auf der angeklickten Seite (x &gt; 160 Gast) Einsatz, Angriff, Reihe auf dem Eis und Kopf/Logo.
    /// [APPROX] Das Original fragt die Maus in der Minutenschleife je Managerpaarung unmittelbar vor deren Rechnung ab (Datei
    /// 0x37EB5/0x382B3, <c>docs/analyse/live-spieltag.md</c> §2), das Remake je Bild zwischen zwei Minuten. Die Wirkung ist
    /// gleich: 0x38D96 zieht keinen Zufall und schreibt nur die Felder der eigenen Paarung, die deren nächste Rechnung liest
    /// (<see cref="MatchIntervention"/>); anders ist nur, welche Meldungen der Minute beim Klick schon zu sehen waren.
    /// </summary>
    private void Click(int mx, int my, bool right)
    {
        LastClick = ClickResult.None;
        if (FastLampHit(mx, my))
        {
            _clock.ToggleLamp(); // [ORIGINAL] Datei 0x38DE1–0x38E19
            LastClick = ClickResult.Lamp;
        }
        for (var p = 0; p < Math.Min(_session.Shown.Count, LiveScoreboard.MaxRows); p++)
        {
            if (my <= RowStep * p || my >= RowStep * p + 43)
                continue;
            var match = _session.Shown[p];
            // [ORIGINAL] entschiedene Paarung rechnet in der Verlängerung nicht und wird nicht abgefragt (Datei 0x3825F–0x3826C vor
            // 0x38D96).
            if (!match.Running)
                continue;
            var side = mx > 160 ? 1 : 0;
            if ((side == 0 ? match.HomeTeam : match.AwayTeam) is not { Managed: true } team)
                continue;
            var x0 = BoardX(side);
            var xk = CrestX(side);
            if (SliderHit(mx, my, x0 + 39, RowStep * p + 22, 25, MatchIntervention.MaxEffort) is > 0 and var e)
            {
                MatchIntervention.SetEffort(team, e - 1);
                LastClick = ClickResult.Effort;
            }
            if (SliderHit(mx, my, x0 + 39, RowStep * p + 29, 25, MatchIntervention.MaxAttack) is > 0 and var a)
            {
                MatchIntervention.SetAttack(team, a - 1);
                LastClick = ClickResult.Attack;
            }
            for (var t = 0; t < 2; t++)
            for (var j = 0; j < MatchIntervention.LineCount; j++)
            {
                if (LampHit(mx, my, x0 + 8 + 8 * t, RowStep * p + 14 + 7 * j) && MatchIntervention.SetLine(team, t, j))
                    LastClick = ClickResult.Line;
            }
            if (mx >= xk && mx <= xk + 39)
            {
                // [ORIGINAL] Datei 0x38FC0–0x38FED → 0x5434C: Aufstellung (links) bzw. Taktik (rechts) im Spiel.
                LastMode = MatchIntervention.ModeFor(right);
                LastClick = ClickResult.Crest;
                BeginIntervention(team, side == 0 ? match.Home : match.Away, LastMode.Value);
                return;
            }
        }
    }

    private (LiveTeam Team, Club Club, int Manager, NationalCoach? Coach)? _intervention;

    /// <summary>
    /// [ORIGINAL] Datei 0x5434C (§4, §10, §15): DGROUP 0xA70E = Manager des angeklickten Vereins (alter Wert gesichert), dann
    /// Aufstellung bzw. Taktik mit Kennwert „im Spiel“ (0x48E28). Das Remake setzt dafür <see cref="GameState.CurrentManager"/>
    /// und stellt ihn bei der Rückkehr zurück (<see cref="EndIntervention"/>).
    /// [ORIGINAL] Das Land des Bundestrainers (Vereinsfeld 0x16 = 6, Datei 0x38E5F–0x38EC8 und 0x38FC0–0x38FED) öffnet über
    /// 0x5434C mit Manager 5 Aufstellung bzw. Taktik des Nationalteams (A455, Spur national Teil 4;
    /// <see cref="LiveIntervention.OpenNational"/>); aktiver Manager ist im Remake der Manager, der Bundestrainer ist.
    /// </summary>
    private void BeginIntervention(LiveTeam team, Club club, InterventionMode mode)
    {
        if (Game.NationalCoach is { } coach && coach.Nation == club.Index && coach.Manager < Game.Managers.Count)
        {
            _intervention = (team, club, Game.CurrentManager, coach);
            Game.CurrentManager = coach.Manager;
            Host.Show(LiveIntervention.OpenNational(Host, this, mode));
            return;
        }
        var managers = Game.Managers.ToList();
        var manager = managers.FindIndex(m => Game.ActiveManagers.Contains(m) && m.ClubIndex == club.Index);
        if (manager < 0)
            return;
        _intervention = (team, club, Game.CurrentManager, null);
        Game.CurrentManager = manager;
        Host.Show(LiveIntervention.Open(Host, this, mode));
    }

    /// <summary>
    /// [ORIGINAL] Datei 0x5438D–0x54419 nach Aufstellung/Taktik: Feldspieler des Managervereins neu zählen (0x38A36), Live-Bildschirm
    /// neu aufbauen 0x5353C(Pokal, 1), DGROUP 0xA70E zurück. Die Aufstellung übernimmt <see cref="LiveLineup.Apply"/> bzw. beim
    /// Nationalteam <see cref="MatchEngine.ApplyNationalLineup"/> in den Spielkader (das Original schreibt dieselben Kaderplätze);
    /// der Neuaufbau ist das nächste Bild.
    /// </summary>
    private void EndIntervention()
    {
        if (_intervention is not { } open)
            return;
        _intervention = null;
        if (open.Coach is { } coach)
            MatchEngine.ApplyNationalLineup(open.Team, coach);
        else
            LiveLineup.Apply(open.Team, open.Club);
        Game.CurrentManager = open.Manager;
        _input = new InputState(); // [ORIGINAL] Maus leeren (38h:8A7h vor dem Einblenden, Datei 0x5438D–0x54419)
        Interventions++;
    }

    /// <summary>Anzahl der abgeschlossenen Eingriffe über Kopf/Logo (für Tests).</summary>
    public int Interventions { get; private set; }

    /// <summary>Modus des letzten Klicks auf Kopf/Logo (Aufstellung/Taktik).</summary>
    public InterventionMode? LastMode { get; private set; }

    /// <summary>
    /// Meldungen einer Minute (§5): Chancen der Reihe (Torszene 0x53C57 → 0x52520 Ereignis 0/1), Strafen (0x35818 → Ereignis
    /// 2/3 mit Strafgrund aus dem Kern), Verletzungen (Ereignis 4). [ORIGINAL] Reihenfolge innerhalb der Minute wie die Rechnung
    /// der Paarung (A456, S05; Datei 0x37F06 bzw. 0x38304 vor 0x9F5C/0xA484, Strafen 0x35818 am Ende): Verletzungen, Chancen,
    /// Strafen der beiden Seiten in der Reihenfolge des Kerns (nach dem Seitentausch random(0, 1), Datei 0x37F9B–0x38073).
    /// Vergebene Chancen nennen Schütze und Vorlage (A456, <see cref="ScoreboardChance.Shooter"/>).
    /// </summary>
    private IEnumerable<IEnumerable<bool>> Messages(LiveMatch match, int minute)
    {
        if (match.Row is not { } row)
            yield break;
        var events = match.Report.Events.Where(e => e.Minute == minute).ToList();
        // [ORIGINAL] 0x38B8F vor den Torchancen (Datei 0x37F06): Ereignis 4 über 0x53C57 → 0x5411E, Reporter sofort.
        foreach (var e in events.Where(e => e.Kind == MatchEventKind.Injury))
            yield return Report(match, minute, ReporterEvent.Injury, ClubName(match, e.Side), e.Player?.Name, null, null);
        var goals = new Queue<MatchEvent>(events.Where(e => e.Kind == MatchEventKind.Goal));
        // Torszene 0x53C57 (Spur torszenen, C3): Szenen der Minute in der Reihenfolge der Chancen (der Kern legt sie zusammen
        // ab), Spielstand vor der Chance aus den Toren der früheren Minuten und Chancen.
        var scenes = new Queue<ScoreboardScene>(row.Scenes.Where(c => c.Minute == minute));
        var earlier = match.Report.Events.Where(e => e.Kind == MatchEventKind.Goal && e.Minute < minute).ToList();
        var score = (Home: earlier.Count(e => e.Side == 0), Away: earlier.Count(e => e.Side == 1));
        foreach (var c in row.ChanceList.Where(c => c.Minute == minute))
        {
            if (scenes.Count > 0 && scenes.Peek() is var scene && scene.Side == c.Side && scene.Goal == c.Goal)
                yield return GoalScene(match, scenes.Dequeue(), score, minute);
            if (c.Goal)
                score = c.Side == 0 ? (score.Home + 1, score.Away) : (score.Home, score.Away + 1);
            MatchEvent? goal = null;
            if (c.Goal && goals.Count > 0)
                goal = goals.Dequeue();
            // A456: [ORIGINAL] auch die vergebene Chance meldet Schütze und Vorlage aus 0x541D9 (ScoreboardChance.Shooter).
            yield return Report(match, minute, c.Goal ? ReporterEvent.Goal : ReporterEvent.ChanceMissed, ClubName(match, c.Side),
                goal?.Player?.Name ?? c.Shooter, goal?.Assist?.Name ?? c.Assist, null);
        }
        var reasons = new Queue<ScoreboardPenaltyReason>(row.PenaltyReasons.Where(r => r.Minute == minute));
        foreach (var e in events.Where(e => e.Kind == MatchEventKind.Penalty))
        {
            var reason = reasons.Count > 0 ? reasons.Dequeue().Reason : GoalSceneSelection.MajorPenaltyReason;
            yield return Report(match, minute, e.PenaltyMinutes == 5 ? ReporterEvent.Major : ReporterEvent.Minor, ClubName(match, e.Side),
                e.Player?.Name, null, GoalSceneSelection.Reasons[reason]);
        }
    }

    /// <summary>
    /// Torszenen einer Minute der Paarung in der Reihenfolge der Chancen mit dem Spielstand vor der Chance (wie
    /// <see cref="Messages"/>); ohne Reihe keine.
    /// </summary>
    private static IEnumerable<(ScoreboardScene Scene, (int Home, int Away) Score)> ScenesOfMinute(LiveMatch match, int minute)
    {
        if (match.Row is not { } row)
            yield break;
        var scenes = new Queue<ScoreboardScene>(row.Scenes.Where(c => c.Minute == minute));
        var earlier = match.Report.Events.Where(e => e.Kind == MatchEventKind.Goal && e.Minute < minute).ToList();
        var score = (Home: earlier.Count(e => e.Side == 0), Away: earlier.Count(e => e.Side == 1));
        foreach (var c in row.ChanceList.Where(c => c.Minute == minute))
        {
            if (scenes.Count > 0 && scenes.Peek() is var scene && scene.Side == c.Side && scene.Goal == c.Goal)
                yield return (scenes.Dequeue(), score);
            if (c.Goal)
                score = c.Side == 0 ? (score.Home + 1, score.Away) : (score.Home, score.Away + 1);
        }
    }

    /// <summary>Reporter-Meldungen in der gezeigten Reihenfolge (Minute, Heimverein der Paarung, Ereignis; für Tests).</summary>
    public List<(int Minute, int HomeClub, ReporterEvent Event)> MessagesShown { get; } = [];

    private IEnumerable<bool> Report(LiveMatch match, int minute, ReporterEvent e, string club, string? player, string? second,
        string? text)
    {
        MessagesShown.Add((minute, match.Home.Index, e));
        return _reporter.Message(club, e, player, second, text);
    }

    /// <summary>Aufträge an den Abspieler in der Reihenfolge der Abläufe (Wiederholungen eingeschlossen); für Proben.</summary>
    public List<GoalSceneRequest> ScenesShown { get; } = [];

    /// <summary>
    /// [ORIGINAL] Anzeigeteil der Torszene 0x53C57 (Datei 0x53DCA–0x5406A, <see cref="GoalSceneCall"/>): mit Darstellung ≠ 0
    /// Pause 30, Abspieler, Pause 20, Taste „r“/„w“ wiederholt bis zu dreimal; danach Reporter wie bisher.
    /// </summary>
    private IEnumerable<bool> GoalScene(LiveMatch match, ScoreboardScene scene, (int Home, int Away) score, int minute)
    {
        if (Game.Options.LiveMode == 0 || !GoalSceneCall.Available(Assets))
            yield break;
        for (var i = 0; i < LiveReporter.PauseFrames(GoalSceneCall.PauseBefore); i++)
            yield return true;
        for (var plays = 1; ; plays++)
        {
            var request = GoalSceneCall.Request(Assets, Game, match, scene, score, minute);
            // A472 T3/T7: [ORIGINAL] auf stehendem Eis Modus 2 (2 · [0x5A55], Datei 0x53FCE) mit der Halle des Aufbaus (Modus 2
            // lädt nichts, Datei 0x5680A).
            if (_rink is { } rink)
                request = request with { HallLevel = rink.HallLevel, Continuous = true };
            GoalSceneScreen? player = null;
            player = new GoalSceneScreen(Host, request, GoalSceneCall.GraphicsFor(Assets), () =>
            {
                _rink?.Keep(player!); // [ORIGINAL] Modus 2: letztes Bild und Kopfzeile bleiben stehen (Datei 0x56E37)
                Host.Show(this);
            });
            ScenesShown.Add(request);
            Host.Show(player);
            do
                yield return true; // weiter erst, wenn der Abspieler fertig ist und den Live-Bildschirm wieder zeigt
            while (!player.Done);
            var key = player.FirstKey;
            for (var i = 0; i < LiveReporter.PauseFrames(GoalSceneCall.PauseAfter); i++)
            {
                if (key is null && _input.Text.Length > 0)
                    key = _input.Text[0];
                yield return true;
            }
            if (key is null && _input.Text.Length > 0)
                key = _input.Text[0];
            if (plays >= GoalSceneCall.MaxPlays || !GoalSceneCall.IsReplayKey(key))
                yield break;
        }
    }

    private readonly LiveGoalNotice _notice = new();

    /// <summary>Laufende Tormeldung (A832).</summary>
    public LiveGoalNotice GoalNotice => _notice;

    /// <summary>Tormeldungen der Minute aus <see cref="LiveSession.PendingNotices"/> (A832, <see cref="LiveGoalNotice"/>).</summary>
    private IEnumerable<bool> Notices(int minute, Func<GoalNotice, bool> filter)
    {
        foreach (var notice in _session.PendingNotices.Where(n => n.Minute == minute && filter(n)))
        {
            foreach (var f in _notice.Show(notice))
                yield return f;
            _reporter.ClearLines();
        }
    }

    private string ClubName(LiveMatch match, int side) => (side == 0 ? match.Home : match.Away).Name;

    public override void Draw(Surface s)
    {
        if (_rink is { } rink)
        {
            rink.Draw(s, Assets);
            return;
        }
        s.Palette = Assets.Palette;
        if (_backgroundOnly)
        {
            s.Blit(Assets.Image(BackgroundGraphic), 0, 0); // [ORIGINAL] Abbau Modus 3: 110h:7Ah (Datei 0x56E9A)
            return;
        }
        DrawBuild(s, Assets, Game, _session.Shown, _clock.Lamp);
        for (var p = 0; p < Math.Min(_session.Shown.Count, LiveScoreboard.MaxRows); p++)
            DrawBoard(s, Assets, _session.Shown[p], p, MinuteOf(_session.Shown[p]));
        LiveWidgets.Clock(s, Assets, 0, DisplayMinute);
        DrawReporter(s, Assets, _reporter);
        _notice.Draw(s, Assets, Game);
    }

    private int MinuteOf(LiveMatch match) => match.Running ? DisplayMinute : match.LastMinute;

    /// <summary>[ORIGINAL] Aufbau 0x5353C (§4): Hintergrund, Kästen, je Reihe Hintergrund, Köpfe bzw. Logos, Namen, Zuschauer, Lampe.</summary>
    public static void DrawBuild(Surface s, Assets a, GameState game, IReadOnlyList<LiveMatch> rows, bool lamp)
    {
        s.Blit(a.Image(BackgroundGraphic), 0, 0);
        var frame = a.Image(FrameGraphic);
        s.Blit(frame, ClockBoxSource, ClockBox.X, ClockBox.Y);
        s.Blit(frame, ReporterBoxSource, ReporterBox.X, ReporterBox.Y);
        // A457: [ORIGINAL] Datei 0x53658–0x5367E: Kopie 50h:BD3h (0x13B23 → 0x11283) ohne Maske; deren Pixelschleifen lassen
        // Farbe 0 aus (Speicher → Speicher Datei 0x1134C, Speicher → Bildschirm Datei 0x11469: or al, al / jz).
        s.Blit(frame, ReporterSource, Reporter.X, Reporter.Y, transparent: 0);
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
            var (text, color) = SpectatorLine(game, match.Home, match.Away, match.Competition);
            CompetitionWidgets.Centered(s, a.Mini, 48, 82, y + 38, text, color, null);
        }
        s.Blit(frame, LampFieldSource, LampField.X, LampField.Y);
        LiveWidgets.Lamp(s, a, FastLamp.X, FastLamp.Y, lamp);
    }

    /// <summary>Farbe der Zuschauerzahl bei ausverkauftem Stadion ([ORIGINAL] Datei 0x53AF5: 0x10, sonst 0).</summary>
    public const byte SoldOutColor = 0x10;

    /// <summary>
    /// Zuschauerzeile einer Reihe. [ORIGINAL] Datei 0x53A11–0x53B37: Zuschauer aus der Spielabrechnung vor dem Spiel
    /// (<see cref="GameState.HomeGate"/>, #837), Farbe <see cref="SoldOutColor"/> bei ausverkauft, sonst 0; ohne Heimspiel
    /// eines Managers oder in der WM (DGROUP 0x12B) „(ausw.)“ (DGROUP 0xD08) in Farbe 0. Zahl über 0x0BAB2 mit DGROUP
    /// 0x38D = 0, also mit Tausenderpunkt (wie <see cref="Statistics.ClubStatisticsScreen.Number"/>; Videobild folge01_00615
    /// „4.335“, gegen die Vermutung „ohne“ in <c>re-livespiel.md</c> §4 Punkt 5). [ORIGINAL] Mindestbreite DGROUP 0x374 = 1
    /// (A475): 0x5353C lädt vor den Reihen die Grafiken über 0xC17D (Datei 0x535DB, 0x537E7, 0x53992), das als Erstes
    /// <c>mov byte [0x374], 1</c> schreibt (Datei 0x0C1A1); bis zur Zuschauerzeile schreibt kein anderer Aufruf 0x374.
    /// Zentriert 48–82, Grundlinie y + 38, ohne Schatten.
    /// </summary>
    public static (string Text, byte Color) SpectatorLine(GameState game, Club home, Club away, MatchCompetition competition)
    {
        if (competition == MatchCompetition.WorldCup || game.HomeGate(home, away) is not { } gate)
            return ("(ausw.)", 0);
        return (Statistics.ClubStatisticsScreen.Number(gate.Spectators, 1, true), gate.SoldOut ? SoldOutColor : (byte)0);
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
