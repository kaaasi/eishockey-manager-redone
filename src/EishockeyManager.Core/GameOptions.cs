namespace EishockeyManager.Core;

/// <summary>Zeilen der Meldungsmatrix im Optionsbildschirm 0x2CD1C (Index = Liga · 3 + Zeile).</summary>
public enum ReportKind
{
    /// <summary>Drittelpause (Zeile 0; Index 0, 3, 6).</summary>
    Intermission = 0,

    /// <summary>Ergebnisse (Zeile 1; Index 1, 4, 7 – DGROUP 0x21E/0x221/0x224, vom Spieltagsablauf 0x3702E gelesen).</summary>
    Results = 1,

    /// <summary>Tabelle (Zeile 2; Index 2, 5, 8).</summary>
    Table = 2,
}

/// <summary>
/// Spielstand-Optionen des Originals (Spur optionen-wirkung, Teil 1). [ORIGINAL] Optionsbildschirm Datei 0x2CD1C, Zustand
/// DGROUP 0x21D…0x229 (Index 0–12) und 0x262 (Spielgeschwindigkeit), Anfangswerte aus der Datei (DGROUP 0x21D = Datei
/// 0x16FED): alle 0 außer Index 10 (DEB-Pokal) = 1, Spielgeschwindigkeit 70. Belege und Leser:
/// <c>docs/reverse-engineering.md</c>, „Spielmodus-Optionen 0x2CD1C“ und „Wirkung der Spielstand-Optionen“.
/// <para>
/// Alle Optionen sind <b>Anzeigeschalter</b>: Keine meldet einen Wettbewerb an oder ab, keine ändert den Spielmodus
/// (<see cref="GameSetup.Mode"/>, DGROUP 0xD0B9 wird nur in der Spielvorbereitung geschrieben). Spielwirkung haben nur
/// <see cref="ShowDebCup"/>, <see cref="ShowEuropeanCup"/> und <see cref="ShowWorldCup"/> – mittelbar über den Weg der
/// Pokalspiele ohne Managerverein (<see cref="CupMatchUsesMinuteLoop"/>).
/// </para>
/// <para>
/// [APPROX] Nicht geführt: Index 13 (Hintergrundgrafik, DGROUP 0x22A) und 14 (Scrolling, je Manager DGROUP 0x120 + Manager,
/// Datei 0x2CFE6/0x2D3CA) – reine Darstellung.
/// </para>
/// </summary>
public sealed class GameOptions
{
    /// <summary>Anzahl der Ligen (Spalten der Meldungsmatrix).</summary>
    public const int LeagueCount = 3;

    /// <summary>Stufen der Live-Darstellung (Index 9: 0, 1, 2).</summary>
    public const int LiveModeCount = 3;

    /// <summary>Live-Darstellung, bei der das Eisfeld das ganze Spiel über an bleibt („Neuer Spielmodus“).</summary>
    public const int PermanentRinkLiveMode = 2;

    /// <summary>[ORIGINAL] Anfangswert der Spielgeschwindigkeit (DGROUP 0x262 = 70).</summary>
    public const int DefaultMatchSpeed = 70;

    /// <summary>[ORIGINAL] Regler 0–80 in Zehnerschritten (<c>(x − 0xBE) / 12 · 10</c>, Datei 0x2CD1C).</summary>
    public const int MaxMatchSpeed = 80;

    /// <summary>
    /// Meldungsmatrix Index 0–8 (DGROUP 0x21D…0x225): Liga · 3 + Zeile <see cref="ReportKind"/>. [ORIGINAL] Schaltertabellen
    /// DGROUP 0x4C06 (x: 222, 222, 222, 252, …) und 0x4BE8 (y: 130, 144, 158, …): Index 0–2 bilden die Spalte „1.Liga“.
    /// </summary>
    public bool[] Reports { get; init; } = new bool[3 * LeagueCount];

    /// <summary>
    /// Index 9 (DGROUP 0x226): Darstellung des Live-Spiels, 0–2; im Optionsbildschirm beschriftet mit „Torszenen“ (Loch,
    /// Pinguin, Pinguin mit rotem Punkt; DOSBox-Bilder 48–51). [ORIGINAL] Bei 2 bleibt das Eisfeld während des ganzen
    /// Spiels an, Eingriffe sind dann nicht möglich (Meldung „Neuer Spielmodus:“, Datei 0x2D4EE). [APPROX] Die Bedeutung
    /// von 0 und 1 ist nicht belegt; die Auswertung gehört zum Live-Ablauf (Folgepaket <c>livespiel</c>).
    /// </summary>
    public int LiveMode { get; set; }

