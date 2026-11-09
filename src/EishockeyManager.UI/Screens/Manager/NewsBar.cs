using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.UI.Screens.ManagerArea;

/// <summary>
/// Nachrichtenleiste (kb\6) unten im Hauptmenü, solange keine große Kachel gewählt ist und eine neue Nachricht da ist
/// (Bedingung: <see cref="MainMenuScreen"/>, #170; Aussehen UI-Video 15:10):
/// Kopfzeile mit „Keine Nachricht für Sie vorhanden.“ bzw. der Nachricht, darunter das weiße Textfeld,
/// rechts ▲▼ zum Blättern. Klick auf die Nachricht öffnet den Posteingang („Neuer“).
/// </summary>
public static class NewsBar
{
    public const int Graphic = 6;
    public static readonly Rect Area = new(3, 201, 314, 36);
    public static readonly Rect ScrollUp = new(300, 206, 13, 11);
    public static readonly Rect ScrollDown = new(300, 219, 13, 11);
    private static readonly Rect Text = new(3, 201, 296, 36);
    private const byte TextColor = 0;

    /// <summary>
    /// Hinweis bei einer Frage rechts unten. [APPROX] „(Neuer)“ verweist auf die Kachel „Neuer“, die den Posteingang
    /// öffnet (<see cref="InboxScreen"/>); ist sie gesperrt (vier Manager, <see cref="TileLocks"/>), bleibt als Zugang
    /// nur der Klick auf die Nachricht – dann steht dort „(Klicken)“ (R4-02, #109).
    /// </summary>
    public static string QuestionHint(GameState game) =>
        game.Managers.Count >= GameSetup.MaxManagers ? "(Klicken)" : "(Neuer)";

    /// <summary>Gezeigte Nachricht (Index im Posteingang) je Manager.</summary>
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<ManagerFinance, StrongBox> Cursor = new();

    private sealed class StrongBox { public int Value = -1; public int Count = -1; public Message? Newest; public Message? Oldest; }

    private static StrongBox Of(GameState game) => Cursor.GetValue(game.Finance(game.ActiveManager), _ => new StrongBox());

    /// <summary>
    /// Index der gezeigten Nachricht: zuletzt gewählte, sonst die neueste. Das Original hat kein Feld „gelesen“ (D8 = A, #537):
    /// die Leiste zeigt die Nachricht, die das Kennzeichen „neue Nachricht“ ausgelöst hat (Datei 0x4B5F4 mit dem Platz der neuen
    /// Nachricht), und blättert von dort (Datei 0x4B9CD). Ändert sich der Speicher, beginnt die Anzeige wieder bei der neuesten.
    /// </summary>
    public static int Shown(GameState game)
    {
        var inbox = game.Inbox(game.ActiveManager);
        var box = Of(game);
        var newest = inbox.Count > 0 ? inbox[^1] : null;
        var oldest = inbox.Count > 0 ? inbox[0] : null;
        // Hinweis aus Review #546: Fällt an einem Tag eine Meldung weg und kommt eine neue hinzu, bleibt die Anzahl gleich, der
        // Index zeigte aber auf eine andere Nachricht. Deshalb gilt auch eine andere neueste bzw. älteste Nachricht als „Speicher
        // geändert“ (ui-rest Teil 4).
        if (box.Value < 0 || box.Value >= inbox.Count || box.Count != inbox.Count
            || !ReferenceEquals(box.Newest, newest) || !ReferenceEquals(box.Oldest, oldest))
        {
            box.Value = inbox.Count - 1;
            box.Count = inbox.Count;
            box.Newest = newest;
            box.Oldest = oldest;
        }
        return box.Value;
    }

    /// <summary>Verarbeitet Klicks auf die Leiste; true, wenn die Eingabe verbraucht wurde.</summary>
    public static bool Update(ScreenHost host, Screen menu, InputState input)
    {
        var game = host.Game!;
        if (input.Wheel != 0 && Area.Contains(input.MouseX, input.MouseY))
            return Scroll(game, -Math.Sign(input.Wheel));
        if (!input.LeftClick || !Area.Contains(input.MouseX, input.MouseY))
            return false;
        if (ScrollUp.Contains(input.MouseX, input.MouseY))
            return Scroll(game, -1);
        if (ScrollDown.Contains(input.MouseX, input.MouseY))
            return Scroll(game, +1);
        host.Show(new InboxScreen(host, menu));
        return true;
    }

