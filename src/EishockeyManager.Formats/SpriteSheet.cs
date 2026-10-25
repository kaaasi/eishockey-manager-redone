namespace EishockeyManager.Formats;

/// <summary>
/// Figuren der Torszenen: kb\233 (Heim, 40 Bilder), kb\234 (Gast, 40 Bilder), kb\235 (Torhüter, 12 Bilder).
/// Jedes Bild hat 32×32 Pixel, zeilenweise; Index 0 ist transparent.
/// </summary>
public sealed class SpriteSheet
{
    public const int FrameSize = 32;
    public const int BytesPerFrame = FrameSize * FrameSize;
    public const byte TransparentIndex = 0;

    private readonly byte[] _data;

    public SpriteSheet(byte[] data)
    {
        if (data.Length % BytesPerFrame != 0)
            throw new InvalidDataException("Figurendaten sind kein Vielfaches von 1024 Bytes.");
        _data = data;
    }

    public int Count => _data.Length / BytesPerFrame;

    public ReadOnlySpan<byte> this[int frame] => _data.AsSpan(frame * BytesPerFrame, BytesPerFrame);

    public static SpriteSheet Load(string path) => new(PackedData.Load(path));

    /// <summary>Alle Bilder nebeneinander in Reihen zu <paramref name="perRow"/> Stück.</summary>
    public VgaImage ToContactSheet(int perRow = 10)
    {
        var rows = (Count + perRow - 1) / perRow;
        var width = perRow * FrameSize;
        var pixels = new byte[width * rows * FrameSize];
        for (var f = 0; f < Count; f++)
        {
            var ox = f % perRow * FrameSize;
            var oy = f / perRow * FrameSize;
            var frame = this[f];
            for (var y = 0; y < FrameSize; y++)
                frame.Slice(y * FrameSize, FrameSize).CopyTo(pixels.AsSpan((oy + y) * width + ox));
        }
        return new VgaImage(width, rows * FrameSize, pixels);
    }
}
