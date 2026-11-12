using EishockeyManager.Core;

namespace EishockeyManager.UI.Screens.Competitions;

/// <summary>
/// „Paarungen 1. Liga / 2. Liga / Oberliga“ (Hauptmenü → Paarungen): die sechs Begegnungen eines Spieltags mit Platz,
/// Stärke und Ergebnis. Zeigt nur vorhandenen Zustand; die Pfeilknöpfe blättern zwischen den Spieltagen.
/// </summary>
/// <remarks>
/// Aufbau aus em.exe (Bildschirmfunktion 0x128:0x39, Datei 0x4E8A3; Aufruf über den Seitenwähler 0xF8:0x3E, Seite 7).
/// Der Aufbau stammt aus der Disassemblierung und ist seit Runde 6 an Videobildern bestätigt (Spur pixel-wettbewerbe, Ablauf
/// <c>pixel-paarungen</c>; Kennzahlen in <c>docs/wettbewerbe.md</c>): [ORIGINAL] Fenster, Kopfzeile, Knöpfe und Zeilen wie unten
/// gegen <c>folge06_01256</c> (Oberliga), <c>folge17_02406</c> (Zweite Liga), <c>folge21_02149</c> (Bundesliga); die Zeilen auch gegen
/// das gleiche Fenster ohne Knöpfe im Spielablauf („60.MINUTE“, <c>duell_s2a_01138</c>, <c>eisbaeren_f14_01518</c>,
/// <c>eisbaeren_f40_01833</c>) – diese Anzeige selbst gibt es im Remake noch nicht.
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
/// <item>[ORIGINAL] Mittlerer Knopf (Knopf 3, Datei 0x4F059–0x4F06A) beendet die Seite mit Rückgabe 1: Seite „Play-Off Spiele“
///   (<see cref="PlayoffGamesScreen"/>) bzw. „Aufstieg“ (<see cref="PromotionRoundScreen"/>); die zuletzt gezeigte Seite merkt
///   DGROUP 0x5125 (<see cref="LeaguePairingsPages"/>).</item>
/// <item>[ORIGINAL] Klick ins Fenster unterhalb y = 34 (Datei 0x4F079–0x4F164): Begegnung k = (y − 40) / 20 (ganzzahlig, gegen 0
///   gerundet), rechts von x = 160 der Gast; gibt es sie, öffnet das Vereinsfenster 0x128:0x2A (<see cref="ClubInfoWindow"/>).
///   [APPROX] Die weiteren Argumente (Lage y &gt; 140, Wert 1) wertet das Fenster des Remakes nicht aus.</item>
/// <item>[ORIGINAL] Spielplan aus dem Spielkern nach der Satzfolge der Liga (<see cref="Schedule.ForSlots"/>, Datei 0x4E7C7, Tabelle
///   DGROUP 0x2DC; A113, Spur wettbewerbe-rest #790).</item>
/// <item>[APPROX] Die Stärkewerte stammen aus dem Remake (<see cref="StrengthTable"/>).</item>
/// </list>
/// </remarks>
/// <summary>Eine Begegnung der Seite: Vereine, Platz und Stärke beider, Ergebnis (null = nicht gespielt).</summary>
public sealed record PairingRow(int Home, int Away, int HomeRank, int HomeStrength, int AwayRank, int AwayStrength, (int Home, int Away)? Result);

