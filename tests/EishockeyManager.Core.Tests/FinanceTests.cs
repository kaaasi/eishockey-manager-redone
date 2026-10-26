using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

public class FinanceTests
{
    [Fact]
    public void Bank_WithdrawUsesSavingsFirstThenCreditUpToLimit()
    {
        var game = TestGames.Create();
        var m = game.ActiveManager;
        var bank = game.Finance(m).Bank;

        Assert.Equal(100_000, game.DepositToBank(m, 100_000));
        Assert.Equal(100_000, bank.Savings);
        Assert.Equal(GameState.StartMoney - 100_000, m.Money);

        Assert.Equal(150_000, game.WithdrawFromBank(m, 150_000));
        Assert.Equal(0, bank.Savings);
        Assert.Equal(50_000, bank.Credit);

        var granted = game.WithdrawFromBank(m, 5_000_000);
        Assert.Equal(FinanceRules.CreditLimit - 50_000, granted);
        Assert.Equal(FinanceRules.CreditLimit, bank.Credit);
    }

    [Fact]
    public void Bank_DepositRepaysCreditFirstAndNeverOverdraws()
    {
        var game = TestGames.Create();
        var m = game.ActiveManager;
        var bank = game.Finance(m).Bank;
        game.WithdrawFromBank(m, 200_000);

        game.DepositToBank(m, 250_000);
        Assert.Equal(0, bank.Credit);
        Assert.Equal(50_000, bank.Savings);

        Assert.Equal(m.Money, game.DepositToBank(m, 10_000_000));
        Assert.Equal(0, m.Money);
    }

    [Fact]
    public void Bank_MonthlyInterest_FourPercentCreditOnePercentSavings()
    {
        var bank = new BankAccount { Credit = 100_000, Savings = 50_000 };
        Assert.Equal(500 - 4_000, bank.MonthlyInterest);
    }

    [Fact]
    public void Stocks_BuyAndSell_UpdateDepotAndBook()
    {
        var game = TestGames.Create();
        var m = game.ActiveManager;
        var stock = game.Stocks.Stocks[0];
        var price = stock.Price;

        Assert.Equal(10, game.BuyStock(m, 0, 10));
        Assert.Equal(10L * price, game.DepotPurchaseCost(m));
        Assert.Equal(GameState.StartMoney - 10L * price, m.Money);

        stock.Price = price + 10;
        Assert.Equal(10L * (price + 10), game.DepotValue(m));
        Assert.Equal(4, game.SellStock(m, 0, 4));
        Assert.Equal(40, game.Finance(m).Book.Current(FinanceCategory.Aktien));
        Assert.Equal(6, game.Finance(m).Holdings[0].Count);
    }

    [Fact]
    public void Stocks_HistoryIsFilledAtStartAndEndsAtStartPrice()
    {
        var market = StockMarket.Create(new Random(1));
        Assert.Equal(4, market.Stocks.Count);
        Assert.All(market.Stocks, s =>
        {
            Assert.Equal(Stock.HistoryLength, s.History.Count);
            Assert.Equal(s.Price, s.History[^1]);
        });
        Assert.Equal([101, 120, 79, 174], market.Stocks.Select(s => s.Price));
    }

    [Fact]
    public void Season_MoneyChangesPlausibly()
    {
        var game = TestGames.Create();
        var m = game.ActiveManager;
        TestSeason.PlayRegularSeason(game);

        var book = game.Finance(m).Book;
        Assert.True(book.Current(FinanceCategory.Eintritt) > 0);
        Assert.True(book.Current(FinanceCategory.Gehaelter) < 0);
        Assert.True(book.Current(FinanceCategory.Unterhalt) < 0);
        Assert.Equal(GameState.StartMoney + book.CurrentTotal, m.Money);
        // Kein Absturz in den Ruin und keine Geldschwemme innerhalb einer Hauptrunde
        Assert.InRange(m.Money, -1_500_000, 3_000_000);
        Assert.NotNull(game.Finance(m).LastHomeGame);
    }

    [Fact]
    public void Season_SalariesArePaidMonthly()
    {
        var game = TestGames.Create();
        var m = game.ActiveManager;
        TestSeason.PlayRegularSeason(game);

        // Summe der Einzelverträge (nicht dieselbe Funktion wie die Abrechnung, siehe SalaryTests)
        var wages = game.Clubs[m.ClubIndex].Players.Sum(p => (long)p.MonthlySalary);
        var months = Enumerable.Range(1, game.Day).Count(d => SeasonCalendar.LayoutDate(d).Day == 1);
        Assert.Equal(-wages * months, game.Finance(m).Book.Current(FinanceCategory.Gehaelter));
    }

    [Fact]
    public void Finances_DoNotChangeMatchResults()
    {
        var a = TestGames.Create(seed: 9);
        var b = TestGames.Create(seed: 9);
        _ = b.Stocks;
        b.BuyStock(b.ActiveManager, 2, 5);
        for (var i = 0; i < 10; i++)
        {
            TestSeason.PlayMatchDay(a);
            TestSeason.PlayMatchDay(b);
        }
        Assert.Equal(a.Results, b.Results);
    }

    [Fact]
    public void Attendance_HigherTicketPriceMeansFewerSpectators()
    {
        var game = TestGames.Create();
        var stadium = new Stadium { Seats = 15_000, Standing = 15_000 };
        var home = game.Clubs[2];
        var away = game.Clubs[3];
        stadium.TicketPrice = 8;
        var cheap = game.Spectators(home, away, stadium, 1, new Random(1));
        stadium.TicketPrice = 30;
        var expensive = game.Spectators(home, away, stadium, 1, new Random(1));
        Assert.True(cheap > expensive);
    }
}
