using EishockeyManager.Core;
using EishockeyManager.UI.Screens;
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

    [Scenario("live-spielende")]
    private static void Spielende(ScreenDriver d)
    {
        d.StartNewGame();
        d.Host.Game!.Setup.Managers[0].Level = 1; // ohne Bestenliste: gleich „Das wars...“
        d.Host.Show(new SeasonEndGameOverScreen(d.Host, null, null));
        d.Move(319, 239); d.Save("01_das_wars");
    }
}
