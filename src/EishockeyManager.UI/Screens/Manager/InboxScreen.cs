using EishockeyManager.Core;

namespace EishockeyManager.UI.Screens.ManagerArea;

/// <summary>
/// Posteingang (Manager → Neuer): alle Nachrichten, neueste oben; Fragen (Vertragsverlängerung, Kreditanfrage)
/// mit „Ja“ / „Nein“. [APPROX] Im UI-Video gibt es zu diesem Zeitpunkt keine Nachrichten – Layout im Stil der
/// übrigen Fenster (oranger Rahmen, Knöpfe aus kb\2).
/// </summary>
/// <remarks>
/// [APPROX] Dass die Kachel „Neuer“ den Posteingang öffnet, ist nicht belegt: Im Video (8:41) zeigt sie einen
/// Managerkopf und wird nur überfahren; die Nachrichtentexte von em.exe gehören zur Nachrichtenleiste.
/// </remarks>
public sealed class InboxScreen : Screen
{
    public static readonly Rect Window = new(6, 14, 308, 214);
    public const int PerPage = 6;
    private const int FirstY = 44;
    private const int Pitch = 29;
    private const int TextX = 18;

    public static Rect YesButton(int slot) => new(250, FirstY + Pitch * slot + 1, Widgets.ButtonWidth, Widgets.ButtonHeight);
    public static Rect NoButton(int slot) => new(250, FirstY + Pitch * slot + 14, Widgets.ButtonWidth, Widgets.ButtonHeight);

    private readonly Screen _back;
    private int _page;

    public InboxScreen(ScreenHost host, Screen back) : base(host) => _back = back;

    private GameState Game => Host.Game!;

    /// <summary>Nachrichten der aktuellen Seite, neueste zuerst.</summary>
    public IReadOnlyList<Message> Page =>
        Game.Inbox(Game.ActiveManager).Reverse().Skip(_page * PerPage).Take(PerPage).ToList();

    public override void Update(InputState input)
    {
        if (input.Pressed(Key.Escape) || input.RightClick)
        {
            Host.Show(_back);
            return;
        }
        var pages = Math.Max(1, (Game.Inbox(Game.ActiveManager).Count + PerPage - 1) / PerPage);
        if (input.Pressed(Key.PageDown) || input.Pressed(Key.Down) || input.Wheel < 0)
            _page = Math.Min(pages - 1, _page + 1);
        if (input.Pressed(Key.PageUp) || input.Pressed(Key.Up) || input.Wheel > 0)
            _page = Math.Max(0, _page - 1);
        if (!input.LeftClick)
            return;

        var page = Page;
        for (var i = 0; i < page.Count; i++)
        {
            if (!page[i].IsQuestion)
                continue;
            if (input.Clicked(YesButton(i)))
                Game.Answer(Game.ActiveManager, page[i], yes: true);
            else if (input.Clicked(NoButton(i)))
                Game.Answer(Game.ActiveManager, page[i], yes: false);
        }
        if (!Window.Contains(input.MouseX, input.MouseY))
            Host.Show(_back);
    }

    public override void Draw(Surface s)
    {
        var mini = Assets.Mini;
        s.Palette = Assets.Palette;
        s.Blit(Assets.Image(MainMenuScreen.Background), 0, 0);
        Widgets.Frame(s, Assets, Window, FrameStyle.Orange, Colors.PanelFill);
        Widgets.ShadowText(s, Assets.Flow, 160, 24, "POSTEINGANG", 11, 198);

        var page = Page;
        if (page.Count == 0)
        {
            s.TextCentered(mini, 160, 110, "Keine Nachricht für Sie vorhanden.", Colors.White);
            return;
        }
        for (var i = 0; i < page.Count; i++)
        {
            var m = page[i];
            m.Read = true;
            var y = FirstY + Pitch * i;
            s.Text(mini, TextX, y, SeasonCalendar.Format(m.Day, Game.Season), 32);
            for (var l = 0; l < Math.Min(3, m.Lines.Length); l++)
                s.Text(mini, TextX, y + 7 + 7 * l, ManagerText.ForFont(m.Lines[l]), m.IsQuestion ? Colors.Yellow : Colors.White);
            if (m.IsQuestion)
            {
                Widgets.Button(s, Assets, YesButton(i).X, YesButton(i).Y, "Ja");
                Widgets.Button(s, Assets, NoButton(i).X, NoButton(i).Y, "Nein");
            }
            if (i < page.Count - 1)
                s.HLine(TextX - 4, y + Pitch - 3, Window.W - 2 * (TextX - Window.X - 4), 1);
        }
    }
}

/// <summary>Anmeldung im Hauptmenü (Manager → Neuer).</summary>
internal static class InboxMenuEntries
{
    [MenuEntry("Neuer")]
    private static Screen Open(ScreenHost host, Screen back) => new InboxScreen(host, back);
}
