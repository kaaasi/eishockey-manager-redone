using System.Buffers.Binary;

namespace EishockeyManager.Formats;

/// <summary>Blocktypen des Creative-Voice-Formats.</summary>
public enum VocBlockType : byte
{
    Terminator = 0,
    SoundData = 1,
    SoundContinue = 2,
    Silence = 3,
    Marker = 4,
    Text = 5,
    RepeatStart = 6,
    RepeatEnd = 7,
    Extended = 8,
    SoundDataNew = 9,
}

/// <summary>Ein Block einer VOC-Datei; <see cref="Body"/> ohne den 4-Byte-Blockkopf.</summary>
public sealed record VocBlock(VocBlockType Type, int Offset, byte[] Body);

/// <summary>
/// Creative Voice File (VOC), im Spiel kb\237–241. Aufbau:
/// Kopf „Creative Voice File“ + 0x1A, u16 Kopflänge, u16 Version, u16 Prüfwert (= ~Version + 0x1234),
/// danach Blöcke: u8 Typ, u24 Länge, Daten. Unterstützt werden
/// <list type="bullet">
/// <item>1 Tondaten: u8 Zeitkonstante TC (Rate = 10⁶ / (256 − TC)), u8 Packverfahren (0 = 8 Bit unsigned), Daten</item>
/// <item>2 Fortsetzung mit der Rate des vorigen Tonblocks</item>
/// <item>3 Stille: u16 Länge − 1, u8 TC</item>
/// <item>4 Marke, 5 Text (werden übersprungen)</item>
/// <item>6 Wiederholungsbeginn: u16 Anzahl (Abspielungen = Anzahl + 1, 0xFFFF = endlos), 7 Wiederholungsende</item>
/// <item>8 Erweiterte Angaben für den folgenden Block 1: u16 TC (Rate = 256·10⁶ / (Kanäle · (65536 − TC))), u8 Packverfahren, u8 Stereo</item>
/// <item>9 Tondaten neu: u32 Rate, u8 Bits, u8 Kanäle, u16 Codec (0 = 8 Bit unsigned, 4 = 16 Bit signed), 4 Byte frei</item>
/// </list>
/// Komprimierte Varianten (Creative ADPCM, A-/µ-Law) kommen im Spiel nicht vor und werden abgelehnt.
/// </summary>
public sealed class VocFile
{
    public static ReadOnlySpan<byte> Signature => "Creative Voice File\x1A"u8;

    /// <summary>Anzahl für Block 6, die eine endlose Wiederholung bedeutet.</summary>
    public const int EndlessRepeat = 0xFFFF;

    private VocFile(ushort version, IReadOnlyList<VocBlock> blocks)
    {
        Version = version;
        Blocks = blocks;
    }

    public ushort Version { get; }
    public IReadOnlyList<VocBlock> Blocks { get; }

    public static bool IsVoc(ReadOnlySpan<byte> data) =>
        data.Length >= 26 && data.StartsWith(Signature);

    public static VocFile Load(string path) => Parse(File.ReadAllBytes(path));

    public static bool TryParse(ReadOnlySpan<byte> data, out VocFile? voc)
    {
        try
        {
            voc = Parse(data);
            return true;
        }
        catch (InvalidDataException)
        {
            voc = null;
            return false;
        }
    }

    public static VocFile Parse(ReadOnlySpan<byte> data)
    {
        if (!IsVoc(data))
            throw new InvalidDataException("Keine VOC-Datei (Kopf „Creative Voice File“ fehlt).");

        int offset = BinaryPrimitives.ReadUInt16LittleEndian(data[20..]);
        var version = BinaryPrimitives.ReadUInt16LittleEndian(data[22..]);
        if (offset < 26 || offset > data.Length)
            throw new InvalidDataException($"VOC: ungültige Kopflänge {offset}.");

        var blocks = new List<VocBlock>();
        while (offset < data.Length)
        {
            var type = (VocBlockType)data[offset];
            if (type == VocBlockType.Terminator)
                break;
            if (offset + 4 > data.Length)
                throw new InvalidDataException($"VOC: Blockkopf bei {offset} abgeschnitten.");
            var length = data[offset + 1] | data[offset + 2] << 8 | data[offset + 3] << 16;
            if (offset + 4 + length > data.Length)
                throw new InvalidDataException($"VOC: Block Typ {(int)type} bei {offset} ist länger als die Datei.");
            blocks.Add(new VocBlock(type, offset, data.Slice(offset + 4, length).ToArray()));
            offset += 4 + length;
        }
        return new VocFile(version, blocks);
    }

