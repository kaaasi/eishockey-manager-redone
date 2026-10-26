using System.Buffers.Binary;

namespace EishockeyManager.Formats;

/// <summary>
/// FM-Instrument einer CMF-Datei: die OPL2-Register für Modulator und Träger.
/// Aufbau der 16 Byte: 0/1 = 0x20 (AM, VIB, EG-Typ, KSR, MULT) Modulator/Träger, 2/3 = 0x40 (KSL, TL),
/// 4/5 = 0x60 (AR, DR), 6/7 = 0x80 (SL, RR), 8/9 = 0xE0 (Wellenform), 10 = 0xC0 (Rückkopplung, Verbindung).
/// </summary>
public sealed record CmfInstrument(
    byte ModulatorCharacteristic, byte CarrierCharacteristic,
    byte ModulatorLevel, byte CarrierLevel,
    byte ModulatorAttackDecay, byte CarrierAttackDecay,
    byte ModulatorSustainRelease, byte CarrierSustainRelease,
    byte ModulatorWaveform, byte CarrierWaveform,
    byte FeedbackConnection)
{
    public const int Length = 16;

    public static CmfInstrument Parse(ReadOnlySpan<byte> b) =>
        new(b[0], b[1], b[2], b[3], b[4], b[5], b[6], b[7], b[8], b[9], b[10]);
}

/// <summary>Ein MIDI-artiges Ereignis mit absoluter Zeit in Ticks.</summary>
/// <param name="Tick">Zeitpunkt in Ticks ab Liedanfang.</param>
/// <param name="Status">Statusbyte (z. B. 0x93 = Note an, Kanal 3; 0xFF = Meta).</param>
/// <param name="Data1">Erstes Datenbyte (Note, Controller, Programm; bei Meta der Typ).</param>
/// <param name="Data2">Zweites Datenbyte (Anschlag, Wert), sonst 0.</param>
public readonly record struct CmfEvent(int Tick, byte Status, byte Data1, byte Data2)
{
    public int Kind => Status & 0xF0;
    public int Channel => Status & 0x0F;
    public bool IsNoteOn => Kind == 0x90 && Data2 > 0;
    public bool IsNoteOff => Kind == 0x80 || (Kind == 0x90 && Data2 == 0);
}

/// <summary>
/// Creative Music File (CMF), im Spiel kb\248. Kopf (Version 1.1):
/// <c>"CTMF", u16 Version, u16 Instrumente-Offset, u16 Musik-Offset, u16 Ticks je Viertel, u16 Ticks je Sekunde,
/// u16 Titel, u16 Komponist, u16 Bemerkung, 16 × u8 Kanal benutzt, u16 Anzahl Instrumente (Version 1.0: u8),
/// u16 Grundtempo</c>. Die Musik ist ein MIDI-Spurdatenstrom (variable Längen, laufender Status), Ende mit <c>FF 2F 00</c>.
/// </summary>
public sealed class CmfFile
{
    public static ReadOnlySpan<byte> Signature => "CTMF"u8;

    private CmfFile(ushort version, int ticksPerQuarter, int ticksPerSecond, int basicTempo,
        bool[] channelInUse, IReadOnlyList<CmfInstrument> instruments, IReadOnlyList<CmfEvent> events, int lengthTicks)
    {
        Version = version;
        TicksPerQuarter = ticksPerQuarter;
        TicksPerSecond = ticksPerSecond;
        BasicTempo = basicTempo;
        ChannelInUse = channelInUse;
        Instruments = instruments;
        Events = events;
        LengthTicks = lengthTicks;
    }

    public ushort Version { get; }
    public int TicksPerQuarter { get; }
    public int TicksPerSecond { get; }
    public int BasicTempo { get; }
    public IReadOnlyList<bool> ChannelInUse { get; }
    public IReadOnlyList<CmfInstrument> Instruments { get; }

    /// <summary>Alle Ereignisse in Dateireihenfolge (ohne das Endeereignis).</summary>
    public IReadOnlyList<CmfEvent> Events { get; }

    /// <summary>Tick des Endeereignisses (bzw. des letzten Ereignisses).</summary>
    public int LengthTicks { get; }

    public TimeSpan Duration => TimeSpan.FromSeconds((double)LengthTicks / TicksPerSecond);

    public static bool IsCmf(ReadOnlySpan<byte> data) => data.Length >= 40 && data.StartsWith(Signature);

    public static CmfFile Load(string path) => Parse(File.ReadAllBytes(path));

