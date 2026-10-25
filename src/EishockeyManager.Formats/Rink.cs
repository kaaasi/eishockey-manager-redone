using System.Buffers.Binary;

namespace EishockeyManager.Formats;

/// <summary>
/// Kacheln der Eishalle für die Torszenen: kb\231 und kb\232 enthalten je 250 Kacheln
/// zu 16×16 Pixeln. In der Datei liegt jede Kachel <b>spaltenweise</b> (Pixel (x, y) an x·16 + y);
/// diese Klasse stellt sie zeilenweise bereit.
/// </summary>
public sealed class RinkTileSet
{
    public const int TileSize = 16;
    public const int BytesPerTile = TileSize * TileSize;

    private readonly byte[] _rowMajor;

    public RinkTileSet(params byte[][] columnMajorParts)
    {
        var total = columnMajorParts.Sum(p => p.Length);
        if (total % BytesPerTile != 0)
            throw new InvalidDataException("Kacheldaten sind kein Vielfaches von 256 Bytes.");

        _rowMajor = new byte[total];
        var offset = 0;
        foreach (var part in columnMajorParts)
        {
            for (var t = 0; t < part.Length / BytesPerTile; t++)
            {
                var src = part.AsSpan(t * BytesPerTile, BytesPerTile);
                var dst = _rowMajor.AsSpan(offset + t * BytesPerTile, BytesPerTile);
                for (var x = 0; x < TileSize; x++)
                    for (var y = 0; y < TileSize; y++)
                        dst[y * TileSize + x] = src[x * TileSize + y];
            }
            offset += part.Length;
        }
    }

    public int Count => _rowMajor.Length / BytesPerTile;

    /// <summary>Pixel einer Kachel, zeilenweise.</summary>
    public ReadOnlySpan<byte> this[int index] => _rowMajor.AsSpan(index * BytesPerTile, BytesPerTile);

    public static RinkTileSet Load(GameData game) =>
        new(PackedData.Load(game.GraphicsPath(231)), PackedData.Load(game.GraphicsPath(232)));
}

/// <summary>
/// Karte der Eishalle (kb\236, kb\242–245): 72 × 42 Einträge, UInt16 <b>Big Endian</b>.
/// Bit 0–14 = Kachelnummer (0–499, beide Kachelsätze hintereinander), Bit 15 = waagerecht gespiegelt.
/// </summary>
public sealed class RinkMap
{
    public const int Columns = 72;
    public const int Rows = 42;
    public const int ByteLength = Columns * Rows * 2;

    private readonly ushort[] _cells;

    private RinkMap(ushort[] cells) => _cells = cells;

    public (int Tile, bool Mirrored) this[int column, int row]
    {
        get
        {
            var value = _cells[row * Columns + column];
            return (value & 0x7FFF, (value & 0x8000) != 0);
        }
    }

    public static RinkMap Parse(ReadOnlySpan<byte> data)
    {
        if (data.Length < ByteLength)
            throw new InvalidDataException($"Hallenkarte zu kurz ({data.Length} statt {ByteLength} Bytes).");
        var cells = new ushort[Columns * Rows];
        for (var i = 0; i < cells.Length; i++)
            cells[i] = BinaryPrimitives.ReadUInt16BigEndian(data[(i * 2)..]);
        return new RinkMap(cells);
    }

    public static RinkMap Load(string path) => Parse(File.ReadAllBytes(path));

    /// <summary>Setzt die komplette Halle (1152 × 672 Pixel) als Palettenbild zusammen.</summary>
    public VgaImage Render(RinkTileSet tiles)
    {
        const int size = RinkTileSet.TileSize;
        var width = Columns * size;
        var pixels = new byte[width * Rows * size];
        for (var row = 0; row < Rows; row++)
        {
            for (var column = 0; column < Columns; column++)
            {
                var (tile, mirrored) = this[column, row];
                if (tile >= tiles.Count)
                    continue;
                var source = tiles[tile];
                for (var y = 0; y < size; y++)
                {
                    var target = (row * size + y) * width + column * size;
                    for (var x = 0; x < size; x++)
                        pixels[target + x] = source[y * size + (mirrored ? size - 1 - x : x)];
                }
            }
        }
        return new VgaImage(width, Rows * size, pixels);
    }
}
