using EishockeyManager.Core;

namespace EishockeyManager.UI.Screens.Live;

/// <summary>Ereignis einer Reporter-Meldung ([ORIGINAL] Argument 2 von 0x52520, Sprungtabellen Datei 0x52B75/0x52B83).</summary>
public enum ReporterEvent
{
    ChanceMissed = 0,
    Goal = 1,
    Minor = 2,
    Major = 3,
    Injury = 4,
    PenaltyShotMissed = 5,
    PenaltyShotGoal = 6,
}

/// <summary>Eine Zeile im Reporterkasten (Hilfsfläche DGROUP 0xC137, 111 × 32): Text zentriert zwischen x1 und x2.</summary>
public readonly record struct ReporterLine(bool Large, string Text, int X1, int X2, int Baseline, byte Color, byte? Shadow);

/// <summary>
/// Reporter der Live-Anzeige (Spur livespiel-ui-2, B2; L4; <c>docs/analyse/re-livespiel.md</c> §5 und §15): Meldung
/// 0x52520 mit Mundfolge 0x5224E, Zeile sprechen 0x52430, Mundbewegung 0x52382 und Zwinkern 0x522EC als Bildfolge. Jede
/// Meldung ist ein Ablauf von Bildern (<see cref="Message"/>); der Bildschirm zeichnet nach jedem Bild <see cref="Lines"/>,
/// <see cref="Mouth"/> und <see cref="EyesClosed"/>.
/// <list type="bullet">
/// <item>[ORIGINAL] Pause <c>50h:19Eh(n)</c> = n / 3 BIOS-Ticks (Datei 0x130EE), hier <see cref="ShortMessage.FramesFor"/>; ein
///   Strahlrücklauf <c>38h:10Eh</c> = ein Bild ([APPROX] 70 Hz des VGA-Modus auf 60 Bilder/s).</item>
/// <item>[ORIGINAL] Argumente des Textes 0x30:0x885 (Datei 0x37BAA zum Vergleich: Schatten, Farbe, Grundlinie, x2, x1 in dieser
///   Reihenfolge auf dem Stapel): Kopftext Schrift 1 x 0–111, Grundlinie 20, Farbe 0x18, Schatten 0 (Datei 0x525F3–0x52611);
///   Ereignistext Grundlinie 22 (Datei 0x52871–0x5288F); Vereinsname Schrift 3, Grundlinie 30, Farbe 7, Schatten 0 (Datei
///   0x52744–0x52764); Zusammenfassung Schrift 3: Ereignistext Grundlinie 10, Spieler 18, zweite Zeile 26, je Farbe 0x18
///   (Datei 0x529BC–0x529DA, 0x52AD4–0x52B1D).</item>
/// </list>
/// </summary>
public sealed class LiveReporter
{
    /// <summary>[ORIGINAL] Vokaltabelle DGROUP 0x541D „25AEIOU“ (die Ziffern für „2 Minuten“, „5 Minuten“).</summary>
    public const string Vowels = "25AEIOU";

    /// <summary>[ORIGINAL] Datei 0x52286: höchstens 28 Mundöffnungen (0x1C).</summary>
    public const int MaxOpenings = 28;

    /// <summary>[ORIGINAL] Datei 0x522D1: mit 9 aufgefüllt bis Eintrag 8, dann 0xFF.</summary>
    public const int PaddedLength = 8;

    public const byte Open = 0;
    public const byte Rest = 9;
    public const byte End = 0xFF;

    /// <summary>Kasten des Reporters auf dem Bildschirm ([ORIGINAL] Datei 0x52659–0x52668: (199 | 183) 111 × 32).</summary>
    public static readonly Rect Box = new(199, 183, 111, 32);

    /// <summary>[ORIGINAL] Datei 0x52382 und 0x522EC: Mund (154 | 207) 12 × 17, Augen (154 | 203) 12 × 4.</summary>
    public static readonly Rect MouthTarget = new(154, 207, 12, 17);

    public static readonly Rect EyesTarget = new(154, 203, 12, 4);

    /// <summary>[ORIGINAL] kb\38: Mund geschlossen (80 | 4), offene Münder (92 + 12k | 4), Augen offen (80 | 0), zu (128 | 0).</summary>
    public static readonly Rect MouthClosedSource = new(80, 4, 12, 17);

    public static Rect MouthOpenSource(int k) => new(92 + 12 * k, 4, 12, 17);

    public static readonly Rect EyesOpenSource = new(80, 0, 12, 4);
    public static readonly Rect EyesClosedSource = new(128, 0, 12, 4);

