using EishockeyManager.Tests.Support;
using System.Buffers.Binary;
using EishockeyManager.Formats;

namespace EishockeyManager.Formats.Tests;

/// <summary>Baut VOC-Dateien für die Tests zusammen.</summary>
internal sealed class VocBuilder
{
    private readonly List<byte> _bytes = [.. "Creative Voice File\x1A"u8, 0x1A, 0x00, 0x0A, 0x01, 0x29, 0x11];

    public VocBuilder Block(VocBlockType type, params byte[] body)
    {
        _bytes.Add((byte)type);
        _bytes.Add((byte)body.Length);
        _bytes.Add((byte)(body.Length >> 8));
        _bytes.Add((byte)(body.Length >> 16));
        _bytes.AddRange(body);
        return this;
    }

    public VocBuilder Sound(byte timeConstant, params byte[] samples) =>
        Block(VocBlockType.SoundData, [timeConstant, 0, .. samples]);

    public VocBuilder Continue(params byte[] samples) => Block(VocBlockType.SoundContinue, samples);

    public VocBuilder Silence(int count, byte timeConstant) =>
        Block(VocBlockType.Silence, (byte)(count - 1), (byte)((count - 1) >> 8), timeConstant);

    public VocBuilder Repeat(int count) => Block(VocBlockType.RepeatStart, (byte)count, (byte)(count >> 8));

    public VocBuilder EndRepeat() => Block(VocBlockType.RepeatEnd);

    public byte[] Build(bool terminator = true) => terminator ? [.. _bytes, 0] : [.. _bytes];
}

public class VocFileTests
{
    // TC 156 → 10⁶ / 100 = 10 000 Hz; TC 206 → 20 000 Hz
    private const byte Tc10k = 156;
    private const byte Tc20k = 206;

    private static short S8(byte b) => (short)((b - 128) * 256);

    [Fact]
    public void Parse_RejectsMissingSignature()
    {
        Assert.False(VocFile.TryParse(new byte[40], out _));
        Assert.Throws<InvalidDataException>(() => VocFile.Parse("RIFF....WAVEfmt "u8));
    }

    [Fact]
    public void Parse_ReadsHeaderAndStopsAtTerminator()
    {
        var data = new VocBuilder().Sound(Tc10k, 1, 2).Build();
        data = [.. data, 0x01, 0x05, 0x00, 0x00]; // Müll hinter dem Endeblock
        var voc = VocFile.Parse(data);
        Assert.Equal(0x010A, voc.Version);
        Assert.Single(voc.Blocks);
        Assert.Equal(VocBlockType.SoundData, voc.Blocks[0].Type);
        Assert.Equal(26, voc.Blocks[0].Offset);
    }

    [Fact]
    public void Parse_AcceptsMissingTerminator() =>
        Assert.Single(VocFile.Parse(new VocBuilder().Sound(Tc10k, 1).Build(terminator: false)).Blocks);

    [Fact]
    public void Parse_RejectsTruncatedBlock()
    {
        var data = new VocBuilder().Sound(Tc10k, 1, 2, 3, 4).Build(terminator: false);
        Assert.Throws<InvalidDataException>(() => VocFile.Parse(data.AsSpan(0, data.Length - 2)));
    }

    [Theory]
    [InlineData(42, 4673)]   // kb\237, kb\238
    [InlineData(101, 6452)]  // kb\240, kb\241
    [InlineData(172, 11905)] // kb\239
    [InlineData(156, 10000)]
    public void RateFromTimeConstant(byte tc, int rate) => Assert.Equal(rate, VocFile.RateFromTimeConstant(tc));

    [Fact]
    public void Type1_Unsigned8BitToSigned16()
    {
        var sound = VocFile.Parse(new VocBuilder().Sound(Tc10k, 0x80, 0xFF, 0x00, 0x40).Build()).Decode();
        Assert.Equal(10000, sound.SampleRate);
        Assert.Equal(8, sound.SourceBits);
        Assert.Equal(new short[] { 0, 32512, -32768, -16384 }, sound.Samples);
        Assert.False(sound.HasLoop);
    }

    [Fact]
    public void Type2_ContinuesWithPreviousRate()
    {
        var sound = VocFile.Parse(new VocBuilder().Sound(Tc10k, 0x90).Continue(0xA0, 0xB0).Build()).Decode();
        Assert.Equal(10000, sound.SampleRate);
        Assert.Equal(new[] { S8(0x90), S8(0xA0), S8(0xB0) }, sound.Samples);
    }

    [Fact]
    public void Type2_WithoutSoundBlockIsInvalid() =>
        Assert.Throws<InvalidDataException>(() => VocFile.Parse(new VocBuilder().Continue(1, 2).Build()).Decode());

