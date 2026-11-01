using EishockeyManager.Core;
using EishockeyManager.UI;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Competitions;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.Tools.Scenarios;

/// <summary>
/// Spur wettbewerbe-komplett Teil 2: Gegner-Fenster 0x128:0x2A aus WM-Tabelle, WM-Paarungen und WM-Statistik. Vorlagen:
/// DOSBox <c>nur-weltkugel/14–16</c> (vor der WM) und Video <c>bundestrainer_00610</c>/<c>_00658</c> (während der WM).
/// </summary>
internal static class WettbewerbeGegnerFenster
{
    [Scenario("gegner-fenster")]
    private static void Window(ScreenDriver d)
    {
        d.StartNewGame();
        var game = d.Host.Game!;
        var menu = new MainMenuScreen(d.Host);
        var germanyGroup = game.WorldCup!.Groups.First(g => g.Teams.Contains(NationalTeams.Germany));
        var view = game.WorldCup.Groups.ToList().IndexOf(germanyGroup);
        var other = germanyGroup.Table(game.Clubs).Select(r => r.Club.Index).First(c => c != NationalTeams.Germany);
        var row = germanyGroup.Table(game.Clubs).Select(r => r.Club.Index).ToList().IndexOf(other);

        var table = new WorldCupScreen(d.Host, menu, view);
        d.Host.Show(table);
        d.Click(200, 43 + WorldCupScreen.RowPitch * row + 5);
        d.Frame(); d.Save("00_tabelle_vor_der_wm");

        for (var i = 0; i < 5000 && game.WorldCup.Groups[0].Results.Count < 6; i++)
            d.Advance();
        table = new WorldCupScreen(d.Host, menu, view);
        d.Host.Show(table);
        row = germanyGroup.Table(game.Clubs).Select(r => r.Club.Index).ToList().IndexOf(other);
        d.Click(200, 43 + WorldCupScreen.RowPitch * row + 5);
        d.Frame(); d.Save("01_tabelle_nach_zwei_spieltagen");

        for (var i = 0; i < 5000 && game.WorldCup.Knockout.Count < 3; i++)
            d.Advance();
        d.Host.Show(new WorldCupStatisticsScreen(d.Host, menu));
        d.Click(30, 140);
        d.Frame(); d.Save("02_wm_statistik_nach_der_wm");
    }
}
