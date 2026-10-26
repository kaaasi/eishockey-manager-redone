using EishockeyManager.Core;
using EishockeyManager.UI.Screens.Team;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.UI.Tests;

/// <summary>R2-11 über die echte Ansicht „Verträge“: angekündigte Absage bzw. Ruhestand verhindert die Verhandlung.</summary>
public class ContractNoticeScreenTests
{
    [GameDataTheory]
    [InlineData(ContractNotice.Refused, NegotiationResult.NotWilling)]
    [InlineData(ContractNotice.Retiring, NegotiationResult.Retiring)]
    public void AnnouncedPlayer_ShowsOriginalMessage_InsteadOfNegotiation(ContractNotice notice, NegotiationResult expected)
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        var club = game.Clubs[game.ActiveManager.ClubIndex];
        d.OpenTile("Verträge");
        var screen = Assert.IsType<ContractsScreen>(d.Host.Current);
        var p = TeamUi.Rows(club)[0];
        p.ContractYears = 1;
        p.Notice = notice;
        var salary = p.MonthlySalary;

        d.Click(60, ContractsScreen.FirstRowY + 2);

        Assert.Null(screen.Negotiating);
        Assert.True(screen.Message.Visible);
        Assert.Equal(Contracts.Message(expected, p), screen.Message.Lines);
        Assert.Equal(p.Name, screen.Message.Lines[0]);
        d.Click(10, 10); // Meldung schließen
        Assert.False(screen.Message.Visible);
        Assert.Equal(1, p.ContractYears);
        Assert.Equal(salary, p.MonthlySalary);
    }

    [GameDataFact]
    public void OfferedPlayer_CanStillBeNegotiated()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        var club = game.Clubs[game.ActiveManager.ClubIndex];
        d.OpenTile("Verträge");
        var screen = Assert.IsType<ContractsScreen>(d.Host.Current);
        var p = TeamUi.Rows(club)[0];
        p.ContractYears = 1;
        p.Notice = ContractNotice.Offered;
        p.NoticeYears = 2;

        d.Click(60, ContractsScreen.FirstRowY + 2);

        Assert.Same(p, screen.Negotiating);
        Assert.False(screen.Message.Visible);
    }
}
