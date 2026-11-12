using EishockeyManager.Core;

namespace EishockeyManager.UI.Screens.Competitions;

/// <summary>Hin- oder Rückspiel einer Paarung: Termin (Tag, Monat) oder Ergebnis aus Sicht der Paarung (links, rechts).</summary>
public readonly record struct PromotionLeg(int First, int Second, bool IsDate);

/// <summary>Eine Paarung der Aufstiegsrunde mit Hin- und Rückspiel (null = nichts anzeigen).</summary>
public sealed record PromotionPairing(int Home, int Away, PromotionLeg? First, PromotionLeg? Second);

/// <summary>Inhalt der Seite „Aufstieg“: sechs Paarungen (je Runde zwei) und die Tabelle. Nur gelesen, ändert nichts.</summary>
public sealed record PromotionRoundPage(IReadOnlyList<PromotionPairing> Pairings, IReadOnlyList<TableRow> Table)
{
    /// <summary>Seite aus der Gruppe des Spielstands (Regeln im Kommentar von <see cref="PromotionRoundScreen"/>).</summary>
    public static PromotionRoundPage Of(GameState game, RelegationGroup group)
    {
        var pairings = new List<PromotionPairing>();
        for (var round = 0; round < 3; round++)
        {
            var pairs = group.Rounds[round].Matches;
            for (var p = 0; p < pairs.Count; p++)
            {
                var (home, away) = pairs[p];
                PromotionLeg? Leg(int leg)
                {
                    var index = round + 3 * leg;
                    if (index >= group.PlayedRounds)
                    {
                        if (p != 0)
                            return null;
                        var date = SeasonCalendar.LayoutDate(PromotionRoundScreen.DayOf(index));
                        return new PromotionLeg(date.Day, date.Month, true);
                    }
                    var (h, a) = leg == 0 ? (home, away) : (away, home);
                    if (group.Results.FirstOrDefault(m => m.MatchDay == index + 1 && m.Home == h && m.Away == a) is not { } match)
                        return null;
                    return leg == 0 ? new PromotionLeg(match.HomeGoals, match.AwayGoals, false) : new PromotionLeg(match.AwayGoals, match.HomeGoals, false);
                }
                pairings.Add(new PromotionPairing(home, away, Leg(0), Leg(1)));
            }
        }
        return new PromotionRoundPage(pairings, group.Table(game.Clubs));
    }
}

/// <summary>
/// „Aufstieg  1.Liga“ / „Aufstieg  2.Liga“ (Paarungen 2. Liga bzw. Oberliga → „Aufstieg“): die Aufstiegsrunde mit drei
/// Spielrunden (Hin- und Rückspiel, Termin bzw. Ergebnis) und der Tabelle der Gruppe. Zeigt nur vorhandenen Zustand.
/// </summary>
/// <remarks>
/// [ORIGINAL] Gruppen-Bildschirm Datei 0x31CD5 (Stub 0xE8:0x6B, wie <see cref="EuropeanCupScreen"/>) mit Wettbewerb 1 und Gruppe
/// Liga − 1, aufgerufen aus Seite 1 des Seitenwählers (Datei 0x3AB05–0x3AB40, <see cref="LeaguePairingsPages"/>). Vermessen an
/// <c>folge06_01257</c> (mit Ergebnissen) und <c>folge07_03307</c> (vor dem ersten Spiel; Ablauf <c>pixel-aufstieg</c>):
/// <list type="bullet">
/// <item>[ORIGINAL] Fenster, Paarungen, Ergebnisse, Tabelle und Knopfrahmen wie beim Europapokal (<see cref="EuropeanCupScreen"/>).</item>
/// <item>[ORIGINAL] Titel DGROUP 0x705 + 4 · 1 „Aufstieg“ + „  “ (DGROUP 0x4E4A) + Gruppe + 1 (Mindestbreite 2, DGROUP 0x374 = 2)
///   + „.“ + „Liga“ (DGROUP 0xC70; Datei 0x31E98–0x31EDA): von der 2. Liga „Aufstieg  1.Liga“, von der Oberliga „Aufstieg  2.Liga“.</item>
/// <item>[ORIGINAL] Pokalbild kb\36 Quelle (0 | 0) ([bp−0x20] = 0 für Wettbewerb ≤ 1, Datei 0x31DC2–0x31DD4).</item>
/// <item>[ORIGINAL] Beide Knöpfe „Liga“ (DGROUP 0x658, Datei 0x31EDD–0x31F01) führen zu den Ligapaarungen (Rückgabe DGROUP
///   0x4DF4/0x4DF6 = 7).</item>
/// <item>[ORIGINAL] Termin statt Ergebnis für Spiel g = Runde + 3 · Rückspiel, solange g ≥ gespielte Termine (Argument 3 des
///   Aufrufs: 2 · DGROUP 0x7B0 + (0xCCA3 &gt; 0) + (0xCCA3 &gt; 2), Datei 0x3AAA4–0x3AAD5; Vergleich Datei 0x3229F–0x322C6), nur
///   in der ersten Zeile der Runde; sonst das Ergebnis (Datei 0x324C7–0x32515).</item>
/// <item>[ORIGINAL] Ohne Aufstiegsrunde (DGROUP 0xCFFA = 0xFF) „Die Paarungen stehen noch nicht fest...“ (Flow, Grundlinie 100,
///   Farbe 30, Datei 0x3206C–0x320CE), dann nur Rahmen, Titel und Knöpfe.</item>
/// <item>[ORIGINAL] Bedienung (Datei 0x32944–0x32A62): Knopf → Ligapaarungen; Klick auf einen Verein der Paarungen
///   (y − 5 &lt; Maus-y &lt; y + 2, Gast ab x = 121) oder der Tabelle (y − 8 &lt; Maus-y &lt; y + 2) → Vereinsfenster
///   0x128:0x2A; Rechtsklick oder Taste → Hauptmenü; andere Linksklicks bewirken nichts.</item>
/// <item>[ORIGINAL] Termine: <see cref="RelegationGroup.DateIndices"/>. Das Original zählt die Gruppentage als Play-Off-Termine
///   mit Rest 0 oder 2 je Fünferblock (Datei 0x322F4–0x32324: 0, 2, 5, 7, 10, 12 → 6.2., 10.2., 20.2., 25.2., 22.3., 29.3. wie in
///   <c>folge07_03307</c>); der Spielkern folgt dem seit Spur wettbewerbe-rest (#790, A142).</item>
/// </list>
/// </remarks>
public sealed class PromotionRoundScreen : Screen
{
    public const string ButtonText = "Liga";
    public static readonly Rect TrophySource = new(0, 0, 22, 22);

