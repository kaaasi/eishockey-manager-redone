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

    /// <summary>
    /// Mit Enter (Überspringen) kehrt „Tag weiter“ ins Hauptmenü zurück, Spielstand gleich ohne Anzeige; Geschwindigkeit und
    /// Darstellung bleiben (Sicherung 0xE19B/0xE19C vor dem ersten Bild; vorher setzte ein sofortiges Enter sie auf 0).
    /// </summary>
    [GameDataFact]
    public void PlayMatchDay_ReturnsToMenu_EqualsWithoutDisplay()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        var reference = SaveGame.Load(SaveGame.Save(game)).Game!;
        for (var i = 0; i < 5; i++)
        {
            d.PlayMatchDay();
            reference.Continue();
            Assert.IsType<MainMenuScreen>(d.Host.Current);
            Assert.Equal(GameOptions.DefaultMatchSpeed, game.Options.MatchSpeed);
            Assert.Equal(SaveGame.Save(reference), SaveGame.Save(game));
        }
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

    /// <summary>
    /// [ORIGINAL] Treffer-Tests 0x534BC/0x53508 (§10, Abnahme B3): Einsatz über 26 Pixelspalten (52 + d → 4d), Angriff 7 nur am
    /// Randpixel x0 + 64, Lampe 6 × 6, Lampe „Schnelldurchlauf“ x 3 … 20, y 226 … 238.
    /// </summary>
    [Fact]
    public void HitTests_AsInOriginal()
    {
        for (var d = 0; d <= 25; d++)
            Assert.Equal(4 * d + 1, LiveMatchScreen.SliderHit(52 + d, 22, 52, 22, 25, 100));
        Assert.Equal(0, LiveMatchScreen.SliderHit(51, 22, 52, 22, 25, 100));
        Assert.Equal(0, LiveMatchScreen.SliderHit(78, 22, 52, 22, 25, 100));
        Assert.Equal(0, LiveMatchScreen.SliderHit(60, 29, 52, 22, 25, 100));
        Assert.Equal(8, LiveMatchScreen.SliderHit(13 + 64, 29, 52, 29, 25, 7));
        Assert.Equal(7, LiveMatchScreen.SliderHit(13 + 63, 29, 52, 29, 25, 7));
        Assert.True(LiveMatchScreen.LampHit(26, 19, 21, 14));
        Assert.False(LiveMatchScreen.LampHit(27, 14, 21, 14));
        Assert.True(LiveMatchScreen.FastLampHit(3, 226) && LiveMatchScreen.FastLampHit(20, 238));
        Assert.False(LiveMatchScreen.FastLampHit(2, 230) || LiveMatchScreen.FastLampHit(21, 230) || LiveMatchScreen.FastLampHit(10, 239));
    }

    /// <summary>
    /// Klickproben B3 im laufenden Spiel: Einsatz (x0 + 39 + d | 22) → 4d, Angriff am Randpixel → 7, Lampe (x0 + 8 + 8t | 14 + 7j)
    /// → Reihe j, Lampe „Schnelldurchlauf“ → Geschwindigkeit 70 und zurück.
    /// </summary>
    [GameDataFact]
    public void Clicks_SetCommands()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        d.Host.Game!.Options.MatchSpeed = 0;
        d.NextDay();
        while (d.Host.Current is MatchDayScreen)
            d.Frame();
        var live = Assert.IsType<LiveMatchScreen>(d.Host.Current);
        var shown = Assert.Single(live.Session.Shown);
        var side = shown.HomeTeam!.Managed ? 0 : 1;
        var team = side == 0 ? shown.HomeTeam! : shown.AwayTeam!;
        var x0 = LiveMatchScreen.BoardX(side);
        d.Click(x0 + 39 + 5, 22);
        Assert.Equal(LiveMatchScreen.ClickResult.Effort, live.LastClick);
        Assert.Equal(20, team.Effort);
        d.Click(x0 + 39 + 25, 30);
        Assert.Equal(7, team.AttackLevel);
        d.Click(x0 + 8 + 8 + 2, 14 + 7 * 2 + 2);
        Assert.Equal(2, team.AttackLine);
        d.Click(10, 230);
        Assert.True(live.Clock.Lamp);
        Assert.Equal(LiveClock.FastSpeed, d.Host.Game!.Options.MatchSpeed);
        d.Click(10, 230);
        Assert.False(live.Clock.Lamp);
        Assert.Equal(0, d.Host.Game!.Options.MatchSpeed);
    }
}
