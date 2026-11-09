using EishockeyManager.Core;
using EishockeyManager.UI.Screens.Competitions;

namespace EishockeyManager.UI.Screens.Statistics;

/// <summary>
/// „Früher“ (Statistiken → Früher): Verlauf des Managers am Zug über alle Saisons – Diagramme für Europacup,
/// DEB-Pokal und Liga, darunter die Saison im Klartext. Zeigt nur vorhandenen Zustand; die Pfeile blättern.
/// </summary>
/// <remarks>
/// Aufbau aus em.exe (Datei 0x2C1EE, Text 0x2C099), vermessen am Video „Die Oberfläche von EM“ (16:52):
/// <list type="bullet">
/// <item>[ORIGINAL] Fenster (63 | 4) 10 × 2, (4 | 51) 10 × 2, (42 | 92) 10 × 5, (21 | 198) 10 × 2 Teile.</item>
/// <item>[ORIGINAL] Beschriftung Flow, Farbe 10, Schatten 28, zweizeilig (Zeilenabstand 9): „Europa“/„cup“ (75 | 23),
///   „DEB-“/„Pokal“ (17 | 69), „Liga“ (54 | 177).</item>
/// <item>[ORIGINAL] Achsen Farbe 6: senkrecht bei x = 135 / 75 / 115 von y = 12 / 58 / 101 bis 30 / 76 / 173, Punkte
///   links daneben alle 3 / 3 / 4 Pixel, waagerecht 152 Pixel lang auf der unteren Kante.</item>
/// <item>[ORIGINAL] Textfeld: Füllfläche (32 | 205) … (217 | 229) Farbe 4; Jahr (Mini, Farbe 30) bei x = 40, Grundlinie
///   211; ab x = 66 drei Zeilen (211, 219, 227): „n. Platz der Liga“ / „Play-Off VIERTELFINALE …“ / „Deutscher Meister“,
///   „DEB-Pokal: Runde“ / „SIEGER“, „Europacup: Gruppe / Endrunde / SIEGER / Nicht im Wettbewerb“; Farbe 11,
///   Titel 7. Ohne Saison: „Im ersten Jahr gibt's hier noch keine Einträge...“.</item>
/// <item>[APPROX] Balken je Saison (3 Pixel Abstand, 2 Pixel breit ab Achse + 2): Höhe aus dem Abschneiden; das
///   Original stapelt 12er-Blöcke nach einer Kodierung, die hier nicht vollständig gelesen ist. Video: 8. der
///   Oberliga ≈ 10 Pixel, DEB-Pokal „Erste Runde“ ≈ 4 Pixel. Unter jeder Achse eine rote Markierung der
///   angezeigten Saison.</item>
/// </list>
/// </remarks>
public sealed class HistoryScreen : Screen
{
    public static readonly Rect EuropeWindow = new(63, 4, 240, 38);
    public static readonly Rect CupWindow = new(4, 51, 240, 38);
    public static readonly Rect LeagueWindow = new(42, 92, 240, 95);
    public static readonly Rect TextWindow = new(21, 198, 240, 38);
    public static readonly Rect ArrowUp = new(242, 208, 9, 8);
    public static readonly Rect ArrowDown = new(242, 217, 9, 8);
    public const string NoEntries = "Im ersten Jahr gibt's hier noch keine Einträge...";

    private static readonly int[] AxisX = [135, 75, 115];
    private static readonly int[] AxisTop = [12, 58, 101];
    private static readonly int[] AxisBottom = [30, 76, 173];
    private static readonly int[] TickStep = [3, 3, 4];

    private readonly Screen _back;
    private int _index = -1;

    public HistoryScreen(ScreenHost host, Screen back) : base(host) => _back = back;

    private IReadOnlyList<ManagerSeasonHistory> Seasons =>
        Host.Game is { } game ? game.ManagerHistory(game.CurrentManager) : [];

    /// <summary>Angezeigte Saison (Index in <see cref="GameState.ManagerHistory"/>), zu Beginn die letzte.</summary>
    public int Index => _index < 0 ? Seasons.Count - 1 : _index;