    private readonly Screen _back;
    private readonly PromotionRoundPage? _fixed;
    private readonly bool _useFixed;
    private readonly IReadOnlyCollection<int>? _highlighted;

    /// <summary>Liga der Kachel (2. Liga oder Oberliga).</summary>
    public League League { get; }

    /// <summary>Gruppe 0 (Bundesliga/2. Liga) oder 1 (2. Liga/Oberliga).</summary>
    public int Group => (int)League - 1;

    /// <param name="league">2. Liga oder Oberliga; die Bundesliga hat keine Aufstiegsrunde (die Seite bliebe leer).</param>
    public PromotionRoundScreen(ScreenHost host, Screen back, League league) : base(host) => (_back, League) = (back, league);

    /// <summary>Feste Seite statt des Spielstands (Bildschirmfotos); ohne Paarungen „noch nicht fest“.</summary>
    public PromotionRoundScreen(ScreenHost host, Screen back, League league, IReadOnlyList<PromotionPairing> pairings, IReadOnlyList<TableRow> table,
        IReadOnlyCollection<int>? highlighted = null)
        : this(host, back, league) =>
        (_fixed, _useFixed, _highlighted) = (pairings.Count == 0 ? null : new PromotionRoundPage(pairings, table), true, highlighted);

    public string Title => "Aufstieg  " + CompetitionWidgets.Number(Group + 1, 2) + ".Liga";

    /// <summary>Gruppe der Seite oder null vor dem Ende der Hauptrunde.</summary>
    public RelegationGroup? RelegationGroup => Host.Game?.RelegationGroups.FirstOrDefault(g => g.Upper == League - 1);

    /// <summary>Inhalt der Seite oder null („Die Paarungen stehen noch nicht fest...“).</summary>
    public PromotionRoundPage? Page => _useFixed ? _fixed
        : RelegationGroup is { } group && Host.Game is { } game ? PromotionRoundPage.Of(game, group) : null;

    /// <summary>Vereinsfenster nach Klick auf einen Verein.</summary>
    public InfoBarWindows InfoWindows { get; } = new();

    /// <summary>Spieltag der Gruppe, an dem Spiel <paramref name="game"/> (Runde + 3 · Rückspiel) stattfindet.</summary>
    public static int DayOf(int game) => SeasonCalendar.DaysOf(DayEvents.Playoffs)[RelegationGroup.DateIndices[game]];

    /// <summary>Verein unter der Maus (Paarung oder Tabellenzeile), sonst null.</summary>
    public int? ClubAt(int x, int y)
    {
        if (Page is not { } page)
            return null;
        for (var i = 0; i < page.Pairings.Count; i++)
        {
            var line = EuropeanCupScreen.PairingBaseline + 26 * (i / 2) + 6 * (i % 2);
            if (line - 5 < y && line + 2 > y)
                return x > 120 ? page.Pairings[i].Away : page.Pairings[i].Home;
        }
        foreach (var row in page.Table)
        {
            var line = EuropeanCupScreen.TableBaseline + 10 * (row.Rank - 1);
            if (line - 8 < y && line + 2 > y)
                return row.Club.Index;
        }
        return null;
    }

