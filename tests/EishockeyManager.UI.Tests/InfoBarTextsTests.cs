using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur optionen-wirkung, Teil 3: rechte Infoleistenhälfte nach em.exe 0x43E21 – Begegnungszeile mit Breitengrenze 85 Pixel
/// (Originalfont Mini), „ Noch keine“, „Spielfrei“. Belegbilder: folge06_00459, folge12_00800 (Referenz-Repo).
/// </summary>
public class InfoBarTextsTests
{
    private static (GameState Game, int Opponent, UI.Assets Assets) Start()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        var own = game.ActiveManager.ClubIndex;
        var opponent = game.Clubs.First(c => c.League == game.Clubs[own].League && c.Index != own).Index;
        return (game, opponent, new UI.Assets(TestGameData.Load()));
    }

    [GameDataFact]
    public void Meetings_FromBothSides_AwayFirst_AsInVideo()
    {
        var (game, o, assets) = Start();
        var own = game.ActiveManager.ClubIndex;
        Assert.Equal(InfoBarTexts.NoMeetings, InfoBarTexts.MeetingsText(game, o, assets.Mini));
        Assert.Equal("", InfoBarTexts.MeetingsText(game, null, assets.Mini));

        game.RecordMeeting(own, o, 8, 4);
        game.RecordMeeting(o, own, 0, 5);
        game.RecordMeeting(own, o, 4, 0);
        game.RecordMeeting(o, own, 0, 5);

        // folge06_00459: „A 5:0,A 5:0,H 4:0,H 8:4“ steht vollständig in der Leiste (≤ 85 Pixel).
        Assert.Equal("A 5:0,A 5:0,H 4:0,H 8:4", InfoBarTexts.MeetingsText(game, o, assets.Mini));
    }

    [GameDataFact]
    public void Meetings_WiderThan85Pixels_DropTheFirstEntry()
    {
        var (game, o, assets) = Start();
        var own = game.ActiveManager.ClubIndex;
        game.RecordMeeting(own, o, 5, 10);
        game.RecordMeeting(o, own, 7, 2);
        game.RecordMeeting(own, o, 2, 7);
        game.RecordMeeting(o, own, 7, 2);

        // Vier Einträge wären zu breit; folge12_00800 zeigt genau drei: „A 2:7,H 2:7,H 5:10“.
        Assert.True(assets.Mini.Measure("A 2:7,A 2:7,H 2:7,H 5:10") > InfoBarTexts.MaxMeetingsWidth);
        Assert.Equal("A 2:7,H 2:7,H 5:10", InfoBarTexts.MeetingsText(game, o, assets.Mini));
    }

    [GameDataFact]
    public void NoMatch_DuringSeason_IsSpielfrei()
    {
        var (game, _, _) = Start();
        Assert.Equal(SeasonPhase.Running, game.Phase);
        Assert.Equal("Spielfrei", InfoBarTexts.NoMatchText(game));
    }
}
