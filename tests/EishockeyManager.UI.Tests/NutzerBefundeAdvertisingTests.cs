using EishockeyManager.Tests.Support;
using EishockeyManager.Core;
using EishockeyManager.UI.Screens.ManagerArea;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur nutzer-befunde, #834: Eisflächenwerbung. Nutzerprobe (main 01ed8a5, neues Spiel in der Oberliga ohne Halle): die
/// gesperrte Eistafel wurde rot gerahmt, ließ sich blättern und zeigte „Info“, aber weder Logo noch Angebotstext. Das Original
/// sperrt die Tafel ganz (em.exe 0x2B56A–0x2B5D3, 0x2B6D6–0x2B70E) und trifft nur die Tafel selbst (0x2B3C9).
/// </summary>
public class NutzerBefundeAdvertisingTests
{
    private static void Click(ScreenDriver d, Rect r) => d.Click(r.X + r.W / 2, r.Y + r.H / 2);

    private static (ScreenDriver Driver, AdvertisingScreen Screen, GameState Game, Core.Manager Me) Open(int roof, int cooling)
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        var me = game.ActiveManager;
        game.Finance(me).Stadium.SetLevel(StadiumFeature.Ueberdachung, roof);
        game.Finance(me).Stadium.SetLevel(StadiumFeature.Kuehlanlage, cooling);
        d.OpenTile("Werbung");
        return (d, Assert.IsType<AdvertisingScreen>(d.Host.Current), game, me);
    }

    [GameDataFact]
    public void LockedIceBoard_HasNoFrame_NoBrowsing_NoInfo()
    {
        var (d, screen, _, _) = Open(roof: 0, cooling: 0);
        Click(d, AdvertisingScreen.IceBoardRect(0));
        Assert.Null(screen.SelectedSlot);
        Click(d, AdvertisingScreen.ArrowDown);
        Assert.Equal(-1, screen.Cursor);
        Click(d, AdvertisingScreen.InfoButton);
        Assert.False(screen.ShowsInfo);
        Assert.Empty(screen.PanelTexts());
    }

    [GameDataFact]
    public void CrowdBetweenBoards_IsNoHitArea()
    {
        var (d, screen, _, _) = Open(roof: 2, cooling: 2);
        d.Click(170, 110); // Zuschauer in der Mitte: 0x2B3C9 prüft nur 0x10C < x < 0x130
        Assert.Null(screen.SelectedSlot);
        Click(d, AdvertisingScreen.IceBoardHit(1));
        Assert.Equal(new AdSlot(AdSlotKind.Eisflaeche2), screen.SelectedSlot);
    }

    [GameDataFact]
    public void UnlockedIceBoard_ShowsLogoAndOffer_AndCanBeTaken()
    {
        var (d, screen, game, me) = Open(roof: 2, cooling: 1);
        var ads = game.Ads(me);
        ads.Offers[AdSlotKind.Eisflaeche] = new() { [1] = new SponsorOffer(AdSlotKind.Eisflaeche, 1, 24, 1, 4_200) };

        Click(d, AdvertisingScreen.IceBoardRect(0));
        Assert.Equal(new AdSlot(AdSlotKind.Eisflaeche), screen.SelectedSlot);
        Assert.Contains(screen.PanelTexts(), t => t.Text == "Diese Werbefläche ist noch unbefleckt.");

        Click(d, AdvertisingScreen.ArrowDown); // Firma 0 ohne Angebot
        Assert.Contains(screen.PanelTexts(), t => t.Text == "Es liegt momentan kein Angebot vor...");
        Assert.Null(screen.CurrentOffer());

        Click(d, AdvertisingScreen.ArrowDown); // Firma 1 mit Angebot
        Assert.Contains(new AdvertisingScreen.PanelText(191, 205, ": 4.200 DM  pro Monat"), screen.PanelTexts());
        var frame = d.Surface.Pixels;
        // Logo der Firma im Feld 0x2AA6B: Fläche nicht schwarz.
        Assert.Contains(Enumerable.Range(0, AdvertisingScreen.LogoArea.W),
            x => frame[(AdvertisingScreen.LogoArea.Y + 12) * 320 + AdvertisingScreen.LogoArea.X + x] != Colors.Black);
        Click(d, AdvertisingScreen.AcceptButton);
        Assert.Equal(1, ads.ContractAt(new AdSlot(AdSlotKind.Eisflaeche))!.Sponsor);
    }
}
