using EishockeyManager.Core;
using EishockeyManager.UI.Screens.Competitions;

namespace EishockeyManager.UI.Screens.Statistics;

/// <summary>Ansicht der Ranglisten: alle Spieler der Liga oder nur die der Managervereine (Knöpfe „Liga“/„Manager“).</summary>
public enum PlayerStatisticsView
{
    League = 0,
    Managers = 1,
}

/// <summary>
/// Statistiken → Spieler: „Die besten SCORER der Liga“ (Feldspieler) und „Die besten KEEPER der Liga“ (Torhüter).
/// Zeigt nur vorhandenen Zustand (<see cref="PlayerStatistics.Ranking"/>); die Knöpfe ändern nur die Ansicht.
/// </summary>
/// <remarks>
/// Aufbau aus em.exe Ov 0xC8, Funktion Datei 0x25E30 mit Argument 0 (Menüsprung Datei 0x4580B); Vermessung nur aus dem
/// Code – die Referenzbilder waren für diese Sitzung nicht erreichbar, ein Pixelvergleich am Video steht aus.
/// <list type="bullet">
/// <item>[ORIGINAL] Fenster 11 × 10 Teile ab (30 | 24), Stil wie die Ewige Tabelle (0x30:0x335 mit 0, 2; Datei 0x25F1D).</item>
/// <item>[ORIGINAL] Zwei Blöcke ab Grundlinie y₀ = 35 und 142 (Datei 0x26186: 0x23, + 0x6B): Linie (36 … 284 | y₀ + 11)
///   Farbe 1; Titel Flow zentriert 35 … 285 auf y₀, Farbe 10, Schatten 28: „Die besten “ + Sortierung (Feldspieler) bzw.
///   „KEEPER“ + „ der “ + „Liga“/„Manager“ (Texte DGROUP 0x6B1, 0xC70 ff.); Kopf (Mini, Farbe 1) „PL. SPIELER … VEREIN“
///   bei x = 38 und „SCORES  TORE/SPIEL“ bzw. „GEGENTORE  MIN.(G/SP.)“ bei x = 190, Grundlinie y₀ + 9.</item>
/// <item>[ORIGINAL] Zeilen (Datei 0x26519–0x26994): höchstens 12 Feldspieler ab Grundlinie 54 und 3 Torhüter ab 161,
///   Abstand 7; Farbe 24 für Spieler eines Managervereins (Vereinsfeld 0x16 ≠ 0), sonst 30. „Platz. Name“ (Platz mit
///   Mindestbreite 2) bei x = 38, Verein bei x = 114. Feldspieler: „Punkte(Tore/Vorlagen)“ (je Mindestbreite 2) bei
///   x = 200 und Tore je Spiel „0.47“ bei x = 252; Torhüter: Gegentore (Mindestbreite 3) bei x = 200 und
///   „Minuten(Gegentore je 60 Minuten)“ (Mindestbreiten 4 und 2) bei x = 237. Ansicht „Manager“: nur Spieler der
///   Managervereine, der Platz bleibt der Ligaplatz.</item>
/// <item>[ORIGINAL] „NOCH KEINE EINTRÄGE VORHANDEN...“ zentriert 35 … 285 auf Grundlinie 60, Farbe 7, Schatten 1, wenn der
///   zuletzt aufgenommene Spieler der Liste ein Feldspieler ist und bis dahin kein Feldspieler gezeigt wurde (Datei
///   0x269B0 prüft die Zeilenlage des letzten Aufgenommenen auf 54, auch wenn dieser selbst dort gezeigt wird). Bei ganz
///   leerer Liste erscheint der Hinweis deshalb nicht – so im Code, am Video noch nicht geprüft.</item>
/// <item>[ORIGINAL] Knöpfe (0x120:0x75): TORSCH., VORLEGER, SCORER bei (54 + 79 k | 182), „Liga“ bei (54 | 195), „Manager“
///   bei (212 | 195); Vorgabe SCORER und Liga. Verlassen nur mit der rechten Maustaste (Datei 0x26D4B: Ereignis 3).</item>
/// <item>[ORIGINAL] Knöpfe nie gedrückt (0x4BFF2: Zustand immer 0, die übergebene Farbe 17 bzw. 10 bleibt ungenutzt):
///   Bild kb\2 (17 | 179) 57 × 12, Text Mini zentriert x … x + 59, Grundlinie y + 7, Farbe 29, Schatten 1 (Datei
///   0x4C154–0x4C188). Texte der Ansicht „LIGA“/„MANAGER“ aus DGROUP 0x974/0x978 (Großbuchstaben; der Titel nutzt
///   „Liga“/„Manager“). Am Video bestätigt (<c>folge15_01007</c>, Spur statistiken-verein). Escape verlässt wie die
///   rechte Maustaste ([APPROX]).</item>
/// <item>[ORIGINAL] Linksklick auf eine Zeile öffnet das Spielerdaten-Fenster 0x041AC (Datei 0x26B13–0x26BF5), wenn der
///   Spieler im Kader eines Managers steht: Zeile = (y − erste Grundlinie + 4) / 7 des Blocks, dessen erste Grundlinie − 5
///   über y liegt (x wird nicht geprüft); das Fenster öffnet für diesen Manager (DGROUP 0xA70E vorübergehend gesetzt),
///   Sichtbarkeit nach <see cref="Team.PlayerDataContext.Create"/>. [APPROX] „Kaufpreis“ (Kaderplatz +0x2C) gibt es im
///   Remake nicht; gezeigt wird der Spielerwert.</item>
/// </list>
/// </remarks>
public sealed class PlayerStatisticsScreen : Screen
{
    public static readonly Rect Window = new(30, 24, 11 * 24, 10 * 19);
    public static readonly int[] BlockBaseline = [35, 142];
    public static readonly int[] FirstRow = [54, 161];
    public const int RowPitch = 7;
    public static readonly int[] MaxRows = [12, 3];
    public const int ModeButtonY = 182;
    public const int ViewButtonY = 195;
    public static readonly int[] ModeButtonX = [54, 133, 212];
    public static readonly int[] ViewButtonX = [54, 212];
    public static readonly string[] ModeText = ["TORSCH.", "VORLEGER", "SCORER"];
    public static readonly string[] ViewText = ["Liga", "Manager"];

