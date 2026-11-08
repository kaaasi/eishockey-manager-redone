using System.Text;
using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Team;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur kader-start, Teil 2c (A380) über die echte Transferliste: Der eigene Spieler auf der Liste steht nicht mehr in der
/// Kaderliste links; ein Klick auf den eigenen Markt-Platz ohne Gebot holt ihn zurück (em.exe Datei 0x47331 → 0x477A9).
/// </summary>
public class KaderStartTeil2cScreenTests
{
    private static (GameState Game, TransferScreen Screen, Player Player) Open()
    {
        var data = new byte[ClubDatabase.FileLength];
        for (var i = 0; i < ClubDatabase.ClubsWithSquad; i++)
        {
            Encoding.ASCII.GetBytes($"VEREIN {i}").CopyTo(data, ClubDatabase.SquadOffset + i * ClubDatabase.SquadRecordLength);
            data[ClubDatabase.StrengthOffset + i * 3] = (byte)(100 - i % 12 * 5);
        }
        var setup = new GameSetup { DebCup = false };
        setup.Managers.Add(new ManagerSetup { Name = "JOERG", Head = 0, ClubIndex = 2 });
        var game = NewGame.Create(ClubDatabase.Parse(data), setup, 5);
        var manager = game.Managers[0];
        var club = game.Clubs[manager.ClubIndex];
        var player = club.Players.First(p => p.Role == PlayerRole.Forward && !p.OnLoan && TransferMarket.CanLeave(club, p));
        var host = new ScreenHost(null!) { Game = game };
        return (game, new TransferScreen(host, new MainMenuScreen(host)), player);
    }

    private static void Click(Screen screen, Rect r) =>
        screen.Update(new InputState { MouseX = r.X + 3, MouseY = r.Y + 3, LeftClick = true });

    [Fact]
    public void OwnListingClick_WithoutBid_ReturnsPlayerToSquad()
    {
        var (game, screen, player) = Open();
        var manager = game.Managers[0];
        var club = game.Clubs[manager.ClubIndex];
        Assert.Equal(TransferResult.Accepted, game.TransferMarket.Offer(game, manager, player));
        Assert.DoesNotContain(player, TeamUi.Rows(club));
        var i = game.TransferMarket.Listings.ToList().FindIndex(l => l.Player == player);

        Click(screen, TransferScreen.EntryBuy(i));

        Assert.Null(screen.Dialog);
        Assert.Null(game.TransferMarket.ListingOf(player));
        Assert.Contains(player, club.Players);
    }

    [Fact]
    public void OwnListingClick_WithBid_OpensBidWindow()
    {
        var (game, screen, player) = Open();
        var manager = game.Managers[0];
        Assert.Equal(TransferResult.Accepted, game.TransferMarket.Offer(game, manager, player));
        for (var d = 0; game.TransferMarket.Bids.Count == 0 && d < 500; d++)
            game.TransferMarket.AdvanceDay(game, game.Random);
        Assert.NotEmpty(game.TransferMarket.Bids);
        var i = game.TransferMarket.Listings.ToList().FindIndex(l => l.Player == player);

        Click(screen, TransferScreen.EntryBuy(i));

        Assert.Equal(TransferDialog.DialogKind.Bid, screen.Dialog?.Kind);
        Assert.NotNull(game.TransferMarket.ListingOf(player));
    }
}
