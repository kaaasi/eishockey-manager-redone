using EishockeyManager.Core;
using EishockeyManager.UI.Screens.ManagerArea;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.Tools.Scenarios;

/// <summary>Abläufe der Spur finanzen-rest: Stände wie in den Referenzvideos für den Pixelvergleich.</summary>
internal static class FinanzenRestScenarios
{
    /// <summary>Jugendteams wie UI-Video 8:39 (Jugend 19.000 DM) und Folge 4 0:28:25 (alle 12. Platz 3. Liga).</summary>
    [Scenario("jugend-original")]
    private static void YouthOriginal(ScreenDriver d)
    {
        d.StartNewGame();
        d.OpenTile("Jugend");
        var bar = YouthScreen.Bar(1);
        d.Click(bar.X + 67, bar.Y + 4);
        d.Move(1, 239); d.Save("01_jugend_19000");
        var game = d.Host.Game!;
        game.SetYouthFunding(game.ActiveManager, YouthTeamKind.Jugend, 0);
        foreach (var team in game.Finance(game.ActiveManager).Youth)
            team.MoveTo(YouthTeam.Leagues * YouthTeam.TeamsPerLeague - 1);
        d.Move(1, 239); d.Save("02_zwoelfter_platz");
    }
}
