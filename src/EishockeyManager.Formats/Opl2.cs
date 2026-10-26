namespace EishockeyManager.Formats;

/// <summary>
/// Emulation des Yamaha YM3812 (OPL2, Sound Blaster/AdLib), eigener Code nach dem dokumentierten Aufbau des Chips:
/// 9 Kanäle mit je zwei Operatoren, Phasengenerator mit Multiplikator, Hüllkurve (Attack/Decay/Sustain/Release,
/// KSR, KSL), logarithmische Sinustabelle mit Exponentialtabelle, 4 Wellenformen, Rückkopplung, AM/FM-Verbindung,
/// Tremolo/Vibrato und Rhythmusmodus (Bass Drum, Snare, Tom, Becken, Hi-Hat mit Rauschgenerator).
/// Der Chip läuft mit 3,579545 MHz / 72 ≈ 49 716 Hz; <see cref="Generate"/> rechnet linear auf die Zielrate um.
/// [APPROX] Nicht bitgenau: Der Attack-Verlauf ist exponentiell genähert, Timer/CSM fehlen (vom Spiel nicht benutzt).
/// </summary>
public sealed class Opl2
{
    public const double NativeRate = 3_579_545.0 / 72;

    private const int MaxAttenuation = 0x1FF;

    private static readonly ushort[] LogSin = BuildLogSin();
    private static readonly ushort[] Exp = BuildExp();
    private static readonly byte[] MultiplierTimesTwo = [1, 2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 20, 24, 24, 30, 30];
    private static readonly byte[] KslTable = [0, 32, 40, 45, 48, 51, 53, 55, 56, 58, 59, 60, 61, 62, 63, 64];
    private static readonly byte[] KslShift = [8, 1, 2, 0]; // Registerwert 0 = aus, 1 = 3 dB, 2 = 1,5 dB, 3 = 6 dB je Oktave

    // Steigerung der Dämpfung je Hüllkurvenschritt: für jede Rate ein Muster aus 8 Schritten
    private static readonly byte[][] IncrementPatterns =
    [
        [0, 1, 0, 1, 0, 1, 0, 1], [0, 1, 0, 1, 1, 1, 0, 1], [0, 1, 1, 1, 0, 1, 1, 1], [0, 1, 1, 1, 1, 1, 1, 1],
        [1, 1, 1, 1, 1, 1, 1, 1], [1, 1, 1, 2, 1, 1, 1, 2], [1, 2, 1, 2, 1, 2, 1, 2], [1, 2, 2, 2, 1, 2, 2, 2],
        [2, 2, 2, 2, 2, 2, 2, 2], [2, 2, 2, 4, 2, 2, 2, 4], [2, 4, 2, 4, 2, 4, 2, 4], [2, 4, 4, 4, 2, 4, 4, 4],
        [4, 4, 4, 4, 4, 4, 4, 4], [4, 4, 4, 8, 4, 4, 4, 8], [4, 8, 4, 8, 4, 8, 4, 8], [4, 8, 8, 8, 4, 8, 8, 8],
        [8, 8, 8, 8, 8, 8, 8, 8],
    ];

    // Registerversatz 0x00–0x15 → Operator 0–17 (−1 = unbelegt)
    private static readonly sbyte[] SlotOfOffset =
        [0, 1, 2, 3, 4, 5, -1, -1, 6, 7, 8, 9, 10, 11, -1, -1, 12, 13, 14, 15, 16, 17];

    private const int SlotHiHat = 13, SlotTom = 14, SlotSnare = 16, SlotCymbal = 17;

    private readonly Operator[] _slots = new Operator[18];
    private readonly Channel[] _channels = new Channel[9];
    private readonly byte[] _registers = new byte[256];

    private bool _waveformSelect;
    private bool _noteSelect;
    private bool _deepTremolo;
    private bool _deepVibrato;
    private bool _rhythm;
    private byte _rhythmKeys;

