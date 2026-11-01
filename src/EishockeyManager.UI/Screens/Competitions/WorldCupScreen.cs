using EishockeyManager.Core;

namespace EishockeyManager.UI.Screens.Competitions;

/// <summary>
/// WM „Gruppe 1/2“ (Tabellen → Tabelle WM): Tabelle einer Gruppe mit Flaggen; nach der Gruppenphase
/// zusätzlich die K.-o.-Spiele. Zeigt nur vorhandenen Zustand.
/// </summary>
/// <remarks>
/// Aufbau aus em.exe (Datei 0x2F5B6), vermessen am Video „Die Oberfläche von EM“ (13:13):
/// <list type="bullet">
/// <item>[ORIGINAL] Fenster 12 × 12 Rahmenteile ab (16 | 2); Titel Bold „Gruppe“ + Nummer (Mindestbreite 2) zentriert
///   0 … 319, Grundlinie 21, Farbe 7, Schatten 1; Linie (27 … 292 | 24) Farbe 7.</item>
/// <item>[ORIGINAL] Platz n (0–5): Grundlinie y = 57 + 28 n, Farbe 7 bzw. 30 im Wechsel; „n.“ Bold bei x = 25;
///   44 × „-“ (Flow) bei x = 25, Grundlinie y + 7; unter Platz 4 rote Doppellinie (Farbe 18, x 27 … 289, y + 8 / y + 9).</item>
/// <item>Flagge = Logo des Nationalteams (kb\(80 + Logo), 38 × 38). [ORIGINAL] Aufruf mit x = 46 bzw. 102 (Farbe 30), y − 20;
///   die Zeichenfunktion 0xF8:0x43 setzt das Bild um (2 | 2) versetzt (vermessen) → (48 | y − 18) bzw. (104 | y − 18).</item>
/// <item>[ORIGINAL] „Plus:Minus Tore:Gegentore“ Bold bei x = 170, Zahlen mit Mindestbreite 2.</item>
/// <item>[APPROX] Knopf „Gruppe 2“/„Gruppe 1“ unten rechts als Knopf aus kb\2 (vermessen); Knopf „Endrunde“ nach der
///   Gruppenphase (Lage nicht im Video) öffnet „WM-Endrundenspiele“ (<see cref="WorldCupKnockoutScreen"/>, seit Runde 6 am Video
///   vermessen), dessen Knopf „Gruppe“ hierher zurückführt.</item>
/// <item>[ORIGINAL] Klick auf eine Zeile öffnet das Gegner-Fenster 0x128:0x2A (<see cref="RowAt"/>, <see cref="TeamInfoWindow"/>).
///   [APPROX] Ein Linksklick außerhalb der Zeilen und Knöpfe kehrt wie bisher zurück (im Original wartet die Tabelle weiter).</item>
/// </list>
/// </remarks>
public sealed class WorldCupScreen : Screen
{
    public static readonly Rect Window = new(16, 2, 288, 228);
    public const int FirstBaseline = 57;
    public const int RowPitch = 28;
    public const int RankX = 25;
    public const int ScoreX = 170;
    public static readonly int[] FlagX = [48, 104];
    public const int ButtonX = 237;
    public const int ButtonY = 211;
    public static readonly Rect GroupButton = Widgets.ButtonRect(ButtonX, ButtonY);
    public static readonly Rect KnockoutButton = Widgets.ButtonRect(ButtonX - 64, ButtonY);

    private readonly Screen _back;
    private readonly CupGroup? _fixed;
    private readonly TeamInfoWindow _info = new();

    /// <summary>Gegner-Fenster (Klick auf eine Zeile).</summary>
    public TeamInfoWindow Info => _info;

    /// <summary>
    /// [ORIGINAL] Zeile unter dem Mauszeiger (Datei 0x2FA37–0x2FAD5): y &gt; 42, Zeile n bis y &lt; 70 + 28 n (n &lt; 6); x wird nicht
    /// geprüft. Klick öffnet das Gegner-Fenster 0x128:0x2A (Seite 2) für das Land dieser Tabellenzeile.
    /// </summary>
    public static int? RowAt(int y) => y > 42 && y < 42 + RowPitch * WorldCup.GroupSize ? (y - 42) / RowPitch : null;

