using EishockeyManager.Core;

namespace EishockeyManager.UI.Screens;

/// <summary>
/// Spielende nach der gewählten Spieldauer oder – nur noch bei ausgeschiedenen Managern älterer Spielstände – Entlassung
/// („hat Sie wegen der hohen Schulden“). Das Spiel selbst entlässt nicht mehr (#688, R688S-03; R688S2-02).
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>[APPROX] Entlassung: Wortlaut und Gestaltung sind nicht aus dem Original belegt (A341, Entlassungstext offen).</item>
/// <item>[ORIGINAL] Spielende em.exe Datei 0x51F3D (<c>docs/analyse/re-livespiel.md</c> „Spielende“, L8): ganzer Bildschirm
///   (0 | 0)–(319 | 239) Farbe 0, Schrift 1, „Das wars...“ zentriert in x 8 … 319 auf Grundlinie 100 in Farbe 7 und
///   „Carpe diem !“ auf Grundlinie 140 in Farbe 30, beide ohne Schatten (Schattenfarbe 0xFF); einblenden (0x0C5CE), auf einen
///   Mausklick warten (0x38:0x118).</item>
/// <item>[ORIGINAL] Reihenfolge Datei 0x5642E/0x56438: erst die Bestenliste (<see cref="Statistics.HighscoreScreen"/>, sofern es
///   für die Spielvorbereitung eine gibt), dann dieser Bildschirm.</item>
/// <item>[APPROX] Danach endet das Original (0xC0:0x61); das Remake kehrt zum Titel zurück. Palette: das Original zeichnet mit der
///   zuletzt gesetzten Palette; das Remake nimmt die Standardpalette kb\21. Der Bildschirm schließt bei jedem Klick oder jeder
///   Taste wie bisher (0x38:0x118 wartet auf die Maus; Tastatur nicht geprüft).</item>
/// </list>
/// </remarks>
public sealed class SeasonEndGameOverScreen : Screen
{
    public static readonly Rect Window = new(20, 50, 280, 140);

    public const string GameEndLine1 = "Das wars...";
    public const string GameEndLine2 = "Carpe diem !";
    public const int GameEndCenter = 163;      // (8 + 319) / 2, abgerundet wie OriginalDialog
    public const int GameEndBaseline1 = 100;
    public const int GameEndBaseline2 = 140;
    public const byte GameEndColor1 = 7;
    public const byte GameEndColor2 = 30;
    private const int BoldTopOffset = 15;       // Schrift 1 (Bold): Oberkante 15 Pixel über der Grundlinie (OriginalDialog)

    private readonly int? _dismissedManager;
    private readonly Screen? _back;
    private bool _highscoreShown;

    /// <param name="dismissedManager">Entlassener Manager oder null für das Spielende.</param>
    /// <param name="back">Bildschirm danach (bei Entlassung im Hot-Seat); null = zurück zum Titel.</param>
    public SeasonEndGameOverScreen(ScreenHost host, int? dismissedManager, Screen? back) : base(host)
    {
        _dismissedManager = dismissedManager;
        _back = back;
    }

    private GameState Game => Host.Game!;

    public override void Enter()
    {
        // [ORIGINAL] Datei 0x5642E: beim Spielende zuerst die Bestenliste, danach „Das wars...“ (0x56438).
        if (_dismissedManager is not null || _highscoreShown || HighscoreRules.Key(Game.Setup) is null)
            return;
        _highscoreShown = true;
        Host.Show(new Statistics.HighscoreScreen(Host, this));
    }

    public override void Update(InputState input)
    {
        if (!(input.LeftClick || input.RightClick || input.Keys.Count > 0))
            return;
        if (_back is not null && !Game.IsGameOver)
            Host.Show(_back);
        else if (_dismissedManager is not null)
            Host.Show(new SeasonEndGameOverScreen(Host, null, null));
        else
        {
            // [APPROX] Das Original endet hier (0xC0:0x61); das Remake zeigt den Titel.
            Host.Game = null;
            Host.Show(new TitleScreen(Host));
        }
    }

    public override void Draw(Surface s)
    {
        s.Palette = Assets.Palette;
        if (_dismissedManager is null)
        {
            DrawGameEnd(s);
            return;
        }

        s.Blit(Assets.Image(MainMenuScreen.Background), 0, 0);
        Widgets.Frame(s, Assets, Window, FrameStyle.Orange, Colors.PanelFill);
        var mini = Assets.Mini;

        if (_dismissedManager is { } i)
        {
            var manager = Game.Managers[i];
            Widgets.ShadowText(s, Assets.Bold, 160, 60, "Entlassen!", 11, 198);
            Widgets.Head(s, Assets, manager.Head, 40, 95);
            s.Text(mini, 90, 98, $"{manager.Name},", Colors.White);
            s.Text(mini, 90, 110, $"der Vorstand von {Game.Clubs[manager.ClubIndex].Name}", Colors.White);
            s.Text(mini, 90, 120, "hat Sie wegen der hohen Schulden", Colors.White);
            s.Text(mini, 90, 130, "mit sofortiger Wirkung entlassen.", Colors.White);
            s.Text(mini, 90, 145, $"Kontostand: {GameState.FormatMoney(manager.Money)}", Colors.Yellow);
            return;
        }

    }

    private void DrawGameEnd(Surface s)
    {
        s.Fill(new Rect(0, 0, Surface.Width, Surface.Height), 0);
        s.TextCentered(Assets.Bold, GameEndCenter, GameEndBaseline1 - BoldTopOffset, GameEndLine1, GameEndColor1);
        s.TextCentered(Assets.Bold, GameEndCenter, GameEndBaseline2 - BoldTopOffset, GameEndLine2, GameEndColor2);
    }
}
