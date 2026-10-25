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

    [Scenario("werbung")]
    private static void Advertising(ScreenDriver d)
    {
        d.StartNewGame();
        d.OpenTile("Werbung");
        var screen = (AdvertisingScreen)d.Host.Current!;
        d.Move(1, 239); d.Save("01_werbung_start");
        Click(d, AdvertisingScreen.BoardRect(6)); d.Save("02_bande_gewaehlt");
        Click(d, AdvertisingScreen.ArrowDown); d.Save("03_erste_firma");
        AcceptFirstOffer(d, screen); d.Save("04_bande_belegt");
        Click(d, AdvertisingScreen.InfoButton); d.Save("05_firmen_info");
        d.Click(10, 10);
        Click(d, AdvertisingScreen.JerseyPanel); AcceptFirstOffer(d, screen); d.Save("06_trikot");
        Click(d, AdvertisingScreen.TvPanel); AcceptFirstOffer(d, screen); d.Save("07_fernsehen");
        Click(d, AdvertisingScreen.IcePanel); AcceptFirstOffer(d, screen); d.Save("08_eisflaeche");
        Click(d, AdvertisingScreen.BoardRect(2)); AcceptFirstOffer(d, screen); d.Save("09_zweite_bande");
    }

    [Scenario("stadion")]
    private static void Stadium(ScreenDriver d)
    {
        d.StartNewGame();
        d.OpenTile("Stadion");
        var game = d.Host.Game!;
        d.Move(1, 239); d.Save("01_stadion_start");
        d.Move(45, 135); d.Save("02_hinweis_gastronomie");
        d.Click(45, 135); d.Save("03_imbissbude");
        Click(d, StadiumScreen.LevelUp); d.Save("04_restaurant");
        Click(d, StadiumScreen.LevelBox); d.Save("05_rueckfrage");
        d.Click(StadiumScreen.YesButton.X + 5, StadiumScreen.YesButton.Y + 3); d.Move(300, 160); d.Save("06_baustelle");
        Click(d, StadiumScreen.TicketUp); Click(d, StadiumScreen.TicketUp);

        // Bauzeit abwarten, dann alles Weitere direkt im Spielstand bauen und ansehen
        for (var md = 0; md < 12; md++) { d.Press(Key.Escape); d.PlayMatchDay(); }
        var stadium = game.Finance(game.ActiveManager).Stadium;
        stadium.SetLevel(Core.StadiumFeature.Verkehrsanbindung, 1);
        stadium.SetLevel(Core.StadiumFeature.Ueberdachung, 1);
        stadium.SetLevel(Core.StadiumFeature.Kino, 2);
        stadium.SetLevel(Core.StadiumFeature.Bowlingbahn, 1);
        d.OpenTile("Stadion"); d.Move(1, 239); d.Save("07_ausgebaut");
        stadium.SetLevel(Core.StadiumFeature.Verkehrsanbindung, 2);
        stadium.SetLevel(Core.StadiumFeature.Ueberdachung, 2);
        stadium.SetLevel(Core.StadiumFeature.Kino, 3);
        stadium.SetLevel(Core.StadiumFeature.Gastronomie, 1);
        d.Move(1, 238); d.Save("08_voll_ausgebaut");
    }

    private static void Click(ScreenDriver d, Rect r) => d.Click(r.X + r.W / 2, r.Y + r.H / 2);

    /// <summary>Blättert, bis ein Angebot kommt, und nimmt es an.</summary>
    private static void AcceptFirstOffer(ScreenDriver d, AdvertisingScreen screen)
    {
        for (var i = 0; i < 10 && screen.CurrentOffer() is null; i++)
            Click(d, AdvertisingScreen.ArrowDown);
        d.Save($"_{screen.SelectedSlot}_angebot".Replace(' ', '_'));
        Click(d, AdvertisingScreen.AcceptButton);
    }
}
