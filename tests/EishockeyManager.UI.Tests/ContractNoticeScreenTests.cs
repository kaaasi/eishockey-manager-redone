using EishockeyManager.Core;
using EishockeyManager.UI.Screens.Team;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.UI.Tests;

/// <summary>R2-11 über die echte Ansicht „Verträge“: angekündigte Absage bzw. Ruhestand verhindert die Verhandlung.</summary>
public class ContractNoticeScreenTests
{
    // Spur ui-rest, Runde 17, Teil 2 (A363, R15-E2 = C): Der Fall Refused steht jetzt in RefusedPlayer_OpensDialog_FieldReportsNotWilling –
    // das Original meldet beim Anklicken der Zeile nur Leihe und Ruhestand (Datei 0x4015D–0x401D3), die Sperre erst im Dialog (0x4A692).
    [GameDataTheory]
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

    /// <summary>
    /// A363: Eine Absage (<see cref="ContractNotice.Refused"/>, nur aus alten Spielständen) öffnet wie im Original das
    /// Verhandlungsfenster; erst das Anklicken der Vertragsdauer meldet „nicht verhandlungsbereit“ ([ORIGINAL] Datei 0x4A692–0x4A770).
    /// </summary>
    [GameDataFact]
    public void RefusedPlayer_OpensDialog_FieldReportsNotWilling()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        var club = game.Clubs[game.ActiveManager.ClubIndex];
        d.OpenTile("Verträge");
        var screen = Assert.IsType<ContractsScreen>(d.Host.Current);
        var p = TeamUi.Rows(club)[0];
        p.ContractYears = 1;
        p.Notice = ContractNotice.Refused;
        var salary = p.MonthlySalary;

        d.Click(60, ContractsScreen.FirstRowY + 2);
        Assert.Same(p, screen.Negotiating);
        Assert.False(screen.Message.Visible);

        var years = screen.Dialog!.YearsField;
        d.Click(years.X + 5, years.Y + 2);
        Assert.True(screen.Message.Visible);
        Assert.Equal(Contracts.Message(NegotiationResult.NotWilling, p), screen.Message.Lines);
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
