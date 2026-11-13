namespace EishockeyManager.UI;

/// <summary>
/// Schmaler grauer Knopf im Stil des Originals für Remake-Erweiterungen ohne Vorbild (Spur nutzer-befunde, #836/#840).
/// [APPROX] Aus dem Knopf kb\2 (57 × 12, <see cref="Widgets.Button"/>) zusammengesetzt: linker Teil bis zur gewünschten Breite
/// und die rechte Kante (3 Spalten) des Originalknopfs, Beschriftung in Mini wie dort.
/// </summary>
public static class SmallButton
{
    private const int Edge = 3;

    public static void Draw(Surface s, Assets a, Rect r, string text, bool pressed = false)
    {
        var sheet = a.Image(2);
        var sourceY = pressed ? 191 : 179;
        var left = Math.Clamp(r.W - Edge, 1, Widgets.ButtonWidth - Edge);
        s.Blit(sheet, new Rect(17, sourceY, left, Widgets.ButtonHeight), r.X, r.Y);
        s.Blit(sheet, new Rect(17 + Widgets.ButtonWidth - Edge, sourceY, Edge, Widgets.ButtonHeight), r.X + left, r.Y);
        s.TextCentered(a.Mini, r.X + r.W / 2 + (pressed ? 1 : 0), r.Y + 4 + (pressed ? 1 : 0), text, 1);
    }
}
