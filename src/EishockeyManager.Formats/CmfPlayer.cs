namespace EishockeyManager.Formats;

/// <summary>
/// Spielt eine <see cref="CmfFile"/> über die <see cref="Opl2"/>-Emulation ab. Die Ansteuerung folgt dem
/// FM-Treiber von Creative (SBFMDRV), den em.exe ab Datei 0x14D70 enthält (Adressen unten relativ dazu):
/// <list type="bullet">
/// <item>Stimmenwahl 0B6E: zuerst eine losgelassene Stimme desselben MIDI-Kanals, dann eine freie, dann irgendeine
/// losgelassene, sonst die Stimme mit dem ältesten Notenbeginn (gezählt in Ereignissen).</item>
/// <item>Instrument 0A1E: wird nur geladen, wenn die Stimme vorher einem anderen Kanal gehörte oder das Programm wechselt;
/// die Trägerlautstärke wird erst beim Notenbeginn gesetzt: TL = 63 − ((63 − TL<sub>Instrument</sub>) · (Anschlag | 0x80) ≫ 8).</item>
/// <item>Tonhöhe 0E75: Oktave = Note / 12 − 1 (Noten unter 24 in Oktave 0, ab 108 in Oktave 7), Halbton in 1/64,
/// F-Nummer = round(342,92 · 2^(i / 768)) (Tabelle bei 0305, 768 Einträge, Formel stimmt mit allen Einträgen).</item>
/// <item>Controller 0EFA: 0x66 Marke, 0x67 Rhythmusmodus (0xBD = 0xE0, 6 Melodiestimmen, sonst 0xC0 und 9),
/// 0x68/0x69 Transponieren (Wert als vorzeichenbehaftetes Byte, Einheit 1/256 Halbton), alle übrigen ignoriert.
/// Tonhöhenbeugung und Aftertouch ignoriert der Treiber.</item>
/// <item>Rhythmus: MIDI-Kanal 11 Bass Drum, 12 Snare, 13 Tom, 14 Becken, 15 Hi-Hat; Instrument beim Programmwechsel,
/// bei Snare/Tom/Becken/Hi-Hat nur die Modulatorwerte in den jeweiligen Operator.</item>
/// </list>
/// </summary>
public sealed class CmfPlayer
{
    private const int MaxVoices = 9;

    /// <summary>Standardinstrument nach dem Zurücksetzen (Treiber 01C6).</summary>
    private static readonly CmfInstrument DefaultInstrument =
        new(0x01, 0x11, 0x4F, 0x00, 0xF1, 0xF2, 0x53, 0x74, 0x00, 0x00, 0x08);

    private static readonly byte[] ChannelOperatorOffset = [0x00, 0x01, 0x02, 0x08, 0x09, 0x0A, 0x10, 0x11, 0x12];

    // Rhythmusstimmen 6–10 (MIDI-Kanal − 5): Operator, Bit in 0xBD, OPL-Kanal der Tonhöhe
    private static readonly byte[] PercussionOperator = [0x10, 0x14, 0x12, 0x15, 0x11];
    private static readonly byte[] PercussionBit = [0x10, 0x08, 0x04, 0x02, 0x01];
    private static readonly byte[] PercussionChannel = [6, 7, 8, 8, 7];

    private static readonly ushort[] FNumbers = BuildFNumbers();

    private readonly CmfFile _file;
    private readonly Opl2 _opl = new();
    private readonly int _sampleRate;

    // Zustand je Stimme (Treiber 0148, 015E, 0169, 0174, 018A, 01A0)
    private readonly byte[] _owner = new byte[11];
    private readonly byte[] _note = new byte[11];
    private readonly byte[] _kslBits = new byte[11];
    private readonly byte[] _volume = new byte[11];
    private readonly ushort[] _frequency = new ushort[11];
    private readonly int[] _started = new int[11];

    // Zustand je MIDI-Kanal (01E9 Programm, 01F9 Transponierung in 1/64 Halbton)
    private readonly byte[] _program = new byte[16];
    private readonly int[] _pitchOffset = new int[16];

    private int _voices;
    private bool _rhythm;
    private byte _bd;
    private int _eventCounter;

    private int _nextEvent;
    private int _tick;
    private double _samplesUntilTick;

