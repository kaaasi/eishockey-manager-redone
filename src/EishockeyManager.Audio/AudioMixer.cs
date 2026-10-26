using EishockeyManager.Formats;

namespace EishockeyManager.Audio;

/// <summary>
/// Mischt Musik und Soundeffekte zu Mono-PCM (16 Bit). Plattformunabhängig – die Ausgabe an die Soundkarte
/// übernimmt das Fenster (Desktop), Tests lesen <see cref="Read"/> direkt.
/// </summary>
/// <remarks>
/// [ORIGINAL] Wie beim Sound Blaster mit CT-VOICE gibt es <b>einen</b> Effektkanal: ein neuer Effekt beendet
/// den laufenden (die Torszene ruft vor jedem Effekt „Stopp“, 99F:002E). Effekte mit Endlosschleife (kb\237)
/// laufen, bis sie ersetzt oder gestoppt werden. Musik läuft auf dem FM-Chip unabhängig davon.
/// [APPROX] Lautstärke 0–10 wirkt linear auf die Amplitude (das Original kennt keine Lautstärkeregler).
/// Methoden sind threadsicher, weil die Soundausgabe Puffer aus einem anderen Thread anfordern kann.
/// </remarks>
public sealed class AudioMixer : IGameAudio
{
    public const int DefaultSampleRate = 44100;
    public const int MaxVolume = 10;

    private readonly object _lock = new();
    private readonly SoundBank _bank;
    private readonly CmfPlayer? _music;
    private short[] _musicBuffer = [];

    private PcmSound? _effect;
    private int _effectPosition;
    private SoundEffect? _currentEffect;

    private int _effectsVolume = MaxVolume;
    private int _musicVolume = MaxVolume;
    private bool _musicPlaying;
    private bool _muted;

    public AudioMixer(SoundBank bank)
    {
        _bank = bank;
        if (bank.Music is { } music)
            _music = new CmfPlayer(music, bank.SampleRate, loop: true); // [APPROX] Musik wiederholt sich
    }

    public int SampleRate => _bank.SampleRate;

    public int EffectsVolume
    {
        get { lock (_lock) return _effectsVolume; }
        set { lock (_lock) _effectsVolume = Math.Clamp(value, 0, MaxVolume); }
    }

    public int MusicVolume
    {
        get { lock (_lock) return _musicVolume; }
        set { lock (_lock) _musicVolume = Math.Clamp(value, 0, MaxVolume); }
    }

    public bool Muted
    {
        get { lock (_lock) return _muted; }
        set { lock (_lock) _muted = value; }
    }

    public bool IsMusicPlaying
    {
        get { lock (_lock) return _musicPlaying; }
    }

    /// <summary>Laufender Effekt (null = Effektkanal frei, entspricht CT-VOICE-Status 0).</summary>
    public SoundEffect? CurrentEffect
    {
        get { lock (_lock) return _currentEffect; }
    }

    public void PlayEffect(SoundEffect effect)
    {
        lock (_lock)
        {
            if (!_bank.Has(effect))
                return;
            _effect = _bank[effect];
            _effectPosition = 0;
            _currentEffect = effect;
        }
    }

    public void StopEffect()
    {
        lock (_lock)
        {
            _effect = null;
            _currentEffect = null;
        }
    }

    /// <summary>Startet die Musik (von vorn, falls sie nicht läuft).</summary>
    public void PlayMusic()
    {
        lock (_lock)
        {
            if (_music is null || _musicPlaying)
                return;
            _music.Restart();
            _musicPlaying = true;
        }
    }

    public void StopMusic()
    {
        lock (_lock)
            _musicPlaying = false;
    }

    public void ToggleMute()
    {
        lock (_lock)
            _muted = !_muted;
    }

    /// <summary>Füllt den Puffer mit der Mischung. Auch stumm laufen Musik und Effekte weiter.</summary>
    public void Read(Span<short> output)
    {
        lock (_lock)
        {
            if (_musicBuffer.Length < output.Length)
                _musicBuffer = new short[output.Length];
            var music = _musicBuffer.AsSpan(0, output.Length);
            if (_musicPlaying && _music is not null)
            {
                var n = _music.Read(music);
                music[n..].Clear();
            }
            else
                music.Clear();

            var musicGain = _muted ? 0 : _musicVolume;
            var effectGain = _muted ? 0 : _effectsVolume;
            for (var i = 0; i < output.Length; i++)
            {
                var sum = music[i] * musicGain + NextEffectSample() * effectGain;
                output[i] = (short)Math.Clamp(sum / MaxVolume, short.MinValue, short.MaxValue);
            }
        }
    }

    private int NextEffectSample()
    {
        if (_effect is null)
            return 0;
        if (_effectPosition >= _effect.Samples.Length)
        {
            if (_effect.HasLoop)
                _effectPosition = _effect.LoopStart;
            else
            {
                _effect = null;
                _currentEffect = null;
                return 0;
            }
        }
        var sample = _effect.Samples[_effectPosition++];
        if (_effect.HasLoop && _effectPosition >= _effect.LoopStart + _effect.LoopLength)
            _effectPosition = _effect.LoopStart;
        return sample;
    }
}
