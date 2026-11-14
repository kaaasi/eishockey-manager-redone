using System.Buffers.Binary;
using System.Text;

namespace EishockeyManager.Formats;

/// <summary>Ein Objekt einer Torszene (Spieler, Torhüter …): Figur und Startposition.</summary>
public readonly record struct GoalSceneObject(byte Unused, byte Figure, int X, int Y);

/// <summary>Ein Bild einer Torszene (28 Byte).</summary>
/// <param name="Camera">Byte 0: Kamerabewegung (Bewegungsbyte, ersetzt die Geschwindigkeit).</param>
/// <param name="Puck">Byte 1: Puckbewegung (addiert).</param>
/// <param name="Unused">Byte 2: in allen Originaldateien 0.</param>
/// <param name="Sound">Byte 3: Toneffekt (0 keiner).</param>
/// <param name="Objects">Je Objekt Figur und Bewegung (12 Paare).</param>
public sealed record GoalSceneFrame(byte Camera, byte Puck, byte Unused, byte Sound, IReadOnlyList<(byte Figure, byte Move)> Objects);

/// <summary>
/// Torszene <c>tore\*</c> (Spur livespiel-kern, Teil A4; <c>docs/analyse/re-livespiel.md</c> §11, <c>dateiformate.md</c>).
/// [ORIGINAL] Gelesen aus dem Szenenstart <c>48h:5250h</c> und dem Bildschritt <c>48h:5316h</c>, Abspieler Datei 0x56CDA–0x56DC3:
/// alle Wörter Big-Endian; +0x00 „TOR!“ (nicht geprüft), +0x04/+0x06 Kamera, +0x08/+0x0A Puck, ab +0x0C zwölf Objekte zu 6 Byte
/// (Byte 0, Figur, x, y), ab +0x54 Bilder zu 28 Byte bis zum Wort 0xAAAA.
/// </summary>
public sealed class GoalScene
{
    /// <summary>[ORIGINAL] Zahl der Objekte im Kopf (+0x0C … +0x53).</summary>
    public const int ObjectCount = 12;

    /// <summary>[ORIGINAL] Länge des Kopfes (erstes Bild ab +0x54).</summary>
    public const int HeaderSize = 0x54;

    /// <summary>[ORIGINAL] Länge eines Bildes.</summary>
    public const int FrameSize = 28;

    /// <summary>[ORIGINAL] Endmarke (Wort am Bildanfang, Datei 0x56CEA).</summary>
    public const ushort EndMarker = 0xAAAA;

    private GoalScene(string id, int cameraX, int cameraY, int puckX, int puckY, IReadOnlyList<GoalSceneObject> objects,
        IReadOnlyList<GoalSceneFrame> frames)
    {
        Id = id;
        CameraX = cameraX;
        CameraY = cameraY;
        PuckX = puckX;
        PuckY = puckY;
        Objects = objects;
        Frames = frames;
    }

    /// <summary>Kennung am Dateianfang („TOR!“).</summary>
    public string Id { get; }

    public int CameraX { get; }
    public int CameraY { get; }
    public int PuckX { get; }
    public int PuckY { get; }
    public IReadOnlyList<GoalSceneObject> Objects { get; }
    public IReadOnlyList<GoalSceneFrame> Frames { get; }

    public static GoalScene Load(string path) => Parse(File.ReadAllBytes(path));