/// <summary>Inhalt einer Seite „Paarungen“: Kopfzeile und bis zu sechs Begegnungen.</summary>
public sealed record PairingsPage(string Header, IReadOnlyList<PairingRow> Rows);

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
    private readonly PairingsPage? _fixed;
    private readonly IReadOnlyCollection<int>? _highlighted;

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

    /// <summary>Feste Seite statt des Spielstands, z. B. für Bildschirmfotos wie im Video; <paramref name="highlighted"/> sind die
    /// gelb gezeigten Vereine (sonst die Vereine der Manager).</summary>
    public LeaguePairingsScreen(ScreenHost host, Screen back, League league, PairingsPage page, IReadOnlyCollection<int>? highlighted = null)
        : this(host, back, league) => (_fixed, _highlighted) = (page, highlighted);

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
        if (InfoWindows.Nation.Update(input) | InfoWindows.Club.Update(input))
            return;
        if (input.Pressed(Key.Escape) || input.RightClick)
        {
            Host.Show(_back);
            return;
        }
        if (input.Clicked(Buttons[1]))
        {
            Host.Show(LeaguePairingsPages.Open(Host, _back, League, LeaguePairingsPages.Final));
            return;
        }
        if (input.LeftClick && ClubAt(input.MouseX, input.MouseY) is { } club)
        {
            InfoWindows.Show(club);
            return;
        }
        if (Count == 0)
            return;
        if (input.Clicked(Buttons[0]))
            Day = Day <= 1 ? Count : Day - 1;
        else if (input.Clicked(Buttons[2]))
            Day = Day >= Count ? 1 : Day + 1;
    }

    /// <summary>Vereinsfenster nach Klick auf eine Begegnung.</summary>
    public InfoBarWindows InfoWindows { get; } = new();

    /// <summary>
    /// [ORIGINAL] Verein unter der Maus (Datei 0x4F079–0x4F132): nur im Fenster und unterhalb y = 34; Begegnung (y − 40) / 20,
    /// Platz 2k + (x &gt; 160) der Paarungstabelle DGROUP 0x5D7B, gültig unterhalb der Vereinszahl der Liga (DGROUP 0x7B1).
    /// </summary>
    public int? ClubAt(int x, int y)
    {
        if (!Window.Contains(x, y) || y <= 34)
            return null;
        var k = (y - 40) / 20;
        var rows = Page?.Rows;
        if (rows is null || k < 0 || k >= rows.Count)
            return null;
        return x > 160 ? rows[k].Away : rows[k].Home;
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
        CompetitionWidgets.Centered(s, Assets.Flow, 5, 313, 30, _fixed?.Header ?? Header(IsPlayed), 10, 28);
        s.Fill(new Rect(12, 35, 306 - 12 + 1, 157 - 35 + 1), 4);

        if (Host.Game is not { } game || Page is not { } page)
            return;
        var managers = (_highlighted ?? game.Managers.Select(m => m.ClubIndex)).ToHashSet();
        byte Color(int club, byte normal) => managers.Contains(club) ? Colors.Yellow : normal;
        static string Info(int rank, int strength) =>
            $"{CompetitionWidgets.Number(rank, 2)}. Platz, Stärke {CompetitionWidgets.Number(strength, 2)}";

        for (var k = 0; k < Math.Min(Rows, page.Rows.Count); k++)
        {
            var y = FirstBaseline + RowPitch * k;
            var row = page.Rows[k];
            CompetitionWidgets.Centered(s, Assets.Flow, 0, 159, y, game.Clubs[row.Home].Name, Color(row.Home, Colors.TileBackground), null);
            CompetitionWidgets.Left(s, Assets.Flow, 156, y, "-", 7);
            CompetitionWidgets.Centered(s, Assets.Flow, 160, 319, y, game.Clubs[row.Away].Name, Color(row.Away, 1), null);
            CompetitionWidgets.Centered(s, Assets.Mini, 0, 159, y + 7, Info(row.HomeRank, row.HomeStrength), 8, null);
            CompetitionWidgets.Centered(s, Assets.Mini, 160, 319, y + 7, Info(row.AwayRank, row.AwayStrength), 8, null);
            var text = row.Result is { } r
                ? CompetitionWidgets.Number(r.Home, 2) + ":" + CompetitionWidgets.Number(r.Away, 2)
                : NoResult;
            CompetitionWidgets.Centered(s, Assets.Flow, 0, 319, y + 9, text, 7, null);
        }
        InfoWindows.Draw(s, Assets, game);
    }

    /// <summary>Inhalt der angezeigten Seite aus dem Spielstand (oder die feste Seite); ändert nichts.</summary>
    public PairingsPage? Page
    {
        get
        {
            if (_fixed is not null)
                return _fixed;
            if (Host.Game is not { } game)
                return null;
            var played = Math.Max(game.PlayedMatchDays, 0);
            var table = LeagueTable.Compute(game, League, TableView.Gesamt, played);
            var strength = StrengthTable.Compute(game, game.LeagueClubs(League), StrengthView.Gesamt);
            int Rank(int club) => table.FirstOrDefault(r => r.Club.Index == club)?.Rank ?? 0;
            int Strength(int club) => strength.FirstOrDefault(r => r.Club.Index == club)?.Average ?? 0;
            var rows = Pairings.Select(p => new PairingRow(p.Home, p.Away, Rank(p.Home), Strength(p.Home), Rank(p.Away), Strength(p.Away),
                IsPlayed && ResultOf(p.Home, p.Away) is { } m ? (m.HomeGoals, m.AwayGoals) : null)).ToList();
            return new PairingsPage(Header(IsPlayed), rows);
        }
    }
}

/// <summary>Anmeldung im Hauptmenü: Paarungen → 1. Liga, 2. Liga, Oberliga.</summary>
internal static class LeaguePairingsMenuEntries
{
    [MenuEntry("Paarungen 1. Liga")]
    private static Screen First(ScreenHost host, Screen back) => LeaguePairingsPages.FromTile(host, back, League.Bundesliga);

    [MenuEntry("Paarungen 2. Liga")]
    private static Screen Second(ScreenHost host, Screen back) => LeaguePairingsPages.FromTile(host, back, League.ZweiteLiga);

    [MenuEntry("Paarungen Oberliga")]
    private static Screen Third(ScreenHost host, Screen back) => LeaguePairingsPages.FromTile(host, back, League.Oberliga);
}
