using EishockeyManager.Audio;
using EishockeyManager.Formats;

namespace EishockeyManager.Audio.Tests;

public class SoundEffectTests
{
    [Theory]
    [InlineData(SoundEffect.Crowd, 237)]
    [InlineData(SoundEffect.Cheer, 238)]
    [InlineData(SoundEffect.Whistle, 239)]
    [InlineData(SoundEffect.Puck, 240)]
    [InlineData(SoundEffect.Check, 241)]
    public void FileNumbers(SoundEffect effect, int file) => Assert.Equal(file, effect.FileNumber());

    [Theory]
    [InlineData(0, null)]
    [InlineData(1, SoundEffect.Cheer)]
    [InlineData(3, SoundEffect.Puck)]
    [InlineData(4, SoundEffect.Check)]
    [InlineData(5, null)]
    [InlineData(-1, null)]
    public void FromSceneIndex(int index, SoundEffect? expected) => Assert.Equal(expected, SoundEffects.FromSceneIndex(index));
}

public class SoundBankTests
{
    [Fact]
    public void Resample_ScalesLengthAndLoop()
    {
        var source = new PcmSound(5000, Enumerable.Range(0, 100).Select(i => (short)(i * 100)).ToArray(), 20, 50, 8);
        var result = SoundBank.Resample(source, 10000);
        Assert.Equal(10000, result.SampleRate);
        Assert.Equal(200, result.Samples.Length);
        Assert.Equal((40, 100), (result.LoopStart, result.LoopLength));
        Assert.Equal(source.Samples[10], result.Samples[20]);
        Assert.Equal((short)1050, result.Samples[21]); // Mitte zwischen 1000 und 1100
    }

    [Fact]
    public void Resample_SameRateKeepsInstance()
    {
        var source = new PcmSound(44100, [1, 2, 3]);
        Assert.Same(source, SoundBank.Resample(source, 44100));
    }
}

public class AudioMixerTests
{
    internal const int Rate = 8000;

    internal static AudioMixer Mixer(CmfFile? music = null) => new(new SoundBank(Rate, new Dictionary<SoundEffect, PcmSound>
    {
        [SoundEffect.Crowd] = new(Rate, Enumerable.Repeat((short)1000, 10).ToArray(), 0, 10),   // Schleife
        [SoundEffect.Puck] = new(Rate, Enumerable.Repeat((short)2000, 5).ToArray()),
        [SoundEffect.Cheer] = new(Rate, Enumerable.Repeat((short)30000, 5).ToArray()),
    }, music));

    private static short[] Read(AudioMixer mixer, int count)
    {
        var buffer = new short[count];
        mixer.Read(buffer);
        return buffer;
    }

    [Fact]
    public void Silent_WithoutAnything() => Assert.All(Read(Mixer(), 100), s => Assert.Equal(0, s));

    [Fact]
    public void Effect_PlaysOnceThenChannelIsFree()
    {
        var mixer = Mixer();
        mixer.PlayEffect(SoundEffect.Puck);
        Assert.Equal(SoundEffect.Puck, mixer.CurrentEffect);
        var output = Read(mixer, 8);
        Assert.Equal(new short[] { 2000, 2000, 2000, 2000, 2000, 0, 0, 0 }, output);
        Assert.Null(mixer.CurrentEffect);
    }

    [Fact]
    public void Effect_LoopRunsUntilReplaced()
    {
        var mixer = Mixer();
        mixer.PlayEffect(SoundEffect.Crowd);
        Assert.All(Read(mixer, 35), s => Assert.Equal(1000, s));
        Assert.Equal(SoundEffect.Crowd, mixer.CurrentEffect);

        // Ein neuer Effekt ersetzt die Schleife (ein Kanal wie CT-VOICE)
        mixer.PlayEffect(SoundEffect.Puck);
        Assert.Equal(new short[] { 2000, 2000, 2000, 2000, 2000, 0 }, Read(mixer, 6));
    }

    [Fact]
    public void StopEffect_FreesChannel()
    {
        var mixer = Mixer();
        mixer.PlayEffect(SoundEffect.Crowd);
        mixer.StopEffect();
        Assert.Null(mixer.CurrentEffect);
        Assert.All(Read(mixer, 10), s => Assert.Equal(0, s));
    }

