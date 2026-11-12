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
        Assert.Equal(FinanceRules.StartMoney(anna.Level), anna.Money);

        Assert.True(game.Answer(bernd, request, yes: true));
        Assert.Equal(FinanceRules.StartMoney(anna.Level) + 50_000, anna.Money);
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

    /// <summary>
    /// Spur finanzen-werbung (#133): Ein abgelaufener Vertrag ohne Option endet still (em.exe 0x3EFB2); die frühere
    /// Absage-Meldung des Remakes entfällt. Mit Option siehe <see cref="SponsorOfferTests"/>.
    /// </summary>
    [Fact]
    public void ExpiredContract_WithoutOption_EndsSilently()
    {
        var game = TestGames.Create();
        var me = game.ActiveManager;
        game.Ads(me).Offers[AdSlotKind.Trikot][3] = new SponsorOffer(AdSlotKind.Trikot, 3, 1, 1, 8_000);
        game.AcceptOffer(me, new AdSlot(AdSlotKind.Trikot), 3);
        AdvanceToNextMonth(game);

        Assert.Empty(game.Inbox(me));
        Assert.Null(game.Ads(me).ContractAt(new AdSlot(AdSlotKind.Trikot)));
    }

    [Fact]
    public void CompletedConstruction_IsReported()
    {
        var game = TestGames.Create();
        var me = game.ActiveManager;
        var stadium = game.Finance(me).Stadium;
        game.StartConstruction(me, StadiumProject.Feature, StadiumFeature.Anzeigetafel, stadium.Level(StadiumFeature.Anzeigetafel) + 1);
        // Bauzeit nach em.exe 10–19 Wochen (Spur geld, Teil 1; vorher 2 Wochen fest).
        // Spur kader-rest (A130, R15-E2 = C): Halt für Halt über die Zeitleiste statt Spieltag für Spieltag. Meldungen ohne Spieler
        // zählen täglich herunter (Datei 0x54F07–0x54F87); fällt das Bauende in eine Lücke zwischen zwei Spieltagen, ist die
        // Meldung beim nächsten Spieltag schon fort (mit der Startbelastung 80–120 Bauende Tag 104, Spieltage 103/108).
        var seen = false;
        for (var guard = 0; guard < 2000 && game.Finance(me).Stadium.Constructions.Any(); guard++)
        {
            TestSeason.KeepSquad(game, game.ActiveManager);
            game.Continue();
            seen |= game.Inbox(me).Any(m => m.Lines[0] == "Der Stadion-Ausbau (Anzeigetafel)");
        }
        Assert.Empty(game.Finance(me).Stadium.Constructions);
        Assert.True(seen, "Meldung zum Bauende fehlt");
    }

    [Fact]
    public void Youth_FundingIsRoundedPaidMonthlyAndCountsAsExpense()
    {
        var game = TestGames.Create();
        var me = game.ActiveManager;
        var expenses = game.MonthlyExpenses(me);
        game.SetYouthFunding(me, YouthTeamKind.Jugend, 7_400);
        game.SetYouthFunding(me, YouthTeamKind.Junioren, 45_000);
        Assert.Equal(7_000, game.Finance(me).Youth[1].Funding);
        Assert.Equal(40_000, game.Finance(me).Youth[2].Funding); // Deckel 10.000 · 2^Team (Spur finanzen-rest, Teil 1)
        Assert.Equal(expenses + 47_000, game.MonthlyExpenses(me));

        AdvanceToNextMonth(game);
        Assert.Equal(-47_000, game.Finance(me).Book.Current(FinanceCategory.Jugend));
    }

    [Fact]
    public void Youth_FullFundingClimbsOverTime()
    {
        var game = TestGames.Create();
        var me = game.ActiveManager;
        var team = game.Finance(me).Youth[0];
        Assert.Equal(YouthTeam.StartRank, team.Rank); // Liga = Liga des Vereins (Spur finanzen-rest, Teil 1)
        var start = team.Position;
        game.SetYouthFunding(me, YouthTeamKind.Schueler, YouthTeam.MaxFunding(YouthTeamKind.Schueler));
        TestSeason.PlayRegularSeason(game);
        Assert.True(team.Position < start, $"Platz {team.Rank}, Liga {team.League}");
    }
}
