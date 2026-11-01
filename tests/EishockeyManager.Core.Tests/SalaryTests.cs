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
        var raise = player.MonthlySalary * 3; // Startgehalt = Wert(Gehalt) (0x29EE4), höher als die Forderung
        // Gleiche Laufzeit, höheres Gehalt: im Original ohne Zufall angenommen (0x4AC1A).
        Assert.Equal(NegotiationResult.Accepted, Contracts.Negotiate(game, me, player, 0, raise, insured: true, demandFilled: false));
        club.Players[1].Insured = true;

        long salaries = 0, insurance = 0;
        foreach (var p in club.Players)
        {
            salaries += p.MonthlySalary;
            if (p.Insured)
                insurance += p.InsurancePremium; // Spur transfer-vertraege-komplett: Prämie +0x30 statt 10 % des Gehalts
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
    public void Wages_AreContractSums_NoLumpSumWithoutSquad()
    {
        // Spur finanzen-2: Die Ersatzpauschale für leere Kader gibt es im Original nicht (em.exe 0x3E8DC).
        var game = TestGames.Create();
        var club = game.Clubs[game.ActiveManager.ClubIndex];
        Assert.Equal(club.Players.Sum(p => (long)p.MonthlySalary), FinanceRules.MonthlyWages(club));

        club.Players.Clear();
        Assert.Equal(0, FinanceRules.MonthlyWages(club));
        Assert.Equal(0, FinanceRules.MonthlyInsurance(club));
    }
}
