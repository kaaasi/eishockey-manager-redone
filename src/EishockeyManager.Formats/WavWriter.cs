using System.Buffers.Binary;

namespace EishockeyManager.Formats;

/// <summary>Schreibt <see cref="PcmSound"/> als RIFF-WAVE (PCM, Mono, 8 oder 16 Bit).</summary>
public static class WavWriter
{
    /// <summary>
    /// Schreibt die Datei. Ohne Angabe wird die Bittiefe der Quelle verwendet (8-Bit-VOC bleibt verlustfrei 8 Bit).
    /// Eine Endlosschleife wird als <c>smpl</c>-Block vermerkt, damit Wiedergabeprogramme sie erkennen.
    /// </summary>
    public static void Save(string path, PcmSound sound, int? bitsPerSample = null)
    {
        using var stream = File.Create(path);
        Write(stream, sound, bitsPerSample);
    }

    public static byte[] ToBytes(PcmSound sound, int? bitsPerSample = null)
    {
        using var stream = new MemoryStream();
        Write(stream, sound, bitsPerSample);
        return stream.ToArray();
    }

    public static void Write(Stream stream, PcmSound sound, int? bitsPerSample = null)
    {
        var bits = bitsPerSample ?? sound.SourceBits;
        if (bits is not (8 or 16))
            throw new ArgumentOutOfRangeException(nameof(bitsPerSample), "Nur 8 oder 16 Bit.");

        var bytesPerSample = bits / 8;
        var dataLength = sound.Samples.Length * bytesPerSample;
        var data = new byte[dataLength + (dataLength & 1)];
        for (var i = 0; i < sound.Samples.Length; i++)
        {
            if (bits == 8)
                data[i] = (byte)((sound.Samples[i] >> 8) + 128);
            else
                BinaryPrimitives.WriteInt16LittleEndian(data.AsSpan(i * 2), sound.Samples[i]);
        }

        var smpl = sound.HasLoop ? SamplerChunk(sound) : [];
        var riffLength = 4 + (8 + 16) + (8 + data.Length) + smpl.Length;

        using var w = new BinaryWriter(stream, System.Text.Encoding.ASCII, leaveOpen: true);
        w.Write("RIFF"u8);
        w.Write(riffLength);
        w.Write("WAVE"u8);
        w.Write("fmt "u8);
        w.Write(16);
        w.Write((short)1);                               // PCM
        w.Write((short)1);                               // Mono
        w.Write(sound.SampleRate);
        w.Write(sound.SampleRate * bytesPerSample);      // Bytes je Sekunde
        w.Write((short)bytesPerSample);                  // Blockausrichtung
        w.Write((short)bits);
        w.Write("data"u8);
        w.Write(dataLength);
        w.Write(data);
        w.Write(smpl);
    }

    private static byte[] SamplerChunk(PcmSound sound)
    {
        var chunk = new byte[8 + 36 + 24];
        var s = chunk.AsSpan();
        "smpl"u8.CopyTo(s);
        BinaryPrimitives.WriteInt32LittleEndian(s[4..], 36 + 24);
        BinaryPrimitives.WriteInt32LittleEndian(s[(8 + 8)..], (int)(1_000_000_000L / sound.SampleRate)); // Periode in ns
        BinaryPrimitives.WriteInt32LittleEndian(s[(8 + 12)..], 60);                                      // MIDI-Grundton
        BinaryPrimitives.WriteInt32LittleEndian(s[(8 + 28)..], 1);                                       // eine Schleife
        var loop = s[(8 + 36)..];
        BinaryPrimitives.WriteInt32LittleEndian(loop[8..], sound.LoopStart);
        BinaryPrimitives.WriteInt32LittleEndian(loop[12..], sound.LoopStart + sound.LoopLength - 1);     // inklusive
        return chunk;                                                                                     // Typ 0, Anzahl 0 = endlos
    }
}
