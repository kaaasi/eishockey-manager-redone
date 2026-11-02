using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens.Team;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur <c>transfer-vertraege-komplett</c>, Teil 4: Pixelvergleich der Transferliste. Der Eintrag unter der Maus wird im
/// Original durch dunkelrote Trennlinien über und unter dem Eintrag markiert (`oberflaeche_1100`, `oberflaeche_1102`). Vorher
/// rot: Das Remake zeichnete eine hellrote Linie (17) eine Zeile über dem Namen, die Trennlinien blieben hell (93).
/// </summary>
public class TransferVertraegeKomplettTeil4ScreenTests
{
    [GameDataFact]
    public void HoveredEntry_RecolorsSeparatorsAboveAndBelow()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        d.OpenTile("Transfer");
        var screen = Assert.IsType<TransferScreen>(d.Host.Current);
        var game = d.Host.Game!;
        Assert.True(game.TransferMarket.Listings.Count >= 3);
        const int entry = 1;
        var y = TransferScreen.FirstEntryY + TransferScreen.EntryPitch * entry;
        d.Move(250, y + 2);
        Assert.Null(screen.Dialog);

        byte At(int px, int py) => d.Surface.Pixels[py * Surface.Width + px];
        // #347 (R9T-P3-01): Sollwert als Zahl, nicht die Konstante des Bildschirms – Index 201 nach `oberflaeche_1100`/`1102`.
        const byte hover = 201;
        Assert.Equal(hover, TransferScreen.HoverEntryColor);
        foreach (var x in new[] { TransferScreen.ListLeft, 250, TransferScreen.ListRight })
        {
            Assert.Equal(hover, At(x, y - 4));
            Assert.Equal(hover, At(x, y + 12));
            Assert.Equal(198, At(x, y - 3)); // zweite Zeile der Trennlinie unverändert
            Assert.Equal(198, At(x, y + 13));
            Assert.NotEqual(17, At(x, y - 1)); // keine hellrote Linie über dem Namen
            Assert.Equal(93, At(x, y - 4 - TransferScreen.EntryPitch)); // Linie über dem vorigen Eintrag bleibt hell
            Assert.Equal(93, At(x, y + 12 + TransferScreen.EntryPitch));
        }
    }
}
