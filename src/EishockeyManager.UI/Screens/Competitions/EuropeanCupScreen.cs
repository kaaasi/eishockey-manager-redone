using EishockeyManager.Core;

namespace EishockeyManager.UI.Screens.Competitions;

/// <summary>
/// „Europacup  Gruppe1/2“ und „Europacup Endrunde“ (Paarungen → Europapokal): drei Spielrunden mit Hin- und
/// Rückspiel (Termin bzw. Ergebnis) und die Tabelle der Gruppe. Zeigt nur vorhandenen Zustand.
/// </summary>
/// <remarks>
/// Aufbau aus em.exe (Gruppen-Bildschirm Datei 0x31CD5, Wettbewerb 2 = Gruppen, 3 = Endrunde), vermessen am Video
/// „Die Oberfläche von EM“ (13:11) und mit Ergebnissen an folge04_01241–01247, eisbaeren_f51_03346, eisbaeren_f58_01323
/// (Spur pixel-wettbewerbe, Ablauf <c>pixel-europapokal</c>):
/// <list type="bullet">
/// <item>[ORIGINAL] Fenster 13 × 10 Rahmenteile ab (4 | 3); Pokalsymbole kb\36 (22 × 22, y = 44) bei (13 | 7) und (286 | 7).</item>
/// <item>[ORIGINAL] Titel Bold zentriert 0 … 319, Grundlinie 23: „Europacup“ + zwei Leerzeichen + „Gruppe“ + Nummer (Mindestbreite 2)
///   bzw. „Europacup Endrunde“.</item>
/// <item>[ORIGINAL] Paarungen (Mini): Zeile y = 54 + 26 · Runde + 6 · Paarung; Heimverein x = 25 (Farbe 30), „-“ x = 120
///   (7), Gast x = 137 (Farbe 1), Manager-Vereine 24; Termine in der ersten Zeile einer Runde bei x = 230 / 276 (Farbe
///   30 / 1), gespielte Begegnungen stattdessen das Ergebnis „links:rechts“ (zweistellig) bei x = 226 / 272.</item>
/// <item>[ORIGINAL] Ergebnis oder Termin je Spiel g = Runde + 3 · Rückspiel (PW-04, Nachbildung
///   <c>tools/reverse-engineering/live/aufstieg_europapokal.py</c>): Ergebnis, wenn g &lt; c oder (g = c und Merker „im Spiel“
///   [bp+0xA]) (Datei 0x324C7–0x32515); Termin nur in der ersten Zeile der Runde, ohne „im Spiel“ und für g ≥ c (Datei
///   0x3228D–0x322C6). c = Terminzahl [bp+0xC] + Merker „Drittelpause“ [bp+0x12] aus dem Seitenwähler (Datei 0x3AB43–0x3ABCE),
///   siehe <see cref="ShownGames"/>. Im Hauptmenü (Minute 0) ist c = gespielte Termine (DGROUP 0x7AF, Endrunde − 6): offene Spiele
///   der ersten Zeile mit Termin wie bei der Aufstiegsrunde (<c>folge06_01257</c>). Die Bilder ohne Termine nach den ersten
///   Spielen (eisbaeren_f20_02950, eisbaeren_f51_03314/03345/03346, eisbaeren_f58_01323) sind Seiten im Spielablauf (davor bzw.
///   danach das Eis des Live-Spiels; Tabelle nach Minute 60 mit, nach 20/40 ohne den laufenden Termin).</item>
/// <item>[ORIGINAL] Im Spielablauf (S04, Seite 3–5 des Seitenwählers mit Minute, <see cref="GroupLive"/>): keine Termine, der
///   laufende Termin mit dem laufenden Stand (Live-Tore 0x3183C nach 20/40/65/70), die Tabelle ab Minute 60 mit dem Termin
///   (0x30852, Datei 0x38691); keine Minute in der Kopfzeile. Die Knöpfe wechseln die Seite mit derselben Minute (Rückgabe an den
///   Seitenwähler, Datei 0x3ACBB–0x3ACC1).</item>
/// <item>[ORIGINAL] Tabelle (Flow): y = 135 + 10 · Platz, „n.“ x = 39 (7), Name x = 57, Punkte und Tore x = 206.</item>
/// <item>[ORIGINAL] Knöpfe: Rahmen 2 × 2 Teile bei (79 | 196) und (193 | 196), Beschriftung Mini Grundlinie 217.</item>
/// <item>[ORIGINAL] Endrunde vor dem siebten Termin: „Die Paarungen stehen noch nicht fest...“ (Flow, Grundlinie 100, Farbe 30).</item>
/// </list>
/// </remarks>
public sealed class EuropeanCupScreen : Screen
{
    public static readonly Rect Window = new(4, 3, 312, 190);
    public static readonly Rect LeftButton = new(79, 196, 48, 38);
    public static readonly Rect RightButton = new(193, 196, 48, 38);
    public const int TrophyGraphic = 36;
    public static readonly Rect TrophySource = new(0, 44, 22, 22);
    public const int PairingBaseline = 54;
    public const int TableBaseline = 135;
    public const string NotYetKnown = "Die Paarungen stehen noch nicht fest...";

