using System.Text;
using EishockeyManager.Core;
using EishockeyManager.UI.Screens.Competitions;

namespace EishockeyManager.UI.Screens.Statistics;

/// <summary>
/// Statistiken → Verein: Serien, Über-/Unterzahl, Zuschauer, Finanzen und Einnahmen/Ausgaben je Monat des Managers am
/// Zug. Zeigt nur vorhandenen Zustand; jeder Klick oder jede Taste schließt.
/// </summary>
/// <remarks>
/// [ORIGINAL] em.exe Ov 0xE0, Funktion Datei 0x2D5AE (3356 Byte, Menüsprung 0x457E1 Index 0), Texte DGROUP 0xADC
/// (30 Zeiger). Vorlage am Video: <c>folge01_00522</c> (Spielbeginn, Farbverlauf sichtbar) und <c>folge01_01022</c>
/// (nach drei Spieltagen). Alle Lagen aus dem Code, im Pixelvergleich bestätigt (<c>docs/wettbewerbe.md</c>).
/// <list type="bullet">
/// <item>[ORIGINAL] Drei Fenster (0x30:0x335, Stil 0, Merker 2): 8 × 10 Teile ab (12 | 0), 8 × 2 ab (12 | 201),
///   4 × 12 ab (210 | 0) (Datei 0x2D5C9–0x2D636).</item>
/// <item>[ORIGINAL] Farbverlauf: die 13 Paletteneinträge DGROUP 0x4C3E[12 − i] (16, 17, 18, 10, 11, 8, 22–28) bekommen
///   die Farben 0x4C24[i] (RGB je 4 Bit · 4: Grün 0x0C0 … Rot 0xD00), Datei 0x2D666–0x2D6C0.</item>
/// <item>[ORIGINAL] Zeilenlauf 0x2D6CC–0x2DE7C über die 30 Texte: Überschriften (erstes Zeichen „!“) in Schrift 2 (Flow)
///   Farbe 7, Schatten 0, Grundlinie y + 6, darunter Linie Farbe 7 (x … x + w + 1 | y + 8) und Farbe 1
///   (x + 1 … x + w − 1 | y + 9), w = Breite des Textes ohne das letzte Zeichen (0x13208 mit strlen − 1, gemessen ab
///   „!“); danach y + 11. Sonst Text in Schrift 3 (Mini) Farbe 1 auf y, Werte Farbe 30; danach y + 8 (links) bzw.
///   y + 15 (rechts) bzw. y + 7 (unten). Sprünge: Text 19 → (217 | 13), 23 → y + 7, 24 → y + 56, 26 → y + 12,
///   27 → (19 | 207).</item>
/// <item>[ORIGINAL] Zahlen 0x0B954: Mindestbreite DGROUP 0x374 (fehlende Stellen als Ziffernabstand), Tausenderpunkt
///   nur bei DGROUP 0x38D = 0 (Zuschauer, Finanzen, Monat). Serien Breite 2, Null als „%-“ (0x2D57A); Über-/Unterzahl
///   und Zuschauer/Finanzen Breite 3; Monat Breite 8.</item>
/// <item>[ORIGINAL] Danach (0x2DE7F–0x2E2B5): „G       H       A“ in Schrift 2 bei (105 | 16); Rahmen der Grafik
///   (217 | 105)–(299 | 145) Farbe 6 mit 13 Streifen (218 … 298, je 3 Zeilen ab 106, Farben 28 … 16); Balken der
///   Heimspiele (<see cref="AudienceBars"/>); Konto- und Schuldenbalken (<see cref="AccountBar"/>, <see cref="DebtBar"/>)
///   in (223 … 293 | 179 … 188) bzw. (… | 206 … 215), Grund 0, Rahmen Farbe 6.</item>
/// <item>[APPROX] Das Original kehrt nach dem Zeichnen ins Menü zurück; wie bei „Fairplay“ schließt hier ein Klick oder
///   eine Taste.</item>
/// <item>[APPROX] Über-/Unterzahl zählt die Minutenrechnung (Spur <c>spielstaerke-manager</c>, Anschluss offen); bis dahin
///   stehen dort die gespeicherten Zähler (<see cref="Manager.PowerPlayCounts"/>, sonst 0).</item>
/// </list>
/// </remarks>
public sealed class ClubStatisticsScreen : Screen
{
    public static readonly Rect MainWindow = new(12, 0, 8 * 24, 10 * 19);
    public static readonly Rect MonthWindow = new(12, 201, 8 * 24, 2 * 19);
    public static readonly Rect SideWindow = new(210, 0, 4 * 24, 12 * 19);