    /// <summary>[ORIGINAL] Beschriftung der Ansichtsknöpfe (DGROUP 0x974/0x978).</summary>
    public static readonly string[] ViewButtonText = ["LIGA", "MANAGER"];
    public const string GoalieTitle = "KEEPER";
    public const string HeaderLeft = "PL. SPIELER                 VEREIN";
    public static readonly string[] HeaderRight = ["       SCORES    TORE/SPIEL", "GEGENTORE     MIN.(G/SP.)"];
    public const string NoEntries = "NOCH KEINE EINTRÄGE VORHANDEN...";

    private readonly Screen _back;
    private readonly Team.PlayerInfoPopup _info = new();

    /// <summary>Spielerdaten-Fenster (offen nach Klick auf einen Spieler eines Managerkaders).</summary>
    public Team.PlayerInfoPopup Info => _info;

    public PlayerStatisticsScreen(ScreenHost host, Screen back) : base(host) => _back = back;

    public SkaterRanking Mode { get; private set; } = SkaterRanking.Scorer;

    public PlayerStatisticsView View { get; private set; } = PlayerStatisticsView.League;

    /// <summary>Titel eines Blocks (0 Feldspieler, 1 Torhüter).</summary>
    public string Title(int block) =>
        "Die besten " + (block == 0 ? ModeText[(int)Mode] : GoalieTitle) + " der " + ViewText[(int)View];

    /// <summary>Gezeigte Zeile: Eintrag, Block und Grundlinie.</summary>
    public sealed record Row(PlayerRankingEntry Entry, int Block, int Baseline, byte Color);

