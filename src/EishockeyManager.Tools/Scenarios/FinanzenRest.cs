using EishockeyManager.Core;
using EishockeyManager.UI;
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

    /// <summary>
    /// Werbung wie UI-Video 6:20–7:00 (Pixelvergleich 0x2A9AD): Start ohne Auswahl (6:24), leere Bande 4 (6:20),
    /// Angebot CCH 12 Monate, 1 Bande, 3.454 DM an Bande 1 (6:40), JWE ohne Angebot (7:00).
    /// </summary>
    [Scenario("werbung-original")]
    private static void AdvertisingOriginal(ScreenDriver d)
    {
        d.StartNewGame();
        var game = d.Host.Game!;
        var ads = game.Ads(game.ActiveManager);
        ads.Offers[AdSlotKind.Bande].Clear();
        ads.Offers[AdSlotKind.Bande][1] = new SponsorOffer(AdSlotKind.Bande, 1, 12, 1, 3_454);
        d.OpenTile("Werbung");
        d.Move(1, 239); d.Save("01_start");
        Click(d, AdvertisingScreen.BoardRect(3)); d.Move(1, 239); d.Save("02_unbefleckt");
        Click(d, AdvertisingScreen.BoardRect(0));
        Click(d, AdvertisingScreen.ArrowDown); Click(d, AdvertisingScreen.ArrowDown);
        d.Move(1, 239); d.Save("03_angebot_cch");
        Click(d, AdvertisingScreen.ArrowUp); d.Move(1, 239); d.Save("04_kein_angebot_jwe");
    }

    private static void Click(ScreenDriver d, Rect r) => d.Click(r.X + r.W / 2, r.Y + r.H / 2);
}
