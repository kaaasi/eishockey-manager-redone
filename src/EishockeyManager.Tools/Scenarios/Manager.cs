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

        // Herrenhäuser wählen, links „kaufen“, Balken und „+“, buchen
        d.Click(20, 115); // Auswahl erst per Klick (#206)
        d.Save("02_herrenhaeuser_gewaehlt");
        d.Click(90, 92); d.Save("03_hinweis_kaufen");
        d.Click(158 - 40, 93);
        d.Click(FinanceScreen.LeftPlus.X + 2, FinanceScreen.LeftPlus.Y + 2);
        d.Move(1, 239); d.Save("04_betrag");
        d.Click(FinanceScreen.LeftArrow.X + 4, FinanceScreen.LeftArrow.Y + 4);
        d.Move(1, 239); d.Save("05_gekauft");

        // Bank, rechts einzahlen: Balken auf 50 Pixel
        d.Click(95, 30); // Bank wählen (Klick neben das Bankbild, das öffnet das Finanzbuch)
        d.Click(230, 92);
        d.Click(160 + 50, 93); d.Save("06_sparbuch_betrag");
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

    [Scenario("jugend")]
    private static void Youth(ScreenDriver d)
    {
        d.StartNewGame();
        d.OpenTile("Jugend");
        d.Move(1, 239); d.Save("01_jugendteams");
        d.Click(YouthScreen.Bar(0).X + 7, YouthScreen.Bar(0).Y + 4);
        d.Click(YouthScreen.Bar(1).Right - 1, YouthScreen.Bar(1).Y + 4);
        d.Click(YouthScreen.Bar(2).X + 35, YouthScreen.Bar(2).Y + 4);
        d.Move(1, 239); d.Save("02_foerderung");
        // Spur geld Teil 6: kein „Spion“ mehr im Jugendfenster (A328, nur Transferliste 0x465F1).
        d.Press(Key.Escape);
        for (var md = 0; md < 30; md++) d.PlayMatchDay();
        d.OpenTile("Jugend"); d.Move(1, 239); d.Save("03_nach_30_spieltagen");
    }

    [Scenario("posteingang")]
    private static void Inbox(ScreenDriver d)
    {
        d.StartNewGame();
        d.Move(1, 1); d.Save("01_leiste_leer");
        var game = d.Host.Game!;
        var me = game.ActiveManager;
        var stadium = game.Finance(me).Stadium;
        game.StartConstruction(me, Core.StadiumProject.Feature, Core.StadiumFeature.Anzeigetafel, 1);
        var ads = game.Ads(me);
        ads.Offers[Core.AdSlotKind.Trikot][5] = new Core.SponsorOffer(Core.AdSlotKind.Trikot, 5, 1, 1, 9_000);
        game.AcceptOffer(me, new Core.AdSlot(Core.AdSlotKind.Trikot), 5);
        // Bauzeit nach em.exe 10–19 Wochen (Spur geld, Teil 1): spielen, bis die Meldung da ist.
        for (var md = 0; md < 10 || (stadium.Constructions.Any() && md < 60); md++) d.PlayMatchDay();
        d.Move(1, 1); d.Save("02_leiste_nachricht");
        d.Click(NewsBar.ScrollDown.X + 5, NewsBar.ScrollDown.Y + 5); d.Move(1, 1); d.Save("03_leiste_weiter");
        d.Click(100, 210); d.Move(1, 239); d.Save("04_posteingang");
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
