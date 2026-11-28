namespace EishockeyManager.Core;

/// <summary>
/// Eine Paarung für den Minutenschritt (<see cref="LiveMatchDay"/>): Vereine, Wettbewerb, K.-o.-Merker und der Zufall, aus
/// dem diese Paarung zieht (wie das Argument von <see cref="MatchEngine.PlayWithEvents"/>).
/// </summary>
public readonly record struct LiveFixture(Club Home, Club Away, MatchCompetition Competition, bool KnockOut, Random Random);

/// <summary>Zustand einer Paarung im Minutenschritt.</summary>
public sealed class LiveMatch
{
    internal LiveMatch(MatchEngine.MatchSetup setup)
    {
        Setup = setup;
        Stepped = setup.Report is null;
        Report = setup.Report ?? new MatchReport();
        Running = Stepped;
    }

    internal MatchEngine.MatchSetup Setup { get; }

    public Club Home => Setup.Home;
    public Club Away => Setup.Away;
    public MatchCompetition Competition => Setup.Competition;

    /// <summary>K.-o.-Spiel (Verlängerung bei Gleichstand; bei der WM die Endrunde, Seite 10 statt 8 in <see cref="LivePages"/>).</summary>
    public bool KnockOut => Setup.KnockOut;

    /// <summary>Mannschaft des Heimvereins für die Minutenrechnung (Ziel der Befehle aus <see cref="MatchIntervention"/>); null bei Spielwertung.</summary>
    public LiveTeam? HomeTeam => Setup.Forfeit ? null : Setup.HomeTeam;

    /// <summary>Mannschaft des Gastvereins; null bei Spielwertung.</summary>
    public LiveTeam? AwayTeam => Setup.Forfeit ? null : Setup.AwayTeam;

    /// <summary>Mannschaft des Vereins (Heim oder Gast) oder null.</summary>
    public LiveTeam? TeamOf(int club) => club == Home.Index ? HomeTeam : club == Away.Index ? AwayTeam : null;

    /// <summary>Reihe der Live-Anzeige (A1) oder null.</summary>
    public LiveScoreboardRow? Row => Setup.Row;

    /// <summary>
    /// Läuft die Paarung durch die Minutenschleife? false bei Spielwertung 0:2/2:0 und beim schnellen Weg 0x3731F – deren
    /// Ergebnis steht schon nach <see cref="LiveMatchDay.Begin"/> fest.
    /// </summary>
    public bool Stepped { get; }

    /// <summary>Wird noch gerechnet (Minutenschleife nicht beendet)?</summary>
    public bool Running { get; private set; }

    /// <summary>Zuletzt gerechnete Minute dieser Paarung (0 vor der ersten, 60 oder 70 am Ende).</summary>
    public int LastMinute { get; private set; }

    /// <summary>Bericht, während des Spiels laufend fortgeschrieben (Stand, Ereignisse).</summary>
    public MatchReport Report { get; }

    /// <summary>
    /// Ziel der Tormeldung dieser Paarung ohne Reihe (A832, <see cref="MatchEngine.NoticeTarget"/>); gesetzt, wenn alle
    /// Paarungen vorbereitet sind (<see cref="LiveMatchDay.Begin"/>), sonst null.
    /// </summary>
    internal GoalNoticeTarget? Notice { get; set; }

    /// <summary>
    /// Rechnet eine Minute dieser Paarung. Liefert true, wenn die Minutenschleife der Paarung mit dieser Minute endet; den
    /// Abschluss (<see cref="Complete"/>) macht <see cref="LiveMatchDay.Step"/> erst nach der Minute aller Paarungen.
    /// </summary>
    internal bool Play(int minute, GameOptions options) => Play(minute, options, skip: false);

