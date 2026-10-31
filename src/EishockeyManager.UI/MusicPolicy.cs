using EishockeyManager.UI.Screens;

namespace EishockeyManager.UI;

/// <summary>
/// Wann die Musik läuft. Befund Nutzer (31.10.2026, DOSBox): Die Musik des Titelbildschirms endet, sobald man in die
/// Spielvorbereitung (Anzahl der Spieler) kommt. Bisher lief sie in allen Bildschirmen außer den Spieltagen.
/// [APPROX] Musik während der Spieltage weiter nach der Einstellung „Musik auch im Spiel“ (Zeitpunkt im Original nicht
/// geprüft).
/// </summary>
public static class MusicPolicy
{
    public static bool Wanted(Screen? current, GameSettings settings) => current switch
    {
        TitleScreen => true,
        MatchDayScreen => settings.MusicInMatch,
        _ => false,
    };
}
