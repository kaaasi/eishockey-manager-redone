using EishockeyManager.Core;
using EishockeyManager.UI.Screens.Competitions;

namespace EishockeyManager.UI.Screens.Statistics;

/// <summary>
/// „STÄRKEN Oberliga - Gesamt“ usw. (Hauptmenü → Stärken): Stärken-Tabelle einer Liga oder der Länderteams.
/// Zeigt nur vorhandenen Zustand; die Knöpfe wechseln die Ansicht.
/// </summary>
/// <remarks>
/// Aufbau aus em.exe (Datei 0x51206), vermessen am Video „Die Oberfläche von EM“ (13:18–13:36):
/// <list type="bullet">
/// <item>[ORIGINAL] Fenster 10 × 12 Rahmenteile ab (30 | 5), Füllfläche (36 | 9) … (262 | 211) Farbe 4; Titel
///   „STÄRKEN “ + Liga + „ - “ + Ansicht (Flow), zentriert 61 … 248, Grundlinie 19, Farbe 10, Schatten 28;
///   Linie (43 … 261 | 25) Farbe 0. Die Kopfzeile „PL. VEREIN KONDITION …“ wird von der Füllfläche übermalt.</item>
/// <item>[ORIGINAL] Zeile n: Grundlinie 34 + 15 n; „n. Name“ x = 45, Werte ab x = 142 (je Zahl gefolgt von zwölf
///   Leerzeichen), Farbe 30, Vereine der Manager 24; Trennlinie (43 … 261 | 43 + 15 n) Farbe 0.</item>
/// <item>[ORIGINAL] Smiley bei (240 | 36 + 15 n); Trendbalken zwei Zeilen (37/38 + 15 n) von x = 250 bis 204 + Wert,
///   Farbe 12 (über 50), 24 (= 50), 18 (unter 50).</item>
/// <item>[ORIGINAL] Knöpfe „Gesamt“, „Abwehr“, „Sturm“ bei x = 43 + 80 i, y = 212.</item>
/// <item>[APPROX] Smiley: Das Original zeichnet ihn aus einem Wertverlauf je Verein (zu Saisonbeginn leer → gelb);
///   das Remake führt diesen Verlauf nicht und zeigt immer das gelbe Gesicht.</item>
/// </list>
/// </remarks>
public sealed class StrengthScreen : Screen
{
    public static readonly Rect Window = new(30, 5, 240, 228);
    public const int FirstBaseline = 34;
    public const int RowPitch = 15;
    public const int NameX = 45;
    public const int ValuesX = 142;
    public const int ButtonY = 212;
    public static Rect ButtonRect(int i) => Widgets.ButtonRect(43 + 80 * i, ButtonY);

    private readonly Screen _back;
    private readonly League? _league;

    public StrengthView View { get; private set; }

    /// <param name="league">Liga oder null für die Länderteams.</param>
    public StrengthScreen(ScreenHost host, Screen back, League? league, StrengthView view = StrengthView.Gesamt) : base(host)
    {
        _back = back;
        _league = league;
        View = view;
    }

    public string Title => "STÄRKEN " + (_league switch
    {
        League.Bundesliga => "Bundesliga",
        League.ZweiteLiga => "Zweite Liga",
        League.Oberliga => "Oberliga",
        _ => "Länderteams",
    }) + " - " + View;

    public IReadOnlyList<StrengthRow> Rows
    {
        get
        {
            if (Host.Game is not { } game)
                return [];
            var clubs = _league is { } league
                ? game.LeagueClubs(league)
                : game.Clubs.Where(c => c.Index is >= NationalTeams.First and <= NationalTeams.Last);
            return StrengthTable.Compute(game, clubs, View);
        }
    }

    /// <summary>
    /// [ORIGINAL] Klick auf eine Zeile öffnet das Vereins-/Länderfenster 0x128:0x2A (Datei 0x4D385, Seite 2 = Gesamt, ohne dritten
    /// Knopf; DOSBox 44): Datei 0x51A02–0x51A59, nur linke Taste, Maus-y (Feld +0 des Mausblocks) mit 27 &lt; y &lt; 213, Zeile =
    /// (y − 27) / 15 (Division zur 0 hin, also auch y 13 … 26 nicht, aber y 28 … 41 → Zeile 0), gültig unter der Zahl der Teams der
    /// Liste (DGROUP 0x7B1). Die Maus-x wird nicht geprüft.
    /// </summary>
    public static int? RowAt(int y, int rowCount)
    {
        if (y <= 27 || y >= 213)
            return null;
        var row = (y - 27) / RowPitch;
        return row < rowCount ? row : null;
    }