    public static GoalScene Parse(ReadOnlySpan<byte> data)
    {
        if (data.Length < HeaderSize + 2)
            throw new InvalidDataException("Torszene zu kurz.");
        int Word(ReadOnlySpan<byte> d, int offset) => BinaryPrimitives.ReadUInt16BigEndian(d[offset..]);
        var objects = new GoalSceneObject[ObjectCount];
        for (var k = 0; k < ObjectCount; k++)
        {
            var o = 0x0C + 6 * k;
            objects[k] = new GoalSceneObject(data[o], data[o + 1], Word(data, o + 2), Word(data, o + 4));
        }
        var frames = new List<GoalSceneFrame>();
        var offset = HeaderSize;
        while (true)
        {
            if (offset + 2 > data.Length)
                throw new InvalidDataException("Torszene ohne Endmarke 0xAAAA.");
            if (Word(data, offset) == EndMarker)
                break;
            if (offset + FrameSize > data.Length)
                throw new InvalidDataException("Torszene: Bild abgeschnitten.");
            var f = data.Slice(offset, FrameSize);
            var moves = new (byte, byte)[ObjectCount];
            for (var k = 0; k < ObjectCount; k++)
                moves[k] = (f[4 + 2 * k], f[5 + 2 * k]);
            frames.Add(new GoalSceneFrame(f[0], f[1], f[2], f[3], moves));
            offset += FrameSize;
        }
        return new GoalScene(Encoding.ASCII.GetString(data[..4]), Word(data, 4), Word(data, 6), Word(data, 8), Word(data, 10),
            objects, frames);
    }

    /// <summary>
    /// [ORIGINAL] Bewegungsbyte (<c>48h:51ADh</c>): Bit 7 Vorzeichen y, Bit 6 Vorzeichen x, Bits 5–3 |dx|, Bits 2–0 |dy|.
    /// </summary>
    public static (int Dx, int Dy) DecodeMove(byte move)
    {
        var dx = (move >> 3) & 7;
        var dy = move & 7;
        return ((move & 0x40) != 0 ? -dx : dx, (move & 0x80) != 0 ? -dy : dy);
    }
}

/// <summary>
/// Szenenzahlen aus <c>tore\anzahl</c> (Spur livespiel-kern, Teil A4). [ORIGINAL] Datei 0x57480: bis 200 Byte lesen, von
/// hinten Nichtziffern überspringen und drei Dezimalzahlen rückwärts lesen (zwischen zwei Zahlen genau ein Trennzeichen):
/// letzte → DGROUP 0xE2F4 (sechs Feldspieler), vorletzte → 0xE2F2 (Penalty), drittletzte → 0xE2F6 (normale Szenen);
/// normale oder Penalty-Szenen unter 1 → Fehler 2 („DISK-Error“). Der Text davor wird nicht geprüft.
/// </summary>
public readonly record struct GoalSceneCounts(int Normal, int Penalty, int SixSkaters)
{
    /// <summary>[ORIGINAL] Datei 0x57480: höchstens 200 Byte.</summary>
    public const int MaxLength = 200;

    public static GoalSceneCounts Load(string path) => Parse(File.ReadAllBytes(path));

    public static GoalSceneCounts Parse(ReadOnlySpan<byte> data)
    {
        if (data.Length > MaxLength)
            data = data[..MaxLength];
        var i = data.Length - 1;
        while (i >= 0 && !IsDigit(data[i]))
            i--;
        var values = new int[3];
        for (var n = 0; n < 3; n++)
        {
            var value = 0;
            var factor = 1;
            while (i >= 0 && IsDigit(data[i]))
            {
                value += (data[i] - '0') * factor;
                factor *= 10;
                i--;
            }
            values[n] = value;
            i--; // [ORIGINAL] genau ein Trennzeichen
        }
        var counts = new GoalSceneCounts(values[2], values[1], values[0]);
        if (counts.Normal < 1 || counts.Penalty < 1)
            throw new InvalidDataException("tore\\anzahl: Szenenzahl unter 1 (Fehler 2 des Originals).");
        return counts;
    }

    private static bool IsDigit(byte b) => b is >= (byte)'0' and <= (byte)'9';
}

/// <summary>Bildquelle einer Figur der Torszene (Spur torszenen, Teil C1).</summary>
/// <param name="Graphic">Grafiknummer <c>kb\233 … 235</c>, 0 für die beiden eingebauten Figuren aus em.exe.</param>
/// <param name="Index">Bild in der Quelle (Figurtafel: 32 × 32 Byte je Bild; eingebaut: Figur 0 oder 1).</param>
/// <param name="Size">Kantenlänge in Pixeln (32 bzw. 5).</param>
public readonly record struct GoalSceneFigureSource(int Graphic, int Index, int Size);

