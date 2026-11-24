using EishockeyManager.Core;
using EishockeyManager.UI.Screens.Competitions;

namespace EishockeyManager.UI.Screens;

/// <summary>
/// Meisterfoto kb\16 mit Titel oben („Deutscher Meister“, „DEB Pokalsieger“, „Europacupsieger“, „Weltmeister“) und dem Sieger
/// unten (Video 10:45, 17:35, 23:26). Klick, Taste oder Zeitablauf → zurück.
/// </summary>
/// <remarks>
/// [ORIGINAL] Meisterfoto 0x350B6 (Aufrufer 0x30E7B, 0x3139F, 0x31934, 0x34B76; Abweichung B25, A343): kb\16 laden, je Titel ein
/// Teil aus dem Bereich rechts oben über den Pokal des Fotos kopieren (Tabelle DGROUP 0x4E2C, Kopie 0x139C9 mit Maske 0xC0,
/// Datei 0x350E8–0x3516C), den Bereich der Teile schwärzen (0x0B620, Datei 0x3516F–0x3518E), Titel in Schrift 1 (Datei
/// 0x3519E–0x351F4) und Vereinsname zweifach (Datei 0x351F7–0x35262). Video <c>folge02_00928</c> („Deutscher Meister“: Figur auf
/// dem Pokal) und <c>folge02_00932</c> („DEB Pokalsieger“: Schläger am Pokal).
/// </remarks>
public sealed class SeasonEndTrophyScreen : Screen
{
    public const int Photo = 16;
    public const int Frames = 150;

    /// <summary>[ORIGINAL] Füllung (201 | 0)–(292 | 60) Farbe 0 nach der Kopie (Datei 0x3516F–0x3518E): Bereich der Einzelteile.</summary>
    public static readonly Rect SpriteArea = new(201, 0, 92, 61);

    /// <summary>[ORIGINAL] Maske der Kopie 0x139C9 (Datei 0x35106: <c>mov al, 0xC0</c>).</summary>
    public const int PartMask = 0xC0;

    /// <summary>Ein Pokalteil aus kb\16: Quelle und Ziel im Foto.</summary>
    public readonly record struct TrophyPart(Rect Source, int DestX, int DestY);

    /// <summary>
    /// [ORIGINAL] Teile k = 1 … 3 (DGROUP 0x4E1A ff., je Eintrag zwei Byte): Quelle x/y 0x4E1A/0x4E1B, Ziel x/y 0x4E20/0x4E21,
    /// Breite/Höhe 0x4E26/0x4E27.
    /// </summary>
    public static readonly TrophyPart[] Parts =
    [
        new(new Rect(201, 0, 47, 26), 108, 36),   // k = 1: Schläger (DEB-Pokal)
        new(new Rect(201, 26, 46, 35), 108, 27),  // k = 2: Kugel (Weltmeister)
        new(new Rect(247, 1, 46, 60), 108, 2),    // k = 3: Figur (Deutscher Meister)
    ];

    /// <summary>
    /// Titelnummer des Originals ([bp+6] von 0x350B6; Texte DGROUP 0x715: 0 Deutscher Meister, 1 DEB Pokalsieger,
    /// 2 Europacupsieger, 3 Weltmeister).
    /// </summary>
    public static int OriginalTitle(Title title) => title switch
    {
        Title.GermanChampion => 0,
        Title.CupWinner => 1,
        Title.EuropeanCupWinner => 2,
        _ => 3,
    };

    /// <summary>
    /// [ORIGINAL] Teil je Titel (DGROUP 0x4E2C = 3, 1, 0, 2; 0 = kein Teil): Meister Figur, DEB-Pokal Schläger, Europacup
    /// unverändert, Weltmeister Kugel.
    /// </summary>
    public static TrophyPart? Part(Title title) => OriginalTitle(title) switch
    {
        0 => Parts[2],
        1 => Parts[0],
        3 => Parts[1],
        _ => null,
    };

    /// <summary>[ORIGINAL] Titel linksbündig ab x 0x95, beim Weltmeister 0x95 + 32 (Datei 0x351BF–0x351D3), Grundlinie 0x1A.</summary>
    public static int TitleX(Title title) => 0x95 + (OriginalTitle(title) == 3 ? 1 << 5 : 0);

    public const int TitleBaseline = 0x1A;
    public const byte TitleColor = 0x1F, TitleShadow = 0x1C;

    /// <summary>[ORIGINAL] Vereinsname zentriert in x 0–319 auf Grundlinie 237 Farbe 0, dann x 0–315 Grundlinie 235 Farbe 0x1C.</summary>
    public const int ClubBaseline = 0xED, ClubBaselineFront = 0xEB;
    public const byte ClubColor = 0x1C;

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
        if (Part(_trophy.Title) is { } part)
            s.Blit(photo, part.Source, part.DestX, part.DestY, transparent: PartMask);
        s.Fill(SpriteArea, 0);

        var bold = Assets.Bold;
        var title = SeasonInfo.TrophyTitle(_trophy.Title);
        CompetitionWidgets.Centered(s, bold, TitleX(_trophy.Title), 0, TitleBaseline, title, TitleColor, TitleShadow, left: true);
        var club = Host.Game!.Clubs[_trophy.Club].Name;
        CompetitionWidgets.Centered(s, bold, 0, 319, ClubBaseline, club, 0, null);
        CompetitionWidgets.Centered(s, bold, 0, 315, ClubBaselineFront, club, ClubColor, null);
    }
}
