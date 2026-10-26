using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>Posteingang (Kreditanfragen, Vertragsverlängerung, Meldungen) und Jugendteams.</summary>
public class InboxYouthTests
{
    private static GameState TwoManagers()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Anna", Head = 0, ClubIndex = 2 });
        setup.Managers.Add(new ManagerSetup { Name = "Bernd", Head = 1, ClubIndex = 5 });
        return NewGame.Create(TestGames.CreateDatabase(), setup, 3);
    }

    private static void AdvanceToNextMonth(GameState game)
    {
        var day = game.FinanceDay + 1;
        while (SeasonCalendar.LayoutDate(day).Day != 1)
            day++;
        game.AdvanceFinanceTo(day);
    }

    [Fact]
    public void LoanRequest_NeedsConsentOfTheLender()
    {
        var game = TwoManagers();
        var (anna, bernd) = (game.Managers[0], game.Managers[1]);
        var request = game.RequestLoan(borrower: 0, lender: 1, 50_000)!;
        Assert.Same(request, Assert.Single(game.Inbox(bernd)));
        Assert.Equal(GameState.StartMoney, anna.Money);

        Assert.True(game.Answer(bernd, request, yes: true));
        Assert.Equal(GameState.StartMoney + 50_000, anna.Money);
        Assert.Equal(50_000, game.LoanAmount(lender: 1, borrower: 0));
        Assert.False(game.Answer(bernd, request, yes: true)); // nur einmal
    }

    [Fact]
    public void LoanRequest_ExpiresAfterAWeek()
    {
        var game = TwoManagers();
        var request = game.RequestLoan(0, 1, 10_000)!;
        game.AdvanceFinanceTo(game.FinanceDay + GameState.LoanRequestDays + 1);
        Assert.False(request.IsQuestion);
        Assert.False(game.Answer(game.Managers[1], request, yes: true));
        Assert.Equal(0, game.LoanAmount(1, 0));
    }

    [Fact]
    public void ExpiredContract_AsksOrDeclines_AndRenewalRestoresIncome()
    {
        var game = TestGames.Create();
        var me = game.ActiveManager;
        game.Ads(me).Offers[AdSlotKind.Trikot][3] = new SponsorOffer(AdSlotKind.Trikot, 3, 1, 1, 8_000);
        game.AcceptOffer(me, new AdSlot(AdSlotKind.Trikot), 3);
        AdvanceToNextMonth(game);

        var message = Assert.Single(game.Inbox(me));
        Assert.Contains("PC Games", string.Join(" ", message.Lines));
        if (message.Kind == MessageKind.SponsorRenewal)
        {
            Assert.True(game.Answer(me, message, yes: true));
            Assert.Equal(8_000, game.Ads(me).ContractAt(new AdSlot(AdSlotKind.Trikot))!.MonthlyAmount);
        }
        else
            Assert.Equal(["Die Firma PC Games", "wird den Vertrag nicht verlängern."], message.Lines);
    }

    [Fact]
    public void CompletedConstruction_IsReported()
    {
        var game = TestGames.Create();
        var me = game.ActiveManager;
        game.StartConstruction(me, StadiumProject.Feature, StadiumFeature.Anzeigetafel, 1);
        for (var i = 0; i < 10 && game.Finance(me).Stadium.Construction is not null; i++)
            TestSeason.PlayMatchDay(game);
        Assert.Contains(game.Inbox(me), m => m.Lines[0] == "Der Stadion-Ausbau (Anzeigetafel)");
    }

    [Fact]
    public void Youth_FundingIsRoundedPaidMonthlyAndCountsAsExpense()
    {
        var game = TestGames.Create();
        var me = game.ActiveManager;
        var expenses = game.MonthlyExpenses(me);
        game.SetYouthFunding(me, YouthTeamKind.Jugend, 7_400);
        game.SetYouthFunding(me, YouthTeamKind.Junioren, 25_000);
        Assert.Equal(7_000, game.Finance(me).Youth[1].Funding);
        Assert.Equal(10_000, game.Finance(me).Youth[2].Funding);
        Assert.Equal(expenses + 17_000, game.MonthlyExpenses(me));

        AdvanceToNextMonth(game);
        Assert.Equal(-17_000, game.Finance(me).Book.Current(FinanceCategory.Jugend));
    }

    [Fact]
    public void Youth_FullFundingClimbsOverTime()
    {
        var game = TestGames.Create();
        var me = game.ActiveManager;
        var team = game.Finance(me).Youth[0];
        Assert.Equal((3, 6), (team.League, team.Rank));
        game.SetYouthFunding(me, YouthTeamKind.Schueler, YouthTeam.MaxFunding);
        TestSeason.PlayRegularSeason(game);
        Assert.True(team.Position < 17, $"Platz {team.Rank}, Liga {team.League}");
    }

    [Fact]
    public void TalentSearch_CostsDiscountedPrice_AndReportsAfterSomeWeeks()
    {
        var game = TestGames.Create();
        var me = game.ActiveManager;
        game.SetYouthFunding(me, YouthTeamKind.Schueler, 10_000);
        game.SetYouthFunding(me, YouthTeamKind.Jugend, 5_000);
        Assert.Equal(30, game.TalentSearchDiscount(me));
        Assert.Equal(21_000, game.TalentSearchPrice(me));

        var squad = game.Clubs[me.ClubIndex].Players.Count;
        var money = me.Money;
        Assert.True(game.StartTalentSearch(me));
        Assert.False(game.StartTalentSearch(me)); // nur eine Suche gleichzeitig
        Assert.Equal(money - 21_000, me.Money);
        Assert.Equal(-21_000, game.Finance(me).Book.Current(FinanceCategory.Talentsuche));

        for (var i = 0; i < 12 && game.Finance(me).TalentSearch is not null; i++)
            TestSeason.PlayMatchDay(game);
        Assert.Null(game.Finance(me).TalentSearch);
        var report = Assert.Single(game.Inbox(me), m => m.Lines[0].StartsWith("Ihr"));
        if (report.Lines[0] == "Ihre Talentsuche war ein Erfolg.")
            Assert.Equal(squad + 1, game.Clubs[me.ClubIndex].Players.Count);
        else
            Assert.Equal(["Ihr Talentspäher gibt die", "Suche ohne Erfolg auf... Pech..."], report.Lines);
    }

    [Fact]
    public void TalentSearch_NotWithFullSquad()
    {
        var game = TestGames.Create();
        var me = game.ActiveManager;
        var club = game.Clubs[me.ClubIndex];
        while (club.Players.Count < GameState.MaxSquad)
            club.Players.Add(new Player { Name = "X", Role = Formats.PlayerRole.Forward });
        Assert.False(game.StartTalentSearch(me));
        Assert.Equal(GameState.StartMoney, me.Money);
    }
}