    public static CmfFile Parse(ReadOnlySpan<byte> data)
    {
        if (!IsCmf(data))
            throw new InvalidDataException("Keine CMF-Datei (Kopf „CTMF“ fehlt).");

        var version = BinaryPrimitives.ReadUInt16LittleEndian(data[4..]);
        int instrumentOffset = BinaryPrimitives.ReadUInt16LittleEndian(data[6..]);
        int musicOffset = BinaryPrimitives.ReadUInt16LittleEndian(data[8..]);
        int ticksPerQuarter = BinaryPrimitives.ReadUInt16LittleEndian(data[10..]);
        int ticksPerSecond = BinaryPrimitives.ReadUInt16LittleEndian(data[12..]);
        var inUse = data.Slice(20, 16).ToArray().Select(b => b != 0).ToArray();
        int count, tempo;
        if (version < 0x0101)
        {
            count = data[36];
            tempo = data[37];
        }
        else
        {
            count = BinaryPrimitives.ReadUInt16LittleEndian(data[36..]);
            tempo = BinaryPrimitives.ReadUInt16LittleEndian(data[38..]);
        }

        if (ticksPerSecond <= 0)
            throw new InvalidDataException("CMF: Ticks je Sekunde fehlen.");
        if (instrumentOffset + count * CmfInstrument.Length > data.Length || musicOffset >= data.Length)
            throw new InvalidDataException("CMF: Instrumente oder Musik liegen außerhalb der Datei.");

        var instruments = new List<CmfInstrument>(count);
        for (var i = 0; i < count; i++)
            instruments.Add(CmfInstrument.Parse(data.Slice(instrumentOffset + i * CmfInstrument.Length, CmfInstrument.Length)));

        var events = ParseEvents(data[musicOffset..], out var lengthTicks);
        return new CmfFile(version, ticksPerQuarter, ticksPerSecond, tempo, inUse, instruments, events, lengthTicks);
    }

    private static List<CmfEvent> ParseEvents(ReadOnlySpan<byte> music, out int endTick)
    {
        var events = new List<CmfEvent>();
        var pos = 0;
        var tick = 0;
        byte status = 0;

        while (pos < music.Length)
        {
            tick += ReadVariable(music, ref pos);
            if (pos >= music.Length)
                break;

            if ((music[pos] & 0x80) != 0)
                status = music[pos++];
            else if (status == 0)
                throw new InvalidDataException($"CMF: Datenbyte ohne Status bei {pos}.");

            switch (status & 0xF0)
            {
                case 0x80:
                case 0x90:
                case 0xA0:
                case 0xB0:
                case 0xE0:
                    Need(music, pos, 2);
                    events.Add(new CmfEvent(tick, status, music[pos], music[pos + 1]));
                    pos += 2;
                    break;
                case 0xC0:
                case 0xD0:
                    Need(music, pos, 1);
                    events.Add(new CmfEvent(tick, status, music[pos], 0));
                    pos += 1;
                    break;
                default:
                    if (status == 0xFF)
                    {
                        Need(music, pos, 1);
                        var type = music[pos++];
                        var length = ReadVariable(music, ref pos);
                        pos += length;
                        if (type == 0x2F)
                        {
                            endTick = tick;
                            return events;
                        }
                        events.Add(new CmfEvent(tick, 0xFF, type, 0));
                    }
                    else if (status is 0xF0 or 0xF7)
                    {
                        pos += ReadVariable(music, ref pos); // SysEx überspringen
                        status = 0;
                    }
                    else
                        throw new InvalidDataException($"CMF: unbekanntes Statusbyte {status:X2}.");
                    break;
            }
        }

        endTick = tick; // ohne Endeereignis: letzter Zeitpunkt
        return events;
    }

    private static int ReadVariable(ReadOnlySpan<byte> data, ref int pos)
    {
        var value = 0;
        for (var i = 0; i < 4 && pos < data.Length; i++)
        {
            var b = data[pos++];
            value = value << 7 | b & 0x7F;
            if ((b & 0x80) == 0)
                return value;
        }
        if (pos >= data.Length)
            return value;
        throw new InvalidDataException("CMF: Zeitangabe länger als 4 Byte.");
    }

    private static void Need(ReadOnlySpan<byte> data, int pos, int count)
    {
        if (pos + count > data.Length)
            throw new InvalidDataException($"CMF: Ereignis bei {pos} abgeschnitten.");
    }
}