    /// <summary>Texte DGROUP 0xADC (Einträge 0–29; „!“ = Überschrift).</summary>
    public static readonly string[] Texts =
    [
        "!Serien", "GEWONNEN", "VERLOREN", "UNENTSCHIEDEN", "NICHT GEWONNEN", "NICHT VERLOREN", "OHNE GEGENTOR",
        "OHNE TORERFOLG", "TORE/SPIEL", "GEGENTORE/SPIEL", "!überzahl/Unterzahl", "", "TORE IN ÜBERZAHL",
        "GEGENTORE IN ÜBERZAHL", "TORE IN UNTERZAHL", "GEGENTORE IN UNTERZAHL", "ÜBERZAHL ERFOLG", "UNTERZAHL ERFOLG", "",
        "!Zuschauer", "SCHNITT/SPIEL", "BENÖTIGTER SCHNITT", "ZUSCHAUERREKORD: ", "MINUSKULISSE: ", "!Finanzen",
        "KONTOSTAND", "SCHULDEN GESAMT", "!Einnahmen,Ausgaben / Monat", "EINNAHMEN(SPONSOREN...)", "AUSGABEN(GEHÄLTER...)",
    ];

    /// <summary>[ORIGINAL] DGROUP 0x31AC (Zeiger 0xEE1).</summary>
    public const string ColumnHeads = "G       H       A";

    /// <summary>[ORIGINAL] DGROUP 0x319B (Zeiger 0xEDD) bzw. 0x318B (Zeiger 0xED9).</summary>
    public const string NoneMasculine = "(noch keiner...)";
    public const string NoneFeminine = "(noch keine...)";

    /// <summary>[ORIGINAL] Spalten G/H/A der Serien (Datei 0x2D990: 0x63 + 0x22 · Spalte).</summary>
    public static readonly int[] SeriesX = [99, 133, 167];

    /// <summary>[ORIGINAL] Spalte der Über-/Unterzahl (Datei 0x2DB0C: 0x87).</summary>
    public const int PowerPlayX = 135;

    /// <summary>[ORIGINAL] Farbverlauf: Paletteneinträge DGROUP 0x4C3E und Farben 0x4C24 (RGB je 4 Bit).</summary>
    public static readonly byte[] GradientIndex = [16, 17, 18, 10, 11, 8, 22, 23, 24, 25, 26, 27, 28];
    private static readonly int[] GradientRgb = [0x0C0, 0x2C0, 0x4C0, 0x6C0, 0x8C0, 0xAC0, 0xCC0, 0xCA0, 0xD80, 0xD60, 0xD40, 0xD20, 0xD00];

    public static readonly Rect AudienceFrame = new(217, 105, 83, 41);
    public static readonly Rect AccountFrame = new(222, 178, 73, 12);
    public static readonly Rect DebtFrame = new(222, 205, 73, 12);
    public const int BarLeft = 223;
    public const int BarEnd = 294;

    private readonly Screen _back;

    public ClubStatisticsScreen(ScreenHost host, Screen back) : base(host) => _back = back;

    /// <summary>Palette mit dem Farbverlauf (Datei 0x2D666–0x2D6C0).</summary>
    public static Formats.Palette GradientPalette(Formats.Palette basis)
    {
        var palette = basis;
        for (var i = 0; i < GradientIndex.Length; i++)
        {
            var rgb = GradientRgb[i];
            palette = palette.WithColor(GradientIndex[12 - i], Dac((rgb >> 8) & 0xF), Dac((rgb >> 4) & 0xF), Dac(rgb & 0xF));
        }
        return palette;

        static byte Dac(int nibble) => Formats.Palette.DacToRgb((byte)(nibble << 2));
    }

