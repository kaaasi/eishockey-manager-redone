using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Live;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur torszene-dauer, Teil 2 (A472, S02; <c>docs/analyse/torszene-dauer.md</c> §10 T1–T8): Darstellung 2 zeigt den Spieltag
/// durchgehend auf dem Eisfeld. Vor dem Teil rot: der Spieltag lief auch mit Darstellung 2 über den Live-Bildschirm mit Reporter,
/// Pausen und Seiten (<see cref="LiveMatchScreen.MessagesShown"/> nicht leer, <c>RinksBuilt</c> fehlte – Build-Fehler), und das
/// Penaltyschießen zeigte keine Schüsse.
/// </summary>
public sealed class TorszeneDauerUiTests
{
    private sealed record DayRun(LiveMatchScreen Live, GoalSceneRinkView? FirstRink, int LiveFrames, bool Clicked, bool Page,
        bool Options, List<int> Minutes, byte[] FirstFrame, Palette? FirstPalette, GoalSceneHeader BuildHeader, ScreenDriver Driver);

    /// <summary>Ersten Spieltag (Liga, Managerpaarung) mit Darstellung 2 spielen; Klicks und Leertaste werden versucht.</summary>
    private static DayRun PlayFirstDay(int liveMode)
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        game.Options.MatchSpeed = 60;
        game.Options.LiveMode = liveMode;
        d.NextDay();
        LiveMatchScreen? live = null;
        GoalSceneRinkView? first = null;
        var (frames, clicked, page, options) = (0, false, false, false);
        var minutes = new List<int>();
        byte[] firstFrame = [];
        Palette? firstPalette = null;
        GoalSceneHeader header = new("", "", -1, -1, -1, "-");
        for (var guard = 0; d.Host.Current is not MainMenuScreen; guard++)
        {
            Assert.True(guard < 400_000, "Spieltag endet nicht");
            switch (d.Host.Current)
            {
                case LiveMatchScreen l:
                    if (live is null)
                    {
                        live = l;
                        first = l.Rink;
                        var s = new Surface();
                        l.Draw(s);
                        firstFrame = s.Pixels.ToArray();
                        firstPalette = s.Palette;
                        if (first is not null)
                            header = first.Header;
                    }
                    frames++;
                    clicked |= l.WaitingForClick;
                    page |= l.ShowingPage;
                    options |= l.ShowingOptions;
                    if (l.Rink is { } rink && (minutes.Count == 0 || minutes[^1] != rink.Minute))
                        minutes.Add(rink.Minute);
                    // T2/T4: Leertaste und Klick auf Kopf/Logo der Reihe 0 bleiben ohne Wirkung.
                    if (frames % 97 == 0)
                        d.Press(Key.Space);
                    else if (frames % 89 == 0)
                        d.Click(100, 10);
                    else if (l.WaitingForClick)
                        d.Click(160, 120);
                    else
                        d.Frame();
                    break;
                case GoalSceneScreen:
                    d.Frame();
                    break;
                default:
                    if (live is not null && live.ShowingPage)
                        d.RightClick(5, 5);
                    else
                        d.Press(Key.Enter);
                    break;
            }
        }
        return new DayRun(live!, first, frames, clicked, page, options, minutes, firstFrame, firstPalette, header, d);
    }

    /// <summary>
    /// T1–T6, T8 am ersten Ligaspieltag: [ORIGINAL] einmal Eis aufbauen (Kopfzeile der Managerpaarung 0 : 0, Minute 0, ohne
    /// Zuschauertext; Hallenstufe Kennbyte des Heimmanagers, Datei 0x3710B–0x37171, 0x54C07–0x54C5E), je Minute nur die
    /// Minutenzeile (Datei 0x37CD5–0x37CEA), Torszenen darauf in Modus 2 mit der Halle des Aufbaus (Datei 0x56B4D–0x56E3F); kein
    /// Reporter (Datei 0x5252A), kein Eingriff (Datei 0x38DA2), keine Leertaste und Pausen (Datei 0x37C6B, 0x37D02), kein Klick
    /// und keine Seiten nach Minute 60 (Datei 0x3855F, 0x385E7), Abbau am Ende (Datei 0x372C2). Spielstand wie ohne Anzeige.
    /// </summary>
    [GameDataFact]
    public void PermanentRink_WholeMatchDayOnRink()
    {
        var run = PlayFirstDay(GameOptions.PermanentRinkLiveMode);
        var live = run.Live;
        Assert.Empty(live.MessagesShown);
        Assert.False(run.Clicked);
        Assert.False(run.Page);
        Assert.False(run.Options);
        Assert.Empty(live.SkippedMinutes);
        Assert.Equal(0, live.Interventions);
        Assert.True(live.PermanentRink);
        var rink = Assert.Single(live.RinksBuilt);
        Assert.Same(rink, run.FirstRink);
        Assert.Null(live.Rink); // abgebaut

        // T1: Kopfzeile der letzten Paarung mit Anzeigereihe, 0 : 0, Minute 0, Zuschauertext leer.
        var match = live.Session.Shown[^1];
        Assert.Equal(new GoalSceneHeader(match.Home.Name, match.Away.Name, 0, 0, 0, ""), RinkHeaderAtBuild(run));
        // T2: Minutenzeile Minute für Minute (Szenen setzen ihre eigene Minute, die dieselbe ist).
        Assert.Equal(Enumerable.Range(0, LiveMatchDay.LastMinute - 9).ToList(), run.Minutes.Take(LiveMatchDay.LastMinute - 9).ToList());

        Assert.NotEmpty(live.ScenesShown);
        Assert.All(live.ScenesShown, r => Assert.True(r.Continuous));
        Assert.All(live.ScenesShown, r => Assert.Equal(rink.HallLevel, r.HallLevel));
        Assert.Equal(live.ScenesShown.Count, rink.Scenes);

        // Bild nach dem Aufbau: Eis ab Sichtfenster (0 | 0) ohne Figuren, Palette kb\247 – kein Live-Bildschirm (kb\22).
        var graphics = GoalSceneCall.GraphicsFor(run.Driver.Host.Assets);
        var ice = graphics.Rink(rink.HallLevel).Image;
        for (var x = 0; x < 320; x++)
            Assert.Equal(ice.Pixels[x], run.FirstFrame[x]);
        Assert.Same(graphics.BasePalette, run.FirstPalette);

        // T5: nach Minute 60 kein Klick – bei Darstellung 1 wartet der Bildschirm dort (Gegenprobe).
        Assert.True(PlayFirstDay(1).Clicked);
    }

    private static GoalSceneHeader RinkHeaderAtBuild(DayRun run) => run.BuildHeader;

    /// <summary>T1: [ORIGINAL] Hallenstufe des Aufbaus mit mehr als einem Manager (DGROUP 0x386) 2 (Datei 0x568A9–0x568B6).</summary>
    [GameDataFact]
    public void BuildHallLevel_TwoManagers_Two()
    {
        var d = FlowTests.CreateDriver();
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "M0", Head = 0, ClubIndex = 2 });
        var one = Core.NewGame.Create(d.Host.Assets.Database, setup, 5);
        setup.Managers.Add(new ManagerSetup { Name = "M1", Head = 1, ClubIndex = 5 });
        var two = Core.NewGame.Create(d.Host.Assets.Database, setup, 5);
        for (var level = 0; level <= 4; level++)
        {
            Assert.Equal(level, GoalSceneRinkView.BuildHallLevel(one, level));
            Assert.Equal(GoalSceneCall.DefaultHallLevel, GoalSceneRinkView.BuildHallLevel(two, level));
        }
    }

    /// <summary>
    /// T7: DEB-Pokaltag der Managerpaarung, vor Minute 60 Gleichstand gesetzt (Aufbau), bis ein Penaltyschießen entsteht.
    /// [ORIGINAL] Mit Darstellung 1 und 2 (Datei 0x3937F: <c>[0x226]</c> ≠ 0): mit Darstellung 2 erst Abbau des Eises der
    /// Verlängerung (Datei 0x39036), Ankündigung, eigenes Eis (Minute 70, Hallenstufe 2, 0 : 0), je Schuss die Szene des
    /// Spielkerns in Modus 2 mit den Treffern vor dem Schuss in der Kopfzeile, Abbau, Endergebnis. Darstellung 0: keine Szene.
    /// </summary>
    [GameDataTheory]
    [InlineData(1)]
    [InlineData(GameOptions.PermanentRinkLiveMode)]
    [InlineData(0)]
    public void Shootout_OwnRinkWithShots(int liveMode)
    {
        for (var seed = 1; seed <= 16; seed++)
        {
            var d = FlowTests.CreateDriver();
            var setup = new GameSetup();
            setup.Managers.Add(new ManagerSetup { Name = "M0", Head = 0, ClubIndex = 2 });
            var game = Core.NewGame.Create(d.Host.Assets.Database, setup, seed);
            game.Options.MatchSpeed = 60;
            game.Options.LiveMode = liveMode;
            game.Options.ShowDebCup = false; // keine Seiten
            d.Host.Game = game;
            d.Host.Show(new MainMenuScreen(d.Host));
            d.Frame();
            LiveMatchScreen? live = null;
            for (var guard = 0; guard < 20000 && live is null; guard++)
            {
                if (d.Host.Current is MainMenuScreen)
                    d.NextDay();
                else if (d.Host.Current is LiveMatchScreen screen && screen.Session.Day?.Matches[0].Competition == MatchCompetition.DebCup)
                    live = screen;
                else if (d.Host.Current is LiveMatchScreen)
                    d.Press(Key.Escape);
                else
                    d.Frame();
            }
            Assert.NotNull(live);
            Assert.Equal(liveMode == GameOptions.PermanentRinkLiveMode, live.PermanentRink);
            var match = Assert.Single(live.Session.Shown);
            var penalties = new List<(LivePenaltyScreen Screen, GoalSceneRinkView? Rink, int Rinks, int Scenes)>();
            var seen = new HashSet<LivePenaltyScreen>();
            for (var i = 0; i < 400000 && !live.Done; i++)
            {
                if (live.Session.Day is { Minute: >= 50 and < 60 })
                    match.Report.AwayGoals = match.Report.HomeGoals; // Aufbau: Gleichstand vor Minute 60 (Vorbedingung)
                if (d.Host.Current == live)
                {
                    if (live.WaitingForClick)
                        d.Click(160, 120);
                    else
                        d.Frame();
                }
                else if (d.Host.Current is LivePenaltyScreen penalty)
                {
                    if (seen.Add(penalty))
                        penalties.Add((penalty, live.Rink, live.RinksBuilt.Count, live.ScenesShown.Count));
                    Assert.True(live.ShowingShootout);
                    d.Click(160, 120);
                }
                else if (d.Host.Current is GoalSceneScreen)
                    d.Frame();
                else
                    d.RightClick(5, 5);
            }
            if (match.Report.ShootoutGoals is not { } goals)
                continue;
            Assert.Equal(2, penalties.Count);
            Assert.Null(penalties[0].Rink); // Darstellung 2: Eis der Verlängerung abgebaut (Datei 0x39036)
            Assert.Null(penalties[1].Rink);
            var shots = OriginalMinutes.ShootoutShots(match.Report);
            Assert.Equal(goals, (shots.Count(s => s.Side == 0 && s.Goal), shots.Count(s => s.Side == 1 && s.Goal)));
            var shotScenes = live.ScenesShown.Skip(penalties[0].Scenes).ToList();
            Assert.Equal(shotScenes.Count, penalties[1].Scenes - penalties[0].Scenes);
            if (liveMode == 0)
            {
                Assert.Empty(shotScenes);
                Assert.Equal(penalties[0].Rinks, live.RinksBuilt.Count);
                Assert.All(shots, s => Assert.Null(s.Scene));
                return;
            }
            var rink = live.RinksBuilt[^1];
            Assert.Equal(penalties[0].Rinks + 1, live.RinksBuilt.Count);
            Assert.Equal(GoalSceneCall.DefaultHallLevel, rink.HallLevel);
            Assert.Equal(shots.Count, shotScenes.Count);
            var score = (Home: 0, Away: 0);
            for (var k = 0; k < shots.Count; k++)
            {
                var (shot, request) = (shots[k], shotScenes[k]);
                Assert.True(request.Continuous);
                Assert.Equal(GoalSceneCall.DefaultHallLevel, request.HallLevel);
                Assert.Equal((shot.Side, shot.Goal), (request.Side, request.Goal));
                Assert.Equal((match.Home.Name, match.Away.Name, score.Home, score.Away, LiveMatchDay.LastMinute),
                    (request.Header.HomeName, request.Header.AwayName, request.Header.HomeGoals, request.Header.AwayGoals,
                        request.Header.Minute));
                if (shot.Goal)
                    score = shot.Side == 0 ? (score.Home + 1, score.Away) : (score.Home, score.Away + 1);
            }
            Assert.Equal(shots.Count, rink.Scenes);
            Assert.Equal((score.Home, score.Away), (rink.Header.HomeGoals, rink.Header.AwayGoals));
            return;
        }
        Assert.Fail("kein Penaltyschießen der Managerpaarung entstanden");
    }
}
