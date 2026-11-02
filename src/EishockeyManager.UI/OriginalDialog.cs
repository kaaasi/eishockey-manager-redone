using EishockeyManager.UI.Screens.Team;

namespace EishockeyManager.UI;

/// <summary>Art des Fensters von em.exe Datei 0x4C246: Meldung (Zeichen „!“) oder Frage (Zeichen „?“).</summary>
public enum OriginalDialogKind
{
    Message,
    Question,
}

/// <summary>
/// Meldungs- und Fragefenster des Originals als ein Baustein (Spur <c>ui-meldungen</c>, M-K1, Inventur Nr. 9).
/// Zeichnet und wertet aus, ändert aber nie den Spielzustand; der Aufrufer entscheidet nach <see cref="Update"/>.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>[ORIGINAL] em.exe Datei 0x4C246 (Stub 0x120:0x7A, <c>docs/analyse/re-meldungen.md</c> §C): Fenster 7 × 4 Rahmenteile
///   ab (75 | 107) (<see cref="Window"/>); Überschrift Schrift 2 (Flow) zentriert x 105 … 210, Grundlinie 125, Farbe 7, Schatten 0;
///   zwei Symbolfelder bei x = 83 und 212, y = 112 (22 × 22, Fläche 17/14, Umriss 16/12 um ein Pixel eingerückt, Zeichen
///   „!“/„?“ in Schrift 1, Grundlinie 129, Farbe 0); Linie oben (109 | 131)–(207 | 131) Farbe 7; Textzeilen Schrift 3 (Mini),
///   Grundlinien 141 + 8 · i (ohne dritte Zeile 145, 153; Meldung mit vierter Zeile 139/146/153/160, ohne dritte 4 tiefer);
///   Linie unten (80 | 163)–(237 | 163) Farbe 0; Knöpfe (Code 95/165 | 166, im Video 94/164), nur wenn Zeile 3 oder 4 gefüllt ist.</item>
/// <item>[ORIGINAL] Eingabe: nur die linke Maustaste (Disassembly 0x4C246: Mausereignis 1 über 0x0C068). Eine Meldung schließt
///   bei jedem Linksklick, eine Frage nur durch einen Klick auf einen Knopf. Tastatur und rechte Maustaste werden nicht
///   ausgewertet; Komfort-Tasten einer Frage (Escape, Enter) bleiben Sache des Aufrufers (Entscheidung Nutzer, #205, A273/A334).</item>
/// <item>[ORIGINAL] Rückgabe: Frage linker Knopf 1, rechter 0; Meldung 0, bei Klick auf den linken Knopf 1.</item>
/// <item>[ORIGINAL] Knopftexte: links Zeile 4 (fehlt sie: Zeile 5), rechts Zeile 5 (0x4C246 + 0x29C … 0x344). Bei einer
///   Meldung ohne vierte Zeile tragen also beide Knöpfe den Text der fünften Zeile.</item>
/// <item>[APPROX] Lage der Knöpfe: Im Code stehen (95 | 166) und (165 | 166); im Video 3:46 beginnt die Knopfgrafik 1 Pixel weiter links
///   (Pixelvergleich, Versatz −1 | 0), daher gezeichnet und getestet ab 94 bzw. 164. Die Beschriftung der Knöpfe (kb\2 mit dunkler Schrift und hellem Schatten) übernimmt
///   <see cref="TeamUi.DialogButton"/>; Schriftlage der Textzeilen mit Schatten 1 Pixel nach rechts unten.</item>
/// </list>
/// </remarks>
public sealed class OriginalDialog
{
    /// <summary>Überschrift bei leerem Managernamen.</summary>
    public const string DefaultTitle = "An Alle:";

    public static readonly Rect Window = new(75, 107, 168, 76);
    public static readonly Rect LeftButton = Widgets.ButtonRect(94, 166);
    public static readonly Rect RightButton = Widgets.ButtonRect(164, 166);

