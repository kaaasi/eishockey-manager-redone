using EishockeyManager.Core;

namespace EishockeyManager.UI.Screens;

/// <summary>
/// Kachelsperren des Vereinsmenüs, die das Original beim Aufbau des Hauptmenüs aus der Spielvorbereitung ableitet
/// (Spur tabellen-highscore). Die Sperren des Bundestrainers und des Spielmodus „nur Fahne“ stehen in
/// <see cref="Competitions.NationalMenu"/>.
/// </summary>
/// <remarks>
/// [ORIGINAL] em.exe, Aufbau der Sperrtabelle (Datei 0x44FBD–0x5009C, Tabelle DGROUP 0x5159, 6 Plätze je Reihe,
/// Reihe Manager = Finanzen, Werbung, Stadion, Jugend, Neuer, Highscore). Für den Verein (Manager 0–3):
/// <list type="bullet">
/// <item>„Neuer“ (0x515D): gesperrt, wenn DGROUP 0x386 = 4 – die Zahl der Manager erreicht die Höchstzahl (Datei 0x45057).</item>
/// <item>„Werbung“ (0x515A): gesperrt, wenn das Spielende DGROUP 0x5A14 = 1994 ist (Datei 0x45079). Das Spielende ist
///   1994 + 2 · Dauer (Datei 0x2987C), bei „unbegrenzt“ 22222 – also nur bei der Dauer „eine Saison“.</item>
/// </list>
/// Die Sperre von „Highscore“ (0x515E) steht bei der Bestenliste (Datei 0x44FCE, abhängig von der Spielvorbereitung).
/// </remarks>
public static class TileLocks
{
    /// <summary>Ist die Kachel des Vereinsmenüs nach der Spielvorbereitung gesperrt?</summary>
    public static bool IsLocked(GameState game, MenuTile tile) => tile.Name switch
    {
        "Neuer" => game.Managers.Count >= GameSetup.MaxManagers,
        "Werbung" => game.Setup.Duration == GameDuration.EineSaison,
        _ => false,
    };
}
