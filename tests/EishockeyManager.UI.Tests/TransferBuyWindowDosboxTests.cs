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
        d.Frame();
        var before = (byte[])d.Surface.Pixels.Clone();
        var index = game.TransferMarket.Listings.ToList().FindIndex(l => l.Seller is null && !l.IsFreeAgent);
        var buy = TransferScreen.EntryBuy(index);
        d.Click(buy.X + 5, buy.Y + 2);
        Assert.Equal(TransferDialog.DialogKind.Buy, screen.Dialog?.Kind);
        d.Frame();
        Assert.Equal(new Rect(2, 29, 165, 43), TransferDialog.BuyBox);

        byte At(int x, int y) => d.Surface.Pixels[y * Surface.Width + x];
        // Füllung Index 4 innen, Schatten Index 6 rechts (x 166–167) und unten (y 71–72) bis (168 | 73)
        Assert.Equal(4, At(100, 65));
        Assert.Equal(6, At(167, 40));
        Assert.Equal(6, At(60, 72));
        Assert.NotEqual(6, At(100, 74));
        // R14UR-04: Kante x 166 / y 71 (dunkel, Index 29), Schatten bis (168 | 73); `file40_08`
        Assert.Equal(29, At(166, 40));
        Assert.Equal(4, At(165, 40));
        Assert.Equal(6, At(167, 40));
        Assert.Equal(29, At(100, 71));
        Assert.Equal(4, At(100, 70));
        Assert.Equal(6, At(100, 72));
        Assert.Equal(6, At(168, 73));
        Assert.Equal(6, At(166, 73));
        Assert.Equal(6, At(168, 40));
        Assert.NotEqual(6, At(169, 40));
        Assert.NotEqual(6, At(100, 74));
        // #663: obere und linke Linie (36 | 44 | 48) = Index 1; Ecken (2 | 29), (166 | 29), (2 | 71) in Füllfarbe, (166 | 71) schwarz
        Assert.Equal(1, At(100, 29));
        Assert.Equal(1, At(2, 50));
        Assert.Equal(4, At(2, 29));
        Assert.Equal(4, At(166, 29));
        Assert.Equal(4, At(2, 71));
        Assert.Equal(0, At(166, 71));
        // keine Knöpfe – auch nicht in der linken oberen Ecke (R14UR-01): Bereich (0 | 0)–(60 | 12) wie ohne Fenster
        Assert.Equal(new Rect(0, 0, 0, 0), screen.Dialog!.ConfirmButton);
        for (var y = 0; y <= 12; y++)
            for (var x = 0; x <= 60; x++)
                Assert.True(before[y * Surface.Width + x] == At(x, y), $"Pixel ({x} | {y}) weicht vom Hintergrund ab");
    }
}