    /// <summary>
    /// [ORIGINAL] Zahlenausgabe 0x0B954: Mindestbreite <paramref name="width"/> (fehlende Stellen als
    /// <see cref="CompetitionWidgets.FigureSpace"/>, bei 0 die Ziffer 0), Tausenderpunkt je drei Stellen bei
    /// <paramref name="separators"/>; negative Zahlen mit „-“ ohne Auffüllen.
    /// </summary>
    public static string Number(long value, int width, bool separators)
    {
        var digits = new StringBuilder();
        var v = Math.Abs(value);
        while (v != 0)
        {
            if (separators && digits.Length is 3 or 7 or 11)
                digits.Append('.');
            digits.Append((char)('0' + v % 10));
            v /= 10;
        }
        if (value < 0)
            return "-" + Reverse(digits);
        if (digits.Length < width)
        {
            digits.Append(digits.Length == 0 ? '0' : CompetitionWidgets.FigureSpace);
            while (digits.Length < width)
                digits.Append(CompetitionWidgets.FigureSpace);
        }
        else if (digits.Length == 0)
            digits.Append('0');
        return Reverse(digits);

        static string Reverse(StringBuilder sb)
        {
            var chars = sb.ToString().ToCharArray();
            Array.Reverse(chars);
            return new string(chars);
        }
    }

    /// <summary>[ORIGINAL] Serienwert 0x2D57A: 0 → „%-“ (Ziffernabstand und Strich), sonst Breite 2.</summary>
    public static string SeriesValue(int value) => value == 0 ? CompetitionWidgets.FigureSpace + "-" : Number(value, 2, false);

    /// <summary>[ORIGINAL] Zelle der Serien: laufend „(“ Rekord „)“ (Datei 0x2D8A3–0x2D98D).</summary>
    public static string SeriesCell(int current, int record) => SeriesValue(current) + "(" + SeriesValue(record) + ")";

    /// <summary>[ORIGINAL] „TORE/SPIEL“: zwei Leerzeichen (DGROUP 0x4DA2), Zehntel als „E.Z“ (0x2EE6A–0x2EEE0).</summary>
    public static string GoalsCell(int tenths) => "  " + Number(tenths / 10, 1, false) + "." + Number(tenths % 10, 1, false);

    /// <summary>Einträge der Spalten G/H/A in der Reihenfolge des Bildschirms (DGROUP 0x4C4B: 2, 0, 1 = Gesamt, Heim, Auswärts).</summary>
    public static readonly SeriesColumn[] Columns = [SeriesColumn.Total, SeriesColumn.Home, SeriesColumn.Away];

    /// <summary>Ein gezeichneter Text des Zeilenlaufs.</summary>
    public sealed record Item(string Text, int X, int Baseline, byte Color, bool Heading);

    /// <summary>Texte und Werte des Zeilenlaufs 0x2D6CC–0x2DE7C für den Manager am Zug.</summary>
    public static IReadOnlyList<Item> Layout(GameState game, Manager manager)
    {
        var items = new List<Item>();
        var club = game.Clubs[manager.ClubIndex];
        var stats = game.Finance(manager).HomeStatistics;
        int x = 19, y = 16;
        const byte label = 1, value = 30;
        for (var si = 0; si < Texts.Length; si++)
        {
            if (si == 19) { x = 217; y = 13; }
            if (si == 23) y += 7;
            if (si == 24) y += 56;
            if (si == 26) y += 12;
            if (si == 27) { x = 19; y = 207; }
            var text = Texts[si];
            if (text.StartsWith('!'))
            {
                y += 6;
                items.Add(new Item(text, x, y, Colors.White, true));
                y += 11;
                continue;
            }
            if (text.Length > 0)
                items.Add(new Item(text, x, y, label, false));
            if (si < 8)
            {
                var kind = (SeriesKind)(si - 1);
                for (var c = 0; c < 3; c++)
                {
                    var i = Series.Index(kind, Columns[c]);
                    items.Add(new Item(SeriesCell(club.SeriesCurrent[i], manager.SeriesRecords[i]), SeriesX[c], y, value, false));
                }
            }
            else if (si < 10)
            {
                for (var c = 0; c < 3; c++)
                    items.Add(new Item(GoalsCell(game.GoalsPerGameTenths(club, Columns[c], against: si == 9)), SeriesX[c], y, value, false));
            }
            else if (si is >= 12 and <= 17)
            {
                var counts = manager.PowerPlayCounts;
                var cell = si switch
                {
                    16 => Number(PowerPlay.PowerPlaySuccess(counts), 3, false) + "#",
                    17 => Number(PowerPlay.ShortHandedSuccess(counts), 3, false) + "#",
                    _ => Number(Math.Clamp(counts[si - 12], 0, 100), 3, false),
                };
                items.Add(new Item(cell, PowerPlayX, y, value, false));
            }
            else if (si is >= 20 and <= 26)
            {
                var (first, second) = SideValues(game, manager, stats, si);
                items.Add(new Item(first, x, y + 6, value, false));
                if (second is not null)
                    items.Add(new Item(second, x, y + 12, value, false));
            }
            else if (si >= 28)
            {
                var amount = si == 28 ? game.MonthlyIncome(manager) : game.MonthlyExpenses(manager);
                items.Add(new Item(Number(amount, 8, true) + " DM", x + 135 - 5, y, value, false));
            }
            y += si > 26 ? 7 : si > 18 ? 15 : 8;
        }
        return items;
    }

