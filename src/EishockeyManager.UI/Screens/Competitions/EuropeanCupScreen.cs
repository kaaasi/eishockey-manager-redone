using EishockeyManager.Core;

namespace EishockeyManager.UI.Screens.Competitions;

/// <summary>
/// „Europacup  Gruppe1/2“ und „Europacup Endrunde“ (Paarungen → Europapokal): drei Spielrunden mit Hin- und
/// Rückspiel (Termin bzw. Ergebnis) und die Tabelle der Gruppe. Zeigt nur vorhandenen Zustand.
/// </summary>
/// <remarks>
/// Aufbau aus em.exe (Gruppen-Bildschirm Datei 0x31CD5, Wettbewerb 2 = Gruppen, 3 = Endrunde), vermessen am Video
/// „Die Oberfläche von EM“ (13:11):
/// <list type="bullet">
/// <item>[ORIGINAL] Fenster 13 × 10 Rahmenteile ab (4 | 3); Pokalsymbole kb\36 (22 × 22, y = 44) bei (13 | 7) und (286 | 7).</item>
/// <item>[ORIGINAL] Titel Bold zentriert 0 … 319, Grundlinie 23: „Europacup“ + zwei Leerzeichen + „Gruppe“ + Nummer (Mindestbreite 2)
///   bzw. „Europacup Endrunde“.</item>
/// <item>[ORIGINAL] Paarungen (Mini): Zeile y = 54 + 26 · Runde + 6 · Paarung; Heimverein x = 25 (Farbe 30), „-“ x = 120
///   (7), Gast x = 137 (Farbe 1), Manager-Vereine 24; Termine in der ersten Zeile einer Runde bei x = 230 / 276 (Farbe
///   30 / 1), gespielte Begegnungen stattdessen „Heim:Gast“ bei x = 226 / 272.</item>
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

    /// <param name="group">Nur für Bildschirmfotos: feste Gruppe statt der laufenden Saison.</param>
    public EuropeanCupScreen(ScreenHost host, Screen back, int view = 0, CupGroup? group = null) : base(host)
    {
        _back = back;
        View = view;
        _fixed = group;
    }

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
        var dates = SeasonCalendar.DaysOf(DayEvents.EuropeanCup);
        var firstDate = View == 2 ? EuropeanCup.GroupDates : 0;
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
                {
                    var day = dates[firstDate + round + 3 * leg];
                    var color = (byte)(30 - 29 * leg);
                    var (h, a) = leg == 0 ? (home, away) : (away, home);
                    if (group.Results.FirstOrDefault(m => m.Day == day && m.Home == h && m.Away == a) is { } match)
                        CompetitionWidgets.Left(s, mini, 226 + 46 * leg, y, $"{match.HomeGoals}:{match.AwayGoals}", color);
                    else if (p == 0)
                    {
                        var date = SeasonCalendar.LayoutDate(day);
                        CompetitionWidgets.Left(s, mini, 230 + 46 * leg, y,
                            CompetitionWidgets.Number(date.Day, 2) + "." + CompetitionWidgets.Number(date.Month, 1) + ".", color);
                    }
                }
            }
        }

        var flow = Assets.Flow;
        foreach (var row in group.Table(game.Clubs))
        {
            var y = TableBaseline + 10 * (row.Rank - 1);
            var color = Color(row.Club.Index, Colors.TileBackground);
            CompetitionWidgets.Left(s, flow, 39, y, $"{row.Rank}.  ", Colors.White);
            CompetitionWidgets.Left(s, flow, 57, y, row.Club.Name, color);
            static string N(int v) => CompetitionWidgets.Number(v, 2);
            CompetitionWidgets.Left(s, flow, 206, y, $"{N(row.PointsPlus)}:{N(row.PointsMinus)}   {N(row.GoalsFor)}:{N(row.GoalsAgainst)}", color);
        }
    }
}

/// <summary>Anmeldung im Hauptmenü: Paarungen → Europapokal.</summary>
internal static class EuropeanCupMenuEntries
{
    [MenuEntry("Europapokal")]
    private static Screen EuropeanCup(ScreenHost host, Screen back) => new EuropeanCupScreen(host, back);
}
