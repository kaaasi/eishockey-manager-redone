using EishockeyManager.Core;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.ManagerArea;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>Bildschirm Werbung mit den Originaldateien (ohne „gamedata“ übersprungen).</summary>
public class AdvertisingScreenTests
{
    private static void Click(ScreenDriver d, Rect r) => d.Click(r.X + r.W / 2, r.Y + r.H / 2);

    [Fact]
    public void SelectBrowseAcceptAndShowInfo()
    {
        var d = FlowTests.CreateDriver();
        if (d is null)
            return;
        d.StartNewGame();
        d.OpenTile("Werbung");
        var screen = Assert.IsType<AdvertisingScreen>(d.Host.Current);
        var game = d.Host.Game!;
        var me = game.ActiveManager;

        // festes Angebot von CCH für die Banden
        game.Ads(me).Offers[AdSlotKind.Bande][1] = new SponsorOffer(AdSlotKind.Bande, 1, 12, 1, 3_454);

        Click(d, AdvertisingScreen.BoardRect(3));
        Assert.Equal(new AdSlot(AdSlotKind.Bande, 3), screen.SelectedSlot);
        Assert.Equal(-1, screen.Cursor);
        Click(d, AdvertisingScreen.ArrowDown);
        Click(d, AdvertisingScreen.ArrowDown);
        Assert.Equal(1, screen.Cursor);
        Assert.NotNull(screen.CurrentOffer());

        var income = game.MonthlyIncome(me);
        Click(d, AdvertisingScreen.AcceptButton);
        Assert.Equal(income + 3_454, game.MonthlyIncome(me));
        Assert.Null(screen.CurrentOffer());

        Click(d, AdvertisingScreen.InfoButton);
        Assert.True(screen.ShowsInfo);
        d.Click(10, 10);
        Assert.False(screen.ShowsInfo);

        Click(d, AdvertisingScreen.TvPanel);
        Assert.Equal(AdSlotKind.Fernsehen, screen.SelectedSlot?.Kind);
        Assert.Equal(-1, screen.Cursor);                         // eigener Zeiger für die Sender
        Click(d, AdvertisingScreen.ArrowUp);
        Assert.Equal(Sponsor.Stations.Length - 1, screen.Cursor);

        d.Press(Key.Escape);
        Assert.IsType<MainMenuScreen>(d.Host.Current);
    }
}
