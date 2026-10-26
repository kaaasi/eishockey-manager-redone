namespace EishockeyManager.Audio;

/// <summary>Ton des Spiels, wie ihn Bildschirme und Abläufe benutzen.</summary>
public interface IGameAudio
{
    /// <summary>Spielt einen Effekt; ein laufender Effekt wird ersetzt (ein Kanal wie im Original).</summary>
    void PlayEffect(SoundEffect effect);

    void StopEffect();

    /// <summary>Laufender Effekt, null wenn der Kanal frei ist.</summary>
    SoundEffect? CurrentEffect { get; }

    void PlayMusic();

    void StopMusic();

    bool IsMusicPlaying { get; }

    /// <summary>F8: Ton aus/an.</summary>
    void ToggleMute();

    bool Muted { get; }
}

/// <summary>
/// Zugang zum Ton für alle Programmteile. Ohne Soundausgabe (Tests, emtool, kein Gerät) ist
/// <see cref="Current"/> ein stummer Ersatz, Aufrufe sind dann wirkungslos.
/// </summary>
public static class GameAudio
{
    private static IGameAudio _current = new SilentAudio();

    public static IGameAudio Current
    {
        get => _current;
        set => _current = value ?? new SilentAudio();
    }

    private sealed class SilentAudio : IGameAudio
    {
        public void PlayEffect(SoundEffect effect) { }
        public void StopEffect() { }
        public SoundEffect? CurrentEffect => null;
        public void PlayMusic() => IsMusicPlaying = true;
        public void StopMusic() => IsMusicPlaying = false;
        public bool IsMusicPlaying { get; private set; }
        public void ToggleMute() => Muted = !Muted;
        public bool Muted { get; private set; }
    }
}
