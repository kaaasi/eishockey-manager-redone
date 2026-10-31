using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Entscheidung Nutzer (#221): Die Musik läuft nur im Titelbildschirm, sonst nirgends – auch nicht im Spieltag.
/// Befund (31.10.2026): Die Titelmusik endet beim Wechsel in die Spielvorbereitung.
/// </summary>
public class MusicPolicyTests
{
    [GameDataFact]
    public void Music_OnlyInTitleScreen()
    {
        var d = FlowTests.CreateDriver();
        Assert.True(MusicPolicy.Wanted(new TitleScreen(d.Host)));
        Assert.False(MusicPolicy.Wanted(new SetupScreen(d.Host)));
        Assert.False(MusicPolicy.Wanted(null));
        d.StartNewGame();
        Assert.IsType<MainMenuScreen>(d.Host.Current);
        Assert.False(MusicPolicy.Wanted(d.Host.Current));
    }

    [GameDataFact]
    public void Music_NotInMatchDay()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        d.NextDay();
        var match = Assert.IsType<MatchDayScreen>(d.Host.Current);
        Assert.False(MusicPolicy.Wanted(match));
        Assert.Equal(MusicAction.Stop, MusicPolicy.Decide(match, isPlaying: true));
    }

    /// <summary>Verdrahtung: <c>SettingsSync</c> (Desktop) führt nur noch diese Entscheidung aus.</summary>
    [GameDataFact]
    public void Decide_StartsStopsOrLeaves()
    {
        var d = FlowTests.CreateDriver();
        var title = new TitleScreen(d.Host);
        var setup = new SetupScreen(d.Host);
        Assert.Equal(MusicAction.Start, MusicPolicy.Decide(title, isPlaying: false));
        Assert.Equal(MusicAction.None, MusicPolicy.Decide(title, isPlaying: true));
        Assert.Equal(MusicAction.Stop, MusicPolicy.Decide(setup, isPlaying: true));
        Assert.Equal(MusicAction.None, MusicPolicy.Decide(setup, isPlaying: false));
    }
}
