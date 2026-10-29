using System.Runtime.CompilerServices;
using EishockeyManager.Core;

namespace EishockeyManager.UI.Screens.SaveLoad;

/// <summary>
/// Ort der Spielstände und Zustand des automatischen Speicherns, je <see cref="ScreenHost"/>.
/// </summary>
/// <remarks>
/// Je Host statt statisch, damit Tests mit eigenem Ordner parallel laufen. Der Standardordner ist <b>nicht</b> gesetzt
/// (Tests und Werkzeuge schreiben nichts); das Fenster setzt <see cref="DefaultDirectory"/> beim Start.
/// </remarks>
public sealed class SaveContext
{
    private static readonly ConditionalWeakTable<ScreenHost, SaveContext> Contexts = new();

    /// <summary>Spielstandordner, solange <see cref="Directory"/> nichts anderes sagt; <c>null</c> = nicht speichern.</summary>
    public static string? DefaultDirectory { get; set; }

    /// <summary>Üblicher Ort im Benutzerprofil, neben der Datei der Einstellungen.</summary>
    public static string UserDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "EishockeyManagerRedone", "Spielstaende");

    public static SaveContext For(ScreenHost host) => Contexts.GetValue(host, _ => new SaveContext());

    private string? _directory;

    /// <summary>Ordner der Spielstände dieses Hosts oder <c>null</c>, wenn keiner eingerichtet ist.</summary>
    public string? Directory
    {
        get => _directory ?? DefaultDirectory;
        set => _directory = value;
    }

    /// <summary>
    /// Überschreibt die Einstellung „Automatisch speichern“ für diesen Host (Tests); <c>null</c> = Einstellung
    /// <see cref="GameSettings.AutoSave"/> gilt.
    /// </summary>
    public bool? AutoSaveOverride { get; set; }

    public bool AutoSaveEnabled => AutoSaveOverride ?? GameSettings.Current.AutoSave;

    // ---- Zustand des automatischen Speicherns (siehe AutoSave) ------------------------------------

    /// <summary>
    /// Zähler der Hauptmenü-Aufrufe. [ORIGINAL] em.exe, DGROUP 0x5148, Anfangswert 1 in der Datei; er gehört nicht
    /// zum Spielstand und läuft beim Laden weiter.
    /// </summary>
    internal int AutoSaveCounter { get; set; } = AutoSave.InitialCounter;

    /// <summary>Letzter gezählter Zug (Spiel, Saison, Tag, Manager), damit die Rückkehr aus einer Kachel nicht zählt.</summary>
    internal (GameState Game, int Season, int Day, int Manager)? LastTurn { get; set; }

    /// <summary>Meldung des letzten gescheiterten automatischen Speicherns (für Tests); <c>null</c> = kein Fehler.</summary>
    public string? LastAutoSaveError { get; internal set; }

    /// <summary>Anzahl der bisher geschriebenen automatischen Spielstände (für Tests).</summary>
    public int AutoSaveWrites { get; internal set; }
}
