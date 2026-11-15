using EishockeyManager.Core;
using EishockeyManager.UI;
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

    /// <summary>
    /// Spur livespiel-ui-2 (B3, Fortsetzung 1): Eingriff über Kopf/Logo in Minute 2 – Aufstellung (linke Taste) und Taktik
    /// (rechte Taste) mit Kopfzeile 0x40290, danach wieder der Live-Bildschirm.
    /// </summary>
    [Scenario("live-eingriff")]
    private static void Eingriff(ScreenDriver d)
    {
        d.StartNewGame();
        d.Host.Game!.Options.MatchSpeed = 0;
        d.NextDay();
        while (d.Host.Current is MatchDayScreen)
            d.Frame();
        var live = (LiveMatchScreen)d.Host.Current!;
        while (live.DisplayMinute < 2)
            d.Frame();
        var side = live.Session.Shown[0].HomeTeam!.Managed ? 0 : 1;
        d.Click(LiveMatchScreen.CrestX(side) + 10, 20);
        d.Move(319, 239); d.Save("01_aufstellung_im_spiel");
        d.Press(Key.Escape);
        d.RightClick(LiveMatchScreen.CrestX(side) + 10, 20);
        d.Move(319, 239); d.Save("02_taktik_im_spiel");
        d.Press(Key.Escape);
        d.Move(319, 239); d.Save("03_zurueck_live");
    }

    /// <summary>
    /// Spur torszenen (C3): erster Ligaspieltag mit Torszenen (Darstellung 1, Geschwindigkeit 60). Bilder: erste Szene nach
    /// 33 Takten (Vergleich mit <c>eisbaeren_f01_01150</c>: Kopfzeile, Eis, Figuren), Szenenende (Kopfzeile nach dem Tor),
    /// Wiederholung mit „r“, danach wieder der Live-Bildschirm mit der Reporter-Meldung.
    /// </summary>
    [Scenario("live-torszene")]
    private static void Torszene(ScreenDriver d)
    {
        d.StartNewGame();
        d.Host.Game!.Options.MatchSpeed = 60;
        d.Host.Game!.Options.LiveMode = 1;
        d.NextDay();
        for (var i = 0; i < 200_000 && d.Host.Current is not GoalSceneScreen; i++)
        {
            if (d.Host.Current is LiveMatchScreen { WaitingForClick: true })
                d.Click(160, 120);
            else if (d.Host.Current is LiveMatchScreen)
                d.Frame();
            else
                d.Press(Key.Enter);
        }
        if (d.Host.Current is not GoalSceneScreen scene)
            return;
        while (scene.Animation.FramesApplied < 33 && !scene.Animation.Finished)
            d.Frame();
        d.Save("01_szene_bild_33");
        d.Type("r");
        // Bei einem Tor steigt die Torzahl der Kopfzeile erst im Takt nach dem letzten Bild (GoalSceneScreen.Tick, #862 P3-4).
        var header = scene.Header;
        while (d.Host.Current == scene && (!scene.Animation.Finished || (scene.Request.Goal && scene.Header == header)))
            d.Frame();
        d.Save("02_szene_ende");
        while (d.Host.Current == scene)
            d.Frame();
        d.Save("03_pause_20");
        while (d.Host.Current is not GoalSceneScreen)
            d.Frame();
        d.Frame();
        d.Save("04_wiederholung");
        while (d.Host.Current is GoalSceneScreen)
            d.Frame();
        var live = (LiveMatchScreen)d.Host.Current!;
        while (!live.ReporterState.Busy || live.ReporterState.Lines.Count == 0)
            d.Frame();
        d.Frames(60);
        d.Save("05_reporter");
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