    /// <summary>
    /// Dekodiert die Datei zu Mono-PCM. Endliche Wiederholungen werden ausgeschrieben; die erste endlose
    /// Wiederholung (Anzahl 0xFFFF) wird einmal ausgeschrieben und als Schleife vermerkt
    /// (<see cref="PcmSound.LoopStart"/>/<see cref="PcmSound.LoopLength"/>). Haben Blöcke verschiedene Raten,
    /// wird auf die Rate des ersten Tonblocks umgerechnet (linear).
    /// </summary>
    public PcmSound Decode()
    {
        var segments = new List<Segment>();
        int? currentRate = null;
        var bits = 8;
        var currentChannels = 1;
        var lastWasSixteen = false;
        Extended? pendingExtended = null;
        var stack = new Stack<(int BlockIndex, int Remaining, int SegmentIndex)>();
        (int Start, int End)? endless = null; // Segmentindizes [Start, End)

        // Schutz gegen fehlerhafte Dateien: endliche Wiederholungen dürfen nicht beliebig wachsen.
        var guard = 0;

        for (var i = 0; i < Blocks.Count; i++)
        {
            if (++guard > 1_000_000)
                throw new InvalidDataException("VOC: zu viele Wiederholungen.");

            var block = Blocks[i];
            var body = block.Body;
            switch (block.Type)
            {
                case VocBlockType.SoundData:
                {
                    Need(block, 2);
                    int rate;
                    int channels = 1;
                    int pack = body[1];
                    if (pendingExtended is { } ext)
                    {
                        rate = ext.Rate;
                        channels = ext.Channels;
                        pack = ext.Pack;
                        pendingExtended = null;
                    }
                    else
                        rate = RateFromTimeConstant(body[0]);
                    if (pack != 0)
                        throw new NotSupportedException($"VOC: Packverfahren {pack} (komprimiert) wird nicht unterstützt.");
                    currentRate = rate;
                    currentChannels = channels;
                    segments.Add(Segment.FromUnsigned8(rate, body.AsSpan(2), channels));
                    break;
                }
                case VocBlockType.SoundContinue:
                    if (currentRate is not { } contRate)
                        throw new InvalidDataException($"VOC: Fortsetzungsblock bei {block.Offset} ohne vorherigen Tonblock.");
                    segments.Add(lastWasSixteen
                        ? Segment.FromSigned16(contRate, body, currentChannels)
                        : Segment.FromUnsigned8(contRate, body, currentChannels));
                    break;
                case VocBlockType.Silence:
                {
                    Need(block, 3);
                    var count = BinaryPrimitives.ReadUInt16LittleEndian(body) + 1;
                    segments.Add(Segment.Silence(RateFromTimeConstant(body[2]), count));
                    break;
                }
                case VocBlockType.Marker:
                case VocBlockType.Text:
                    break;
                case VocBlockType.RepeatStart:
                {
                    Need(block, 2);
                    var count = BinaryPrimitives.ReadUInt16LittleEndian(body);
                    stack.Push((i, count == EndlessRepeat ? -1 : count, segments.Count));
                    break;
                }
                case VocBlockType.RepeatEnd:
                {
                    if (stack.Count == 0)
                        break; // Ende ohne Beginn: wie im Creative-Treiber ignorieren
                    var (start, remaining, segmentIndex) = stack.Pop();
                    if (remaining < 0)
                    {
                        endless ??= (segmentIndex, segments.Count);
                    }
                    else if (remaining > 0)
                    {
                        stack.Push((start, remaining - 1, segmentIndex));
                        i = start; // Schleife nach Block 6 erneut beginnen
                    }
                    break;
                }
                case VocBlockType.Extended:
                {
                    Need(block, 4);
                    var tc = BinaryPrimitives.ReadUInt16LittleEndian(body);
                    var channels = body[3] == 0 ? 1 : 2;
                    var rate = (int)Math.Round(256_000_000.0 / (channels * (65536 - tc)));
                    pendingExtended = new Extended(rate, body[2], channels);
                    break;
                }
                case VocBlockType.SoundDataNew:
                {
                    Need(block, 12);
                    var rate = (int)BinaryPrimitives.ReadUInt32LittleEndian(body);
                    int sampleBits = body[4];
                    int channels = Math.Max((int)body[5], 1);
                    var codec = BinaryPrimitives.ReadUInt16LittleEndian(body[6..]);
                    if (rate <= 0)
                        throw new InvalidDataException($"VOC: Rate {rate} bei {block.Offset}.");
                    var samples = body.AsSpan(12);
                    if (codec == 0 && sampleBits == 8)
                    {
                        segments.Add(Segment.FromUnsigned8(rate, samples, channels));
                        lastWasSixteen = false;
                    }
                    else if (codec == 4 && sampleBits == 16)
                    {
                        segments.Add(Segment.FromSigned16(rate, samples, channels));
                        lastWasSixteen = true;
                        bits = 16;
                    }
                    else
                        throw new NotSupportedException($"VOC: Codec {codec} mit {sampleBits} Bit wird nicht unterstützt.");
                    currentRate = rate;
                    currentChannels = channels;
                    break;
                }
                default:
                    break; // unbekannte Blöcke überspringen
            }

            if (block.Type == VocBlockType.SoundData)
                lastWasSixteen = false;
        }

        if (segments.Count == 0)
            return new PcmSound(currentRate ?? 11025, [], SourceBits: bits);

        var outputRate = (segments.FirstOrDefault(s => !s.IsSilence) ?? segments[0]).Rate;
        var parts = segments.Select(s => s.Resample(outputRate)).ToList();
        var samplesOut = parts.SelectMany(p => p).ToArray();

        int loopStart = 0, loopLength = 0;
        if (endless is { } loop)
        {
            loopStart = parts.Take(loop.Start).Sum(p => p.Length);
            loopLength = parts.Skip(loop.Start).Take(loop.End - loop.Start).Sum(p => p.Length);
        }
        return new PcmSound(outputRate, samplesOut, loopStart, loopLength, bits);
    }