    private uint _sampleCounter;
    private uint _envelopeCounter;
    private int _tremoloPosition;
    private int _vibratoPosition;
    private uint _noise = 1;

    // Umrechnung auf die Zielrate
    private double _resamplePosition = 1;
    private short _previous;
    private short _current;

    public Opl2()
    {
        for (var i = 0; i < _slots.Length; i++)
            _slots[i] = new Operator();
        for (var c = 0; c < _channels.Length; c++)
        {
            var modulator = c / 3 * 6 + c % 3;
            _channels[c] = new Channel(_slots[modulator], _slots[modulator + 3]);
        }
    }

    /// <summary>Zuletzt geschriebener Wert eines Registers.</summary>
    public byte ReadRegister(int register) => _registers[register & 0xFF];

    /// <summary>Setzt den Chip in den Einschaltzustand (alle Register 0, Hüllkurven stumm).</summary>
    public void Reset()
    {
        for (var r = 0; r < 256; r++)
            Write(r, 0);
        foreach (var slot in _slots)
            slot.Silence();
        _resamplePosition = 1;
        _previous = _current = 0;
    }

    public void Write(int register, byte value)
    {
        register &= 0xFF;
        _registers[register] = value;

        switch (register)
        {
            case 0x01:
                _waveformSelect = (value & 0x20) != 0;
                return;
            case 0x08:
                _noteSelect = (value & 0x40) != 0;
                return;
            case 0xBD:
                _deepTremolo = (value & 0x80) != 0;
                _deepVibrato = (value & 0x40) != 0;
                var wasRhythm = _rhythm;
                _rhythm = (value & 0x20) != 0;
                _rhythmKeys = _rhythm ? (byte)(value & 0x1F) : (byte)0;
                if (wasRhythm != _rhythm || _rhythm)
                    UpdateRhythmKeys();
                return;
        }

        var group = register & 0xE0;
        if (group is 0x20 or 0x40 or 0x60 or 0x80 or 0xE0)
        {
            var offset = register & 0x1F;
            if (offset >= SlotOfOffset.Length || SlotOfOffset[offset] < 0)
                return;
            var slot = _slots[SlotOfOffset[offset]];
            switch (group)
            {
                case 0x20:
                    slot.Tremolo = (value & 0x80) != 0;
                    slot.Vibrato = (value & 0x40) != 0;
                    slot.Sustained = (value & 0x20) != 0;
                    slot.KeyScaleRate = (value & 0x10) != 0;
                    slot.MultiplierX2 = MultiplierTimesTwo[value & 0x0F];
                    break;
                case 0x40:
                    slot.KeyScaleLevel = value >> 6;
                    slot.TotalLevel = value & 0x3F;
                    break;
                case 0x60:
                    slot.AttackRate = value >> 4;
                    slot.DecayRate = value & 0x0F;
                    break;
                case 0x80:
                    var sl = value >> 4;
                    slot.SustainLevel = (sl == 15 ? 31 : sl) << 4;
                    slot.ReleaseRate = value & 0x0F;
                    break;
                case 0xE0:
                    slot.Waveform = value & 0x03;
                    break;
            }
            return;
        }

        var channelIndex = register & 0x0F;
        if (channelIndex > 8)
            return;
        var channel = _channels[channelIndex];
        switch (register & 0xF0)
        {
            case 0xA0:
                channel.FNumber = channel.FNumber & 0x300 | value;
                break;
            case 0xB0:
                channel.FNumber = channel.FNumber & 0xFF | (value & 0x03) << 8;
                channel.Block = (value >> 2) & 0x07;
                channel.KeyOn = (value & 0x20) != 0;
                UpdateKeys(channelIndex);
                break;
            case 0xC0:
                channel.Feedback = (value >> 1) & 0x07;
                channel.Additive = (value & 0x01) != 0;
                break;
        }
    }

