using EishockeyManager.Core;

namespace EishockeyManager.UI.Screens;

/// <summary>
/// Hauptmenü / Wochenübersicht: Titelleiste kb\20, Infoleiste, acht große Kacheln (kb\3) und
/// – nach Auswahl einer großen Kachel – sechs kleine Kacheln am unteren Rand.
/// </summary>
public sealed class MainMenuScreen : Screen
{
    public const int Background = 22;
    public const int TitleBar = 20;
    public const int TileSheet = 3;

    public static string[] BigTiles => MenuTiles.Big;

    public const int Continue = 7;

    public static MenuTile[][] SmallTiles => MenuTiles.Small;

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
                Continue_();
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
                    Host.Show(OpenTile(tiles[i]));
                    return;
                }
            }
        }

        if (input.Pressed(Key.Space))
            Continue_();
    }

    /// <summary>„und tschüß…“: weiter bis zum nächsten eigenen Spiel bzw. zum Saisonende (Paket saison).</summary>
    private void Continue_()
    {
        _selectedBig = -1;
        Host.Show(new MatchDayScreen(Host, this));
    }

    /// <summary>Bildschirm zu einer kleinen Kachel – angemeldet per <see cref="MenuEntryAttribute"/>, sonst Platzhalter.</summary>
    private Screen OpenTile(MenuTile tile) =>
        MenuEntries.TryCreate(tile.Name, Host, this, out var screen) ? screen! : new PlaceholderScreen(Host, tile.Name, this);

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
        // Datum und „n.Spieltag“ beziehen sich auf den nächsten Termin (siehe docs/saison.md)
        var next = game.UpcomingMatch(manager.ClubIndex);
        if (game.PlayedMatchDays == 0)
            s.Text(mini, 5, 35, "Vor dem ersten Spieltag.", Colors.White);
        else if (game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver)
        {
            if (game.UpcomingLeagueMatchDay is { } md)
                s.TextCentered(mini, 55, 35, $"{md}.Spieltag", Colors.White);
            if (next?.Opponent.Index is { } o)
                DrawRankCurve(s, game, o, Colors.White);
            DrawRankCurve(s, game, manager.ClubIndex, Colors.Yellow);
        }

        // Symbole: Wettbewerb, Heim/Auswärts, bisherige Begegnungen, Kontostand
        s.Blit(sheet, SeasonInfo.CompetitionIcon(game, next?.Competition), 111, 35);
        if (next is { } n)
            s.Blit(sheet, n.Home ? new Rect(54, 231, 12, 11) : new Rect(66, 231, 12, 11), 126, 35);
        s.Blit(sheet, new Rect(0, 231, 27, 11), 111, 48);
        s.Blit(sheet, new Rect(27, 231, 27, 11), 111, 61);

        s.TextCentered(mini, 184, 38, next?.Opponent.Name ?? SeasonInfo.NoMatchText(game), Colors.White);
        s.TextCentered(mini, 184, 51, MeetingsText(game, manager.ClubIndex, next?.Opponent.Index), Colors.White);
        s.TextCentered(mini, 184, 64, GameState.FormatMoney(manager.Money), Colors.White);

        // Lampen [APPROX] Bedeutung: rot = Manager am Zug
        for (var i = 0; i < 6; i++)
            Widgets.Lamp(s, Assets, 226, 31 + i * 7, red: i == game.CurrentManager);

        Raised(s, new Rect(231, 32, 42, 42));
        Widgets.Head(s, Assets, manager.Head, 232, 33);
        Raised(s, new Rect(274, 32, 41, 42));
        Widgets.Logo(s, Assets, club.LogoGraphic, 275, 34);
    }

    /// <summary>
    /// Bisherige Begegnungen aus Sicht des eigenen Vereins, z. B. „A 1:4,H 5:4“; „Noch keine“ ohne Begegnung.
    /// [APPROX] Ob das Original eigene Tore zuerst nennt, ist noch offen.
    /// </summary>
    public static string MeetingsText(GameState game, int club, int? opponent)
    {
        if (opponent is not { } o)
            return "";
        var parts = game.Meetings(club, o).Select(r =>
        {
            var (f, a, home) = r.For(club);
            return $"{(home ? 'H' : 'A')} {f}:{a}";
        }).ToList();
        return parts.Count == 0 ? "Noch keine" : string.Join(",", parts.TakeLast(4));
    }

    /// <summary>
    /// Tabellenkurve im Nachrichtenfeld: Platz nach jedem Spieltag (oben = Platz 1), rechts der aktuelle Platz.
    /// [APPROX] Maßstab und Lage aus Videobildern geschätzt.
    /// </summary>
    private void DrawRankCurve(Surface s, GameState game, int club, byte color)
    {
        var history = game.RankHistory(club);
        if (history.Count == 0)
            return;
        const int left = 6, right = 84, top = 43, bottom = 68;
        // Im Original füllt die Kurve schon nach 17 Spieltagen die ganze Breite → Maßstab = bisherige Spieltage
        var n = Math.Max(1, history.Count - 1);
        int X(int i) => left + i * (right - left) / n;
        int Y(int rank) => top + (rank - 1) * (bottom - top) / 11;
        for (var i = 0; i < history.Count; i++)
        {
            var x0 = X(i);
            var y0 = Y(history[i]);
            if (i == 0)
            {
                s.Set(x0, y0, color);
                continue;
            }
            var x1 = X(i - 1);
            var y1 = Y(history[i - 1]);
            Line(s, x1, y1, x0, y0, color);
        }
        s.Text(Assets.Mini, right + 3, Y(history[^1]) - 2, $"{history[^1]}.", color);
    }

    private static void Line(Surface s, int x0, int y0, int x1, int y1, byte color)
    {
        int dx = Math.Abs(x1 - x0), dy = -Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1, err = dx + dy;
        while (true)
        {
            s.Set(x0, y0, color);
            if (x0 == x1 && y0 == y1)
                break;
            var e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
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
