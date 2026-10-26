namespace EishockeyManager.Formats;

/// <summary>
/// Dekodierter Ton: Mono, 16 Bit vorzeichenbehaftet, feste Abtastrate.
/// Gemeinsames Ergebnis von <see cref="VocFile"/> und der CMF-Wiedergabe.
/// </summary>
/// <param name="SampleRate">Abtastrate in Hz.</param>
/// <param name="Samples">Abtastwerte (Mono, 16 Bit).</param>
/// <param name="LoopStart">Beginn der Endlosschleife in Abtastwerten, falls vorhanden.</param>
/// <param name="LoopLength">Länge der Endlosschleife in Abtastwerten (0 = keine Schleife).</param>
/// <param name="SourceBits">Bittiefe der Quelle (8 oder 16) – bestimmt die verlustfreie WAV-Ausgabe.</param>
public sealed record PcmSound(int SampleRate, short[] Samples, int LoopStart = 0, int LoopLength = 0, int SourceBits = 16)
{
    public bool HasLoop => LoopLength > 0;

    public TimeSpan Duration => TimeSpan.FromSeconds((double)Samples.Length / SampleRate);
}
