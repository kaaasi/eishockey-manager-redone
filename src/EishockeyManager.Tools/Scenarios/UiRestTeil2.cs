using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Team;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.Tools.Scenarios;

/// <summary>
/// Spur ui-rest (Runde 17), Teil 2: Fenster „Saisonende“ (Symbole im Raster 0x06CA6, Vorlagen <c>file16_9</c>, <c>file46_2</c>)
/// und Verhandlungen am Saisonende mit Hintergrund (Vorlage <c>file46_3</c>, KÖLNER EC). Vergleichswerte im PR.
/// </summary>
internal static class UiRestTeil2
{
    [Scenario("pixel-saisonende-symbole")]
    private static void SeasonEnd(ScreenDriver d)
    {
        d.StartNewGame(clubIndex: 5); // KÖLNER EC wie file46_3
        var game = d.Host.Game!;
        for (var guard = 0; d.Host.Current is not SeasonEndSummaryScreen && guard++ < 20000;)
            d.Advance();
        d.Move(0, 239); d.Save("01_ereignisse");

        var menu = new MainMenuScreen(d.Host);
        foreach (var m in game.ActiveManagers)
            game.Clubs[m.ClubIndex].Players.Last(x => x.Role == PlayerRole.Forward && !x.OnLoan).ContractYears = 1;
        game.StartNextSeasonWithRenewals();
        d.Host.Show(ContractsScreen.ForSeasonEnd(d.Host, () => menu));
        d.Move(0, 239); d.Save("02_verhandlung_file46_3");
    }
}
