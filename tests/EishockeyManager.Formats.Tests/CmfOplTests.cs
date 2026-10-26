using EishockeyManager.Tests.Support;
using EishockeyManager.Formats;

namespace EishockeyManager.Formats.Tests;

/// <summary>Baut CMF-Dateien (Version 1.1) für die Tests.</summary>
internal sealed class CmfBuilder
{
    private readonly List<CmfInstrument> _instruments = [];
    private readonly List<byte> _music = [];
    private int _ticksPerSecond = 96;

    public CmfBuilder TicksPerSecond(int value)
    {
        _ticksPerSecond = value;
        return this;
    }

    public CmfBuilder Instrument(CmfInstrument instrument)
    {
        _instruments.Add(instrument);
        return this;
    }

    /// <summary>Ein Ereignis mit vorangestellter Zeitdifferenz; Statusbyte darf fehlen (laufender Status).</summary>
    public CmfBuilder Event(int delta, params byte[] bytes)
    {
        Variable(delta);
        _music.AddRange(bytes);
        return this;
    }

    public CmfBuilder End(int delta) => Event(delta, 0xFF, 0x2F, 0x00);

    private void Variable(int value)
    {
        var groups = new List<byte> { (byte)(value & 0x7F) };
        while ((value >>= 7) > 0)
            groups.Insert(0, (byte)(value & 0x7F | 0x80));
        _music.AddRange(groups);
    }

    public byte[] Build()
    {
        const int instrumentOffset = 40;
        var musicOffset = instrumentOffset + _instruments.Count * CmfInstrument.Length;
        var data = new byte[musicOffset + _music.Count];
        "CTMF"u8.CopyTo(data);
        BitConverter.GetBytes((ushort)0x0101).CopyTo(data, 4);
        BitConverter.GetBytes((ushort)instrumentOffset).CopyTo(data, 6);
        BitConverter.GetBytes((ushort)musicOffset).CopyTo(data, 8);
        BitConverter.GetBytes((ushort)30).CopyTo(data, 10);
        BitConverter.GetBytes((ushort)_ticksPerSecond).CopyTo(data, 12);
        data[20] = 1;
        BitConverter.GetBytes((ushort)_instruments.Count).CopyTo(data, 36);
        BitConverter.GetBytes((ushort)120).CopyTo(data, 38);
        for (var i = 0; i < _instruments.Count; i++)
        {
            var x = _instruments[i];
            byte[] raw =
            [
                x.ModulatorCharacteristic, x.CarrierCharacteristic, x.ModulatorLevel, x.CarrierLevel,
                x.ModulatorAttackDecay, x.CarrierAttackDecay, x.ModulatorSustainRelease, x.CarrierSustainRelease,
                x.ModulatorWaveform, x.CarrierWaveform, x.FeedbackConnection,
            ];
            raw.CopyTo(data, instrumentOffset + i * CmfInstrument.Length);
        }
        _music.CopyTo(data, musicOffset);
        return data;
    }
}

public class CmfFileTests
{
    internal static readonly CmfInstrument Organ = new(0x21, 0x21, 0x3F, 0x00, 0xF0, 0xF0, 0x00, 0x0F, 0, 0, 0x01);

    [Fact]
    public void Parse_RejectsMissingSignature() =>
        Assert.Throws<InvalidDataException>(() => CmfFile.Parse(new byte[64]));