    /// <summary>0 = Gruppe 1, 1 = Gruppe 2.</summary>
    public int View { get; private set; }

    /// <param name="group">Nur für Bildschirmfotos: feste Gruppe statt der laufenden Saison.</param>
    public WorldCupScreen(ScreenHost host, Screen back, int view = 0, CupGroup? group = null) : base(host)
    {
        _back = back;
        View = view;
        _fixed = group;
    }

    public CupGroup? Group => _fixed ?? Host.Game?.WorldCup?.Groups.ElementAtOrDefault(View);

    private bool KnockoutKnown => Host.Game?.WorldCup?.KnockoutPairings(0).Count > 0;

    public override void Update(InputState input)
    {
        if (_info.Update(input))
            return;
        if (input.LeftClick && RowAt(input.MouseY) is { } row && Host.Game is { } game && Group is { } g
            && g.Table(game.Clubs).ElementAtOrDefault(row) is { } clicked)
        {
            _info.Show(clicked.Club.Index);
            return;
        }
        if (input.Pressed(Key.Escape) || input.RightClick)
        {
            Host.Show(_back);
            return;
        }
        if (input.Clicked(GroupButton))
            View = View == 0 ? 1 : 0;
        else if (KnockoutKnown && input.Clicked(KnockoutButton))
            Host.Show(new WorldCupKnockoutScreen(Host, _back, () => new WorldCupScreen(Host, _back, View)));
        else if (input.LeftClick || input.Keys.Count > 0)
            Host.Show(_back);
    }

    public override void Draw(Surface s)
    {
        s.Palette = Assets.Palette;
        s.Blit(Assets.Image(MainMenuScreen.Background), 0, 0);
        Widgets.Frame(s, Assets, Window, FrameStyle.Orange);

        var title = "Gruppe" + CompetitionWidgets.Number(View + 1, 2);
        CompetitionWidgets.Centered(s, Assets.Bold, 0, 319, 21, title, Colors.White, DebCupDrawScreen.TitleShadow);
        s.HLine(27, 24, 292 - 27 + 1, Colors.White);

        Widgets.Button(s, Assets, ButtonX, ButtonY, View == 0 ? "Gruppe 2" : "Gruppe 1");
        if (KnockoutKnown)
            Widgets.Button(s, Assets, KnockoutButton.X, ButtonY, "Endrunde");

        if (Group is not { } group || Host.Game is not { } game)
            return;

        var bold = Assets.Bold;
        var dashes = new string('-', 44);
        var rows = group.Table(game.Clubs);
        for (var n = 0; n < rows.Count; n++)
        {
            var y = FirstBaseline + RowPitch * n;
            var color = (byte)(Colors.White + 23 * (n & 1));
            CompetitionWidgets.Left(s, bold, RankX, y, $"{n + 1}.", color);
            CompetitionWidgets.Left(s, Assets.Flow, RankX, y + 7, dashes, color);
            if (n == 3)
            {
                s.HLine(RankX + 2, y + 8, 264 - 2 + 1, Colors.Red);
                s.HLine(RankX + 2, y + 9, 264 - 2 + 1, Colors.Red);
            }
            Widgets.Logo(s, Assets, rows[n].Club.LogoGraphic, FlagX[n & 1], y - 18);
            static string N(int v) => CompetitionWidgets.Number(v, 2);
            var row = rows[n];
            CompetitionWidgets.Left(s, bold, ScoreX, y, $"{N(row.PointsPlus)}:{N(row.PointsMinus)} {N(row.GoalsFor)}:{N(row.GoalsAgainst)}", color);
        }
        _info.Draw(s, Assets, game);
    }
}

/// <summary>
/// Anmeldung im Hauptmenü: Paarungen → WM (Paarungsansicht 0x2FC2F, Seite 8; #194) und Tabellen → Tabelle WM
/// (Gruppentabelle 0x2F5B6).
/// </summary>
internal static class WorldCupMenuEntries
{
    [MenuEntry("WM")]
    private static Screen Pairings(ScreenHost host, Screen back) => new CupPairingsScreen(host, back);

    [MenuEntry("Tabelle WM")]
    private static Screen Table(ScreenHost host, Screen back) => new WorldCupScreen(host, back);
}
