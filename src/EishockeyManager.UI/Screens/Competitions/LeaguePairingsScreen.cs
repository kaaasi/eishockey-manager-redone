using EishockeyManager.Core;

namespace EishockeyManager.UI.Screens.Competitions;

/// <summary>
/// „Paarungen 1. Liga / 2. Liga / Oberliga“ (Hauptmenü → Paarungen): die sechs Begegnungen eines Spieltags mit Platz,
/// Stärke und Ergebnis. Zeigt nur vorhandenen Zustand; die Pfeilknöpfe blättern zwischen den Spieltagen.
/// </summary>
/// <remarks>
/// Aufbau aus em.exe (Bildschirmfunktion 0x128:0x39, Datei 0x4E8A3; Aufruf über den Seitenwähler 0xF8:0x3E, Seite 7).
/// Es gibt kein Video dazu: Der Aufbau stammt allein aus der Disassemblierung (<c>docs/wettbewerbe.md</c>, Paarungen).
/// <list type="bullet">
/// <item>[ORIGINAL] Fenster (4 | 15) 312 × 152 orange; drei Knöpfe (Rahmen 48 × 38) bei x = 30 / 140 / 250, y = 180.</item>
/// <item>[ORIGINAL] Pfeile aus kb\4 (29 × 9, Quelle (19 | 104) links, (19 | 125) rechts; gedrückt y + 10) bei (40 | 195) und
///   (260 | 195). Mittlerer Knopf „Play-Off“ (Bundesliga) bzw. „Aufstieg“ (übrige), Mini, zentriert 147 … 182, Grundlinie 200.</item>
/// <item>[ORIGINAL] Kopfzeile (Flow, 5 … 313, Grundlinie 30, Farbe 10, Schatten 28): Liga + drei Leerzeichen + Spieltag
///   (Mindestbreite 2, wenn der zuvor gezeichnete Tag ein gespielter war) + „.Spieltag  “ + Datum „t.m.“. Linien
///   (13 … 303 | 34) Farbe 7 und (14 … 304 | 35) Farbe 1, Streifen (8 | 23) … (310 | 33) Farbe 4, Körper (12 | 35) … (306 | 157) Farbe 4.</item>
/// <item>[ORIGINAL] Zeile k = 0 … 5, y = 48 + 20 k: Heimverein (Flow, zentriert 0 … 159, Farbe 30), „-“ bei x = 156 (Farbe 7),
///   Gast (zentriert 160 … 319, Farbe 1), Vereine der Manager Farbe 24; darunter (Mini, Grundlinie y + 7, Farbe 8)
///   „n. Platz, Stärke m“ des Heimvereins und des Gasts; Ergebnis (Flow, Grundlinie y + 9, Farbe 7, zentriert) „g:g“ oder „--:--“.</item>
/// <item>[ORIGINAL] Startspieltag = gespielte + 1 (über das Ende hinaus: 1); links zurück (vor 1 → letzter), rechts vor (nach dem
///   letzten → 1). Rechtsklick und Esc kehren zurück.</item>
/// <item>[APPROX] Mittlerer Knopf: Die Seiten „Play-Off“ und „Aufstieg“ gibt es noch nicht – der Klick tut nichts.</item>
/// <item>[APPROX] Klick auf eine Zeile (Original: Vereinsinfo) tut nichts; die zuletzt gewählte Seite (DGROUP 0x5125) wird nicht gemerkt.</item>
/// <item>[APPROX] Spielplan und Stärkewerte stammen aus dem Remake (<see cref="Schedule.DoubleRoundRobin"/>, <see cref="StrengthTable"/>).</item>
/// </list>
/// </remarks>
public sealed class LeaguePairingsScreen : Screen
{
    public static readonly Rect Window = new(4, 15, 312, 152);
    public static readonly Rect[] Buttons = [new(30, 180, 48, 38), new(140, 180, 48, 38), new(250, 180, 48, 38)];
    public const int Rows = 6;
    public const int FirstBaseline = 48;
    public const int RowPitch = 20;
    public const string NoResult = "--:--";

    private static readonly Rect LeftArrow = new(19, 104, 29, 9);
    private static readonly Rect RightArrow = new(19, 125, 29, 9);

    private readonly Screen _back;

    public League League { get; }

    /// <summary>Angezeigter Spieltag (1-basiert).</summary>
    public int Day { get; private set; }

    public LeaguePairingsScreen(ScreenHost host, Screen back, League league) : base(host)
    {
        _back = back;
        League = league;
        var played = host.Game?.PlayedMatchDays ?? 0;
        Day = played + 1 > Count ? 1 : played + 1;
    }

    private int Count => Host.Game is { } g && g.Schedules.TryGetValue(League, out var days) ? days.Count : 0;

    public string MiddleLabel => League == League.Bundesliga ? "Play-Off" : "Aufstieg";

    public static string LeagueName(League league) => league switch
    {
        League.Bundesliga => "Bundesliga",
        League.ZweiteLiga => "Zweite Liga",
        _ => "Oberliga",
    };

    /// <summary>Ob der angezeigte Tag schon gespielt ist.</summary>
    public bool IsPlayed => Host.Game is { } g && Day <= g.PlayedMatchDays;