    /// <summary>[ORIGINAL] Sieben offene Münder je Mundbewegung (Datei 0x5239C–0x52429).</summary>
    public const int MouthFrames = 7;

    /// <summary>[ORIGINAL] Texte DGROUP 0x5456 ff. (Kopf- und Ereignistexte), „Vorlage: “ 0x54AC, „wegen “ 0x194C.</summary>
    public static string HeaderText(ReporterEvent e) => e switch
    {
        ReporterEvent.ChanceMissed or ReporterEvent.Goal => "Chance:",
        ReporterEvent.Minor or ReporterEvent.Major => "Strafzeit:",
        ReporterEvent.Injury => "Verletzt:",
        _ => "Penalty:",
    };

    public static string EventText(ReporterEvent e) => e switch
    {
        ReporterEvent.ChanceMissed or ReporterEvent.PenaltyShotMissed => "Vergeben",
        ReporterEvent.Goal or ReporterEvent.PenaltyShotGoal => "TOR!",
        ReporterEvent.Minor => "2 Minuten",
        ReporterEvent.Major => "5 Minuten",
        _ => "",
    };

    private readonly GameOptions _options;
    private readonly LRandom _wink;
    private readonly List<ReporterLine> _lines = [];

    public LiveReporter(GameOptions options, LRandom wink)
    {
        _options = options;
        _wink = wink;
    }

    /// <summary>Inhalt des Kastens (leer = nur der Hintergrund aus kb\19).</summary>
    public IReadOnlyList<ReporterLine> Lines => _lines;

    /// <summary>Offener Mund 0 … 6 oder −1 (geschlossen).</summary>
    public int Mouth { get; private set; } = -1;

    public bool EyesClosed { get; private set; }

    /// <summary>Läuft gerade eine Meldung?</summary>
    public bool Busy { get; private set; }

    /// <summary>
    /// [ORIGINAL] Mundfolge Datei 0x5224E: Zeichen der Reihe nach, Kleinbuchstaben 0x61–0x7A als Großbuchstaben; steht das
    /// Zeichen in <see cref="Vowels"/> und sind weniger als 28 Einträge da, wird 0 angehängt und das folgende Zeichen
    /// übersprungen (Datei 0x5228C). Danach mit 9 bis Eintrag 8 auffüllen (Datei 0x522C2–0x522D5, nicht acht Einträge 9
    /// anhängen) und 0xFF anhängen.
    /// </summary>
    public static byte[] MouthSequence(string text)
    {
        var codes = Formats.GameText.Encode(text);
        var seq = new List<byte>();
        for (var i = 0; i < codes.Length; i++)
        {
            var c = codes[i];
            if (c is >= 0x61 and <= 0x7A)
                c -= 0x20;
            if (Vowels.Contains((char)c) && seq.Count < MaxOpenings)
            {
                i++;
                seq.Add(Open);
            }
        }
        while (seq.Count < PaddedLength)
            seq.Add(Rest);
        seq.Add(End);
        return [.. seq];
    }

    /// <summary>Bilder einer Pause n ([ORIGINAL] 0x130EE: n / 3 Ticks); n ≤ 0 → keine Pause (siehe <see cref="LiveMatchScreen"/>, #823).</summary>
    public static int PauseFrames(int units) => units <= 0 ? 0 : ShortMessage.FramesFor(units);

    private int Speed => _options.MatchSpeed;

    private static IEnumerable<bool> Pause(int units)
    {
        for (var i = 0; i < PauseFrames(units); i++)
            yield return true;
    }

    /// <summary>[ORIGINAL] 0x52382: bei Geschwindigkeit &gt; 40 nichts; sieben offene Münder je ein Bild, dann geschlossen.</summary>
    private IEnumerable<bool> MoveMouth()
    {
        if (Speed > 40)
            yield break;
        for (var k = 0; k < MouthFrames; k++)
        {
            Mouth = k;
            yield return true;
        }
        Mouth = -1;
    }

    /// <summary>[ORIGINAL] 0x522EC: Augen zu, Pause 3, Augen auf.</summary>
    public IEnumerable<bool> Wink()
    {
        EyesClosed = true;
        yield return true;
        foreach (var f in Pause(3))
            yield return f;
        EyesClosed = false;
    }