    private void UpdateKeys(int channelIndex)
    {
        var channel = _channels[channelIndex];
        channel.Modulator.SetKey(KeySource.Channel, channel.KeyOn);
        channel.Carrier.SetKey(KeySource.Channel, channel.KeyOn);
    }

    private void UpdateRhythmKeys()
    {
        var keys = _rhythmKeys;
        _channels[6].Modulator.SetKey(KeySource.Rhythm, (keys & 0x10) != 0);
        _channels[6].Carrier.SetKey(KeySource.Rhythm, (keys & 0x10) != 0);
        _slots[SlotSnare].SetKey(KeySource.Rhythm, (keys & 0x08) != 0);
        _slots[SlotTom].SetKey(KeySource.Rhythm, (keys & 0x04) != 0);
        _slots[SlotCymbal].SetKey(KeySource.Rhythm, (keys & 0x02) != 0);
        _slots[SlotHiHat].SetKey(KeySource.Rhythm, (keys & 0x01) != 0);
    }

    /// <summary>Füllt den Puffer mit Mono-Abtastwerten der gewünschten Rate.</summary>
    public void Generate(Span<short> output, int sampleRate)
    {
        var step = NativeRate / sampleRate;
        for (var i = 0; i < output.Length; i++)
        {
            while (_resamplePosition >= 1)
            {
                _previous = _current;
                _current = NextSample();
                _resamplePosition -= 1;
            }
            output[i] = (short)Math.Round(_previous + (_current - _previous) * _resamplePosition);
            _resamplePosition += step;
        }
    }

    /// <summary>Ein Abtastwert in der Chiprate (<see cref="NativeRate"/>).</summary>
    public short NextSample()
    {
        _sampleCounter++;
        if ((_sampleCounter & 63) == 0)
            _tremoloPosition = (_tremoloPosition + 1) % 210;
        if ((_sampleCounter & 1023) == 0)
            _vibratoPosition = (_vibratoPosition + 1) & 7;
        var envelopeTick = (_sampleCounter & 1) == 0;
        if (envelopeTick)
            _envelopeCounter++;

        var tremolo = _tremoloPosition < 105 ? _tremoloPosition : 209 - _tremoloPosition;
        tremolo >>= _deepTremolo ? 2 : 4;

        // Phasen und Hüllkurven aller Operatoren fortschreiben
        for (var c = 0; c < 9; c++)
        {
            var channel = _channels[c];
            var fnum = channel.FNumber;
            var keyCode = channel.Block << 1 | (fnum >> (_noteSelect ? 8 : 9)) & 1;
            var ksl = KslTable[fnum >> 6] * 4 - ((8 - channel.Block) << 5);
            if (ksl < 0)
                ksl = 0;
            foreach (var slot in channel.Operators)
            {
                var f = slot.Vibrato ? ApplyVibrato(fnum) : fnum;
                slot.Phase += (uint)((f << channel.Block) * slot.MultiplierX2 >> 2);
                slot.Phase &= 0x7FFFF;
                if (envelopeTick)
                    slot.ClockEnvelope(_envelopeCounter, keyCode);
                slot.LevelOffset = (slot.TotalLevel << 2) + (ksl >> KslShift[slot.KeyScaleLevel]) +
                                   (slot.Tremolo ? tremolo : 0);
            }
        }

        // Rauschgenerator (23 Bit)
        var noiseBit = (_noise ^ (_noise >> 14)) & 1;
        _noise = (_noise >> 1) | (noiseBit << 22);

        var sum = 0;
        var melodicChannels = _rhythm ? 6 : 9;
        for (var c = 0; c < melodicChannels; c++)
            sum += ChannelOutput(_channels[c]);

        if (_rhythm)
            sum += RhythmOutput(_noise & 1);

        return (short)Math.Clamp(sum, short.MinValue, short.MaxValue);
    }

