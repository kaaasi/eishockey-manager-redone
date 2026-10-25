using EishockeyManager.Formats;

namespace EishockeyManager.Formats.Tests;

public class VgaImageTests
{
    private static VgaImage CreateTestImage(int width, int height, bool withPalette)
    {
        var pixels = new byte[width * height];
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
                pixels[y * width + x] = (byte)((x / 8 + y / 8) % 16);

        Palette? palette = null;
        if (withPalette)
        {
            var rgb = new byte[Palette.StoredLength];
            for (var i = 0; i < rgb.Length; i++)
                rgb[i] = (byte)(i * 4);
            palette = Palette.FromBytes(rgb);
        }
        return new VgaImage(width, height, pixels, palette);
    }

    [Fact]
    public void Parse_SmallImage_RoundTrip()
    {
        var original = CreateTestImage(38, 38, withPalette: false);
        var parsed = VgaImage.Parse(original.ToBytes());

        Assert.Equal(38, parsed.Width);
        Assert.Equal(38, parsed.Height);
        Assert.Equal(original.Pixels, parsed.Pixels);
        Assert.Null(parsed.Palette);
    }

    [Fact]
    public void Parse_FullScreenWithPalette_UsesTwoStreams()
    {
        // 320×240 ist größer als 200 Zeilen → zwei PackBits-Ströme wie im Original
        var original = CreateTestImage(320, 240, withPalette: true);
        var bytes = original.ToBytes();
        var parsed = VgaImage.Parse(bytes);

        Assert.Equal(original.Pixels, parsed.Pixels);
        Assert.NotNull(parsed.Palette);
        Assert.Equal(original.Palette![5], parsed.Palette![5]);
    }

    [Fact]
    public void Parse_HeaderOnlyGarbage_Throws()
    {
        byte[] garbage = [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
        Assert.False(VgaImage.TryParse(garbage, out _));
    }

    [Fact]
    public void Parse_KnownLayout()
    {
        // 4×2 Bild: Zeile 1 = 1,2,3,4 · Zeile 2 = 4× Farbe 9
        byte[] data = [4, 0, 2, 0, 8, 0, 0x03, 1, 2, 3, 4, 0x83, 9, 0x80];
        var image = VgaImage.Parse(data);

        Assert.Equal(1, image[0, 0]);
        Assert.Equal(4, image[3, 0]);
        Assert.Equal(9, image[2, 1]);
    }

    [Fact]
    public void PngWriter_WritesValidSignature()
    {
        var image = CreateTestImage(16, 16, withPalette: false);
        using var stream = new MemoryStream();
        IndexedPngWriter.Write(stream, image, Palette.Grayscale());

        var bytes = stream.ToArray();
        Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, bytes[..4]);
        Assert.True(bytes.Length > 50);
    }
}
