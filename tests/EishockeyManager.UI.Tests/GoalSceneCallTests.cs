using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Live;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur torszenen, Teil C3: Einbindung des Anzeigeteils der Torszene 0x53C57 (Datei 0x53DCA–0x5406A) in den Live-Bildschirm.
/// Vor dem Teil rot: der Live-Bildschirm spielte keine Szene (<c>LiveMatchScreen.ScenesShown</c>, <c>GoalSceneCall</c> fehlten,
/// Build-Fehler).
/// </summary>
public sealed class GoalSceneCallTests
{
    [Fact]
    public void ReplayKeys_AsInOriginal()
    {
        // [ORIGINAL] Datei 0x5405C–0x54067: nur 0x72 „r“ und 0x77 „w“.
        Assert.True(GoalSceneCall.IsReplayKey('r'));
        Assert.True(GoalSceneCall.IsReplayKey('w'));
        Assert.False(GoalSceneCall.IsReplayKey('R'));
        Assert.False(GoalSceneCall.IsReplayKey(' '));
        Assert.False(GoalSceneCall.IsReplayKey(null));
        Assert.Equal(3, GoalSceneCall.MaxPlays);
        Assert.Equal(10, GoalSceneCall.PauseBefore / 3); // Takte
        Assert.Equal(6, GoalSceneCall.PauseAfter / 3);
    }

    private sealed record Run(byte[] Save, List<GoalSceneRequest> Requests, int Chances, int Scenes);

