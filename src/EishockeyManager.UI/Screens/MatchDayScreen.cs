using EishockeyManager.Core;
using EishockeyManager.UI.Screens.Live;

namespace EishockeyManager.UI.Screens;

/// <summary>
/// „Tag weiter“ (Klick aufs Vereinslogo, früher „und tschüß…“) ohne Live-Spiel, wie im Original:
/// <see cref="GameState.Continue"/> spielt bis zum nächsten eigenen Spiel; angezeigt werden der Reihe nach
/// 1. Banner („Ligaspiel“, „Play-Off / Aufstiegsrunden“ …), wenn ein Manager-Verein spielt,
/// 2. „Spiele laufen“ mit der Spieluhr des Originals (<see cref="LiveWidgets.Clock"/>: drei Balken zu 4 Pixel je Minute, Minute),
/// 3. Banner und Meisterfotos der übrigen Tage (z. B. „Deutscher Meister“), die DEB-Pokal-Auslosung nach dem dritten
///    Termin einer Runde (<see cref="Competitions.DebCupDrawFlow"/>),
/// 4. zurück ins Hauptmenü – oder am 29. April der Bildschirm „Saisonende“.
/// Klick, Enter oder die Vorspultaste oben rechts überspringen (keine Leertaste, Entscheidung Nutzer 31.10.2026, #205).
/// <para>
/// Live-Spiel (Spur livespiel-ui-2, B2): Über <see cref="Start"/> läuft „Tag weiter“ als <see cref="LiveSession"/>. Hält der
/// Spielkern an einer Managerpaarung mit Anzeigereihe, zeigt der Bildschirm das Banner ihres Wettbewerbs und dann den
/// Live-Bildschirm (<see cref="LiveMatchScreen"/>), der die Minuten treibt; danach geht es mit dem nächsten Halt bzw. den
/// übrigen Schritten weiter. „Spiele laufen“ und das Banner entfallen für die live gezeigten Spiele ([ORIGINAL] §1 Schritt 1/2:
/// mit Anzeige baut die Minutenschleife den Live-Bildschirm statt „Spiele laufen“).
/// </para>
/// </summary>
public sealed class MatchDayScreen : Screen
{
    /// <summary>
    /// [ORIGINAL] Datei 0x35D5D/0x35E1D: Argument 0x3C = 60 an die Wartefunktion Datei 0x130EE, die zuerst durch 3 teilt
    /// (0x130F1–0x130FE) → 20 Takte zu 18,2 Hz ≈ 1,1 s = 66 Bilder (R-K3, Review #546 R12PM-01). Video `bundestrainer_00120`,
    /// `_00157`, `_00246`: Banner auf genau einem Bild (1 Bild/s).
    /// </summary>
    public static readonly int BannerFrames = ShortMessage.FramesFor(60);
    public const int FramesPerMinute = 3;
    public const int Minutes = 60;

    public static Rect Banner => SeasonInfo.Banner;
    public static readonly Rect ProgressBox = new(3, 183, 121, 36);
    public static readonly Rect FastForward = new(249, 12, 58, 58);

    private abstract record Item;
    private sealed record BannerItem(string Text) : Item;
    private sealed record RunningItem : Item;
    private sealed record ScreenItem(Func<Screen> Create) : Item;
    private sealed record LiveItem : Item;

    private readonly MainMenuScreen _menu;
    private readonly Queue<Item> _items = new();
    private Item? _current;
    private int _frame;
    private bool _started;
    private readonly LiveSession? _session;
    private readonly IReadOnlyList<SeasonStep> _steps;
    private bool _expanded;

    /// <summary>
    /// <paramref name="steps"/> sind die Ergebnisse von <see cref="GameState.Continue"/>, das der Aufrufer (Eingabe „Tag weiter“ im
    /// Hauptmenü) bereits ausgeführt hat: Der Bildschirm zeigt nur an und lässt beim Öffnen keine Zeit vergehen
    /// (AGENTS.md §4, #558).
    /// </summary>
    public MatchDayScreen(ScreenHost host, MainMenuScreen menu, IReadOnlyList<SeasonStep>? steps = null) : base(host)
    {
        _menu = menu;
        _steps = steps ?? [];
    }

    private MatchDayScreen(ScreenHost host, MainMenuScreen menu, LiveSession session) : base(host)
    {
        _menu = menu;
        _session = session;
        _steps = [];
    }

    /// <summary>
    /// „Tag weiter“ mit Live-Spiel: startet <see cref="GameState.Continue"/> als <see cref="LiveSession"/> (die Zeit vergeht in
    /// diesem Aufruf, also in der Eingabe des Hauptmenüs, und in den Minuten, die der Live-Bildschirm anstößt).
    /// </summary>
    public static MatchDayScreen Start(ScreenHost host, MainMenuScreen menu) =>
        new(host, menu, LiveSession.Start(host.Game!));

