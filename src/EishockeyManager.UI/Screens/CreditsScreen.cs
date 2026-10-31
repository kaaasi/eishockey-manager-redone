using EishockeyManager.Formats;

namespace EishockeyManager.UI.Screens;

/// <summary>
/// Credits-Fenster des Hauptmenüs (#207): Klick in die Titelleiste „EISHOCKEY MANAGER“ zeigt das Bild kb\15 über dem
/// Menü, der nächste Klick schließt es wieder. Das Öffnen ändert keinen Spielzustand.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>[ORIGINAL] Auslöser: em.exe Hauptmenüschleife, Datei 0x4544E–0x45488: nach einem Mausereignis 1 oder 3
///   (DGROUP 0x5A16, linke bzw. rechte Taste) und Maus-y &lt; 0x15 (Register si) Fernaufruf <c>0xF8:0x20</c>.
///   Die x-Lage spielt keine Rolle.</item>
/// <item>[ORIGINAL] Fenster: Overlay 0xF8 (Segmenttabelle Eintrag 0xF8, Datei 0x399D0), erste Funktion. Sie lädt
///   kb\15 (216 × 151 Pixel) – die Texte sind kein Text in em.exe, sondern fertig in diesem Bild –, sichert den
///   Bildschirm darunter (216 × 153 ab x 0x33, y 0x3D), kopiert das Bild nach (0x33 | 0x3D) = (51 | 61), wartet und
///   stellt den gesicherten Hintergrund wieder her.</item>
/// <item>[ORIGINAL] Schließen: Wartefunktion <c>0x38:0x118</c> (Datei 0xB908): zuerst bis kein Mausereignis mehr
///   ansteht (0xC097), dann bis Mausereignis 1 oder 3 (linke oder rechte Taste, irgendwo) oder DGROUP 0x1DB
///   (Durchlauf ohne Halt, gibt es im Remake nicht). Tasten schließen das Fenster dort nicht; ob 0xBBF:0x24DF auch
///   Tasten als Ereignis 1/3 meldet, ist nicht geprüft.</item>
/// <item>[ORIGINAL] Schrift der Bildtexte: exakt der Mini-Font mit Schatten (Farbe 0, 1 Pixel rechts unten), linke
///   Spalte x 11 in Farbe 30, rechte Spalte x 100 in Farbe 7 (im Bild nachgemessen: jede Zeile pixelgenau gleich).
///   Zeilenabstand innerhalb eines Eintrags 8, zwischen Einträgen 13/13/12/12/12/11/11; letzte Zeile
///   „PC Umsetzung“ / „Ralph Noeske“ bei y 138.</item>
/// <item>[REMAKE] Bewusste Erweiterung (Wunsch Nutzer, #207, <c>docs/portierung.md</c>): letzter Eintrag
///   „Remake“ / „Jörg Klinge“ 11 Zeilen unter „PC Umsetzung“ (Abstand wie der letzte Eintragsabstand des Originals),
///   gleiche Spalten, Schrift und Farben. Dafür wird das Bild um <see cref="ExtraRows"/> Zeilen verlängert: Die neuen
///   Zeilen wiederholen die beiden textfreien Zeilen 145/146 über dem unteren Rand (Hintergrund und Rahmen, das
///   Schachbrettmuster des Verlaufs bleibt in der Phase), der untere Rand (Zeilen 147–150) rückt nach unten.</item>
/// </list>
/// </remarks>
public sealed class CreditsScreen : Screen
{
    /// <summary>Bildnummer kb\15. [ORIGINAL] em.exe Datei 0x399FF (<c>mov al,0xf</c> vor dem Bildladen).</summary>
    public const int Picture = 15;

    /// <summary>Lage des Fensters. [ORIGINAL] em.exe Datei 0x39A25/0x39A29 (Zielpunkt 0x3D, 0x33).</summary>
    public const int X = 51, Y = 61;

    /// <summary>Größe des Originalbilds kb\15. [ORIGINAL] em.exe Datei 0x39A45/0x39A49 (0x97 × 0xD8).</summary>
    public const int OriginalWidth = 216, OriginalHeight = 151;