    /// <summary>Rate eines Blocks 1 oder 3: 10⁶ / (256 − TC), gerundet.</summary>
    public static int RateFromTimeConstant(byte timeConstant) =>
        (int)Math.Round(1_000_000.0 / (256 - timeConstant));

    private static void Need(VocBlock block, int length)
    {
        if (block.Body.Length < length)
            throw new InvalidDataException($"VOC: Block Typ {(int)block.Type} bei {block.Offset} zu kurz.");
    }

    private readonly record struct Extended(int Rate, int Pack, int Channels);

    private sealed class Segment
    {
        private Segment(int rate, short[] samples, bool isSilence)
        {
            Rate = rate;
            Samples = samples;
            IsSilence = isSilence;
        }

        public int Rate { get; }
        public short[] Samples { get; }
        public bool IsSilence { get; }

        public static Segment FromUnsigned8(int rate, ReadOnlySpan<byte> data, int channels)
        {
            var frames = data.Length / channels;
            var samples = new short[frames];
            for (var f = 0; f < frames; f++)
            {
                var sum = 0;
                for (var c = 0; c < channels; c++)
                    sum += data[f * channels + c] - 128;
                samples[f] = (short)(sum * 256 / channels);
            }
            return new Segment(rate, samples, false);
        }

        public static Segment FromSigned16(int rate, ReadOnlySpan<byte> data, int channels)
        {
            var frames = data.Length / (2 * channels);
            var samples = new short[frames];
            for (var f = 0; f < frames; f++)
            {
                var sum = 0;
                for (var c = 0; c < channels; c++)
                    sum += BinaryPrimitives.ReadInt16LittleEndian(data[((f * channels + c) * 2)..]);
                samples[f] = (short)(sum / channels);
            }
            return new Segment(rate, samples, false);
        }

        public static Segment Silence(int rate, int count) => new(rate, new short[count], true);

        public short[] Resample(int targetRate)
        {
            if (targetRate == Rate || Samples.Length == 0)
                return Samples;
            var length = (int)Math.Round((double)Samples.Length * targetRate / Rate);
            var result = new short[length];
            if (IsSilence)
                return result;
            var step = (double)Rate / targetRate;
            for (var i = 0; i < length; i++)
            {
                var pos = i * step;
                var index = (int)pos;
                var next = Math.Min(index + 1, Samples.Length - 1);
                var frac = pos - index;
                result[i] = (short)Math.Round(Samples[Math.Min(index, Samples.Length - 1)] * (1 - frac) + Samples[next] * frac);
            }
            return result;
        }
    }
}
