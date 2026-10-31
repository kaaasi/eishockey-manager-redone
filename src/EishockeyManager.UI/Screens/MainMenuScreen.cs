using EishockeyManager.Core;
using EishockeyManager.UI.Screens.Competitions;

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

    /// <summary>„und tschüß…“: Sicherheitsfrage und Programmende (<see cref="Screens.QuitQuestion"/>, #205).</summary>
    public const int QuitTile = 7;

    /// <summary>
    /// Klickfeld „Tag weiter“ auf dem Vereinslogo der Infoleiste (#205). [ORIGINAL] em.exe Datei 0x45483–0x4551E
    /// (Hauptmenüschleife): bei Maustaste DGROUP 0x5A16 = 1 oder 3 und 33 &lt; y &lt; 72, 274 &lt; x &lt; 313 endet die
    /// Schleife mit „weiter“ (0x4551B). Vorher prüft sie den Bundestrainer (<see cref="CoachWarning"/>, #217, 0x454C2) und
    /// „Sie können nicht mehr weiterspielen.“ (DGROUP 0x176, 0x454F5) – letzteres gehört zur Sicher-/Ladesperre
    /// des Originals, die nach Entscheidung des Nutzers nicht übernommen wird (<c>docs/formate/spielstand-original.md</c>).
    /// [APPROX] Nur die linke Maustaste; was die rechte (0x5A16 = 3) im Original anders macht, ist nicht geprüft.
    /// </summary>
    public static readonly Rect ContinueLogo = new(275, 34, 38, 38);

    /// <summary>
    /// Klickfeld des Managerkopfs in der Infoleiste (#216). [ORIGINAL] em.exe Datei 0x4549D–0x454B3: im selben y-Bereich wie
    /// das Logo (33 &lt; y &lt; 72) ruft 232 &lt; x &lt; 272 die Leistenfunktion 0x120:0x48 (Datei 0x4B5F4) mit 0xFF
    /// („Manager am Zug“, Datei 0x4B68C) auf – dieselbe Nachrichtenleiste wie beim Kennzeichen „neue Nachricht“
    /// (0x4527C), danach geht die Schleife mit der Logo-Prüfung weiter.
    /// [APPROX] Das Remake wählt dabei die Kachel ab, weil Leiste und kleine Kacheln dieselbe Fläche belegen.
    /// </summary>
    public static readonly Rect ManagerHead = new(233, 34, 39, 38);

    /// <summary>
    /// Warnung des Bundestrainers beim Klick aufs Logo (#217). [ORIGINAL] Datei 0x454C2–0x454F2: Ist der Manager am Zug der
    /// Bundestrainer (DGROUP 0xA70E = 5) und zählt 0x120:0x66 (Modus 0, Datei 0x4BE17: Kaderplätze mit Feld 0x0F ≠ 0)
    /// weniger als 10 Spieler, erscheint die Meldung „Wollen Sie die WM wirklich / mit SO wenigen Spielern bestreiten ?“
    /// (DGROUP 0x403B/0x4056, über 0x38:0x8C8 = Datei 0xC0B8) und die Schleife beginnt von vorn – der Tag läuft nicht weiter.
    /// [APPROX] Aussehen und Lage der Meldung (Rahmen, Knopf „Na gut.“) sind nicht aus dem Video belegt; das Original
    /// wertet die Antwort nicht aus (Rücksprung 0x535B in jedem Fall).
    /// </summary>
    public static readonly string[] CoachWarning = ["Wollen Sie die WM wirklich", "mit SO wenigen Spielern bestreiten ?"];

    /// <summary>[ORIGINAL] Mindestzahl Kaderspieler, ab der die Warnung entfällt (Datei 0x454D6: <c>cmp al,0xA</c>).</summary>
    public const int CoachMinPlayers = 10;

    /// <summary>Meldung über dem Hauptmenü (Bundestrainer-Warnung, #217).</summary>
    public Team.MessageBox Message { get; } = new();

    public static MenuTile[][] SmallTiles => MenuTiles.Small;

    public static Rect BigTile(int i) => new(8 + i % 4 * 77, 80 + i / 4 * 60, 72, 57);
    public static Rect BigIconSource(int i) => new(1 + i % 4 * 59, 1 + i / 4 * 43, 58, 42);
    public static Rect SmallTile(int i) => new(3 + i * 53, 201, 48, 38);

    public static readonly Rect InfoBar = new(0, 21, 320, 56);

    private int _selectedBig = -1;
    private int _mouseX = -1, _mouseY = -1;

    /// <summary>
    /// Nachrichtenleiste sichtbar (PV-08, #170). [ORIGINAL] em.exe Datei 0x4527C–0x45291 (Hauptmenüschleife): Die Leiste
    /// wird nur gezeichnet, wenn für den aktiven Manager das Kennzeichen „neue Nachricht“ (DGROUP 0x5A50 + Manager) gesetzt
    /// ist; es wird dabei gelöscht (Leistenfunktion Datei 0x4B5F4, Aufruf mit 0xFF). Ohne Nachricht bleibt der Hintergrund.
    /// [APPROX] Das Kennzeichen entspricht hier einer ungelesenen Nachricht; die Leiste bleibt, bis das Menü neu
    /// aufgebaut wird (Rückkehr aus einem Bildschirm, Tageswechsel, Managerwechsel) – das Original zeichnet das Menü
    /// dann ohne Leiste neu. Klick auf den Managerkopf als zweiter Auslöser: <see cref="ManagerHead"/> (#216).
    /// </summary>
    private bool _newsBar;

    /// <summary>Nachrichtenleiste gerade sichtbar (für Tests).</summary>
    public bool NewsBarVisible => _newsBar;

    public MainMenuScreen(ScreenHost host) : base(host) { }

    /// <summary>
    /// Das Hauptmenü eines Zuges ist da: Zähler des automatischen Speicherns fortschreiben (Spur „speichern-dialog“,
    /// <see cref="SaveLoad.AutoSave"/>). Schreibt nur eine Datei, ändert den Spielzustand nicht.
    /// </summary>
    public override void Enter()
    {
        _newsBar = false; // Menü wird neu aufgebaut: Leiste weg, bis eine neue Nachricht kommt
        SaveLoad.AutoSave.OnMainMenu(Host);
    }

    /// <summary>Offene Sicherheitsfrage von „und tschüß…“ oder null.</summary>
    public QuitQuestion? QuitQuestion { get; private set; }

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
        if (QuitQuestion is not null)
        {
            switch (QuitQuestion.Evaluate(input))
            {
                case QuitQuestion.Outcome.Quit:
                    QuitQuestion = null;
                    Host.Exit();
                    break;
                case QuitQuestion.Outcome.Cancel:
                    QuitQuestion = null;
                    break;
            }
            return;
        }
        if (Message.Update(input))
            return;
        if (_selectedBig < 0 && ManagerArea.NewsBar.HasUnread(Game))
            _newsBar = true;

        // Credits (#207): linker oder rechter Klick in die Titelleiste, vor dem Abwählen per Rechtsklick (0x45483).
        if ((input.LeftClick || input.RightClick) && CreditsScreen.TitleArea.Contains(input.MouseX, input.MouseY))
        {
            Host.Show(new CreditsScreen(Host, this));
            return;
        }

        if (input.Pressed(Key.Escape) || input.RightClick)
        {
            _selectedBig = -1;
            return;
        }

        // Managerkopf (#216): zeigt die Nachrichtenleiste des Managers am Zug, auch ohne neue Nachricht (0x4549D).
        if (input.Clicked(ManagerHead))
        {
            _selectedBig = -1;
            _newsBar = true;
            return;
        }

        // Tag weiter: Klick aufs Vereinslogo (#205); die Leertaste hat diese Funktion im Original nicht.
        if (input.Clicked(ContinueLogo))
        {
            // Bundestrainer mit zu kleinem Kader: Warnung statt Tagesablauf (#217, 0x454C2).
            // [APPROX] Ohne ernannten Bundestrainer (Modus 1 vor dem ersten WM-Termin) gibt es keinen Kader zu zählen.
            if (Game.NationalCoachMenu && Game.NationalCoach is { } coach && coach.Squad.Count < CoachMinPlayers)
            {
                Message.Show(CoachWarning);
                return;
            }
            Continue_();
            return;
        }

        for (var i = 0; i < BigTiles.Length; i++)
        {
            if (!input.Clicked(BigTile(i)))
                continue;
            if (i == QuitTile)
            {
                _selectedBig = -1;
                QuitQuestion = new QuitQuestion(Game.ActiveManager.Name);
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
                if (input.Clicked(SmallTile(i)) && !tiles[i].IsEmpty && !NationalMenu.IsLocked(Game, tiles[i]))
                {
                    Host.Show(OpenTile(tiles[i]));
                    return;
                }
            }
        }

        else if (_newsBar && ManagerArea.NewsBar.Update(Host, this, input))
            return;
    }

    /// <summary>
    /// Tag weiter (Klick aufs Vereinslogo): Im Hot-Seat gibt der Manager am Zug nur an den nächsten noch nicht bereiten Manager ab
    /// (Hauptmenü bleibt, Infoleiste zeigt den neuen Manager). Erst der letzte bestätigt den Tag, dann läuft er
    /// bis zum nächsten eigenen Spiel bzw. zum Saisonende (Paket saison).
    /// </summary>
    private void Continue_()
    {
        _selectedBig = -1;
        _newsBar = false; // neuer Tag bzw. nächster Manager: Menü wird neu aufgebaut
        if (!Game.IsLastTurnOfDay)
        {
            Game.Continue();
            // Der nächste Manager ist am Zug, das Hauptmenü bleibt stehen: auch das ist ein neuer Hauptmenü-Aufruf
            // für das automatische Speichern (R3C-01, em.exe 0x44E49–0x44E69).
            SaveLoad.AutoSave.OnMainMenu(Host);
            return;
        }
        Host.Show(new MatchDayScreen(Host, this));
    }

    /// <summary>Bildschirm zu einer kleinen Kachel – angemeldet per <see cref="MenuEntryAttribute"/>, sonst Platzhalter.</summary>
    private Screen OpenTile(MenuTile tile) =>
        NationalMenu.Open(Game, tile.Name, Host, this) // Menü des Bundestrainers (Spur wettbewerbe-weltkugel)
        ?? (MenuEntries.TryCreate(tile.Name, Host, this, out var screen) ? screen! : new PlaceholderScreen(Host, tile.Name, this));

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
                var locked = NationalMenu.IsLocked(Game, tiles[i]); // Kachelsperren (Spur wettbewerbe-weltkugel)
                s.Blit(sheet, locked ? NationalMenu.LockedIcon : tiles[i].Source, r.X + 5, r.Y + 5);
                if (!tiles[i].IsEmpty && !locked && r.Contains(_mouseX, _mouseY))
                    s.HLine(r.X + 4, r.Bottom - 5, r.W - 8, Colors.Yellow);
            }
        }
        else if (_newsBar)
            ManagerArea.NewsBar.Draw(s, Assets, Game); // Nachrichtenleiste (Paket „manager“), nur mit Nachricht (#170)

        QuitQuestion?.Draw(s, Assets);
        Message.Draw(s, Assets);
    }

    private void DrawInfoBar(Surface s)
    {
        var game = Game;
        var manager = game.ActiveManager;
        var team = NationalMenu.Team(game); // Bundestrainer: Nationalteam statt Verein (Spur wettbewerbe-weltkugel)
        var club = game.Clubs[team];
        var mini = Assets.Mini;
        var sheet = Assets.Image(TileSheet);

        Widgets.Frame(s, Assets, InfoBar, FrameStyle.Gray, Colors.PanelFill, 3);

        s.Text(mini, 6, 25, game.DateText, Colors.Yellow);
        s.TextCentered(mini, 168, 25, manager.Name, Colors.Yellow);
        s.TextCentered(mini, 272, 25, club.Name, Colors.Yellow);

        // Nachrichtenfeld
        Raised(s, new Rect(3, 32, 103, 41));
        // Datum und „n.Spieltag“ beziehen sich auf den nächsten Termin (siehe docs/saison.md)
        var next = game.UpcomingMatch(team);
        if (game.NationalCoachMenu)
        {
            // [APPROX] Ohne Liga keine Tabellenkurve; der Bundestrainer sieht hier nichts (kein Videobeleg).
        }
        else if (game.PlayedMatchDays == 0)
            s.Text(mini, 5, 35, "Vor dem ersten Spieltag.", Colors.White);
        else if (game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver)
        {
            if (game.UpcomingLeagueMatchDay is { } md)
                s.TextCentered(mini, 55, 35, $"{md}.Spieltag", Colors.White);
            if (next?.Opponent.Index is { } o)
                DrawRankCurve(s, game, o, Colors.White);
            DrawRankCurve(s, game, team, Colors.Yellow);
        }

        // Symbole: Wettbewerb, Heim/Auswärts, bisherige Begegnungen, Kontostand
        // WM-Spiel des Bundestrainers: Weltkugel auch nach der Vereinssaison (Modus 4) und kein Heim-/Auswärtssymbol
        // (Spur pixel-verein, PV-04; Videos bundestrainer_00030, eisbaeren_f24_00039).
        var worldCup = next?.Competition == Competition.WorldCup;
        s.Blit(sheet, worldCup ? SeasonInfo.IconSource(Competition.WorldCup) : SeasonInfo.CompetitionIcon(game, next?.Competition), 111, 35);
        if (next is { } n && !worldCup)
            s.Blit(sheet, n.Home ? new Rect(54, 231, 12, 11) : new Rect(66, 231, 12, 11), 126, 35);
        s.Blit(sheet, new Rect(0, 231, 27, 11), 111, 48);
        s.Blit(sheet, new Rect(27, 231, 27, 11), 111, 61);

        s.TextCentered(mini, 184, 38, next?.Opponent.Name ?? SeasonInfo.NoMatchText(game), Colors.White);
        s.TextCentered(mini, 184, 51, MeetingsText(game, team, next?.Opponent.Index), Colors.White);
        s.TextCentered(mini, 184, 64, GameState.FormatMoney(NationalMenu.Money(game)), Colors.White);

        // Warnlampen des Managers am Zug (Spur pixel-verein, PV-03; Regeln und Belege in MenuLamps).
        var lamps = MenuLamps.Red(game);
        for (var i = 0; i < MenuLamps.Count; i++)
            Widgets.Lamp(s, Assets, MenuLamps.X, MenuLamps.Y(i), red: lamps[i]);

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
