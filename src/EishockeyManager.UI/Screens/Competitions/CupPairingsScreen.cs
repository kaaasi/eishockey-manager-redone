using EishockeyManager.Core;

namespace EishockeyManager.UI.Screens.Competitions;

/// <summary>Eine Begegnung der Paarungsansicht: Heim, Gast und Tore (0:0, solange nicht gespielt).</summary>
public sealed record CupPairingRow(int Home, int Away, int HomeGoals, int AwayGoals);

/// <summary>
/// „Gruppe n   m.Spieltag“ (Paarungen → WM, #194): die drei Begegnungen eines WM-Gruppenspieltags mit Flaggen und
/// Ergebnis. Zeigt nur vorhandenen Zustand; die Pfeile blättern die fünf Spieltage, „Gruppe 2“/„Gruppe 1“ wechselt die
/// Gruppe, „Endrunde“ öffnet „WM-Endrundenspiele“, dessen Knopf „Gruppe“ hierher zurückführt.
/// </summary>
/// <remarks>
/// [ORIGINAL] em.exe Datei 0x2FC2F (Overlay 0xE8, Stub 0xE8:0x34), Argumente Gruppe (0/1), Minute (0 = Paarungsansicht,
/// sonst Anzeige im Spielablauf) und Spieltag. Aufruf aus dem Seitenwähler 0x3A88A (Seite 8 = Gruppe 1, 9 = Gruppe 2,
/// Datei 0x3AC29–0x3AC3F) mit Spieltag = WM-Stufe DGROUP 0x7AE (gespielte WM-Termine), begrenzt auf 4; die Kachel
/// „Paarungen → WM“ beginnt mit Seite 8 (DGROUP 0x5127, Datei 0x458A2). Belegt an DOSBox `nur-weltkugel/20`, `21` und
/// (Anzeige im Spielablauf) Video `bundestrainer_00437`.
/// <list type="bullet">
/// <item>[ORIGINAL] Fenster 12 × 9 Rahmenteile ab (16 | 2) (0x30:0x335 mit 0, 2). Pokale kb\36 Quelle (0 | 66) 22 × 22 bei
///   (29 | 10) und (269 | 10), nur wenn nicht der Bundestrainer am Zug ist (DGROUP 0xA70E ≠ 5, Datei 0x2FE15).
///   Flächen (51 | 12) … (268 | 32) und (26 | 33) … (292 | 165) Farbe 4.</item>
/// <item>[ORIGINAL] Titel Bold zentriert 0 … 319, Grundlinie 30, Farbe 7, Schatten 1: „Gruppe“ + „ “ + Nummer + drei
///   Leerzeichen + Spieltag + „.Spieltag“ (DGROUP 0x65C, 0x4E3E, 0x4E40, 0x670) bzw. Minute + „. Min.“ (0x6F9);
///   Linie (27 … 292 | 33) Farbe 7.</item>
/// <item>[ORIGINAL] Begegnung k = 0 … 2, Kasten y = 39 + 43 k: Heim x = 73, Gast x = 141; Umriss (x | y) … (x + 41 | y + 41)
///   Farbe 7 bzw. 18 bei einem Land mit Manager (Vereinsfeld 0x16 ≠ 0, also mit Bundestrainer), Umriss + 1 Farbe 4,
///   Flagge 38 × 38 (Logo des Landes, 0x2F530) bei (x + 2 | y + 2). Bold, Grundlinie y + 27, Farbe 7, Schatten 0: „-“
///   zentriert bei 125, Tore zentriert bei 218 und 253, „:“ zentriert 234 … 237. Ungespielt 0:0 (Tabelle DGROUP 0xCDB8).</item>
/// <item>[ORIGINAL] Nur in der Paarungsansicht: vier Knöpfe 2 × 2 Teile bei x = 39 + 65 i, y = 180; Pfeile kb\4 (29 × 9,
///   Quelle (19 | 104) bzw. (19 | 125)) bei (49 | 195) und (244 | 195) (0x2FAF5); Mini, Farbe 30, ohne Schatten,
///   Grundlinie 201: „Endrunde“ zentriert 108 … 151, „Gruppe“ + „ “ + andere Gruppe zentriert 173 … 215.</item>
/// <item>[ORIGINAL] Pfeil links: Spieltag − 1 (vor 1 → 5), rechts: + 1 (nach 5 → 1) (Datei 0x30201–0x30244). „Endrunde“
///   gibt Seite 10 zurück, „Gruppe n“ Seite 9 − Gruppe (0x30303–0x3031A); der Seitenwähler ruft die neue Seite wieder mit
///   Spieltag = WM-Stufe auf. Die Endrunde (Seite 10, Datei 0x32E23) kehrt mit „Gruppe“ zur vorigen Seite zurück
///   (Datei 0x3AC75–0x3AC81).</item>
/// <item>[ORIGINAL] Klick auf eine Flagge ruft das Gegner-Fenster 0x128:0x2A mit Seite 2 (Datei 0x302AA,
///   <see cref="FlagAt"/>, <see cref="TeamInfoWindow"/>).</item>
/// <item>[APPROX] Rechtsklick und Esc kehren ins Menü zurück; gedrückte Pfeile (Quelle y + 10) werden nicht gezeigt; die
///   zuletzt gewählte Seite (DGROUP 0x5127) wird nicht gemerkt.</item>
/// </list>
/// </remarks>
public sealed class CupPairingsScreen : Screen
{
    public static readonly Rect Window = new(16, 2, 12 * 24, 9 * 19);
    public const int FirstBoxY = 39;
    public const int BoxPitch = 43;
    public static readonly int[] BoxX = [73, 141];
    public const int Days = WorldCup.GroupDates;
    public static readonly Rect[] Buttons = [.. Enumerable.Range(0, 4).Select(i => new Rect(39 + 65 * i, 180, 48, 38))];
    public static readonly Rect TrophySource = new(0, 66, 22, 22);
    private static readonly Rect LeftArrow = new(19, 104, 29, 9);
    private static readonly Rect RightArrow = new(19, 125, 29, 9);