    [Fact]
    public void Type3_SilenceLengthIsCountPlusOne()
    {
        var sound = VocFile.Parse(new VocBuilder().Sound(Tc10k, 0xC0).Silence(5, Tc10k).Sound(Tc10k, 0xC0).Build()).Decode();
        Assert.Equal(7, sound.Samples.Length);
        Assert.All(sound.Samples[1..6], s => Assert.Equal(0, s));
        Assert.Equal(S8(0xC0), sound.Samples[6]);
    }

    [Fact]
    public void Type3_SilenceAtOtherRateKeepsDuration()
    {
        // 100 Abtastwerte Stille bei 20 kHz = 5 ms = 50 Werte bei 10 kHz
        var sound = VocFile.Parse(new VocBuilder().Sound(Tc10k, 0xC0).Silence(100, Tc20k).Build()).Decode();
        Assert.Equal(10000, sound.SampleRate);
        Assert.Equal(51, sound.Samples.Length);
    }

    [Fact]
    public void DifferentRates_AreResampledToFirstRate()
    {
        var samples = Enumerable.Repeat((byte)0xC0, 40).ToArray();
        var sound = VocFile.Parse(new VocBuilder().Sound(Tc10k, 0x80).Sound(Tc20k, samples).Build()).Decode();
        Assert.Equal(10000, sound.SampleRate);
        Assert.Equal(1 + 20, sound.Samples.Length);
        Assert.All(sound.Samples[1..], s => Assert.Equal(S8(0xC0), s));
    }

    [Fact]
    public void Type6And7_FiniteRepeatPlaysCountPlusOneTimes()
    {
        var sound = VocFile.Parse(new VocBuilder()
            .Sound(Tc10k, 0x81)
            .Repeat(2).Sound(Tc10k, 0x90, 0x91).EndRepeat()
            .Sound(Tc10k, 0xF0)
            .Build()).Decode();
        Assert.Equal(new[] { S8(0x81), S8(0x90), S8(0x91), S8(0x90), S8(0x91), S8(0x90), S8(0x91), S8(0xF0) }, sound.Samples);
        Assert.False(sound.HasLoop);
    }

    [Fact]
    public void Type6And7_ZeroRepeatPlaysOnce()
    {
        var sound = VocFile.Parse(new VocBuilder().Repeat(0).Sound(Tc10k, 0x90).EndRepeat().Build()).Decode();
        Assert.Single(sound.Samples);
    }

    [Fact]
    public void Type6And7_NestedRepeats()
    {
        // außen 2×, innen 3× → A B B B A B B B
        var sound = VocFile.Parse(new VocBuilder()
            .Repeat(1).Sound(Tc10k, 0xA0)
            .Repeat(2).Sound(Tc10k, 0xB0).EndRepeat()
            .EndRepeat().Build()).Decode();
        var a = S8(0xA0);
        var b = S8(0xB0);
        Assert.Equal(new[] { a, b, b, b, a, b, b, b }, sound.Samples);
    }

    [Fact]
    public void Type6And7_EndlessRepeatIsMarkedAsLoop()
    {
        // wie kb\237: Wiederholung 0xFFFF um einen Tonblock, mit Vorspann und Nachspann
        var sound = VocFile.Parse(new VocBuilder()
            .Sound(Tc10k, 0x81, 0x82)
            .Repeat(VocFile.EndlessRepeat).Sound(Tc10k, 0x90, 0x91, 0x92).EndRepeat()
            .Sound(Tc10k, 0xF0)
            .Build()).Decode();
        Assert.Equal(6, sound.Samples.Length);
        Assert.True(sound.HasLoop);
        Assert.Equal(2, sound.LoopStart);
        Assert.Equal(3, sound.LoopLength);
    }

    [Fact]
    public void Type7_WithoutStartIsIgnored() =>
        Assert.Single(VocFile.Parse(new VocBuilder().Sound(Tc10k, 0x90).EndRepeat().Build()).Decode().Samples);

    [Fact]
    public void Type4And5_AreSkipped()
    {
        var sound = VocFile.Parse(new VocBuilder()
            .Block(VocBlockType.Text, "Hallo\0"u8.ToArray())
            .Sound(Tc10k, 0x90)
            .Block(VocBlockType.Marker, 1, 0)
            .Build()).Decode();
        Assert.Single(sound.Samples);
    }

    [Fact]
    public void Type8_StereoOverridesFollowingSoundBlock()
    {
        // 22 050 Hz Stereo: TC = 65536 − 256·10⁶ / (2 · 22050) = 59731
        var voc = new VocBuilder()
            .Block(VocBlockType.Extended, 0x53, 0xE9, 0x00, 0x01)
            .Sound(0, 0x80, 0xC0, 0x00, 0x80) // TC im Block 1 wird überschrieben
            .Build();
        var sound = VocFile.Parse(voc).Decode();
        Assert.Equal(22050, sound.SampleRate);
        Assert.Equal(new short[] { 8192, -16384 }, sound.Samples); // Mittelwert beider Kanäle
    }

