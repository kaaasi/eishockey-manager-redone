namespace EishockeyManager.UI.Screens;

/// <summary>
/// Ablauf eines Spieltags ohne Live-Spiel, wie im Original:
/// 1. Banner „Ligaspiel“ (wenn ein Manager-Verein spielt),
/// 2. „Spiele laufen“ mit Spielminute und drei Balken (je Drittel: gelb = gespielt, rot = offen),
/// 3. zurück ins Hauptmenü.
/// Klick, Leertaste oder die Vorspultaste oben rechts überspringen.
/// </summary>
public sealed class MatchDayScreen : Screen
{
    public const int BannerFrames = 75;
    public const int FramesPerMinute = 3;
    public const int Minutes = 60;

    public static readonly Rect Banner = new(3, 92, 314, 57);
    public static readonly Rect ProgressBox = new(3, 183, 121, 36);
    public static readonly Rect FastForward = new(249, 12, 58, 58);

    private readonly MainMenuScreen _menu;
    private readonly bool _ownMatch;
    private int _frame;
    private bool _played;

    public MatchDayScreen(ScreenHost host, MainMenuScreen menu, bool ownMatch) : base(host)
    {
        _menu = menu;
        _ownMatch = ownMatch;
        if (!ownMatch)
            _frame = BannerFrames;
    }

    private bool ShowingBanner => _frame < BannerFrames;
    private int Minute => Math.Clamp((_frame - BannerFrames) / FramesPerMinute, 0, Minutes);

    public override void Update(InputState input)
    {
        if (!_played)
        {
            Host.Game!.Continue();
            _played = true;
        }

        var skip = input.LeftClick || input.Pressed(Key.Space) || input.Pressed(Key.Enter) || input.Pressed(Key.Escape);
        if (skip && ShowingBanner)
            _frame = BannerFrames;
        else if (skip || Minute >= Minutes && _frame > BannerFrames + Minutes * FramesPerMinute + 20)
        {
            Host.Show(_menu);
            return;
        }
        _frame++;
    }

    public override void Draw(Surface s)
    {
        s.Palette = Assets.Palette;
        s.Blit(Assets.Image(MainMenuScreen.Background), 0, 0);
        DrawFastForward(s);

        if (ShowingBanner)
        {
            Widgets.Frame(s, Assets, Banner, FrameStyle.Orange);
            Widgets.ShadowText(s, Assets.Bold, 160, 110, "Ligaspiel", 11, 198);
            return;
        }

        Widgets.ShadowText(s, Assets.Bold, 160, 65, "Spiele laufen", Colors.TileBackground, 53);

        var box = ProgressBox;
        s.Fill(box, Colors.PanelFill);
        s.Outline(box, 241);
        for (var third = 0; third < 3; third++)
        {
            var color = Minute >= (third + 1) * 20 ? Colors.Yellow : Colors.Red;
            s.Fill(new Rect(9, 192 + third * 8, 80, 1), color);
        }
        DrawNumber(s, Minute, 95, 196);
    }

    /// <summary>Spielminute mit den Ziffern aus kb\38 (8 × 10), zweistellig.</summary>
    private void DrawNumber(Surface s, int value, int x, int y)
    {
        var digits = Assets.Image(38);
        var text = value.ToString("00");
        foreach (var ch in text)
        {
            s.Blit(digits, new Rect((ch - '0') * 8, 0, 8, 10), x, y, transparent: 0);
            x += 8;
        }
    }

    /// <summary>Vorspultaste: zwei schwarze Dreiecke.</summary>
    private static void DrawFastForward(Surface s)
    {
        for (var tri = 0; tri < 2; tri++)
        {
            var x0 = FastForward.X + tri * 28;
            for (var dy = 0; dy < FastForward.H; dy++)
            {
                var half = FastForward.H / 2;
                var w = (half - Math.Abs(dy - half)) * 28 / half;
                s.HLine(x0, FastForward.Y + dy, w, Colors.Black);
            }
        }
    }
}
