using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Live;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur live-verschraenkt, Teil 2 (S01: A450, A555; <c>docs/analyse/live-spieltag.md</c>): Am Ligaspieltag zeigt der
/// Live-Bildschirm alle Managerpaarungen von Minute 1 an und rechnet sie Minute für Minute gleichzeitig ([ORIGINAL] 0x544C0
/// vor den Minuten, Minutenschleife 0x37A62 über alle Paarungen, Tafel 0x52BD6 je Minute für alle Reihen). Vor dem Teil rot:
/// die zweite Managerpaarung erschien erst nach dem Ende der ersten (eine Reihe).
/// </summary>
public sealed class LiveVerschraenktUiTests
{
    [GameDataFact]
    public void TwoManagerPairings_BothRowsFromMinuteOne()
    {
        var d = FlowTests.CreateDriver();
        var second = Enumerable.Range(0, Core.NewGame.SelectableClubs)
            .First(c => Core.NewGame.DatabaseLeague(c) == League.ZweiteLiga);
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "M0", Head = 0, ClubIndex = 2 });
        setup.Managers.Add(new ManagerSetup { Name = "M1", Head = 1, ClubIndex = second, StartLeague = League.ZweiteLiga });
        var game = Core.NewGame.Create(d.Host.Assets.Database, setup, 5);
        game.Options.MatchSpeed = 0;
        d.Host.Game = game;
        d.Host.Show(new MainMenuScreen(d.Host));
        d.Frame();
        LiveMatchScreen? live = null;
        for (var guard = 0; guard < 5000 && live is null; guard++)
        {
            if (d.Host.Current is MainMenuScreen)
                d.NextDay();
            else if (d.Host.Current is LiveMatchScreen screen && screen.Session.Day?.Matches[0].Competition == MatchCompetition.League)
                live = screen;
            else if (d.Host.Current is LiveMatchScreen)
                d.Press(Key.Escape); // andere Termine überspringen (Enter/Escape, #205)
            else
                d.Frame();
        }
        Assert.NotNull(live);
        Assert.Equal(2, live.Session.Shown.Count);
        Assert.All(live.Session.Shown, m => Assert.True(m.Running));
        for (var i = 0; i < 20000 && live.DisplayMinute < 5; i++)
            d.Frame();
        Assert.True(live.DisplayMinute >= 5);
        var minute = live.Session.Day!.Minute;
        Assert.All(live.Session.Shown, m => Assert.Equal(minute, m.LastMinute));
    }
}