    [Fact]
    public void Parse_HeaderInstrumentsAndEvents()
    {
        var data = new CmfBuilder()
            .Instrument(Organ)
            .Event(0, 0xB0, 0x67, 0x01)
            .Event(0, 0xC3, 0x00)
            .Event(200, 0x93, 60, 100)  // Zeitdifferenz mit zwei Byte
            .Event(10, 64, 90)          // laufender Status
            .Event(10, 60, 0)           // Note an mit Anschlag 0 = Note aus
            .Event(0, 0x83, 64, 0)
            .Event(0, 0xFF, 0x01, 0x02, (byte)'h', (byte)'i') // Meta-Text wird übersprungen, aber vermerkt
            .End(30)
            .Build();

        var cmf = CmfFile.Parse(data);
        Assert.Equal(0x0101, cmf.Version);
        Assert.Equal(96, cmf.TicksPerSecond);
        Assert.Equal(30, cmf.TicksPerQuarter);
        Assert.Equal(120, cmf.BasicTempo);
        Assert.True(cmf.ChannelInUse[0]);
        Assert.Equal(Organ, Assert.Single(cmf.Instruments));
        Assert.Equal(250, cmf.LengthTicks);
        Assert.Equal(TimeSpan.FromSeconds(250 / 96.0), cmf.Duration);

        var e = cmf.Events;
        Assert.Equal(new CmfEvent(0, 0xB0, 0x67, 1), e[0]);
        Assert.Equal(new CmfEvent(0, 0xC3, 0, 0), e[1]);
        Assert.Equal(new CmfEvent(200, 0x93, 60, 100), e[2]);
        Assert.Equal(new CmfEvent(210, 0x93, 64, 90), e[3]);
        Assert.True(e[4].IsNoteOff);
        Assert.Equal(220, e[4].Tick);
        Assert.True(e[5].IsNoteOff);
        Assert.Equal(0xFF, e[6].Status);
        Assert.Equal(2, e.Count(x => x.IsNoteOn));
    }

    [Fact]
    public void Parse_TruncatedEventThrows()
    {
        var data = new CmfBuilder().Event(0, 0x90, 60).Build();
        Assert.Throws<InvalidDataException>(() => CmfFile.Parse(data));
    }
}

public class CmfPlayerTests
{
    private static CmfPlayer Play(CmfBuilder builder, int ticks, int sampleRate = 9600)
    {
        var player = new CmfPlayer(CmfFile.Parse(builder.Build()), sampleRate);
        player.Read(new short[sampleRate / 96 * ticks + 1]);
        return player;
    }

    [Theory]
    [InlineData(0, 343)]
    [InlineData(64, 363)]   // Cis
    [InlineData(9 * 64, 577)] // A
    [InlineData(767, 685)]
    public void FNumberTable(int index, int expected) => Assert.Equal(expected, CmfPlayer.FNumber(index));

    [Theory]
    [InlineData(69, 4, 577)]  // A4: Oktave = 69 / 12 − 1
    [InlineData(60, 4, 343)]
    [InlineData(12, 0, 343)]  // unter 24 immer Oktave 0
    [InlineData(5, 0, 458)]
    [InlineData(115, 7, 514)] // ab 108 Oktave 7 (G)
    [InlineData(127, 7, 647)] // ab 120 fest auf H
    public void NoteOn_WritesFrequencyWithKey(int note, int block, int fnum)
    {
        var p = Play(new CmfBuilder().Instrument(CmfFileTests.Organ).Event(0, 0x90, (byte)note, 127).End(10), 2);
        Assert.Equal(fnum & 0xFF, p.Chip.ReadRegister(0xA0));
        Assert.Equal(0x20 | block << 2 | fnum >> 8, p.Chip.ReadRegister(0xB0));
    }

    [Theory]
    [InlineData(0x00, 127, 0x01)] // Lautstärke 63 · 255 / 256 = 62 → TL 1
    [InlineData(0x00, 1, 0x20)]   // 63 · 129 / 256 = 31 → TL 32
    [InlineData(0x80, 127, 0x81)] // KSL-Bits bleiben erhalten
    [InlineData(0x3F, 127, 0x3F)] // stumm bleibt stumm
    public void NoteOn_VelocityScalesCarrierLevel(byte carrierLevel, byte velocity, int expected)
    {
        var instrument = CmfFileTests.Organ with { CarrierLevel = carrierLevel };
        var p = Play(new CmfBuilder().Instrument(instrument).Event(0, 0x90, 60, velocity).End(10), 2);
        Assert.Equal(expected, p.Chip.ReadRegister(0x43));
        Assert.Equal(instrument.ModulatorLevel, p.Chip.ReadRegister(0x40));
    }

