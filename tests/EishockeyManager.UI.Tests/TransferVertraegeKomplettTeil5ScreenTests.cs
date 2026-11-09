using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens.Team;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur <c>transfer-vertraege-komplett</c>, Teil 5 über die Oberfläche: Ein Gebot für einen eigenen Spieler auf dem Markt
/// erscheint im Angebotsfenster 0x491F9 mit Argument 0 (Aufruf 0x473FF): „Behalten“ / „Verkaufen“, unter „ABLÖSESUMME :“ der
/// Rest Angebot − Angebot / 7 · Restlaufzeit (0x49332), der beim Verkauf gutgeschrieben wird. Vorher eine Rückfrage mit vollem
/// Betrag.
/// </summary>
public class TransferVertraegeKomplettTeil5ScreenTests
{
    private static (ScreenDriver Driver, TransferScreen Screen, TransferBid Bid) OpenBid()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        var manager = game.ActiveManager;
        var club = game.Clubs[manager.ClubIndex];
        var player = club.Players.OrderByDescending(p => p.Condition + p.Technique)
            .First(p => p.Role == PlayerRole.Forward && TransferMarket.CanLeave(club, p));
        player.ContractYears = 2;
        Assert.Equal(TransferResult.Accepted, game.TransferMarket.Offer(game, manager, player));
        for (var day = 0; day < 300 && game.TransferMarket.Bids.Count == 0; day++)
            game.TransferMarket.AdvanceDay(game, game.Random);
        var bid = Assert.Single(game.TransferMarket.Bids);
        d.OpenTile("Transfer");
        var screen = Assert.IsType<TransferScreen>(d.Host.Current);
        return (d, screen, bid);
    }

    [GameDataFact]
    public void MarketBid_OpensOfferWindow_SellCreditsFee()
    {
        var (d, screen, bid) = OpenBid();
        var game = d.Host.Game!;
        var manager = game.ActiveManager;
        var dialog = Assert.IsType<TransferDialog>(screen.Dialog);
        Assert.Equal(TransferDialog.DialogKind.Bid, dialog.Kind);
        Assert.True(dialog.IsOfferWindow);
        Assert.False(dialog.IsQuestion);
        Assert.Equal(0x0B, dialog.CancelButton.X); // „Behalten“ fest aus dem Code (R14KS-08: nicht OfferBox.X + 10) (0x0B | 0x53)
        Assert.Equal(90, dialog.ConfirmButton.X); // „Verkaufen“ (0x5A | 0x53)
        var fee = TransferMarket.TransferFee(bid);
        Assert.Equal((TransferRules.Thirds(bid.Amount) - TransferRules.Thirds(bid.Amount) / 7 * 2) / 3, fee);
        d.Frame(); // zeichnet das Fenster mit Ablösesumme
        var money = manager.Money;
        d.Click(dialog.ConfirmButton.X + 5, dialog.ConfirmButton.Y + 3);
        Assert.Equal(money + fee, manager.Money);
        Assert.Contains(bid.Listing.Player, bid.Buyer.Players);
    }

    [GameDataFact]
    public void MarketBid_KeepDeclines()
    {
        var (d, screen, bid) = OpenBid();
        var game = d.Host.Game!;
        var money = game.ActiveManager.Money;
        var dialog = screen.Dialog!;
        d.Click(dialog.CancelButton.X + 5, dialog.CancelButton.Y + 3);
        Assert.Equal(money, game.ActiveManager.Money);
        Assert.Empty(game.TransferMarket.Bids);
        // A380: „Behalten“ lehnt nur das Gebot ab (Datei 0x47433); der Spieler bleibt auf der Liste, außerhalb des Kaders.
        Assert.DoesNotContain(bid.Listing.Player, game.Clubs[game.ActiveManager.ClubIndex].Players);
        Assert.Same(bid.Listing, game.TransferMarket.ListingOf(bid.Listing.Player));
    }
}
