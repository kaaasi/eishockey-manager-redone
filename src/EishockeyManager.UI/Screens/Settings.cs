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

/// <summary>
/// Lesende Anzeige der Spielstand-Optionen des Originals (Einstellungsbildschirm 0x2CD1C, <c>docs/reverse-engineering.md</c>
/// „Spielmodus-Optionen“). Die Umsetzung der Optionen in Core samt Migration ist Folgepaket <c>optionen-wirkung</c>;
/// bis dahin zeigt das Modell nur, was das Remake tatsächlich tut.
/// </summary>
/// <param name="Mode">Spielmodus des Spielstands (Index 9 „Neuer Spielmodus“, DGROUP 0x226).</param>
/// <param name="Matrix">
/// Drittelpause · Ergebnisse · Tabelle (Zeilen) × 1./2./3. Liga (Spalten), Index 0–8 = DGROUP 0x21D + 3 · Zeile + Spalte.
/// [ORIGINAL] Anfangswerte alle 0 (Datei 0x16DD0 + 0x21D); Zuordnung an DOSBox-Bildern 48/49 bestätigt. Das Remake kennt
/// die Meldungsfilter nicht (Live-Spiel: Paket <c>livespiel</c>), daher immer aus.
/// </param>
/// <param name="GermanCup">DEB-Pokal (Index 10).</param>
/// <param name="EuropeanCup">Europapokal (Index 11).</param>
/// <param name="WorldCup">WM-Spiele (Index 12).</param>
/// <remarks>
/// [APPROX] Pokal-Schalter aus dem Spielmodus abgeleitet (Nur Fahne: Liga und DEB-Pokal; Nur Weltkugel: WM;
/// beides: alle). Die Zuordnung Index 10–12 zu DEB-Pokal/Europapokal/WM-Spiele ist aus den Beschriftungen und Bild 48/49
/// geschlossen, ihre Wirkung im Original ist nicht belegt.
/// </remarks>
public sealed record GameOptionsView(GameMode? Mode, bool[] Matrix, bool GermanCup, bool EuropeanCup, bool WorldCup)
{
    public static readonly string[] MatrixRows = ["Drittelpause", "Ergebnisse", "Tabelle"];
    public static readonly string[] MatrixColumns = ["1.Liga", "2.Liga", "3.Liga"];

    public static GameOptionsView For(GameMode? mode) => new(
        mode, new bool[9],
        GermanCup: mode is GameMode.ClubOnly or GameMode.Full,
        EuropeanCup: mode == GameMode.Full,
        WorldCup: mode is GameMode.WorldOnly or GameMode.Full);

    public static string ModeName(GameMode? mode) => mode switch
    {
        GameMode.WorldOnly => "Nur Weltkugel",
        GameMode.ClubOnly => "Nur Fahne",
        GameMode.Full => "Fahne und Weltkugel",
        _ => "-",
    };

    public bool Equals(GameOptionsView? other) => other is not null && Mode == other.Mode
        && Matrix.AsSpan().SequenceEqual(other.Matrix) && GermanCup == other.GermanCup
        && EuropeanCup == other.EuropeanCup && WorldCup == other.WorldCup;

    public override int GetHashCode() => HashCode.Combine(Mode, GermanCup, EuropeanCup, WorldCup);
}
