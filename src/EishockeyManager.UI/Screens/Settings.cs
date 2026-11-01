using System.Text.Json;
using EishockeyManager.Core;

namespace EishockeyManager.UI.Screens;

/// <summary>
/// Einstellungen des Ports (das Original hat keinen solchen Bildschirm). Gelten für alle Spielstände und werden
/// – sobald <see cref="StorePath"/> gesetzt ist – als JSON gespeichert. Das Fenster (Desktop) setzt den Pfad und
/// wendet Effektlautstärke und Vollbild an (Musik: nur Titelbildschirm, ohne Einstellung, siehe <c>MusicPolicy</c>;
/// alte Dateien mit MusicVolume/MusicInMatch laden weiter, die Felder werden ignoriert); Torszenen liest das Live-Spiel, das automatische Speichern der Hauptmenü-Aufruf.
/// </summary>
public sealed class GameSettings
{
    public const int MaxVolume = 10;

    private int _effectsVolume = MaxVolume;

    /// <summary>Lautstärke der Soundeffekte 0–10.</summary>
    public int EffectsVolume
    {
        get => _effectsVolume;
        set => _effectsVolume = Math.Clamp(value, 0, MaxVolume);
    }

    /// <summary>Torszenen im Live-Spiel zeigen.</summary>
    public bool GoalScenes { get; set; } = true;

    public bool Fullscreen { get; set; }

    /// <summary>
    /// Automatisch speichern (Standard: an). [ORIGINAL] Das Original speichert immer selbst (em.exe, „autosave.hok“);
    /// die Möglichkeit, es abzuschalten, ist neu. Zeitpunkt und Datei: <see cref="SaveLoad.AutoSave"/>.
    /// </summary>
    public bool AutoSave { get; set; } = true;

    /// <summary>Die geltenden Einstellungen.</summary>
    public static GameSettings Current { get; set; } = new();

    /// <summary>Datei, in die <see cref="SaveCurrent"/> schreibt; null = nicht speichern (Tests, Werkzeuge).</summary>
    public static string? StorePath { get; set; }

    /// <summary>Üblicher Speicherort im Benutzerprofil.</summary>
    public static string DefaultPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "EishockeyManagerRedone", "einstellungen.json");

    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public GameSettings Clone() => (GameSettings)MemberwiseClone();

    /// <summary>Liest die Datei; fehlt sie oder ist sie beschädigt, gelten die Standardwerte.</summary>
    public static GameSettings Load(string path)
    {
        try
        {
            if (File.Exists(path))
                return JsonSerializer.Deserialize<GameSettings>(File.ReadAllText(path), Json) ?? new GameSettings();
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
        }
        return new GameSettings();
    }

    public void Save(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
        File.WriteAllText(path, JsonSerializer.Serialize(this, Json));
    }

    /// <summary>Speichert <see cref="Current"/>, falls ein Pfad gesetzt ist; Schreibfehler werden ignoriert.</summary>
    public static void SaveCurrent()
    {
        if (StorePath is not { } path)
            return;
        try
        {
            Current.Save(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }
}