    /// <summary>Die Schritte des letzten „Tag weiter“ (für Tests); mit Live-Spiel erst, wenn der Tag fertig ist.</summary>
    public IReadOnlyList<SeasonStep> Steps => _session?.Steps ?? _steps;

    /// <summary>Sitzung des Live-Spiels oder null.</summary>
    public LiveSession? Session => _session;

    /// <summary>
    /// Wettbewerb des Banners zu einer Paarung ([APPROX] Länder-Freundschaftsspiel ohne Banner: <see cref="SeasonInfo.BannerText"/>
    /// kennt keines).
    /// </summary>
    public static Competition? BannerCompetition(MatchCompetition competition) => competition switch
    {
        MatchCompetition.League => Competition.League,
        MatchCompetition.Playoff => Competition.Playoffs,
        MatchCompetition.Relegation => Competition.Relegation,
        MatchCompetition.DebCup => Competition.DebCup,
        MatchCompetition.EuropeanCup => Competition.EuropeanCup,
        MatchCompetition.WorldCup => Competition.WorldCup,
        _ => null,
    };

    /// <summary>Text des gerade gezeigten Banners oder null.</summary>
    public string? CurrentBanner => (_current as BannerItem)?.Text;

    private GameState Game => Host.Game!;

    public override void Enter()
    {
        if (_started)
            return;
        _started = true;
        if (_session is null)
            Enqueue(_steps);
        else
            EnqueueSession();
        Next();
    }

    /// <summary>Nächster Halt der Live-Sitzung: Banner und Live-Bildschirm; ist der Tag fertig, die übrigen Schritte.</summary>
    private bool EnqueueSession()
    {
        if (_session is null)
            return false;
        if (_session.Day is { } day)
        {
            if (BannerCompetition(day.Matches[0].Competition) is { } competition)
                _items.Enqueue(new BannerItem(SeasonInfo.BannerText(competition)));
            _items.Enqueue(new LiveItem());
            return true;
        }
        if (_expanded || _session.Steps is not { } steps)
            return false;
        _expanded = true;
        Enqueue(steps);
        return true;
    }

    private void Enqueue(IReadOnlyList<SeasonStep> steps)
    {
        foreach (var step in steps)
        {
            switch (step)
            {
                case MatchesStep { ManagerInvolved: true } m when LiveShown(m):
                    break;
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
    }

    private bool LiveShown(MatchesStep step) =>
        _session is not null && _session.LiveDays.Any(l => l.Day == step.Day && BannerCompetition(l.Competition) == step.Competition);

    private void Next()
    {
        _frame = 0;
        if (_items.Count == 0)
            EnqueueSession();
        if (_items.TryDequeue(out var item))
        {
            _current = item;
            if (item is ScreenItem screen)
                Host.Show(screen.Create());
            else if (item is LiveItem)
                Host.Show(new LiveMatchScreen(Host, _session!, this));
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
            case ScreenItem or LiveItem:
                // zurück vom Meisterfoto, Live-Bildschirm o. Ä.
                Next();
                return;
            case null:
                Next();
                return;
        }

        var skip = input.LeftClick || input.Pressed(Key.Enter) || input.Pressed(Key.Escape);
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
        DrawRunning(s, Assets, Minute);
    }

    /// <summary>[ORIGINAL] Datei 0x37BAA–0x37BCB: Text zentriert x 0–319 (0x13F), Grundlinie 80, Farbe 30, Schatten 0.</summary>
    public const int RunningBaseline = 80;
    public const byte RunningTextColor = 30;
    public const byte RunningShadow = 0;

    /// <summary>[ORIGINAL] Datei 0x37BD3–0x37C16: Rahmen 0x0B5A7 Farbe 0, Füllung (4 | 184)–(122 | 217) 0x0B620 Farbe 3.</summary>
    public const byte ProgressFrameColor = 0;
    public const byte ProgressFill = 3;

    /// <summary>
    /// „Spiele laufen“ ohne Live-Anzeige (em.exe Datei 0x37B82–0x37C5C, <c>docs/analyse/re-livespiel.md</c> §1 Schritt 2): Text,
    /// Kasten (3 | 183)–(123 | 218) und die Spieluhr 0x51E90 mit Stil 1 (Datei 0x37C44–0x37C56: Stil 1 ohne Anzeige); am
    /// Minutenende zeichnet 0x52BD6 ohne Anzeige nur die Uhr neu (§1 Schritt 5).
    /// </summary>
    public static void DrawRunning(Surface s, Assets assets, int minute)
    {
        Competitions.CompetitionWidgets.Centered(s, assets.Bold, 0, 0x13F, RunningBaseline, "Spiele laufen", RunningTextColor,
            RunningShadow);
        var box = ProgressBox;
        s.Outline(box, ProgressFrameColor);
        s.Fill(box.Inflate(-1), ProgressFill);
        LiveWidgets.Clock(s, assets, 1, minute);
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
