namespace EishockeyManager.UI;

/// <summary>
/// Bausteine der Live-Anzeige aus Overlay 0x130 von em.exe (L3, <c>docs/analyse/re-livespiel.md</c> §4): Spieluhr, Balken,
/// Lampe und Ziffern. Alle Koordinaten sind wie im Original eingeschlossen (Rechteck 0x0B620 und Linie 0x0B7B5 zeichnen
/// beide Endpunkte).
/// </summary>
public static class LiveWidgets
{
    /// <summary>Grafik mit Ziffern (8 × 10 ab (Ziffer · 8 | 0)) und Lampe (4 × 4 ab (76 | 10)).</summary>
    public const int DigitsGraphic = 38;
    public const int DigitWidth = 8;
    public const int DigitHeight = 10;

    /// <summary>[ORIGINAL] Datei 0x53234: Füllfarbe der Arbeitszelle bei Stil ≠ 0 (0x0B620 mit Farbe 3).</summary>
    public const byte DigitCellFill = 3;

    /// <summary>[ORIGINAL] Datei 0x533D0: Grund des waagerechten Balkens (Farbe 0x11).</summary>
    public const byte BarBackground = 0x11;

    /// <summary>[ORIGINAL] Datei 0x53307: oberer, leerer Teil des senkrechten Balkens (Farbe 0x12).</summary>
    public const byte BarEmpty = 0x12;

    /// <summary>
    /// [ORIGINAL] Farben der zwei Balkenzeilen bzw. -spalten: DGROUP 0x5425 (senkrecht) und 0x5427 (waagerecht), beide
    /// 0x18, 0x13 (gelesen aus em.exe, DGROUP ab Datei 0x16DD0).
    /// </summary>
    public static readonly byte[] BarColors = [0x18, 0x13];

    /// <summary>[ORIGINAL] Datei 0x53453: Lampe aus = Rechteck in Farbe 0x10.</summary>
    public const byte LampOff = 0x10;

    /// <summary>[ORIGINAL] Datei 0x53453 und 0x53234: Durchsichtigkeit 0xC0 beim Kopieren aus der Arbeitsfläche.</summary>
    public const byte Transparent = 0xC0;

    /// <summary>Quelle der Lampe in kb\38.</summary>
    public static readonly Rect LampSource = new(76, 10, 4, 4);

    /// <summary>
    /// [ORIGINAL] Datei 0x51E90 (Spieluhr, Stub 0x130:0x20): drei waagerechte Balken
    /// <c>0x533D0(9, 192 + 8k, 79, 4 · (Minute − 20k))</c> – ein negativer Rest zählt als 0 – und die Minute als zwei Ziffern
    /// (0x53234) bei (96 | 196) und (106 | 196).
    /// </summary>
    public const int ClockBarX = 9;
    public const int ClockBarY = 192;
    public const int ClockBarStep = 8;
    public const int ClockBarMax = 79;
    public const int ClockDigitX = 96;
    public const int ClockDigitStep = 10;
    public const int ClockDigitY = 196;

    /// <summary>Spieluhr Datei 0x51E90 mit Stil (0 = Hintergrund unter den Ziffern, sonst Farbe 3) und Minute.</summary>
    public static void Clock(Surface s, Assets assets, int style, int minute)
    {
        // [ORIGINAL] Datei 0x51E96–0x51EE7: Rest als Byte mit Vorzeichen; Wert = Rest · 4, danach Rest − 20, unter 0 → 0.
        var rest = (int)(sbyte)(byte)minute;
        for (var k = 0; k < 3; k++)
        {
            HorizontalBar(s, ClockBarX, ClockBarY + ClockBarStep * k, ClockBarMax, rest * 4);
            rest = Math.Max(0, rest - 20);
        }
        // [ORIGINAL] Datei 0x51EE9–0x51F33: Minute / 10 bei x 0x60, Minute mod 10 bei x 0x6A, y 0xC4.
        Digit(s, assets, style, minute / 10, ClockDigitX, ClockDigitY);
        Digit(s, assets, style, minute % 10, ClockDigitX + ClockDigitStep, ClockDigitY);
    }