    private int ApplyVibrato(int fnum)
    {
        var range = (fnum >> 7) & 7;
        var pos = _vibratoPosition;
        if ((pos & 3) == 0)
            range = 0;
        else if ((pos & 1) != 0)
            range >>= 1;
        if (!_deepVibrato)
            range >>= 1;
        if ((pos & 4) != 0)
            range = -range;
        return (fnum + range) & 0x3FF;
    }

    private int ChannelOutput(Channel channel)
    {
        var modulator = channel.Modulator;
        var carrier = channel.Carrier;
        var feedback = channel.Feedback == 0 ? 0 : (modulator.Out0 + modulator.Out1) >> (9 - channel.Feedback);
        var modOut = OperatorOutput(modulator, (int)(modulator.Phase >> 9) + feedback);
        modulator.Out1 = modulator.Out0;
        modulator.Out0 = modOut;
        var carOut = OperatorOutput(carrier, (int)(carrier.Phase >> 9) + (channel.Additive ? 0 : modOut));
        return channel.Additive ? modOut + carOut : carOut;
    }

    private int RhythmOutput(uint noise)
    {
        // Bass Drum: Kanal 6 wie ein normaler Kanal, doppelt laut
        var sum = ChannelOutput(_channels[6]) * 2;

        var hihat = _slots[SlotHiHat];
        var cymbal = _slots[SlotCymbal];
        var hhPhase = (int)(hihat.Phase >> 9);
        var tcPhase = (int)(cymbal.Phase >> 9);
        var hhBit2 = (hhPhase >> 2) & 1;
        var hhBit3 = (hhPhase >> 3) & 1;
        var hhBit7 = (hhPhase >> 7) & 1;
        var hhBit8 = (hhPhase >> 8) & 1;
        var tcBit3 = (tcPhase >> 3) & 1;
        var tcBit5 = (tcPhase >> 5) & 1;
        var xor = (hhBit2 ^ hhBit7) | (hhBit3 ^ tcBit5) | (tcBit3 ^ tcBit5);
        var n = (int)noise;

        var hhOut = xor << 9 | ((xor ^ n) != 0 ? 0xD0 : 0x34);
        sum += OperatorOutput(hihat, hhOut) * 2;

        var tom = _slots[SlotTom];
        sum += OperatorOutput(tom, (int)(tom.Phase >> 9)) * 2;

        var sdOut = hhBit8 << 9 | (hhBit8 ^ n) << 8;
        sum += OperatorOutput(_slots[SlotSnare], sdOut) * 2;

        var tcOut = xor << 9 | 0x80;
        sum += OperatorOutput(cymbal, tcOut) * 2;
        return sum;
    }

    private int OperatorOutput(Operator slot, int phase)
    {
        var level = slot.Attenuation + slot.LevelOffset;
        if (level > MaxAttenuation)
            level = MaxAttenuation;
        var waveform = _waveformSelect ? slot.Waveform : 0;
        phase &= 0x3FF;

        int log;
        var negative = false;
        switch (waveform)
        {
            case 0: // Sinus
                log = LogSinAt(phase);
                negative = (phase & 0x200) != 0;
                break;
            case 1: // Halbsinus
                if ((phase & 0x200) != 0)
                    return 0;
                log = LogSinAt(phase);
                break;
            case 2: // Betrag des Sinus
                log = LogSinAt(phase);
                break;
            default: // Viertelsinus-Pulse
                if ((phase & 0x100) != 0)
                    return 0;
                log = LogSin[phase & 0xFF];
                break;
        }

        var total = log + (level << 3);
        if (total > 0x1FFF)
            return 0;
        var value = (Exp[total & 0xFF] << 1) >> (total >> 8);
        return negative ? -value - 1 : value;
    }

    private static int LogSinAt(int phase) =>
        (phase & 0x100) != 0 ? LogSin[(phase & 0xFF) ^ 0xFF] : LogSin[phase & 0xFF];

