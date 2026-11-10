using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens.Team;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur <c>kader-transfer</c>, Teil 3, über die echte Transferliste: nach einer Leihe vom Computermarkt fragt das Fenster
/// „Nehmen Sie einen Sponsor- / zuschuß von / … DM an ?“ (em.exe 0x063BE); „Aber immer!“ bucht den Betrag, danach die Meldung.
/// Vorher rot: kein Fragefenster.
/// </summary>
public class KaderTransferTeil3ScreenTests
{
    /// <remarks>
    /// Spur tagesablauf (#699 Punkt 2, R15-E1 = A): Der Zuschuss fällt nur bei random(0, 6) = 0 (Datei 0x063BE). Mit Tag 0 vor dem
    /// ersten Menü (Listenwechsel-Wurf, anderer Zufallsverlauf) reichen die Einträge der ersten Liste mit Saat 1993 nicht mehr bis
    /// zu einem Zuschuss; der Test probiert deshalb nacheinander mehrere Saaten und prüft den Ablauf beim ersten Zuschuss.
    /// </remarks>
    [GameDataFact]
    public void ComputerMarketLoan_AsksForSubsidy_AcceptBooks()
    {
        foreach (var seed in new[] { 1993, 1994, 1995, 1996, 1997 })
            if (TryLoanWithSubsidy(seed))
                return;
        Assert.Fail("kein Zuschuss angeboten");
    }

    private static bool TryLoanWithSubsidy(int seed)
    {
        var d = FlowTests.CreateDriver(seed);
        d.StartNewGame();
        var game = d.Host.Game!;
        var manager = game.ActiveManager;
        manager.Money = 100_000_000;
        d.OpenTile("Transfer");
        var screen = Assert.IsType<TransferScreen>(d.Host.Current);

        for (var attempt = 0; attempt < 200; attempt++)
        {
            var listings = game.TransferMarket.Listings;
            var i = Enumerable.Range(0, listings.Count).FirstOrDefault(k => listings[k].Seller is null
                && !listings[k].IsFreeAgent && listings[k].LoanFee > 0 && !TransferMarket.IsLocked(game, manager, listings[k]), -1);
            if (i < 0)
                return false; // kein Eintrag mehr
            var loan = TransferScreen.EntryLoan(i);
            d.Click(loan.X + 5, loan.Y + 2);
            Assert.Equal(TransferDialog.DialogKind.Loan, screen.Dialog?.Kind);
            // Gebot über 130 % der Leihgebühr: immer angenommen (0x4991D); Kader klein halten.
            var field = TransferDialog.AmountField;
            d.Click(field.X + 5, field.Y + 2);
            d.Type((listings[i].LoanFee * 2).ToString(System.Globalization.CultureInfo.InvariantCulture));
            d.Press(Key.Enter);
            d.Press(Key.Enter);
            var club = game.Clubs[manager.ClubIndex];
            if (club.Players.FirstOrDefault(p => p.OnLoan) is { } loaned)
            {
                club.Players.Remove(loaned);
                club.Lineup.Remove(loaned);
            }
            if (screen.Dialog is { Kind: TransferDialog.DialogKind.Subsidy } question)
            {
                var subsidy = question.Subsidy!;
                Assert.Same(manager, subsidy.Manager);
                Assert.Equal(subsidy.Amount, question.Price);
                Assert.True(subsidy.Amount > 0);
                Assert.True(question.IsQuestion);
                var money = manager.Money;
                d.Click(question.ConfirmButton.X + 5, question.ConfirmButton.Y + 5); // „Aber immer!“
                Assert.Null(screen.Dialog);
                Assert.Equal(money + subsidy.Amount, manager.Money);
                Assert.Equal(TransferMarket.Message(TransferResult.Accepted), screen.Message.Lines);
                return true;
            }
            d.Click(10, 10); // Meldung schließen
        }
        return false;
    }
}
