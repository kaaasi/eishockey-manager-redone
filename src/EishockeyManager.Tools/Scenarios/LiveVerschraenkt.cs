using EishockeyManager.Core;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Live;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.Tools.Scenarios;

/// <summary>
/// Spur live-verschraenkt (S01, <c>docs/analyse/live-spieltag.md</c>): Ligaspieltag mit zwei bzw. drei Managerpaarungen in
/// verschiedenen Ligen; alle Reihen stehen von Minute 1 an und laufen gleichzeitig (Vergleich mit <c>duell_s2a_01126</c>).
/// </summary>
internal static class LiveVerschraenkt
{
    [Scenario("live-verschraenkt-2")]
    private static void TwoPairings(ScreenDriver d) => Run(d, 2);

    [Scenario("live-verschraenkt-3")]
    private static void ThreePairings(ScreenDriver d) => Run(d, 3);

    private static void Run(ScreenDriver d, int managers)
    {
        var setup = new GameSetup();
        var leagues = new[] { League.Bundesliga, League.ZweiteLiga, League.Oberliga };
        for (var i = 0; i < managers; i++)
        {
            var club = Enumerable.Range(0, Core.NewGame.SelectableClubs).First(c => Core.NewGame.DatabaseLeague(c) == leagues[i]);
            setup.Managers.Add(new ManagerSetup { Name = $"M{i}", Head = i, ClubIndex = club, StartLeague = leagues[i] });
        }
        var game = Core.NewGame.Create(d.Host.Assets.Database, setup, 5);
        game.Options.MatchSpeed = 0;
        d.Host.Game = game;
        d.Host.Show(new MainMenuScreen(d.Host));
        d.Frame();
        LiveMatchScreen? live = null;
        for (var guard = 0; guard < 20000 && live is null; guard++)
        {
            if (d.Host.Current is MainMenuScreen)
                d.NextDay();
            else if (d.Host.Current is LiveMatchScreen s && s.Session.Day?.Matches[0].Competition == MatchCompetition.League)
                live = s;
            else if (d.Host.Current is LiveMatchScreen)
                d.Press(UI.Key.Escape);
            else
                d.Frame();
        }
        if (live is null)
            throw new InvalidOperationException("Kein Ligaspieltag mit Live-Bildschirm erreicht.");
        while (live.DisplayMinute < 2)
            d.Frame();
        d.Move(319, 239);
        d.Save("01_minute_02");
        while (live.DisplayMinute < 21 && !live.Done)
            d.Frame();
        d.Save("02_minute_21");
    }
}
