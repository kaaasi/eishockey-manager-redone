using EishockeyManager.Formats;

namespace EishockeyManager.Formats.Tests;

public class PackBitsTests
{
    [Fact]
    public void Decode_LiteralRunAndEndMarker()
    {
        // 0x02 → 3 Literale; 0x83 → 4× Wert 0x07; 0x80 → Ende
        byte[] packed = [0x02, 0x0A, 0x0B, 0x0C, 0x83, 0x07, 0x80];
        var output = new byte[7];
        var position = 0;

        var written = PackBits.Decode(packed, ref position, output);

        Assert.Equal(7, written);
        Assert.Equal(packed.Length, position);
        Assert.Equal(new byte[] { 0x0A, 0x0B, 0x0C, 0x07, 0x07, 0x07, 0x07 }, output);
    }

    [Fact]
    public void Decode_MissingEndMarker_Throws()
    {
        byte[] packed = [0x00, 0x01];
        var position = 0;
        Assert.Throws<InvalidDataException>(() => PackBits.Decode(packed, ref position, new byte[4]));
    }

    [Fact]
    public void Decode_OutputOverflow_Throws()
    {
        byte[] packed = [0xFF, 0x01, 0x80]; // 128 Bytes in einen 10-Byte-Puffer
        var position = 0;
        Assert.Throws<InvalidDataException>(() => PackBits.Decode(packed, ref position, new byte[10]));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(127)]
    [InlineData(128)]
    [InlineData(129)]
    [InlineData(5000)]
    public void EncodeDecode_RoundTrip(int length)
    {
        var random = new Random(length);
        var data = new byte[length];
        for (var i = 0; i < length; i++)
            data[i] = random.Next(3) == 0 ? (byte)random.Next(256) : (byte)(i / 37);

        var packed = PackBits.Encode(data);
        var output = new byte[length];
        var position = 0;
        var written = PackBits.Decode(packed, ref position, output);

        Assert.Equal(length, written);
        Assert.Equal(packed.Length, position);
        Assert.Equal(data, output);
    }
}