    /// <summary>
    /// Wie <see cref="Play(int, GameOptions)"/>; mit <paramref name="skip"/> entfällt die Rechnung der Minute (Leertaste, S05),
    /// Drittelpause und Ende der Schleife bleiben ([ORIGINAL] 0x37A86 vor der Minutenschleife, 0x32AA3 nach ihr).
    /// </summary>
    internal bool Play(int minute, GameOptions options, bool skip)
    {
        var home = Setup.HomeTeam;
        var away = Setup.AwayTeam;
        if (minute is 21 or 41 or 61 or 66)
        {
            // [ORIGINAL] 0x37A86: in jeder Drittelpause (auch vor der Verlängerung) 0x84D7(1) – wie OriginalMinutes.PlayPeriod.
            OriginalMinutes.RecoverEnergy(home);
            OriginalMinutes.RecoverEnergy(away);
        }
        if (skip)
            return EndOfMinute(minute);
        var periodEnd = minute switch
        {
            <= 20 => 20,
            <= 40 => 40,
            <= 60 => 60,
            <= 65 => 65,
            _ => 70,
        };
        var previous = OriginalMinutes.Display;
        OriginalMinutes.Display = new LiveDisplay(Row, Setup.DayActive || Row is not null, options, Notice);
        try
        {
            OriginalMinutes.PlayMinute(home, away, Setup.Rules, minute, periodEnd, Report, Setup.Random);
        }
        finally
        {
            OriginalMinutes.Display = previous;
        }
        return EndOfMinute(minute);
    }

    private bool EndOfMinute(int minute)
    {
        LastMinute = minute;
        if (minute == 60)
        {
            // [ORIGINAL] wie OriginalMinutes.Play: Verlängerung 61–70 nur im K.-o.-Spiel bei Gleichstand nach 60 Minuten.
            if (Setup.KnockOut && Report.HomeGoals == Report.AwayGoals)
                Report.Overtime = true;
            else
                return true;
        }
        return minute == 70;
    }

    /// <summary>
    /// Ende der Minutenschleife: wie <see cref="OriginalMinutes.Play"/> und <c>MatchEngine.PlayWithScoreboard</c> (mit
    /// Penaltyschießen nach der Verlängerung).
    /// </summary>
    internal void Complete()
    {
        Report.HomePenaltyMinutes = Setup.HomeTeam.PenaltyMinutes;
        Report.AwayPenaltyMinutes = Setup.AwayTeam.PenaltyMinutes;
        Row?.Finish(Report);
        MatchEngine.FinishMinuteLoop(Setup, Report);
        Running = false;
    }
}

