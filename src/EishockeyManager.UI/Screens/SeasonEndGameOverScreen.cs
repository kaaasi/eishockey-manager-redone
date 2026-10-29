using EishockeyManager.Core;

namespace EishockeyManager.UI.Screens;

/// <summary>
/// Entlassung eines Managers (zu lange zu hohe Schulden) oder Spielende nach der gewählten Spieldauer.
/// [APPROX] Wortlaut und Gestaltung sind nicht aus dem Original belegt. Nach dem Spielende folgt die Bestenliste
/// (<see cref="Statistics.HighscoreScreen"/>), wenn es für die Spielvorbereitung eine gibt.
/// </summary>
public sealed class SeasonEndGameOverScreen : Screen
{
    public static readonly Rect Window = new(20, 50, 280, 140);

    private readonly int? _dismissedManager;
    private readonly Screen? _back;

    /// <param name="dismissedManager">Entlassener Manager oder null für das Spielende.</param>
    /// <param name="back">Bildschirm danach (bei Entlassung im Hot-Seat); null = zurück zum Titel.</param>
    public SeasonEndGameOverScreen(ScreenHost host, int? dismissedManager, Screen? back) : base(host)
    {
        _dismissedManager = dismissedManager;
        _back = back;
    }

    private GameState Game => Host.Game!;

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
            // [ORIGINAL] Spielende (Datei 0x5642E): Bestenliste eintragen und zeigen, dann zurück zum Titel. Ohne Bestenliste
            // (gesperrte Kachel, Schlüssel 0) entfällt sie.
            Screen next = HighscoreRules.Key(Game.Setup) is null
                ? new TitleScreen(Host)
                : new Statistics.HighscoreScreen(Host, new TitleScreen(Host));
            Host.Game = null;
            Host.Show(next);
        }
    }

    public override void Draw(Surface s)
    {
        s.Palette = Assets.Palette;
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

        Widgets.ShadowText(s, Assets.Bold, 160, 60, "Spielende", 11, 198);
        var seasons = Game.History.Count;
        s.TextCentered(mini, 160, 88, seasons == 1 ? "Nach einer Saison:" : $"Nach {seasons} Saisons:", Colors.Yellow);
        var y = 102;
        for (var m = 0; m < Game.Managers.Count; m++)
        {
            var manager = Game.Managers[m];
            var last = Game.History.SelectMany(r => r.Managers).LastOrDefault(r => r.Manager == m);
            var line = manager.Dismissed
                ? $"{manager.Name} – entlassen"
                : last is null
                    ? manager.Name
                    : $"{manager.Name} – {last.Rank}. Platz der {SeasonInfo.LeagueGenitive(last.League)}";
            s.TextCentered(mini, 160, y, line, Colors.White);
            s.TextCentered(mini, 160, y + 8, GameState.FormatMoney(manager.Money), Colors.Yellow);
            y += 20;
        }
    }
}
