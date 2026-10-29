using EishockeyManager.Audio;
using EishockeyManager.UI;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.SaveLoad;
using Microsoft.Xna.Framework;

namespace EishockeyManager.Desktop;

/// <summary>Überträgt die <see cref="GameSettings"/> auf Fenster und Ton (einmal je Frame).</summary>
internal static class SettingsSync
{
    /// <summary>Lädt die gespeicherten Einstellungen aus dem Benutzerprofil.</summary>
    public static void Load()
    {
        GameSettings.StorePath = GameSettings.DefaultPath;
        GameSettings.Current = GameSettings.Load(GameSettings.DefaultPath);
        SaveContext.DefaultDirectory = SaveContext.UserDirectory; // Spielstände und Autosave
    }

    /// <summary>F11: Vollbild umschalten und merken.</summary>
    public static void ToggleFullscreen()
    {
        GameSettings.Current.Fullscreen = !GameSettings.Current.Fullscreen;
        GameSettings.SaveCurrent();
    }

    public static void Apply(GraphicsDeviceManager graphics, AudioMixer? mixer, Screen? current)
    {
        var settings = GameSettings.Current;
        if (graphics.IsFullScreen != settings.Fullscreen)
        {
            graphics.IsFullScreen = settings.Fullscreen;
            graphics.ApplyChanges();
        }

        if (mixer is null)
            return;
        mixer.EffectsVolume = settings.EffectsVolume;
        mixer.MusicVolume = settings.MusicVolume;

        // „Musik auch im Spiel“: während der Spieltage (Bildschirm „Spiele laufen“) nur, wenn eingeschaltet
        var inMatch = current is MatchDayScreen;
        var wanted = !inMatch || settings.MusicInMatch;
        if (wanted && !mixer.IsMusicPlaying)
            mixer.PlayMusic();
        else if (!wanted && mixer.IsMusicPlaying)
            mixer.StopMusic();
    }
}
