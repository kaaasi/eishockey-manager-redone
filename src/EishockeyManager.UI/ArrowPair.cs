using EishockeyManager.Formats;

namespace EishockeyManager.UI;

/// <summary>
/// Pfeilpaar des Originals (em.exe Datei 0x4BA58, Stub 0x120:0x3E): zwei Trefferflächen 8 × 9, gedrückte Darstellung aus kb\4
/// und Wiederholung bei gehaltener Taste. Baustein für alle Bildschirme mit Pfeilpaaren (M-K4).
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>[ORIGINAL] Argumente (x1, y1, x2, y2, Halten); Fläche (x1 | y1) liefert −1, (x2 | y2) +1, je x ≤ Maus-x &lt; x + 8 und
///   y ≤ Maus-y &lt; y + 9 (Datei 0x4BA76–0x4BABA). Geprüft wird nur beim Drücken der linken Taste (Ereignis 1); liegen beide
///   Flächen übereinander, gewinnt die zweite.</item>
/// <item>[ORIGINAL] Gedrückt: Quelle kb\4 (22 | 19 + 9 · j), 8 × 7, Ziel (x + 2 | y + 1); j = 0 für −1, 1 für +1
///   (Datei 0x4BB73–0x4BBFE). Losgelassen: Quelle (22 | 1 + 9 · j), Ziel (x + 2 | y + 1) (Datei 0x4BAD3–0x4BB5C); bei Halten ≠ 0
///   Quelle (21 | 1 + 9 · j), Ziel (x + 1 | y + 1) (Datei 0x4BC27–0x4BCA9). Kopie 0x139C9 mit Maske 0xC0 (deckend, wie die
///   Hintergrundsicherung in Fenster_Zu).</item>
/// <item>[ORIGINAL] Halten = 0: Richtung in DGROUP 0x5350 merken und sofort ±1 liefern; jeder weitere Aufruf liefert die
///   gemerkte Richtung, bis die Taste losgelassen wird (Ereignis 2: ungedrückt zeichnen, Merker 0, Rückgabe 0).
///   Halten ≠ 0: auf das Loslassen warten, dann ungedrückt zeichnen und ±1 liefern.</item>
/// <item>[APPROX] Das Original wartet bei Halten ≠ 0 in einer Schleife (0x38:0x878); der Baustein blockiert nicht, sondern zeigt
///   den gedrückten Pfeil bis zum Loslassen und liefert ±1 erst im Aufruf, der das Loslassen sieht. Ereignis 2 entspricht
///   „linke Taste nicht mehr unten“ (<see cref="InputState.LeftDown"/>). Die Wiederholung kommt einmal je Aufruf (je Frame);
///   eine Bremse legt der Aufrufer fest wie im Original.</item>
/// <item>Bekannte Aufrufstellen in em.exe (x1, y1, x2, y2, Halten): 0x2BA72/0x2BFF7 (138, 183, 138, 192, 0) Werbung;
///   0x2C77E/0x2CBAD (242, 208, 242, 217, 0); 0x2EB4A/0x2EC76 (165, 30, 165, 39, 0); 0x3DD89/0x3E4CC (192, 189, 202, 189, 0)
///   und 0x3E0BC/0x3E1F6 (192, 210, 202, 210, 0) Stadion; 0x5E3B5 (150, 169, 174, 169, 1).</item>
/// </list>
/// </remarks>
public sealed class ArrowPair
{
    public const int HitWidth = 8;
    public const int HitHeight = 9;
    public const int ArrowWidth = 8;
    public const int ArrowHeight = 7;

    /// <summary>Bildnummer der Pfeilgrafik (kb\4, beim Spielstart geladen, Datei 0x438B5).</summary>
    public const int Image = 4;

    private readonly int _x1;
    private readonly int _y1;
    private readonly int _x2;
    private readonly int _y2;
    private sbyte _held;    // DGROUP 0x5350 (Halten = 0)
    private sbyte _waiting; // Halten ≠ 0: gedrückt, Loslassen steht aus

    public ArrowPair(int x1, int y1, int x2, int y2, bool waitForRelease = false)
    {
        _x1 = x1;
        _y1 = y1;
        _x2 = x2;
        _y2 = y2;
        WaitForRelease = waitForRelease;
    }

    /// <summary>Halten ≠ 0: ±1 erst beim Loslassen.</summary>
    public bool WaitForRelease { get; }

    /// <summary>Trefferfläche für −1.</summary>
    public Rect First => new(_x1, _y1, HitWidth, HitHeight);

    /// <summary>Trefferfläche für +1.</summary>
    public Rect Second => new(_x2, _y2, HitWidth, HitHeight);

    /// <summary>Gedrückt gezeichneter Pfeil: −1, +1 oder 0.</summary>
    public int Pressed => _held != 0 ? _held : _waiting;

    /// <summary>Ein Aufruf von 0x4BA58 mit den Eingaben dieses Frames; liefert −1, +1 oder 0.</summary>
    public int Update(InputState input)
    {
        var released = !input.LeftDown && !input.LeftClick;
        // [APPROX] Ein neues Drücken (LeftClick) während einer gemerkten Richtung heißt: dazwischen wurde losgelassen, ohne dass ein
        // Frame es sah (im Original steht das Loslassen als Ereignis 2 in der Warteschlange). Neu auswerten statt weiterzählen.
        if (_held != 0 && input.LeftClick)
            _held = 0;
        if (_held != 0)
        {
            if (!released)
                return _held;
            _held = 0;
            return 0;
        }

        if (_waiting != 0)
        {
            if (!released)
                return 0;
            var result = _waiting;
            _waiting = 0;
            return result;
        }

        if (!input.LeftClick)
            return 0;
        sbyte hit = 0;
        if (First.Contains(input.MouseX, input.MouseY))
            hit = -1;
        if (Second.Contains(input.MouseX, input.MouseY))
            hit = 1;
        if (hit == 0)
            return 0;
        if (WaitForRelease)
        {
            _waiting = hit;
            return 0;
        }

        _held = hit;
        return hit;
    }

    /// <summary>Zeichnet den gedrückten Pfeil (falls einer gedrückt ist); den ungedrückten zeichnet der Bildschirm mit.</summary>
    public void Draw(Surface s, VgaImage arrows)
    {
        if (Pressed == 0)
            return;
        var j = Pressed > 0 ? 1 : 0;
        var (x, y) = Pressed > 0 ? (_x2, _y2) : (_x1, _y1);
        s.Blit(arrows, new Rect(22, 19 + 9 * j, ArrowWidth, ArrowHeight), x + 2, y + 1);
    }

    /// <summary>Zeichnet einen Pfeil ungedrückt wie beim Loslassen (Halten = 0 bzw. Halten ≠ 0).</summary>
    public void DrawReleased(Surface s, VgaImage arrows, int direction)
    {
        var j = direction > 0 ? 1 : 0;
        var (x, y) = direction > 0 ? (_x2, _y2) : (_x1, _y1);
        if (WaitForRelease)
            s.Blit(arrows, new Rect(21, 1 + 9 * j, ArrowWidth, ArrowHeight), x + 1, y + 1);
        else
            s.Blit(arrows, new Rect(22, 1 + 9 * j, ArrowWidth, ArrowHeight), x + 2, y + 1);
    }
}
