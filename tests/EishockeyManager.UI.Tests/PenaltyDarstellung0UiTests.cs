using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Live;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// A1027 (fix/penalty-darstellung-0, R26-E3 = A; <c>tools/reverse-engineering/live/torszene_dauer.py</c> Teil H): Penaltyschießen
/// einer zugesehenen Paarung mit Darstellung 0. Vor dem Fix rot: zwischen Ankündigung und Endergebnis nur die Pause 50, ohne
/// Schüsse (<see cref="LiveMatchScreen.ShootoutScore"/> fehlte – Build-Fehler).
/// </summary>
public sealed class PenaltyDarstellung0UiTests
{
    /// <summary>
    /// [ORIGINAL] Datei 0x3941A: Live-Bildschirm 0x5353C neu (Stand der Paarung 0 : 0, Datei 0x39284); je Schuss Torszene 0x53C57
    /// ohne Szene, bei Treffer Spielstand 0x51FEA mit den Treffern bis dahin (Datei 0x394D3), Pause 80 (Datei 0x394FB); danach
    /// Pause 50 und Endergebnis.
    /// </summary>
    [GameDataFact]
    public void Shootout_LiveScreenShowsEveryShot()
    {
        for (var seed = 1; seed <= 16; seed++)
        {
            var d = FlowTests.CreateDriver();
            var setup = new GameSetup();
            setup.Managers.Add(new ManagerSetup { Name = "M0", Head = 0, ClubIndex = 2 });
            var game = Core.NewGame.Create(d.Host.Assets.Database, setup, seed);
            game.Options.MatchSpeed = 60;
            game.Options.LiveMode = 0;
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
            var match = Assert.Single(live.Session.Shown);
            var penalties = 0;
            var seen = new HashSet<LivePenaltyScreen>();
            var between = new List<(int Home, int Away)?>();
            byte[]? frame = null;
            (int Home, int Away) frameScore = default;
            for (var i = 0; i < 400000 && !live.Done; i++)
            {
                if (live.Session.Day is { Minute: >= 50 and < 60 })
                    match.Report.AwayGoals = match.Report.HomeGoals; // Aufbau: Gleichstand vor Minute 60 (Vorbedingung)
                if (d.Host.Current == live)
                {
                    if (penalties == 1)
                    {
                        between.Add(live.ShootoutScore);
                        if (live.ShootoutScore is { } sc && (sc.Home + sc.Away) > 0 && frame is null)
                        {
                            var s = new Surface();
                            live.Draw(s);
                            frame = s.Pixels.ToArray();
                            frameScore = sc;
                        }
                    }
                    if (live.WaitingForClick)
                        d.Click(160, 120);
                    else
                        d.Frame();
                }
                else if (d.Host.Current is LivePenaltyScreen penalty)
                {
                    if (seen.Add(penalty))
                        penalties++;
                    d.Click(160, 120);
                }
                else
                    d.RightClick(5, 5);
            }
            if (match.Report.ShootoutGoals is not { } goals)
                continue;
            Assert.Equal(2, penalties);
            var shots = OriginalMinutes.ShootoutShots(match.Report);
            Assert.All(shots, s => Assert.Null(s.Scene));
            Assert.Empty(live.RinksBuilt);
            Assert.Null(live.ShootoutScore); // nach dem Endergebnis wieder der Stand des Spiels

            // Erwartete Bildfolge (Zustand vor jedem Bild): Rückkehr von der Ankündigung, Aufbau (0 : 0), je Schuss Pause 80 mit dem
            // Stand nach dem Schuss, Pause 50 mit dem Endstand.
            var expected = new List<(int Home, int Away)?> { null, (0, 0) };
            var score = (Home: 0, Away: 0);
            foreach (var shot in shots)
            {
                if (shot.Goal)
                    score = shot.Side == 0 ? (score.Home + 1, score.Away) : (score.Home, score.Away + 1);
                expected.AddRange(Enumerable.Repeat<(int, int)?>(score, LiveReporter.PauseFrames(LiveMatchScreen.ShotPause)));
            }
            Assert.Equal(goals, score);
            expected.AddRange(Enumerable.Repeat<(int, int)?>(score, LiveReporter.PauseFrames(LivePenaltyScreen.PauseBeforeResult)));
            Assert.Equal(expected, between);

            // Bild: Spielstandziffern der Reihe zeigen die Treffer des Penaltyschießens (0x51FEA liest DGROUP 0x5C4B).
            // Erwartung unabhängig von DrawBoard: Aufbau plus die vier Ziffern des Penaltystands (0x52190 bei x = 139/148/160/169,
            // y = 6 für Reihe 0); verglichen werden nur die Ziffernzellen.
            Assert.NotNull(frame);
            var (fh, fa) = frameScore;
            var expect = new Surface();
            LiveMatchScreen.DrawBuild(expect, d.Host.Assets, game, live.Session.Shown, lamp: false);
            LiveWidgets.Digit(expect, d.Host.Assets, 0, fh / 10 % 10, 139, 6);
            LiveWidgets.Digit(expect, d.Host.Assets, 0, fh % 10, 148, 6);
            LiveWidgets.Digit(expect, d.Host.Assets, 0, fa / 10 % 10, 160, 6);
            LiveWidgets.Digit(expect, d.Host.Assets, 0, fa % 10, 169, 6);
            // Gegenprobe: dieselben Zellen mit dem Stand des Spiels (score: null) müssen abweichen, sonst prüft der Vergleich nichts.
            var matchBoard = new Surface();
            LiveMatchScreen.DrawBuild(matchBoard, d.Host.Assets, game, live.Session.Shown, lamp: false);
            LiveMatchScreen.DrawBoard(matchBoard, d.Host.Assets, match, 0, LiveMatchDay.LastMinute, score: null);
            Assert.NotEqual((match.Report.HomeGoals, match.Report.AwayGoals), (fh, fa));
            var differs = false;
            foreach (var x0 in new[] { 139, 148, 160, 169 })
                for (var y = 6; y < 6 + LiveWidgets.DigitHeight; y++)
                    for (var x = x0; x < x0 + LiveWidgets.DigitWidth; x++)
                    {
                        var i = y * Surface.Width + x;
                        Assert.True(expect.Pixels[i] == frame[i], $"Ziffer bei ({x} | {y})");
                        differs |= matchBoard.Pixels[i] != frame[i];
                    }
            Assert.True(differs, "Tafel zeigt den Stand des Spiels statt des Penaltystands");
            return;
        }
        Assert.Fail("kein Penaltyschießen der Managerpaarung entstanden");
    }
}
