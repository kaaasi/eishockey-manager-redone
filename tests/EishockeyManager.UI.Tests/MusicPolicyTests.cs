using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;

namespace EishockeyManager.UI.Tests;

/// <summary>Befund Nutzer (31.10.2026): Die Titelmusik endet beim Wechsel in die Spielvorbereitung.</summary>
public class MusicPolicyTests
{
    [GameDataFact]
    public void TitleMusic_StopsInSetup_AndMainMenu()
    {
        var d = FlowTests.CreateDriver();
        var settings = new GameSettings();
        var title = new TitleScreen(d.Host);
        Assert.True(MusicPolicy.Wanted(title, settings));
        Assert.False(MusicPolicy.Wanted(new SetupScreen(d.Host), settings));
        d.StartNewGame();
        Assert.IsType<MainMenuScreen>(d.Host.Current);
        Assert.False(MusicPolicy.Wanted(d.Host.Current, settings));
    }

    [GameDataFact]
    public void MatchDay_FollowsMusicInMatchSetting()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        d.NextDay();
        var match = Assert.IsType<MatchDayScreen>(d.Host.Current);
        Assert.True(MusicPolicy.Wanted(match, new GameSettings { MusicInMatch = true }));
        Assert.False(MusicPolicy.Wanted(match, new GameSettings { MusicInMatch = false }));
    }
}