    [Fact]
    public void MissingEffect_IsIgnored()
    {
        var mixer = Mixer();
        mixer.PlayEffect(SoundEffect.Whistle);
        Assert.Null(mixer.CurrentEffect);
    }

    [Theory]
    [InlineData(10, 2000)]
    [InlineData(5, 1000)]
    [InlineData(0, 0)]
    [InlineData(15, 2000)] // auf 10 begrenzt
    public void EffectsVolume_ScalesLinearly(int volume, int expected)
    {
        var mixer = Mixer();
        mixer.EffectsVolume = volume;
        mixer.PlayEffect(SoundEffect.Puck);
        Assert.Equal(expected, Read(mixer, 1)[0]);
    }

    [Fact]
    public void Mute_SilencesButKeepsPosition()
    {
        var mixer = Mixer();
        mixer.PlayEffect(SoundEffect.Puck);
        mixer.ToggleMute();
        Assert.True(mixer.Muted);
        Assert.All(Read(mixer, 3), s => Assert.Equal(0, s));
        mixer.ToggleMute();
        Assert.Equal(new short[] { 2000, 2000, 0 }, Read(mixer, 3)); // drei Werte liefen stumm weiter
    }

    [Fact]
    public void Mixing_ClipsInsteadOfWrapping()
    {
        var mixer = Mixer(SyntheticMusic());
        mixer.PlayMusic();
        Read(mixer, Rate / 10);                 // Musik einschwingen lassen
        mixer.PlayEffect(SoundEffect.Cheer);    // 30 000 + Musik > 32 767
        var output = Read(mixer, 5);
        Assert.All(output, s => Assert.True(s >= 0));
    }

    [Fact]
    public void Music_PlaysAndStops()
    {
        var mixer = Mixer(SyntheticMusic());
        Assert.False(mixer.IsMusicPlaying);
        mixer.PlayMusic();
        Assert.True(mixer.IsMusicPlaying);
        Assert.Contains(Read(mixer, Rate / 4), s => Math.Abs((int)s) > 500);

        mixer.MusicVolume = 0;
        Assert.All(Read(mixer, 100), s => Assert.Equal(0, s));

        mixer.MusicVolume = 10;
        mixer.StopMusic();
        Assert.All(Read(mixer, 100), s => Assert.Equal(0, s));
    }

    [Fact]
    public void Music_LoopsAfterEnd()
    {
        var mixer = Mixer(SyntheticMusic());
        mixer.PlayMusic();
        Read(mixer, Rate * 3); // Lied ist 1 s lang
        Assert.True(mixer.IsMusicPlaying);
        Assert.Contains(Read(mixer, Rate / 4), s => Math.Abs((int)s) > 500);
    }

    /// <summary>Eine Sekunde A4 mit einer Orgelstimme.</summary>
    internal static CmfFile SyntheticMusic()
    {
        var data = new byte[40 + 16 + 12];
        "CTMF"u8.CopyTo(data);
        data[4] = 0x01; data[5] = 0x01;
        data[6] = 40;                   // Instrumente
        data[8] = 56;                   // Musik
        data[10] = 48;
        data[12] = 96;                  // Ticks je Sekunde
        data[36] = 1;                   // ein Instrument
        byte[] organ = [0x21, 0x21, 0x3F, 0x00, 0xF0, 0xF0, 0x00, 0x0F, 0, 0, 0x01];
        organ.CopyTo(data, 40);
        byte[] music = [0x00, 0x90, 69, 127, 0x00, 0xFF, 0x2F, 0x00];
        music.CopyTo(data, 56);
        // Ende nach 96 Ticks: Zeitdifferenz vor FF 2F
        data[56 + 4] = 96;
        return CmfFile.Parse(data.AsSpan(0, 56 + music.Length));
    }
}

