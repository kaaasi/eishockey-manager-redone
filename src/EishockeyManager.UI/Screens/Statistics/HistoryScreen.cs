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
/// <item>[ORIGINAL] Balken je Saison (3 Pixel Abstand, 2 Pixel breit ab Achse + 2) aus den Saisonbytes des Managers (Schreiber
///   Datei 0x55B36–0x55DBD, Bildschirm 0x2C1EE, Füllungen 0x0B620; Abweichung B28, Beleg
///   <c>tools/reverse-engineering/abweichungen/b28_historie_balken.py</c>): siehe <see cref="Bars"/>. Unter jeder Achse eine rote
///   Markierung der angezeigten Saison.</item>
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
            _ => ($"{h.Rank}. Platz der {SeasonInfo.LeagueName(h.League)}", (byte)11),
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
                foreach (var (y1, y2, color) in Bars(g, seasons[i], AxisBottom[g]))
                    if (y1 <= y2)
                        s.Fill(new Rect(AxisX[g] + 2 + 3 * i, y1, 2, y2 - y1 + 1), color);
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
    /// Balkenfarbe je Grafik: Liga Index 29 (32 | 48 | 81, unterster Block), DEB-Pokal und Europacup Index 27 (97 | 65 | 48).
    /// [ORIGINAL] DOSBox-Sitzung 2, `file14_5` (A357); Europacup Farbe 27 (Datei 0x2C1EE, A805: keine Abweichung).
    /// </summary>
    public static byte BarColorNormal(int graph) => graph == 2 ? (byte)29 : (byte)27;

    /// <summary>[ORIGINAL] Farben der drei Liga-Blöcke von unten nach oben und Farbe der Titel (Sieger, Meister).</summary>
    public const byte LeagueMiddleColor = 22, LeagueTopColor = 23, TitleColor = 30;

    /// <summary>
    /// Saisonbytes des Originals (Manager + 0x18 + 4 · Saison, Schreiber Datei 0x55B36–0x55DBD).
    /// <list type="bullet">
    /// <item>[ORIGINAL] Liga: Platz in der Liga + DGROUP 0x7C0[Liga] (0, 12, 24; Datei 0x55C24–0x55C52); in der Bundesliga ersetzen
    ///   Plätze 1–8 die Play-off-Stufe (0x35 Endspiel, 0x34 Halbfinale, 0x33 Viertelfinale; Suche in DGROUP 0x5D3B + 8 … 13, Datei
    ///   0x55B76–0x55BD2), Meister setzt Bit 7 (Datei 0x55BEA).</item>
    /// <item>[ORIGINAL] DEB-Pokal: erreichte Runde, Sieger Bit 7 (Datei 0x55C87–0x55CC4).</item>
    /// <item>Europacup: 8 + Stufe, Sieger 0x8D (Datei 0x55D58–0x55D9D); [APPROX] Stufe Gruppe 1, Endrunde 2 wie
    ///   <see cref="SeasonCodes.Of"/>.</item>
    /// </list>
    /// </summary>
    public static (int League, int Deb, int Europe) Codes(ManagerSeasonHistory h)
    {
        var codes = SeasonCodes.Of(h);
        return (codes.League, codes.Deb, codes.Europe); // Ligaversatz seit #1007 in SeasonCodes.Of
    }

    /// <summary>
    /// Füllungen (y1, y2 einschließlich, Farbe) eines Balkens in der Zeichenfolge des Originals; <paramref name="bottom"/> = Achse.
    /// <list type="bullet">
    /// <item>[ORIGINAL] Liga, Byte v (Play-off 0x33/0x34/0x35 zählt als 6/4/2): drei gestapelte Blöcke zu je 12 Plätzen à 2 Pixel,
    ///   unten Farbe 29 (Achse − 25 … Achse − 1, ab v = 25 nur 2 · (37 − v) Pixel), Mitte Farbe 22 (bis v = 24, ab v = 13 gekürzt),
    ///   oben Farbe 23 (bis v = 12); die Blockgrenzen werden doppelt gezeichnet, der obere Block gewinnt. Bit 7 (Meister) oder
    ///   v = 1: alle drei Blöcke voll in Farbe 30. Beispiele (Achse 173): v = 6 → 148–172/29, 124–148/22, 111–124/23.</item>
    /// <item>[ORIGINAL] DEB-Pokal: 3 · Byte Pixel Farbe 27, Sieger 18 Pixel Farbe 30.</item>
    /// <item>[ORIGINAL] Europacup: nur bei Bit 3–6 ≠ 0, 3 · (Byte &amp; 7) Pixel Farbe 27, Sieger 18 Pixel Farbe 30.</item>
    /// <item>Byte 0 bzw. Runde 0 ergibt im Original eine umgekehrte Füllung (y1 = y2 + 1), hier kein Balken.
    ///   [APPROX] DEB-Pokal „nicht im Wettbewerb“ (0x1E) zeichnet hier keinen Balken; das Original würde 90 Pixel über die
    ///   Achse hinaus füllen (Nachbildung y1 = −14), der Fall ist im Spiel nicht beobachtet.</item>
    /// </list>
    /// </summary>
    public static IReadOnlyList<(int Y1, int Y2, byte Color)> Bars(int graph, ManagerSeasonHistory h, int bottom)
    {
        var (league, deb, europe) = Codes(h);
        var bars = new List<(int, int, byte)>();
        switch (graph)
        {
            case 0:
                if ((europe & 0x78) == 0)
                    break;
                if ((europe & 0x80) != 0)
                    bars.Add((bottom - 18, bottom - 1, TitleColor));
                else if ((europe & 7) > 0)
                    bars.Add((bottom - 3 * (europe & 7), bottom - 1, BarColorNormal(0)));
                break;
            case 1:
                if (h.Deb == CupOutcome.NotParticipating)
                    break;
                if ((deb & 0x80) != 0)
                    bars.Add((bottom - 18, bottom - 1, TitleColor));
                else if (deb > 0)
                    bars.Add((bottom - 3 * deb, bottom - 1, BarColorNormal(1)));
                break;
            default:
                var full = (league & 0x80) != 0 || league == 1;
                var v = full ? 1 : league is >= 0x33 and <= 0x35 ? 2 * (0x36 - league) : league;
                byte Color(byte c) => full ? TitleColor : c;
                bars.Add((v >= 25 ? bottom - 2 * (37 - v) : bottom - 25, bottom - 1, Color(BarColorNormal(2))));
                if (v <= 24)
                    bars.Add((v >= 13 ? bottom - 24 - 2 * (25 - v) : bottom - 49, bottom - 25, Color(LeagueMiddleColor)));
                if (v <= 12)
                    bars.Add((bottom - 48 - 2 * (13 - v), bottom - 49, Color(LeagueTopColor)));
                break;
        }
        return bars;
    }
}
