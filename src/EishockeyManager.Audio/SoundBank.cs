using EishockeyManager.Formats;

namespace EishockeyManager.Audio;

/// <summary>
/// Die Klänge des Spiels, auf die Ausgaberate umgerechnet: fünf Effekte (VOC) und die Musik (CMF).
/// </summary>
public sealed class SoundBank
{
    public const int MusicFileNumber = 248;

    private readonly Dictionary<SoundEffect, PcmSound> _effects;

    public SoundBank(int sampleRate, IReadOnlyDictionary<SoundEffect, PcmSound> effects, CmfFile? music)
    {
        SampleRate = sampleRate;
        _effects = effects.ToDictionary(e => e.Key, e => Resample(e.Value, sampleRate));
        Music = music;
    }

    public int SampleRate { get; }

    /// <summary>Musik kb\248, falls vorhanden.</summary>
    public CmfFile? Music { get; }

    public bool Has(SoundEffect effect) => _effects.ContainsKey(effect);

    public PcmSound this[SoundEffect effect] => _effects[effect];

    /// <summary>Lädt alle vorhandenen Klänge; fehlende Dateien werden ausgelassen.</summary>
    public static SoundBank Load(GameData game, int sampleRate)
    {
        var effects = new Dictionary<SoundEffect, PcmSound>();
        foreach (var effect in SoundEffects.All)
        {
            var path = game.GraphicsPath(effect.FileNumber());
            if (File.Exists(path))
                effects[effect] = VocFile.Load(path).Decode();
        }
        var musicPath = game.GraphicsPath(MusicFileNumber);
        var music = File.Exists(musicPath) ? CmfFile.Load(musicPath) : null;
        return new SoundBank(sampleRate, effects, music);
    }

    /// <summary>Lineare Umrechnung auf die Zielrate; die Schleife wird mitgerechnet.</summary>
    public static PcmSound Resample(PcmSound sound, int targetRate)
    {
        if (sound.SampleRate == targetRate || sound.Samples.Length == 0)
            return sound;
        var ratio = (double)targetRate / sound.SampleRate;
        var length = (int)Math.Round(sound.Samples.Length * ratio);
        var result = new short[length];
        var step = 1 / ratio;
        var source = sound.Samples;
        for (var i = 0; i < length; i++)
        {
            var pos = i * step;
            var index = Math.Min((int)pos, source.Length - 1);
            var next = Math.Min(index + 1, source.Length - 1);
            var frac = pos - (int)pos;
            result[i] = (short)Math.Round(source[index] + (source[next] - source[index]) * frac);
        }
        var loopStart = (int)Math.Round(sound.LoopStart * ratio);
        var loopLength = sound.HasLoop ? Math.Min(length - loopStart, (int)Math.Round(sound.LoopLength * ratio)) : 0;
        return new PcmSound(targetRate, result, loopStart, loopLength, sound.SourceBits);
    }
}
