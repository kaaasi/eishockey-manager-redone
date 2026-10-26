using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Regressionstest zu R2-01 (Folgereview 26.10.2026): Die Monatsabrechnung bucht die tatsächlichen Vertragsgehälter
/// und Versicherungsprämien – nicht die Pauschalformel. Die erwarteten Beträge werden hier aus den Einzelverträgen
/// summiert, nicht über dieselbe Funktion wie die Abrechnung.
/// </summary>
public class SalaryTests
{
    private static int MonthStarts(int fromExclusive, int toInclusive) =>
        Enumerable.Range(fromExclusive + 1, Math.Max(0, toInclusive - fromExclusive))
            .Count(d => d > 0 && SeasonCalendar.LayoutDate(d).Day == 1);

    /// <summary>Spult über <see cref="GameState.Continue"/> genau bis zur nächsten Monatsabrechnung.</summary>
    private static void ContinueToNextMonth(GameState game)
    {
        var months = 0;
        while (months == 0)
        {
            var before = game.FinanceDay;
            game.Continue();
            months += MonthStarts(before, game.FinanceDay);
        }
        Assert.Equal(1, months);
    }

    [Fact]
    public void ContractChange_IsBookedExactly_InNextMonth()
    {
        var game = TestGames.Create();
        var me = game.ActiveManager;
        var club = game.Clubs[me.ClubIndex];
        var book = game.Finance(me).Book;

        // Vertrag ändern wie im Bildschirm „Verträge“: höheres Gehalt, mit Versicherung
        var player = club.Players[0];
        var raise = Contracts.Demand(player) * 3;
        Assert.Equal(NegotiationResult.Accepted, Contracts.Offer(player, 4, raise, insured: true, matchDay: 0, new Random(1)));
        club.Players[1].Insured = true;

        long salaries = 0, insurance = 0;
        foreach (var p in club.Players)
        {
            salaries += p.MonthlySalary;
            if (p.Insured)
                insurance += p.MonthlySalary * Contracts.InsurancePercent / 100;
        }
        Assert.True(insurance > 0);

        var wagesBefore = book.Current(FinanceCategory.Gehaelter);
        var insuranceBefore = book.Current(FinanceCategory.Versicherung);
        var money = me.Money;
        var totalBefore = book.CurrentTotal;

        ContinueToNextMonth(game);

        Assert.Equal(-salaries, book.Current(FinanceCategory.Gehaelter) - wagesBefore);
        Assert.Equal(-insurance, book.Current(FinanceCategory.Versicherung) - insuranceBefore);
        Assert.Equal(money + (book.CurrentTotal - totalBefore), me.Money); // Vermögen genau um die Buchungen verändert
    }

    [Fact]
    public void MonthlyExpenses_ShowContractCosts()
    {
        var game = TestGames.Create();
        var me = game.ActiveManager;
        var club = game.Clubs[me.ClubIndex];
        var before = game.MonthlyExpenses(me);
        club.Players[0].MonthlySalary += 5_000;
        club.Players[0].Insured = true;
        Assert.Equal(before + 5_000 + club.Players[0].MonthlySalary / 10, game.MonthlyExpenses(me));
    }

    [Fact]
    public void FallbackFormula_OnlyForClubWithoutSquad()
    {
        var game = TestGames.Create();
        var club = game.Clubs[game.ActiveManager.ClubIndex];
        Assert.Equal(club.Players.Sum(p => (long)p.MonthlySalary), FinanceRules.MonthlyWages(club));

        club.Players.Clear();
        Assert.Equal(FinanceRules.MinimumSquad * FinanceRules.MonthlyWagePerPlayer(club.Strength), FinanceRules.MonthlyWages(club));
        Assert.Equal(0, FinanceRules.MonthlyInsurance(club));
    }
}
