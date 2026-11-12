using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Competitions;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur ui-rest, Teil 1 (A280/A282): Seiten „Play-Off Spiele“ und „Aufstieg“ der Ligapaarungen (Seitenwähler 0x3A88A, Seite 1),
/// Klick auf eine Zeile → Vereinsfenster, gemerkte Seite DGROUP 0x5125. Pixelvergleich mit Fundstellen im PR
/// (Abläufe <c>pixel-playoff</c>, <c>pixel-aufstieg</c>).
/// </summary>
public class LigapaarungenSeitenTests
{
    private static GameState Synthetic()
    {
        var data = new byte[Formats.ClubDatabase.FileLength];
        for (var i = 0; i < Formats.ClubDatabase.ClubsWithSquad; i++)
        {
            System.Text.Encoding.ASCII.GetBytes($"VEREIN {i}").CopyTo(data, Formats.ClubDatabase.SquadOffset + i * Formats.ClubDatabase.SquadRecordLength);
            data[Formats.ClubDatabase.StrengthOffset + i * 3] = (byte)(100 - i % 12 * 5);
        }
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "A", Head = 0, ClubIndex = 2 });
        return NewGame.Create(Formats.ClubDatabase.Parse(data), setup, 3);
    }

    private static InputState Click(int x, int y) => new() { MouseX = x, MouseY = y, LeftClick = true };

    private static InputState Click(Rect r) => Click(r.X + 5, r.Y + 5);

    private static (ScreenHost Host, Screen Menu) Host(GameState game)
    {
        var host = new ScreenHost(null!) { Game = game };
        var menu = new MainMenuScreen(host);
        host.Show(menu);
        return (host, menu);
    }

    /// <summary>Zeitleiste bis zum Ende der Hauptrunde (wie TestSeason.PlayRegularSeason der Core-Tests).</summary>
    private static void PlayRegularSeason(GameState game)
    {
        while (!game.RegularSeasonOver && game.Phase == SeasonPhase.Running)
            game.Continue();
    }

    /// <summary>Zeitleiste, bis <paramref name="count"/> Play-Off-Termine vorbei sind.</summary>
    private static void PassPlayoffDates(GameState game, int count)
    {
        var day = SeasonCalendar.DaysOf(DayEvents.Playoffs)[count - 1];
        while (game.CurrentDay <= day && game.Phase == SeasonPhase.Running)
            game.Continue();
    }

    /// <summary>Regression A280: Der mittlere Knopf tat nichts; jetzt Seite 1 je Liga, „Liga“ führt zurück (Datei 0x4F059, 0x3AA21).</summary>
    [Fact]
    public void MiddleButton_OpensPlayoffOrPromotionPage_AndLigaReturns()
    {
        var game = Synthetic();
        var (host, menu) = Host(game);
        var before = StateFingerprint.Of(game);

        foreach (var league in new[] { League.Bundesliga, League.ZweiteLiga, League.Oberliga })
        {
            host.Show(LeaguePairingsPages.FromTile(host, menu, league));
            var pairings = Assert.IsType<LeaguePairingsScreen>(host.Current);
            pairings.Update(Click(LeaguePairingsScreen.Buttons[1]));
            if (league == League.Bundesliga)
            {
                var playoff = Assert.IsType<PlayoffGamesScreen>(host.Current);
                Assert.Null(playoff.Overview); // vor den Play-Offs: „Die Paarungen stehen noch nicht fest...“
                playoff.Update(Click(PlayoffGamesScreen.Button));
            }
            else
            {
                var promotion = Assert.IsType<PromotionRoundScreen>(host.Current);
                Assert.Equal(league, promotion.League);
                Assert.Equal(league == League.ZweiteLiga ? "Aufstieg   1.Liga" : "Aufstieg   2.Liga", promotion.Title);
                Assert.Null(promotion.Page);
                promotion.Update(Click(league == League.ZweiteLiga ? EuropeanCupScreen.LeftButton : EuropeanCupScreen.RightButton));
            }
            Assert.Equal(league, Assert.IsType<LeaguePairingsScreen>(host.Current).League);
            host.Current.Update(new InputState { RightClick = true });
            Assert.Same(menu, host.Current);
        }
        Assert.Equal(before, StateFingerprint.Of(game));
    }

    /// <summary>
    /// [ORIGINAL] DGROUP 0x5125 gilt für alle drei Kacheln (Datei 0x4587E–0x45891): wer Seite 1 mit Rechtsklick verlässt, kommt
    /// bei jeder Ligakachel wieder auf Seite 1 (der eigenen Liga); zurück über „Liga“ gilt wieder Seite 7.
    /// </summary>
    [Fact]
    public void LastPage_IsRemembered_ForAllThreeTiles()
    {
        var game = Synthetic();
        var (host, menu) = Host(game);
        Assert.Equal(LeaguePairingsPages.Pairings, LeaguePairingsPages.Current(game));

        host.Show(LeaguePairingsPages.FromTile(host, menu, League.Bundesliga));
        host.Current!.Update(Click(LeaguePairingsScreen.Buttons[1]));
        host.Current!.Update(new InputState { RightClick = true });
        Assert.Same(menu, host.Current);
        Assert.Equal(LeaguePairingsPages.Final, LeaguePairingsPages.Current(game));

        host.Show(LeaguePairingsPages.FromTile(host, menu, League.Oberliga));
        Assert.Equal(League.Oberliga, Assert.IsType<PromotionRoundScreen>(host.Current).League);
        host.Current!.Update(new InputState { Keys = { Key.Escape } });
        host.Show(LeaguePairingsPages.FromTile(host, menu, League.Bundesliga));
        Assert.IsType<PlayoffGamesScreen>(host.Current);
        host.Current!.Update(Click(PlayoffGamesScreen.Button));
        Assert.Equal(LeaguePairingsPages.Pairings, LeaguePairingsPages.Current(game));
        host.Current!.Update(new InputState { RightClick = true });
        host.Show(LeaguePairingsPages.FromTile(host, menu, League.ZweiteLiga));
        Assert.Equal(League.ZweiteLiga, Assert.IsType<LeaguePairingsScreen>(host.Current).League);
    }

    /// <summary>[ORIGINAL] Klick auf eine Begegnung (Datei 0x4F079–0x4F132): k = (y − 40) / 20, Gast rechts von x = 160.</summary>
    [Fact]
    public void PairingsRowClick_OpensClubWindow_OfThatClub()
    {
        var game = Synthetic();
        var (host, menu) = Host(game);
        var screen = new LeaguePairingsScreen(host, menu, League.ZweiteLiga);
        host.Show(screen);
        var pairs = screen.Pairings;
        var before = StateFingerprint.Of(game);

        Assert.Null(screen.ClubAt(50, 34));
        Assert.Equal(pairs[0].Home, screen.ClubAt(50, 35));
        Assert.Equal(pairs[0].Home, screen.ClubAt(160, 59));
        Assert.Equal(pairs[0].Away, screen.ClubAt(161, 59));
        Assert.Equal(pairs[1].Home, screen.ClubAt(50, 60));
        Assert.Equal(pairs[5].Away, screen.ClubAt(300, 159));
        Assert.Null(screen.ClubAt(50, 160)); // k = 6: keine Begegnung
        Assert.Null(screen.ClubAt(2, 60)); // außerhalb des Fensters

        screen.Update(Click(200, 70));
        Assert.Same(screen, host.Current);
        Assert.Equal(pairs[1].Away, screen.InfoWindows.Club.Club);
        screen.Update(new InputState { RightClick = true }); // schließt zuerst das Fenster
        Assert.False(screen.InfoWindows.Visible);
        Assert.Same(screen, host.Current);
        Assert.Equal(before, StateFingerprint.Of(game));
    }

    private static KnockoutMatch Game(PlayoffSeries series, int homeGoals, int awayGoals)
    {
        var (home, away) = series.NextPairing;
        var match = new KnockoutMatch(0, home, away, homeGoals, awayGoals);
        series.Games.Add(match);
        return match;
    }

    /// <summary>
    /// [ORIGINAL] Ausrichtung der Serien (0x31505, Datei 0x3168B–0x316C9, 0x31C24–0x31C46): laufende Runde nach ungerader Terminzahl
    /// getauscht (auch entschiedene Serien), Ergebnisse folgen den Namen; ab dem fünften Termin die nächste Runde
    /// (Tabelle DGROUP 0x144) und die abgeschlossene Runde mit dem ersten Verein links.
    /// </summary>
    [Fact]
    public void PlayoffOverview_FollowsSlotSwaps_AndShowsNextRoundAfterFifthDate()
    {
        var days = SeasonCalendar.DaysOf(DayEvents.Playoffs);
        var playoffs = new Playoffs([10, 11, 12, 13, 14, 15, 16, 17]);
        var quarter = playoffs.Rounds[0];

        var start = PlayoffOverview.Of(playoffs, days[0]);
        var rows = Assert.Single(start.Sections).Rows;
        Assert.Equal("VIERTELFINALE", start.Sections[0].Title);
        Assert.Equal([(10, 17), (11, 16), (12, 15), (13, 14)], rows.Select(r => (r.Left, r.Right)));
        Assert.Equal(days[0], start.NextDay);
        Assert.Equal(1, start.Headings);

        // Termin 1: Heimsieg 4:1 → getauscht, aus Sicht des Gasts 1:4
        foreach (var series in quarter)
            Game(series, 4, 1);
        var one = PlayoffOverview.Of(playoffs, days[0] + 1);
        Assert.Equal([(17, 10), (16, 11), (15, 12), (14, 13)], one.Sections[0].Rows.Select(r => (r.Left, r.Right)));
        Assert.All(one.Sections[0].Rows, r => Assert.Equal(new DebCupResult(1, 4, 0), Assert.Single(r.Results)));
        Assert.Equal(days[1], one.NextDay);

        // Termine 2 und 3: Serie 0 mit 3:0 entschieden, die übrigen weiter
        for (var date = 1; date < 3; date++)
            foreach (var series in quarter)
            {
                var homeClubWins = series == quarter[0];
                var homeClubAtHome = series.NextPairing.Home == series.HomeClub;
                Game(series, homeClubWins == homeClubAtHome ? 5 : 0, homeClubWins == homeClubAtHome ? 0 : 5);
            }
        Assert.True(quarter[0].IsDecided);
        Assert.False(quarter[1].IsDecided);
        var three = PlayoffOverview.Of(playoffs, days[2] + 1);
        Assert.Equal((17, 10), (three.Sections[0].Rows[0].Left, three.Sections[0].Rows[0].Right));
        Assert.Equal([new(1, 4, 0), new(0, 5, 0), new(0, 5, 0)], three.Sections[0].Rows[0].Results);

        // Termin 4 ohne Spiel für Serie 0: trotzdem getauscht
        var four = PlayoffOverview.Of(playoffs, days[3] + 1);
        Assert.Equal((10, 17), (four.Sections[0].Rows[0].Left, four.Sections[0].Rows[0].Right));
        Assert.Single(four.Sections);

        // Nach dem fünften Termin: Halbfinale Sieger 1–8 gegen Sieger 3–6, Viertelfinale wieder mit dem ersten Verein links
        foreach (var series in quarter.Skip(1))
            while (!series.IsDecided)
                Game(series, 0, 3); // Gast des Spiels gewinnt: 2:2, dann Spiel 5 für den zweiten Verein
        var five = PlayoffOverview.Of(playoffs, days[4] + 1);
        Assert.Equal(2, five.Sections.Count);
        Assert.Equal(2, five.Headings);
        Assert.Equal([(10, 17), (11, 16), (12, 15), (13, 14)], five.Sections[0].Rows.Select(r => (r.Left, r.Right)));
        var winners = quarter.Select(s => s.Winner!.Value).ToList();
        Assert.Equal([10, 16, 15, 14], winners);
        Assert.Equal([(winners[0], winners[2]), (winners[1], winners[3])], five.Sections[1].Rows.Select(r => (r.Left, r.Right)));
        Assert.Equal("HALBFINALE", five.Sections[1].Title);
        Assert.All(five.Sections[1].Rows, r => Assert.Empty(r.Results));
        Assert.Single(PlayoffOverview.Of(playoffs, days[4]).Sections); // am fünften Termin selbst noch nicht
    }

    /// <summary>Ganze Saison: Seite nach den Play-Offs mit drei Runden, Endspiel mit dem Meister links; Lesen ändert nichts.</summary>
    [Fact]
    public void PlayoffOverview_AfterPlayoffs_ShowsAllRounds_ChampionLeft()
    {
        var game = Synthetic();
        PlayRegularSeason(game);
        var playoffs = Assert.IsType<Playoffs>(game.Playoffs);
        var start = PlayoffOverview.Of(game)!;
        Assert.Equal(playoffs.Rounds[0].Select(s => (s.HomeClub, s.AwayClub)), start.Sections[0].Rows.Select(r => (r.Left, r.Right)));

        var last = SeasonCalendar.DaysOf(DayEvents.Playoffs)[^1];
        while (game.CurrentDay <= last && game.Phase == SeasonPhase.Running)
            game.Continue();
        var before = StateFingerprint.Of(game);
        var end = PlayoffOverview.Of(game)!;
        Assert.Equal(before, StateFingerprint.Of(game));
        Assert.Equal(PlayoffOverview.SectionTitles, end.Sections.Select(s => s.Title));
        Assert.Equal(3, end.Headings);
        Assert.Equal(playoffs.Champion, end.Sections[2].Rows[0].Left);
        Assert.Equal(playoffs.Rounds[1].Select(s => (s.HomeClub, s.AwayClub)), end.Sections[1].Rows.Select(r => (r.Left, r.Right)));
        foreach (var (row, series) in end.Sections[0].Rows.Zip(playoffs.Rounds[0]))
            Assert.Equal(series.Games.Select(m => PlayoffOverview.ResultOf(m, series.HomeClub)), row.Results);
    }

    /// <summary>Aufstiegsrunde: vor dem ersten Spiel Termine in der ersten Zeile jeder Runde, danach Ergebnisse links:rechts.</summary>
    [Fact]
    public void PromotionPage_ShowsDatesBeforeGames_AndResultsAfter()
    {
        var game = Synthetic();
        PlayRegularSeason(game);
        var group = game.RelegationGroups.Single(g => g.Upper == League.ZweiteLiga);
        var page = PromotionRoundPage.Of(game, group);
        Assert.Equal(6, page.Pairings.Count);
        for (var i = 0; i < 6; i++)
        {
            Assert.Equal(group.Rounds[i / 2].Matches[i % 2], (page.Pairings[i].Home, page.Pairings[i].Away));
            PromotionLeg? Date(int index) => SeasonCalendar.LayoutDate(PromotionRoundScreen.DayOf(index)) is var d
                ? new PromotionLeg(d.Day, d.Month, true) : null;
            Assert.Equal(i % 2 == 0 ? Date(i / 2) : null, page.Pairings[i].First);
            Assert.Equal(i % 2 == 0 ? Date(i / 2 + 3) : null, page.Pairings[i].Second);
        }

        var (host, menu) = Host(game);
        var screen = new PromotionRoundScreen(host, menu, League.Oberliga);
        host.Show(screen);
        Assert.Equal(page.Pairings[1].Away, screen.ClubAt(140, EuropeanCupScreen.PairingBaseline + 6));
        Assert.Equal(page.Pairings[1].Home, screen.ClubAt(120, EuropeanCupScreen.PairingBaseline + 7));
        Assert.Null(screen.ClubAt(120, EuropeanCupScreen.PairingBaseline + 8));
        Assert.Equal(page.Table[2].Club.Index, screen.ClubAt(60, EuropeanCupScreen.TableBaseline + 20 - 7));
        screen.Update(Click(60, EuropeanCupScreen.TableBaseline));
        Assert.Equal(page.Table[0].Club.Index, screen.InfoWindows.Club.Club);
        Assert.Same(screen, host.Current);

        var last = SeasonCalendar.DaysOf(DayEvents.Playoffs)[^1];
        while (game.CurrentDay <= last && game.Phase == SeasonPhase.Running)
            game.Continue();
        Assert.True(group.IsFinished);
        page = PromotionRoundPage.Of(game, group);
        for (var i = 0; i < 6; i++)
        {
            var (home, away) = (page.Pairings[i].Home, page.Pairings[i].Away);
            var first = group.Results.Single(r => r.MatchDay == i / 2 + 1 && r.Home == home && r.Away == away);
            var back = group.Results.Single(r => r.MatchDay == i / 2 + 4 && r.Home == away && r.Away == home);
            Assert.Equal(new PromotionLeg(first.HomeGoals, first.AwayGoals, false), page.Pairings[i].First);
            Assert.Equal(new PromotionLeg(back.AwayGoals, back.HomeGoals, false), page.Pairings[i].Second);
        }
    }

    /// <summary>[ORIGINAL] Klick auf einen Namen der Play-Off-Seite (Datei 0x33AAF–0x33B30): y − 6 &lt; Maus-y ≤ y, rechts ab x = 100.</summary>
    [Fact]
    public void PlayoffRowClick_OpensClubWindow()
    {
        var game = Synthetic();
        PlayRegularSeason(game);
        var (host, menu) = Host(game);
        var screen = new PlayoffGamesScreen(host, menu);
        host.Show(screen);
        var overview = screen.Overview!;
        var (_, top) = PlayoffGamesScreen.FrameFor(overview);
        var lines = PlayoffGamesScreen.RowLines(overview, top);
        Assert.Equal(top + 53, lines[0].Y);
        Assert.Equal(lines[0].Left, PlayoffGamesScreen.ClubAt(overview, 99, lines[0].Y));
        Assert.Equal(lines[0].Right, PlayoffGamesScreen.ClubAt(overview, 100, lines[0].Y - 5));
        Assert.Null(PlayoffGamesScreen.ClubAt(overview, 40, lines[0].Y + 1)); // y − 6 < Maus-y: Lücke von einer Zeile
        Assert.Equal(lines[1].Left, PlayoffGamesScreen.ClubAt(overview, 40, lines[0].Y + 2));
        Assert.Null(PlayoffGamesScreen.ClubAt(overview, 40, lines[3].Y + 1));

        screen.Update(Click(150, lines[2].Y));
        Assert.Equal(lines[2].Right, screen.InfoWindows.Club.Club);
        screen.Update(Click(5, 5)); // anderer Linksklick: Seite bleibt
        Assert.Same(screen, host.Current);
    }

    /// <summary>Alle drei Seiten zeichnen ohne Zustandsänderung, auch während der Play-Offs mit Original-Daten.</summary>
    [GameDataFact]
    public void Draw_AllPages_IsStateFree_DuringPlayoffs()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        PlayRegularSeason(game);
        PassPlayoffDates(game, 3);
        d.Host.Show(new MainMenuScreen(d.Host));
        var before = StateFingerprint.Of(game);
        foreach (var tile in new[] { "Paarungen 1. Liga", "Paarungen 2. Liga", "Paarungen Oberliga" })
        {
            d.OpenTile(tile);
            d.Frame();
            d.Click(LeaguePairingsScreen.Buttons[1].X + 5, LeaguePairingsScreen.Buttons[1].Y + 5);
            d.Frame();
            Assert.True(d.Host.Current is PlayoffGamesScreen or PromotionRoundScreen);
            d.RightClick(5, 5);
            Assert.IsType<MainMenuScreen>(d.Host.Current);
        }
        Assert.Equal(before, StateFingerprint.Of(game));
        Assert.Equal(LeaguePairingsPages.Final, LeaguePairingsPages.Current(game));
    }
}
