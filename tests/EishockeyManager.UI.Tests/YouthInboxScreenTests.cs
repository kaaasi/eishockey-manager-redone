using EishockeyManager.Tests.Support;
using EishockeyManager.Core;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.ManagerArea;

namespace EishockeyManager.UI.Tests;

/// <summary>Jugendteams, Nachrichtenleiste und Posteingang mit den Originaldateien (ohne „gamedata“ übersprungen).</summary>
public class YouthInboxScreenTests
{
    [GameDataFact]
    public void YouthBars_SetFunding()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        d.OpenTile("Jugend");
        Assert.IsType<YouthScreen>(d.Host.Current);
        var youth = d.Host.Game!.Finance(d.Host.Game.ActiveManager).Youth;

        d.Click(YouthScreen.Bar(0).Right - 1, YouthScreen.Bar(0).Y + 4);
        d.Click(YouthScreen.Bar(2).X + 7, YouthScreen.Bar(2).Y + 4);
        Assert.Equal(10_000, youth[0].Funding);
        Assert.Equal(1_000, youth[2].Funding);
        d.Press(Key.Escape);
        Assert.IsType<MainMenuScreen>(d.Host.Current);
    }

    [GameDataFact]
    public void ChartWindow_AsksSpyForTalentSearch()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        d.OpenTile("Jugend");
        var youth = Assert.IsType<YouthScreen>(d.Host.Current);
        var game = d.Host.Game!;
        var me = game.ActiveManager;

        d.Click(150, 100);
        Assert.True(youth.AsksTalentSearch);
        d.Click(YouthScreen.NoButton.X + 5, YouthScreen.NoButton.Y + 4);
        Assert.False(youth.AsksTalentSearch);
        Assert.Null(game.Finance(me).TalentSearch);

        var price = game.TalentSearchPrice(me);
        var money = me.Money;
        d.Click(150, 100);
        d.Click(YouthScreen.YesButton.X + 5, YouthScreen.YesButton.Y + 4);
        Assert.NotNull(game.Finance(me).TalentSearch);
        Assert.Equal(money - price, me.Money);
        Assert.IsType<YouthScreen>(d.Host.Current);
    }

    [GameDataFact]
    public void NewsBar_OpensInbox_WhereQuestionsAreAnswered()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        var me = game.ActiveManager;
        var message = game.Notify(me, "Testmeldung", "zweite Zeile");
        Assert.Equal(0, NewsBar.Shown(game));
        d.Frame();
        Assert.True(message.Read);

        d.Click(100, 210);
        var inbox = Assert.IsType<InboxScreen>(d.Host.Current);
        Assert.Same(message, Assert.Single(inbox.Page));
        d.Press(Key.Escape);
        Assert.IsType<MainMenuScreen>(d.Host.Current);

        // Frage: Vertragsverlängerung
        game.Ads(me).Offers[AdSlotKind.Fernsehen][1] = new SponsorOffer(AdSlotKind.Fernsehen, 1, 1, 1, 12_000);
        var contract = game.AcceptOffer(me, new AdSlot(AdSlotKind.Fernsehen), 1)!;
        game.Ads(me).Contracts.Remove(contract);
        var question = new Message { Kind = MessageKind.SponsorRenewal, Day = game.Day, Lines = ["Möchten Sie den Vertrag mit", "4 SAT verlängern ?"], Contract = contract };
        game.Finance(me).Inbox.Add(question);

        d.OpenTile("Neuer");
        Assert.IsType<InboxScreen>(d.Host.Current);
        var yes = InboxScreen.YesButton(0);
        d.Click(yes.X + 5, yes.Y + 5);
        Assert.False(question.IsQuestion);
        Assert.NotNull(game.Ads(me).ContractAt(new AdSlot(AdSlotKind.Fernsehen)));
    }
}
