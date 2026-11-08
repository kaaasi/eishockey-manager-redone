using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens.Team;

namespace EishockeyManager.UI.Tests;

/// <summary>ui-rest Teil 2 (A377, A374): Kauffenster nach DOSBox-Sitzung 2, <c>file40_08</c>.</summary>
public class TransferBuyWindowDosboxTests
{
    [GameDataFact]
    public void BuyWindow_IsLeftOverSquadList_WithoutButtons_AndSolidShadow()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        d.OpenTile("Transfer");
        var screen = Assert.IsType<TransferScreen>(d.Host.Current);
        var index = game.TransferMarket.Listings.ToList().FindIndex(l => l.Seller is null && !l.IsFreeAgent);
        var buy = TransferScreen.EntryBuy(index);
        d.Click(buy.X + 5, buy.Y + 2);
        Assert.Equal(TransferDialog.DialogKind.Buy, screen.Dialog?.Kind);
        d.Frame();
        Assert.Equal(new Rect(2, 29, 164, 42), TransferDialog.BuyBox);

        byte At(int x, int y) => d.Surface.Pixels[y * Surface.Width + x];
        // Füllung Index 4 innen, Schatten Index 6 rechts (x 166–167) und unten (y 71–72) bis (168 | 73)
        Assert.Equal(4, At(100, 65));
        Assert.Equal(6, At(167, 40));
        Assert.Equal(6, At(60, 72));
        Assert.NotEqual(6, At(100, 74));
        // keine Knöpfe
        Assert.Equal(new Rect(0, 0, 0, 0), screen.Dialog!.ConfirmButton);
    }
}
