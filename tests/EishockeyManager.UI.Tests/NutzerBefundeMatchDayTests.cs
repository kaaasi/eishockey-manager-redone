using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur nutzer-befunde, #838: „Spiele laufen“ lief in der Nutzerprobe (main 01ed8a5) in ≈ 2–3 s durch (3 Bilder je Minute),
/// das Original nach Schätzung des Nutzers ≈ 20 s. Das Original wartet ohne Anzeige nicht (einzige Pause 0x37CB7 im Zweig
/// mit Anzeige); die Dauer ist Rechenzeit, im Remake [APPROX] 1/3 s je Minute.
/// </summary>
public class NutzerBefundeMatchDayTests
{
    [Fact]
    public void RunningDay_TakesAboutTwentySeconds()
    {
        Assert.InRange(MatchDayScreen.RunningSeconds, 18.0, 22.0);
    }

    /// <summary>Ohne Live-Anzeige zieht die Uhr des Kerns keine Pause und keinen Zufall (Datei 0x37C6B–0x37CBD nur mit Anzeige).</summary>
    [Fact]
    public void LiveClock_WithoutDisplay_HasNoPause()
    {
        var clock = new LiveClock(new GameOptions());
        Assert.Equal(new LiveMinute(false, 0, 0), clock.Minute(display: false));
    }

    /// <summary>Ohne Eingabe bleibt „Spiele laufen“ so lange stehen; ein Klick überspringt.</summary>
    [GameDataFact]
    public void Running_LastsFramesPerMinuteTimesSixty_UnlessSkipped()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var menu = Assert.IsType<MainMenuScreen>(d.Host.Current);
        var screen = new MatchDayScreen(d.Host, menu, [new MatchesStep(0, Competition.League, true)]);
        d.Host.Show(screen);
        d.Frames(MatchDayScreen.BannerFrames + 1);
        Assert.Null(screen.CurrentBanner);
        d.Frames(MatchDayScreen.Minutes * MatchDayScreen.FramesPerMinute);
        Assert.Same(screen, d.Host.Current);
        d.Click(160, 120);
        Assert.NotSame(screen, d.Host.Current);
    }
}
