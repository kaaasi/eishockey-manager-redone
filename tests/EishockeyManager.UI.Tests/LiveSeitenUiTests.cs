using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Competitions;
using EishockeyManager.UI.Screens.Live;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur live-verschraenkt, Teil 3 (S04: A292, A436, F068; <c>docs/analyse/live-spieltag.md</c> §8): Nach dem Drittel zeigt das
/// Live-Spiel die Seiten des Seitenwählers 0x3A88A mit „n.Minute“ und dem laufenden Stand ([ORIGINAL] Datei 0x3858C–0x386CD,
/// Nachbildungen <c>drittelseiten.py</c> und <c>pokalseiten.py</c>). Vor dem Teil rot: das Live-Spiel zeigte keine Seite.
/// </summary>
public sealed class LiveSeitenUiTests
{
    private static (ScreenDriver D, LiveMatchScreen Live) LiveDay(MatchCompetition competition, Action<GameOptions> options)
    {
        var d = FlowTests.CreateDriver();
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "M0", Head = 0, ClubIndex = 2 });
        var game = Core.NewGame.Create(d.Host.Assets.Database, setup, 5);
        game.Options.MatchSpeed = 0;
        options(game.Options);
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
        throw new InvalidOperationException("kein Live-Termin " + competition);
    }

    /// <summary>Spielt den Termin mit Klicks; liefert je gezeigter Seite (Minute des Live-Bildschirms, Seite).</summary>
    private static List<(int Minute, Screen Page, (int Home, int Away) Score)> Play(ScreenDriver d, LiveMatchScreen live, LiveMatch? watched = null)
    {
        var pages = new List<(int, Screen, (int, int))>();
        for (var i = 0; i < 200000 && !live.Done; i++)
        {
            if (d.Host.Current == live)
            {
                if (live.WaitingForClick)
                    d.Click(160, 120);
                else
                    d.Frame();
            }
            else if (live.ShowingPage)
            {
                pages.Add((live.DisplayMinute, d.Host.Current!, watched is null ? (0, 0) : (watched.Report.HomeGoals, watched.Report.AwayGoals)));
                d.RightClick(5, 5); // [ORIGINAL] Rechtsklick führt weiter (Datei 0x33B33)
            }
            else
                break;
        }
        return pages;
    }

    /// <summary>
    /// DEB-Pokal mit den Anfangswerten (nur DEB-Pokal an): die Seite „DEB-Pokalspiele“ zuerst nach Minute 60 mit „60.Minute“ und
    /// dem Stand der Managerpaarung in der Spalte des laufenden Termins.
    /// </summary>
    [GameDataFact]
    public void DebCupDay_DefaultOptions_PageAfterMinute60WithRunningScore()
    {
        var (d, live) = LiveDay(MatchCompetition.DebCup, _ => { });
        var game = d.Host.Game!;
        var club = game.Managers[0].ClubIndex;
        var match = live.Session.Day!.MatchOf(club)!;
        var pages = Play(d, live, match);
        var debPages = pages.Where(p => p.Page is DebCupGamesScreen).ToList();
        // [ORIGINAL] nach 20/40 keine Seite (Drittelpause aus); 60, bei Verlängerung einer Paarung des Termins 65 und 70, nach
        // dem Penaltyschießen noch einmal 70 (Datei 0x37254/0x37276/0x372A7)
        Assert.Equal(60, debPages[0].Minute);
        Assert.All(debPages, p => Assert.Contains(p.Minute, new[] { 60, 65, 70 }));
        Assert.True(debPages.Count(p => p.Minute == 70) <= 2);
        Assert.All(pages, p => Assert.IsType<DebCupGamesScreen>(p.Page));
        var deb = debPages[0];
        Assert.Equal(60, deb.Minute);
        var screen = (DebCupGamesScreen)deb.Page;
        Assert.Equal(60, screen.LiveMinute);
        Assert.Equal("       60.Minute", DebCupGamesScreen.LiveLine(60));
        var overview = screen.Overview!;
        var row = overview.Groups.SelectMany(g => g.Rows).Single(r => r.LeftClub == club || r.RightClub == club);
        Assert.Equal(overview.DatesPlayed + 1, row.Results.Count);
        var live60 = row.Results[^1]!.Value;
        var leftIsHome = row.LeftClub == match.Home.Index;
        var (h, a) = deb.Score; // Stand des Berichts, als die Seite stand
        Assert.Equal(leftIsHome ? (h, a) : (a, h), (live60.Left, live60.Right));
        Assert.Equal(0, live60.Dots); // [ORIGINAL] Marke erst nach den Seiten der Minute 60 (0x32AA3 Datei 0x37211)
    }

    /// <summary>„Drittelpause 1.Liga“ an: die Seite auch nach Minute 20 und 40 (drittelseiten.py Fall 2).</summary>
    [GameDataFact]
    public void DebCupDay_Intermission_PagesAfter20And40()
    {
        var (d, live) = LiveDay(MatchCompetition.DebCup, o => o.Reports[0] = true);
        var minutes = Play(d, live).Where(p => p.Page is DebCupGamesScreen).Select(p => p.Minute).ToList();
        Assert.Equal([20, 40, 60], minutes.Take(3));
    }

    /// <summary>Schalter DEB-Pokal aus: keine Seite (drittelseiten.py Fall 2, „alles aus“).</summary>
    [GameDataFact]
    public void DebCupDay_SwitchOff_NoPage()
    {
        var (d, live) = LiveDay(MatchCompetition.DebCup, o => o.ShowDebCup = false);
        Assert.Empty(Play(d, live));
    }
}