    private readonly Screen _back;
    private readonly CupGroup? _fixed;

    /// <summary>0 = Gruppe 1, 1 = Gruppe 2, 2 = Endrunde.</summary>
    public int View { get; private set; }

    /// <summary>Seite im Spielablauf (S04) oder null (Hauptmenü).</summary>
    public GroupLive? Live { get; }

    /// <summary>Minute der Seite im Spielablauf oder null.</summary>
    public int? LiveMinute => Live?.Minute;

    /// <param name="group">Nur für Bildschirmfotos: feste Gruppe statt der laufenden Saison.</param>
    /// <param name="live">Seite im Spielablauf (S04): Minute, Termin und laufende Stände.</param>
    public EuropeanCupScreen(ScreenHost host, Screen back, int view = 0, CupGroup? group = null, GroupLive? live = null) : base(host)
    {
        _back = back;
        View = view;
        _fixed = group;
        Live = live;
    }

    /// <summary>
    /// [ORIGINAL] Grenze c der Ergebnisse von 0x31CD5: Terminzahl [bp+0xC] = <paramref name="played"/> + (Minute ∈ {0, 60}) − 1
    /// (Seitenwähler Datei 0x3AB58–0x3AB6F, 0x3ABA0–0x3ABB7, Aufstiegsrunde 0x3AB0F–0x3AB27) plus Merker „Drittelpause“
    /// [bp+0x12] = (Minute ∈ {20, 40}) (Datei 0x3A8A3–0x3A8B3, 0x322A4/0x324CC). <paramref name="played"/>: vor dem Termin gespielte
    /// Termine der Ansicht (Europapokal min(DGROUP 0x7AF, 6) bzw. Endrunde 0x7AF − 6, Aufstiegsrunde 2 · 0x7B0 + (0xCCA3 &gt; 0) +
    /// (0xCCA3 &gt; 2)). Im Hauptmenü (Minute 0) also die gespielten Termine, nach 20, 40 und 60 der laufende Termin mit, nach 65/70
    /// nicht (<c>aufstieg_europapokal.py</c>).
    /// </summary>
    public static int ShownGames(int played, int minute) =>
        played + (minute is 0 or 60 ? 1 : 0) - 1 + (minute is 20 or 40 ? 1 : 0);

    /// <summary>[ORIGINAL] Ergebnis für Spiel <paramref name="game"/> (Datei 0x324C7–0x32515): g &lt; c oder g = c im Spiel.</summary>
    public static bool ShowsResult(int game, int shown, bool inGame) => game < shown || (inGame && game == shown);

    /// <summary>[ORIGINAL] Termin für Spiel <paramref name="game"/> (Datei 0x3228D–0x322C6): erste Zeile, nicht im Spiel, g ≥ c.</summary>
    public static bool ShowsDate(int game, int shown, bool inGame, bool firstLine) => firstLine && !inGame && game >= shown;

    public string Title => View == 2 ? "Europacup Endrunde" : "Europacup  Gruppe" + CompetitionWidgets.Number(View + 1, 2);

    /// <summary>
    /// Ziele der Knöpfe wie im Original (0x31F04): Gruppe 1 → Gruppe 2 / Endrunde, Gruppe 2 → Gruppe 1 / Endrunde,
    /// Endrunde → Gruppe 2 / Gruppe 1.
    /// </summary>
    public (int Left, int Right) Targets => View switch
    {
        0 => (1, 2),
        1 => (0, 2),
        _ => (1, 0),
    };

    public static string Label(int view) => view == 2 ? "Endrunde" : "Gruppe" + CompetitionWidgets.Number(view + 1, 2);

    /// <summary>Gruppe der Ansicht oder null (Endrunde noch nicht ermittelt, Europapokal aus).</summary>
    public CupGroup? Group
    {
        get
        {
            if (_fixed is not null)
                return _fixed;
            var cup = Host.Game?.EuropeanCup;
            if (cup is null)
                return null;
            return View < 2 ? cup.Groups.ElementAtOrDefault(View) : cup.FinalRound;
        }
    }

    public override void Update(InputState input)
    {
        if (input.Pressed(Key.Escape) || input.RightClick)
        {
            Host.Show(_back);
            return;
        }
        if (input.Clicked(LeftButton))
            View = Targets.Left;
        else if (input.Clicked(RightButton))
            View = Targets.Right;
        else if (input.LeftClick || input.Keys.Count > 0)
            Host.Show(_back);
    }