    private static bool Scroll(GameState game, int delta)
    {
        var count = game.Inbox(game.ActiveManager).Count;
        if (count > 0)
            Of(game).Value = Math.Clamp(Shown(game) + delta, 0, count - 1);
        return true;
    }

    public static void Draw(Surface s, Assets assets, GameState game)
    {
        var mini = assets.Mini;
        s.Blit(assets.Image(Graphic), new Rect(0, 0, Area.W, Area.H), Area.X, Area.Y);
        var inbox = game.Inbox(game.ActiveManager);
        const int center = 159; // [ORIGINAL] Mitte der Zeilen x ≈ 158 (`file37a_1`: Zeile 1 x 95–221, Zeile 2 x 133–183 → Mitte 159 bei zentrierter Abrundung), nicht die Mitte der Leiste
        if (inbox.Count == 0)
        {
            s.TextCentered(mini, center, 207, "Keine Nachricht für Sie vorhanden.", TextColor);
            return;
        }
        var index = Shown(game);
        var message = inbox[index];
        // Zeichnen und Öffnen des Posteingangs ändern nichts (AGENTS.md §4, E4 = B, D8 = A): kein Feld „gelesen“ im Original.
        // [ORIGINAL] Oberes Feld = Datum der Nachricht („6. Dez. 1993“), weißes Feld = Text in zwei zentrierten Zeilen (7 Zeilen
        // Abstand); DOSBox-Sitzung 2, `file37a_1`, A321: Datum Zeilen 208–212, Text 220–224 und 227–231, Mitte x ≈ 158.
        // [ORIGINAL] Datei 0x4B6F0–0x4B76D: Das Original bricht nicht um, sondern zeichnet die Zeilen der Nachricht (getrennt durch „^“)
        // je zentriert im Bereich x 0x10 … 0x12F (Mitte 159), erste Zeile Grundlinie 212 (Datum), danach + 12, dann + 7. Die Leiste
        // (Fläche bis y 236) fasst daher nur zwei Textzeilen (Grundlinien 224, 231).
        // [APPROX] Die Nachrichten des Remakes stehen als Zeilen ohne „^“-Trennung: Sie werden mit Leerzeichen verbunden und auf
        // die Breite des weißen Feldes umbrochen (im Bild bricht „EC HEDOS MÜNCHEN ist an SCHIFFL“ / „interessiert.“ so um). Ergeben
        // sich mehr als zwei Zeilen, wird breiter umbrochen (bis zur Feldbreite) und, falls es dann noch zu viele sind, nach der
        // zweiten Zeile abgeschnitten – eine dritte Zeile läge auf dem unteren Rand der Leiste (y 234); der volle Text steht im Posteingang.
        s.TextCentered(mini, center, DateY, SeasonCalendar.FormatShort(message.Day, message.Season), TextColor);
        var y = TextY;
        var text = ManagerText.ForFont(string.Join(' ', message.Lines));
        var lines = Wrap(mini, text, WrapWidth).ToList();
        if (lines.Count > MaxTextLines)
            lines = Wrap(mini, text, Text.W - 12).ToList();
        foreach (var line in lines.Take(MaxTextLines))
        {
            s.TextCentered(mini, center, y, line, TextColor);
            y += LineStep;
        }
        // [APPROX] Hinweis bei einer Frage und Zähler bei mehreren Nachrichten: im Bild nicht belegt (eine Nachricht ohne Zähler).
        if (message.IsQuestion)
            s.TextRight(mini, Text.Right - 4, 228, QuestionHint(game), Colors.Red);
        if (inbox.Count > 1)
            s.TextRight(mini, Text.Right - 4, 221, $"{index + 1}/{inbox.Count}", TextColor);
    }

    private const int DateY = 207;
    private const int TextY = 220;
    private const int LineStep = 7;
    private const int WrapWidth = 128;
    private const int MaxTextLines = 2;

    internal static IEnumerable<string> Wrap(BitmapFont font, string text, int width)
    {
        var line = "";
        foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var next = line.Length == 0 ? word : line + " " + word;
            if (line.Length > 0 && font.Measure(next) > width)
            {
                yield return line;
                line = word;
            }
            else
                line = next;
        }
        if (line.Length > 0)
            yield return line;
    }
}