    /// <summary>Zeilen wie Datei 0x26365–0x26994 und ob der Hinweis „Noch keine Einträge“ erscheint.</summary>
    public (IReadOnlyList<Row> Rows, bool NoEntriesHint) Layout(GameState game)
    {
        var rows = new List<Row>();
        if (game.Clubs[game.ActiveManager.ClubIndex].League is not { } league)
            return (rows, false);
        var managerClubs = game.ActiveManagers.Select(m => m.ClubIndex).ToHashSet();
        int[] next = [FirstRow[0], FirstRow[1]];
        int[] shown = [0, 0];
        var last = -1;
        foreach (var entry in PlayerStatistics.Ranking(game, league, Mode))
        {
            var block = entry.IsGoalie ? 1 : 0;
            last = next[block];
            if (shown[block] >= MaxRows[block])
                continue;
            if (View == PlayerStatisticsView.Managers && !managerClubs.Contains(entry.Club.Index))
                continue;
            var color = managerClubs.Contains(entry.Club.Index) ? Colors.Yellow : Colors.TileBackground;
            rows.Add(new Row(entry, block, next[block], color));
            shown[block]++;
            next[block] += RowPitch;
        }
        return (rows, last == FirstRow[0]);
    }

    /// <summary>[ORIGINAL] Spalte bei x = 200 (Datei 0x265C7–0x266A9).</summary>
    public static string FirstColumn(PlayerRankingEntry e)
    {
        var p = e.Player;
        if (e.IsGoalie)
            return CompetitionWidgets.Number(p.GoalsAgainst, 3);
        return CompetitionWidgets.Number(p.Goals + p.Assists, 2) + "(" + CompetitionWidgets.Number(p.Goals, 2) + "/"
               + CompetitionWidgets.Number(p.Assists, 2) + ")";
    }

    /// <summary>[ORIGINAL] Spalte bei x = 252 bzw. 237 (Datei 0x26713–0x26936).</summary>
    public static string SecondColumn(PlayerRankingEntry e)
    {
        var p = e.Player;
        if (!e.IsGoalie)
        {
            var v = PlayerStatistics.GoalsPerGameHundredths(p);
            return (v / 100) + "." + (v % 100 < 10 ? "0" : "") + (v % 100);
        }
        var g = PlayerStatistics.GoalsAgainstPerGameHundredths(p);
        var rest = g - g / 100 * 100;
        return CompetitionWidgets.Number(p.GoalieMinutes, 4) + "(" + CompetitionWidgets.Number((int)(g / 100), 2) + "."
               + (rest / 10) + (rest % 10) + ")";
    }

    /// <summary>Zeile unter der Maus (Datei 0x26B13–0x26B84) oder null.</summary>
    public Row? RowAt(GameState game, int mouseY)
    {
        var rows = Layout(game).Rows;
        for (var block = 0; block < 2; block++)
        {
            var top = FirstRow[block];
            if (top - 5 >= mouseY)
                continue;
            var inBlock = rows.Where(r => r.Block == block).ToList();
            if (mouseY >= top - 4 + RowPitch * MaxRows[block])
                continue;
            var index = (mouseY - top + 4) / RowPitch;
            if (index < inBlock.Count)
                return inBlock[index];
        }
        return null;
    }

    /// <summary>
    /// Öffnet das Spielerdaten-Fenster für einen Spieler aus dem Kader eines Managers (Datei 0x26BA3–0x26BF8: Suche über
    /// alle Manager, Fenster als dieser Manager). Liefert false, wenn der Spieler in keinem Managerkader steht.
    /// </summary>
    public static bool OpenPlayerData(Team.PlayerInfoPopup info, GameState game, Player player)
    {
        foreach (var manager in game.Managers)
        {
            var club = game.Clubs[manager.ClubIndex];
            if (!club.Players.Contains(player))
                continue;
            var context = Team.PlayerDataContext.Create(game, player, club, club, manager.Level);
            info.Show(player, context.MarketValue, context);
            return true;
        }
        return false;
    }