/// <summary>
/// Minutenschritt im Spielkern (Spur livespiel-kern, Teil A5; Entscheidung Nutzer R18-E1 = A, 13.11.2026;
/// <c>docs/archiv/analyse/re-livespiel.md</c> §13 A5). Damit Eingriffe aus dem Live-Bildschirm (Spur livespiel-ui-2, B2/B3) während
/// des Spiels wirken, rechnet der Kern die Paarungen eines Spieltags Minute für Minute auf Befehl.
/// <list type="number">
/// <item><see cref="Begin"/>: [ORIGINAL] 0x36B5D bereitet erst alle Paarungen vor (Spielabrechnung 0x544C0 je Paarung, Datei
///   0x36DBD/0x36F9C), dann laufen die Minuten. Je Paarung dieselbe Vorbereitung wie <see cref="MatchEngine.PlayWithEvents"/>
///   (gemeinsamer Teil <c>MatchEngine.BeginMatch</c>): Spielwertung, Anzeigereihe, Mannschaften, Spielvorbereitung 0x8365,
///   schneller Weg 0x3731F.</item>
/// <item><see cref="Step"/>: [ORIGINAL] Minutenschleife 0x37A62 – je Minute alle laufenden Paarungen in ihrer Reihenfolge
///   über <see cref="OriginalMinutes.PlayMinute"/>, Drittelpausen wie <see cref="OriginalMinutes.Play"/>, Verlängerung 61–70
///   nur im K.-o.-Spiel bei Gleichstand.</item>
/// <item><see cref="Finish"/>: restliche Minuten, dann je Paarung in ihrer Reihenfolge der Abschluss wie
///   <see cref="MatchEngine.PlayWithEvents"/> (Penaltyschießen schon am Ende der Minutenschleife, dann Vereinsfelder,
///   Spielerzähler, Tagesform).</item>
/// </list>
/// Eingriffe (<see cref="MatchIntervention"/> auf <see cref="LiveMatch.HomeTeam"/>/<see cref="LiveMatch.AwayTeam"/>) zwischen
/// zwei Schritten wirken ab der nächsten Minute: die gerechnete Minute ist abgeschlossen, die nächste liest den neuen Wert
/// ([ORIGINAL] Datei 0x38ECD–0x38FBE schreibt die Vereinsfelder, gelesen wird erst in der nächsten Rechnung, §10).
/// <para>
/// Zufall: Jede Paarung zieht aus dem <see cref="LiveFixture.Random"/> ihrer Vorgabe. Mit einem eigenen Generator je Paarung
/// liefert der Minutenschritt ohne Eingriff dieselben Ergebnisse wie <see cref="MatchEngine.PlayWithEvents"/> mit diesem
/// Generator. Teilen sich Paarungen einen Generator, ziehen sie in der verschränkten Reihenfolge des Originals (0x37A62: Minute
/// für Minute alle Paarungen, <c>docs/analyse/live-spieltag.md</c>); so rechnet der Ligaspieltag
/// (<see cref="MatchEngine.PlayMatchDay"/>, S01). Das Ende der Minutenschleife (Reihe abschließen, Penaltyschießen) folgt erst
/// nach der Minute aller Paarungen, in Paarungsreihenfolge ([ORIGINAL] 0x32AA3 nach 0x37A62, Datei 0x37211/0x3728B).
/// </para>
/// Darstellungszustand: nicht im Spielstand; Zeit vergeht weiter nur über <c>GameState.Continue()</c> (der Aufrufer rechnet
/// den Spieltag innerhalb seines Schritts).
/// </summary>
public sealed class LiveMatchDay
{
    /// <summary>[ORIGINAL] Letzte Minute der Verlängerung (0x36B5D, zweiter Abschnitt 66–70).</summary>
    public const int LastMinute = 70;

    private readonly GameState game;
    private readonly List<LiveMatch> matches;
    private bool ended;

    private LiveMatchDay(GameState game, List<LiveMatch> matches)
    {
        this.game = game;
        this.matches = matches;
    }

    /// <summary>Paarungen in der Reihenfolge der Vorgabe.</summary>
    public IReadOnlyList<LiveMatch> Matches => matches;

    /// <summary>Zuletzt gerechnete Minute (0 vor dem ersten Schritt).</summary>
    public int Minute { get; private set; }

    /// <summary>Laufen noch Paarungen durch die Minutenschleife?</summary>
    public bool Running => matches.Any(m => m.Running);

    /// <summary>Paarung eines Vereins (Heim oder Gast) oder null.</summary>
    public LiveMatch? MatchOf(int club) => matches.FirstOrDefault(m => m.Home.Index == club || m.Away.Index == club);

