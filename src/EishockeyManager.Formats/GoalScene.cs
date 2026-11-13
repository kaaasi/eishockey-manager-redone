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
