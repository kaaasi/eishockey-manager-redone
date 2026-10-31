using EishockeyManager.UI.Screens.Team;

namespace EishockeyManager.UI.Screens;

/// <summary>
/// Sicherheitsfrage von „und tschüß…“ (große Kachel 7 des Hauptmenüs, Befund NB-01, #205). Wird vom Hauptmenü über
/// dessen Bild gelegt; „Leider ja“ beendet das Programm, „Kein Gedanke“ schließt die Frage. Ändert keinen Spielzustand.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>[ORIGINAL] em.exe Datei 0x45C33: Frage 0x120:0x7A mit Überschrift = Name des Managers am Zug (DGROUP 0xA70E),
///   Zeilen DGROUP 0x12E5 „Sie haben also wirklich“, 0x12E9 „etwas besseres zu tun ?“, 0x12ED „Sind Sie sicher ??“ und
///   Knöpfen 0x1319 „Leider ja“ / 0x131D „Kein Gedanke“. Antwort ≠ 0 → Speicher freigeben und Programmende
///   (0x45C91–0x45CCF), sonst zurück in die Hauptmenüschleife (0x4555C → 0x4535B). Aufruf aus dem Hauptmenü bei Klick
///   auf Kachel 7 (Datei 0x45551–0x45559).</item>
/// <item>[ORIGINAL] Text und Knöpfe wie im Bild des Nutzers (DOSBox, 31.10.2026, Kommentar in #205).</item>
/// <item>[APPROX] Lage, Rahmen und „?“-Felder wie die anderen Fragen über 0x120:0x7A (<see cref="Competitions.DebCupDrawLiveScreen"/>,
///   vermessen am Video 3:46); für diese Frage gibt es kein Videobild. Das Nutzerbild zeigt die „?“-Felder gelb, hier
///   sind sie wie in den übrigen Fragen gezeichnet.</item>
/// <item>[APPROX] Escape und rechte Maustaste wie „Kein Gedanke“ (Tastenbelegung von 0x120:0x7A nicht geprüft). Keine
///   Taste beendet das Programm.</item>
/// </list>
/// </remarks>
public sealed class QuitQuestion
{
    public enum Outcome { None, Quit, Cancel }

    public static readonly string[] Lines = ["Sie haben also wirklich", "etwas besseres zu tun ?", "Sind Sie sicher ??"];
    public const string YesText = "Leider ja";
    public const string NoText = "Kein Gedanke";

    public static Rect Box => TransferDialog.QuestionBox;
    public static Rect YesButton => Widgets.ButtonRect(94, 166);
    public static Rect NoButton => Widgets.ButtonRect(164, 166);

    public QuitQuestion(string managerName) => Title = managerName;

    /// <summary>Überschrift: Name des Managers am Zug.</summary>
    public string Title { get; }

    public static Outcome Evaluate(InputState input)
    {
        if (input.Clicked(YesButton))
            return Outcome.Quit;
        if (input.Clicked(NoButton) || input.RightClick || input.Pressed(Key.Escape))
            return Outcome.Cancel;
        return Outcome.None;
    }

    public void Draw(Surface s, Assets a)
    {
        var b = Box;
        var cx = b.X + b.W / 2 - 2;
        Widgets.Frame(s, a, b, FrameStyle.Orange);
        QuestionMark(s, a, b.X + 9, b.Y + 3);
        QuestionMark(s, a, b.Right - 30, b.Y + 3);
        Widgets.ShadowText(s, a.Flow, cx, b.Y + 12, Title, Colors.White, Colors.Black);
        s.HLine(b.X + 34, b.Y + 24, b.W - 68, Colors.White);
        for (var i = 0; i < Lines.Length; i++)
            TeamUi.LabelCentered(s, a, cx, b.Y + 30 + 8 * i, Lines[i], Colors.White, Colors.Black);
        s.HLine(b.X + 5, b.Y + 56, b.W - 10, TeamUi.DialogDark);
        s.HLine(b.X + 5, b.Y + 57, b.W - 10, 37);
        TeamUi.DialogButton(s, a, YesButton.X, YesButton.Y, YesText);
        TeamUi.DialogButton(s, a, NoButton.X, NoButton.Y, NoText);
    }

    /// <summary>„?“-Feld (22 × 22) wie in <see cref="Competitions.DebCupDrawLiveScreen"/> (dort privat).</summary>
    private static void QuestionMark(Surface s, Assets a, int x, int y)
    {
        var r = new Rect(x, y, 22, 22);
        s.Fill(r, 14);
        s.Outline(r, 12);
        s.HLine(x + 1, y + 1, 20, 15);
        s.VLine(x + 1, y + 1, 20, 15);
        s.TextCentered(a.Bold, x + 11, y + 4, "?", Colors.Black);
    }
}
