using EishockeyManager.Core;

namespace EishockeyManager.UI.Screens.Competitions;

/// <summary>Eine Begegnung der WM-Endrunde; <see cref="Dots"/> Punkte hinter dem Ergebnis (0 = reguläre Spielzeit).</summary>
public sealed record WorldCupKnockoutRow(int Left, int Right, int LeftGoals, int RightGoals, int Dots);

/// <summary>Ein Abschnitt „VIERTELFINALE“, „SPIELE 5.-8.“ … mit seinen Begegnungen.</summary>
public sealed record WorldCupKnockoutSection(string Title, IReadOnlyList<WorldCupKnockoutRow> Rows);

/// <summary>
/// „WM-Endrundenspiele“: die K.-o.-Begegnungen der WM nach Abschnitten, mit Ergebnissen. Zeigt nur vorhandenen Zustand.
/// </summary>
/// <remarks>
/// [ORIGINAL] Dieselbe Bildschirmfunktion wie „DEB-Pokalspiele“ (Datei 0x32E23, Wettbewerb 3; Titel DGROUP 0xCD0 + 4 · 3,
/// Knopftext DGROUP 0x650 + 4 · 3 = „Gruppe“), vermessen an <c>eisbaeren_f35_00646</c> (nur Viertelfinale) und
/// <c>eisbaeren_f44_01330</c> (alle Abschnitte, Anzeige im Spielablauf mit „60.Minute“; Spur pixel-wettbewerbe):
/// <list type="bullet">
/// <item>[ORIGINAL] Zeilenzahl n = Begegnungen + 1 − 6 + Abschnitte (Datei 0x32F92, 0x32FB0: Tabelle DGROUP 0x4DFD nach der
///   WM-Stufe [0x7AE] = 1 / 3 / 7 Überschriften), höchstens 0; Rahmen wie bei <see cref="DebCupGamesScreen.FrameFor"/>
///   (Teile min(11, (7 · (n + 4) + 60) / 19), Oberkante (217 − 19 · h) / 2). Ohne laufendes Spiel steht der Rahmen bei mehr als
///   acht Teilen oben (y = 2), damit der Knopf frei bleibt (0x33074).</item>
/// <item>[ORIGINAL] Titel, Rahmen und Abschnitte wie „DEB-Pokalspiele“; Pokalbild kb\36 Quelle (0 | 66) statt (0 | 22) (0x3312A).</item>
/// <item>[ORIGINAL] Knopf 2 × 2 Teile bei (136 | 201), „Gruppe“ zentriert bei x = 160, Grundlinie 221, Farbe 7, ohne Schatten
///   (0x3307E–0x330E0); nicht im laufenden Spiel.</item>
/// <item>[ORIGINAL] Ergebnis „g: g“ (zweistellig) bei x = 233, Farbe 1, Punkte bei x + 22 / + 24.</item>
/// <item>[ORIGINAL] Noch nicht gespielte Begegnungen zeigen „0: 0“ (<c>eisbaeren_f35_00646</c>).</item>
/// <item>[ORIGINAL] Vor der Endrunde (WM-Stufe DGROUP 0x7AE &lt; 5, Datei 0x3337B) ein Rahmen ohne Zeilen (5 Teile ab y = 70) mit
///   „Die Paarungen stehen noch nicht fest...“ (Flow, Grundlinie y + 34, Farbe 30) und Knopf „Gruppe“; DOSBox
///   <c>dosbox/nur-weltkugel/24</c>. Erreichbar über „Endrunde“ der Paarungsansicht „Gruppe n   m.Spieltag“ (<see cref="CupPairingsScreen"/>,
///   DOSBox 20, 21; #194). [ORIGINAL] Im Menü des Bundestrainers öffnet „Paarungen → WM“ ab WM-Stufe 5 direkt diese Seite (A293,
///   DOSBox `file49_7`; <c>WorldCupMenuEntries.Pairings</c>); die Gruppentabelle hat keinen Knopf „Endrunde“ (`file49_6`).</item>
/// <item>[ORIGINAL] Punkte hinter dem Ergebnis = Marken + 30 (Anzeige 0x338AB–0x338E3): einer nach Penaltyschießen mit Endstand
///   (schneller Weg 0x37761, Minutenweg 0x39789), zwei nach einem Tor in der Verlängerung (0x38402; eisbaeren_f44_01330 „4: 3..“
///   neben „4: 3.“) – <see cref="WorldCup.MarksOf"/>, <see cref="CupMatch"/>. [APPROX] Die Herkunft des Stands vor 0x38402 ist
///   nicht verfolgt.</item>
/// <item>[APPROX] Die Anzeige im Spielablauf („60.Minute“) gibt es im Remake noch nicht (Spur livespiel); vorbereitet über
///   <c>liveMinute</c>.</item>
/// </list>
/// </remarks>
public sealed class WorldCupKnockoutScreen : Screen
{
    public const string Title = "WM-Endrundenspiele";
    public const string ButtonText = "Gruppe";

