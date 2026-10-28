using EishockeyManager.Core;
using EishockeyManager.UI.Screens.Competitions;

namespace EishockeyManager.UI.Screens.Statistics;

/// <summary>
/// „Ewige Tabelle“ (Statistiken → Totale): Punkte und Titel aller Vereine über alle Saisons, darunter die Ewige
/// Bilanz des Managers am Zug. Zeigt nur vorhandenen Zustand; die Pfeile verschieben die Liste.
/// </summary>
/// <remarks>
/// Aufbau aus em.exe (Datei 0x2E356), vermessen am Video „Die Oberfläche von EM“ (16:38–16:44):
/// <list type="bullet">
/// <item>[ORIGINAL] Fenster 9 × 6 Teile ab (50 | 23) und 9 × 4 Teile ab (50 | 154); Titel Flow zentriert um x = 100,
///   Grundlinie 41, Farbe 10, Schatten 28; Linien (57 … 259 | 51) und (57 … 259 | 173) Farbe 1.</item>
/// <item>[ORIGINAL] Pokale kb\36 (0 | 22 k, 22 × 22) bei (189 + 24 k | 29): Meister, DEB-Pokal, Europapokal;
///   Bildlauf-Pfeile kb\4 (20 | 0, 9 × 17) bei (165 | 30).</item>
/// <item>[ORIGINAL] Liste in einem Zwischenbild (Farbe 4): Zeile n Grundlinie 6 + 6 n, „n. Name“ (Mini) bei x = 9,
///   Punkte x = 105 (Mindestbreite 5), Titel x = 137 + 24 k (Mindestbreite 2); Farbe 30, Vereine der Manager 24.
///   Sichtbar: Ausschnitt (2 | 2 + 6 · Bildlauf), 200 × 72, bei (57 | 58) – zwölf Zeilen.</item>
/// <item>[ORIGINAL] Unten: „Name (Platz.),“ + Punkte (Mindestbreite 5) + „ P.“ (Flow, zentriert 67 … 252, Grundlinie 167);
///   Kopf „GESAMT … HEIM … AUSWÄRTS“ (118 | 183), Zeilen PUNKTE, TORE, SIEGE, NIEDERLAGEN, UNENTSCH. bei
///   (62 | 192 + 7 k) in Farbe 24, Werte bei x = 110 + 52 Spalte (Mindestbreite 4) in Farbe 30 (Video).</item>
/// <item>[APPROX] Bildlauf je Klick eine Zeile (Schrittweite im Original nicht gelesen).</item>
/// </list>
/// </remarks>
public sealed class EternalTableScreen : Screen
{
    public static readonly Rect TopWindow = new(50, 23, 216, 114);
    public static readonly Rect BottomWindow = new(50, 154, 216, 76);
    public static readonly Rect ArrowUp = new(165, 30, 9, 8);
    public static readonly Rect ArrowDown = new(165, 39, 9, 8);
    public const int VisibleRows = 12;
    public const string Header = "GESAMT             HEIM              AUSWÄRTS";
    public static readonly string[] RowLabels = ["PUNKTE", "TORE", "SIEGE", "NIEDERLAGEN", "UNENTSCH."];

    private readonly Screen _back;
    private int _scroll;

    public EternalTableScreen(ScreenHost host, Screen back) : base(host) => _back = back;

    public int Scroll => _scroll;

    private int MaxScroll => Math.Max(0, (Host.Game?.EternalTable.Count ?? 0) - VisibleRows);

    public override void Update(InputState input)
    {
        if (input.Pressed(Key.Escape) || input.RightClick)
        {
            Host.Show(_back);
            return;
        }
        if (input.Clicked(ArrowUp) || input.Pressed(Key.Up) || input.Wheel > 0)
            _scroll = Math.Max(0, _scroll - 1);
        else if (input.Clicked(ArrowDown) || input.Pressed(Key.Down) || input.Wheel < 0)
            _scroll = Math.Min(MaxScroll, _scroll + 1);
        else if (input.LeftClick && !TopWindow.Contains(input.MouseX, input.MouseY) && !BottomWindow.Contains(input.MouseX, input.MouseY))
            Host.Show(_back);
    }