    public override void Draw(Surface s)
    {
        s.Palette = Assets.Palette;
        s.Blit(Assets.Image(MainMenuScreen.Background), 0, 0);
        Widgets.Frame(s, Assets, Window, FrameStyle.Orange);
        var trophies = Assets.Image(TrophyGraphic);
        s.Blit(trophies, TrophySource, 13, 7);
        s.Blit(trophies, TrophySource, 286, 7);
        Widgets.Frame(s, Assets, LeftButton, FrameStyle.Orange);
        Widgets.Frame(s, Assets, RightButton, FrameStyle.Orange);

        CompetitionWidgets.Centered(s, Assets.Bold, 0, 319, 23, Title, Colors.White, DebCupDrawScreen.TitleShadow);
        var (left, right) = Targets;
        CompetitionWidgets.Centered(s, Assets.Mini, LeftButton.X, LeftButton.X + 49, 217, Label(left), Colors.White, null);
        CompetitionWidgets.Centered(s, Assets.Mini, RightButton.X, RightButton.X + 49, 217, Label(right), Colors.White, null);

        if (Group is not { } group || Host.Game is not { } game)
        {
            CompetitionWidgets.Centered(s, Assets.Flow, 0, 320, 100, NotYetKnown, Colors.TileBackground, Colors.Black);
            return;
        }

        var managers = game.Managers.Select(m => m.ClubIndex).ToHashSet();
        byte Color(int club, byte normal) => managers.Contains(club) ? Colors.Yellow : normal;
        var mini = Assets.Mini;
        for (var round = 0; round < 3; round++)
        {
            var pairs = group.Pairings(round);
            for (var p = 0; p < pairs.Count; p++)
            {
                var y = PairingBaseline + 26 * round + 6 * p;
                var (home, away) = pairs[p];
                CompetitionWidgets.Left(s, mini, 25, y, game.Clubs[home].Name, Color(home, Colors.TileBackground));
                CompetitionWidgets.Left(s, mini, 120, y, "-", Colors.White);
                CompetitionWidgets.Left(s, mini, 137, y, game.Clubs[away].Name, Color(away, 1));
                for (var leg = 0; leg < 2; leg++)
                    if (Cell(round, p, leg) is { } cell)
                        CompetitionWidgets.Left(s, mini, cell.X, y, cell.Text, (byte)(30 - 29 * leg));
            }
        }

        var flow = Assets.Flow;
        foreach (var row in Standings!)
        {
            var y = TableBaseline + 10 * (row.Rank - 1);
            var color = Color(row.Club.Index, Colors.TileBackground);
            CompetitionWidgets.Left(s, flow, 39, y, $"{row.Rank}.  ", Colors.White);
            CompetitionWidgets.Left(s, flow, 57, y, row.Club.Name, color);
            static string N(int v) => CompetitionWidgets.Number(v, 2);
            CompetitionWidgets.Left(s, flow, 206, y, $"{N(row.PointsPlus)}:{N(row.PointsMinus)}   {N(row.GoalsFor)}:{N(row.GoalsAgainst)}", color);
        }
    }

    /// <summary>
    /// Ergebnis „links:rechts“ (x = 226 / 272) oder Termin „tt.m.“ (x = 230 / 276) der Paarung <paramref name="pairing"/> in Runde
    /// <paramref name="round"/> für Hin- (0) oder Rückspiel (1), sonst null (Regeln im Kommentar der Klasse).
    /// </summary>
    public (string Text, int X, bool IsDate)? Cell(int round, int pairing, int leg)
    {
        if (Group is not { } group)
            return null;
        var dates = SeasonCalendar.DaysOf(DayEvents.EuropeanCup);
        var firstDate = View == 2 ? EuropeanCup.GroupDates : 0;
        var shown = ShownGames(PlayedDates(group, dates, firstDate), Live?.Minute ?? 0);
        var (home, away) = group.Pairings(round)[pairing];
        var g = round + 3 * leg;
        var day = dates[firstDate + g];
        if (ShowsResult(g, shown, Live is not null))
        {
            // [ORIGINAL] Auch das Rückspiel steht „links:rechts“ wie die Paarung (folge04_01241: Chamonix – Dynamo „5: 6  2: 6“
            // bei 12:0 Punkten für Dynamo; eisbaeren_f58_01323), beide Zahlen zweistellig aufgefüllt wie in der Tabelle.
            return Result(group, day, home, away, leg) is { } r
                ? (CompetitionWidgets.Number(r.Left, 2) + ":" + CompetitionWidgets.Number(r.Right, 2), 226 + 46 * leg, false)
                : null;
        }
        if (!ShowsDate(g, shown, Live is not null, pairing == 0))
            return null;
        var date = SeasonCalendar.LayoutDate(day);
        return (CompetitionWidgets.Number(date.Day, 2) + "." + CompetitionWidgets.Number(date.Month, 1) + ".", 230 + 46 * leg, true);
    }