public class GameAudioTests
{
    [Fact]
    public void Default_IsSilentAndSafe()
    {
        var previous = GameAudio.Current;
        try
        {
            GameAudio.Current = null!;
            GameAudio.Current.PlayEffect(SoundEffect.Puck);
            Assert.Null(GameAudio.Current.CurrentEffect);
            GameAudio.Current.ToggleMute();
            Assert.True(GameAudio.Current.Muted);
        }
        finally
        {
            GameAudio.Current = previous;
        }
    }

    [Fact]
    public void Current_CanBeReplaced()
    {
        var previous = GameAudio.Current;
        try
        {
            var mixer = AudioMixerTests.Mixer();
            GameAudio.Current = mixer;
            GameAudio.Current.PlayEffect(SoundEffect.Puck);
            Assert.Equal(SoundEffect.Puck, mixer.CurrentEffect);
        }
        finally
        {
            GameAudio.Current = previous;
        }
    }
}

/// <summary>Belege aus den Originaldateien (übersprungen ohne „gamedata“).</summary>
public class SoundOriginalTests
{
    private static GameData? Game()
    {
        try { return GameData.Locate(); }
        catch (DirectoryNotFoundException) { return null; }
    }

    /// <summary>
    /// Byte 3 jedes 28-Byte-Bildes einer Torszene ist der Effekt. Jubel (1) fast nur in Szenen mit Treffer (*.at/*.ht…),
    /// Pfiff (2) nie, Puck (3) am häufigsten.
    /// </summary>
    [Fact]
    public void GoalScenes_UseEffectIndices()
    {
        if (Game() is not { } game)
            return;
        var counts = new int[256];
        var cheerFiles = new List<string>();
        foreach (var path in Directory.EnumerateFiles(game.GoalScenesDirectory))
        {
            var data = File.ReadAllBytes(path);
            var name = Path.GetFileName(path);
            for (var p = 0x54; p + 28 <= data.Length && !(data[p] == 0xAA && data[p + 1] == 0xAA); p += 28)
            {
                counts[data[p + 3]]++;
                if (data[p + 3] == (int)SoundEffect.Cheer)
                    cheerFiles.Add(name);
            }
        }
        Assert.Equal(0, counts[(int)SoundEffect.Whistle]);
        Assert.All(counts.Skip(5), c => Assert.Equal(0, c));
        Assert.Equal(149, counts[(int)SoundEffect.Puck]);
        Assert.Equal(53, counts[(int)SoundEffect.Check]);
        Assert.Equal(25, counts[(int)SoundEffect.Cheer]);
        // 24 von 25 am Ende einer Szene mit Treffer (*.at/*.ht…), einmal in 4.hv
        Assert.Equal(24, cheerFiles.Count(f => System.Text.RegularExpressions.Regex.IsMatch(f, @"^\d+\.[ah]t[ej]?$")));
    }

    /// <summary>Die Torszene lädt kb\(231 + i) und spielt Effekt s aus Tabelleneintrag 6 + s.</summary>
    [Fact]
    public void EmExe_LoadsSceneFilesFrom231()
    {
        if (Game() is not { } game)
            return;
        var exe = File.ReadAllBytes(Path.Combine(game.RootDirectory, "em.exe"));
        Assert.Equal(new byte[] { 0x04, 0xE7 }, exe[0x568C2..0x568C4]);             // add al, 231
        Assert.Equal(new byte[] { 0x89, 0x87, 0xB9, 0xE2 }, exe[0x56934..0x56938]); // Tabelle 0xE2B9
        Assert.Equal(new byte[] { 0xFF, 0xB7, 0xD1, 0xE2 }, exe[0x56D65..0x56D69]); // Effekt = Tabelle 0xE2D1 + 4·s
        Assert.Equal(0xE2B9 + 6 * 4, 0xE2D1);
    }

    [Fact]
    public void SoundBank_LoadsAllOriginalSounds()
    {
        if (Game() is not { } game)
            return;
        var bank = SoundBank.Load(game, AudioMixer.DefaultSampleRate);
        Assert.All(SoundEffects.All, e => Assert.True(bank.Has(e)));
        Assert.True(bank[SoundEffect.Crowd].HasLoop);
        Assert.Equal(1.96, bank[SoundEffect.Crowd].Duration.TotalSeconds, 0.01);
        Assert.NotNull(bank.Music);
    }
}