    /// <summary>Info-Fenster aus der Liste (Verein oder Land), ändert keinen Spielzustand.</summary>
    public InfoBarWindows InfoWindows { get; } = new();

    public override void Update(InputState input)
    {
        if (InfoWindows.Nation.Update(input) | InfoWindows.Club.Update(input))
            return;
        if (input.LeftClick && RowAt(input.MouseY, Rows.Count) is { } clicked)
        {
            InfoWindows.Show(Rows[clicked].Club.Index);
            return;
        }
        if (input.Pressed(Key.Escape) || input.RightClick)
        {
            Host.Show(_back);
            return;
        }
        for (var i = 0; i < 3; i++)
            if (input.Clicked(ButtonRect(i)))
            {
                View = (StrengthView)i;
                return;
            }
        if (input.LeftClick && !Window.Contains(input.MouseX, input.MouseY))
            Host.Show(_back);
    }

    public override void Draw(Surface s)
    {
        s.Palette = Assets.Palette;
        s.Blit(Assets.Image(MainMenuScreen.Background), 0, 0);
        Widgets.Frame(s, Assets, Window, FrameStyle.Orange);

        var mini = Assets.Mini;
        // Das Original schreibt die Kopfzeile (Farbe 1, Grundlinie 23) und übermalt sie gleich danach mit der
        // Füllfläche (36 | 9) … (262 | 211) in Farbe 4 (0x30:0x800) – im Video ist sie nicht zu sehen.
        s.Fill(new Rect(36, 9, 262 - 36 + 1, 211 - 9 + 1), 4);
        CompetitionWidgets.Centered(s, Assets.Flow, 61, 248, 19, Title, 10, 28);
        s.HLine(43, 25, 261 - 43 + 1, Colors.Black);

        var managers = Host.Game?.Managers.Select(m => m.ClubIndex).ToHashSet() ?? [];
        var rows = Rows;
        const string gap = "            ";
        for (var n = 0; n < rows.Count; n++)
        {
            var row = rows[n];
            var y = FirstBaseline + RowPitch * n;
            var color = managers.Contains(row.Club.Index) ? Colors.Yellow : Colors.TileBackground;
            CompetitionWidgets.Left(s, mini, NameX, y, $"{CompetitionWidgets.Number(n + 1, 2)}. {row.Club.Name}", color);
            static string N(int v) => CompetitionWidgets.Number(v, 2);
            CompetitionWidgets.Left(s, mini, ValuesX, y, N(row.Condition) + gap + N(row.Technique) + gap + N(row.Form) + gap + N(row.Average), color);
            s.HLine(43, 43 + RowPitch * n, 261 - 43 + 1, Colors.Black);

            Widgets.Smiley(s, Assets, Widgets.Mood.Same, 240, 36 + RowPitch * n);
            var barColor = row.Trend > 50 ? (byte)12 : row.Trend == 50 ? Colors.Yellow : Colors.Red;
            var end = 204 + row.Trend;
            for (var k = 0; k < 2; k++)
                s.HLine(250, 37 + RowPitch * n + k, end - 250 + 1, barColor);
        }

        for (var i = 0; i < 3; i++)
            Widgets.Button(s, Assets, 43 + 80 * i, ButtonY, ((StrengthView)i).ToString()); // im Video alle ungedrückt
        if (Host.Game is { } game)
            InfoWindows.Draw(s, Assets, game);
    }
}

/// <summary>Anmeldung im Hauptmenü: Stärken → 1. Liga, 2. Liga, Oberliga, Länderteams.</summary>
internal static class StrengthMenuEntries
{
    [MenuEntry("Stärken 1. Liga")]
    private static Screen First(ScreenHost host, Screen back) => new StrengthScreen(host, back, League.Bundesliga);

    [MenuEntry("Stärken 2. Liga")]
    private static Screen Second(ScreenHost host, Screen back) => new StrengthScreen(host, back, League.ZweiteLiga);

    [MenuEntry("Stärken Oberliga")]
    private static Screen Oberliga(ScreenHost host, Screen back) => new StrengthScreen(host, back, League.Oberliga);

    [MenuEntry("Stärken Länderteams")]
    private static Screen Nations(ScreenHost host, Screen back) => new StrengthScreen(host, back, null);
}