    /// <summary>
    /// [ORIGINAL] Datei 0x533D0 (Stub 0x130:0x61), waagerechter Balken: Wert auf <paramref name="max"/> begrenzt; ist er
    /// kleiner, Rechteck (x | y)–(x + max | y + 1) in Farbe 0x11; ist er größer als 0, zwei Linien (x | y + k)–(x + Wert | y + k)
    /// in den Farben <see cref="BarColors"/>.
    /// </summary>
    public static void HorizontalBar(Surface s, int x, int y, int max, int value)
    {
        value = Math.Min(value, max);
        if (max > value)
            FillInclusive(s, x, y, x + max, y + 1, BarBackground);
        if (value <= 0)
            return;
        for (var k = 0; k < 2; k++)
            FillInclusive(s, x, y + k, x + value, y + k, BarColors[k]);
    }

    /// <summary>
    /// [ORIGINAL] Datei 0x53307 (Stub 0x130:0x5C), senkrechter Balken: Wert auf <paramref name="max"/> begrenzt; ist er größer
    /// als 0, zwei Spalten x + 2 + k von y + max bis y + max − Wert − 1 in den Farben <see cref="BarColors"/>; ist er kleiner
    /// als max, danach dieselben Spalten von y bis y + max − Wert in Farbe 0x12. Die zweite Linie überdeckt die obersten zwei
    /// Zeilen der ersten, sichtbar bleiben also genau Wert Pixel.
    /// </summary>
    public static void VerticalBar(Surface s, int x, int y, int max, int value)
    {
        value = Math.Min(value, max);
        if (value > 0)
            for (var k = 0; k < 2; k++)
                FillInclusive(s, x + 2 + k, y + max - value - 1, x + 2 + k, y + max, BarColors[k]);
        if (max > value)
            for (var k = 0; k < 2; k++)
                FillInclusive(s, x + 2 + k, y, x + 2 + k, y + max - value, BarEmpty);
    }

    /// <summary>
    /// [ORIGINAL] Datei 0x53453 (Stub 0x130:0x66), Lampe: an → kb\38 (76 | 10) 4 × 4 mit Durchsichtigkeit 0xC0 nach
    /// (x + 1 | y + 2); aus → Rechteck (x | y + 1)–(x + 4 | y + 5) in Farbe 0x10.
    /// </summary>
    public static void Lamp(Surface s, Assets assets, int x, int y, bool on)
    {
        if (on)
            s.Blit(assets.Image(DigitsGraphic), LampSource, x + 1, y + 2, transparent: Transparent);
        else
            FillInclusive(s, x, y + 1, x + 4, y + 5, LampOff);
    }

    /// <summary>
    /// [ORIGINAL] Datei 0x53234 (Stub 0x130:0x57), Ziffer 8 × 10 bei (x | y): Arbeitszelle vorbereiten – Stil 0: Hintergrund
    /// der Zielstelle, Stil ≠ 0: Farbe 3 –, die Ziffer (Ziffer · 8 | 0) aus kb\38 hineinkopieren (0x50:0xBD3; Farbe 0 der
    /// Ziffer lässt die Zelle stehen), Zelle mit Durchsichtigkeit 0xC0 ans Ziel.
    /// </summary>
    /// <remarks>
    /// [APPROX] Stil 0 kopiert im Original aus dem Hintergrundpuffer (Fläche DGROUP 0xC12B), das Remake aus dem Ziel selbst:
    /// Bildschirme zeichnen in jedem Bild den Hintergrund neu, die Stelle enthält also keine alte Ziffer. Dass 0x50:0xBD3
    /// Farbe 0 durchlässt, ist aus dem Video geschlossen (`karriere_02315`: Inneres der „0“ in Farbe 3, nicht schwarz).
    /// </remarks>
    public static void Digit(Surface s, Assets assets, int style, int digit, int x, int y)
    {
        if (style != 0)
            s.Fill(new Rect(x, y, DigitWidth, DigitHeight), DigitCellFill);
        s.Blit(assets.Image(DigitsGraphic), new Rect(digit * DigitWidth, 0, DigitWidth, DigitHeight), x, y, transparent: 0);
    }

    private static void FillInclusive(Surface s, int x1, int y1, int x2, int y2, byte color) =>
        s.Fill(new Rect(Math.Min(x1, x2), Math.Min(y1, y2), Math.Abs(x2 - x1) + 1, Math.Abs(y2 - y1) + 1), color);
}
