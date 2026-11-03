using EishockeyManager.Core;

namespace EishockeyManager.UI.Screens;

/// <summary>
/// Bildschirm „Saisonende“ (29. April): oben die Titelträger (Weltmeister, DEB-Pokal, Deutscher Meister, Europapokal),
/// unten je Liga die neuen Vereine der nächsten Saison – Absteiger rot mit Pfeil nach unten, Aufsteiger grün mit
/// Pfeil nach oben, der Deutsche Meister blau unter „Bundesliga“. Vermessen aus dem Video „MEISTER werden in
/// 75 Minuten“ (11:50, 18:36, 25:52). Klick oder Taste → Fenster mit dem eigenen Abschneiden.
/// </summary>
public sealed class SeasonEndScreen : Screen
{
    public static readonly Rect Window = new(4, 2, 312, 208);
    public static readonly int[] TitleColumns = [42, 122, 202, 282];
    // [ORIGINAL] em.exe Datei 0x4CCB4 (R-K2): Zeichen bei x = 100 · Liga + 20, Vereinsname bei x = 100 · Liga + 28, Schatten Farbe 0.
    public static readonly int[] LeagueColumns = [20, 120, 220];
    public static readonly int[] LeagueHeaderCenters = [52, 159, 264];
    public const int TopRowY = 152;
    public const int BottomRowY = 172;
    public const int RowPitch = 8;

    // Farben: nächste Palettenfarbe zum Videobild (Fenster dunkler als die übrigen grau-blauen Fenster)
    public const byte WindowFill = 4;
    private const byte TextColor = 1;
    private const byte HeadingColor = 7;
    private const byte Shadow = 0;
    private const byte LineColor = 1;
    // [ORIGINAL] Datei 0x4CCB4: Farben der Zeichen DGROUP 0x5358[Art] = 18 (Absteiger), 12 (Aufsteiger), 22 (Meister).
    // Die Paletteneinträge 238/13/151 waren nur die nächsten Farben zum Videobild (andere RGB-Werte).
    private const byte ArrowRed = 18;
    private const byte ArrowGreen = 12;
    private const byte ArrowBlue = 22;
    public const int NameOffset = 8;

    // Pfeile und Kreis aus dem Mini-Font: '&' = Pfeil nach unten, '@' = Pfeil nach oben, '*' = Kreis
    private const string ArrowDown = "&";
    private const string ArrowUp = "@";
    private const string Circle = "*";

    private static readonly Title[] TitleOrder = [Title.WorldChampion, Title.CupWinner, Title.GermanChampion, Title.EuropeanCupWinner];
    private static readonly (string, string)[] TitleLabels =
        [("Weltmeister", ""), ("DEB-", "Pokal"), ("Deutscher", "Meister"), ("Europa-", "Pokal")];

    private readonly MainMenuScreen _menu;

    public SeasonEndScreen(ScreenHost host, MainMenuScreen menu) : base(host) => _menu = menu;

    private GameState Game => Host.Game!;
    private SeasonRecord Record => Game.History[^1];

    public override void Update(InputState input)
    {
        if (!(input.LeftClick || input.RightClick || input.Keys.Count > 0))
            return;
        // [ORIGINAL] Datei 0x5641A–0x56444: in der letzten Saison endet das Programm nach Austausch und Highscore, vor dem
        // Fenster „Ereignisse“ (Datei 0x5645D → 0x26F20) – hier direkt zum Spielende (S-K5). [APPROX] ob das Fenster mit den
        // Titelträgern (dieser Bildschirm) davor noch erscheint: im Video nicht belegbar (kein Spielende aufgenommen).
        if (Game.IsGameOver)
            Host.Show(new SeasonEndGameOverScreen(Host, null, null));
        else
            Host.Show(new SeasonEndSummaryScreen(Host, _menu));
    }

    public override void Draw(Surface s)
    {
        var mini = Assets.Mini;
        var flow = Assets.Flow;
        s.Palette = Assets.Palette;
        s.Blit(Assets.Image(MainMenuScreen.Background), 0, 0);
        Widgets.Frame(s, Assets, Window, FrameStyle.Orange, WindowFill);
        Widgets.ShadowText(s, Assets.Bold, 160, 8, "Saisonende", HeadingColor, Shadow);
        Line(s, 28);
        Line(s, 129);

        var sheet = Assets.Image(MainMenuScreen.TileSheet);
        for (var i = 0; i < TitleOrder.Length; i++)
        {
            var x = TitleColumns[i];
            var icon = TitleOrder[i] switch
            {
                Title.WorldChampion => SeasonInfo.IconSource(Competition.WorldCup),
                Title.CupWinner => SeasonInfo.IconSource(Competition.DebCup),
                Title.GermanChampion => SeasonInfo.IconSource(Competition.League),
                _ => SeasonInfo.IconSource(Competition.EuropeanCup),
            };
            s.Blit(sheet, icon, x - 7, 35);
            var (l1, l2) = TitleLabels[i];
            Widgets.ShadowText(s, flow, x, 46, l1, HeadingColor, Shadow);
            if (l2.Length > 0)
                Widgets.ShadowText(s, flow, x, 54, l2, HeadingColor, Shadow);

            if (Record.Titles.TryGetValue(TitleOrder[i], out var winner))
            {
                var (n1, n2) = SeasonInfo.SplitName(Game.Clubs[winner].Name);
                Widgets.ShadowText(s, mini, x, 68, n1, TextColor, Shadow);
                Widgets.ShadowText(s, mini, x, 76, n2, TextColor, Shadow);
            }
            else
                Widgets.ShadowText(s, mini, x, 68, "Wer weiß...", TextColor, Shadow);
        }

        foreach (var league in Enum.GetValues<League>())
        {
            var i = (int)league;
            Widgets.ShadowText(s, flow, LeagueHeaderCenters[i], 136, SeasonInfo.LeagueName(league), HeadingColor, Shadow);
            var x = LeagueColumns[i];
            var top = new List<(string Mark, byte Color, int Club)>();
            if (league == League.Bundesliga && Record.Titles.TryGetValue(Title.GermanChampion, out var champion))
                top.Add((Circle, ArrowBlue, champion));
            // [ORIGINAL] Doppelwechsel: Platz statt Verein (Spur saisonwechsel Teil 6, D5, `file16_8`).
            top.AddRange(GameState.ShownRelegatedInto(Record, league).Select(c => (ArrowDown, ArrowRed, c)));
            DrawRows(s, x, TopRowY, top);
            DrawRows(s, x, BottomRowY, Record.PromotedInto[league].Select(c => (ArrowUp, ArrowGreen, c)).ToList());
        }
    }

    private void DrawRows(Surface s, int x, int y, List<(string Mark, byte Color, int Club)> rows)
    {
        foreach (var (mark, color, club) in rows.Take(2))
        {
            s.Text(Assets.Mini, x + 1, y + 1, mark, Shadow);
            s.Text(Assets.Mini, x, y, mark, color);
            s.Text(Assets.Mini, x + NameOffset + 1, y + 1, Game.Clubs[club].Name, Shadow);
            s.Text(Assets.Mini, x + NameOffset, y, Game.Clubs[club].Name, TextColor);
            y += RowPitch;
        }
    }

    private static void Line(Surface s, int y) => s.HLine(12, y, 296, LineColor);
}
