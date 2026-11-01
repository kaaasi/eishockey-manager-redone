using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens.Team;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur <c>transfer-vertraege-komplett</c>, Teil 3 über die Oberfläche: Anklicken eines Spielers mit unaufgefordertem Angebot
/// im eigenen Kader der Transferliste öffnet das Angebotsfenster 0x491F9 („Behalten“ / „Verkaufen“, Ablösesumme „keine“);
/// Knöpfe unter der gedrückten Maustaste erscheinen gedrückt (0x4BEBD).
/// </summary>
public class TransferVertraegeKomplettTeil3ScreenTests
{
    private static (ScreenDriver Driver, TransferScreen Screen, Player Player, Club Buyer) OpenOffer()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        var club = game.Clubs[game.ActiveManager.ClubIndex];
        var buyer = game.Clubs.First(c => c.Index < 43 && c.Players.Count > 0 && game.Managers.All(m => m.ClubIndex != c.Index));
        var rows = TeamUi.Rows(club);
        var player = rows.First(p => !p.OnLoan);
        player.UnsolicitedOffer = buyer;
        d.OpenTile("Transfer");
        var screen = Assert.IsType<TransferScreen>(d.Host.Current);
        Assert.Null(screen.Dialog); // Öffnen des Bildschirms ändert nichts
        Assert.Same(buyer, player.UnsolicitedOffer);
        var row = rows.ToList().IndexOf(player);
        d.Click(20, TransferScreen.FirstRowY + row * TransferScreen.RowPitch + 1);
        return (d, screen, player, buyer);
    }

    /// <summary>„Verkaufen“: Betrag ohne Abzug gutgeschrieben, Spieler wechselt zum bietenden Verein.</summary>
    [GameDataFact]
    public void ClickOnOfferedPlayer_OpensOfferWindow_SellMovesPlayer()
    {
        var (d, screen, player, buyer) = OpenOffer();
        var game = d.Host.Game!;
        var manager = game.ActiveManager;
        var dialog = Assert.IsType<TransferDialog>(screen.Dialog);
        Assert.Equal(TransferDialog.DialogKind.Offer, dialog.Kind);
        Assert.Same(buyer, dialog.Offer!.Buyer);
        Assert.Null(player.UnsolicitedOffer); // Bit 0x40 beim Anklicken gelöscht (0x46EB1)
        Assert.Equal(TransferDialog.OfferBox.X + 10, dialog.CancelButton.X); // „Behalten“ links (0x0B | 0x53)
        Assert.Equal(90, dialog.ConfirmButton.X); // „Verkaufen“ rechts (0x5A | 0x53)
        var money = manager.Money;
        d.Click(dialog.ConfirmButton.X + 5, dialog.ConfirmButton.Y + 3);
        Assert.Null(screen.Dialog);
        Assert.Equal(money + dialog.Offer.Amount, manager.Money);
        Assert.Contains(player, buyer.Players);
        Assert.DoesNotContain(player, game.Clubs[manager.ClubIndex].Players);
    }

    /// <summary>„Behalten“: Spieler bleibt, kein Geld, das Angebot ist verfallen.</summary>
    [GameDataFact]
    public void KeepButton_KeepsPlayer()
    {
        var (d, screen, player, _) = OpenOffer();
        var game = d.Host.Game!;
        var money = game.ActiveManager.Money;
        var dialog = screen.Dialog!;
        d.Click(dialog.CancelButton.X + 5, dialog.CancelButton.Y + 3);
        Assert.Null(screen.Dialog);
        Assert.Equal(money, game.ActiveManager.Money);
        Assert.Contains(player, game.Clubs[game.ActiveManager.ClubIndex].Players);
        Assert.Null(player.UnsolicitedOffer);
    }

    /// <summary>Gedrückte Maustaste über einem Knopf: Zustand „gedrückt“ (kb\2 Zeile 191), sonst normal.</summary>
    [Fact]
    public void PressedAt_ReportsButtonUnderHeldMouse()
    {
        var a = Widgets.ButtonRect(11, 80);
        var b = Widgets.ButtonRect(90, 80);
        Assert.Equal(b, ContractDialog.PressedAt(new InputState { LeftDown = true, MouseX = 95, MouseY = 85 }, a, b));
        Assert.Null(ContractDialog.PressedAt(new InputState { LeftDown = false, MouseX = 95, MouseY = 85 }, a, b));
        Assert.Null(ContractDialog.PressedAt(new InputState { LeftDown = true, MouseX = 70, MouseY = 85 }, a, b));
    }
}
