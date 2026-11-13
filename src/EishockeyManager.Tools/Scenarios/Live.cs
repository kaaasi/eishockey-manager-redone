using EishockeyManager.Core;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Live;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.Tools.Scenarios;

/// <summary>
/// Live-Spiel (Runde 18, Spur <c>livespiel-ui</c>): „Spiele laufen“ mit der Spieluhr 0x51E90 (Vergleich mit
/// <c>karriere_02315</c>, Minute 08) und das Spielende 0x51F3D („Das wars...“).
/// </summary>
internal static class Live
{
    [Scenario("live-bausteine")]
    private static void Bausteine(ScreenDriver d)
    {
        d.StartNewGame();
        var menu = (MainMenuScreen)d.Host.Current!;
        d.Host.Show(new MatchDayScreen(d.Host, menu, [new MatchesStep(0, Competition.League, true)]));
        d.Frames(MatchDayScreen.BannerFrames + 1);
        d.Frames(8 * MatchDayScreen.FramesPerMinute);
        d.Move(283, 62); d.Save("01_spiele_laufen_minute_08");
        d.Frames(40 * MatchDayScreen.FramesPerMinute);
        d.Move(283, 62); d.Save("02_spiele_laufen_minute_48");
    }

    /// <summary>
    /// Spur livespiel-ui-2 (B2): erster Ligaspieltag über den Live-Bildschirm mit Geschwindigkeit 0 (Reporter, Pausen); Bilder
    /// zu Minute 2 (Vergleich <c>bundestrainer_00200</c>, <c>duell_s2a_01126</c>), zur ersten Reporter-Meldung und nach Minute 60.
    /// </summary>
    [Scenario("live-spiel")]
    private static void LiveSpiel(ScreenDriver d)
    {
        d.StartNewGame();
        d.Host.Game!.Options.MatchSpeed = 0;
        d.NextDay();
        while (d.Host.Current is MatchDayScreen)
            d.Frame();
        var live = (LiveMatchScreen)d.Host.Current!;
        while (live.DisplayMinute < 2)
            d.Frame();
        d.Move(319, 239); d.Save("01_minute_02");
        for (var i = 0; i < 20000 && !live.ReporterState.Busy; i++)
            d.Frame();
        while (live.ReporterState.Busy && live.ReporterState.Lines.Count == 0)
            d.Frame();
        d.Save("02_reporter_kopf");
        while (live.ReporterState.Busy && !live.ReporterState.Lines.Any(l => !l.Large))
            d.Frame();
        d.Save("03_reporter_verein");
        while (live.ReporterState.Busy && live.ReporterState.Lines.All(l => l.Baseline != 10))
            d.Frame();
        d.Frame();
        d.Save("04_reporter_zusammenfassung");
        while (!live.WaitingForClick && !live.Done)
            d.Frame();
        d.Save("05_minute_60");
    }

    [Scenario("live-spielende")]
    private static void Spielende(ScreenDriver d)
    {
        d.StartNewGame();
        d.Host.Game!.Setup.Managers[0].Level = 1; // ohne Bestenliste: gleich „Das wars...“
        d.Host.Show(new SeasonEndGameOverScreen(d.Host, null, null));
        d.Move(319, 239); d.Save("01_das_wars");
    }
}