    /// <summary>[ORIGINAL] 0x52430: Zeile in den Kasten, Mundfolge, mit lrandom(0, 1) = 0 Zwinkern, ab Geschwindigkeit &gt; 40 fertig.</summary>
    private IEnumerable<bool> Speak(ReporterLine line)
    {
        _lines.Add(line);
        yield return true;
        var seq = MouthSequence(line.Text);
        if (_wink.Next(0, 1) == 0)
            foreach (var f in Wink())
                yield return f;
        if (Speed > 40)
            yield break;
        foreach (var entry in seq)
        {
            if (entry == End)
                break;
            var frames = entry == Open ? MoveMouth() : Pause(6);
            foreach (var f in frames)
                yield return f;
        }
    }

    /// <summary>
    /// Meldung 0x52520 (§5): Verein, Ereignis, Spieler (null = 0xFF), Zweiter (Vorlage) und Text (Verletzungsart bzw.
    /// Strafgrund aus dem Spielkern, <see cref="GoalSceneSelection.Reasons"/>). Ohne Live-Anzeige oder ab Geschwindigkeit 70
    /// nichts ([ORIGINAL] Datei 0x5252A–0x5253B).
    /// </summary>
    public IEnumerable<bool> Message(string club, ReporterEvent e, string? player, string? second, string? text)
    {
        if (Speed >= LiveClock.FastSpeed)
            yield break;
        Busy = true;
        _lines.Clear();
        var g = Speed;
        if (_options.LiveMode == 0)
        {
            // [ORIGINAL] Datei 0x52567–0x526DB: Kopftext zweimal aufblinken, Mundfolge des Kopftexts.
            var header = HeaderText(e);
            var seq = MouthSequence(header);
            if (_wink.Next(0, 1) == 0)
                foreach (var f in Wink())
                    yield return f;
            var count = g == 60 ? 2 : 4;
            for (var i = 0; i < count; i++)
            {
                _lines.Clear();
                if (i % 2 != 0)
                    _lines.Add(new ReporterLine(true, header, 0, 111, 20, 0x18, 0));
                foreach (var f in Pause(g == 50 ? 5 : 0))
                    yield return f;
                yield return true;
                foreach (var f in Pause(2))
                    yield return f;
                foreach (var f in seq[i] == Open ? MoveMouth() : Pause(4))
                    yield return f;
            }
        }
        // [ORIGINAL] Datei 0x526FE–0x52791: Schrift 3, Pause 80 − g, Vereinsname sprechen, bei 50/60 Pause 25.
        foreach (var f in Pause(80 - g))
            yield return f;
        foreach (var f in Speak(new ReporterLine(false, club, 0, 111, 30, 7, 0)))
            yield return f;
        if (g is 50 or 60)
            foreach (var f in Pause(25))
                yield return f;
        var lastText = HeaderText(e);
        if (e != ReporterEvent.Injury)
        {
            lastText = EventText(e);
            if (_options.LiveMode == 0)
            {
                var seq = MouthSequence(lastText);
                var count = g == 60 ? 1 : 4;
                for (var i = 0; i < count; i++)
                {
                    _lines.Clear();
                    if (i % 2 == 0)
                        _lines.Add(new ReporterLine(true, lastText, 0, 111, 22, 0x18, 0));
                    foreach (var f in Pause(g == 50 ? 5 : 0))
                        yield return f;
                    yield return true;
                    foreach (var f in Pause((g == 50 ? 1 : 0) + 1))
                        yield return f;
                    foreach (var f in seq[i] == Open ? MoveMouth() : Pause(4))
                        yield return f;
                }
            }
        }
        // [ORIGINAL] Datei 0x5296B–0x52B6E: Zusammenfassung in Schrift 3.
        _lines.Clear();
        _lines.Add(new ReporterLine(false, lastText, 0, 111, 10, 0x18, 0));
        var second2 = e switch
        {
            ReporterEvent.ChanceMissed or ReporterEvent.Goal when second is not null => "Vorlage: " + second,
            ReporterEvent.Injury when text is not null => text,
            ReporterEvent.Minor or ReporterEvent.Major => "wegen " + text,
            _ => null,
        };
        if (player is not null)
            foreach (var f in Speak(new ReporterLine(false, player, 0, 111, 18, 0x18, 0)))
                yield return f;
        if (player is not null && second2 is not null)
            foreach (var f in Speak(new ReporterLine(false, second2, 0, 111, 26, 0x18, 0)))
                yield return f;
        foreach (var f in Pause(45 - 10 * (g > 49 ? 1 : 0)))
            yield return f;
        _lines.Clear();
        Mouth = -1;
        EyesClosed = false;
        Busy = false;
        yield return true;
    }
}
