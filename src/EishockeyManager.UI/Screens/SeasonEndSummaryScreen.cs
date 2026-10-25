using EishockeyManager.Core;

namespace EishockeyManager.UI.Screens;

/// <summary>
/// Fenster „Saisonende“ je Manager: Hauptmenü mit Infoleiste (29. April), statt der Titelleiste „Saisonende“
/// in großer Schrift, darunter das Fenster „Ereignisse“ (Symbole aus kb\13 oder „Keine besonderen Ereignisse...“),
/// Platzierung und Prämie (Video 11:53 und 18:38). Danach beginnt die neue Saison – oder das Spiel endet.
/// </summary>
public sealed class SeasonEndSummaryScreen : Screen
{
    public static readonly Rect Window = new(3, 80, 314, 113);
    public const int IconWidth = 24;
    public const int IconHeight = 21;
    public const int IconPitch = 35;

    private readonly MainMenuScreen _menu;
    private int _index;

    public SeasonEndSummaryScreen(ScreenHost host, MainMenuScreen menu) : base(host) => _menu = menu;

    private GameState Game => Host.Game!;
    private SeasonRecord Record => Game.History[^1];

    /// <summary>
    /// Ereignissymbol in kb\13 (Zellen 24 × 21 im Raster 24 × 21).
    /// [APPROX] Zuordnung nach Bildinhalt: Pfeil hoch = Aufstieg, Pfeil runter = Abstieg, Puck = Meister,
    /// Fahne = DEB-Pokal, Landkarte = Europapokal.
    /// </summary>
    public static Rect EventIcon(SeasonEvent e) => e switch
    {
        SeasonEvent.Promotion => Cell(2, 0),
        SeasonEvent.Relegation => Cell(1, 0),
        SeasonEvent.GermanChampion => Cell(3, 0),
        SeasonEvent.CupWinner => Cell(4, 0),
        _ => Cell(0, 1),
    };

    private static Rect Cell(int column, int row) => new(column * IconWidth, row * IconHeight, IconWidth, IconHeight);

    public override void Update(InputState input)
    {
        if (!(input.LeftClick || input.RightClick || input.Keys.Count > 0))
            return;
        _index++;
        if (_index < Record.Managers.Count)
        {
            Game.CurrentManager = Record.Managers[_index].Manager;
            return;
        }
        if (Game.IsGameOver)
        {
            Host.Show(new SeasonEndGameOverScreen(Host, null, null));
            return;
        }
        Game.StartNextSeason();
        Game.CurrentManager = Game.ActiveManagers.Select(m => Game.Managers.ToList().IndexOf(m)).First();
        Host.Show(_menu);
    }

    public override void Enter()
    {
        if (Record.Managers.Count > 0)
            Game.CurrentManager = Record.Managers[Math.Min(_index, Record.Managers.Count - 1)].Manager;
    }

    public override void Draw(Surface s)
    {
        _menu.SelectedBigTile = -1;
        _menu.Draw(s);

        // Titelleiste durch „Saisonende“ ersetzen, Kacheln ausblenden (nur Sternenhintergrund)
        var background = Assets.Image(MainMenuScreen.Background);
        s.Blit(background, new Rect(0, 0, Surface.Width, 21), 0, 0);
        s.Blit(background, new Rect(0, 77, Surface.Width, Surface.Height - 77), 0, 77);
        Widgets.ShadowText(s, Assets.Bold, 160, 4, "Saisonende", Colors.White, 0);

        Widgets.Frame(s, Assets, Window, FrameStyle.Orange, SeasonEndScreen.WindowFill);
        var flow = Assets.Flow;
        Widgets.ShadowText(s, flow, 160, 84, "Ereignisse", 11, 198);
        s.HLine(10, 91, 300, 1);

        if (Record.Managers.Count == 0)
            return;
        var result = Record.Managers[Math.Min(_index, Record.Managers.Count - 1)];
        if (result.Events.Count == 0)
            Widgets.ShadowText(s, flow, 160, 97, "Keine besonderen Ereignisse...", Colors.White, 0);
        else
        {
            var icons = Assets.Image(13);
            var x = 160 - (result.Events.Count * IconPitch - (IconPitch - IconWidth)) / 2;
            foreach (var e in result.Events)
            {
                s.Outline(new Rect(x - 1, 102, IconWidth + 2, IconHeight + 2), 0);
                s.Blit(icons, EventIcon(e), x, 103);
                x += IconPitch;
            }
        }

        Widgets.ShadowText(s, flow, 160, 164, $"{result.Rank}. Platz der {SeasonInfo.LeagueGenitive(result.League)}", Colors.White, 0);
        var premium = result.Premium > 0 ? GameState.FormatMoney(result.Premium) : "keine";
        Widgets.ShadowText(s, flow, 160, 175, $"Erhaltene Prämien: {premium}", Colors.White, 0);
    }
}