    public CmfPlayer(CmfFile file, int sampleRate, bool loop = false)
    {
        if (sampleRate <= 0)
            throw new ArgumentOutOfRangeException(nameof(sampleRate));
        _file = file;
        _sampleRate = sampleRate;
        Loop = loop;
        Restart();
    }

    public bool Loop { get; set; }

    /// <summary>Lied zu Ende (nur ohne <see cref="Loop"/>).</summary>
    public bool Finished { get; private set; }

    /// <summary>Wert der letzten Marke (Controller 0x66), wie ihn der Treiber im Statusbyte meldet.</summary>
    public int Marker { get; private set; }

    /// <summary>Anzahl ausgelöster Noten (zum Prüfen).</summary>
    public int NotesPlayed { get; private set; }

    public int SampleRate => _sampleRate;

    /// <summary>Der emulierte Chip (zum Prüfen der Register).</summary>
    public Opl2 Chip => _opl;

    public TimeSpan Position => TimeSpan.FromSeconds((double)_tick / _file.TicksPerSecond);

    /// <summary>Zurück an den Anfang; der Chip wird wie vom Treiber zurückgesetzt (0BEE, 09AD).</summary>
    public void Restart()
    {
        _opl.Reset();
        ResetVoices(rhythm: false);
        Array.Clear(_pitchOffset);
        _nextEvent = 0;
        _tick = 0;
        _samplesUntilTick = 0;
        _eventCounter = 0;
        Finished = false;
        Marker = 0;
    }

    /// <summary>Füllt den Puffer; liefert die Anzahl geschriebener Abtastwerte (weniger nur am Liedende ohne Schleife).</summary>
    public int Read(Span<short> buffer)
    {
        var written = 0;
        var samplesPerTick = (double)_sampleRate / _file.TicksPerSecond;
        while (written < buffer.Length && !Finished)
        {
            if (_samplesUntilTick <= 0)
            {
                ProcessTick();
                _samplesUntilTick += samplesPerTick;
                if (Finished)
                    break;
            }
            var count = Math.Min(buffer.Length - written, Math.Max(1, (int)Math.Ceiling(_samplesUntilTick)));
            _opl.Generate(buffer.Slice(written, count), _sampleRate);
            written += count;
            _samplesUntilTick -= count;
        }
        return written;
    }

    /// <summary>Das ganze Lied einmal als PCM (ohne Schleife).</summary>
    public static PcmSound Render(CmfFile file, int sampleRate)
    {
        var player = new CmfPlayer(file, sampleRate);
        var samples = new List<short>((int)(file.Duration.TotalSeconds * sampleRate) + sampleRate);
        var buffer = new short[4096];
        int n;
        while ((n = player.Read(buffer)) > 0)
            samples.AddRange(buffer.AsSpan(0, n));
        return new PcmSound(sampleRate, [.. samples], 0, 0, 16);
    }

    private void ProcessTick()
    {
        var events = _file.Events;
        while (_nextEvent < events.Count && events[_nextEvent].Tick <= _tick)
            Handle(events[_nextEvent++]);

        if (_nextEvent >= events.Count && _tick >= _file.LengthTicks)
        {
            if (Loop && events.Count > 0)
            {
                // Das Spiel startet das Lied neu: wie Stopp (0964) und erneutes Abspielen
                StopAllVoices();
                _nextEvent = 0;
                _tick = 0;
                return;
            }
            StopAllVoices();
            Finished = true;
            return;
        }
        _tick++;
    }

    private void Handle(CmfEvent e)
    {
        _eventCounter++;
        switch (e.Kind)
        {
            case 0x80:
                NoteOff(e.Channel, e.Data1);
                break;
            case 0x90:
                if (e.Data2 == 0)
                    NoteOff(e.Channel, e.Data1);
                else
                    NoteOn(e.Channel, e.Data1, e.Data2);
                break;
            case 0xB0:
                Controller(e.Channel, e.Data1, e.Data2);
                break;
            case 0xC0:
                ProgramChange(e.Channel, e.Data1);
                break;
        }
    }

    private void Controller(int channel, int controller, int value)
    {
        switch (controller)
        {
            case 0x66:
                Marker = value;
                break;
            case 0x67:
                ResetVoices(rhythm: value != 0);
                break;
            case 0x68:
                Transpose(channel, (sbyte)value);
                break;
            case 0x69:
                Transpose(channel, (sbyte)-value);
                break;
        }
    }