    public override void Draw(Surface s)
    {
        s.Palette = Assets.Palette;
        s.Blit(Assets.Image(MainMenuScreen.Background), 0, 0);
        Widgets.Frame(s, Assets, TopWindow, FrameStyle.Orange);
        Widgets.Frame(s, Assets, BottomWindow, FrameStyle.Orange);
        CompetitionWidgets.Centered(s, Assets.Flow, 100, 100, 41, "Ewige Tabelle", 10, 28);
        s.HLine(57, 51, 259 - 57 + 1, 1);
        s.HLine(57, 173, 259 - 57 + 1, 1);
        var trophies = Assets.Image(EuropeanCupScreen.TrophyGraphic);
        for (var k = 0; k < 3; k++)
            s.Blit(trophies, new Rect(0, 22 * k, 22, 22), 189 + 24 * k, 29);
        s.Blit(Assets.Image(4), new Rect(20, 0, 9, 17), 165, 30);

        if (Host.Game is not { } game)
            return;
        DrawList(s, game);
        DrawBalance(s, game);
    }

    private void DrawList(Surface s, GameState game)
    {
        var buffer = new Surface { Palette = s.Palette };
        buffer.Clear(4);
        var mini = Assets.Mini;
        var managers = game.Managers.Select(m => m.ClubIndex).ToHashSet();
        var table = game.EternalTable;
        // Nur die sichtbaren Zeilen (das Original zeichnet alle und blendet ab Zeile 2 + 6 · Bildlauf ein).
        for (var n = _scroll; n < table.Count && n < _scroll + VisibleRows + 1; n++)
        {
            var e = table[n];
            var color = managers.Contains(e.Club) ? Colors.Yellow : Colors.TileBackground;
            var y = 6 + 6 * (n - _scroll);
            CompetitionWidgets.Left(buffer, mini, 9, y, CompetitionWidgets.Number(n + 1, 2) + ". " + game.Clubs[e.Club].Name, color);
            CompetitionWidgets.Left(buffer, mini, 105, y, CompetitionWidgets.Number((int)(e.Points % 100000), 5), color);
            int[] titles = [e.Championships, e.CupWins, e.EuropeanCupWins];
            for (var k = 0; k < 3; k++)
                CompetitionWidgets.Left(buffer, mini, 137 + 24 * k, y, CompetitionWidgets.Number(titles[k], 2), color);
        }
        s.Blit(buffer.Pixels, Surface.Width, new Rect(2, 2, 200, 72), 57, 58);
    }

    private void DrawBalance(Surface s, GameState game)
    {
        var managerIndex = game.CurrentManager;
        var club = game.Managers[managerIndex].ClubIndex;
        var rank = game.EternalTable.Select((e, i) => (e, i)).First(x => x.e.Club == club).i + 1;
        var title = $"{game.Clubs[club].Name} ({rank}.)," + CompetitionWidgets.Number((int)(game.Eternal(club).Points % 100000), 5) + " P.";
        CompetitionWidgets.Centered(s, Assets.Flow, 67, 252, 167, title, 10, 28);

        var mini = Assets.Mini;
        CompetitionWidgets.Left(s, mini, 118, 183, Header, Colors.Yellow);
        var (home, away) = game.ManagerBalance(managerIndex);
        MatchBalance[] columns = [home.Plus(away), home, away];
        for (var k = 0; k < RowLabels.Length; k++)
        {
            var y = 192 + 7 * k;
            CompetitionWidgets.Left(s, mini, 62, y, RowLabels[k], Colors.Yellow);
            for (var c = 0; c < 3; c++)
            {
                var b = columns[c];
                static string N(int v) => CompetitionWidgets.Number(v, 4);
                var text = k switch
                {
                    0 => N(b.PointsPlus) + ":" + N(b.PointsMinus),
                    1 => N(b.GoalsFor) + ":" + N(b.GoalsAgainst),
                    2 => N(b.Wins),
                    3 => N(b.Losses),
                    _ => N(b.Draws),
                };
                CompetitionWidgets.Left(s, mini, 110 + 52 * c, y, text, Colors.TileBackground);
            }
        }
    }
}

/// <summary>Anmeldung im Hauptmenü: Statistiken → Totale (Ewige Tabelle) und Früher.</summary>
internal static class HistoryMenuEntries
{
    [MenuEntry("Totale")]
    private static Screen Eternal(ScreenHost host, Screen back) => new EternalTableScreen(host, back);

    [MenuEntry("Früher")]
    private static Screen History(ScreenHost host, Screen back) => new HistoryScreen(host, back);
}
