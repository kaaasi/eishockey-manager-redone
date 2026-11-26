using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Live;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur live-verschraenkt, Teil 4 (S05; <c>docs/analyse/live-spieltag.md</c> §4 E5, E6, §9): Reihenfolge der Reportermeldungen
/// in der Minute (A456), Leertaste = Spielmodus-Optionen mit übersprungener Minute (Teil von A449), Farbe 0 des Reporterbilds
/// (A457).
/// </summary>
public sealed class LiveTastenUiTests
{
    /// <summary>Bis zum ersten Live-Ligaspieltag (ein Manager, Verein 2).</summary>
    private static (ScreenDriver Driver, LiveMatchScreen Live) FirstLeagueDay(int speed)
    {
        var d = FlowTests.CreateDriver();
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "M0", Head = 0, ClubIndex = 2 });
        var game = Core.NewGame.Create(d.Host.Assets.Database, setup, 5);
        game.Options.MatchSpeed = speed;
        d.Host.Game = game;
        d.Host.Show(new MainMenuScreen(d.Host));
        d.Frame();
        for (var guard = 0; guard < 5000; guard++)
        {
            if (d.Host.Current is MainMenuScreen)
                d.NextDay();
            else if (d.Host.Current is LiveMatchScreen screen && screen.Session.Day?.Matches[0].Competition == MatchCompetition.League)
                return (d, screen);
            else if (d.Host.Current is LiveMatchScreen)
                d.Press(Key.Escape);
            else
                d.Frame();
        }
        throw new InvalidOperationException("kein Live-Ligaspieltag");
    }

    /// <summary>
    /// E6: [ORIGINAL] Leertaste (Datei 0x37CFA–0x37D89) öffnet die Spielmodus-Optionen 0x2CD1C; die Minute, in der die Taste
    /// gelesen wurde, rechnet für keine Paarung (keine Ereignisse, keine Chancen), danach läuft das Spiel weiter. Vor dem Teil rot:
    /// die Leertaste war nicht angeschlossen (keine Optionen).
    /// </summary>
    [GameDataFact]
    public void Space_OpensGameOptions_MinuteNotComputed()
    {
        var (d, live) = FirstLeagueDay(speed: 60);
        for (var i = 0; i < 20000 && live.DisplayMinute < 5; i++)
            d.Frame();
        d.Press(Key.Space);
        for (var i = 0; i < 20000 && d.Host.Current is not GameOptionsScreen; i++)
            d.Frame();
        Assert.IsType<GameOptionsScreen>(d.Host.Current);
        Assert.True(live.ShowingOptions);
        d.RightClick(160, 120); // [ORIGINAL] Rechtsklick verlässt die Optionen
        Assert.Same(live, d.Host.Current);
        for (var i = 0; i < 40000 && live.DisplayMinute < 12; i++)
            d.Frame();
        var skipped = Assert.Single(live.SkippedMinutes);
        var day = live.Session.Day!;
        Assert.True(day.Minute > skipped);
        Assert.All(day.Matches, m => Assert.DoesNotContain(m.Report.Events, e => e.Minute == skipped));
        Assert.All(live.Session.Shown, m => Assert.DoesNotContain(m.Row!.ChanceList, c => c.Minute == skipped));
        Assert.DoesNotContain(live.MessagesShown, x => x.Minute == skipped);
        Assert.False(live.Clock.Lamp);
    }

    /// <summary>
    /// A457: [ORIGINAL] Reporterbild über 50h:BD3h (Datei 0x5367E → 0x11283): Farbe 0 der Quelle wird nicht geschrieben, dort
    /// bleibt der Hintergrund kb\22; alle anderen Farben deckend.
    /// </summary>
    [GameDataFact]
    public void ReporterImage_Color0LeavesBackground()
    {
        var d = FlowTests.CreateDriver();
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "M0", Head = 0, ClubIndex = 2 });
        var game = Core.NewGame.Create(d.Host.Assets.Database, setup, 5);
        var a = d.Host.Assets;
        var s = new Surface();
        LiveMatchScreen.DrawBuild(s, a, game, [], lamp: false);
        var frame = a.Image(LiveMatchScreen.FrameGraphic);
        var background = a.Image(LiveMatchScreen.BackgroundGraphic);
        var src = LiveMatchScreen.ReporterSource;
        var (zero, other) = (0, 0);
        for (var y = 0; y < src.H; y++)
        for (var x = 0; x < src.W; x++)
        {
            var (tx, ty) = (LiveMatchScreen.Reporter.X + x, LiveMatchScreen.Reporter.Y + y);
            if (ty >= Surface.Height)
                continue;
            var c = frame[src.X + x, src.Y + y];
            var expected = c == 0 ? background[tx, ty] : c;
            Assert.Equal(expected, s.Pixels[ty * Surface.Width + tx]);
            if (c == 0)
                zero++;
            else
                other++;
        }
        Assert.True(zero > 0 && other > 0);
    }

    /// <summary>
    /// E5 (A456): [ORIGINAL] Verletzungen 0x38B8F laufen je Paarung vor den Chancen (Datei 0x37F06), Strafen 0x35818 am Ende der
    /// Paarung – der Reporter meldet in dieser Reihenfolge. Aufbau: je Minute eine Verletzung in den Bericht der Reihenpaarung
    /// gelegt (reine Anzeige), dann muss in jeder Minute mit weiteren Meldungen die Verletzung zuerst kommen. Vor dem Teil rot:
    /// Verletzungen kamen nach Chancen und Strafen.
    /// </summary>
    [GameDataFact]
    public void ReporterOrder_InjuryBeforeChancesAndPenalties()
    {
        var (d, live) = FirstLeagueDay(speed: 60);
        var match = Assert.Single(live.Session.Shown);
        var first = live.Session.Day!.Minute + 1;
        for (var m = first; m <= 60; m++)
            match.Report.Events.Add(new MatchEvent(m, 0, MatchEventKind.Injury));
        for (var i = 0; i < 200_000 && live.DisplayMinute < 30; i++)
            d.Frame();
        Assert.True(live.DisplayMinute >= 30);
        var mixed = 0;
        foreach (var minute in live.MessagesShown.Where(x => x.HomeClub == match.Home.Index).GroupBy(x => x.Minute))
        {
            var events = minute.Select(x => x.Event).ToList();
            Assert.Equal(ReporterEvent.Injury, events[0]);
            Assert.Single(events, e => e == ReporterEvent.Injury);
            if (events.Count > 1)
                mixed++;
        }
        Assert.True(mixed > 0, "keine Minute mit Verletzung und weiterer Meldung");
    }
}
