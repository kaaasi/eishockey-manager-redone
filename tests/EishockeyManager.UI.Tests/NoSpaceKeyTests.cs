using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Competitions;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Entscheidung Nutzer (31.10.2026, #205): Die Leertaste hat im Remake keine Weiter-Funktion mehr – weder im Hauptmenü
/// noch im Spieltagsbildschirm noch in der Frage der Live-Auslosung. Enter und Klick bleiben.
/// </summary>
public class NoSpaceKeyTests
{
    [GameDataFact]
    public void MatchDay_SpaceDoesNotSkip()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        d.NextDay();
        var screen = Assert.IsType<MatchDayScreen>(d.Host.Current);
        d.Frame();
        var banner = screen.CurrentBanner;
        Assert.NotNull(banner); // Banner läuft MatchDayScreen.BannerFrames Bilder
        d.Press(Key.Space);
        Assert.Same(screen, d.Host.Current);
        Assert.Equal(banner, screen.CurrentBanner); // Leertaste überspringt nicht
        d.Press(Key.Enter);
        Assert.True(d.Host.Current != screen || screen.CurrentBanner != banner, "Enter überspringt das Banner nicht");
    }

    [GameDataFact]
    public void LiveDrawQuestion_SpaceDoesNotAnswer()
    {
        var d = DebCupDrawFlowTests.StartStoppingAtDraw();
        var screen = Assert.IsType<DebCupDrawLiveScreen>(d.Host.Current);
        d.Press(Key.Space);
        Assert.Equal(DebCupDrawLiveScreen.Phase.Ask, screen.Current);
        d.Press(Key.Enter);
        Assert.Equal(DebCupDrawLiveScreen.Phase.Reveal, screen.Current);
    }
}
