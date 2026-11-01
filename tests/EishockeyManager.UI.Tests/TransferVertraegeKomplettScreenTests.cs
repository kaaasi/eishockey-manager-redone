using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Team;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur <c>transfer-vertraege-komplett</c>, Teil 1 über die Oberfläche: Verhandlungsfenster nach einem angenommenen Kauf
/// (em.exe 0x4786A, y = 0) und Verhandlungen ausgelaufener Verträge nach „Erhaltene Prämien“ (0x26F20, 0x27D91).
/// </summary>
public class TransferVertraegeKomplettScreenTests
{
    /// <summary>Kauf: Annahme → Verhandlungsfenster oben; „Abbruch“ → „Der Transfer findet nicht statt.“, nichts geändert.</summary>
    [GameDataFact]
    public void Buy_OpensContractDialog_CancelLeavesEverything()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        var manager = game.ActiveManager;
        var club = game.Clubs[manager.ClubIndex];
        manager.Money = 5_000_000;
        d.OpenTile("Transfer");
        var screen = Assert.IsType<TransferScreen>(d.Host.Current);
        var index = game.TransferMarket.Listings.ToList().FindIndex(l => l.Seller is null && !l.IsFreeAgent);
        var listing = game.TransferMarket.Listings[index];
        var buy = TransferScreen.EntryBuy(index);
        d.Click(buy.X + 5, buy.Y + 2);
        d.Click(TransferDialog.AmountField.X + 5, TransferDialog.AmountField.Y + 2);
        d.Type((listing.Price * 2).ToString());
        d.Press(Key.Enter);
        d.Press(Key.Enter); // „Kaufen“: über 130 % nimmt der Computermarkt immer an (0x4991D)
        var contract = Assert.IsType<ContractDialog>(screen.Contract);
        Assert.Equal(listing.Player, contract.Player);
        Assert.Equal(ContractsScreen.DialogRect(12), contract.Rect); // y = 0 (Datei 0x4785D)
        Assert.Equal(5_000_000, manager.Money);

        d.Click(contract.CancelButton.X + 5, contract.CancelButton.Y + 3);
        Assert.Null(screen.Contract);
        Assert.Equal(["Der Transfer", "findet nicht statt."], screen.Message.Lines);
        Assert.DoesNotContain(listing.Player, club.Players);
        Assert.Contains(listing, game.TransferMarket.Listings);
        Assert.Equal(5_000_000, manager.Money);
    }

    /// <summary>
    /// Saisonende: nach den Prämien das Verhandlungsfenster für den ausgelaufenen Vertrag (Lage nach der Zeile, 0x27D74);
    /// „Abbruch“ → „… kehrt Ihrem Verein den Rücken...“, „Sie erhalten eine Ablösesumme von … DM“, dann weiter wie bisher.
    /// Das Öffnen ändert nichts (Bildschirm zustandsrein).
    /// </summary>
    [GameDataFact]
    public void SeasonEnd_NegotiatesExpiredContract_ThenContinues()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        for (var guard = 0; game.Phase != SeasonPhase.SeasonOver; guard++)
        {
            Assert.True(guard < 40000, "Saisonende nicht erreicht");
            game.ActiveManager.Money = Math.Max(game.ActiveManager.Money, 5_000_000);
            d.Advance();
        }
        var manager = game.ActiveManager;
        var club = game.Clubs[manager.ClubIndex];
        foreach (var p in club.Players)
        {
            p.ContractYears = 3;
            p.Notice = ContractNotice.None;
        }
        var expiring = club.Players.First(p => p.Role == PlayerRole.Forward && !p.OnLoan);
        expiring.ContractYears = 1;

        ContractsScreen? contracts = null;
        for (var guard = 0; contracts is null; guard++)
        {
            Assert.True(guard < 200, "Verhandlung am Saisonende nicht erreicht");
            d.Press(Key.Enter);
            contracts = d.Host.Current as ContractsScreen;
        }
        Assert.True(contracts.SeasonEnd);
        Assert.Equal(expiring, contracts.Negotiating);
        var row = TeamUi.Rows(club).ToList().IndexOf(expiring);
        Assert.Equal(ContractsScreen.DialogRect(row), contracts.Dialog!.Rect);
        Assert.True(GameState.IsRenewalPending(expiring));
        var save = SaveGame.Save(game);
        d.Frame(_ => { });
        Assert.Equal(save, SaveGame.Save(game));

        var money = manager.Money;
        var cancel = contracts.Dialog.CancelButton;
        d.Click(cancel.X + 5, cancel.Y + 3);
        Assert.Equal([expiring.Name, "kehrt Ihrem Verein", "den Rücken..."], contracts.Message.Lines);
        d.Click(10, 10);
        Assert.Equal("Sie erhalten eine", contracts.Message.Lines[0]);
        Assert.True(manager.Money > money);
        Assert.DoesNotContain(expiring, club.Players);
        d.Click(10, 10);
        Assert.IsNotType<ContractsScreen>(d.Host.Current);
        Assert.False(game.HasPendingRenewals);
    }
}
