namespace EishockeyManager.UI.Screens;

/// <summary>
/// Sicherheitsfrage von „und tschüß…“ (große Kachel 7 des Hauptmenüs, Befund NB-01, #205). Wird vom Hauptmenü über
/// dessen Bild gelegt; „Leider ja“ beendet das Programm, „Kein Gedanke“ schließt die Frage. Ändert keinen Spielzustand.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>[ORIGINAL] em.exe Datei 0x45C33: Frage 0x120:0x7A mit dem Index des Managers am Zug (Byte DGROUP 0xA70E) als erstem Argument; Überschrift = dessen Name (Nutzerbild, Review HB-02),
///   Zeilen DGROUP 0x12E5 „Sie haben also wirklich“, 0x12E9 „etwas besseres zu tun ?“, 0x12ED „Sind Sie sicher ??“ und
///   Knöpfen 0x1319 „Leider ja“ / 0x131D „Kein Gedanke“. Antwort ≠ 0 → Speicher freigeben und Programmende
///   (0x45C91–0x45CCF), sonst zurück in die Hauptmenüschleife (0x4555C → 0x4535B). Aufruf aus dem Hauptmenü bei Klick
///   auf Kachel 7 (Datei 0x45551–0x45559).</item>
/// <item>[ORIGINAL] Text und Knöpfe wie im Bild des Nutzers (DOSBox, 31.10.2026, Kommentar in #205).</item>
/// <item>[ORIGINAL] Lage, Rahmen, Symbolfelder, Linien und Knöpfe: Baustein <see cref="OriginalDialog"/> (Datei 0x4C246, Aufbau
///   in <c>docs/analyse/re-meldungen.md</c> §C); für diese Frage gibt es kein Videobild, die Lage gilt wie bei der Frage der
///   DEB-Pokal-Auslosung (Video 3:46). Das Nutzerbild zeigt die „?“-Felder gelb, hier sind sie wie dort gezeichnet.</item>
/// <item>[APPROX] Escape und rechte Maustaste wie „Kein Gedanke“ als Komfort des Remakes: Das Original wertet nur die linke
///   Maustaste aus (0x4C246, A334 – Entscheidung Nutzer offen). Keine Taste beendet das Programm.</item>
/// </list>
/// </remarks>
public sealed class QuitQuestion
{
    public enum Outcome { None, Quit, Cancel }

    public static readonly string[] Lines = ["Sie haben also wirklich", "etwas besseres zu tun ?", "Sind Sie sicher ??"];
    public const string YesText = "Leider ja";
    public const string NoText = "Kein Gedanke";

    public static Rect Box => OriginalDialog.Window;
    public static Rect YesButton => OriginalDialog.LeftButton;
    public static Rect NoButton => OriginalDialog.RightButton;

    private readonly OriginalDialog _dialog;

    public QuitQuestion(string managerName)
    {
        Title = managerName;
        _dialog = new OriginalDialog(OriginalDialogKind.Question, managerName, Lines[0], Lines[1], Lines[2], YesText, NoText);
    }

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

    public void Draw(Surface s, Assets a) => _dialog.Draw(s, a);
}
