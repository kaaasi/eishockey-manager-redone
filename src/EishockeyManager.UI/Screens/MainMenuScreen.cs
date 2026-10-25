using EishockeyManager.Core;

namespace EishockeyManager.UI.Screens;

/// <summary>Kleine Kachel im Hauptmenü (Symbol aus kb\3, 38 × 28).</summary>
public sealed record MenuTile(string Name, int Column, int Row)
{
    /// <summary>Platzhalter (Fisch) für unbelegte Plätze.</summary>
    public static readonly MenuTile Empty = new("", 3, 4);

    public bool IsEmpty => Name.Length == 0;
    public Rect Source => new(1 + Column * 39, 87 + Row * 29, 38, 28);
}

/// <summary>
/// Hauptmenü / Wochenübersicht: Titelleiste kb\20, Infoleiste, acht große Kacheln (kb\3) und
/// – nach Auswahl einer großen Kachel – sechs kleine Kacheln am unteren Rand.
/// </summary>
public sealed class MainMenuScreen : Screen
{
    public const int Background = 22;
    public const int TitleBar = 20;
    public const int TileSheet = 3;

    public static readonly string[] BigTiles =
    [
        "Manager", "Mannschaft", "Statistiken", "Tabellen",
        "Paarungen", "Stärken", "Gemischtes", "und tschüß…",
    ];

    public const int Continue = 7;

    /// <summary>Kleine Kacheln je großer Kachel, wie im Original (Video); leere Plätze zeigen den Fisch.</summary>
    public static readonly MenuTile[][] SmallTiles =
    [
        [new("Finanzen", 0, 0), new("Werbung", 1, 0), new("Stadion", 2, 0), new("Jugend", 3, 0), new("Neuer", 4, 0), new("Highscore", 5, 0)],
        [new("Aufstellung", 0, 1), new("Taktik", 1, 1), new("Verträge", 2, 1), new("Transfer", 3, 1), new("Training", 4, 1), MenuTile.Empty],
        [new("Totale", 5, 1), new("Früher", 0, 2), new("Verein", 1, 2), new("Spieler", 2, 2), new("Fairplay", 3, 2), new("WM-Statistik", 4, 2)],
        [new("Tabelle 1. Liga", 5, 2), new("Tabelle 2. Liga", 0, 3), new("Tabelle Oberliga", 1, 3), new("Tabelle WM", 2, 3), MenuTile.Empty, MenuTile.Empty],
        [new("Paarungen 1. Liga", 5, 2), new("Paarungen 2. Liga", 0, 3), new("Paarungen Oberliga", 1, 3), new("DEB-Pokal", 3, 3), new("Europapokal", 4, 3), new("WM", 5, 3)],
        // [APPROX] Belegung der Stärken nicht im Video zu sehen; Länderteams laut Bildschirmtitel vorhanden.
        [new("Stärken 1. Liga", 5, 2), new("Stärken 2. Liga", 0, 3), new("Stärken Oberliga", 1, 3), new("Stärken Länderteams", 2, 3), MenuTile.Empty, MenuTile.Empty],
        [new("Laden", 0, 4), new("Sichern", 1, 4), new("Einstellungen", 2, 4), MenuTile.Empty, MenuTile.Empty, MenuTile.Empty],
        [],
    ];

    public static Rect BigTile(int i) => new(8 + i % 4 * 77, 80 + i / 4 * 60, 72, 57);
    public static Rect BigIconSource(int i) => new(1 + i % 4 * 59, 1 + i / 4 * 43, 58, 42);
    public static Rect SmallTile(int i) => new(3 + i * 53, 201, 48, 38);

    public static readonly Rect InfoBar = new(0, 21, 320, 56);

    private int _selectedBig = -1;
    private int _mouseX = -1, _mouseY = -1;

    public MainMenuScreen(ScreenHost host) : base(host) { }

    private GameState Game => Host.Game ?? throw new InvalidOperationException("Kein Spiel geladen.");

    public int SelectedBigTile
    {
        get => _selectedBig;
        set => _selectedBig = value;
    }

    public override void Update(InputState input)
    {
        _mouseX = input.MouseX;
        _mouseY = input.MouseY;

        if (input.Pressed(Key.Escape) || input.RightClick)
        {
            _selectedBig = -1;
            return;
        }

        for (var i = 0; i < BigTiles.Length; i++)
        {
            if (!input.Clicked(BigTile(i)))
                continue;
            if (i == Continue)
            {
                Host.Show(new PlaceholderScreen(Host, "Spiele laufen", this));
                return;
            }
            _selectedBig = i;
            return;
        }

        if (_selectedBig >= 0)
        {
            var tiles = SmallTiles[_selectedBig];
            for (var i = 0; i < tiles.Length; i++)
            {
                if (input.Clicked(SmallTile(i)) && !tiles[i].IsEmpty)
                {
                    Host.Show(new PlaceholderScreen(Host, tiles[i].Name, this));
                    return;
                }
            }
        }

        if (input.Pressed(Key.Space))
            Host.Show(new PlaceholderScreen(Host, "Spiele laufen", this));
    }

