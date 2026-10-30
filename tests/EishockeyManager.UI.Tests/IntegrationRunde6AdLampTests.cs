using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Review-Befund R6-R01 (Integration Runde 6, P2): Lampe 4 (freie Werbefläche, Spur pixel-verein) kannte nur eine
/// Eistafel; seit #133 (Spur finanzen-werbung) gibt es zwei mit eigener Freigabe. [ORIGINAL] em.exe Datei
/// 0x42C09–0x42C1E (Fläche 9, Überdachung HALLE) und 0x42C20–0x42C35 (Fläche 10, Kühlanlage MEGAFROST) prüfen jede
/// Tafel getrennt.
/// </summary>
public class IntegrationRunde6AdLampTests
{
    private static ScreenDriver WithAllOtherAdsTaken(int roof, int cooling, params AdSlotKind[] iceContracts)
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        var me = game.ActiveManager;
        var finance = game.Finance(me);
        finance.Stadium.SetLevel(StadiumFeature.Ueberdachung, roof);
        finance.Stadium.SetLevel(StadiumFeature.Kuehlanlage, cooling);

        var ads = game.Ads(me);
        void Take(AdSlot slot, int boards = 1)
        {
            ads.Offers[slot.Kind] = new() { [1] = new SponsorOffer(slot.Kind, 1, 12, boards, 1_000) };
            Assert.NotNull(game.AcceptOffer(me, slot, 1));
        }
        foreach (var board in ads.FreeBoards().ToArray())
            Take(new AdSlot(AdSlotKind.Bande, board));
        foreach (var kind in new[] { AdSlotKind.Trikot, AdSlotKind.Fernsehen })
            if (ads.ContractAt(new AdSlot(kind)) is null)
                Take(new AdSlot(kind));
        foreach (var kind in iceContracts)
            Take(new AdSlot(kind));
        d.Move(1, 239);
        return d;
    }

    [GameDataFact]
    public void BothBoardsAvailable_FirstTaken_SecondFree_IsRed()
    {
        var d = WithAllOtherAdsTaken(roof: 2, cooling: 2, AdSlotKind.Eisflaeche);
        Assert.True(PixelVereinTests.LampRed(d, 4));
    }

    [GameDataFact]
    public void OnlySecondBoardAvailable_AndTaken_IsGreen()
    {
        var d = WithAllOtherAdsTaken(roof: 0, cooling: 2, AdSlotKind.Eisflaeche2);
        Assert.False(PixelVereinTests.LampRed(d, 4));
    }

    [GameDataFact]
    public void BothBoardsTaken_IsGreen()
    {
        var d = WithAllOtherAdsTaken(roof: 2, cooling: 2, AdSlotKind.Eisflaeche, AdSlotKind.Eisflaeche2);
        Assert.False(PixelVereinTests.LampRed(d, 4));
    }
}
