using System.Text;
using EishockeyManager.Core;

namespace EishockeyManager.UI.Screens.SaveLoad;

/// <summary>Art eines Eintrags in der Dateiliste des Laden/Sichern-Dialogs.</summary>
public enum SaveEntryKind
{
    /// <summary>Unterverzeichnis (grau, ohne Wirkung).</summary>
    Directory,

    /// <summary>Spielstand (<c>*.ems</c>, gelb, wählbar).</summary>
    Save,

    /// <summary>Andere Datei (grau, ohne Wirkung).</summary>
    Other,
}

/// <summary>Eintrag der Dateiliste: Dateiname (wie auf der Platte) und Art.</summary>
public sealed record SaveEntry(string FileName, SaveEntryKind Kind)
{
    /// <summary>Anzeigename wie im Original (DOS): Großbuchstaben, höchstens 12 Zeichen.</summary>
    public string DisplayName
    {
        get
        {
            var text = FileName.ToUpperInvariant();
            return text.Length <= SaveFiles.MaxDisplayLength ? text : text[..SaveFiles.MaxDisplayLength];
        }
    }

    /// <summary>Dateiname ohne Endung (nur bei Spielständen sinnvoll).</summary>
    public string BaseName => Path.GetFileNameWithoutExtension(FileName).ToUpperInvariant();
}

/// <summary>
/// Dateien der Spielstände: Namensregeln, Dateiliste, sicheres Schreiben. Kennt weder Bildschirm noch Spielzustand.
/// </summary>
/// <remarks>
/// [APPROX] Das Original (DOS) nennt Spielstände <c>*.HOK</c> mit 8.3-Namen und lässt Laufwerk und Verzeichnis
/// wählen. Hier liegen alle Spielstände in einem Ordner (siehe <see cref="SaveContext"/>); Namen folgen der
/// DOS-Regel (höchstens 8 Zeichen, Großbuchstaben, Ziffern, <c>_</c> und <c>-</c>), damit sie auf allen
/// Betriebssystemen gültig sind und im Dialog wie im Original aussehen.
/// </remarks>
public static class SaveFiles
{
    /// <summary>Längster Name ohne Endung (DOS 8.3).</summary>
    public const int MaxNameLength = 8;

    /// <summary>Längste Anzeige in der Dateiliste (8.3 = 12 Zeichen).</summary>
    public const int MaxDisplayLength = 12;

    /// <summary>Dateiname des automatischen Speicherns. [ORIGINAL] em.exe DGROUP 0x5231 „autosave.hok“ (hier mit eigener Endung).</summary>
    public const string AutoSaveBaseName = "AUTOSAVE";

    private static readonly string[] Reserved =
        ["CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
         "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"];

    /// <summary>Ist das Zeichen in einem Namen erlaubt?</summary>
    public static bool IsAllowed(char c) => c is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '_' or '-';

    /// <summary>
    /// Gültiger Name ohne Endung in Großbuchstaben oder <c>null</c>. Eine getippte Endung <c>.ems</c> wird ignoriert,
    /// Leerzeichen am Rand ebenso.
    /// </summary>
    public static string? NormalizeName(string? typed)
    {
        if (typed is null)
            return null;
        var text = typed.Trim();
        if (text.EndsWith(SaveGameExtension, StringComparison.OrdinalIgnoreCase))
            text = text[..^SaveGameExtension.Length];
        if (text.Length is 0 or > MaxNameLength || !text.All(IsAllowed))
            return null;
        text = text.ToUpperInvariant();
        return Reserved.Contains(text) ? null : text;
    }

    private static string SaveGameExtension => SaveGame.FileExtension;

    /// <summary>Dateiname auf der Platte zu einem gültigen Namen (<c>TEST1</c> → <c>TEST1.ems</c>).</summary>
    public static string FileNameOf(string baseName) => baseName + SaveGameExtension;

    /// <summary>
    /// Liest die Dateiliste wie im Original: zuerst Verzeichnisse, dann Dateien, je nach Name sortiert;
    /// Spielstände sind wählbar, alles andere nur zur Ansicht. Ein fehlender Ordner ergibt eine leere Liste.
    /// </summary>
    public static IReadOnlyList<SaveEntry> List(string directory)
    {
        if (!Directory.Exists(directory))
            return [];
        try
        {
            var directories = Directory.EnumerateDirectories(directory)
                .Select(p => new SaveEntry(Path.GetFileName(p), SaveEntryKind.Directory))
                .OrderBy(e => e.FileName, StringComparer.OrdinalIgnoreCase);
            var files = Directory.EnumerateFiles(directory)
                .Select(p => Path.GetFileName(p))
                .Where(n => !n.EndsWith(TempSuffix, StringComparison.OrdinalIgnoreCase))
                .Select(n => new SaveEntry(n, IsSaveFileName(n) ? SaveEntryKind.Save : SaveEntryKind.Other))
                .OrderBy(e => e.FileName, StringComparer.OrdinalIgnoreCase);
            return [.. directories, .. files];
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    /// <summary>Hat die Datei die Endung eines Spielstands und einen gültigen Namen?</summary>
    public static bool IsSaveFileName(string fileName) =>
        fileName.EndsWith(SaveGameExtension, StringComparison.OrdinalIgnoreCase)
        && NormalizeName(fileName) is not null;

    /// <summary>
    /// Pfad der Spielstanddatei zu einem Namen. Liegt schon eine Datei an, deren Name sich nur in der Schreibweise
    /// unterscheidet (<c>test1.EMS</c>), wird sie benutzt – so entstehen auf Systemen mit Groß-/Kleinschreibung
    /// keine zwei Dateien zum selben Namen.
    /// </summary>
    public static string PathOf(string directory, string baseName)
    {
        var wanted = FileNameOf(baseName);
        if (Directory.Exists(directory))
        {
            try
            {
                foreach (var path in Directory.EnumerateFiles(directory))
                    if (string.Equals(Path.GetFileName(path), wanted, StringComparison.OrdinalIgnoreCase))
                        return path;
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
            }
        }
        return Path.Combine(directory, wanted);
    }

    public static bool Exists(string directory, string baseName) => File.Exists(PathOf(directory, baseName));

    /// <summary>Endung der Zwischendatei beim Schreiben.</summary>
    public const string TempSuffix = ".tmp";

    /// <summary>
    /// Schreibt die Datei ganz oder gar nicht: erst in eine Zwischendatei, dann an den Zielnamen verschieben. Bricht
    /// das Schreiben ab, bleibt eine vorhandene Datei unverändert und die Zwischendatei wird entfernt.
    /// </summary>
    /// <exception cref="IOException">Schreiben nicht möglich.</exception>
    public static void WriteAtomic(string path, byte[] data)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
        var temp = path + TempSuffix;
        try
        {
            File.WriteAllBytes(temp, data);
            File.Move(temp, path, overwrite: true);
        }
        catch (Exception e) when (e is UnauthorizedAccessException)
        {
            throw new IOException(e.Message, e);
        }
        finally
        {
            if (File.Exists(temp))
            {
                try { File.Delete(temp); }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
            }
        }
    }

    /// <summary>Verzeichnis für die Anzeige: Schrägstriche (der Zeichensatz kennt kein „\“), vorn gekürzt, wenn zu lang.</summary>
    public static string DisplayPath(string directory, int maxChars)
    {
        var text = directory.Replace('\\', '/');
        if (text.Length <= maxChars)
            return text;
        var sb = new StringBuilder("...");
        sb.Append(text.AsSpan(text.Length - (maxChars - 3)));
        return sb.ToString();
    }
}