    /// <summary>Treiber 0F3C: Versatz speichern und klingende Stimmen des Kanals neu stimmen (mit Tastendruck).</summary>
    private void Transpose(int channel, int value)
    {
        _pitchOffset[channel] = value >> 2;
        for (var v = 0; v < _voices; v++)
        {
            if (_owner[v] != channel)
                continue;
            var freq = Frequency(channel, _note[v], v);
            _opl.Write(0xA0 + v, (byte)freq);
            _opl.Write(0xB0 + v, (byte)(freq >> 8 | 0x20));
        }
    }

    private void ProgramChange(int channel, int program)
    {
        var count = Math.Max(1, _file.Instruments.Count);
        _program[channel] = (byte)(program % count);

        if (_rhythm && channel >= 11)
        {
            LoadInstrument(channel - 5, _program[channel]);
            return;
        }
        for (var v = 0; v < _voices; v++)
        {
            if (_owner[v] == (channel | 0x80))
                _owner[v] = 0xFF; // losgelassene Stimmen des Kanals freigeben
            else if (_owner[v] == channel)
                LoadInstrument(v, _program[channel]);
        }
    }

    private void NoteOn(int channel, int note, int velocity)
    {
        NotesPlayed++;
        if (_rhythm && channel >= 11)
        {
            var p = channel - 5;
            _bd |= PercussionBit[p - 6];
            var op = PercussionOperator[p - 6] + (p == 6 ? 3 : 0);
            _opl.Write(0x40 + op, Level(p, velocity));
            var freq = Frequency(channel, note, p);
            var oplChannel = PercussionChannel[p - 6];
            _opl.Write(0xA0 + oplChannel, (byte)freq);
            _opl.Write(0xB0 + oplChannel, (byte)(freq >> 8));
            _opl.Write(0xBD, _bd);
            return;
        }

        var v = AllocateVoice(channel);
        var previous = _owner[v] & 0x7F;
        _owner[v] = (byte)channel;
        if (previous != channel)
            LoadInstrument(v, _program[channel]);

        _opl.Write(0x40 + ChannelOperatorOffset[v] + 3, Level(v, velocity));
        var f = Frequency(channel, note, v);
        _opl.Write(0xA0 + v, (byte)f);
        _opl.Write(0xB0 + v, (byte)(f >> 8 | 0x20));
    }

    private void NoteOff(int channel, int note)
    {
        if (_rhythm && channel >= 11)
        {
            _bd &= (byte)~PercussionBit[channel - 11];
            _opl.Write(0xBD, _bd);
            return;
        }
        for (var v = 0; v < _voices; v++)
        {
            if (_note[v] != note || _owner[v] != channel)
                continue;
            _owner[v] |= 0x80;
            _opl.Write(0xA0 + v, (byte)_frequency[v]);
            _opl.Write(0xB0 + v, (byte)(_frequency[v] >> 8));
            return;
        }
    }

    /// <summary>Treiber 0B6E.</summary>
    private int AllocateVoice(int channel)
    {
        for (var v = 0; v < _voices; v++)
            if (_owner[v] == (channel | 0x80))
                return v;
        for (var v = 0; v < _voices; v++)
            if (_owner[v] == 0xFF)
                return v;
        for (var v = 0; v < _voices; v++)
            if (_owner[v] > 0x7F)
                return v;

        var oldest = 0;
        var age = 0;
        for (var v = 0; v < _voices; v++)
        {
            var a = _eventCounter - _started[v];
            if (a > age)
            {
                age = a;
                oldest = v;
            }
        }
        _opl.Write(0xA0 + oldest, (byte)_frequency[oldest]);
        _opl.Write(0xB0 + oldest, (byte)(_frequency[oldest] >> 8));
        return oldest;
    }

    private byte Level(int voice, int velocity) =>
        (byte)(0x3F - (_volume[voice] * (velocity | 0x80) >> 8) | _kslBits[voice]);

