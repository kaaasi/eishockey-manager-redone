using EishockeyManager.UI.Screens;

namespace EishockeyManager.UI;

/// <summary>Was der Ton-Treiber (Desktop) mit der Musik tun soll.</summary>
public enum MusicAction
{
    None,
    Start,
    Stop,
}

/// <summary>
/// Wann die Musik läuft. Entscheidung Nutzer (#221): nur im Titelbildschirm, sonst nirgends – auch nicht im Spieltag,
/// und ohne Einstellung dafür. Befund (31.10.2026, DOSBox): Die Musik des Titelbildschirms endet, sobald man in die
/// Spielvorbereitung (Anzahl der Spieler) kommt.
/// </summary>
/// <remarks>
/// [APPROX] Ob das Original die Musik an weiteren Stellen spielt, ist nicht geprüft; hier gilt die Vorgabe des Nutzers.
/// Der Desktop-Treiber (<c>SettingsSync</c>) fragt nur <see cref="Decide"/>; die Regel steht allein hier und ist ohne
/// Fenster testbar.
/// </remarks>
public static class MusicPolicy
{
    public static bool Wanted(Screen? current) => current is TitleScreen;

    /// <summary>Start, Stopp oder nichts, je nach Bildschirm und Zustand des Mixers.</summary>
    public static MusicAction Decide(Screen? current, bool isPlaying) => (Wanted(current), isPlaying) switch
    {
        (true, false) => MusicAction.Start,
        (false, true) => MusicAction.Stop,
        _ => MusicAction.None,
    };
}
