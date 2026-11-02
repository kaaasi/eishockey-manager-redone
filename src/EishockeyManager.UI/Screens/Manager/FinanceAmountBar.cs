namespace EishockeyManager.UI.Screens.ManagerArea;

/// <summary>
/// Rechenregeln des Betragsbalkens im Bildschirm Finanzen (em.exe Datei 0x58409, Ov 0x138; docs/analyse/re-geld.md,
/// „Datei 0x58409 – Betragsbalken“). Das Original rechnet Geld in Drittel-DM; die Methoden nehmen und liefern ganze DM
/// bzw. Stück und rechnen intern in Dritteln, damit Rundung und Grenzen genau wie im Original fallen.
/// </summary>
public static class FinanceAmountBar
{
    /// <summary>[ORIGINAL] Teiler für den Schrittwert: q = Höchstwert / 154 + 1 (Datei 0x59A15–0x59A30).</summary>
    public const int Divisor = 154;

    /// <summary>[ORIGINAL] Rundung beim Klick auf den Balken: ganze 3.000 Drittel-DM = 1.000 DM (Datei 0x58463–0x5848C).</summary>
    public const long MoneyRounding = 1_000;

    /// <summary>[ORIGINAL] Schritt der Knöpfe: ±1 Stück bzw. ±3.000 Drittel-DM = 1.000 DM (Datei 0x58FCC–0x58FED).</summary>
    public static long Step(bool stock) => stock ? 1 : MoneyRounding;

    /// <summary>Höchstwert in Einheiten des Originals (Drittel-DM bzw. Stück).</summary>
    private static long Units(long value, bool stock) => stock ? value : value * 3;

    /// <summary>[ORIGINAL] Schrittwert q je Pixel in Einheiten des Originals (Drittel-DM bzw. Stück).</summary>
    public static long Quantum(long max, bool stock) => Units(Math.Max(0, max), stock) / Divisor + 1;

    /// <summary>
    /// [ORIGINAL] Betrag für einen Klick auf Pixel <paramref name="pixels"/> des Balkens (Datei 0x58409, s ≠ −1):
    /// v = s · q, bei Geld auf ganze 3.000 Drittel abgerundet, höchstens der Höchstwert. Ein Klick in die andere Hälfte
    /// ergibt ein negatives s; das Original vergleicht vorzeichenlos (Teilung 0x02283, Vergleich Datei 0x58495), der Betrag
    /// wird dann zum Höchstwert.
    /// </summary>
    public static long FromPixels(int pixels, long max, bool stock)
    {
        max = Math.Max(0, max);
        if (pixels < 0)
            return max;
        var v = pixels * Quantum(max, stock);
        if (!stock)
            v = v / (3 * MoneyRounding) * (3 * MoneyRounding) / 3;
        return Math.Min(v, max);
    }

    /// <summary>
    /// [ORIGINAL] Balkenlänge zu einem vorgegebenen Betrag (Datei 0x58441–0x58461, s = −1): s = v / q, ohne Rundung.
    /// [APPROX] Für die Zeichnung auf 0–154 begrenzt (das Original rechnet mit dem ungekürzten Betrag weiter).
    /// </summary>
    public static int Pixels(long amount, long max, bool stock) =>
        (int)Math.Clamp(Units(Math.Max(0, amount), stock) / Quantum(max, stock), 0, Divisor);

    /// <summary>
    /// [ORIGINAL] Ein Schritt mit den Knöpfen (Datei 0x58F80–0x59017): „−“ nur bei Betrag ≠ 0, „+“ nur unter dem Höchstwert;
    /// danach höchstens der Höchstwert. Ein Betrag unter einem Schritt wird bei „−“ negativ und im vorzeichenlosen Vergleich
    /// (Datei 0x58495) zum Höchstwert. Liefert den neuen Betrag oder null, wenn der Knopf nichts tut.
    /// </summary>
    public static long? Press(long amount, int direction, long max, bool stock)
    {
        max = Math.Max(0, max);
        if (direction < 0 ? amount == 0 : amount >= max)
            return null;
        var v = amount + direction * Step(stock);
        return v < 0 ? max : Math.Min(v, max);
    }
}
