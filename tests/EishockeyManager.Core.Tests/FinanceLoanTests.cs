using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>Kredite zwischen Managern, Kreditrahmen und Kursmodell (Bildschirm Finanzen).</summary>
public class FinanceLoanTests
{
    private static GameState TwoManagers()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Anna", Head = 0, ClubIndex = 2 });
        setup.Managers.Add(new ManagerSetup { Name = "Bernd", Head = 1, ClubIndex = 5 });
        return NewGame.Create(TestGames.CreateDatabase(), setup, 3);
    }

    [Fact]
    public void MaxWithdrawal_IsSavingsPlusFreeCredit()
    {
        var game = TestGames.Create();
        var m = game.ActiveManager;
        game.DepositToBank(m, 30_000);
        Assert.Equal(30_000 + FinanceRules.CreditLimit, game.MaxWithdrawal(m));
        game.WithdrawFromBank(m, 230_000);
        Assert.Equal(FinanceRules.CreditLimit - 200_000, game.MaxWithdrawal(m));
    }

    [Fact]
    public void PayToManager_RepaysDebtFirstThenLends()
    {
        var game = TwoManagers();
        var (a, b) = (game.Managers[0], game.Managers[1]);

        game.LendBetweenManagers(lender: 1, borrower: 0, 50_000);   // Anna schuldet Bernd 50.000
        Assert.Equal(50_000, game.ManagerDebts(a));
        Assert.Equal(50_000, game.ManagerReceivables(b));
        Assert.Equal(-50_000, game.ManagerLoanBalance(a));

        Assert.Equal(80_000, game.PayToManager(from: 0, to: 1, 80_000));
        Assert.Equal(0, game.LoanAmount(lender: 1, borrower: 0));
        Assert.Equal(30_000, game.LoanAmount(lender: 0, borrower: 1));
        Assert.Equal(FinanceRules.StartMoney(a.Level) + 50_000 - 80_000, a.Money);
        Assert.Equal(FinanceRules.StartMoney(b.Level) - 50_000 + 80_000, b.Money);
    }

    [Fact]
    public void ManagerLoanInterest_IsTwoAndAHalfPercentPerMonth()
    {
        var game = TwoManagers();
        game.LendBetweenManagers(lender: 0, borrower: 1, 40_000);
        Assert.Equal(1_000, game.ManagerLoanInterest(0, 1));
        Assert.Equal(-1_000, game.ManagerLoanInterest(1, 0));
    }

    [Fact]
    public void Stocks_MoveCalmlyFromDayToDay()
    {
        var market = StockMarket.Create(new Random(5));
        for (var day = 0; day < 300; day++)
            market.Advance(new Random(day));
        Assert.All(market.Stocks, s =>
        {
            Assert.InRange(s.Price, 5, 999);
            // Im Video ändern sich die Kurse meist um 0–2 DM am Tag; Sprünge sind selten.
            var steps = s.History.Zip(s.History.Skip(1), (x, y) => Math.Abs(y - x)).ToList();
            Assert.True(steps.Count(d => d > s.Price / 12) <= 5, $"{s.Name}: zu viele Sprünge");
        });
    }
}
