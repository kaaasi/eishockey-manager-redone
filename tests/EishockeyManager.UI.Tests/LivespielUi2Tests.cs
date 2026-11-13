using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Live;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur livespiel-ui-2, B2 (L2/L4/L5): Live-Bildschirm und Reporter (<c>Screens/Live/</c>, <c>docs/analyse/re-livespiel.md</c>
/// §15). Vor dem Teil rot: es gab keinen Live-Bildschirm (Build-Fehler), „Tag weiter“ zeigte für die Managerpaarung nur
/// „Spiele laufen“.
/// </summary>
public sealed class LivespielUi2Tests
{
    private static readonly byte[] ChanceSequence = [0, 0, 9, 9, 9, 9, 9, 9, 0xFF];

    /// <summary>
    /// [ORIGINAL] Mundfolge 0x5224E: Vokal → 0 und das nächste Zeichen überspringen (Datei 0x5228C), mit 9 bis Eintrag 8
    /// auffüllen (Datei 0x522C2–0x522D5), 0xFF. Der Plan (§12 L4) erwartete für „Chance:“ [0, 9 × 8, 0xFF]; der Code ergibt zwei
    /// Öffnungen (A und E) und füllt nur bis 8 Einträge auf.
    /// </summary>
    [Fact]
    public void MouthSequence_AsInOriginal()
    {
        Assert.Equal(ChanceSequence, LiveReporter.MouthSequence("Chance:"));
        Assert.Equal([0, 0, 0, 0, 9, 9, 9, 9, 0xFF], LiveReporter.MouthSequence("2 Minuten"));
        Assert.Equal([0, 0, 0, 0, 0, 9, 9, 9, 0xFF], LiveReporter.MouthSequence("5 Minuten aus"));
        Assert.Equal([9, 9, 9, 9, 9, 9, 9, 9, 0xFF], LiveReporter.MouthSequence("SCHWD"));
        var many = LiveReporter.MouthSequence(new string('a', 80));
        Assert.Equal(LiveReporter.MaxOpenings + 1, many.Length);
        Assert.Equal(0xFF, many[^1]);
    }

    /// <summary>
    /// [ORIGINAL] 0x52520: ab Geschwindigkeit 70 keine Meldung (Datei 0x52534); sonst Kopftext, Verein, Zusammenfassung mit
    /// „wegen “ + Strafgrund (5 Minuten: Index 5 „Übertr. Härte.“) in Farbe 0x18 auf den Grundlinien 10/18/26.
    /// </summary>
    [Fact]
    public void Reporter_MessageFlow()
    {
        var options = new GameOptions { MatchSpeed = 70 };
        var reporter = new LiveReporter(options, new LRandom());
        Assert.Empty(reporter.Message("SCHWEDEN", ReporterEvent.ChanceMissed, null, null, null));

        options.MatchSpeed = 0;
        var seen = new List<IReadOnlyList<ReporterLine>>();
        foreach (var _ in reporter.Message("BERLINER SC", ReporterEvent.Major, "GLATTHAAR", null,
                     GoalSceneSelection.Reasons[GoalSceneSelection.MajorPenaltyReason]))
            seen.Add([.. reporter.Lines]);
        Assert.Contains(seen, l => l.Any(x => x is { Large: true, Text: "Strafzeit:", Baseline: 20, Color: 0x18 }));
        Assert.Contains(seen, l => l.Any(x => x is { Large: false, Text: "BERLINER SC", Baseline: 30, Color: 7 }));
        Assert.Contains(seen, l => l.Any(x => x is { Large: true, Text: "5 Minuten", Baseline: 22 }));
        var summary = seen.Last(l => l.Count == 3);
        Assert.Equal(["5 Minuten", "GLATTHAAR", "wegen Übertr. Härte."], summary.Select(x => x.Text));
        Assert.Equal([10, 18, 26], summary.Select(x => x.Baseline));
        Assert.Empty(reporter.Lines);
        Assert.False(reporter.Busy);
    }

    /// <summary>Pause n ≤ 0 (Geschwindigkeit 62 … 69 mit Zwinkern, #823) wartet nicht; n / 3 Ticks sonst.</summary>
    [Fact]
    public void PauseFrames_NonPositiveIsNoPause()
    {
        Assert.Equal(0, LiveReporter.PauseFrames(-4));
        Assert.Equal(0, LiveReporter.PauseFrames(0));
        Assert.Equal(ShortMessage.FramesFor(45), LiveReporter.PauseFrames(45));
    }

    /// <summary>
    /// Abnahme B2 (Rundenplan 18): „Tag weiter“ mit Managerpaarung über den Live-Bildschirm (Geschwindigkeit 0, jede Minute
    /// einzeln, Reporter, Klick nach Minute 60) ergibt denselben Spielstand wie <see cref="GameState.Continue"/> ohne Anzeige.
    /// </summary>
    [GameDataFact]
    public void MatchDayThroughLiveScreen_EqualsWithoutDisplay()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        game.Options.MatchSpeed = 60;
        var reference = SaveGame.Load(SaveGame.Save(game)).Game!;
        reference.Continue();

        d.NextDay();
        var match = Assert.IsType<MatchDayScreen>(d.Host.Current);
        Assert.NotNull(match.Session);
        Assert.NotNull(match.Session!.Day); // Managerpaarung hält den Spielkern an
        var minutes = new HashSet<int>();
        var frames = 0;
        while (d.Host.Current is not MainMenuScreen)
        {
            Assert.True(++frames < 200_000, "Live-Spiel endet nicht");
            if (d.Host.Current is LiveMatchScreen live)
            {
                minutes.Add(live.DisplayMinute);
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
        Assert.Contains(60, minutes);
        Assert.Equal(SaveGame.Save(reference), SaveGame.Save(game));
        Assert.True(match.Session.Completed);
    }

    /// <summary>Mit der Vorgabe (Geschwindigkeit 70, Schnelldurchlauf) und Enter kehrt „Tag weiter“ ins Hauptmenü zurück.</summary>
    [GameDataFact]
    public void PlayMatchDay_ReturnsToMenu()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        for (var i = 0; i < 3; i++)
            d.PlayMatchDay();
        Assert.IsType<MainMenuScreen>(d.Host.Current);
    }

    /// <summary>Darstellung und Spielzustand: Zeichnen des Live-Bildschirms ändert nichts; Spielstand und Reihe stehen in der Tafel.</summary>
    [GameDataFact]
    public void Drawing_DoesNotChangeState()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        d.NextDay();
        while (d.Host.Current is MatchDayScreen)
            d.Frame();
        var live = Assert.IsType<LiveMatchScreen>(d.Host.Current);
        var before = SaveGame.Save(d.Host.Game!);
        for (var i = 0; i < 3; i++)
            live.Draw(d.Surface);
        Assert.Equal(before, SaveGame.Save(d.Host.Game!));
    }
}
