namespace EishockeyManager.UI.Screens;

/// <summary>
/// Ereignissymbole aus kb\13 mit Rahmen, wie das Original sie im Fenster „Saisonende“ und hinter den Vertragsverhandlungen
/// am Saisonende zeichnet (Spur ui-rest, Runde 17, Teil 2).
/// </summary>
public static class EventSymbols
{
    /// <summary>Bildnummer der Symbole (kb\13, geladen in Datei 0x26F5B–0x26F73).</summary>
    public const int Sheet = 13;

    public const int SymbolWidth = 24;
    public const int SymbolHeight = 21;

    /// <summary>
    /// [ORIGINAL] Rahmen hinter jedem Symbol: kb\13 Quelle (144 | 0), 30 × 27, deckend (Datei 0x06CF1–0x06D17: Kopie 0x139C9
    /// mit Maske 0xC0).
    /// </summary>
    public static readonly Rect Frame = new(144, 0, 30, 27);

    /// <summary>[ORIGINAL] Versatz des Symbols im Rahmen (Datei 0x06D42–0x06D4D: x + 3, y + 3).</summary>
    public const int Inset = 3;

    /// <summary>Symbol 9 (Fläschchen): ein Jugendspieler rückt am Saisonende in den Kader (Datei 0x27302).</summary>
    public const int Youth = 9;

    /// <summary>Symbol 10 (Vertrag): Kachel rechts unten hinter den Verhandlungen am Saisonende (Datei 0x27CA7).</summary>
    public const int Contract = 10;

    /// <summary>
    /// Quelle der Symbolnummer <paramref name="n"/> in kb\13. [ORIGINAL] Datei 0x06D21–0x06D35: <c>while (n &gt; 5) { n -= 6;
    /// y += 21; }</c>, x = n · 24 – sechs Zellen je Zeile.
    /// </summary>
    public static Rect Source(int n) => new(n % 6 * SymbolWidth, n / 6 * SymbolHeight, SymbolWidth, SymbolHeight);

    /// <summary>
    /// Lage des <paramref name="index"/>-ten Symbols im Fenster „Saisonende“ (linke obere Ecke des Rahmens).
    /// [ORIGINAL] Datei 0x06CA6–0x06CC7: Start (76 | 101), je Symbol x + 35; überschreitet x den Wert 230, wieder x = 76 und
    /// y + 30 – also fünf Symbole je Zeile, linksbündig. DOSBox-Bilder <c>file16_9</c> (drei Symbole ab x = 76) und
    /// <c>file46_2</c> (eines bei x = 76).
    /// </summary>
    public static (int X, int Y) Position(int index)
    {
        int x = 76, y = 101;
        for (var i = 0; i < index; i++)
        {
            x += 35;
            if (x > 230)
            {
                x = 76;
                y += 30;
            }
        }
        return (x, y);
    }

    /// <summary>[ORIGINAL] Datei 0x06CDF: Rahmen bei (x | y), darauf das Symbol bei (x + 3 | y + 3), beides deckend.</summary>
    public static void Draw(Surface s, Assets assets, Rect symbol, int x, int y)
    {
        var sheet = assets.Image(Sheet);
        s.Blit(sheet, Frame, x, y);
        s.Blit(sheet, symbol, x + Inset, y + Inset);
    }
}