    private static ushort[] BuildLogSin()
    {
        var table = new ushort[256];
        for (var i = 0; i < 256; i++)
            table[i] = (ushort)Math.Round(-Math.Log2(Math.Sin((i + 0.5) * Math.PI / 512)) * 256);
        return table;
    }

    private static ushort[] BuildExp()
    {
        var table = new ushort[256];
        for (var i = 0; i < 256; i++)
            table[i] = (ushort)Math.Round(1024 * Math.Pow(2, (255 - i) / 256.0));
        return table;
    }

    private enum KeySource
    {
        Channel = 1,
        Rhythm = 2,
    }

    private enum EnvelopeState
    {
        Attack,
        Decay,
        Sustain,
        Release,
    }

    private sealed class Channel(Operator modulator, Operator carrier)
    {
        public Operator Modulator { get; } = modulator;
        public Operator Carrier { get; } = carrier;
        public Operator[] Operators { get; } = [modulator, carrier];
        public int FNumber;
        public int Block;
        public bool KeyOn;
        public int Feedback;
        public bool Additive;
    }

    private sealed class Operator
    {
        public bool Tremolo, Vibrato, Sustained, KeyScaleRate;
        public int MultiplierX2 = 1;
        public int KeyScaleLevel, TotalLevel;
        public int AttackRate, DecayRate, SustainLevel, ReleaseRate;
        public int Waveform;

        public uint Phase;
        public int Attenuation = MaxAttenuation;
        public int LevelOffset;
        public int Out0, Out1;

        private EnvelopeState _state = EnvelopeState.Release;
        private int _keys;

        public void Silence()
        {
            Attenuation = MaxAttenuation;
            _state = EnvelopeState.Release;
            _keys = 0;
            Out0 = Out1 = 0;
        }

        public void SetKey(KeySource source, bool on)
        {
            var before = _keys;
            _keys = on ? _keys | (int)source : _keys & ~(int)source;
            if (before == 0 && _keys != 0)
            {
                Phase = 0;
                _state = EnvelopeState.Attack;
            }
            else if (before != 0 && _keys == 0)
                _state = EnvelopeState.Release;
        }

        public void ClockEnvelope(uint counter, int keyCode)
        {
            var rate = _state switch
            {
                EnvelopeState.Attack => AttackRate,
                EnvelopeState.Decay => DecayRate,
                EnvelopeState.Sustain => Sustained ? 0 : ReleaseRate,
                _ => ReleaseRate,
            };

            if (_state == EnvelopeState.Attack && Attenuation == 0)
            {
                _state = EnvelopeState.Decay;
                return;
            }
            if (_state == EnvelopeState.Decay && Attenuation >= SustainLevel)
            {
                _state = EnvelopeState.Sustain;
                return;
            }

            if (rate == 0)
                return;
            var effective = Math.Min(63, rate * 4 + (KeyScaleRate ? keyCode : keyCode >> 2));
            if (_state == EnvelopeState.Attack && effective >= 60)
            {
                Attenuation = 0; // schnellster Attack: sofort volle Lautstärke
                return;
            }

            var shift = effective >> 2;
            var shifted = counter << shift;
            if ((shifted & 0x7FF) != 0)
                return;
            var selector = (int)(shifted >> (shift <= 11 ? 11 : shift)) & 7;
            var pattern = effective < 48 ? effective & 3 : Math.Min(effective - 44, 16);
            var increment = effective < 4 ? 0 : IncrementPatterns[pattern][selector];
            if (increment == 0)
                return;

            if (_state == EnvelopeState.Attack)
            {
                // [APPROX] exponentieller Anstieg wie beim Chip, Schrittweite genähert
                Attenuation -= Math.Max(1, ((Attenuation + 1) * increment) >> 3);
                if (Attenuation < 0)
                    Attenuation = 0;
            }
            else
            {
                Attenuation = Math.Min(MaxAttenuation, Attenuation + increment);
            }
        }
    }
}