    public override void Update(InputState input)
    {
        if (_info.Update(input))
            return;
        if (input.LeftClick && Host.Game is { } game && RowAt(game, input.MouseY) is { } row
            && OpenPlayerData(_info, game, row.Entry.Player))
            return;
        if (input.Pressed(Key.Escape) || input.RightClick)
        {
            Host.Show(_back);
            return;
        }
        for (var k = 0; k < ModeButtonX.Length; k++)
            if (input.Clicked(Widgets.ButtonRect(ModeButtonX[k], ModeButtonY)))
                Mode = (SkaterRanking)k;
        for (var k = 0; k < ViewButtonX.Length; k++)
            if (input.Clicked(Widgets.ButtonRect(ViewButtonX[k], ViewButtonY)))
                View = (PlayerStatisticsView)k;
    }

    public override void Draw(Surface s)
    {
        s.Palette = Assets.Palette;
        s.Blit(Assets.Image(MainMenuScreen.Background), 0, 0);
        Widgets.Frame(s, Assets, Window, FrameStyle.Orange);
        var mini = Assets.Mini;
        for (var block = 0; block < 2; block++)
        {
            var y0 = BlockBaseline[block];
            s.HLine(36, y0 + 11, 284 - 36 + 1, 1);
            CompetitionWidgets.Centered(s, Assets.Flow, 35, 285, y0, Title(block), 10, 28);
            CompetitionWidgets.Left(s, mini, 38, y0 + 9, HeaderLeft, 1);
            CompetitionWidgets.Left(s, mini, 190, y0 + 9, HeaderRight[block], 1);
        }
        if (Host.Game is { } game)
        {
            var (rows, hint) = Layout(game);
            foreach (var row in rows)
            {
                var e = row.Entry;
                CompetitionWidgets.Left(s, mini, 38, row.Baseline, CompetitionWidgets.Number(e.Rank, 2) + ". " + e.Player.Name, row.Color);
                CompetitionWidgets.Left(s, mini, 114, row.Baseline, e.Club.Name, row.Color);
                CompetitionWidgets.Left(s, mini, 200, row.Baseline, FirstColumn(e), row.Color);
                CompetitionWidgets.Left(s, mini, 252 - 15 * row.Block, row.Baseline, SecondColumn(e), row.Color);
            }
            if (hint)
                CompetitionWidgets.Centered(s, mini, 35, 285, 60, NoEntries, Colors.White, 1);
        }
        for (var k = 0; k < ModeButtonX.Length; k++)
            DrawButton(s, ModeButtonX[k], ModeButtonY, ModeText[k]);
        for (var k = 0; k < ViewButtonX.Length; k++)
            DrawButton(s, ViewButtonX[k], ViewButtonY, ViewButtonText[k]);
        _info.Draw(s, Assets);
    }

    /// <summary>[ORIGINAL] Knopf 0x4BFF2 mit Zustand 0 (Datei 0x4C154–0x4C188).</summary>
    private void DrawButton(Surface s, int x, int y, string text)
    {
        s.Blit(Assets.Image(2), new Rect(17, 179, Widgets.ButtonWidth, Widgets.ButtonHeight), x, y);
        CompetitionWidgets.Centered(s, Assets.Mini, x, x + 59, y + 7, text, 29, 1);
    }
}

/// <summary>Anmeldung im Hauptmenü: Statistiken → Spieler und Fairplay (Spur statistiken).</summary>
internal static class PlayerStatisticsMenuEntries
{
    [MenuEntry("Spieler")]
    private static Screen Players(ScreenHost host, Screen back) => new PlayerStatisticsScreen(host, back);

    [MenuEntry("Fairplay")]
    private static Screen FairPlay(ScreenHost host, Screen back) => new FairplayScreen(host, back);
}
