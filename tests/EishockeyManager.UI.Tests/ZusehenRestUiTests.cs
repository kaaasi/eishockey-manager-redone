using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Live;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur zusehen-rest, A832: Tormeldung der Paarungen ohne Reihe im Live-Bildschirm (<see cref="LiveGoalNotice"/>, Datei
/// 0xABB4–0xAD71). Vor dem Teil rot: es gab keine Meldung (Build-Fehler).
/// </summary>
public sealed class ZusehenRestUiTests
{
    /// <summary>
    /// Alle Ligavereine außer dem Managerverein im Modus 1: Der Live-Bildschirm zeigt die Tore der früher gerechneten Paarungen in
    /// ihrer Minute – Namen, „-“ und Doppelpunkt (Farbe 4 bei (256 | 206), Farbe 0 bei (258 | 208)), nach der Pause 50 der
    /// Spielstand –, danach ist der Reporterkasten leer. Spielstand gleich ohne Anzeige (reine Anzeige).
    /// </summary>
    [GameDataFact]
    public void LiveScreen_ShowsGoalNotices()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        var managerClub = game.Managers[0].ClubIndex;
        foreach (var c in game.Clubs.Where(c => c.Index != managerClub && c.League == game.Clubs[managerClub].League))
            game.SetWatchMode(c, evict: false, GameState.WatchGoals);
        var reference = SaveGame.Load(SaveGame.Save(game)).Game!;
        var seen = new List<GoalNotice>();
        var scoreSeen = false;
        for (var day = 0; day < 4 && seen.Count == 0; day++)
        {
            reference.Continue();
            d.NextDay();
            var frames = 0;
            while (d.Host.Current is not MainMenuScreen)
            {
                Assert.True(++frames < 400_000, "Live-Spiel endet nicht");
                if (d.Host.Current is LiveMatchScreen live)
                {
                    if (live.GoalNotice.Current is { } notice)
                    {
                        if (seen.Count == 0 || seen[^1] != notice)
                        {
                            seen.Add(notice);
                            Assert.Equal(live.DisplayMinute, notice.Minute);
                            Assert.Contains(notice, live.Session.PendingNotices);
                        }
                        live.Draw(d.Surface);
                        Assert.Equal(4, d.Surface.Pixels[206 * 320 + 256]);
                        Assert.Equal(0, d.Surface.Pixels[208 * 320 + 258]);
                        scoreSeen |= live.GoalNotice.ScoreShown;
                    }
                    if (live.WaitingForClick)
                    {
                        d.Click(160, 120);
                        continue;
                    }
                    d.Frame();
                }
                else
                    d.Press(Key.Enter);
            }
            reference.OnMainMenu(reference.ActiveManager, isReturn: true);
        }
        Assert.NotEmpty(seen);
        Assert.True(scoreSeen);
        Assert.Equal(SaveGame.Save(reference), SaveGame.Save(game));
    }

    /// <summary>[ORIGINAL] Ablauf Datei 0xAD1D–0xAD6E: Namen, Pause 50, Spielstand, Pause 70, dann nichts mehr angezeigt.</summary>
    [Fact]
    public void Notice_Sequence()
    {
        var notice = new LiveGoalNotice();
        var n = new GoalNotice(12, 1, 2, 3, 1);
        var frames = notice.Show(n).ToList();
        Assert.Null(notice.Current);
        Assert.Equal(2 + LiveReporter.PauseFrames(LiveGoalNotice.PauseNames) + LiveReporter.PauseFrames(LiveGoalNotice.PauseScore),
            frames.Count);
    }
}
