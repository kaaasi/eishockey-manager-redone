using EishockeyManager.Core;

namespace EishockeyManager.UI.Screens;

/// <summary>
/// Meisterfoto kb\16 mit Titel oben („Deutscher Meister“, „DEB Pokalsieger“, „Europacupsieger“) und dem Sieger unten
/// (Video 10:45, 17:35, 23:26). Klick, Taste oder Zeitablauf → zurück.
/// </summary>
/// <remarks>
/// kb\16 enthält rechts oben Einzelteile für die Pokale (Schläger, Kugel, Figur). Das Original setzt daraus je Titel
/// einen eigenen Pokal über den Pokal im Foto. [APPROX] Hier wird der Bereich geschwärzt und der Pokal des Fotos
/// beibehalten; Position und Farben der Schrift aus dem Video geschätzt.
/// </remarks>
public sealed class SeasonEndTrophyScreen : Screen
{
    public const int Photo = 16;
    public const int Frames = 150;
    public static readonly Rect SpriteArea = new(196, 0, 124, 61);

    private readonly TrophyStep _trophy;
    private readonly Screen _back;
    private int _frame;

    public SeasonEndTrophyScreen(ScreenHost host, TrophyStep trophy, Screen back) : base(host)
    {
        _trophy = trophy;
        _back = back;
    }

    public override void Update(InputState input)
    {
        if (input.LeftClick || input.RightClick || input.Keys.Count > 0 || ++_frame >= Frames)
            Host.Show(_back);
    }

    public override void Draw(Surface s)
    {
        var photo = Assets.Image(Photo);
        s.Palette = photo.Palette ?? Assets.Palette;
        s.Blit(photo, 0, 0);
        s.Fill(SpriteArea, photo.Pixels[5 * photo.Width + 190]);

        var bold = Assets.Bold;
        var title = SeasonInfo.TrophyTitle(_trophy.Title);
        // Farben aus der Palette des Fotos: Weiß 136, Gold 112, Schwarz 0
        s.Text(bold, 148, 10, title, 0);
        s.Text(bold, 147, 9, title, 136);
        var club = Host.Game!.Clubs[_trophy.Club].Name;
        var x = 157 - bold.Measure(club) / 2;
        s.Text(bold, x + 1, 219, club, 0);
        s.Text(bold, x, 218, club, 112);
    }
}