    public override void Update(InputState input)
    {
        if (InfoWindows.Nation.Update(input) | InfoWindows.Club.Update(input))
            return;
        if (input.RightClick || input.Keys.Count > 0)
            Host.Show(_back);
        else if (input.Clicked(EuropeanCupScreen.LeftButton) || input.Clicked(EuropeanCupScreen.RightButton))
            Host.Show(LeaguePairingsPages.Open(Host, _back, League, LeaguePairingsPages.Pairings));
        else if (input.LeftClick && ClubAt(input.MouseX, input.MouseY) is { } club)
            InfoWindows.Show(club);
    }

    public override void Draw(Surface s)
    {
        s.Palette = Assets.Palette;
        s.Blit(Assets.Image(MainMenuScreen.Background), 0, 0);
        Widgets.Frame(s, Assets, EuropeanCupScreen.Window, FrameStyle.Orange);
        var trophies = Assets.Image(EuropeanCupScreen.TrophyGraphic);
        s.Blit(trophies, TrophySource, 13, 7);
        s.Blit(trophies, TrophySource, 286, 7);
        var (leftButton, rightButton) = (EuropeanCupScreen.LeftButton, EuropeanCupScreen.RightButton);
        Widgets.Frame(s, Assets, leftButton, FrameStyle.Orange);
        Widgets.Frame(s, Assets, rightButton, FrameStyle.Orange);
        CompetitionWidgets.Centered(s, Assets.Bold, 0, 319, 23, Title, Colors.White, DebCupDrawScreen.TitleShadow);
        CompetitionWidgets.Centered(s, Assets.Mini, leftButton.X, leftButton.X + 49, 217, ButtonText, Colors.White, null);
        CompetitionWidgets.Centered(s, Assets.Mini, rightButton.X, rightButton.X + 49, 217, ButtonText, Colors.White, null);

        if (Host.Game is not { } game)
            return;
        if (Page is not { } page)
        {
            CompetitionWidgets.Centered(s, Assets.Flow, 0, 320, 100, EuropeanCupScreen.NotYetKnown, Colors.TileBackground, Colors.Black);
            return;
        }

        var managers = (_highlighted ?? game.Managers.Select(m => m.ClubIndex)).ToHashSet();
        byte Color(int club, byte normal) => managers.Contains(club) ? Colors.Yellow : normal;
        var mini = Assets.Mini;
        for (var i = 0; i < page.Pairings.Count; i++)
        {
            var y = EuropeanCupScreen.PairingBaseline + 26 * (i / 2) + 6 * (i % 2);
            var pairing = page.Pairings[i];
            CompetitionWidgets.Left(s, mini, 25, y, game.Clubs[pairing.Home].Name, Color(pairing.Home, Colors.TileBackground));
            CompetitionWidgets.Left(s, mini, 120, y, "-", Colors.White);
            CompetitionWidgets.Left(s, mini, 137, y, game.Clubs[pairing.Away].Name, Color(pairing.Away, 1));
            for (var leg = 0; leg < 2; leg++)
            {
                var color = (byte)(30 - 29 * leg);
                if ((leg == 0 ? pairing.First : pairing.Second) is not { } value)
                    continue;
                if (value.IsDate)
                    CompetitionWidgets.Left(s, mini, 230 + 46 * leg, y,
                        CompetitionWidgets.Number(value.First, 2) + "." + CompetitionWidgets.Number(value.Second, 1) + ".", color);
                else
                    CompetitionWidgets.Left(s, mini, 226 + 46 * leg, y,
                        CompetitionWidgets.Number(value.First, 2) + ":" + CompetitionWidgets.Number(value.Second, 2), color);
            }
        }

        var flow = Assets.Flow;
        foreach (var row in page.Table)
        {
            var y = EuropeanCupScreen.TableBaseline + 10 * (row.Rank - 1);
            var color = Color(row.Club.Index, Colors.TileBackground);
            CompetitionWidgets.Left(s, flow, 39, y, $"{row.Rank}.  ", Colors.White);
            CompetitionWidgets.Left(s, flow, 57, y, row.Club.Name, color);
            static string N(int v) => CompetitionWidgets.Number(v, 2);
            CompetitionWidgets.Left(s, flow, 206, y, $"{N(row.PointsPlus)}:{N(row.PointsMinus)}   {N(row.GoalsFor)}:{N(row.GoalsAgainst)}", color);
        }
        InfoWindows.Draw(s, Assets, game);
    }
}