    private readonly Screen _back;
    private readonly IReadOnlyList<CupPairingRow>? _fixed;
    private readonly TeamInfoWindow _info = new();

    /// <summary>Gegner-Fenster (Klick auf eine Flagge).</summary>
    public TeamInfoWindow Info => _info;

    /// <summary>
    /// [ORIGINAL] Land unter dem Mauszeiger (Datei 0x30251–0x302C6): Begegnung k bei 39 &lt; y &lt; 82 + 43 k (k &lt; 3), Gast bei
    /// x &gt; 125, sonst Heim; ruft das Gegner-Fenster 0x128:0x2A mit Seite 2.
    /// </summary>
    public static (int Match, bool Away)? FlagAt(int x, int y) =>
        y > FirstBoxY && y < FirstBoxY + BoxPitch * 3 ? ((y - FirstBoxY) / BoxPitch, x > 125) : null;
    private readonly int? _liveMinute;
    private readonly bool? _trophies;

    /// <summary>0 = Gruppe 1, 1 = Gruppe 2.</summary>
    public int Group { get; private set; }

    /// <summary>Angezeigter Spieltag 0 … 4.</summary>
    public int Day { get; private set; }

    /// <summary>Paarungsansicht wie über die Kachel: Spieltag = gespielte WM-Termine, höchstens der fünfte.</summary>
    public CupPairingsScreen(ScreenHost host, Screen back, int group = 0) : base(host)
    {
        _back = back;
        Group = group;
        Day = StartDay(host.Game);
    }

    /// <summary>
    /// Feste Begegnungen statt des Spielstands (Bildschirmfotos); <paramref name="liveMinute"/> zeigt die Anzeige im
    /// Spielablauf („n. Min.“, ohne Knöpfe), <paramref name="trophies"/> erzwingt die Pokale.
    /// </summary>
    public CupPairingsScreen(ScreenHost host, Screen back, int group, int day, IReadOnlyList<CupPairingRow> rows,
        int? liveMinute = null, bool? trophies = null) : base(host)
        => (_back, Group, Day, _fixed, _liveMinute, _trophies) = (back, group, day, rows, liveMinute, trophies);

    /// <summary>[ORIGINAL] Startspieltag: WM-Stufe DGROUP 0x7AE (gespielte WM-Termine), begrenzt auf 4 (Datei 0x2FDC2).</summary>
    public static int StartDay(GameState? game)
    {
        if (game?.WorldCup is not { } wm || wm.Groups.Count == 0)
            return 0;
        var played = wm.Groups[0].Results.Count / (WorldCup.GroupSize / 2) + wm.Knockout.Count;
        return Math.Min(played, Days - 1);
    }

    public string Title => "Gruppe " + (Group + 1) + "   " + (_liveMinute is { } m ? m + ". Min." : Day + 1 + ".Spieltag");

    public string GroupButtonText => "Gruppe " + (2 - Group);

