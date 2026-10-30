using EishockeyManager.Core;

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

    private sealed class StrongBox { public int Value = -1; }

    private static StrongBox Of(GameState game) => Cursor.GetValue(game.Finance(game.ActiveManager), _ => new StrongBox());

    /// <summary>Index der gezeigten Nachricht: zuletzt gewählte, sonst die älteste ungelesene, sonst die neueste.</summary>
    public static int Shown(GameState game)
    {
        var inbox = game.Inbox(game.ActiveManager);
        var box = Of(game);
        if (box.Value < 0 || box.Value >= inbox.Count)
        {
            var unread = inbox.ToList().FindIndex(m => !m.Read);
            box.Value = unread >= 0 ? unread : inbox.Count - 1;
        }
        return box.Value;
    }

    /// <summary>Hat der aktive Manager eine ungelesene Nachricht (Auslöser der Leiste im Hauptmenü, #170)?</summary>
    public static bool HasUnread(GameState game) => game.Inbox(game.ActiveManager).Any(m => !m.Read);

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
        var center = Text.X + Text.W / 2;
        if (inbox.Count == 0)
        {
            s.TextCentered(mini, center, 207, "Keine Nachricht für Sie vorhanden.", TextColor);
            return;
        }
        var index = Shown(game);
        var message = inbox[index];
        message.Read = true;
        // [APPROX] Kopfzeile = erste Zeile, weißes Feld = weitere Zeilen; Inhalt einer Nachricht ist im Video nicht zu sehen.
        s.TextCentered(mini, center, 207, ManagerText.ForFont(message.Lines[0]), TextColor);
        for (var i = 1; i < Math.Min(3, message.Lines.Length); i++)
            s.TextCentered(mini, center, 214 + 7 * i, ManagerText.ForFont(message.Lines[i]), TextColor);
        if (message.IsQuestion)
            s.TextRight(mini, Text.Right - 4, 228, QuestionHint(game), Colors.Red);
        s.TextRight(mini, Text.Right - 4, 221, $"{index + 1}/{inbox.Count}", TextColor);
    }
}