    [Fact]
    public void NoteOff_ReleasesKeyAndKeepsFrequency()
    {
        var p = Play(new CmfBuilder().Instrument(CmfFileTests.Organ)
            .Event(0, 0x90, 69, 100).Event(1, 0x80, 69, 0).End(10), 4);
        Assert.Equal(0x12, p.Chip.ReadRegister(0xB0));
        Assert.Equal(577 & 0xFF, p.Chip.ReadRegister(0xA0));
    }

    [Fact]
    public void Voices_AreReusedPerChannelAndOldestIsStolen()
    {
        var b = new CmfBuilder().Instrument(CmfFileTests.Organ);
        for (var n = 0; n < 9; n++)
            b.Event(0, 0x90, (byte)(48 + n), 100);  // alle 9 Stimmen belegt (Stimme 0 = Note 48)
        b.Event(1, 0x90, 70, 100);                  // zehnte Note: älteste Stimme (0) wird übernommen
        b.End(10);
        var p = Play(b, 4);
        // Stimme 0 spielt jetzt Note 70 = Ais4 (Oktave 4, Index 10 · 64)
        Assert.Equal(CmfPlayer.FNumber(10 * 64) & 0xFF, p.Chip.ReadRegister(0xA0));
        Assert.Equal(0x20 | 4 << 2 | CmfPlayer.FNumber(10 * 64) >> 8, p.Chip.ReadRegister(0xB0));
        // Stimme 1 unverändert Note 49
        Assert.Equal(CmfPlayer.FNumber(64) & 0xFF, p.Chip.ReadRegister(0xA1));
    }

    [Fact]
    public void ProgramChange_LoadsInstrumentOnNextNote()
    {
        var second = CmfFileTests.Organ with { ModulatorCharacteristic = 0x07, FeedbackConnection = 0x0E };
        var p = Play(new CmfBuilder().Instrument(CmfFileTests.Organ).Instrument(second)
            .Event(0, 0xC2, 1).Event(0, 0x92, 60, 100).End(10), 2);
        Assert.Equal(0x07, p.Chip.ReadRegister(0x20));
        Assert.Equal(0x0E, p.Chip.ReadRegister(0xC0));
    }

    [Fact]
    public void RhythmMode_DrumsUseBdRegister()
    {
        var drum = CmfFileTests.Organ with { ModulatorCharacteristic = 0x01, ModulatorLevel = 0x0B };
        var b = new CmfBuilder().Instrument(CmfFileTests.Organ).Instrument(drum)
            .Event(0, 0xB0, 0x67, 1)
            .Event(0, 0xCC, 1)          // Snare bekommt Instrument 1
            .Event(0, 0x9C, 60, 127)
            .Event(0, 0x9B, 36, 127)    // Bass Drum
            .End(10);
        var p = Play(b, 2);
        Assert.Equal(0xE0 | 0x10 | 0x08, p.Chip.ReadRegister(0xBD));
        Assert.Equal(0x01, p.Chip.ReadRegister(0x34));             // Snare-Operator (Kanal 7, Träger)
        Assert.Equal(0x3F - (0x34 * 0xFF >> 8), p.Chip.ReadRegister(0x54)); // Modulatorpegel 0x0B als Lautstärke
        Assert.Equal(CmfPlayer.FNumber(0) & 0xFF, p.Chip.ReadRegister(0xA7)); // Snare-Tonhöhe auf Kanal 7

        var off = Play(new CmfBuilder().Instrument(CmfFileTests.Organ)
            .Event(0, 0xB0, 0x67, 1).Event(0, 0x9C, 60, 127).Event(1, 0x9C, 60, 0).End(10), 4);
        Assert.Equal(0xE0, off.Chip.ReadRegister(0xBD));
    }

    [Fact]
    public void Transpose_ShiftsSoundingVoices()
    {
        // 0x68 mit Wert 64 → 64 / 4 = 16 Schritte (1/4 Halbton)
        var p = Play(new CmfBuilder().Instrument(CmfFileTests.Organ)
            .Event(0, 0x90, 69, 100).Event(1, 0xB0, 0x68, 64).End(10), 4);
        Assert.Equal(CmfPlayer.FNumber(9 * 64 + 16) & 0xFF, p.Chip.ReadRegister(0xA0));
    }