    /// <summary>Begegnungen des angezeigten Spieltags: Paarungen der Gruppe, Tore aus den Ergebnissen.</summary>
    public IReadOnlyList<CupPairingRow> Rows
    {
        get
        {
            if (_fixed is not null)
                return _fixed;
            if (Host.Game?.WorldCup?.Groups.ElementAtOrDefault(Group) is not { } group)
                return [];
            return group.Pairings(Day).Select(p =>
            {
                var r = group.Results.FirstOrDefault(m => m.Home == p.Home && m.Away == p.Away);
                return new CupPairingRow(p.Home, p.Away, r?.HomeGoals ?? 0, r?.AwayGoals ?? 0);
            }).ToList();
        }
    }

    private bool Trophies => _trophies ?? Host.Game is not { NationalCoachMenu: true };

    public override void Update(InputState input)
    {
        if (input.Pressed(Key.Escape) || input.RightClick)
        {
            Host.Show(_back);
            return;
        }
        if (_liveMinute is not null)
        {
            if (input.LeftClick || input.Keys.Count > 0)
                Host.Show(_back);
            return;
        }
        if (_info.Update(input))
            return;
        if (input.Clicked(Buttons[0]))
            Day = Day == 0 ? Days - 1 : Day - 1;
        else if (input.Clicked(Buttons[3]))
            Day = Day == Days - 1 ? 0 : Day + 1;
        else if (input.Clicked(Buttons[1]))
            Host.Show(WorldCupPairingsPages.Open(Host, _back, WorldCupPairingsPages.Knockout, Group)); // gemerkte Seite (A293)
        else if (input.Clicked(Buttons[2]))
            Host.Show(WorldCupPairingsPages.Open(Host, _back, WorldCupPairingsPages.GroupPage(1 - Group)));
        else if (input.LeftClick && FlagAt(input.MouseX, input.MouseY) is var (k, away) && Rows.ElementAtOrDefault(k) is { } row)
            _info.Show(away ? row.Away : row.Home);
    }

    public override void Draw(Surface s)
    {
        DrawScreen(s);
        if (Host.Game is { } game)
            _info.Draw(s, Assets, game);
    }

    private void DrawScreen(Surface s)
    {
        s.Palette = Assets.Palette;
        s.Blit(Assets.Image(MainMenuScreen.Background), 0, 0);
        Widgets.Frame(s, Assets, Window, FrameStyle.Orange);
        if (_liveMinute is null)
        {
            foreach (var b in Buttons)
                Widgets.Frame(s, Assets, b, FrameStyle.Orange);
            var mini = Assets.Mini;
            CompetitionWidgets.Centered(s, mini, 108, 151, 201, "Endrunde", Colors.TileBackground, null);
            CompetitionWidgets.Centered(s, mini, 173, 215, 201, GroupButtonText, Colors.TileBackground, null);
            var arrows = Assets.Image(4);
            s.Blit(arrows, LeftArrow, 49, 195);
            s.Blit(arrows, RightArrow, 244, 195);
        }

        if (Trophies)
        {
            var trophies = Assets.Image(EuropeanCupScreen.TrophyGraphic);
            s.Blit(trophies, TrophySource, 29, 10);
            s.Blit(trophies, TrophySource, 269, 10);
        }
        NationalWidgets.FillInclusive(s, 51, 12, 268, 32, 4);
        NationalWidgets.FillInclusive(s, 26, 33, 292, 165, 4);
        var bold = Assets.Bold;
        CompetitionWidgets.Centered(s, bold, 0, 319, 30, Title, Colors.White, DebCupDrawScreen.TitleShadow);
        s.HLine(27, 33, 292 - 27 + 1, Colors.White);

        if (Host.Game is not { } game)
            return;
        var managed = game.NationalCoach?.Nation;
        var rows = Rows;
        for (var k = 0; k < rows.Count && k < 3; k++)
        {
            var y = FirstBoxY + BoxPitch * k;
            var row = rows[k];
            CompetitionWidgets.Centered(s, bold, 125, 125, y + 27, "-", Colors.White, Colors.Black);
            int[] clubs = [row.Home, row.Away];
            int[] goals = [row.HomeGoals, row.AwayGoals];
            for (var side = 0; side < 2; side++)
            {
                var x = BoxX[side];
                NationalWidgets.Outline(s, x, y, x + 41, y + 41, clubs[side] == managed ? Colors.Red : Colors.White);
                NationalWidgets.Outline(s, x + 1, y + 1, x + 40, y + 40, 4);
                Widgets.Logo(s, Assets, game.Clubs[clubs[side]].LogoGraphic, x + 2, y + 2);
                CompetitionWidgets.Centered(s, bold, 218 + 35 * side, 218 + 35 * side, y + 27, goals[side].ToString(), Colors.White, Colors.Black);
            }
            CompetitionWidgets.Centered(s, bold, 234, 237, y + 27, ":", Colors.White, Colors.Black);
        }
    }
}
