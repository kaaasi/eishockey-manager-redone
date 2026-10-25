namespace EishockeyManager.UI.Screens;

/// <summary>Hinweis für Bildschirme, die noch nicht portiert sind. Klick oder Taste → zurück.</summary>
public sealed class PlaceholderScreen : Screen
{
    private readonly string _title;
    private readonly Screen _back;

    public PlaceholderScreen(ScreenHost host, string title, Screen back) : base(host)
    {
        _title = title;
        _back = back;
    }

    public override void Update(InputState input)
    {
        if (input.LeftClick || input.RightClick || input.Keys.Count > 0)
            Host.Show(_back);
    }

    public override void Draw(Surface s)
    {
        s.Palette = Assets.Palette;
        s.Blit(Assets.Image(MainMenuScreen.Background), 0, 0);
        var window = new Rect(40, 70, 240, 100);
        Widgets.Frame(s, Assets, window, FrameStyle.Orange, Colors.PanelFill);
        s.TextCentered(Assets.Bold, 160, 84, _title, Colors.White);
        s.TextCentered(Assets.Mini, 160, 126, "Dieser Bildschirm ist noch nicht portiert.", Colors.Yellow);
        s.TextCentered(Assets.Mini, 160, 140, "Klick oder Taste = zurück", Colors.White);
    }
}
