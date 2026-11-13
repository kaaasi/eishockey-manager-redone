using EishockeyManager.Tests.Support;
using EishockeyManager.Core;
using EishockeyManager.UI.Screens.ManagerArea;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur finanzen-werbung (#133) im Werbebildschirm: zwei Eistafeln als eigene Flächen (HALLE bzw. MEGAFROST) und die
/// Verhandlung über einen abgelaufenen Vertrag mit Option beim Klick (em.exe 0x2B75F).
/// </summary>
public class FinanceWerbungScreenTests
{
    private static void Click(ScreenDriver d, Rect r) => d.Click(r.X + r.W / 2, r.Y + r.H / 2);

    private static (ScreenDriver Driver, AdvertisingScreen Screen, GameState Game, Core.Manager Me) Open()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        d.OpenTile("Werbung");
        var screen = Assert.IsType<AdvertisingScreen>(d.Host.Current);
        var game = d.Host.Game!;
        return (d, screen, game, game.ActiveManager);
    }

    [GameDataFact]
    public void IceBoards_AreTwoSlots_LockedWithoutHallOrMegafrost()
    {
        var (d, screen, game, me) = Open();
        var stadium = game.Finance(me).Stadium;
        stadium.SetLevel(StadiumFeature.Ueberdachung, 1);
        stadium.SetLevel(StadiumFeature.Kuehlanlage, 2);
        game.Ads(me).Offers[AdSlotKind.Eisflaeche][0] = new SponsorOffer(AdSlotKind.Eisflaeche, 0, 12, 1, 2_000);
        game.Ads(me).Offers[AdSlotKind.Eisflaeche2][0] = new SponsorOffer(AdSlotKind.Eisflaeche2, 0, 12, 1, 2_500);

        Click(d, AdvertisingScreen.IceBoardRect(0));
        // #834 (R15-E2 = C): gesperrte Tafel bleibt ohne Auswahl, em.exe 0x2B56A–0x2B5D3 und 0x2B6D6–0x2B70E.
        Assert.Null(screen.SelectedSlot);
        Click(d, AdvertisingScreen.ArrowDown);
        Assert.Null(screen.CurrentOffer());

        Click(d, AdvertisingScreen.IceBoardRect(1));
        Assert.Equal(new AdSlot(AdSlotKind.Eisflaeche2), screen.SelectedSlot);
        Click(d, AdvertisingScreen.ArrowDown); // Flächenklick zeigt den Vertrag, erst Blättern das Angebot (0x2B633)
        Assert.NotNull(screen.CurrentOffer());
        Click(d, AdvertisingScreen.AcceptButton);
        Assert.NotNull(game.Ads(me).ContractAt(new AdSlot(AdSlotKind.Eisflaeche2)));
        Assert.Null(game.Ads(me).ContractAt(new AdSlot(AdSlotKind.Eisflaeche)));
    }

    [GameDataFact]
    public void ClickOnExpiredOptionContract_AsksOrDeclines()
    {
        var (d, screen, game, me) = Open();
        var ads = game.Ads(me);
        var tv = new SponsorContract { Kind = AdSlotKind.Fernsehen, Sponsor = 1, Boards = [], MonthlyAmount = 9_000, OptionMonths = 12, SignedRating = 0 };
        ads.Contracts.Add(tv);

        Click(d, AdvertisingScreen.TvPanel);
        Assert.Same(tv, screen.RenewalQuestion);
        Click(d, AdvertisingScreen.YesButton);
        Assert.Null(screen.RenewalQuestion);
        Assert.Equal((12, 0), (tv.MonthsLeft, tv.OptionMonths));

        var shirt = new SponsorContract { Kind = AdSlotKind.Trikot, Sponsor = 2, Boards = [], MonthlyAmount = 9_000, OptionMonths = 12, SignedRating = 100 };
        ads.Contracts.Add(shirt);
        Click(d, AdvertisingScreen.JerseyPanel);
        Assert.Null(screen.RenewalQuestion);
        Assert.Equal(["Die Firma", "Korona Soft", "wird den Vertrag", "nicht verlängern."], screen.MessageLines);
        Assert.DoesNotContain(shirt, ads.Contracts);
        d.Click(10, 10);
        Assert.Empty(screen.MessageLines);
    }
}
