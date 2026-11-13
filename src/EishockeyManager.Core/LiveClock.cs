namespace EishockeyManager.Core;

/// <summary>
/// Was die Live-Anzeige in einer Minute tut (Spur livespiel-kern, Teil A2): Augenzwinkern des Reporters und Pause.
/// </summary>
/// <param name="Wink">Zwinkern 0x130:0x39 vor der Pause.</param>
/// <param name="PauseUnits">Argument n der Pause <c>50h:19Eh</c> (0 = keine Pause).</param>
/// <param name="Ticks">BIOS-Ticks (18,2 Hz) der Pause: n / 3 – das Modell der Bilder je Minute (keine Wanduhr).</param>
public readonly record struct LiveMinute(bool Wink, int PauseUnits, int Ticks);

/// <summary>
/// Ablaufmodell der Live-Anzeige (Spur livespiel-kern, Teil A2; <c>docs/analyse/re-livespiel.md</c> §1, §10, §13). Nur
/// Modell: Der Bildschirm fragt je Minute, wie lange er wartet und ob er zwinkert; Zeit vergeht im Spiel weiter nur über
/// <see cref="GameState.Continue"/>.
/// <list type="bullet">
/// <item>[ORIGINAL] Spieltagssicherung: Der Spieltagsablauf 0x36B5D setzt die Lampe DGROUP 0xE19A = 0 und sichert
///   Geschwindigkeit DGROUP 0x262 in 0xE19B und Darstellung DGROUP 0x226 in 0xE19C (Datei 0x36B65–0x36B77); am Ende
///   schreibt er beide zurück (Datei 0x37833–0x3783E) – <see cref="BeginMatchDay"/>, <see cref="EndMatchDay"/>.</item>
/// <item>[ORIGINAL] Lampe „Schnelldurchlauf“ (Eingriffsmenü Datei 0x38DBB–0x38E19): umschalten; an → Geschwindigkeit 70
///   und Darstellung 0 (Torszenen aus), aus → beide aus der Sicherung (<see cref="ToggleLamp"/>).</item>
/// <item>[ORIGINAL] Spielmodus-Optionen mit der Leertaste (Datei 0x37D29–0x37D5B): vorher Sicherung zurück, danach neue
///   Sicherung und Lampe aus (<see cref="BeforeOptions"/>, <see cref="AfterOptions"/>).</item>
/// <item>[ORIGINAL] Je Minute (Datei 0x37C6B–0x37CBD), nur mit Live-Anzeige und Geschwindigkeit &lt; 70: mit lrandom(0, 4) = 0
///   Zwinkern und Pause 30 − g/2, sonst Pause 45 − g/2 (g = Geschwindigkeit, <c>sar</c>). Ab 70 Schnelldurchlauf: kein
///   Zug, keine Pause (<see cref="Minute"/>). DGROUP 0x1DB („Durchlauf ohne Halt“) prüft das Original dort auch; im Spielcode
///   wird es nur gelesen (Anfangswert 0), es ist hier nicht geführt.</item>
/// <item>[ORIGINAL] Pause <c>50h:19Eh(n)</c> = Datei 0x130EE: n / 3 (Langwort-Division 0x227C) BIOS-Ticks abwarten.</item>
/// <item>[ORIGINAL] Nach Minute 60 bzw. 70 (Drittelende, Datei 0x3855F–0x38587): mit Live-Anzeige und Geschwindigkeit
///   &lt; 70 auf einen Mausklick warten (<see cref="WaitsForClick"/>); nach Minute 20, 40 und 65 nicht.</item>
/// <item>Zwinkerzufall: [ORIGINAL] lrandom (Datei 0xBFD5): x = (16 · x + 0xA7) mod 4 000 000 (Langwort, Zustand DGROUP
///   0xE017/0xE019, Anfangswert 0), Ergebnis x · (b − a + 1) / 4 000 000 + a (<see cref="LRandom"/>). [APPROX] Im Original
///   teilt sich das Zwinkern den Zustand mit der Spielrechnung (lrandom auch in 0x87F7, 0x9F5C, 0xA484, 0x35818); das
///   Remake zieht die Spielrechnung aus <see cref="GameState.Random"/> und hält das Zwinkern getrennt (Review #795 P3-1,
///   Rundenplan 18), damit die Anzeige keinen Spielwert verschiebt.</item>
/// </list>
/// Darstellungszustand, nicht im Spielstand.
/// </summary>
public sealed class LiveClock
{
    /// <summary>[ORIGINAL] Ab dieser Geschwindigkeit Schnelldurchlauf (Datei 0x37C7A <c>cmp [0x262], 0x46</c>); die Lampe setzt genau sie.</summary>
    public const int FastSpeed = 70;

    /// <summary>[ORIGINAL] Grundpause je Minute (Datei 0x37CAE, 0x2D) bzw. mit Zwinkern (Datei 0x37CA3, 0x1E).</summary>
    public const int PauseBase = 45;