    /// <summary>Werte der rechten Spalte (Datei 0x2DB30–0x2DDA1).</summary>
    private static (string First, string? Second) SideValues(GameState game, Manager manager, StadiumStatistics stats, int si)
    {
        switch (si)
        {
            case 20:
                return (stats.Average is { } average ? Number(average, 3, true) : NoneMasculine, null);
            case 21:
                return (Number(game.RequiredAverage(manager), 3, true), null);
            case 22:
                return stats.Record == 0
                    ? (NoneMasculine, null)
                    : (Number(stats.Record, 3, true), "(" + ClubName(game, stats.RecordOpponent) + ")");
            case 23:
                return stats.Minimum is StadiumStatistics.InitialMinimum or 0
                    ? (NoneFeminine, null)
                    : (Number(stats.Minimum, 3, true), "(" + ClubName(game, stats.MinimumOpponent) + ")");
            case 25:
                return (Number(manager.Money, 3, true) + " DM", null);
            case 26:
                return (Number(game.TotalDebts(manager), 3, true) + " DM", null);
            default:
                return ("", null);
        }
    }

    private static string ClubName(GameState game, int index) =>
        index >= 0 && index < game.Clubs.Count ? game.Clubs[index].Name : "";

    /// <summary>[ORIGINAL] Ganzzahlige Wurzel 0x2E2CA (größtes n mit n² ≤ x).</summary>
    public static int IntegerSqrt(int x)
    {
        int n = 0, step = 100;
        while (true)
        {
            n += step;
            if (n * n <= x)
                continue;
            if (step <= 1)
                return n - 1;
            n -= step;
            step /= 10;
        }
    }

    /// <summary>
    /// Balken der Heimspiele (Datei 0x2DFD2–0x2E162): k = Benötigter Schnitt / 1000; Streuung s = √(Σ |v − k|² / Spiele),
    /// mindestens 10; Obergrenze T = 2k − Min, wenn k − s &gt; Min, sonst k + s, mindestens Max; Halbbreite r = |T − k|
    /// (0 → 1). Höhe je Spiel h = (v − k) · 19 / r + 19 auf 0 … 37 begrenzt, Linie (227 + 3 i | 143 − h … 144) Farbe 4.
    /// Liefert (x, Oberkante) je Heimspiel.
    /// </summary>
    public static IReadOnlyList<(int X, int Top)> AudienceBars(IReadOnlyList<int> curve, int games, long requiredAverage)
    {
        var k = (int)(requiredAverage / 1000);
        int min = 0x80, max = -127, sum = 0;
        var n = Math.Min(games, curve.Count);
        for (var i = 0; i < n; i++)
        {
            var v = curve[i];
            if (v < min) min = v;
            if (v > max) max = v;
            var d = Math.Abs(v - k);
            sum += d * d;
        }
        var spread = n > 0 ? sum / n : 0;
        spread = Math.Max(IntegerSqrt(spread), 10);
        var top = k - spread > min ? 2 * k - min : spread + k;
        if (top < max)
            top = max;
        var r = Math.Abs(top - k);
        if (r == 0)
            r = 1;
        var bars = new List<(int, int)>();
        for (var i = 0; i < n; i++)
        {
            var h = Math.Clamp((curve[i] - k) * 19 / r + 19, 0, 37);
            bars.Add((227 + 3 * i, 143 - h));
        }
        return bars;
    }

    /// <summary>
    /// Kontobalken (Datei 0x2E162–0x2E1DC): je Pixel ab x = 223 bis 293 gezeichnet, solange Konto (Drittel-DM) &gt; Schwelle;
    /// Schwelle ab −2.449.488 je Pixel + 70.000, Farbe 0x4C3E[s] mit s + 1 nach je 6 Pixeln (rot → grün).
    /// Liefert (x, Farbe) je gezeichnetem Pixel.
    /// </summary>
    public static IReadOnlyList<(int X, byte Color)> AccountBar(long moneyThirds)
    {
        var bar = new List<(int, byte)>();
        long threshold = -2_449_488;
        var s = 0;
        for (var x = BarLeft; x < BarEnd;)
        {
            if (moneyThirds > threshold)
                bar.Add((x, ColorAt(s)));
            x++;
            if ((x - BarLeft) % 6 == 0 && s < 13)
                s++;
            threshold += 70_000;
        }
        return bar;
    }

