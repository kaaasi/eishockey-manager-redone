using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Live;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur live-verschraenkt, Teil 3b (F076; <c>docs/analyse/live-spieltag.md</c> §10): Penalty-Bildschirm des Penaltyschießens
/// 0x3901B mit dem Hintergrund 0x35CBE (drei Farbstreifen), Ankündigung und Endergebnis. Vor dem Teil rot: das Penaltyschießen
/// lief ohne Anzeige (Build-Fehler: kein <see cref="LivePenaltyScreen"/>).
/// </summary>
public sealed class LivePenaltyUiTests
{
    /// <summary>[ORIGINAL] 0x35CBE: Rand Farbe 11, Streifen 1 … 79 Farbe 0, 80 … 159 Farbe 0x12, 160 … 238 Farbe 0x18.</summary>
    [GameDataFact]
    public void Background_ThreeStripes_AndTexts()
    {
        var d = FlowTests.CreateDriver();
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "M0", Head = 0, ClubIndex = 2 });
        var game = Core.NewGame.Create(d.Host.Assets.Database, setup, 5);
        d.Host.Game = game;
        var back = new MainMenuScreen(d.Host);
        var s = new Surface();
        new LivePenaltyScreen(d.Host, back, game.Clubs[2], game.Clubs[3], null).Draw(s);
        byte At(int x, int y) => s.Pixels[y * Surface.Width + x];
        Assert.Equal(11, At(0, 0));
        Assert.Equal(11, At(319, 120));
        Assert.Equal(11, At(160, 239));
        Assert.Equal(0, At(5, 5));
        Assert.Equal(0x12, At(5, 120));
        Assert.Equal(0x18, At(5, 200));
        // Texte: Überschrift (Farbe 0x1E) im oberen, Heimverein (0x18) im mittleren, kein Endergebnis (0x10) im unteren Streifen.
        bool Any(int y1, int y2, byte c) => Enumerable.Range(y1, y2 - y1 + 1).Any(y => Enumerable.Range(0, 320).Any(x => At(x, y) == c));
        Assert.True(Any(15, 34, 0x1E));
        Assert.True(Any(82, 101, 0x18));
        Assert.False(Any(195, 214, 0x10));
        var result = new LivePenaltyScreen(d.Host, back, game.Clubs[2], game.Clubs[3], (4, 3));
        result.Draw(s);
        Assert.True(Any(195, 214, 0x10));
        Assert.Equal("4 : 3   n.P.", LivePenaltyScreen.ResultLine((4, 3)));
        d.Host.Show(result);
        d.Click(160, 120);
        Assert.Same(back, d.Host.Current);
    }

    /// <summary>
    /// Ablauf: DEB-Pokaltag der Managerpaarung, vor Minute 60 Gleichstand gesetzt (Aufbau), bis ein Penaltyschießen entsteht.
    /// [ORIGINAL] nach Minute 70 und deren Seiten (Datei 0x3728B) erst die Ankündigung, dann das Endergebnis mit den Treffern des
    /// Penaltyschießens; danach läuft der Termin weiter.
    /// </summary>
    [GameDataFact]
    public void DebCupShootout_AnnouncementThenResult()
    {
        for (var seed = 1; seed <= 12; seed++)
        {
            var d = FlowTests.CreateDriver();
            var setup = new GameSetup();
            setup.Managers.Add(new ManagerSetup { Name = "M0", Head = 0, ClubIndex = 2 });
            var game = Core.NewGame.Create(d.Host.Assets.Database, setup, seed);
            game.Options.MatchSpeed = 60;
            game.Options.ShowDebCup = false; // keine Seiten
            d.Host.Game = game;
            d.Host.Show(new MainMenuScreen(d.Host));
            d.Frame();
            LiveMatchScreen? live = null;
            for (var guard = 0; guard < 20000 && live is null; guard++)
            {
                if (d.Host.Current is MainMenuScreen)
                    d.NextDay();
                else if (d.Host.Current is LiveMatchScreen screen && screen.Session.Day?.Matches[0].Competition == MatchCompetition.DebCup)
                    live = screen;
                else if (d.Host.Current is LiveMatchScreen)
                    d.Press(Key.Escape);
                else
                    d.Frame();
            }
            Assert.NotNull(live);
            var match = Assert.Single(live.Session.Shown);
            var screens = new List<LivePenaltyScreen>();
            var tied = false;
            for (var i = 0; i < 400000 && !live.Done; i++)
            {
                if (!tied && live.Session.Day!.Minute == 59 && live.DisplayMinute == 60)
                {
                    // Aufbau: Gleichstand vor der Rechnung der Minute 60 (reine Vorbedingung für die Verlängerung).
                    match.Report.AwayGoals = match.Report.HomeGoals;
                    tied = true;
                }
                if (d.Host.Current == live)
                {
                    if (live.WaitingForClick)
                        d.Click(160, 120);
                    else
                        d.Frame();
                }
                else if (d.Host.Current is LivePenaltyScreen penalty)
                {
                    Assert.True(live.ShowingShootout);
                    screens.Add(penalty);
                    d.Click(160, 120);
                }
                else
                    d.RightClick(5, 5);
            }
            if (match.Report.ShootoutGoals is not { } goals)
                continue;
            Assert.Equal(2, screens.Count);
            Assert.Null(screens[0].Result);
            Assert.Equal(goals, screens[1].Result);
            return;
        }
        Assert.Fail("kein Penaltyschießen der Managerpaarung entstanden");
    }
}
