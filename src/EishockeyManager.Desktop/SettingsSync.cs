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

        // Musikpegel bleibt beim Standard des Mixers (voll): Die Musik läuft nur im Titelbildschirm und hat keine
        // eigene Einstellung mehr (#221); Ton aus bleibt über F8 (Muted) möglich. Ein fester Wert wäre willkürlich.
        switch (MusicPolicy.Decide(current, mixer.IsMusicPlaying))
        {
            case MusicAction.Start:
                mixer.PlayMusic();
                break;
            case MusicAction.Stop:
                mixer.StopMusic();
                break;
        }
    }
}