    /// <summary>
    /// WM-Stufe, ab der „Paarungen → WM“ im Menü des Bundestrainers diese Seite öffnet (A293): nach den fünf Gruppenterminen
    /// (DGROUP 0x7AE = 5, Datei 0x44B15).
    /// </summary>
    public const int CoachStartStage = 5;

    /// <summary>Text vor der Endrunde (DGROUP 0x1E58, wie beim Europapokal).</summary>
    public const string NotYetKnown = EuropeanCupScreen.NotYetKnown;
    public static readonly Rect Button = new(136, 201, 48, 38);
    public static readonly Rect TrophySource = new(0, 66, 22, 22);

    /// <summary>[ORIGINAL] Ergebnisspalte: x = 187 + 46 (nur Wettbewerb 3) wie „DEB-Pokalspiele“ ohne dessen Terminversatz
    /// (Datei 0x3394E–0x33996), Farbe 1 wie das erste Spiel.</summary>
    public const int ResultX = 187 + 46;

    /// <summary>[ORIGINAL] Abstand Überschrift → erste Zeile 7 statt 8 wie bei „DEB-Pokalspiele“: Wettbewerb 3 zeichnet die
    /// Überschriften im Zeilenlauf (Datei 0x334B9–0x3350C, Tabelle DGROUP 0x4E06, danach + 7); belegt in eisbaeren_f44_01330.</summary>
    public const int HeadingPitch = 7;

    private readonly Screen _back;
    private readonly IReadOnlyList<WorldCupKnockoutSection>? _fixed;
    private readonly IReadOnlyCollection<int>? _highlighted;
    private readonly int? _liveMinute;

    private readonly Func<Screen>? _groupPage;

    public WorldCupKnockoutScreen(ScreenHost host, Screen back) : base(host) => _back = back;

    /// <summary>
    /// Aus einer Gruppenseite geöffnet: „Gruppe“ kehrt zu dieser Seite zurück. [ORIGINAL] Der Seitenwähler 0x3A88A setzt
    /// nach Seite 10 mit Rückgabe ≠ 0 die vorige Seite (Datei 0x3AC75–0x3AC81) – Paarungen bzw. Tabelle der Gruppe.
    /// </summary>
    public WorldCupKnockoutScreen(ScreenHost host, Screen back, Func<Screen> groupPage) : base(host)
        => (_back, _groupPage) = (back, groupPage);

    /// <summary>Feste Abschnitte statt des Spielstands (Bildschirmfotos); <paramref name="liveMinute"/> zeigt die Anzeige im
    /// Spielablauf („n.Minute“, ohne Knopf).</summary>
    public WorldCupKnockoutScreen(ScreenHost host, Screen back, IReadOnlyList<WorldCupKnockoutSection> sections,
        IReadOnlyCollection<int>? highlighted = null, int? liveMinute = null) : base(host)
        => (_back, _fixed, _highlighted, _liveMinute) = (back, sections, highlighted, liveMinute);

    /// <summary>Abschnitte der feststehenden K.-o.-Termine.</summary>
    public IReadOnlyList<WorldCupKnockoutSection> Sections => _fixed ?? (Host.Game?.WorldCup is { } wm ? Of(wm) : []);

    public static IReadOnlyList<WorldCupKnockoutSection> Of(WorldCup wm)
    {
        var sections = new List<(string Title, List<WorldCupKnockoutRow> Rows)>();
        for (var stage = 0; stage < 3; stage++)
        {
            var pairs = wm.KnockoutPairings(stage);
            for (var k = 0; k < pairs.Count; k++)
            {
                var title = WorldCup.KnockoutNames[stage][k];
                if (sections.Count == 0 || sections[^1].Title != title || k == 0)
                    sections.Add((title, []));
                var (home, away) = pairs[k];
                var match = stage < wm.Knockout.Count ? wm.Knockout[stage].ElementAtOrDefault(k) : null;
                sections[^1].Rows.Add(match is null
                    ? new WorldCupKnockoutRow(home, away, 0, 0, 0)
                    : new WorldCupKnockoutRow(home, away, match.HomeGoals, match.AwayGoals, wm.MarksOf(stage, k)));
            }
        }
        return sections.Select(s => new WorldCupKnockoutSection(s.Title, s.Rows)).ToList();
    }