    /// <summary>Tabelle der Ansicht (im Spielablauf ab Minute 60 mit dem laufenden Termin) oder null ohne Gruppe.</summary>
    public IReadOnlyList<TableRow>? Standings
    {
        get
        {
            if (Group is not { } group || Host.Game is not { } game)
                return null;
            var dates = SeasonCalendar.DaysOf(DayEvents.EuropeanCup);
            var firstDate = View == 2 ? EuropeanCup.GroupDates : 0;
            return Table(group, game.Clubs, dates, firstDate, PlayedDates(group, dates, firstDate));
        }
    }

    /// <summary>
    /// Vor dem Termin gespielte Termine der Ansicht (0x7AF bzw. 0x7AF − 6): im Spielablauf die Termine vor dem laufenden, sonst die
    /// Termine mit Ergebnissen (alle Gruppen eines Termins spielen zusammen, Datei 0x3139F).
    /// </summary>
    private int PlayedDates(CupGroup group, IReadOnlyList<int> dates, int firstDate)
    {
        if (Live is { } live)
            return Math.Clamp(dates.Count(d => d < live.Today) - firstDate, 0, EuropeanCup.GroupDates);
        var played = 0;
        for (var g = 0; g < EuropeanCup.GroupDates; g++)
            if (group.Results.Any(m => m.Day == dates[firstDate + g]))
                played = g + 1;
        return played;
    }

    /// <summary>Ergebnis „links:rechts“ aus Sicht der Paarung (Hinspiel Heim – Gast); laufender Termin aus dem Spielablauf.</summary>
    private DebCupResult? Result(CupGroup group, int day, int home, int away, int leg)
    {
        if (Live is { } live && day == live.Today && live.Score(home, away) is { } running)
            return running;
        var (h, a) = leg == 0 ? (home, away) : (away, home);
        if (group.Results.FirstOrDefault(m => m.Day == day && m.Home == h && m.Away == a) is not { } match)
            return null;
        return leg == 0 ? new DebCupResult(match.HomeGoals, match.AwayGoals, 0) : new DebCupResult(match.AwayGoals, match.HomeGoals, 0);
    }

    /// <summary>
    /// Tabelle der Gruppe; im Spielablauf [ORIGINAL] ohne den laufenden Termin bis Minute 60, danach mit dessen Ständen (0x30852 bei
    /// Minute 60, Datei 0x38691; eisbaeren_f51_03314 nach 20/40 ohne, eisbaeren_f20_02951 nach 60 mit dem Termin).
    /// </summary>
    private IReadOnlyList<TableRow> Table(CupGroup group, IReadOnlyList<Club> clubs, IReadOnlyList<int> dates, int firstDate, int played)
    {
        if (Live is not { } live)
            return group.Table(clubs);
        var rounds = group.Results.Where(r => r.Day < live.Today).GroupBy(r => r.Day)
            .Select(day => day.Select(r => (r.Home, r.Away, r.HomeGoals, r.AwayGoals)).ToList()).ToList();
        if (live.Minute >= 60 && played < EuropeanCup.GroupDates && dates[firstDate + played] == live.Today)
        {
            var today = new List<(int, int, int, int)>();
            foreach (var (home, away) in group.Pairings(played))
                if (Result(group, live.Today, home, away, 0) is { } r)
                    today.Add((home, away, r.Left, r.Right));
            rounds.Add(today);
        }
        return CupGroup.Standings(clubs, group.Teams, rounds);
    }
}

/// <summary>
/// Gruppenseite im Spielablauf (S04, 0x31CD5 mit Merker „im Spiel“): <paramref name="Minute"/> nach dem Drittel,
/// <paramref name="Today"/> Tag des laufenden Termins, <paramref name="Score"/> laufender Stand einer Paarung aus Sicht des
/// ersten Vereins (null, wenn der Termin sie nicht enthält).
/// </summary>
public sealed record GroupLive(int Minute, int Today, Func<int, int, DebCupResult?> Score);

/// <summary>Anmeldung im Hauptmenü: Paarungen → Europapokal, mit gemerkter Seite (DGROUP 0x5126, <see cref="EuropeanCupPages"/>).</summary>
internal static class EuropeanCupMenuEntries
{
    [MenuEntry("Europapokal")]
    private static Screen EuropeanCup(ScreenHost host, Screen back) =>
        EuropeanCupPages.FromTile(host, back, EuropeanCupPages.Tile.Pairings);
}