    [Fact]
    public void Render_DurationFollowsTicksPerSecond()
    {
        var cmf = CmfFile.Parse(new CmfBuilder().Instrument(CmfFileTests.Organ)
            .Event(0, 0x90, 69, 127).Event(480, 0x80, 69, 0).End(480).Build());
        var sound = CmfPlayer.Render(cmf, 11025);
        Assert.Equal(10.0, sound.Duration.TotalSeconds, 0.02);
        Assert.InRange(sound.Samples.Take(11025 * 4).Max(s => Math.Abs((int)s)), 1000, 32767);
    }

    [Fact]
    public void Loop_RestartsAfterEnd()
    {
        var player = new CmfPlayer(CmfFile.Parse(new CmfBuilder().Instrument(CmfFileTests.Organ)
            .Event(0, 0x90, 69, 127).End(96).Build()), 9600, loop: true);
        var buffer = new short[9600 * 3];
        Assert.Equal(buffer.Length, player.Read(buffer));
        Assert.False(player.Finished);
        Assert.Equal(3, player.NotesPlayed);
    }
}

public class Opl2Tests
{
    private static Opl2 SineVoice(int fnum, int block)
    {
        var opl = new Opl2();
        opl.Reset();
        // Modulator stumm, Träger Sinus, MULT 1, Attack/Decay schnell, Sustain gehalten
        opl.Write(0x20, 0x21);
        opl.Write(0x23, 0x21);
        opl.Write(0x40, 0x3F);
        opl.Write(0x43, 0x00);
        opl.Write(0x60, 0xF0);
        opl.Write(0x63, 0xF0);
        opl.Write(0x80, 0x0F);
        opl.Write(0x83, 0x0F);
        opl.Write(0xC0, 0x01); // additiv, Modulator trägt nichts bei
        opl.Write(0xA0, (byte)fnum);
        opl.Write(0xB0, (byte)(0x20 | block << 2 | fnum >> 8));
        return opl;
    }

    private static double DominantFrequency(short[] samples, int rate)
    {
        var crossings = 0;
        for (var i = 1; i < samples.Length; i++)
            if (samples[i - 1] < 0 && samples[i] >= 0)
                crossings++;
        return crossings * (double)rate / samples.Length;
    }

    [Fact]
    public void Silent_AfterReset()
    {
        var opl = new Opl2();
        opl.Reset();
        var buffer = new short[1000];
        opl.Generate(buffer, 44100);
        Assert.All(buffer, s => Assert.Equal(0, s));
    }

    [Theory]
    [InlineData(577, 4, 437.7)] // A4 aus der Treibertabelle
    [InlineData(343, 2, 65.05)]
    [InlineData(686, 6, 2081.6)]
    public void Sine_HasExpectedFrequency(int fnum, int block, double hz)
    {
        var opl = SineVoice(fnum, block);
        var buffer = new short[44100];
        opl.Generate(buffer, 44100);
        Assert.Equal(hz, DominantFrequency(buffer, 44100), 1.5);
        Assert.InRange(buffer.Max(), 3500, 4200); // volle Lautstärke eines Operators ≈ 4084 (+ Rest des Modulators)
    }

    [Fact]
    public void KeyOff_ReleasesToSilence()
    {
        var opl = SineVoice(577, 4);
        opl.Write(0x83, 0x0F | 0x00); // Release-Rate 15
        opl.Generate(new short[4410], 44100);
        opl.Write(0xB0, 4 << 2 | 577 >> 8);
        var after = new short[4410];
        opl.Generate(after, 44100);
        Assert.All(after[^1000..], s => Assert.InRange(s, -2, 2));
    }

