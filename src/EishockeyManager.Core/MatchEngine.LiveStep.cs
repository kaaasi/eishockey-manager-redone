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

    internal void Play(int minute, GameOptions options)
    {
        var home = Setup.HomeTeam;
        var away = Setup.AwayTeam;
        if (minute is 21 or 41 or 61 or 66)
        {
            // [ORIGINAL] 0x37A86: in jeder Drittelpause (auch vor der Verlängerung) 0x84D7(1) – wie OriginalMinutes.PlayPeriod.
            OriginalMinutes.RecoverEnergy(home);
            OriginalMinutes.RecoverEnergy(away);
        }
        var periodEnd = minute switch
        {
            <= 20 => 20,
            <= 40 => 40,
            <= 60 => 60,
            <= 65 => 65,
            _ => 70,
        };
        var previous = OriginalMinutes.Display;
        OriginalMinutes.Display = new LiveDisplay(Row, Setup.DayActive || Row is not null, options);
        try
        {
            OriginalMinutes.PlayMinute(home, away, Setup.Rules, minute, periodEnd, Report, Setup.Random);
        }
        finally
        {
            OriginalMinutes.Display = previous;
        }
        LastMinute = minute;
        if (minute == 60)
        {
            // [ORIGINAL] wie OriginalMinutes.Play: Verlängerung 61–70 nur im K.-o.-Spiel bei Gleichstand nach 60 Minuten.
            if (Setup.KnockOut && Report.HomeGoals == Report.AwayGoals)
                Report.Overtime = true;
            else
                Complete();
        }
        else if (minute == 70)
            Complete();
    }

    /// <summary>Ende der Minutenschleife: wie <see cref="OriginalMinutes.Play"/> und <c>MatchEngine.PlayWithScoreboard</c>.</summary>
    private void Complete()
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
/// <c>docs/analyse/re-livespiel.md</c> §13 A5). Damit Eingriffe aus dem Live-Bildschirm (Spur livespiel-ui-2, B2/B3) während
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
/// für Minute alle Paarungen) und damit anders als die Spieltage des Remakes, die Paarung für Paarung rechnen
/// (<c>GameState.PlayNextMatchDay</c>). [APPROX] Wie der Spieltagsablauf den Zufall auf die Paarungen verteilt, wenn er über
/// den Minutenschritt läuft, entscheidet der Anschluss (B2/B3, Leitstelle); der Minutenschritt ändert keinen Spieltag.
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
        foreach (var match in matches)
        {
            if (match.Running)
                match.Play(Minute, game.Options);
        }
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
