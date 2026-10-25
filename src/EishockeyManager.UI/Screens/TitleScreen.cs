namespace EishockeyManager.UI.Screens;

/// <summary>Titelbild kb\197 mit eigener Palette. Klick oder Taste → Spielvorbereitung.</summary>
public sealed class TitleScreen : Screen
{
    public const int TitleImage = 197;

    public TitleScreen(ScreenHost host) : base(host) { }

    public override void Update(InputState input)
    {
        if (input.Pressed(Key.Escape))
        {
            Host.Exit();
            return;
        }
        if (input.LeftClick || input.RightClick || input.Keys.Count > 0)
            Host.Show(new SetupScreen(Host));
    }

    public override void Draw(Surface surface)
    {
        var image = Assets.Image(TitleImage);
        surface.Palette = image.Palette ?? Assets.Palette;
        surface.Blit(image, 0, 0);
    }
}