    [Fact]
    public void Type9_Signed16BitMono()
    {
        var body = new byte[12 + 4];
        BinaryPrimitives.WriteUInt32LittleEndian(body, 44100);
        body[4] = 16;
        body[5] = 1;
        body[6] = 4;
        BinaryPrimitives.WriteInt16LittleEndian(body.AsSpan(12), -1234);
        BinaryPrimitives.WriteInt16LittleEndian(body.AsSpan(14), 4321);
        var sound = VocFile.Parse(new VocBuilder().Block(VocBlockType.SoundDataNew, body).Build()).Decode();
        Assert.Equal(44100, sound.SampleRate);
        Assert.Equal(16, sound.SourceBits);
        Assert.Equal(new short[] { -1234, 4321 }, sound.Samples);
    }

    [Fact]
    public void CompressedData_IsRejected()
    {
        var voc = new VocBuilder().Block(VocBlockType.SoundData, Tc10k, 1, 0x12, 0x34).Build();
        Assert.Throws<NotSupportedException>(() => VocFile.Parse(voc).Decode());
    }

    [Fact]
    public void Empty_DecodesToNoSamples() =>
        Assert.Empty(VocFile.Parse(new VocBuilder().Build()).Decode().Samples);
}

public class WavWriterTests
{
    [Fact]
    public void Eight_Bit_HeaderAndSamples()
    {
        var bytes = WavWriter.ToBytes(new PcmSound(4673, [0, 32512, -32768], SourceBits: 8));
        Assert.Equal("RIFF"u8.ToArray(), bytes[..4]);
        Assert.Equal(bytes.Length - 8, BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(4)));
        Assert.Equal("WAVE"u8.ToArray(), bytes[8..12]);
        Assert.Equal(1, BinaryPrimitives.ReadInt16LittleEndian(bytes.AsSpan(20)));    // PCM
        Assert.Equal(1, BinaryPrimitives.ReadInt16LittleEndian(bytes.AsSpan(22)));    // Mono
        Assert.Equal(4673, BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(24)));
        Assert.Equal(8, BinaryPrimitives.ReadInt16LittleEndian(bytes.AsSpan(34)));
        Assert.Equal("data"u8.ToArray(), bytes[36..40]);
        Assert.Equal(3, BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(40)));
        Assert.Equal(new byte[] { 0x80, 0xFF, 0x00 }, bytes[44..47]);
        Assert.Equal(48, bytes.Length); // Füllbyte bei ungerader Datenlänge
    }

    [Fact]
    public void SixteenBit_AndLoopChunk()
    {
        var bytes = WavWriter.ToBytes(new PcmSound(22050, [1, -2, 3, -4], LoopStart: 1, LoopLength: 2));
        Assert.Equal(16, BinaryPrimitives.ReadInt16LittleEndian(bytes.AsSpan(34)));
        Assert.Equal(-2, BinaryPrimitives.ReadInt16LittleEndian(bytes.AsSpan(46)));
        var smpl = bytes.AsSpan(44 + 8);
        Assert.Equal("smpl"u8.ToArray(), smpl[..4].ToArray());
        Assert.Equal(1, BinaryPrimitives.ReadInt32LittleEndian(smpl[(8 + 28)..]));
        Assert.Equal(1, BinaryPrimitives.ReadInt32LittleEndian(smpl[(8 + 36 + 8)..]));
        Assert.Equal(2, BinaryPrimitives.ReadInt32LittleEndian(smpl[(8 + 36 + 12)..]));
        Assert.Equal(bytes.Length - 8, BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(4)));
    }
}

/// <summary>Die Sounddateien des Originals (übersprungen ohne „gamedata“).</summary>
public class VocOriginalTests
{
    [GameDataTheory]
    [InlineData(237, 4673, 9146, true)]
    [InlineData(238, 4673, 4910, false)]
    [InlineData(239, 11905, 9194, false)]
    [InlineData(240, 6452, 906, false)]
    [InlineData(241, 6452, 2574, false)]
    public void OriginalSounds_Decode(int number, int rate, int samples, bool loops)
    {
        var game = TestGameData.Load();

        var sound = VocFile.Load(game.GraphicsPath(number)).Decode();
        Assert.Equal(rate, sound.SampleRate);
        Assert.Equal(samples, sound.Samples.Length);
        Assert.Equal(loops, sound.HasLoop);
        if (loops)
            Assert.Equal((0, samples), (sound.LoopStart, sound.LoopLength));
    }
}