    /// <summary>
    /// Ersten Spieltag über den Live-Bildschirm spielen; <paramref name="replays"/> = wie oft je Chance „r“ getippt wird.
    /// </summary>
    private static Run PlayFirstDay(int liveMode, int replays, bool lamp = false)
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        game.Options.MatchSpeed = 60;
        game.Options.LiveMode = liveMode;
        d.NextDay();
        var frames = 0;
        LiveMatchScreen? live = null;
        GoalSceneScreen? last = null;
        var (streak, lastTyped) = (0, false);
        while (d.Host.Current is not MainMenuScreen)
        {
            Assert.True(++frames < 400_000, "Live-Spiel endet nicht");
            switch (d.Host.Current)
            {
                case LiveMatchScreen l:
                    var first = live is null;
                    live = l;
                    if (first && lamp)
                        d.Click(12, 230);
                    else if (l.WaitingForClick)
                        d.Click(160, 120);
                    else
                        d.Frame();
                    break;
                case GoalSceneScreen g when !ReferenceEquals(g, last):
                    last = g;
                    streak = lastTyped && streak < GoalSceneCall.MaxPlays ? streak + 1 : 1;
                    lastTyped = streak - 1 < replays;
                    if (lastTyped)
                        d.Type("r");
                    else
                        d.Frame();
                    break;
                case GoalSceneScreen:
                    d.Frame();
                    break;
                default:
                    d.Press(Key.Enter);
                    break;
            }
        }
        var rows = live!.Session.Shown.Select(m => m.Row!).ToList();
        return new Run(Normalized(game), live.ScenesShown, rows.Sum(r => r.ChanceList.Count), rows.Sum(r => r.Scenes.Count));
    }

    /// <summary>
    /// Spielstand ohne das Kennzeichen „neue Nachricht“ (Manager._newMessage): Es gehört zur Sitzung, nicht zum Spielverlauf –
    /// das Hauptmenü der Anzeige-Läufe löscht es im Ereignis OnMainMenu (Datei 0x44E22), der Lauf ohne Anzeige baut kein Hauptmenü.
    /// Seit Runde 22 (transfer-pool, A864) meldet schon der erste Tageslauf „Verletzung außerhalb des Spieles“; vorher blieb das
    /// Kennzeichen an diesem Tag ungesetzt. Alle übrigen Felder bleiben im Vergleich.
    /// </summary>
    private static byte[] Normalized(GameState game) =>
        System.Text.Encoding.UTF8.GetBytes(System.Text.Encoding.UTF8.GetString(SaveGame.Save(game))
            .Replace("\"_newMessage\": true", "\"_newMessage\": false"));

    private static byte[] Reference(int liveMode)
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        game.Options.MatchSpeed = 60;
        game.Options.LiveMode = liveMode;
        game.Continue();
        return Normalized(game);
    }

    /// <summary>
    /// Abnahme C3: Ganzer Spieltag mit Managerpaarung über den Live-Bildschirm, Torszenen an (Darstellung 1, jede Chance
    /// mit Szene) ergibt denselben Spielstand wie <see cref="GameState.Continue"/> ohne Anzeige bei gleicher Darstellung
    /// (der Kern zieht die Szenen nur bei Darstellung ≠ 0, R18-D3 = A). Spielstand der Kopfzeile = Stand vor der Chance.
    /// </summary>
    [GameDataFact]
    public void MatchDayWithGoalScenes_EqualsWithoutDisplay()
    {
        var run = PlayFirstDay(liveMode: 1, replays: 0);
        Assert.True(run.Chances > 0);
        Assert.Equal(run.Chances, run.Scenes);
        Assert.Equal(run.Scenes, run.Requests.Count);
        Assert.All(run.Requests, r => Assert.False(r.Continuous));
        Assert.Equal(Reference(1), run.Save);

        // Kopfzeile: Tore steigen nur um die Tore der vorherigen Szenen.
        var (home, away) = (0, 0);
        foreach (var r in run.Requests)
        {
            Assert.Equal((home, away), (r.Header.HomeGoals, r.Header.AwayGoals));
            if (r.Goal)
                (home, away) = r.Side == 0 ? (home + 1, away) : (home, away + 1);
        }
    }

    /// <summary>
    /// Wiederholung (R19-E1 = A): „r“ während der Szene → dieselbe Szene noch einmal, höchstens drei Abläufe je Chance
    /// ([ORIGINAL] <c>DGROUP 0xCC35</c>, Datei 0x54055); Spielstand unverändert gleich ohne Anzeige.
    /// </summary>
    [GameDataFact]
    public void Replay_AtMostThreeTimes_SameScene_EqualsWithoutDisplay()
    {
        var once = PlayFirstDay(liveMode: 1, replays: 1);
        Assert.Equal(2 * once.Scenes, once.Requests.Count);
        var always = PlayFirstDay(liveMode: 1, replays: 10);
        Assert.Equal(3 * always.Scenes, always.Requests.Count);
        for (var i = 0; i < always.Requests.Count; i += 3)
        {
            for (var k = 1; k < 3; k++)
            {
                var (a, b) = (always.Requests[i], always.Requests[i + k]);
                Assert.Equal((a.Header, a.Side, a.Goal, a.HallLevel), (b.Header, b.Side, b.Goal, b.HallLevel));
                Assert.Equal(a.Scene.Frames.Select(f => (f.Camera, f.Puck, f.Sound)), b.Scene.Frames.Select(f => (f.Camera, f.Puck, f.Sound))); // dieselbe Szene
            }
        }
        var reference = Reference(1);
        Assert.Equal(reference, once.Save);
        Assert.Equal(reference, always.Save);
    }

    /// <summary>Darstellung 2 (Dauer-Eisfeld) → Modus 2 ([ORIGINAL] <c>2 · [0x5A55]</c>, Datei 0x53FCE); Ergebnis gleich.</summary>
    [GameDataFact]
    public void PermanentRink_Mode2_EqualsWithoutDisplay()
    {
        var run = PlayFirstDay(liveMode: GameOptions.PermanentRinkLiveMode, replays: 0);
        Assert.NotEmpty(run.Requests);
        Assert.All(run.Requests, r => Assert.True(r.Continuous));
        Assert.Equal(Reference(GameOptions.PermanentRinkLiveMode), run.Save);
    }

    /// <summary>
    /// Lampe „Schnelldurchlauf“ setzt die Darstellung auf 0 ([ORIGINAL] Datei 0x38DBB–0x38E19) → keine Szene mehr
    /// (Datei 0x53DDC); Darstellung 0 von Anfang an → keine Szene.
    /// </summary>
    [GameDataFact]
    public void LampOrDisplayZero_NoScenes()
    {
        Assert.Empty(PlayFirstDay(liveMode: 0, replays: 0).Requests);
        var lamp = PlayFirstDay(liveMode: 1, replays: 0, lamp: true);
        Assert.Empty(lamp.Requests);
    }

    /// <summary>
    /// Zuschauerzeile und Hallenstufe ([ORIGINAL] Datei 0x53EE6–0x53F72): Managerverein daheim → Zahl + „ ZUSCHAUER“, Stufe
    /// ⌊4 · Zuschauer / Plätze⌋; ohne Manager „AUSWÄRTS“ (auch wenn der Gast einen hat), Länderteam 15 Leerzeichen, je Stufe 2.
    /// </summary>
    [GameDataFact]
    public void Spectators_AsInOriginal()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        var managed = game.Clubs[game.ActiveManagers.First().ClubIndex];
        var other = game.Clubs.First(c => c.Index < NationalTeams.First && game.ActiveManagers.All(m => m.ClubIndex != c.Index));
        Assert.Equal((GoalSceneCall.AwayText, 2), GoalSceneCall.Spectators(game, other, managed));
        Assert.Equal((GoalSceneCall.NationalText, 2),
            GoalSceneCall.Spectators(game, game.Clubs[NationalTeams.Germany], game.Clubs[NationalTeams.First]));
        var (text, level) = GoalSceneCall.Spectators(game, managed, other);
        Assert.Equal("0 ZUSCHAUER", text); // vor der Vorbereitung des Spiels kein Satz
        Assert.Equal(0, level);
        Assert.Equal(15, GoalSceneCall.NationalText.Length);
        // [ORIGINAL] eisbaeren_f01_01150: „ZUSCHAUER“ ab x 44, Text ab x 8 → vierstellige Zahl ohne Tausenderpunkt.
        var flow = d.Host.Assets.Flow;
        Assert.Equal(36, Screens.Competitions.CompetitionWidgets.Width(flow, "4335 "));
        Assert.Equal(39, Screens.Competitions.CompetitionWidgets.Width(flow, "4.335 "));
    }
}
