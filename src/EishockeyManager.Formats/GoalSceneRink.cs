using System.Buffers.Binary;

namespace EishockeyManager.Formats;

/// <summary>
/// Eisfläche der Torszene (Spur torszenen, Teil C1; <c>docs/analyse/re-livespiel.md</c> §16): Kachelkarte der Halle aus
/// <c>kb\236</c> bzw. <c>kb\242 … 245</c> mit den Kacheln aus <c>kb\231</c>/<c>kb\232</c>, dazu Kamera und Sichtfenster.
/// <para>[ORIGINAL] Grafiksegment 0BBF (Fernaufrufe <c>48h:…</c>, Datei 0xD9F0 + Offset):</para>
/// <list type="bullet">
/// <item>Karte: <c>cs:0x50</c> (Bild i = 5 des Abspielers), 42 Spalten zu je 72 Big-Endian-Wörtern, spaltenweise
///   (Datei 0x124CD–0x124E9: Index (x / 16) · 0x48 + y / 16, Wort; nächste Spalte + 0x90 Byte, Datei 0x124A4).</item>
/// <item>Wort (Datei 0x122BF–0x12365): Bit 15 = Kachel senkrecht gespiegelt (Zeilen von unten, Quelle + 0xF0, je Zeile − 0x10),
///   Rest n &lt; 250 → <c>cs:0x3C</c> = <c>kb\231</c> Kachel n, sonst <c>cs:0x40</c> = <c>kb\232</c> Kachel n − 250; je Kachel
///   16 × 16 Byte (Index · 0x100).</item>
/// <item>Kamera (Datei 0x127B7–0x12869): x 0 … 0x160, y 0 … 0x3B8, Sichtfenster 0x140 × 0xC8 = 320 × 200; Eisfläche damit
///   672 × 1 152 = 42 × 72 Kacheln.</item>
/// </list>
/// </summary>
public sealed class GoalSceneRink
{
    /// <summary>[ORIGINAL] Kantenlänge einer Kachel.</summary>
    public const int TileSize = 16;

    /// <summary>[ORIGINAL] Spalten und Zeilen der Kachelkarte (Datei 0x124D5: 0x48 Zeilen je Spalte).</summary>
    public const int Columns = 42, Rows = 0x48;

    /// <summary>[ORIGINAL] Kacheln je Kacheldatei (Datei 0x122C8: <c>cmp cx,0xFA</c>).</summary>
    public const int TilesPerGraphic = 0xFA;

    /// <summary>[ORIGINAL] Sichtfenster (Datei 0x12800: + 0x140, Datei 0x12865: + 0xC8).</summary>
    public const int ViewWidth = 0x140, ViewHeight = 0xC8;

    /// <summary>[ORIGINAL] Größte Kameraposition (Datei 0x127E7, 0x12845).</summary>
    public const int MaxCameraX = 0x160, MaxCameraY = 0x3B8;

    public const int Width = Columns * TileSize, Height = Rows * TileSize;

    /// <summary>
    /// [ORIGINAL] Versatz der Figuren: ein Objekt bei (x | y) wird mit der linken oberen Ecke bei (x − 0x50 | y − 0x60) der
    /// Eisfläche gezeichnet (Datei 0x128CB–0x128D4).
    /// </summary>
    public const int FigureOffsetX = 0x50, FigureOffsetY = 0x60;

    /// <summary>[ORIGINAL] Startkamera aus dem Szenenkopf: x − 16, y − 32, jeweils ≥ 0 (<c>48h:5250h</c>, Datei 0x12C62–0x12C85).</summary>
    public const int StartOffsetX = 0x10, StartOffsetY = 0x20;

    private GoalSceneRink(int hallLevel, VgaImage image)
    {
        HallLevel = hallLevel;
        Image = image;
    }

    /// <summary>Hallenstufe 0 … 4 (Füllgrad der Halle, siehe <see cref="MapGraphic"/>).</summary>
    public int HallLevel { get; }

    /// <summary>Die ganze Eisfläche (672 × 1 152 Palettenindizes, Palette <c>kb\247</c>).</summary>
    public VgaImage Image { get; }

    /// <summary>
    /// [ORIGINAL] Kachelkarte zur Hallenstufe (Datei 0x568C2–0x568DE): Bild i = 5 ist <c>kb\236</c>, bei Stufe 1 … 4 ersetzt
    /// durch <c>kb\(0xF1 + Stufe)</c> = <c>kb\242 … 245</c>. Die Stufe ist das Kennbyte <c>DGROUP 0x5D99 + 6 · m</c>
    /// (Datei 0x53F10), das die Spielabrechnung als ⌊4 · Zuschauer / Plätze⌋ ablegt, ohne Plätze 2 (Datei 0x54AE0–0x54B24);
    /// ohne Manager oder bei mehreren Managern im Dauer-Eisfeld gilt 2 (Datei 0x53F55, 0x568A9–0x568B6). Die fünf Karten
    /// unterscheiden sich in der Füllung der Ränge und in einzelnen Eiskacheln (Linien und Kreise gleich).
    /// </summary>
    public static int MapGraphic(int hallLevel) => hallLevel is >= 1 and <= 4 ? 0xF1 + hallLevel : 236;