    /// <summary>[ORIGINAL] Rahmenteile (mit Zusatzteil) und Oberkante, siehe Klassenkommentar.</summary>
    /// <summary>[ORIGINAL] Zeile im Spielablauf: sieben Leerzeichen (DGROUP 0x4E5F, Datei 0x3325C) vor „n.Minute“, zentriert –
    /// steht deshalb rechts der Mitte (eisbaeren_f44_01330).</summary>
    public static string LiveLine(int minute) => $"       {minute}.Minute";

    public static (int Parts, int Y) FrameFor(int rows, int sections, bool live)
    {
        var n = Math.Max(rows + 1 - 6 + sections, 0);
        var h = Math.Min(11, (7 * (n + 4) + 60) / 19);
        var y = (217 - 19 * h) / 2;
        h = Math.Max(h, 2);
        if (!live && h > 8)
            y = 2;
        return (h + 1, y);
    }

    public override void Update(InputState input)
    {
        if (input.Pressed(Key.Escape) || input.RightClick)
            Host.Show(_back);
        else if (_liveMinute is null && input.Clicked(Button))
            Host.Show(_groupPage?.Invoke() ?? new WorldCupScreen(Host, _back));
        else if (input.LeftClick || input.Keys.Count > 0)
            Host.Show(_back);
    }

    public override void Draw(Surface s)
    {
        s.Palette = Assets.Palette;
        s.Blit(Assets.Image(MainMenuScreen.Background), 0, 0);
        var live = _liveMinute is not null;
        var sections = Sections;
        var (parts, y) = FrameFor(sections.Sum(x => x.Rows.Count), sections.Count, live);
        if (!live)
        {
            Widgets.Frame(s, Assets, Button, FrameStyle.Orange);
            CompetitionWidgets.Centered(s, Assets.Mini, 160, 160, 221, ButtonText, Colors.White, null);
        }
        Widgets.Frame(s, Assets, new Rect(DebCupGamesScreen.FrameX, y, DebCupGamesScreen.FramePartWidth * DebCupGamesScreen.FrameParts,
            DebCupGamesScreen.FramePartHeight * parts), FrameStyle.Orange);
        var trophies = Assets.Image(DebCupGamesScreen.TrophyGraphic);
        s.Blit(trophies, TrophySource, 13, y + 4);
        s.Blit(trophies, TrophySource, 286, y + 4);
        CompetitionWidgets.Centered(s, Assets.Bold, 0, 319, y + 19, Title, Colors.White, DebCupDrawScreen.TitleShadow);
        if (_liveMinute is { } minute)
            CompetitionWidgets.Centered(s, Assets.Flow, 0, 319, y + 34, LiveLine(minute), Colors.TileBackground, Colors.Black);
        else if (sections.Count == 0)
            // [ORIGINAL] Datei 0x3337B–0x333C8: vor der Endrunde (DGROUP 0x7AE < 5) Text DGROUP 0x1E58, Flow, Grundlinie
            // Oberkante + 0x10 + 0x12, Farbe 30, Schatten 0 (DOSBox dosbox/nur-weltkugel/24).
            CompetitionWidgets.Centered(s, Assets.Flow, 0, 319, y + 34, NotYetKnown, Colors.TileBackground, Colors.Black);

        if (Host.Game is not { } game)
            return;
        var highlighted = (_highlighted ?? game.Managers.Select(m => m.ClubIndex)
            .Concat(game.NationalCoach is { } coach ? [coach.Nation] : [])).ToHashSet();
        var mini = Assets.Mini;
        var line = y + 46;
        foreach (var section in sections)
        {
            CompetitionWidgets.Centered(s, mini, 15, 183, line, section.Title, Colors.White, Colors.Black);
            line += HeadingPitch;
            foreach (var row in section.Rows)
            {
                CompetitionWidgets.Centered(s, mini, 15, 94, line, game.Clubs[row.Left].Name,
                    highlighted.Contains(row.Left) ? Colors.Yellow : Colors.TileBackground, null);
                CompetitionWidgets.Centered(s, mini, 98, 100, line, "-", Colors.White, null);
                CompetitionWidgets.Centered(s, mini, 104, 183, line, game.Clubs[row.Right].Name,
                    highlighted.Contains(row.Right) ? Colors.Yellow : (byte)1, null);
                var x = ResultX;
                var color = DebCupGamesScreen.ResultColors[0];
                CompetitionWidgets.Left(s, mini, x, line,
                    $"{CompetitionWidgets.Number(row.LeftGoals, 2)}:{CompetitionWidgets.Number(row.RightGoals, 2)}", color);
                for (var d = 0; d < row.Dots; d++)
                    s.Set(x + 22 + 2 * d, line, color);
                line += DebCupGamesScreen.RowPitch;
            }
        }
    }
}
