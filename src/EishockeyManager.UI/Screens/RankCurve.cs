namespace EishockeyManager.UI.Screens;

/// <summary>
/// Tabellenkurve im Nachrichtenfeld der Infoleiste (Spur ui-meldungen, Teil 2, M-K6): Platz nach jedem Spieltag, oben =
/// Platz 1, rechts daneben der aktuelle Platz mit Punkt.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>[ORIGINAL] em.exe Datei 0x445E0 (Aufbau in <c>docs/analyse/re-meldungen.md</c> §E), Aufruf der Infoleiste Datei
///   0x445A6 mit Ursprung x = 3, Grundwert y = −2. Achsen: senkrecht (X + 2 | Y + 48)–(X + 2 | Y + 71) Farbe 6, waagrecht
///   (X + 3 | Y + 72)–(X + 98 | Y + 72) Farbe 8; sie stehen auch vor dem ersten Spieltag (Video 14:00 vermessen, Bild
///   <c>oberflaeche_0842</c>: senkrecht ab y 46, waagrecht y 70).</item>
/// <item>[ORIGINAL] Schrittweite d = ⌊85 / (n − 1)⌋ (n − 1 = gespielte Spieltage), d = 1 → 2; Punkt k bei x = X + 2 + (k + 1) · d,
///   y = Y + 49 + 2 · p (p = Platz − 1); die Linie beginnt bei (X + 3 | Y + 49 + 2 · p₀). Eigener Verein Farbe 24, Gegner Farbe 7.
///   Im Bild <c>folge06_00000</c> (35 Spieltage, d = 2) endet die gelbe Kurve bei x 75; Achsen und Plätze 3 / 12 stimmen.</item>
/// <item>[APPROX] Lage der Platzzahl (x = Kurvenende + 1, Oberkante = y − 4, im Bild <c>folge06_00000</c> auf ±1 Pixel) und die
///   Verschiebung bei |Δy| &lt; 7 (Datei 0x44801–0x44891 nicht im Pixel ausgewertet: die Zahl des Gegners rückt auf 7 Pixel Abstand).</item>
/// </list>
/// </remarks>
public static class RankCurve
{
    public const int OriginX = 3, BaseY = -2;
    public const byte VerticalAxisColor = 6, HorizontalAxisColor = 8;

    /// <summary>Schrittweite je Spieltag bei <paramref name="played"/> gespielten Spieltagen. [ORIGINAL] 0x445E0.</summary>
    public static int Step(int played)
    {
        var d = 8500 / Math.Max(1, played) / 100;
        return d == 1 ? 2 : d;
    }

    /// <summary>Y-Lage eines Platzes (1-basiert).</summary>
    public static int Y(int rank, int baseY = BaseY) => baseY + 49 + 2 * (rank - 1);

    /// <summary>Stützpunkte der Kurve in Bildschirmkoordinaten; Punkt 0 ist der Linienanfang (X + 3).</summary>
    public static IReadOnlyList<(int X, int Y)> Points(IReadOnlyList<int> ranks, int originX = OriginX, int baseY = BaseY)
    {
        var points = new List<(int, int)>();
        var d = Step(ranks.Count);
        for (var k = 0; k < ranks.Count; k++)
        {
            var y = Y(ranks[k], baseY);
            if (k == 0)
                points.Add((originX + 3, y));
            points.Add((originX + 2 + (k + 1) * d, y));
        }
        return points;
    }

    public static void DrawAxes(Surface s, int originX = OriginX, int baseY = BaseY)
    {
        s.VLine(originX + 2, baseY + 48, 24, VerticalAxisColor);
        s.HLine(originX + 3, baseY + 72, 96, HorizontalAxisColor);
    }

    /// <summary>Achsen und die Kurven; <paramref name="opponent"/> darf leer sein (kein Verein als Gegner).</summary>
    public static void Draw(Surface s, Assets a, IReadOnlyList<int> own, IReadOnlyList<int>? opponent,
        int originX = OriginX, int baseY = BaseY)
    {
        DrawAxes(s, originX, baseY);
        var ownY = own.Count > 0 ? Y(own[^1], baseY) : int.MinValue;
        if (opponent is { Count: > 0 })
        {
            var y = Y(opponent[^1], baseY);
            if (own.Count > 0 && Math.Abs(y - ownY) < 7)
                y = y >= ownY ? ownY + 7 : ownY - 7; // [APPROX]
            DrawOne(s, a, opponent, Colors.White, y, originX, baseY);
        }
        if (own.Count > 0)
            DrawOne(s, a, own, Colors.Yellow, ownY, originX, baseY);
    }

    private static void DrawOne(Surface s, Assets a, IReadOnlyList<int> ranks, byte color, int numberY, int originX, int baseY)
    {
        var points = Points(ranks, originX, baseY);
        for (var i = 1; i < points.Count; i++)
            Line(s, points[i - 1].X, points[i - 1].Y, points[i].X, points[i].Y, color);
        var end = points[^1];
        s.Text(a.Mini, end.X + 1, numberY - 4, $"{ranks[^1]}.", color);
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
}
