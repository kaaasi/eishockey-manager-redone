using EishockeyManager.Formats;

namespace EishockeyManager.Tools;

/// <summary><c>emtool export-sound</c>: Soundeffekte (VOC) als WAV-Dateien.</summary>
internal static class SoundExport
{
    /// <summary>
    /// Effekte des Originals. [APPROX] Die Namen sind nach Gehör vergeben (siehe docs/formate/dateiformate.md),
    /// die Nummern stehen fest.
    /// </summary>
    public static readonly (int Number, string Name)[] Effects =
    [
        (237, "publikum"),
        (238, "jubel"),
        (239, "pfiff"),
        (240, "puck"),
        (241, "check"),
    ];

    public static void Run(GameData game, string targetDirectory)
    {
        Directory.CreateDirectory(targetDirectory);
        foreach (var (number, name) in Effects)
        {
            var path = game.GraphicsPath(number);
            if (!File.Exists(path))
            {
                Console.WriteLine($"{number}.vga fehlt – übersprungen.");
                continue;
            }
            var sound = VocFile.Load(path).Decode();
            var file = Path.Combine(targetDirectory, $"{number}_{name}.wav");
            WavWriter.Save(file, sound);
            Console.WriteLine($"{Path.GetFileName(file),-18} {sound.SampleRate,6} Hz  {sound.Duration.TotalSeconds,5:0.00} s" +
                              (sound.HasLoop ? "  (Schleife)" : ""));
        }
        Console.WriteLine($"Ziel: {Path.GetFullPath(targetDirectory)}");
    }
}