    private const int TitleCenter = 157; // (105 + 210) / 2, abgerundet
    private const int TitleBaseline = 125;
    private const int TitleTop = TitleBaseline - 6;   // Flow: Oberkante 6 Pixel über der Grundlinie
    private const int TextTopOffset = 4;               // Mini: Oberkante 4 Pixel über der Grundlinie
    private const int SymbolTop = 114;                 // Bold: Oberkante 15 Pixel über der Grundlinie 129 (Video: Zeichen y 115 … 129)

    public OriginalDialog(OriginalDialogKind kind, string title, string line1, string line2 = "", string line3 = "",
        string line4 = "", string line5 = "")
    {
        Kind = kind;
        Title = title.Length == 0 ? DefaultTitle : title;
        Line1 = line1;
        Line2 = line2;
        Line3 = line3;
        Line4 = line4;
        Line5 = line5;
    }

    public OriginalDialogKind Kind { get; }
    public string Title { get; }
    public string Line1 { get; }
    public string Line2 { get; }
    public string Line3 { get; }
    public string Line4 { get; }
    public string Line5 { get; }

    /// <summary>Beide Knöpfe werden nur gezeichnet, wenn Zeile 3 oder Zeile 4 gefüllt ist.</summary>
    public bool HasButtons => Line3.Length > 0 || Line4.Length > 0;

    /// <summary>Beschriftung des linken Knopfs: Zeile 4, sonst Zeile 5.</summary>
    public string LeftText => Line4.Length > 0 ? Line4 : Line5;

    /// <summary>Beschriftung des rechten Knopfs: Zeile 5.</summary>
    public string RightText => Line5;

    /// <summary>Gezeichnete Textzeilen (Meldung mit Zeile 4: vier Zeilen, Zeile 4 steht dann als Text und als linker Knopf).</summary>
    public IReadOnlyList<string> TextLines
    {
        get
        {
            var lines = new List<string>();
            if (Line1.Length > 0) lines.Add(Line1);
            if (Line2.Length > 0) lines.Add(Line2);
            if (Line3.Length > 0) lines.Add(Line3);
            if (Kind == OriginalDialogKind.Message && Line4.Length > 0) lines.Add(Line4);
            return lines;
        }
    }

    /// <summary>
    /// Grundlinien der Textzeilen: Frage 141 + 8 · i, ohne dritte Zeile 4 tiefer; Meldung mit vierter Zeile Beginn 139
    /// und Abstand 7 (ohne dritte Zeile wieder 4 tiefer).
    /// </summary>
    public IReadOnlyList<int> Baselines
    {
        get
        {
            var count = TextLines.Count;
            var fourLines = Kind == OriginalDialogKind.Message && Line4.Length > 0;
            var start = fourLines ? 139 : 141;
            var step = fourLines ? 7 : 8;
            if (Line3.Length == 0)
                start += 4;
            return Enumerable.Range(0, count).Select(i => start + step * i).ToArray();
        }
    }

    /// <summary>Linke obere Ecke des Symbolfelds i (0 links, 1 rechts).</summary>
    public static Rect SymbolField(int i) => new(83 + 129 * i, 112, 22, 22);

    /// <summary>Wert nach dem Schließen: Frage 1 = linker Knopf, 0 = rechter; Meldung 0, linker Knopf 1.</summary>
    public int? Result { get; private set; }

    public bool Closed => Result is not null;

    /// <summary>Wertet einen Frame aus. Nur die linke Maustaste zählt (Original 0x4C246).</summary>
    public void Update(InputState input)
    {
        if (Closed || !input.LeftClick)
            return;
        var left = HasButtons && input.Hover(LeftButton);
        var right = HasButtons && input.Hover(RightButton);
        if (Kind == OriginalDialogKind.Question)
        {
            if (left)
                Result = 1;
            else if (right)
                Result = 0;
        }
        else
            Result = left ? 1 : 0;
    }

    public void Draw(Surface s, Assets a)
    {
        if (Closed)
            return;
        Widgets.Frame(s, a, Window, FrameStyle.Orange);
        Widgets.ShadowText(s, a.Flow, TitleCenter, TitleTop, Title, Colors.White, Colors.Black);
        for (var i = 0; i < 2; i++)
            Symbol(s, a, SymbolField(i));
        s.HLine(109, 131, 99, Colors.White);

        var lines = TextLines;
        var baselines = Baselines;
        for (var i = 0; i < lines.Count; i++)
            TeamUi.LabelCentered(s, a, TitleCenter, baselines[i] - TextTopOffset, lines[i], Colors.White, Colors.Black);

        s.HLine(80, 163, 158, Colors.Black);
        if (HasButtons)
        {
            TeamUi.DialogButton(s, a, LeftButton.X, LeftButton.Y, LeftText);
            TeamUi.DialogButton(s, a, RightButton.X, RightButton.Y, RightText);
        }
    }