    public override void Draw(Surface s)
    {
        s.Palette = Assets.Palette;
        s.Blit(Assets.Image(Background), 0, 0);
        s.Blit(Assets.Image(TitleBar), 0, 0);
        DrawInfoBar(s);

        var sheet = Assets.Image(TileSheet);
        for (var i = 0; i < BigTiles.Length; i++)
        {
            var r = BigTile(i);
            Widgets.Frame(s, Assets, r, FrameStyle.Orange, Colors.TileBackground);
            s.Blit(sheet, BigIconSource(i), r.X + 7, r.Y + 7);
            if (i == _selectedBig)
                s.HLine(r.X + 4, r.Bottom - 5, r.W - 8, Colors.Yellow); // [APPROX] Markierung
        }

        if (_selectedBig >= 0)
        {
            var tiles = SmallTiles[_selectedBig];
            for (var i = 0; i < tiles.Length; i++)
            {
                var r = SmallTile(i);
                Widgets.Frame(s, Assets, r, FrameStyle.Orange, Colors.TileBackground);
                s.Blit(sheet, tiles[i].Source, r.X + 5, r.Y + 5);
                if (!tiles[i].IsEmpty && r.Contains(_mouseX, _mouseY))
                    s.HLine(r.X + 4, r.Bottom - 5, r.W - 8, Colors.Yellow);
            }
        }
    }

    private void DrawInfoBar(Surface s)
    {
        var game = Game;
        var manager = game.ActiveManager;
        var club = game.Clubs[manager.ClubIndex];
        var mini = Assets.Mini;
        var sheet = Assets.Image(TileSheet);

        Widgets.Frame(s, Assets, InfoBar, FrameStyle.Gray, Colors.PanelFill, 3);

        s.Text(mini, 6, 25, game.DateText, Colors.Yellow);
        s.TextCentered(mini, 168, 25, manager.Name, Colors.Yellow);
        s.TextCentered(mini, 272, 25, club.Name, Colors.Yellow);

        // Nachrichtenfeld
        Raised(s, new Rect(3, 32, 103, 41));
        var message = game.PlayedMatchDays == 0 ? "Vor dem ersten Spieltag." : "";
        s.Text(mini, 5, 35, message, Colors.White);

        // Symbole: Wettbewerb, Heim/Auswärts, bisherige Begegnungen, Kontostand
        var next = game.NextLeagueMatch(manager.ClubIndex);
        s.Blit(sheet, new Rect(78, 231, 14, 11), 111, 35);
        if (next is { } n)
            s.Blit(sheet, n.Home ? new Rect(54, 231, 12, 11) : new Rect(66, 231, 12, 11), 126, 35);
        s.Blit(sheet, new Rect(0, 231, 27, 11), 111, 48);
        s.Blit(sheet, new Rect(27, 231, 27, 11), 111, 61);

        s.TextCentered(mini, 184, 38, next?.Opponent.Name ?? "", Colors.White);
        s.TextCentered(mini, 184, 51, "Noch keine", Colors.White); // [APPROX] bisherige Begegnungen
        s.TextCentered(mini, 184, 64, GameState.FormatMoney(manager.Money), Colors.White);

        // Lampen [APPROX] Bedeutung: rot = Manager am Zug
        for (var i = 0; i < 6; i++)
            Widgets.Lamp(s, Assets, 226, 31 + i * 7, red: i == game.CurrentManager);

        Raised(s, new Rect(231, 32, 42, 42));
        Widgets.Head(s, Assets, manager.Head, 232, 33);
        Raised(s, new Rect(274, 32, 41, 42));
        Widgets.Logo(s, Assets, club.LogoGraphic, 275, 34);
    }

    /// <summary>Erhabener Kasten wie im Original: oben/links hell, unten/rechts dunkel mit Schatten.</summary>
    private static void Raised(Surface s, Rect r)
    {
        s.HLine(r.X, r.Y, r.W, 1);
        s.VLine(r.X, r.Y, r.H, 1);
        s.HLine(r.X, r.Bottom - 1, r.W, 241);
        s.VLine(r.Right - 1, r.Y, r.H, 241);
        s.HLine(r.X + 1, r.Bottom, r.W, 6);
        s.VLine(r.Right, r.Y + 1, r.H, 6);
    }
}
