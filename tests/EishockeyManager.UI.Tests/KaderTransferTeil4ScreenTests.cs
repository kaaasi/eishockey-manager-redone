using System.Text;
using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Team;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur <c>kader-transfer</c>, Teil 4 (A258), über die echte Transferliste: Ein Gebot auf den Spieler eines anderen Managers
/// öffnet die Frage an den Verkäufer (em.exe 0x47655–0x47743: „Nehmen Sie das Angebot (… DM)“ / „für … an ?“, „Na gut.“ /
/// „Nein !“). „Nein !“ meldet „Ihr Angebot wurde abgelehnt.“ und sperrt den Platz, „Na gut.“ leiht bzw. öffnet die Verhandlung.
/// Vorher rot: kein Fragefenster, die Antwort wurde gewürfelt.
/// </summary>
public class KaderTransferTeil4ScreenTests
{
    private static (GameState Game, TransferScreen Screen, int Index) Open(int seed = 5)
    {
        var data = new byte[ClubDatabase.FileLength];
        for (var i = 0; i < ClubDatabase.ClubsWithSquad; i++)
        {
            Encoding.ASCII.GetBytes($"VEREIN {i}").CopyTo(data, ClubDatabase.SquadOffset + i * ClubDatabase.SquadRecordLength);
            data[ClubDatabase.StrengthOffset + i * 3] = (byte)(100 - i % 12 * 5);
        }
        var setup = new GameSetup { DebCup = false };
        setup.Managers.Add(new ManagerSetup { Name = "KAUF", Head = 0, ClubIndex = 2 });
        setup.Managers.Add(new ManagerSetup { Name = "VERKAUF", Head = 1, ClubIndex = 7 });
        var game = NewGame.Create(ClubDatabase.Parse(data), setup, seed);
        var seller = game.Managers[1];
        var player = game.Clubs[seller.ClubIndex].Players.First(p => p.Role == PlayerRole.Forward && !p.OnLoan);
        Assert.Equal(TransferResult.Accepted, game.TransferMarket.Offer(game, seller, player));
        game.Managers[0].Money = 50_000_000;
        var host = new ScreenHost(null!) { Game = game };
        var screen = new TransferScreen(host, new MainMenuScreen(host));
        var index = game.TransferMarket.Listings.ToList().FindIndex(l => l.Seller == seller);
        return (game, screen, index);
    }

    private static void Click(Screen screen, Rect r) => screen.Update(new InputState { MouseX = r.X + 3, MouseY = r.Y + 3, LeftClick = true });

    private static void Enter(Screen screen)
    {
        var input = new InputState();
        input.Keys.Add(Key.Enter);
        screen.Update(input);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(true, false)]
    public void BidOnManagerListing_AsksSeller(bool accept, bool loan)
    {
        var (game, screen, i) = Open();
        var (buyer, seller) = (game.Managers[0], game.Managers[1]);
        var listing = game.TransferMarket.Listings[i];
        var money = buyer.Money;
        Click(screen, loan ? TransferScreen.EntryLoan(i) : TransferScreen.EntryBuy(i));
        Assert.Equal(loan ? TransferDialog.DialogKind.Loan : TransferDialog.DialogKind.Buy, screen.Dialog?.Kind);
        Enter(screen); // „Mach' ich!“ mit dem geforderten Betrag

        var question = screen.Dialog;
        Assert.NotNull(question);
        Assert.Equal(TransferDialog.DialogKind.Seller, question!.Kind);
        Assert.True(question.IsQuestion);
        Assert.Same(seller, question.Question!.Seller);
        Assert.Equal(loan ? listing.LoanFee : listing.Price, question.Price);
        Assert.Equal(money, buyer.Money);
        Assert.Contains(listing, game.TransferMarket.Listings);

        Click(screen, accept ? question.ConfirmButton : question.CancelButton);
        if (!accept)
        {
            Assert.Null(screen.Dialog);
            Assert.Equal(TransferMarket.Message(TransferResult.Rejected), screen.Message.Lines);
            Assert.True(TransferMarket.IsLocked(game, buyer, listing));
            return;
        }
        if (loan)
        {
            Assert.Equal(TransferMarket.Message(TransferResult.Accepted), screen.Message.Lines);
            Assert.Equal(money - listing.LoanFee, buyer.Money);
            Assert.Contains(listing.Player, game.Clubs[buyer.ClubIndex].Players);
        }
        else
            Assert.NotNull(screen.Contract); // Verhandlung des neuen Kaderplatzes (0x4785D)
    }
}