/// <summary>
/// Figuren der Torszene (Spur torszenen, Teil C1; <c>docs/analyse/re-livespiel.md</c> §16).
/// [ORIGINAL] Zeichnen eines Objekts <c>48h:4FE7h</c> = Datei 0x129D7–0x12AA8 (Grafiksegment 0BBF): Figur 0/1 → 5 × 5 Pixel aus
/// dem Code-Segment (<c>cs:0x54</c> bzw. <c>cs:0x6D</c>, Zeilenlänge 5); Figur 2 … 41 → Zeiger <c>cs:0x44</c>, 42 … 81 →
/// <c>cs:0x48</c>, 82 … 93 → <c>cs:0x4C</c>, je Bild 1 024 Byte (Index · 0x400, Zeilenlänge 0x20); Figur ≥ 94 zeichnet nichts.
/// Die drei Zeiger kopiert <c>48h:538Ch</c> (Datei 0x12D95–0x12DA1) aus <c>DGROUP 0xE2B9 + 8 … 0x13</c>, also die Bilder
/// i = 2, 3, 4 des Abspielers = <c>kb\233</c>, <c>kb\234</c>, <c>kb\235</c> (Datei 0x568BF–0x568C4: <c>kb\231 + i</c>,
/// Größen <c>DGROUP 0x5404 … 0x5409</c> = 0xA000, 0xA000, 0x3000). Farbindex 0 ist durchsichtig (Datei 0x12A84).
/// </summary>
public static class GoalSceneFigures
{
    /// <summary>[ORIGINAL] Höchste Figurnummer, die gezeichnet wird (Datei 0x12A2F: <c>cmp al,0x5D</c>).</summary>
    public const int MaxFigure = 0x5D;

    /// <summary>[ORIGINAL] Kantenlänge der eingebauten Figuren 0 und 1 (Datei 0x128E0).</summary>
    public const int BuiltInSize = 5;

    /// <summary>[ORIGINAL] Kantenlänge der Figuren aus den Tafeln (Datei 0x128EE).</summary>
    public const int SheetSize = 32;

    /// <summary>[ORIGINAL] Offsets der eingebauten Figuren im Grafiksegment 0BBF (Datei 0x129F3, 0x129F9).</summary>
    public const int BuiltInOffset0 = 0x54, BuiltInOffset1 = 0x6D;

    /// <summary>Bildquelle einer Figurnummer; null, wenn das Original nichts zeichnet (Figur &gt; 93).</summary>
    public static GoalSceneFigureSource? Resolve(int figure) => figure switch
    {
        < 0 => null,
        < 2 => new GoalSceneFigureSource(0, figure, BuiltInSize),
        <= 0x29 => new GoalSceneFigureSource(233, figure - 2, SheetSize),
        <= 0x51 => new GoalSceneFigureSource(234, figure - 0x2A, SheetSize),
        <= MaxFigure => new GoalSceneFigureSource(235, figure - 0x52, SheetSize),
        _ => null,
    };

    /// <summary>
    /// Liest die beiden eingebauten Figuren (je 25 Byte, zeilenweise) aus em.exe, Grafiksegment 0BBF wie
    /// <see cref="BitmapFont.LoadFromExe"/>. Figur 0 ist eine graue Scheibe (Farben 0xC6/0xC7, der Puck im Video), Figur 1
    /// eine Scheibe in Farbe 0xFB.
    /// </summary>
    public static byte[][] LoadBuiltIn(string exePath)
    {
        var exe = File.ReadAllBytes(exePath);
        if (exe.Length < 0x20 || exe[0] != 'M' || exe[1] != 'Z')
            throw new InvalidDataException("Keine gültige DOS-EXE.");
        var segment = BinaryPrimitives.ReadUInt16LittleEndian(exe.AsSpan(8)) * 16 + BitmapFont.GraphicsSegment * 16;
        const int bytes = BuiltInSize * BuiltInSize;
        return [exe.AsSpan(segment + BuiltInOffset0, bytes).ToArray(), exe.AsSpan(segment + BuiltInOffset1, bytes).ToArray()];
    }
}