    /// <summary>Index 10 (DGROUP 0x227): DEB-Pokal anzeigen. [ORIGINAL] Anfangswert an.</summary>
    public bool ShowDebCup { get; set; } = true;

    /// <summary>Index 11 (DGROUP 0x228): Europapokal anzeigen.</summary>
    public bool ShowEuropeanCup { get; set; }

    /// <summary>Index 12 (DGROUP 0x229): WM-Spiele anzeigen.</summary>
    public bool ShowWorldCup { get; set; }

    /// <summary>Spielgeschwindigkeit (DGROUP 0x262), 0–80 in Zehnerschritten.</summary>
    public int MatchSpeed { get; set; } = DefaultMatchSpeed;

    /// <summary>Meldung der Art <paramref name="kind"/> für die Liga <paramref name="league"/> eingeschaltet?</summary>
    public bool ShowsReport(ReportKind kind, League league) => Reports[ReportIndex(kind, league)];

    /// <summary>[ORIGINAL] Index des Matrixfelds: Liga · 3 + Zeile (siehe <see cref="Reports"/>).</summary>
    public static int ReportIndex(ReportKind kind, League league) => (int)league * 3 + (int)kind;

    /// <summary>[ORIGINAL] Klick auf ein Feld der Matrix: <c>xor 1</c> (Datei 0x2D32A ff.).</summary>
    public void ToggleReport(ReportKind kind, League league) =>
        Reports[ReportIndex(kind, league)] ^= true;

    /// <summary>
    /// [ORIGINAL] Klick auf Index 9: <c>inc</c>, bei 3 → 0 (Datei 0x2D32A ff.). Das Original setzt 2 außerdem sofort auf 0
    /// zurück, wenn DGROUP 0x5A56 ≠ 0 – dafür steht <paramref name="permanentRinkBlocked"/> ([APPROX] Bedeutung von 0x5A56
    /// nicht belegt).
    /// </summary>
    public void NextLiveMode(bool permanentRinkBlocked = false)
    {
        LiveMode = (LiveMode + 1) % LiveModeCount;
        if (LiveMode == PermanentRinkLiveMode && permanentRinkBlocked)
            LiveMode = 0;
    }

    /// <summary>Anzeigeschalter des Pokals; für Liga, Play-offs und Aufstiegsrunde <c>null</c> (kein Schalter).</summary>
    public bool? ShowsCup(MatchCompetition competition) => competition switch
    {
        MatchCompetition.DebCup => ShowDebCup,
        MatchCompetition.EuropeanCup => ShowEuropeanCup,
        MatchCompetition.WorldCup => ShowWorldCup,
        _ => null,
    };

    /// <summary>
    /// Weg eines Pokalspiels <b>ohne</b> Managerverein: [ORIGINAL] Spieltagsablauf Datei 0x3702E–0x37085 – bei
    /// eingeschaltetem Anzeigeschalter (Wettbewerbscode DGROUP 0xD0C2 6 → 0x227, 3/5 → 0x228, 8 → 0x229) die Minutenschleife
    /// mit Anzeige (0x37088), sonst der schnelle Weg 0x3731F. Die WM-Endrunde (Code 10) prüft keinen Schalter (immer schnell,
    /// Aufrufer). [APPROX] Play-offs/Aufstiegsrunde (Code 1) hängen im Original am Feld „Ergebnisse“ der Meldungsmatrix
    /// (0x37058–0x37085, voreingestellt aus → schneller Weg); im Remake liefert die Methode für sie <c>false</c> und der
    /// Aufrufer rechnet sie Minute für Minute (R9S-03, #351).
    /// <para>
    /// Aufrufer: <c>MatchEngine.PlayWithEvents</c> (<c>MatchEngine.Live.cs</c>), angeschlossen in der Integration Runde 9.
    /// </para>
    /// </summary>
    public bool CupMatchUsesMinuteLoop(MatchCompetition competition) => ShowsCup(competition) ?? false;

    /// <summary>Spielgeschwindigkeit setzen, auf 0–80 in Zehnerschritten gerundet.</summary>
    public void SetMatchSpeed(int speed) => MatchSpeed = Math.Clamp(speed / 10 * 10, 0, MaxMatchSpeed);
}

/// <summary>Spielstand – Spur optionen-wirkung: Zugriff auf die Spielstand-Optionen.</summary>
public sealed partial class GameState
{
    /// <summary>Spielstand-Optionen (Optionsbildschirm 0x2CD1C), gespeichert mit der Spielvorbereitung.</summary>
    public GameOptions Options => Setup.Options;
}