    public override void Update(InputState input)
    {
        if (input.Pressed(Key.Escape) || input.RightClick)
        {
            Host.Show(_back);
            return;
        }
        if (input.Clicked(ArrowUp) || input.Pressed(Key.Up))
            _index = Math.Max(0, Index - 1);
        else if (input.Clicked(ArrowDown) || input.Pressed(Key.Down))
            _index = Math.Min(Seasons.Count - 1, Index + 1);
        else if (input.LeftClick || input.Keys.Count > 0)
            Host.Show(_back);
    }

    /// <summary>Die drei Textzeilen einer Saison mit Farben (wie em.exe 0x2C099 und 0x2C9D6).</summary>
    public static IReadOnlyList<(string Text, byte Color)> Lines(ManagerSeasonHistory h)
    {
        var league = h.LeagueOutcome switch
        {
            LeagueOutcome.GermanChampion => ("Deutscher Meister", (byte)7),
            LeagueOutcome.PlayoffQuarterFinal => ("Play-Off VIERTELFINALE", (byte)11),
            LeagueOutcome.PlayoffSemiFinal => ("Play-Off HALBFINALE", (byte)11),
            LeagueOutcome.PlayoffFinal => ("Play-Off ENDSPIEL", (byte)11),
            _ => ($"{h.Rank}. Platz der {LeagueName(h.League)}", (byte)11),
        };
        var deb = h.Deb switch
        {
            CupOutcome.Winner => ("DEB-Pokal: SIEGER", (byte)7),
            CupOutcome.Eliminated => ("DEB-Pokal: " + DebCup.RoundNames[h.DebRound] + " ", (byte)11),
            _ => ("DEB-Pokal: ", (byte)11),
        };
        var europe = h.Europe switch
        {
            CupOutcome.Winner => ("Europacup: SIEGER ", (byte)7),
            CupOutcome.Eliminated => ("Europacup: " + (h.EuropeFinalRound ? "Endrunde" : "Gruppe"), (byte)11),
            _ => ("Europacup: Nicht im Wettbewerb", (byte)11),
        };
        return [league, deb, europe];
    }

    private static string LeagueName(League league) => league switch
    {
        League.Bundesliga => "Bundesliga",
        League.ZweiteLiga => "Zweite Liga",
        _ => "Oberliga",
    };

    public override void Draw(Surface s)
    {
        s.Palette = Assets.Palette;
        s.Blit(Assets.Image(MainMenuScreen.Background), 0, 0);
        foreach (var w in new[] { EuropeWindow, CupWindow, LeagueWindow, TextWindow })
            Widgets.Frame(s, Assets, w, FrameStyle.Orange);

        var flow = Assets.Flow;
        (string, string, int, int)[] labels = [("Europa", "cup", 75, 23), ("DEB-", "Pokal", 17, 69), ("", "Liga", 54, 168)];
        foreach (var (first, second, x, y) in labels)
        {
            if (first.Length > 0)
                CompetitionWidgets.Centered(s, flow, x, x, y, first, 10, 28, left: true);
            CompetitionWidgets.Centered(s, flow, x, x, y + 9, second, 10, 28, left: true);
        }

        var seasons = Seasons;
        for (var g = 0; g < 3; g++)
        {
            for (var y = AxisTop[g]; y <= AxisBottom[g]; y += TickStep[g])
                s.Set(AxisX[g] - 1, y, 6);
            s.VLine(AxisX[g], AxisTop[g], AxisBottom[g] - AxisTop[g] + 1, 6);
            s.HLine(AxisX[g], AxisBottom[g], 0x98 + 1, 6);
            for (var i = 0; i < seasons.Count && i < 50; i++)
            {
                var height = BarHeight(g, seasons[i], AxisBottom[g] - AxisTop[g]);
                if (height > 0)
                    s.Fill(new Rect(AxisX[g] + 2 + 3 * i, AxisBottom[g] - height, 2, height), BarColor(g, seasons[i]));
            }
            if (seasons.Count > 0)
                Marker(s, AxisX[g] + 2 + 3 * Math.Min(Index, 49), AxisBottom[g] + 1);
        }

        s.Fill(new Rect(32, 205, 217 - 32 + 1, 229 - 205 + 1), 4);
        var mini = Assets.Mini;
        if (seasons.Count == 0)
        {
            CompetitionWidgets.Centered(s, mini, 22, 260, 219, NoEntries, Colors.TileBackground, null);
            return;
        }
        s.Blit(Assets.Image(4), new Rect(20, 0, 9, 17), 242, 208);
        var season = seasons[Index];
        CompetitionWidgets.Left(s, mini, 40, 211, CompetitionWidgets.Number(season.Season, 5), Colors.TileBackground);
        var lines = Lines(season);
        for (var k = 0; k < 3; k++)
            CompetitionWidgets.Left(s, mini, 66, 211 + 8 * k, lines[k].Text, lines[k].Color);
    }