    /// <summary>Treiber 0E75/0E9E: F-Nummer und Oktave (ohne Tastenbit), merkt Note, Frequenz und Zeitpunkt.</summary>
    private ushort Frequency(int channel, int note, int voice)
    {
        _note[voice] = (byte)note;
        var n = Math.Clamp(note, 0, 127);
        var block = n < 24 ? 0 : Math.Min(n / 12 - 1, 7);
        var semitone = n >= 120 ? 11 : n % 12;

        var index = semitone * 64 + _pitchOffset[channel];
        var blockBits = block << 2;
        if (index < 0)
        {
            index += 0x300;
            blockBits -= 4;
            if (blockBits < 0)
            {
                blockBits = 0;
                index = 0;
            }
        }
        else if (index >= 0x300)
        {
            index -= 0x300;
            blockBits += 4;
            if (blockBits > 0x1C)
            {
                index = 0x2FF;
                blockBits = 0x1C;
            }
        }

        var value = (ushort)(FNumbers[index] | blockBits << 8);
        _frequency[voice] = value;
        _started[voice] = _eventCounter;
        return value;
    }

    /// <summary>Treiber 0A1E.</summary>
    private void LoadInstrument(int voice, int program)
    {
        var instrument = program < _file.Instruments.Count ? _file.Instruments[program] : DefaultInstrument;
        WriteInstrument(voice, instrument);
    }

    private void WriteInstrument(int voice, CmfInstrument i)
    {
        var percussionOperator = _rhythm && voice >= 7;
        var level = percussionOperator ? i.ModulatorLevel : i.CarrierLevel;
        _kslBits[voice] = (byte)(level & 0xC0);
        _volume[voice] = (byte)(0x3F - (level & 0x3F));

        if (!percussionOperator)
        {
            var op = ChannelOperatorOffset[voice];
            _opl.Write(0x20 + op, i.ModulatorCharacteristic);
            _opl.Write(0x23 + op, i.CarrierCharacteristic);
            _opl.Write(0x40 + op, i.ModulatorLevel);
            _opl.Write(0x60 + op, i.ModulatorAttackDecay);
            _opl.Write(0x63 + op, i.CarrierAttackDecay);
            _opl.Write(0x80 + op, i.ModulatorSustainRelease);
            _opl.Write(0x83 + op, i.CarrierSustainRelease);
            _opl.Write(0xE0 + op, i.ModulatorWaveform);
            _opl.Write(0xE3 + op, i.CarrierWaveform);
            _opl.Write(0xC0 + voice, i.FeedbackConnection);
            return;
        }

        var p = PercussionOperator[voice - 6];
        _opl.Write(0x20 + p, i.ModulatorCharacteristic);
        _opl.Write(0x60 + p, i.ModulatorAttackDecay);
        _opl.Write(0x80 + p, i.ModulatorSustainRelease);
        _opl.Write(0xE0 + p, i.ModulatorWaveform);
        _opl.Write(0xC0 + PercussionChannel[voice - 6], i.FeedbackConnection);
    }

    /// <summary>Treiber 09AD/0F16: Stimmen frei, Programme 0, Standardinstrument auf allen Kanälen, 0xBD setzen.</summary>
    private void ResetVoices(bool rhythm)
    {
        Array.Clear(_program);
        Array.Fill(_owner, (byte)0xFF);
        _rhythm = false;
        _voices = MaxVoices;
        _opl.Write(0x08, 0);
        for (var v = 0; v < MaxVoices; v++)
            WriteInstrument(v, DefaultInstrument);

        _rhythm = rhythm;
        _voices = rhythm ? 6 : 9;
        _bd = rhythm ? (byte)0xE0 : (byte)0xC0;
        _opl.Write(0xBD, _bd);
    }

    /// <summary>Treiber 0964: alle Stimmen ohne Tastenbit, Rhythmustasten aus.</summary>
    private void StopAllVoices()
    {
        for (var v = 0; v < MaxVoices; v++)
        {
            if (_owner[v] > 0x7F)
                continue;
            _opl.Write(0xA0 + v, (byte)_frequency[v]);
            _opl.Write(0xB0 + v, (byte)(_frequency[v] >> 8));
        }
        if (_rhythm)
        {
            _bd &= 0xE0;
            _opl.Write(0xBD, _bd);
        }
        Array.Fill(_owner, (byte)0xFF);
    }

    /// <summary>F-Nummern-Tabelle des Treibers (0305): 768 Schritte je Oktave ab C.</summary>
    private static ushort[] BuildFNumbers()
    {
        var table = new ushort[768];
        for (var i = 0; i < table.Length; i++)
            table[i] = (ushort)Math.Round(342.92 * Math.Pow(2, i / 768.0));
        return table;
    }

    /// <summary>F-Nummer zum Tabellenindex (Halbton · 64 + Feinstimmung).</summary>
    public static ushort FNumber(int index) => FNumbers[index];
}
