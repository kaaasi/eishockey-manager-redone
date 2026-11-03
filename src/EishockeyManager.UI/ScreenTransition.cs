using EishockeyManager.Core;

namespace EishockeyManager.UI;

/// <summary>
/// Zwischenbild beim Bildschirmwechsel wie em.exe Datei 0x0C5CE (Bildschirm aus/ein) und 0x45D46 (M-K9): Während der nächste
/// Bildschirm aufgebaut wird, sieht der Spieler den Hintergrund kb\22 bzw. – ohne Seitenwechsel – ein dunkles Bild.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>[ORIGINAL] Mit Seitenwechsel (DGROUP 0x22A ≠ 0, gesetzt beim Spielstart Datei 0x439C0, gelöscht beim Laden eines
///   Spielstands Datei 0x5687A): „aus“ zeigt die Seite VRAM 0x9600, in die beim ersten Mal kb\22 kopiert wurde; gezeichnet wird
///   weiter auf Seite 0; „ein“ zeigt Seite 0 mit der ausstehenden Palette (<c>docs/analyse/re-meldungen.md</c> §0x0C5CE).</item>
/// <item>[ORIGINAL] Ohne Seitenwechsel: „aus“ schaltet das Bild ab (<c>INT 10h</c> AX 0x1201, Datei 0x10505), „ein“ wieder an
///   (AX 0x1200, Datei 0x1050D) – der Spieler sieht ein schwarzes Bild.</item>
/// <item>[APPROX] Dauer: Im Original so lange, wie der Aufbau des neuen Bildschirms dauert (nicht messbar aus den Videos mit
///   1 Bild/s); hier fest <see cref="DefaultDuration"/>. 0 schaltet das Zwischenbild ab.</item>
/// <item>[APPROX] Palette des Zwischenbilds: die von kb\22 bzw. die Standardpalette kb\21. Das Original setzt vor „aus“ die
///   Palette des zuletzt geladenen Bilds (DGROUP 0xE1A0, wenn 0x49A9 gesetzt ist); das bildet das Remake nicht nach.</item>
/// <item>Nicht jeder Wechsel von <see cref="ScreenHost.Current"/> ist ein Aufruf von 0x0C5CE: Bildschirme, die über ihrem
///   Vorgänger zeichnen (Fenster), wechseln im Original ohne Zwischenbild. Welche das sind, legt der Aufrufer über
///   <c>skip</c> fest.</item>
/// </list>
/// </remarks>
public sealed class ScreenTransition
{
    /// <summary>[APPROX] Anzeigedauer des Zwischenbilds.</summary>
    public static readonly TimeSpan DefaultDuration = TimeSpan.FromMilliseconds(150);

    private readonly Func<Screen, bool> _skip;
    private Screen? _last;
    private TimeSpan _until = TimeSpan.MinValue;

    /// <param name="skip">Wechsel zu diesem Bildschirm ohne Zwischenbild (z. B. Fenster über dem Vorgänger).</param>
    public ScreenTransition(Func<Screen, bool>? skip = null) => _skip = skip ?? (_ => false);

    public TimeSpan Duration { get; set; } = DefaultDuration;

    /// <summary>DGROUP 0x22A: true nach dem Spielstart, false nach dem Laden eines Spielstands.</summary>
    public bool PageFlip { get; set; } = true;

    /// <summary>Nach jedem <see cref="ScreenHost.Update"/> aufrufen; startet bei einem Wechsel das Zwischenbild.</summary>
    public void Observe(Screen? current, TimeSpan now)
    {
        if (current is not null && _last is not null && !ReferenceEquals(current, _last) && !_skip(current)
            && Duration > TimeSpan.Zero)
            _until = now + Duration;
        _last = current;
    }

    public bool IsActive(TimeSpan now) => now < _until;

    /// <summary>
    /// Zwischenbild des Desktop-Spiels (Anschluss Integration Runde 12, ui-meldungen Teil 5): ohne Zwischenbild zu den Bildschirmen,
    /// die über ihrem Vorgänger zeichnen (<see cref="Screens.CreditsScreen"/>, <see cref="Screens.InfoFileScreen"/>,
    /// <see cref="Screens.ManagerArea.FinanceBookScreen"/>; Liste aus #492).
    /// </summary>
    public static ScreenTransition ForDesktop() =>
        new(s => s is Screens.CreditsScreen or Screens.InfoFileScreen or Screens.ManagerArea.FinanceBookScreen);

    private GameState? _lastGame;

    /// <summary>
    /// Wie <see cref="Observe(Screen?, TimeSpan)"/>, setzt zusätzlich <see cref="PageFlip"/>, wenn der Host ein anderes Spiel bekommt:
    /// [ORIGINAL] false nach dem Laden eines Spielstands (Datei 0x5687A), true beim Spielstart (Datei 0x439C0). [APPROX] „Laden“ =
    /// das neue Spiel kam aus dem Bildschirm Laden/Sichern (<see cref="Screens.SaveLoad.SaveLoadScreen"/>); jeder andere Wechsel
    /// des Spiels gilt als Spielstart.
    /// </summary>
    public void Observe(ScreenHost host, TimeSpan now)
    {
        if (host.Game is { } game && !ReferenceEquals(game, _lastGame))
            PageFlip = _last is not Screens.SaveLoad.SaveLoadScreen;
        _lastGame = host.Game;
        Observe(host.Current, now);
    }

    /// <summary>Zeichnet das Zwischenbild, solange es aktiv ist; sonst false (dann zeichnet der Bildschirm selbst).</summary>
    public bool Draw(Surface s, Assets assets, TimeSpan now)
    {
        if (!IsActive(now))
            return false;
        if (PageFlip)
        {
            var background = assets.Image(Screens.MainMenuScreen.Background);
            s.Palette = background.Palette ?? assets.Palette;
            s.Blit(background, 0, 0);
        }
        else
        {
            s.Palette = assets.Palette;
            s.Fill(new Rect(0, 0, Surface.Width, Surface.Height), 0);
        }
        return true;
    }
}