    /// <summary>
    /// Markierung der angezeigten Saison unter der Achse (Farbe 16): drei Zeilen 2 / 4 / 6 Pixel breit, darunter ein Stiel
    /// 2 Pixel × 3 Zeilen, ausgerichtet am Balken (x = linke Balkenkante). [ORIGINAL] DOSBox-Sitzung 2, `file14_5`: Liga y 174–179
    /// bei Achse y 173, DEB y 77–82 bei Achse y 76 (A357); vorher 1 / 3 / 5 Pixel mit 1 Pixel Stiel.
    /// </summary>
    private static void Marker(Surface s, int x, int y)
    {
        s.HLine(x, y, 2, MarkerColor);
        s.HLine(x - 1, y + 1, 4, MarkerColor);
        s.HLine(x - 2, y + 2, 6, MarkerColor);
        s.Fill(new Rect(x, y + 3, 2, 3), MarkerColor);
    }

    public const byte MarkerColor = 16;

    /// <summary>
    /// Balkenfarbe je Grafik: Liga Index 29 (32 | 48 | 81), DEB-Pokal Index 27 (97 | 65 | 48). [ORIGINAL] DOSBox-Sitzung 2,
    /// `file14_5` (A357). Europapokal nicht belegt (nicht im Wettbewerb aufgenommen): [APPROX] wie der DEB-Pokal.
    /// </summary>
    public static byte BarColorNormal(int graph) => graph == 2 ? (byte)29 : (byte)27;

    /// <summary>
    /// Balkenhöhe: Liga nach Gesamtplatz (2 · (37 − …) Pixel mit 1-basiertem Rang, wie ihn <c>StatisticsHistory</c> speichert; „12. Platz der Oberliga“ = 2 Pixel, [ORIGINAL] `file14_5`; R14UR-02),
    /// Pokale nach erreichter Runde. [APPROX] DEB-Pokal 3 Pixel je Runde („Erste Runde“ = 3 Pixel, `file14_5`; Formel in em.exe
    /// nicht gelesen, zweiter Messpunkt fehlt), Europapokal wie bisher geschätzt.
    /// </summary>
    private static int BarHeight(int graph, ManagerSeasonHistory h, int range) => graph switch
    {
        0 => h.Europe switch { CupOutcome.Winner => range, CupOutcome.Eliminated => h.EuropeFinalRound ? range * 2 / 3 : range / 3, _ => 0 },
        1 => h.Deb switch { CupOutcome.Winner => range, CupOutcome.Eliminated => Math.Min(range, 3 * (h.DebRound + 1)), _ => 0 },
        _ => Math.Min(range, 2 * (37 - (h.Rank + 12 * (int)h.League))),
    };

    private static byte BarColor(int graph, ManagerSeasonHistory h) =>
        graph switch
        {
            0 when h.Europe == CupOutcome.Winner => 7,
            1 when h.Deb == CupOutcome.Winner => 7,
            2 when h.LeagueOutcome == LeagueOutcome.GermanChampion => 7,
            _ => BarColorNormal(graph),
        };
}