    /// <summary>
    /// Schuldenbalken (Datei 0x2E1DE–0x2E286): Schulden (0x57768) plus negatives Konto, je Pixel gezeichnet solange &gt;
    /// Schwelle; Schwelle ab 0 je Pixel + 25.000 Drittel-DM, Farbe 0x4C3E[s] ab s = 12 mit s − 1 nach je 6 Pixeln.
    /// </summary>
    public static IReadOnlyList<(int X, byte Color)> DebtBar(long debtThirds, long moneyThirds)
    {
        var debts = debtThirds + (moneyThirds < 0 ? -moneyThirds : 0);
        var bar = new List<(int, byte)>();
        long threshold = 0;
        var s = 12;
        for (var x = BarLeft; x < BarEnd;)
        {
            if (debts > threshold)
                bar.Add((x, ColorAt(s)));
            x++;
            if ((x - BarLeft) % 6 == 0 && s != 0)
                s--;
            threshold += 25_000;
        }
        return bar;
    }

    /// <summary>DGROUP 0x4C3E ff. (Eintrag 13 ist das erste Byte von 0x4C4B = 2).</summary>
    private static byte ColorAt(int s) => s < GradientIndex.Length ? GradientIndex[s] : (byte)2;

    public override void Update(InputState input)
    {
        if (input.LeftClick || input.RightClick || input.Keys.Count > 0)
            Host.Show(_back);
    }

    public override void Draw(Surface s)
    {
        s.Palette = GradientPalette(Assets.Palette);
        s.Blit(Assets.Image(MainMenuScreen.Background), 0, 0);
        Widgets.Frame(s, Assets, MainWindow, FrameStyle.Orange);
        Widgets.Frame(s, Assets, MonthWindow, FrameStyle.Orange);
        Widgets.Frame(s, Assets, SideWindow, FrameStyle.Orange);
        if (Host.Game is not { } game)
            return;
        var manager = game.ActiveManager;
        foreach (var item in Layout(game, manager))
        {
            if (item.Heading)
                DrawHeading(s, item);
            else
                CompetitionWidgets.Left(s, Assets.Mini, item.X, item.Baseline, item.Text, item.Color);
        }
        CompetitionWidgets.Centered(s, Assets.Flow, 105, -1, 16, ColumnHeads, Colors.White, Colors.Black, left: true);

        s.Fill(new Rect(223, 179, 71, 10), 0);
        s.Outline(AccountFrame, 6);
        s.Fill(new Rect(223, 206, 71, 10), 0);
        s.Outline(DebtFrame, 6);
        s.Outline(AudienceFrame, 6);
        for (var i = 0; i < 13; i++)
            s.Fill(new Rect(218, 106 + 3 * i, 81, 3), GradientIndex[12 - i]);

        var stats = game.Finance(manager).HomeStatistics;
        foreach (var (x, top) in AudienceBars(stats.Curve, stats.Games, game.RequiredAverage(manager)))
            s.VLine(x, top, 144 - top + 1, 4);
        foreach (var (x, color) in AccountBar(manager.Money * 3))
            s.VLine(x, 179, 10, color);
        foreach (var (x, color) in DebtBar(game.TotalDebts(manager) * 3, manager.Money * 3))
            s.VLine(x, 206, 10, color);
    }

    private void DrawHeading(Surface s, Item item)
    {
        var flow = Assets.Flow;
        var shown = item.Text[1..];
        CompetitionWidgets.Centered(s, flow, item.X, -1, item.Baseline, shown, Colors.White, Colors.Black, left: true);
        var w = flow.Measure(item.Text[..^1]);
        s.HLine(item.X, item.Baseline + 2, w + 2, Colors.White);
        s.HLine(item.X + 1, item.Baseline + 3, w - 1, 1);
    }
}

internal static class ClubStatisticsMenuEntries
{
    [MenuEntry("Verein")]
    private static Screen Open(ScreenHost host, Screen back) => new ClubStatisticsScreen(host, back);
}
