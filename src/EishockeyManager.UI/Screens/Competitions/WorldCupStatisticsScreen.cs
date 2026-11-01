using EishockeyManager.Core;

namespace EishockeyManager.UI.Screens.Competitions;

/// <summary>
/// „WM-Statistik“ (Statistiken → WM-Statistik): oben die acht Länder mit den meisten WM-Punkten und je WM die Flagge des
/// Landes auf dem jeweiligen Platz, unten alle zwölf Länder in der Reihenfolge der laufenden WM. Zeigt nur vorhandenen Zustand.
/// </summary>
/// <remarks>
/// [ORIGINAL] em.exe Datei 0x3ADE1 (Overlay 0xF8, Aufruf aus der Hauptmenüschleife 0x44E22, Sprungtabelle cs:0x2927),
/// Bilder kb\12 (kleine Flaggen 9 × 6, Reihenfolge der Datenbank: Index = Land − erstes Land) und die Flaggenleiste 0x2F530.
/// Belegt an DOSBox `nur-weltkugel/17` (vor der ersten WM) und Video `eisbaeren_f07_03843` (nach drei WMs).
/// <list type="bullet">
/// <item>[ORIGINAL] Zwei Fenster 12 × 6 Rahmenteile bei (16 | 2) und (16 | 119). Titel „WM-Statistik“ (DGROUP 0x1061) Bold
///   zentriert 0 … 319, Grundlinie 21, Farbe 7, Schatten 1; Linie (27 … 292 | 23) Farbe 7.</item>
/// <item>[ORIGINAL] Kästchen 13 × 8 in Spalte c = 0 und 3 … 18 (x = 31 + 14 c), Zeile r = 0 … 7 (y = 36 + 9 r): Fläche
///   Farbe 1, Kante oben/links Farbe 23, rechts/unten Farbe 22 (Datei 0x3AEE7–0x3AFE6).</item>
/// <item>[ORIGINAL] Platz „n.“ (Mini, x = 23, Grundlinie 42 + 9 r) Farbe 17 für Platz 1, sonst 7; Linien (31 … 295) Farbe 8
///   unter Platz 1, 2 und 4 (y = 44, 53, 71; DGROUP 0x4ED8).</item>
/// <item>[ORIGINAL] Punkte je Land: Σ (9 − Platz) über alle WMs mit Platz 1 … 8; Reihenfolge Tauschsortierung der zwölf
///   Länder in WM-Reihenfolge nach Punkten (Datei 0x3B254–0x3B2F7, bei Gleichstand bleibt die WM-Reihenfolge). Zeile r:
///   kleine Flagge bei (33 | 37 + 9 r), „n P.“ (Mini, Mindestbreite 3, DGROUP 0x4EE5) linksbündig ab x = 45, Grundlinie
///   42 + 9 r, Farbe 30, Schatten 6.</item>
/// <item>[ORIGINAL] WM k (älteste zuerst): Flagge des Landes auf Platz p ≤ 8 bei (75 + 14 k | 28 + 9 p).</item>
/// <item>[ORIGINAL] Unten: Land i der WM-Reihenfolge (Gruppe 1 oben, Gruppe 2 unten) als Knopf bei x = 24 + 46 (i mod 6),
///   y = 131 bzw. 177: Fläche (x | y) … (x + 41 | y + 41) Farbe 1, Kante oben/links 23, rechts/unten 29, Flagge 38 × 38 bei
///   (x + 2 | y + 2); darüber (oben, y = 122) bzw. darunter (unten, y = 220) ein Kästchen bei x + 14 mit der kleinen Flagge
///   bei (x + 16 | + 1) (Datei 0x3ACE3, 0x3B0C2–0x3B19D).</item>
/// <item>[ORIGINAL] Höchstens 16 WMs: ab der 17. entfällt im Spielstand die älteste (Schreibstelle 0x311CF,
///   <see cref="GameState.MaxWorldCupRankings"/>), Spalte 0 ist also die älteste der letzten 16. Klick auf ein Land ruft im Original das Gegner-Fenster 0x128:0x2A (Datei 0x3B437–0x3B4AE) –
///   im Remake noch nicht vorhanden. Esc und Rechtsklick kehren zurück.</item>
/// </list>
/// </remarks>
public sealed class WorldCupStatisticsScreen : Screen
{
    public const string Title = "WM-Statistik";
    public static readonly Rect TopWindow = new(16, 2, 12 * 24, 6 * 19);
    public static readonly Rect BottomWindow = new(16, 119, 12 * 24, 6 * 19);
    public const int SmallFlagGraphic = 12;
    public const int SmallFlagWidth = 9;
    public const int SmallFlagHeight = 6;
    public const int Columns = 16;
    public const int Places = 8;
    private static readonly int[] SeparatorY = [44, 53, 71];

    private readonly Screen _back;
    private readonly IReadOnlyList<IReadOnlyList<int>>? _fixedRankings;
    private readonly IReadOnlyList<int>? _fixedOrder;

    public WorldCupStatisticsScreen(ScreenHost host, Screen back) : base(host) => _back = back;

    /// <summary>Feste WM-Reihenfolgen und Länderfolge statt des Spielstands (Bildschirmfotos wie im Video).</summary>
    public WorldCupStatisticsScreen(ScreenHost host, Screen back, IReadOnlyList<IReadOnlyList<int>> rankings, IReadOnlyList<int> order)
        : base(host) => (_back, _fixedRankings, _fixedOrder) = (back, rankings, order);

