using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur livespiel-ui-2, B2: Anschluss des Minutenschritts an den Spieltagsablauf (<c>MatchEngine.LiveDriver.cs</c>,
/// <c>docs/analyse/re-livespiel.md</c> §15). Vor dem Teil rot: es gab keinen Treiber (<c>ILiveMatchDriver</c> fehlte,
/// Build-Fehler) – der Live-Bildschirm konnte die Minuten nicht über <see cref="LiveMatchDay"/> treiben.
/// </summary>
public sealed class LivespielUi2DriverTests
{
    private const int ManagedClub = 2;

    private static GameState NewGame(int seed)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "M", Head = 0, ClubIndex = ManagedClub });
        return Core.NewGame.Create(TestGames.CreateDatabase(), setup, seed);
    }

    /// <summary>Treiber, der Minute für Minute rechnet und dabei mitschreibt (wie der Live-Bildschirm ohne Eingriff).</summary>
    private sealed class StepDriver(Action<LiveMatchDay>? perMinute = null) : ILiveMatchDriver
    {
        public List<(int Home, int Away, int Minutes)> Runs { get; } = [];

        public void Run(LiveMatchDay day)
        {
            var minutes = 0;
            while (day.Step())
            {
                minutes++;
                perMinute?.Invoke(day);
            }
            Runs.Add((day.Matches[0].Home.Index, day.Matches[0].Away.Index, minutes));
        }
    }

    /// <summary>
    /// Abnahme B2 (Rundenplan 18): Spieltage mit Managerpaarung über den Live-Bildschirm (Treiber rechnet jede Minute einzeln)
    /// = Ergebnis ohne Anzeige – ganzer Spielstand nach mehreren „Tag weiter“ gleich, und der Treiber lief für die
    /// Managerpaarung.
    /// </summary>
    [Fact]
    public void MatchDaysThroughDriver_EqualWithoutDisplay()
    {
        for (var seed = 0; seed < 3; seed++)
        {
            var a = NewGame(seed);
            var b = NewGame(seed);
            var driver = new StepDriver();
            for (var i = 0; i < 6; i++)
            {
                a.Continue();
                using (MatchEngine.UseLiveDriver(driver))
                    b.Continue();
                Assert.Equal(SaveGame.Save(a), SaveGame.Save(b));
            }
            Assert.NotEmpty(driver.Runs);
            Assert.All(driver.Runs, r => Assert.True(r.Home == ManagedClub || r.Away == ManagedClub));
            Assert.All(driver.Runs, r => Assert.InRange(r.Minutes, 60, 70));
        }
    }

    /// <summary>Ohne Treiber (und nach dem Ende von <see cref="MatchEngine.UseLiveDriver"/>) läuft nichts über ihn.</summary>
    [Fact]
    public void DriverScope_RestoresPrevious()
    {
        Assert.Null(MatchEngine.LiveDriver);
        var driver = new StepDriver();
        using (MatchEngine.UseLiveDriver(driver))
            Assert.Same(driver, MatchEngine.LiveDriver);
        Assert.Null(MatchEngine.LiveDriver);
        var game = NewGame(1);
        game.Continue();
        Assert.Empty(driver.Runs);
    }

    /// <summary>
    /// Eingriff zwischen zwei Minuten wirkt ab der nächsten Minute (A3/A5): Einsatz 0 ab Minute 1 ändert die Rechnung der
    /// Managerpaarung gegenüber dem Lauf ohne Eingriff (mindestens in einer der Proben).
    /// </summary>
    [Fact]
    public void InterventionBetweenMinutes_ChangesResult()
    {
        var differs = false;
        for (var seed = 0; seed < 6 && !differs; seed++)
        {
            var a = NewGame(seed);
            var b = NewGame(seed);
            a.Continue();
            var driver = new StepDriver(day =>
            {
                if (day.Matches[0].TeamOf(ManagedClub) is { } team)
                {
                    MatchIntervention.SetEffort(team, 0);
                    MatchIntervention.SetAttack(team, 0);
                }
            });
            using (MatchEngine.UseLiveDriver(driver))
                b.Continue();
            Assert.NotEmpty(driver.Runs);
            differs = !SaveGame.Save(a).AsSpan().SequenceEqual(SaveGame.Save(b));
        }
        Assert.True(differs);
    }
}
