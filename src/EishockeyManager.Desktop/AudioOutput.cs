using EishockeyManager.Audio;
using EishockeyManager.Formats;
using Microsoft.Xna.Framework.Audio;

namespace EishockeyManager.Desktop;

/// <summary>
/// Gibt den <see cref="AudioMixer"/> über MonoGame aus (<see cref="DynamicSoundEffectInstance"/>, Mono, 44,1 kHz).
/// Ohne Soundgerät läuft das Spiel stumm weiter.
/// </summary>
internal sealed class AudioOutput : IDisposable
{
    private const int BufferSamples = 2048;   // ≈ 46 ms
    private const int QueuedBuffers = 3;

    private readonly AudioMixer _mixer;
    private readonly DynamicSoundEffectInstance? _instance;
    private readonly short[] _samples = new short[BufferSamples];
    private readonly byte[] _bytes = new byte[BufferSamples * 2];

    private AudioOutput(AudioMixer mixer, DynamicSoundEffectInstance? instance)
    {
        _mixer = mixer;
        _instance = instance;
    }

    public AudioMixer Mixer => _mixer;

    public bool Available => _instance is not null;

    /// <summary>Lädt die Klänge und öffnet die Ausgabe; setzt <see cref="GameAudio.Current"/>.</summary>
    public static AudioOutput Create(GameData game)
    {
        var mixer = new AudioMixer(EishockeyManager.Audio.SoundBank.Load(game, AudioMixer.DefaultSampleRate));
        DynamicSoundEffectInstance? instance = null;
        try
        {
            instance = new DynamicSoundEffectInstance(AudioMixer.DefaultSampleRate, AudioChannels.Mono);
        }
        catch (Exception ex) when (ex is NoAudioHardwareException or InvalidOperationException or DllNotFoundException
                                       or TypeInitializationException)
        {
            Console.Error.WriteLine($"Keine Soundausgabe: {ex.Message}");
        }

        GameAudio.Current = mixer;
        var output = new AudioOutput(mixer, instance);
        output.Pump();
        instance?.Play();
        return output;
    }

    /// <summary>Hält genug Puffer in der Warteschlange; einmal je Frame aufrufen.</summary>
    public void Pump()
    {
        if (_instance is null)
            return;
        while (_instance.PendingBufferCount < QueuedBuffers)
        {
            _mixer.Read(_samples);
            Buffer.BlockCopy(_samples, 0, _bytes, 0, _bytes.Length);
            _instance.SubmitBuffer(_bytes);
        }
    }

    public void Dispose()
    {
        if (ReferenceEquals(GameAudio.Current, _mixer))
            GameAudio.Current = null!;
        _instance?.Stop();
        _instance?.Dispose();
    }
}
