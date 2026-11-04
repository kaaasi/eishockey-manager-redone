using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Statistics;

namespace EishockeyManager.UI.Tests;

/// <summary>ui-rest Teil 1 (#535): Info-Fenster aus „Stärken“ (em.exe Datei 0x51A02–0x51A59 → 0x4D385).</summary>
public class StrengthInfoWindowTests
{
    [Theory]
    [InlineData(28, 30, 0)]
    [InlineData(212, 30, 0)]
    [InlineData(100, 13, 0)]   // Division zur 0 hin: (13 − 27) / 15 = 0
    [InlineData(100, 41, 0)]
    [InlineData(100, 42, 1)]
    [InlineData(100, 191, 10)]
    [InlineData(100, 206, 11)]
    public void RowAt_FollowsOriginalHitTest(int x, int y, int expected) =>
        Assert.Equal(expected, StrengthScreen.RowAt(x, y, 12));

    [Theory]
    [InlineData(27, 30)]   // 0x1B < x
    [InlineData(213, 30)]  // x < 0xD5
    [InlineData(100, 12)]  // Zeile −1
    [InlineData(100, 207)] // Zeile 12 von 12
    public void RowAt_OutsideIsNull(int x, int y) => Assert.Null(StrengthScreen.RowAt(x, y, 12));

    [GameDataFact]
    public void ClickOnClubRow_OpensClubWindow_ClosesWithoutChangingState()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        d.OpenTile("Stärken 1. Liga");
        var screen = Assert.IsType<StrengthScreen>(d.Host.Current);
        var before = StateFingerprint.Of(game);

        d.Click(100, 27 + 15 * 2 + 5);
        Assert.True(screen.InfoWindows.Club.Visible);
        Assert.Equal(screen.Rows[2].Club.Index, screen.InfoWindows.Club.Club);
        d.Frame();
        d.Click(100, 100); // weiter im geöffneten Fenster: kein zweites Fenster
        Assert.IsType<StrengthScreen>(d.Host.Current);
        d.Press(Key.Escape);
        Assert.False(screen.InfoWindows.Visible);
        Assert.IsType<StrengthScreen>(d.Host.Current);
        Assert.Equal(before, StateFingerprint.Of(game));
    }

    [GameDataFact]
    public void ClickOnCountryRow_OpensNationWindow()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        d.OpenTile("Stärken Länderteams");
        var screen = Assert.IsType<StrengthScreen>(d.Host.Current);
        d.Click(100, 27 + 15 * 3 + 5);
        Assert.True(screen.InfoWindows.Nation.Visible);
        Assert.False(screen.InfoWindows.Club.Visible);
        Assert.Equal(screen.Rows[3].Club.Index, screen.InfoWindows.Nation.Club);
    }
}