    /// <summary>Abschlussreihenfolgen der bisherigen WMs, älteste zuerst.</summary>
    public IReadOnlyList<IReadOnlyList<int>> Rankings => _fixedRankings ?? Host.Game?.WorldCupRankings ?? [];

    /// <summary>Länder in der Reihenfolge der laufenden WM (Plätze 1–6 = Gruppe 1).</summary>
    public IReadOnlyList<int> Order => _fixedOrder ?? Host.Game?.WorldCup?.Order
        ?? [.. Enumerable.Range(NationalTeams.First, NationalTeams.Last - NationalTeams.First + 1)];

    /// <summary>[ORIGINAL] Punkte eines Landes: Σ (9 − Platz) über die Plätze 1 … 8 aller WMs.</summary>
    public int Points(int nation) => Rankings.Sum(r => r.ToList().IndexOf(nation) is var i and >= 0 and < Places ? Places - i : 0);

    /// <summary>[ORIGINAL] Rangfolge: Tauschsortierung der WM-Reihenfolge nach Punkten (mehr Punkte verdrängen).</summary>
    public IReadOnlyList<int> Standings()
    {
        var order = Order.ToList();
        var points = order.ToDictionary(n => n, Points);
        LeagueTable.ExchangeSort(order, (b, a) => points[b] > points[a]);
        return order;
    }

    public override void Update(InputState input)
    {
        if (input.Pressed(Key.Escape) || input.RightClick)
            Host.Show(_back);
    }

    public override void Draw(Surface s)
    {
        s.Palette = Assets.Palette;
        s.Blit(Assets.Image(MainMenuScreen.Background), 0, 0);
        Widgets.Frame(s, Assets, TopWindow, FrameStyle.Orange);
        Widgets.Frame(s, Assets, BottomWindow, FrameStyle.Orange);
        CompetitionWidgets.Centered(s, Assets.Bold, 0, 319, 21, Title, Colors.White, DebCupDrawScreen.TitleShadow);
        s.HLine(27, 23, 292 - 27 + 1, Colors.White);

        for (var c = 0; c < 3 + Columns; c++)
        {
            if (c is 1 or 2)
                continue;
            for (var r = 0; r < Places; r++)
                Cell(s, 31 + 14 * c, 36 + 9 * r);
        }
        var mini = Assets.Mini;
        for (var r = 0; r < Places; r++)
        {
            CompetitionWidgets.Left(s, mini, 23, 42 + 9 * r, $"{r + 1}.", r == 0 ? (byte)17 : Colors.White);
            if (r < SeparatorY.Length)
                s.HLine(31, SeparatorY[r], 295 - 31 + 1, 8);
        }

        var rankings = Rankings;
        for (var k = 0; k < rankings.Count && k < Columns; k++)
            for (var p = 0; p < Places && p < rankings[k].Count; p++)
                SmallFlag(s, rankings[k][p], 75 + 14 * k, 28 + 9 * (p + 1));

        var standings = Standings();
        for (var r = 0; r < Places && r < standings.Count; r++)
        {
            var nation = standings[r];
            SmallFlag(s, nation, 33, 37 + 9 * r);
            CompetitionWidgets.Centered(s, mini, 45, 45, 42 + 9 * r, CompetitionWidgets.Number(Points(nation), 3) + " P.",
                Colors.TileBackground, 6, left: true);
        }

        var order = Order;
        for (var i = 0; i < order.Count && i < 12; i++)
        {
            var x = 24 + 46 * (i % 6);
            var y = i < 6 ? 131 : 177;
            var small = i < 6 ? 122 : 220;
            NationalWidgets.FillInclusive(s, x, y, x + 41, y + 41, 1);
            s.HLine(x, y, 42, 23);
            s.VLine(x, y, 42, 23);
            s.VLine(x + 41, y + 1, 41, 29);
            s.HLine(x + 1, y + 41, 40, 29);
            if (Host.Game?.Clubs.ElementAtOrDefault(order[i]) is { } club)
                Widgets.Logo(s, Assets, club.LogoGraphic, x + 2, y + 2);
            Cell(s, x + 14, small);
            SmallFlag(s, order[i], x + 16, small + 1);
        }
    }

    /// <summary>[ORIGINAL] Kästchen 13 × 8: Fläche Farbe 1, oben/links Farbe 23, rechts/unten Farbe 22.</summary>
    private static void Cell(Surface s, int x, int y)
    {
        NationalWidgets.FillInclusive(s, x, y, x + 12, y + 7, 1);
        s.HLine(x, y, 13, 23);
        s.VLine(x, y, 8, 23);
        s.VLine(x + 12, y + 1, 7, 22);
        s.HLine(x + 1, y + 7, 11, 22);
    }

    /// <summary>Kleine Flagge aus kb\12 (Index = Land − erstes Land).</summary>
    private void SmallFlag(Surface s, int nation, int x, int y)
    {
        var index = nation - NationalTeams.First;
        if (index is < 0 or > NationalTeams.Last - NationalTeams.First)
            return;
        s.Blit(Assets.Image(SmallFlagGraphic), new Rect(index * SmallFlagWidth, 0, SmallFlagWidth, SmallFlagHeight), x, y);
    }
}

/// <summary>Anmeldung im Hauptmenü: Statistiken → WM-Statistik.</summary>
internal static class WorldCupStatisticsMenuEntries
{
    [MenuEntry("WM-Statistik")]
    private static Screen Open(ScreenHost host, Screen back) => new WorldCupStatisticsScreen(host, back);
}
