using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Competitions;
using EishockeyManager.UI.Screens.Live;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur live-seiten-rest (Rest S04, R26-E2 = C; <c>docs/analyse/live-spieltag.md</c> §11): Aufstiegsrunde (Seite 2) und Europapokal
/// (Seite 3/5) im Spielablauf – 0x31CD5 mit Merker „im Spiel“ [bp+0xA] und Terminzahl aus dem Seitenwähler (Datei 0x3AAA4–0x3ABCE),
/// Nachbildung <c>tools/reverse-engineering/live/aufstieg_europapokal.py</c>. Vor der Spur rot: die Seiten entfielen
/// (<see cref="LivePageScreens.Create"/> lieferte keinen Bildschirm), die zweite Aufstiegsgruppe fehlte in der Folge.
/// </summary>
public sealed class LiveSeitenRestTests
{
    private static GameState Synthetic(int seed = 3)
    {
        var data = new byte[Formats.ClubDatabase.FileLength];
        for (var i = 0; i < Formats.ClubDatabase.ClubsWithSquad; i++)
        {
            System.Text.Encoding.ASCII.GetBytes($"VEREIN {i}").CopyTo(data, Formats.ClubDatabase.SquadOffset + i * Formats.ClubDatabase.SquadRecordLength);
            data[Formats.ClubDatabase.StrengthOffset + i * 3] = (byte)(100 - i % 12 * 5);
        }
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "A", Head = 0, ClubIndex = 2 });
        return NewGame.Create(Formats.ClubDatabase.Parse(data), setup, seed);
    }

    private static (ScreenHost Host, Screen Menu) Host(GameState game)
    {
        var host = new ScreenHost(null!) { Game = game };
        var menu = new MainMenuScreen(host);
        host.Show(menu);
        return (host, menu);
    }

    /// <summary>Zeitleiste bis zum Tag <paramref name="day"/> (noch nicht gespielt).</summary>
    private static void PassTo(GameState game, int day)
    {
        for (var guard = 0; game.CurrentDay < day; guard++)
        {
            Assert.True(guard < 1000 && game.Phase == SeasonPhase.Running, "Tag nicht erreicht");
            game.Continue();
        }
        Assert.Equal(day, game.CurrentDay);
    }

    private static void StepTo(LiveMatchDay day, int minute)
    {
        for (var guard = 0; day.Minute < minute; guard++)
        {
            Assert.True(guard < 100, "Minute nicht erreicht");
            Assert.True(day.Step());
        }
    }

    /// <summary>
    /// [ORIGINAL] Grenze c = Terminzahl + Merker „Drittelpause“ aus dem Seitenwähler (aufstieg_europapokal.py Teil 1, Europapokal
    /// Gruppe 1 mit DGROUP 0x7AF = 2: Minute 0 → (2, 0), 20/40 → (1, 1), 60 → (2, 0), 65/70 → (1, 0)).
    /// </summary>
    [Theory]
    [InlineData(0, 2)]
    [InlineData(20, 2)]
    [InlineData(40, 2)]
    [InlineData(60, 2)]
    [InlineData(65, 1)]
    [InlineData(70, 1)]
    public void ShownGames_MatchesSelectorReplay(int minute, int expected) =>
        Assert.Equal(expected, EuropeanCupScreen.ShownGames(2, minute));

    /// <summary>
    /// [ORIGINAL] 0x31CD5 (aufstieg_europapokal.py Teil 2): Ergebnis für g &lt; c und im Spiel auch g = c; Termin nur ohne „im Spiel“,
    /// in der ersten Zeile und für g ≥ c.
    /// </summary>
    [Fact]
    public void ResultAndDateRule_MatchesGroupScreenReplay()
    {
        // Hauptmenü, zwei Termine gespielt: Ergebnisse g = 0, 1; Termine g = 2 … 5 in der ersten Zeile
        Assert.Equal([true, true, false, false, false, false], Enumerable.Range(0, 6).Select(g => EuropeanCupScreen.ShowsResult(g, 2, false)));
        Assert.Equal([false, false, true, true, true, true], Enumerable.Range(0, 6).Select(g => EuropeanCupScreen.ShowsDate(g, 2, false, true)));
        Assert.False(EuropeanCupScreen.ShowsDate(3, 2, false, false));
        // 3. Termin nach Minute 20 (c = 1 + 1) bzw. 65 (c = 1): im Spiel der laufende Termin mit bzw. ohne, nie ein Termin
        Assert.Equal([true, true, true, false, false, false], Enumerable.Range(0, 6).Select(g => EuropeanCupScreen.ShowsResult(g, 2, true)));
        Assert.Equal([true, true, false, false, false, false], Enumerable.Range(0, 6).Select(g => EuropeanCupScreen.ShowsResult(g, 1, true)));
        Assert.DoesNotContain(true, Enumerable.Range(0, 6).Select(g => EuropeanCupScreen.ShowsDate(g, 2, true, true)));
    }

    /// <summary>
    /// Aufstiegsrunde am ersten Gruppenspieltag (Play-off-Spiel 0) mit „Drittelpause 2.Liga“ (3) und „Drittelpause Oberliga“ (6):
    /// nach Minute 20 beide Gruppenseiten ([ORIGINAL] Datei 0x38699–0x386C5), Gruppe 0 mit dem laufenden Stand in der Spalte des
    /// Hinspiels, ohne Termine, Tabelle noch ohne den Spieltag; nach Minute 60 die Tabelle mit dem Spieltag (0x30852, Datei 0x38691).
    /// Vor der Spur rot: eine Seite (nur Gruppe 0), ohne Bildschirm.
    /// </summary>
    [Fact]
    public void PromotionDay_BothGroups_RunningScore_NoDates()
    {
        var game = Synthetic();
        game.Options.Reports[3] = true;
        game.Options.Reports[6] = true;
        game.Options.Reports[7] = true;
        var first = PromotionRoundScreen.DayOf(0);
        PassTo(game, first);
        var group = game.RelegationGroups.Single(g => g.Upper == League.Bundesliga); // Gruppe 0: „Aufstieg  1.Liga“
        Assert.Equal(0, group.PlayedRounds);
        var random = new Random(7);
        var day = LiveMatchDay.Begin(game, group.Rounds[0].Matches.Select(m =>
            new LiveFixture(game.Clubs[m.Home], game.Clubs[m.Away], MatchCompetition.Relegation, false, random)).ToList());
        StepTo(day, 20);

        var pages = LivePageScreens.For(game, day, 20);
        Assert.Equal([LivePageKind.PromotionRound, LivePageKind.PromotionRound], pages.Select(p => p.Kind));
        Assert.Equal([League.ZweiteLiga, League.Oberliga], pages.Select(LivePageScreens.PromotionLeague));

        var (host, menu) = Host(game);
        var screen = Assert.IsType<PromotionRoundScreen>(Assert.Single(LivePageScreens.Create(host, menu, day, pages[0], 20)));
        Assert.Equal(League.ZweiteLiga, screen.League);
        Assert.Equal(20, screen.LiveMinute);
        var page = screen.Page!;
        for (var i = 0; i < page.Pairings.Count; i++)
        {
            var pairing = page.Pairings[i];
            if (i < 2)
            {
                var report = day.Matches[i].Report;
                Assert.Equal((day.Matches[i].Home.Index, day.Matches[i].Away.Index), (pairing.Home, pairing.Away));
                Assert.Equal(new PromotionLeg(report.HomeGoals, report.AwayGoals, false), pairing.First);
            }
            else
                Assert.Null(pairing.First); // [ORIGINAL] im Spiel kein Termin (Datei 0x32294)
            Assert.Null(pairing.Second);
        }
        Assert.All(page.Table, r => Assert.Equal(0, r.GoalsFor + r.GoalsAgainst + r.PointsPlus));

        var other = Assert.IsType<PromotionRoundScreen>(Assert.Single(LivePageScreens.Create(host, menu, day, pages[1], 20)));
        Assert.Equal(League.Oberliga, other.League);
        Assert.All(other.Page!.Pairings, p => Assert.True(p.First is null && p.Second is null));

        StepTo(day, 60);
        // nach 60: Index 4 aus, 7 an – nur die Seite der Gruppe 1
        Assert.Equal(League.Oberliga, LivePageScreens.PromotionLeague(Assert.Single(LivePageScreens.For(game, day, 60))));
        var table = new PromotionRoundScreen(host, menu, League.ZweiteLiga, new GroupLive(60, game.CurrentDay,
            (l, r) => LivePageScreens.Score(day, l, r, 60, false))).Page!.Table;
        var goals = day.Matches.Sum(m => m.Report.HomeGoals + m.Report.AwayGoals);
        Assert.Equal(goals, table.Sum(r => r.GoalsFor));
        Assert.Equal(8, table.Sum(r => r.PointsPlus + r.PointsMinus));
    }

    /// <summary>
    /// Europapokal-Gruppe im Spielablauf am dritten Termin (zwei gespielt): nach 20 die Ergebnisse der Termine 1–2 und der laufende
    /// Stand des dritten, keine Termine, Tabelle ohne den laufenden Termin; nach 60 mit ihm; nach 65 ohne den laufenden Termin
    /// ([ORIGINAL] c = Terminzahl + Drittelpause, aufstieg_europapokal.py).
    /// </summary>
    [Fact]
    public void EuropeanCupGroup_Live_RunningDate_NoDates_TableFrom60()
    {
        var game = Synthetic();
        var (host, menu) = Host(game);
        var dates = SeasonCalendar.DaysOf(DayEvents.EuropeanCup);
        var group = new CupGroup(1, [64, 65, 66, 67]);
        for (var round = 0; round < 2; round++)
            foreach (var (home, away) in group.Pairings(round))
                group.Results.Add(new KnockoutMatch(dates[round], home, away, 2, 1));
        var today = group.Pairings(2);
        DebCupResult? Score(int left, int right) =>
            (left, right) == today[0] ? new DebCupResult(4, 3, 0) : (left, right) == today[1] ? new DebCupResult(0, 1, 0) : null;

        EuropeanCupScreen Live(int minute) => new(host, menu, 0, group, new GroupLive(minute, dates[2], Score));
        var at20 = Live(20);
        static string R(int l, int r) => CompetitionWidgets.Number(l, 2) + ":" + CompetitionWidgets.Number(r, 2);
        Assert.Equal((R(2, 1), 226, false), at20.Cell(0, 0, 0));
        Assert.Equal((R(4, 3), 226, false), at20.Cell(2, 0, 0));
        Assert.Equal((R(0, 1), 226, false), at20.Cell(2, 1, 0));
        for (var round = 0; round < 3; round++)
            Assert.Null(at20.Cell(round, 0, 1)); // [ORIGINAL] im Spiel kein Termin für die Rückspiele (Datei 0x32294)
        Assert.Equal(12, at20.Standings!.Sum(r => r.GoalsFor));
        Assert.Equal(16, at20.Standings!.Sum(r => r.PointsPlus + r.PointsMinus));

        var at60 = Live(60);
        Assert.Equal((R(4, 3), 226, false), at60.Cell(2, 0, 0));
        Assert.Equal(20, at60.Standings!.Sum(r => r.GoalsFor));
        Assert.Equal(24, at60.Standings!.Sum(r => r.PointsPlus + r.PointsMinus));

        Assert.Null(Live(65).Cell(2, 0, 0)); // c = 1 + 0 + 0 − … : laufender Termin nach der Verlängerung nicht (Datei 0x3AB58)
    }

    /// <summary>Endrunde ab dem siebten Termin (Code 5, DGROUP 0x7AF &gt; 5): Seite 5 statt 3.</summary>
    [Fact]
    public void EuropeanCupView_FinalRoundFromSeventhDate()
    {
        var game = Synthetic();
        var dates = SeasonCalendar.DaysOf(DayEvents.EuropeanCup);
        PassTo(game, dates[0]);
        Assert.Equal(0, LivePageScreens.EuropeanCupView(game));
        PassTo(game, dates[EuropeanCup.GroupDates]);
        Assert.Equal(2, LivePageScreens.EuropeanCupView(game));
    }

    private static (ScreenDriver D, LiveMatchScreen Live) LiveDay(MatchCompetition competition, Action<GameOptions> options)
    {
        var d = FlowTests.CreateDriver();
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "M0", Head = 0, ClubIndex = 2 });
        var game = Core.NewGame.Create(d.Host.Assets.Database, setup, 5);
        game.Options.MatchSpeed = 0;
        options(game.Options);
        // ohne Anzeige bis kurz vor den ersten Europapokal-Termin (die Zeitleiste allein, kein Live-Bildschirm)
        var first = SeasonCalendar.DaysOf(DayEvents.EuropeanCup)[0];
        while (game.CurrentDay < first - 1 && game.Phase == SeasonPhase.Running)
            game.Continue();
        d.Host.Game = game;
        d.Host.Show(new MainMenuScreen(d.Host));
        d.Frame();
        for (var guard = 0; guard < 20000; guard++)
        {
            if (d.Host.Current is MainMenuScreen)
                d.NextDay();
            else if (d.Host.Current is LiveMatchScreen screen && screen.Session.Day?.Matches[0].Competition == competition)
                return (d, screen);
            else if (d.Host.Current is LiveMatchScreen)
                d.Press(Key.Escape); // andere Termine überspringen (Enter/Escape, #205)
            else
                d.Frame();
        }
        throw new InvalidOperationException($"kein Live-Termin {competition}, Tag {d.Host.Game!.CurrentDay}");
    }

    /// <summary>
    /// Europapokal-Termin im Live-Spiel mit „Europapokal“ (11) und „Drittelpause 1.Liga“ (0): nach 20, 40 und 60 „Europacup
    /// Gruppe1“ mit dem laufenden Stand der Managerpaarung, ohne Termine; Tabelle bis 40 ohne, nach 60 mit dem Termin
    /// (eisbaeren_f51_03314 bzw. eisbaeren_f20_02951). Vor der Spur rot: keine Seite.
    /// </summary>
    [GameDataFact]
    public void EuropeanCupDay_PagesWithRunningScore()
    {
        var (d, live) = LiveDay(MatchCompetition.EuropeanCup, o => (o.ShowEuropeanCup, o.Reports[0]) = (true, true));
        var game = d.Host.Game!;
        var match = live.Session.Day!.MatchOf(game.Managers[0].ClubIndex)!;
        var seen = new List<int>();
        for (var i = 0; i < 200000 && !live.Done; i++)
        {
            if (d.Host.Current == live)
            {
                if (live.WaitingForClick)
                    d.Click(160, 120);
                else
                    d.Frame();
            }
            else if (d.Host.Current is EuropeanCupScreen page)
            {
                var minute = Assert.IsType<int>(page.LiveMinute);
                seen.Add(minute);
                Assert.Equal(0, page.View);
                // Managerverein in Gruppe 2: Knopf „Gruppe 2“ wechselt mit derselben Minute (Datei 0x3ACBB)
                if (!page.Group!.Contains(match.Home.Index))
                {
                    d.Click(EuropeanCupScreen.LeftButton.X + 5, EuropeanCupScreen.LeftButton.Y + 5);
                    Assert.Equal(1, page.View);
                }
                var group = page.Group!;
                var dates = SeasonCalendar.DaysOf(DayEvents.EuropeanCup);
                var g = dates.Count(x => x < game.CurrentDay);
                var round = g % 3;
                var pairing = group.Pairings(round).ToList().FindIndex(p => p.Home == match.Home.Index || p.Away == match.Home.Index);
                var leftIsHome = group.Pairings(round)[pairing].Home == match.Home.Index;
                var (h, a) = (match.Report.HomeGoals, match.Report.AwayGoals);
                var expected = leftIsHome ? (h, a) : (a, h);
                Assert.Equal(CompetitionWidgets.Number(expected.Item1, 2) + ":" + CompetitionWidgets.Number(expected.Item2, 2),
                    page.Cell(round, pairing, g / 3)?.Text);
                for (var r = 0; r < 3; r++)
                    for (var leg = 0; leg < 2; leg++)
                        Assert.False(page.Cell(r, 0, leg)?.IsDate ?? false);
                // je Termin zwei Spiele zu je 2 + 2 Punkten (Plus- und Minuspunkte)
                Assert.Equal(8 * (g + (minute >= 60 ? 1 : 0)), page.Standings!.Sum(r => r.PointsPlus + r.PointsMinus));
                page.Draw(d.Surface);
                d.RightClick(5, 5);
            }
            else if (live.ShowingPage)
                d.RightClick(5, 5);
            else
                break;
        }
        Assert.Equal([20, 40, 60], seen);
    }
}