    public const int WinkPauseBase = 30;

    /// <summary>[ORIGINAL] Pause: n / 3 BIOS-Ticks (Datei 0x130F3).</summary>
    public const int UnitsPerTick = 3;

    private readonly GameOptions options;

    public LiveClock(GameOptions options, LRandom? wink = null)
    {
        this.options = options;
        WinkRandom = wink ?? new LRandom();
    }

    /// <summary>Lampe „Schnelldurchlauf“ (DGROUP 0xE19A).</summary>
    public bool Lamp { get; private set; }

    /// <summary>Gesicherte Geschwindigkeit (DGROUP 0xE19B).</summary>
    public int SavedSpeed { get; private set; }

    /// <summary>Gesicherte Darstellung (DGROUP 0xE19C).</summary>
    public int SavedLiveMode { get; private set; }

    /// <summary>Generator des Zwinkerns (siehe Klasse).</summary>
    public LRandom WinkRandom { get; }

    /// <summary>Schnelldurchlauf: Geschwindigkeit ≥ 70 (keine Pause, keine Reporter-Meldung, kein Klick am Ende).</summary>
    public bool Fast => options.MatchSpeed >= FastSpeed;

    /// <summary>[ORIGINAL] Datei 0x36B65–0x36B77: Lampe aus, Geschwindigkeit und Darstellung sichern.</summary>
    public void BeginMatchDay()
    {
        Lamp = false;
        Save();
    }

    /// <summary>[ORIGINAL] Datei 0x37833–0x3783E: Geschwindigkeit und Darstellung aus der Sicherung zurück.</summary>
    public void EndMatchDay() => Restore();

    /// <summary>[ORIGINAL] Datei 0x38DBB–0x38E19: Lampe umschalten; an → 70 und Darstellung 0, aus → Sicherung.</summary>
    public void ToggleLamp()
    {
        Lamp = !Lamp;
        if (Lamp)
        {
            options.MatchSpeed = FastSpeed;
            options.LiveMode = 0;
        }
        else
        {
            Restore();
        }
    }

    /// <summary>[ORIGINAL] Datei 0x37D29–0x37D34: vor den Spielmodus-Optionen die Sicherung zurückschreiben.</summary>
    public void BeforeOptions() => Restore();

    /// <summary>[ORIGINAL] Datei 0x37D41–0x37D5B: nach den Optionen neu sichern, Lampe aus.</summary>
    public void AfterOptions()
    {
        Save();
        Lamp = false;
    }

    /// <summary>
    /// Anzeige einer Minute (vor der Rechnung der Minute, Datei 0x37C6B–0x37CBD). Ohne Live-Anzeige
    /// (<paramref name="display"/> = DGROUP 0x5A56 bzw. Argument der Drittelschleife) und im Schnelldurchlauf nichts und
    /// kein Zufallszug.
    /// </summary>
    public LiveMinute Minute(bool display)
    {
        if (!display || Fast)
            return new LiveMinute(false, 0, 0);
        var wink = WinkRandom.Next(0, 4) == 0;
        var units = (wink ? WinkPauseBase : PauseBase) - (options.MatchSpeed >> 1);
        return new LiveMinute(wink, units, units / UnitsPerTick);
    }

    /// <summary>[ORIGINAL] Datei 0x3855F–0x38587: nach dem Drittel bis 60 bzw. 70 mit Anzeige und unter 70 auf einen Klick warten.</summary>
    public bool WaitsForClick(int periodEnd, bool display) =>
        display && periodEnd is 60 or 70 && !Fast;

    private void Save()
    {
        SavedSpeed = options.MatchSpeed;
        SavedLiveMode = options.LiveMode;
    }

    private void Restore()
    {
        options.MatchSpeed = SavedSpeed;
        options.LiveMode = SavedLiveMode;
    }
}

/// <summary>
/// [ORIGINAL] lrandom des Originals (Datei 0xBFD5, <c>38h:7E5h</c>): Langwort-Zustand x (DGROUP 0xE017/0xE019, Anfangswert 0),
/// x = ((x &lt;&lt; 4) + 0xA7) mod 0x3D0900 (Rest 0x228B), Ergebnis x · (b − a + 1) / 0x3D0900 + a (Multiplikation 0x21CA,
/// Division 0x227C), beide Grenzen eingeschlossen.
/// </summary>
public sealed class LRandom(int state = 0)
{
    /// <summary>[ORIGINAL] Modul 0x3D0900 = 4 000 000.</summary>
    public const int Modulus = 0x3D0900;

    public int State { get; private set; } = state;

    public int Next(int min, int max)
    {
        State = (int)(((long)State * 16 + 0xA7) % Modulus);
        return (int)((long)State * (max - min + 1) / Modulus) + min;
    }
}