    /// <summary>Lädt Karte, Kacheln und Palette aus den Originaldateien.</summary>
    public static GoalSceneRink Load(GameData data, int hallLevel) =>
        Build(hallLevel, File.ReadAllBytes(data.GraphicsPath(MapGraphic(hallLevel))),
            PackedData.Load(data.GraphicsPath(231)), PackedData.Load(data.GraphicsPath(232)),
            Palette.Load(data.GraphicsPath(247)));

    /// <summary>Setzt die Eisfläche aus Karte (Rohdaten, 6 048 Byte) und den entpackten Kacheln zusammen.</summary>
    public static GoalSceneRink Build(int hallLevel, ReadOnlySpan<byte> map, ReadOnlySpan<byte> tiles231,
        ReadOnlySpan<byte> tiles232, Palette? palette = null)
    {
        if (map.Length < Columns * Rows * 2)
            throw new InvalidDataException("Kachelkarte der Torszene zu kurz.");
        const int tileBytes = TileSize * TileSize;
        var pixels = new byte[Width * Height];
        for (var c = 0; c < Columns; c++)
        {
            for (var r = 0; r < Rows; r++)
            {
                var word = BinaryPrimitives.ReadUInt16BigEndian(map[(2 * (c * Rows + r))..]);
                var n = word & 0x7FFF;
                var source = n < TilesPerGraphic ? tiles231 : tiles232;
                var tile = n < TilesPerGraphic ? n : n - TilesPerGraphic;
                if ((tile + 1) * tileBytes > source.Length)
                    throw new InvalidDataException($"Kachel {n} fehlt.");
                var flip = (word & 0x8000) != 0;
                for (var y = 0; y < TileSize; y++)
                {
                    var sourceRow = flip ? TileSize - 1 - y : y;
                    source.Slice(tile * tileBytes + sourceRow * TileSize, TileSize)
                        .CopyTo(pixels.AsSpan((r * TileSize + y) * Width + c * TileSize));
                }
            }
        }
        return new GoalSceneRink(hallLevel, new VgaImage(Width, Height, pixels, palette));
    }

    /// <summary>[ORIGINAL] Kopfbild der Torszene <c>kb\246</c> (Bild i = 12, roh geladen): 320 × 40.</summary>
    public const int HeaderGraphic = 246, HeaderWidth = 320, HeaderHeight = 40;

    /// <summary>[ORIGINAL] Palette der Torszene <c>kb\247</c> (Bild i = 13, 255 Farben, Datei 0x56B12–0x56B2C).</summary>
    public const int PaletteGraphic = 247;

    /// <summary>
    /// [ORIGINAL] Kopfbild entpacken wie <c>48h:54BFh</c> (Datei 0x12EAF–0x12F48): Steuerbyte b, Bit 7 frei → b + 1 Byte
    /// übernehmen, sonst (b &amp; 0x7F) + 1 Mal das folgende Byte, 0x80 = Ende; Pixel zeilenweise ab Bildschirmspeicher 0
    /// (80 Byte je Ebenenzeile, dann + 4 = Zeilenlänge 0x54). Das ist PackBits (<see cref="PackBits"/>).
    /// </summary>
    public static VgaImage DecodeHeader(ReadOnlySpan<byte> data)
    {
        var pixels = new byte[HeaderWidth * HeaderHeight];
        var position = 0;
        var written = PackBits.Decode(data, ref position, pixels);
        if (written != pixels.Length)
            throw new InvalidDataException($"Kopfbild der Torszene: {written} statt {pixels.Length} Pixel.");
        return new VgaImage(HeaderWidth, HeaderHeight, pixels);
    }

    /// <summary>[ORIGINAL] Startkamera einer Szene (Kopf +0x04/+0x06).</summary>
    public static (int X, int Y) StartCamera(GoalScene scene) =>
        (Math.Max(0, scene.CameraX - StartOffsetX), Math.Max(0, scene.CameraY - StartOffsetY));

    /// <summary>
    /// [ORIGINAL] Kamera um eine Bewegung (Bewegungsbyte des Bildes, <see cref="GoalScene.DecodeMove"/>) verschieben und auf
    /// 0 … <see cref="MaxCameraX"/> bzw. 0 … <see cref="MaxCameraY"/> begrenzen (Datei 0x127B7–0x12869; die Bewegung gilt nur für
    /// dieses Bild, <c>48h:5316h</c> setzt sie je Bild neu, Datei 0x12D2B).
    /// </summary>
    public static (int X, int Y) MoveCamera((int X, int Y) camera, byte move)
    {
        var (dx, dy) = GoalScene.DecodeMove(move);
        return (Math.Clamp(camera.X + dx, 0, MaxCameraX), Math.Clamp(camera.Y + dy, 0, MaxCameraY));
    }

    /// <summary>Ausschnitt der Eisfläche im Sichtfenster (320 × 200) bei der Kamera.</summary>
    public VgaImage View((int X, int Y) camera)
    {
        var x = Math.Clamp(camera.X, 0, MaxCameraX);
        var y = Math.Clamp(camera.Y, 0, MaxCameraY);
        var pixels = new byte[ViewWidth * ViewHeight];
        for (var row = 0; row < ViewHeight; row++)
            Image.Pixels.AsSpan((y + row) * Width + x, ViewWidth).CopyTo(pixels.AsSpan(row * ViewWidth));
        return new VgaImage(ViewWidth, ViewHeight, pixels, Image.Palette);
    }
}