    private void Symbol(Surface s, Assets a, Rect field)
    {
        var question = Kind == OriginalDialogKind.Question;
        s.Fill(field, question ? (byte)14 : (byte)17);
        s.Outline(new Rect(field.X + 1, field.Y + 1, field.W - 2, field.H - 2), question ? (byte)12 : (byte)16);
        // Zeichen zwischen x0 und x0 + 23 (Frage + 24)
        var center = field.X + (question ? 12 : 11);
        s.TextCentered(a.Bold, center, SymbolTop, question ? "?" : "!", Colors.Black);
    }
}

/// <summary>
/// Kurzmeldung ohne Knopf und ohne Eingabe, die nach einer Zeit von selbst schließt (M-K2).
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>[ORIGINAL] em.exe Datei 0x0C0B8 (<c>docs/analyse/re-meldungen.md</c> §B): Meldung 0x4C246 mit Modus m = 75 − 30 ·
///   [Zeile 1 beginnt mit „Auto-“]; Modus ≥ 2 heißt: keine Knöpfe, keine Eingabe, Pause m / 3 BIOS-Takte (25 bzw. 15 Takte
///   zu 18,2 Hz ≈ 1,4 s bzw. 0,8 s). Fehlt Zeile 2, steht dort „einlegen.“. Rückgabe immer 1.</item>
/// <item>[APPROX] Die Takt-Umrechnung nimmt 18,2 Hz an (BIOS-Zähler; ob 0x11FD den Zähler beschleunigt, ist nicht geprüft,
///   <c>re-meldungen.md</c> „Offen“) und 60 Bilder je Sekunde des Remakes.</item>
/// <item>[APPROX] Die Meldung ist eine Meldung wie <see cref="OriginalDialogKind.Message"/> ohne Zeilen 3–5, also ohne Knopf; die
///   Überschrift ist der Name des Managers am Zug (Aufrufer).</item>
/// </list>
/// </remarks>
public sealed class ShortMessage
{
    public const double TicksPerSecond = 18.2;
    public const int FramesPerSecond = 60;

    private readonly OriginalDialog _dialog;
    private int _frames;

    public ShortMessage(string title, string line1, string line2 = "")
    {
        Mode = Duration(line1);
        _dialog = new OriginalDialog(OriginalDialogKind.Message, title, line1, line2.Length > 0 ? line2 : "einlegen.");
        TotalFrames = FramesFor(Mode);
    }

    /// <summary>Modus der Meldung (Zeit in 1/18,2-Sekunden-Takten · 3): 75, bei „Auto-…“ 45.</summary>
    public int Mode { get; }

    /// <summary>Bilder, die die Meldung steht.</summary>
    public int TotalFrames { get; }

    public bool Closed => _frames >= TotalFrames;

    public string Line1 => _dialog.Line1;
    public string Line2 => _dialog.Line2;

    public static int Duration(string line1) => line1.StartsWith("Auto-", StringComparison.Ordinal) ? 45 : 75;

    /// <summary>Bilder für Modus m: Pause m / 3 Takte (Ganzzahldivision wie 0x130EE), Takt 18,2 Hz.</summary>
    public static int FramesFor(int mode) => (int)Math.Round(mode / 3 / TicksPerSecond * FramesPerSecond);

    /// <summary>Ein Bild weiter; Eingaben haben keine Wirkung. Liefert true, solange die Meldung offen ist (sie verbraucht die Eingabe).</summary>
    public bool Update(InputState input)
    {
        if (Closed)
            return false;
        _frames++;
        return true;
    }

    public void Draw(Surface s, Assets a)
    {
        if (!Closed)
            _dialog.Draw(s, a);
    }
}
