using EishockeyManager.Core;

namespace EishockeyManager.UI.Screens;

/// <summary>
/// „Tag weiter“ (Klick aufs Vereinslogo, früher „und tschüß…“) ohne Live-Spiel, wie im Original:
/// <see cref="GameState.Continue"/> spielt bis zum nächsten eigenen Spiel; angezeigt werden der Reihe nach
/// 1. Banner („Ligaspiel“, „Play-Off / Aufstiegsrunden“ …), wenn ein Manager-Verein spielt,
/// 2. „Spiele laufen“ mit Spielminute und drei Balken (je Drittel: gelb = gespielt, rot = offen),
/// 3. Banner und Meisterfotos der übrigen Tage (z. B. „Deutscher Meister“), die DEB-Pokal-Auslosung nach dem dritten
///    Termin einer Runde (<see cref="Competitions.DebCupDrawFlow"/>),
/// 4. zurück ins Hauptmenü – oder am 29. April der Bildschirm „Saisonende“.
/// Klick, Leertaste oder die Vorspultaste oben rechts überspringen.
/// </summary>
public sealed class MatchDayScreen : Screen
{
    public const int BannerFrames = 75;
    public const int FramesPerMinute = 3;
    public const int Minutes = 60;

    public static Rect Banner => SeasonInfo.Banner;
    public static readonly Rect ProgressBox = new(3, 183, 121, 36);
    public static readonly Rect FastForward = new(249, 12, 58, 58);

    private abstract record Item;
    private sealed record BannerItem(string Text) : Item;
    private sealed record RunningItem : Item;
    private sealed record ScreenItem(Func<Screen> Create) : Item;

    private readonly MainMenuScreen _menu;
    private readonly Queue<Item> _items = new();
    private Item? _current;
    private int _frame;
    private bool _started;

    public MatchDayScreen(ScreenHost host, MainMenuScreen menu) : base(host) => _menu = menu;

    /// <summary>Die Schritte des letzten „Tag weiter“ (für Tests).</summary>
    public IReadOnlyList<SeasonStep> Steps { get; private set; } = [];

    /// <summary>Text des gerade gezeigten Banners oder null.</summary>
    public string? CurrentBanner => (_current as BannerItem)?.Text;

    private GameState Game => Host.Game!;

    public override void Enter()
    {
        if (_started)
            return;
        _started = true;
        Steps = Game.Continue();
        foreach (var step in Steps)
        {
            switch (step)
            {
                case MatchesStep { ManagerInvolved: true } m:
                    _items.Enqueue(new BannerItem(SeasonInfo.BannerText(m.Competition)));
                    _items.Enqueue(new RunningItem());
                    break;
                case BannerStep b:
                    _items.Enqueue(new BannerItem(b.Text));
                    break;
                case TrophyStep t:
                    _items.Enqueue(new ScreenItem(() => new SeasonEndTrophyScreen(Host, t, this)));
                    break;
                case CupDrawStep { Competition: Competition.DebCup } c: // Spur debpokal-auslosung (0x34F64 → 0x3489D)
                    _items.Enqueue(new ScreenItem(() => Competitions.DebCupDrawFlow.Create(Host, c.Round, c.Slots, this)));
                    break;
                case DismissalStep d:
                    _items.Enqueue(new ScreenItem(() => new SeasonEndGameOverScreen(Host, d.Manager, this)));
                    break;
            }
        }
        Next();
    }

    private void Next()
    {
        _frame = 0;
        if (_items.TryDequeue(out var item))
        {
            _current = item;
            if (item is ScreenItem screen)
                Host.Show(screen.Create());
            return;
        }

        _current = null;
        if (Steps.OfType<SeasonEndStep>().Any())
            Host.Show(new SeasonEndScreen(Host, _menu));
        else if (Game.IsGameOver)
            Host.Show(new SeasonEndGameOverScreen(Host, null, null));
        else
            Host.Show(_menu);
    }

    private int Minute => Math.Clamp(_frame / FramesPerMinute, 0, Minutes);

    public override void Update(InputState input)
    {
        switch (_current)
        {
            case ScreenItem:
                // zurück vom Meisterfoto o. Ä.
                Next();
                return;
            case null:
                Next();
                return;
        }

        var skip = input.LeftClick || input.Pressed(Key.Space) || input.Pressed(Key.Enter) || input.Pressed(Key.Escape);
        var done = _current switch
        {
            BannerItem => _frame >= BannerFrames,
            _ => _frame > Minutes * FramesPerMinute + 20,
        };
        if (skip || done)
        {
            Next();
            return;
        }
        _frame++;
    }

    public override void Draw(Surface s)
    {
        s.Palette = Assets.Palette;
        s.Blit(Assets.Image(MainMenuScreen.Background), 0, 0);

        if (_current is BannerItem banner)
        {
            SeasonInfo.DrawBanner(s, Assets, banner.Text);
            return;
        }
        if (_current is not RunningItem)
            return;

        DrawFastForward(s);
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
