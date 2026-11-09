using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.UI.Screens.Team;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.UI.Tests;

/// <summary>PR35-02 aus docs/reviews/2026-10-26-pr35-review.md über die echten Knöpfe der Transferansicht.</summary>
public class WaveBReviewScreenTests
{
    [GameDataFact]
    public void PR3502_SecondBidBecameInvalid_ShowsMessageAndDoesNotReopenSameBid()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        var market = game.TransferMarket;
        var manager = game.ActiveManager;
        var club = game.Clubs[manager.ClubIndex];
        // A380 (kader-start Teil 2c): Mindestbesetzung greift beim Aufsetzen; sechs Verteidiger erlauben zwei Markt-Plätze.
        foreach (var extra in club.Players.Where(p => p.Role == PlayerRole.Defender).Skip(6).ToArray())
        {
            club.Players.Remove(extra);
            club.Lineup.Remove(extra);
        }
        Assert.Equal(6, club.Players.Count(p => p.Role == PlayerRole.Defender));
        foreach (var defender in club.Players.Where(p => p.Role == PlayerRole.Defender).Take(2).ToArray())
            Assert.Equal(TransferResult.Accepted, market.Offer(game, manager, defender));
        for (var i = 0; market.Bids.Count(b => b.Listing.Seller == manager) < 2 && i < 500; i++)
            market.AdvanceDay(game, game.Random);
        var bids = market.Bids.Where(b => b.Listing.Seller == manager).ToArray();
        Assert.Equal(2, bids.Length);

        d.OpenTile("Transfer");
        var screen = Assert.IsType<TransferScreen>(d.Host.Current);
        Assert.Same(bids[0], screen.Dialog!.Bid);
        var confirm = screen.Dialog.ConfirmButton;
        d.Click(confirm.X + 5, confirm.Y + 5);
        Assert.Same(bids[1], screen.Dialog!.Bid); // nächstes Gebot

        // Ungültig wird das zweite Gebot, weil der Spieler inzwischen in einem fremden Kader steht.
        var elsewhere = game.Clubs.First(c => c != club && c != bids[1].Buyer && c.Index < 52);
        elsewhere.Players.Add(bids[1].Listing.Player);
        var money = manager.Money;
        confirm = screen.Dialog.ConfirmButton;
        d.Click(confirm.X + 5, confirm.Y + 5);

        Assert.True(screen.Message.Visible, "Keine Rückmeldung zum abgelehnten Gebot");
        Assert.Equal(TransferMarket.Message(TransferResult.Impossible), screen.Message.Lines);
        Assert.Null(screen.Dialog); // nicht kommentarlos dasselbe Gebot erneut
        d.Frames(100); // Integration Runde 11 (ui-meldungen, K2): Kurzmeldung 0x0C0B8 ohne Knopf, schließt von selbst (Klick wirkt nicht)
        Assert.False(screen.Message.Visible);
        Assert.Null(screen.Dialog);
        Assert.DoesNotContain(bids[1], market.Bids);
        Assert.Equal(money, manager.Money);
        Assert.Contains(bids[1].Listing.Player, elsewhere.Players);
        Assert.DoesNotContain(bids[1].Listing.Player, bids[1].Buyer.Players);
    }
}