    /// <summary>Titelleiste: Klick mit y &lt; 21 öffnet die Credits. [ORIGINAL] em.exe Datei 0x45483 (<c>cmp si,0x15</c>).</summary>
    public static readonly Rect TitleArea = new(0, 0, Surface.Width, 0x15);

    /// <summary>Spalten und Farben der Bildtexte (im Bild kb\15 nachgemessen, siehe Klassenkommentar).</summary>
    public const int LeftColumn = 11, RightColumn = 100;

    public const byte LeftColor = 30, RightColor = Colors.White, ShadowColor = Colors.Black;

    /// <summary>Oberkante der letzten Originalzeile „PC Umsetzung“ / „Ralph Noeske“ im Bild.</summary>
    public const int LastOriginalLine = 138;

    /// <summary>Oberkante der Zeile davor („Franz Hauerstein“) – ihr Abstand ist der Eintragsabstand für die Ergänzung.</summary>
    public const int LineBeforeLast = 127;

    /// <summary>[REMAKE] Abstand der Ergänzung = letzter Eintragsabstand des Originals (138 − 127).</summary>
    public const int ExtraRows = LastOriginalLine - LineBeforeLast;

    /// <summary>[REMAKE] Oberkante der Zeile „Remake“ / „Jörg Klinge“ im verlängerten Bild.</summary>
    public const int RemakeLine = LastOriginalLine + ExtraRows;

    public const string RemakeRole = "Remake", RemakeName = "Jörg Klinge";

    /// <summary>Erste Zeile des unteren Rands im Originalbild (Zeilen 147–150: Innenkante und Rahmen).</summary>
    private const int BottomEdge = 147;

    private readonly Screen _back;
    private VgaImage? _window;

    public CreditsScreen(ScreenHost host, Screen back) : base(host) => _back = back;

    /// <summary>Höhe des Fensters mit der Ergänzung.</summary>
    public static int Height => OriginalHeight + ExtraRows;

    public static Rect Area => new(X, Y, OriginalWidth, Height);

    public override void Update(InputState input)
    {
        if (input.LeftClick || input.RightClick)
            Host.Show(_back);
    }

    public override void Draw(Surface s)
    {
        _back.Draw(s);
        s.Blit(_window ??= BuildWindow(Assets), X, Y);
    }

    /// <summary>Bild kb\15, nach unten verlängert und um die Zeile „Remake“ / „Jörg Klinge“ ergänzt.</summary>
    public static VgaImage BuildWindow(Assets assets)
    {
        var source = assets.Image(Picture);
        if (source.Width != OriginalWidth || source.Height != OriginalHeight)
            throw new InvalidDataException($"kb\\{Picture} hat {source.Width} × {source.Height} statt {OriginalWidth} × {OriginalHeight} Pixel.");

        var w = source.Width;
        var pixels = new byte[w * Height];
        for (var y = 0; y < Height; y++)
        {
            int from;
            if (y < BottomEdge)
                from = y;
            else if (y < BottomEdge + ExtraRows)
                from = (y - (BottomEdge - 2)) % 2 == 0 ? BottomEdge - 2 : BottomEdge - 1; // 145/146 im Wechsel, gleiche Phase
            else
                from = y - ExtraRows;
            Array.Copy(source.Pixels, from * w, pixels, y * w, w);
        }

        var font = assets.Mini;
        DrawShadowed(font, pixels, w, LeftColumn, RemakeLine, RemakeRole, LeftColor);
        DrawShadowed(font, pixels, w, RightColumn, RemakeLine, RemakeName, RightColor);
        return new VgaImage(w, Height, pixels);
    }

    private static void DrawShadowed(BitmapFont font, byte[] pixels, int width, int x, int y, string text, byte color)
    {
        font.Draw(pixels, width, x + 1, y + 1, text, ShadowColor);
        font.Draw(pixels, width, x, y, text, color);
    }
}
