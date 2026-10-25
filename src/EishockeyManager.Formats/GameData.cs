namespace EishockeyManager.Formats;

/// <summary>
/// Zugriff auf die Originaldateien der DOS-Version. Die Dateien liegen nicht im Repo,
/// sondern werden vom Nutzer bereitgestellt (Standard: Ordner "gamedata" im Repo-Stamm).
/// </summary>
public sealed class GameData
{
    public const string FolderName = "gamedata";

    public GameData(string rootDirectory)
    {
        RootDirectory = rootDirectory;
        if (!Directory.Exists(GraphicsDirectory))
            throw new DirectoryNotFoundException(
                $"Im Ordner '{rootDirectory}' fehlt das Unterverzeichnis 'kb'. " +
                "Bitte die Dateien der DOS-Version dorthin kopieren.");
    }

    public string RootDirectory { get; }
    public string GraphicsDirectory => Path.Combine(RootDirectory, "kb");
    public string GoalScenesDirectory => Path.Combine(RootDirectory, "tore");

    /// <summary>Standardpalette des Spiels (kb\21.vga, 8-Bit-Werte).</summary>
    public Palette LoadDefaultPalette()
    {
        var path = GraphicsPath(21);
        return File.Exists(path) ? Palette.Load(path) : Palette.Grayscale();
    }

    public string GraphicsPath(int number) => Path.Combine(GraphicsDirectory, $"{number}.vga");

    /// <summary>Nummern aller .vga-Dateien, aufsteigend sortiert.</summary>
    public IReadOnlyList<int> GraphicsNumbers() =>
        Directory.EnumerateFiles(GraphicsDirectory, "*.vga")
            .Select(p => int.TryParse(Path.GetFileNameWithoutExtension(p), out var n) ? n : -1)
            .Where(n => n >= 0)
            .Order()
            .ToList();

    /// <summary>
    /// Sucht den Spielordner: zuerst das übergebene Argument, dann "gamedata"
    /// im aktuellen Verzeichnis und in allen übergeordneten Verzeichnissen des Programms.
    /// </summary>
    public static GameData Locate(string? explicitPath = null)
    {
        if (!string.IsNullOrWhiteSpace(explicitPath))
            return new GameData(Path.GetFullPath(explicitPath));

        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var dir = new DirectoryInfo(start);
            while (dir is not null)
            {
                var candidate = Path.Combine(dir.FullName, FolderName);
                if (Directory.Exists(Path.Combine(candidate, "kb")))
                    return new GameData(candidate);
                dir = dir.Parent;
            }
        }

        throw new DirectoryNotFoundException(
            $"Kein '{FolderName}'-Ordner mit den Originaldateien gefunden. " +
            "Siehe README, Abschnitt \"Originaldateien bereitstellen\".");
    }
}