    /// <summary>Kopfzeile: „Bundesliga   x.Spieltag  d.m.“.</summary>
    public string Header(bool previousWasPlayed)
    {
        var date = Host.Game is { } g && Day <= g.MatchDayDates.Count ? SeasonCalendar.LayoutDate(g.MatchDayDates[Day - 1]) : default;
        return LeagueName(League) + "   " + CompetitionWidgets.Number(Day, previousWasPlayed ? 2 : 1)
            + ".Spieltag  " + date.Day + "." + date.Month + ".";
    }

    public IReadOnlyList<(int Home, int Away)> Pairings =>
        Host.Game is { } g && Day >= 1 && Day <= Count ? g.Schedules[League][Day - 1].Matches : [];

    public MatchResult? ResultOf(int home, int away) =>
        Host.Game?.Results.FirstOrDefault(r => r.League == League && r.MatchDay == Day && r.Home == home && r.Away == away);

    public override void Update(InputState input)
    {
        if (input.Pressed(Key.Escape) || input.RightClick)
        {
            Host.Show(_back);
            return;
        }
        if (Count == 0)
            return;
        if (input.Clicked(Buttons[0]))
            Day = Day <= 1 ? Count : Day - 1;
        else if (input.Clicked(Buttons[2]))
            Day = Day >= Count ? 1 : Day + 1;
    }

    public override void Draw(Surface s)
    {
        s.Palette = Assets.Palette;
        s.Blit(Assets.Image(MainMenuScreen.Background), 0, 0);
        Widgets.Frame(s, Assets, Window, FrameStyle.Orange);
        foreach (var button in Buttons)
            Widgets.Frame(s, Assets, button, FrameStyle.Orange);
        var arrows = Assets.Image(4);
        s.Blit(arrows, LeftArrow, 40, 195);
        s.Blit(arrows, RightArrow, 260, 195);
        CompetitionWidgets.Centered(s, Assets.Mini, 147, 182, 200, MiddleLabel, 1, null);

        s.HLine(13, 34, 303 - 13 + 1, 7);
        s.HLine(14, 35, 304 - 14 + 1, 1);
        s.Fill(new Rect(8, 23, 310 - 8 + 1, 33 - 23 + 1), 4);
        CompetitionWidgets.Centered(s, Assets.Flow, 5, 313, 30, Header(IsPlayed), 10, 28);
        s.Fill(new Rect(12, 35, 306 - 12 + 1, 157 - 35 + 1), 4);

        if (Host.Game is not { } game)
            return;
        var managers = game.Managers.Select(m => m.ClubIndex).ToHashSet();
        byte Color(int club, byte normal) => managers.Contains(club) ? Colors.Yellow : normal;
        var played = Math.Max(game.PlayedMatchDays, 0);
        var table = LeagueTable.Compute(game, League, TableView.Gesamt, played);
        var strength = StrengthTable.Compute(game, game.LeagueClubs(League), StrengthView.Gesamt);
        string Info(int club)
        {
            var rank = table.FirstOrDefault(r => r.Club.Index == club)?.Rank ?? 0;
            var average = strength.FirstOrDefault(r => r.Club.Index == club)?.Average ?? 0;
            return $"{CompetitionWidgets.Number(rank, 2)}. Platz, Stärke {CompetitionWidgets.Number(average, 2)}";
        }

        var pairs = Pairings;
        for (var k = 0; k < Math.Min(Rows, pairs.Count); k++)
        {
            var y = FirstBaseline + RowPitch * k;
            var (home, away) = pairs[k];
            CompetitionWidgets.Centered(s, Assets.Flow, 0, 159, y, game.Clubs[home].Name, Color(home, Colors.TileBackground), null);
            CompetitionWidgets.Left(s, Assets.Flow, 156, y, "-", 7);
            CompetitionWidgets.Centered(s, Assets.Flow, 160, 319, y, game.Clubs[away].Name, Color(away, 1), null);
            CompetitionWidgets.Centered(s, Assets.Mini, 0, 159, y + 7, Info(home), 8, null);
            CompetitionWidgets.Centered(s, Assets.Mini, 160, 319, y + 7, Info(away), 8, null);
            var text = IsPlayed && ResultOf(home, away) is { } m
                ? CompetitionWidgets.Number(m.HomeGoals, 2) + ":" + CompetitionWidgets.Number(m.AwayGoals, 2)
                : NoResult;
            CompetitionWidgets.Centered(s, Assets.Flow, 0, 319, y + 9, text, 7, null);
        }
    }
}

/// <summary>Anmeldung im Hauptmenü: Paarungen → 1. Liga, 2. Liga, Oberliga.</summary>
internal static class LeaguePairingsMenuEntries
{
    [MenuEntry("Paarungen 1. Liga")]
    private static Screen First(ScreenHost host, Screen back) => new LeaguePairingsScreen(host, back, League.Bundesliga);

    [MenuEntry("Paarungen 2. Liga")]
    private static Screen Second(ScreenHost host, Screen back) => new LeaguePairingsScreen(host, back, League.ZweiteLiga);

    [MenuEntry("Paarungen Oberliga")]
    private static Screen Third(ScreenHost host, Screen back) => new LeaguePairingsScreen(host, back, League.Oberliga);
}