    /// <summary>Bereitet alle Paarungen in der gegebenen Reihenfolge vor (siehe Klassenbeschreibung, Punkt 1).</summary>
    public static LiveMatchDay Begin(GameState game, IEnumerable<LiveFixture> fixtures)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(fixtures);
        var list = new List<LiveMatch>();
        foreach (var f in fixtures)
        {
            var match = new LiveMatch(MatchEngine.BeginMatch(game, f.Home, f.Away, f.Competition, f.KnockOut, f.Random));
            if (match.Stepped)
                OriginalMinutes.MinuteLoopCalls++; // wie OriginalMinutes.Play (Testzähler)
            list.Add(match);
        }
        // A909: [ORIGINAL] 0x544C0 läuft für alle Paarungen vor der Minutenschleife und setzt die Live-Anzeige des Tages (DGROUP
        // 0x5A56, Datei 0x54C63–0x54C94); das Ziel der Tormeldung steht damit für alle Paarungen erst nach der Vorbereitung fest.
        // R26-G1: [ORIGINAL] Strafgrund (Datei 0x35C1C–0x35C7B → 0x52520, Rückkehr bei 0x5A56 = 0 in Datei 0x5252A) und
        // Verletzungsregister (Datei 0x5419A) lesen in den Minuten diesen Tageswert, nicht den Stand bei der Vorbereitung
        // der eigenen Paarung; auch Paarungen vor der ersten Reihe. Mit Dauer-Eisfeld ist 0x5A56 in den Minuten 0
        // (A472, Datei 0x3710B–0x37171): das Register gilt dann ohne Anzeige, der Strafgrund prüft das Eisfeld selbst
        // (OriginalMinutes.PenaltyReason).
        var dayActive = game.LiveScoreboard is { } board && board.Season == game.Season && board.Day == game.Day && board.Active;
        foreach (var match in list)
        {
            if (!match.Stepped)
                continue;
            match.Setup.DayActive = dayActive;
            if (match.Setup.HomeTeam.TurnManager is { } register)
                register.LiveDisplay = dayActive && !OriginalMinutes.PermanentRink(game.Options);
            if (match.Setup.AwayTeam.TurnManager is { } awayRegister)
                awayRegister.LiveDisplay = dayActive && !OriginalMinutes.PermanentRink(game.Options);
            match.Notice = MatchEngine.NoticeTarget(match.Setup);
        }
        return new LiveMatchDay(game, list);
    }

    /// <summary>
    /// Rechnet die nächste Minute aller laufenden Paarungen. Liefert false, wenn keine Paarung mehr läuft (dann rechnet der
    /// Schritt nichts). Paarungen ohne Verlängerung enden mit Minute 60 (<see cref="LiveMatch.Running"/> = false).
    /// </summary>
    public bool Step()
    {
        if (ended || !Running || Minute >= LastMinute)
            return false;
        Minute++;
        var completed = new List<LiveMatch>();
        foreach (var match in matches)
        {
            if (!match.Running)
                continue; // A766: [ORIGINAL] entschiedene Paarung rechnet in der Verlängerung nicht (Datei 0x3825F–0x3826C)
            if (match.Play(Minute, game.Options))
                completed.Add(match);
        }
        // [ORIGINAL] 0x36B5D: Ende der Schleife erst nach der Minute aller Paarungen (0x32AA3 nach Minute 60 Datei 0x37211, mit
        // Penaltyschießen 0x3901B nach Minute 70 Datei 0x3728B) – in Paarungsreihenfolge.
        foreach (var match in completed)
            match.Complete();
        return true;
    }

    /// <summary>
    /// Leertaste in der Live-Anzeige (S05, Teil von A449): die nächste Minute vergeht für **keine** Paarung gerechnet. [ORIGINAL]
    /// Datei 0x37D0B–0x37D89: nach Taste 0x20 Spielmodus-Optionen 0x2CD1C, Neuaufbau 0x5353C, dann <c>jmp 0x38551</c> an das
    /// Minutenende – ohne Rechnung und ohne Tafel; die Schleife zählt weiter, Drittelpause (0x37A86) und Ende der Schleife
    /// (0x32AA3, Verlängerung bei Gleichstand nach 60) bleiben. Liefert false wie <see cref="Step"/>, wenn nichts mehr läuft.
    /// </summary>
    public bool Skip()
    {
        if (ended || !Running || Minute >= LastMinute)
            return false;
        Minute++;
        var completed = new List<LiveMatch>();
        foreach (var match in matches)
        {
            if (match.Running && match.Play(Minute, game.Options, skip: true))
                completed.Add(match);
        }
        foreach (var match in completed)
            match.Complete();
        return true;
    }


    /// <summary>
    /// Rechnet die restlichen Minuten und schließt alle Paarungen in ihrer Reihenfolge ab (wie
    /// <see cref="MatchEngine.PlayWithEvents"/>). Nur einmal aufrufbar; danach ist der Spieltag gerechnet.
    /// </summary>
    public IReadOnlyList<MatchReport> Finish()
    {
        if (ended)
            throw new InvalidOperationException("Der Spieltag ist schon abgeschlossen.");
        while (Step())
        {
        }
        ended = true;
        return matches.Select(m => MatchEngine.EndMatch(m.Setup)).ToList();
    }
}