    [Fact]
    public void TotalLevel_AttenuatesBySixDbPerEightSteps()
    {
        var loud = SineVoice(577, 4);
        var quiet = SineVoice(577, 4);
        quiet.Write(0x43, 8); // 8 · 0,75 dB = 6 dB → halbe Amplitude
        var a = new short[4410];
        var b = new short[4410];
        loud.Generate(a, 44100);
        quiet.Generate(b, 44100);
        Assert.Equal(0.5, (double)b.Max() / a.Max(), 0.03);
    }

    [Fact]
    public void Modulation_AddsHarmonics()
    {
        var plain = SineVoice(577, 4);
        var fm = SineVoice(577, 4);
        fm.Write(0xC0, 0x00);  // FM-Verbindung
        fm.Write(0x40, 0x10);  // Modulator hörbar
        fm.Write(0x60, 0xF0);
        var a = new short[8820];
        var b = new short[8820];
        plain.Generate(a, 44100);
        fm.Generate(b, 44100);
        Assert.NotEqual(a, b);
        Assert.True(DominantFrequency(b, 44100) > 437.7 * 1.2);
    }

    [Fact]
    public void RhythmMode_HiHatProducesNoise()
    {
        var opl = new Opl2();
        opl.Reset();
        opl.Write(0x31, 0x01);   // Hi-Hat-Operator (Kanal 7, Modulator)
        opl.Write(0x51, 0x00);
        opl.Write(0x71, 0xF0);
        opl.Write(0x91, 0x0F);
        opl.Write(0xA7, 0x00);
        opl.Write(0xB7, 0x1F);
        opl.Write(0xBD, 0x21);   // Rhythmusmodus, Hi-Hat an
        var buffer = new short[4410];
        opl.Generate(buffer, 44100);
        Assert.True(buffer.Max(s => Math.Abs((int)s)) > 1000);
    }
}

/// <summary>Musik des Originals kb\248 und FM-Treiber in em.exe (übersprungen ohne „gamedata“).</summary>
public class CmfOriginalTests
{
    private static GameData Game() => TestGameData.Load();

    [GameDataFact]
    public void Music248_EventsAndDuration()
    {
        var game = Game();
        var cmf = CmfFile.Load(game.GraphicsPath(248));
        Assert.Equal(14, cmf.Instruments.Count);
        Assert.Equal(96, cmf.TicksPerSecond);
        Assert.Equal(4550, cmf.Events.Count(e => e.IsNoteOn));
        Assert.Equal(4550, cmf.Events.Count(e => e.IsNoteOff));
        Assert.Equal(16491, cmf.LengthTicks);
        Assert.Equal(172, cmf.Duration.TotalSeconds, 1.0);
        Assert.Equal(new CmfEvent(0, 0xB0, 0x67, 1), cmf.Events[0]); // Rhythmusmodus zuerst
    }

    [GameDataFact]
    public void Music248_RendersAllNotes()
    {
        var game = Game();
        var cmf = CmfFile.Load(game.GraphicsPath(248));
        var player = new CmfPlayer(cmf, 11025);
        var total = 0;
        var peak = 0;
        var buffer = new short[11025];
        int n;
        while ((n = player.Read(buffer)) > 0)
        {
            total += n;
            for (var i = 0; i < n; i++)
                peak = Math.Max(peak, Math.Abs((int)buffer[i]));
        }
        Assert.Equal(4550, player.NotesPlayed);
        Assert.Equal(172, total / 11025.0, 1.0);
        Assert.InRange(peak, 2000, 32000); // hörbar, nicht übersteuert
    }

    /// <summary>Die F-Nummern-Formel stimmt mit der Tabelle des FM-Treibers in em.exe überein.</summary>
    [GameDataFact]
    public void FNumberTable_MatchesDriverInEmExe()
    {
        var game = Game();
        var exe = File.ReadAllBytes(Path.Combine(game.RootDirectory, "em.exe"));
        const int driver = 0x14D70;
        Assert.Equal("FMDRV"u8.ToArray(), exe[(driver + 3)..(driver + 8)]);
        for (var i = 0; i < 768; i++)
            Assert.True(BitConverter.ToUInt16(exe, driver + 0x305 + 2 * i) == CmfPlayer.FNumber(i), $"Index {i}");
    }
}
