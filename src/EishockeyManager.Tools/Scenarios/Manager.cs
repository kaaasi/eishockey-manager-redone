using EishockeyManager.UI;
using EishockeyManager.UI.Screens.ManagerArea;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.Tools.Scenarios;

/// <summary>Abläufe des Pakets „manager“: Finanzen, Werbung, Stadion, Jugend, Posteingang.</summary>
internal static class ManagerScenarios
{
    [Scenario("finanzen")]
    private static void Finances(ScreenDriver d)
    {
        d.StartNewGame();
        d.OpenTile("Finanzen");
        d.Move(1, 239); d.Save("01_finanzen_start");
        d.Move(90, 92); d.Save("02_hinweis_bank_links");
        d.Move(230, 92); d.Save("03_hinweis_bank_rechts");

        // Herrenhäuser wählen, 100 Stück kaufen
        d.Move(20, 115);
        for (var i = 0; i < 30; i++) d.Move(20, 115);
        d.Save("04_herrenhaeuser_gewaehlt");
        d.RightClick(FinanceScreen.LeftPlus.X + 4, FinanceScreen.LeftPlus.Y + 4);
        d.Move(90, 92); d.Save("05_100_stueck");
        d.Click(FinanceScreen.LeftArrow.X + 4, FinanceScreen.LeftArrow.Y + 4);
        d.Move(1, 239); d.Save("06_gekauft");

        // 50.000 DM aufs Sparbuch
        d.Move(50, 30);
        for (var i = 0; i < 5; i++) d.Click(FinanceScreen.RightPlus.X + 4, FinanceScreen.RightPlus.Y + 4);
        d.Click(FinanceScreen.RightArrow.X + 4, FinanceScreen.RightArrow.Y + 4);
        d.Move(1, 239); d.Save("07_sparbuch");

        for (var md = 0; md < 8; md++)
        {
            d.Press(Key.Escape);
            d.PlayMatchDay();
        }
        d.OpenTile("Finanzen"); d.Move(1, 239); d.Save("08_nach_8_spieltagen");
        d.Click(FinanceScreen.BankPicture.X + 10, FinanceScreen.BankPicture.Y + 10); d.Save("09_finanzbuch");
    }
}
