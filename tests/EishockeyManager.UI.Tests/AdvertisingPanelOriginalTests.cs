using EishockeyManager.Tests.Support;
using EishockeyManager.Core;
using EishockeyManager.UI.Screens.ManagerArea;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur finanzen-rest, Teil 2: Angebots- und Vertragsfeld des Werbebildschirms gegen em.exe 0x2A9AD (Aufrufe 0x2B633
/// mit Vertrag, 0x2BE95 mit Angebot) und den Bildschirmaufbau 0x2B268–0x2B320.
/// </summary>
public class AdvertisingPanelOriginalTests
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

    private static AdvertisingScreen.PanelText[] Texts(AdvertisingScreen screen) => screen.PanelTexts().ToArray();

    [GameDataFact]
    public void Offer_ShowsOptionRange_AtOriginalPositions()
    {
        var (d, screen, game, me) = Open();
        game.Ads(me).Offers[AdSlotKind.Bande][0] = new SponsorOffer(AdSlotKind.Bande, 0, 12, 1, 3_454, OptionMonths: 12);
        Click(d, AdvertisingScreen.BoardRect(3));
        Click(d, AdvertisingScreen.ArrowDown);

        Assert.Equal(
        [
            new(156, 179, Sponsor.Companies[0].Name),
            new(156, 189, "Dauer"), new(191, 189, ": 12 - 24 Monate"),
            new(156, 197, "Banden"), new(191, 197, ": 1"),
            new(156, 205, "Angebot"), new(191, 205, ": 3.454 DM  pro Monat"),
        ], Texts(screen));
    }

    [GameDataFact]
    public void Offer_OneMonthWithoutOption_IsSingular_AndShownEvenWithoutFreeBoards()
    {
        var (d, screen, game, me) = Open();
        var ads = game.Ads(me);
        ads.Offers[AdSlotKind.Bande][0] = new SponsorOffer(AdSlotKind.Bande, 0, 1, 3, 2_000);
        for (var b = 0; b < 5; b++)
            ads.Contracts.Add(new SponsorContract { Kind = AdSlotKind.Bande, Sponsor = 5, Boards = [b], MonthlyAmount = 100, MonthsLeft = 12 });
        Click(d, AdvertisingScreen.BoardRect(6));
        Click(d, AdvertisingScreen.ArrowDown);

        // 0x2A9AD zeigt den Angebotssatz unabhängig von freien Banden; nur „Annehmen“ fehlt (0x2BF86).
        Assert.Contains(new AdvertisingScreen.PanelText(191, 189, ": 1 Monat"), Texts(screen));
        Assert.Contains(new AdvertisingScreen.PanelText(191, 197, ": 3"), Texts(screen));
        Assert.Null(screen.CurrentOffer());
    }

    [GameDataFact]
    public void Contract_ShowsPraemie_AndOptionNote()
    {
        var (d, screen, game, me) = Open();
        var ads = game.Ads(me);
        ads.Contracts.Add(new SponsorContract { Kind = AdSlotKind.Bande, Sponsor = 2, Boards = [1, 4], MonthlyAmount = 5_464, MonthsLeft = 7 });
        var tv = new SponsorContract { Kind = AdSlotKind.Fernsehen, Sponsor = 1, Boards = [], MonthlyAmount = 9_000, MonthsLeft = 0, OptionMonths = 12 };
        ads.Contracts.Add(tv);

        Click(d, AdvertisingScreen.BoardRect(4));
        Assert.Equal(
        [
            new(156, 179, Sponsor.Companies[2].Name),
            new(156, 189, "Dauer"), new(191, 189, ": 7 Monate"),
            new(156, 197, "Banden"), new(191, 197, ": 2"),
            new(156, 205, "Prämie"), new(191, 205, ": 5.464 DM  pro Monat"),
        ], Texts(screen));

        Click(d, AdvertisingScreen.TvPanel); // Vertragsfeld vor der Verhandlung (0x2B633 vor 0x2B75F)
        Assert.Same(tv, screen.RenewalQuestion);
        Assert.Equal(
        [
            new(156, 179, Sponsor.Stations[1].Name),
            new(156, 189, "Dauer"), new(191, 189, ": 0 - 12 Monate"), new(275, 189, "! Option !"),
            new(156, 205, "Prämie"), new(191, 205, ": 9.000 DM  pro Monat"),
        ], Texts(screen));
    }

    [GameDataFact]
    public void SelectingSlot_ShowsContractView_NotLastBrowsedOffer()
    {
        var (d, screen, game, me) = Open();
        game.Ads(me).Offers[AdSlotKind.Bande][0] = new SponsorOffer(AdSlotKind.Bande, 0, 12, 1, 3_454);
        Click(d, AdvertisingScreen.BoardRect(3));
        Click(d, AdvertisingScreen.ArrowDown);
        Click(d, AdvertisingScreen.BoardRect(5));
        Assert.Equal(-1, screen.Cursor);
        Assert.Equal([new(156, 189, "Diese Werbefläche ist noch unbefleckt.")], Texts(screen));
    }

    [GameDataFact]
    public void Pixels_LogoBox_Line_Arrows_Text()
    {
        var (d, screen, game, me) = Open();
        var s = d.Surface;
        var background = d.Host.Assets.Image(AdvertisingScreen.Background);
        d.Frame();

        // Start: Kasten nur umrandet (Farbe 24), innen Hintergrund (Video 6:24).
        Assert.Equal(24, s.Pixels[178 * 320 + 110]);
        Assert.Equal(24, s.Pixels[190 * 320 + 99]);
        Assert.Equal(24, s.Pixels[190 * 320 + 135]);
        Assert.Equal(background.Pixels[190 * background.Width + 110], s.Pixels[190 * 320 + 110]);

        // Trennlinie 0x30:0x995 (90|213)–(310|213), Farbe 28.
        Assert.Equal(28, s.Pixels[213 * 320 + 90]);
        Assert.Equal(28, s.Pixels[213 * 320 + 310]);
        Assert.NotEqual(28, s.Pixels[213 * 320 + 89]);
        Assert.NotEqual(28, s.Pixels[212 * 320 + 200]);

        // Pfeile aus kb\4 (20|0) 10 × 17 bei (138|183), Rahmen (137|182)–(148|200) Farbe 24.
        var arrows = d.Host.Assets.Image(4);
        for (var y = 0; y < 17; y++)
            for (var x = 0; x < 10; x++)
                Assert.Equal(arrows.Pixels[y * arrows.Width + 20 + x], s.Pixels[(183 + y) * 320 + 138 + x]);
        Assert.Equal(24, s.Pixels[182 * 320 + 137]);
        Assert.Equal(24, s.Pixels[200 * 320 + 148]);

        // Leere Fläche: schwarz (100|179) 35 × 25 innerhalb der Umrandung (Video 6:20), Auswahlrahmen Farbe 18.
        Click(d, AdvertisingScreen.BoardRect(3));
        d.Frame();
        Assert.Equal(0, s.Pixels[179 * 320 + 100]);
        Assert.Equal(0, s.Pixels[203 * 320 + 134]);
        Assert.Equal(24, s.Pixels[204 * 320 + 134]);
        Assert.Equal(18, s.Pixels[AdvertisingScreen.BoardRect(3).Y * 320 + AdvertisingScreen.BoardRect(3).X + 5]);

        // Beim Blättern: Logo 35 × 25 aus kb\11 bei (100|179), Umrandung weiß (7, 0x2B719; Video 6:40).
        Click(d, AdvertisingScreen.ArrowDown);
        d.Frame();
        var logos = d.Host.Assets.Image(AdvertisingScreen.Logos);
        Assert.Equal(logos.Pixels[0], s.Pixels[179 * 320 + 100]);
        Assert.Equal(logos.Pixels[24 * logos.Width + 34], s.Pixels[203 * 320 + 134]);
        Assert.Equal(7, s.Pixels[190 * 320 + 99]);
        Click(d, AdvertisingScreen.BoardRect(3));
        d.Frame();

        // Schrift Farbe 0 ab x = 156 („Diese …“, Grundlinie 189).
        var rows = Enumerable.Range(180, 12);
        Assert.Contains(rows, y => s.Pixels[y * 320 + 156] == 0);
        Assert.DoesNotContain(rows, y => s.Pixels[y * 320 + 155] == 0);
    }
}
