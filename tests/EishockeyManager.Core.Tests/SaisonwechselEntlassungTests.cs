using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Ausgeschiedene Manager (<see cref="Manager.Dismissed"/>) im Saisonablauf. #688 (R688S-03): Die Entlassung wegen Schulden gibt es
/// nicht mehr (im Original nicht vorhanden, <c>SeasonDismissal.cs</c>); kein Weg der Zeitleiste setzt das Kennzeichen, es kommt nur
/// aus älteren Spielständen. Der Testaufbau setzt es direkt am Halt des 14. April. Der Fall der Spur saisonwechsel (Review #441,
/// SW-02: alle Manager genau am 29. April entlassen, Saison noch abgeschlossen) ist von der Zeitleiste aus nicht mehr erreichbar;
/// geprüft bleiben Hot-Seat mit einem ausgeschiedenen Manager und das Spielende, wenn keiner mehr aktiv ist.
/// </summary>
public class SaisonwechselEntlassungTests
{
    [Fact]
    public void OneManagerDismissed_OtherFinishesSeason_RecordWithoutDismissed()
    {
        var game = PlayToClubSeasonOver(seed: 73, clubs: [2, 14]);
        game.Managers[0].Dismissed = true;
        var steps = new List<SeasonStep>();
        for (var guard = 0; game.Phase == SeasonPhase.ClubSeasonOver; guard++)
        {
            Assert.True(guard < 100, "Saisonende nicht erreicht");
            steps.AddRange(game.Continue());
        }
        Assert.Empty(steps.OfType<DismissalStep>()); // R688S-03: die Zeitleiste erzeugt keinen Entlassungsschritt mehr
        Assert.Equal(SeasonPhase.SeasonOver, game.Phase);
        Assert.Equal(1, game.CurrentManager);
        var record = Assert.Single(game.History);
        Assert.Single(record.Managers, m => m.Manager == 1); // ausgeschieden: kein Platz, keine Prämie
        Assert.DoesNotContain(record.Managers, m => m.Manager == 0);
        var champion = record.Titles[Title.GermanChampion];
        Assert.Equal(1, game.Eternal(champion).Championships);
    }

    [Fact]
    public void AllManagersDismissed_GameOverAtNextDay()
    {
        var game = PlayToClubSeasonOver(seed: 71, clubs: [2]);
        game.Managers[0].Dismissed = true;
        var steps = game.Continue();
        Assert.Empty(steps.OfType<DismissalStep>());
        Assert.True(game.IsGameOver);
        Assert.Empty(game.History); // vor dem 29. April ausgeschieden: kein Saisonabschluss (wie bisher mitten in der Saison)
    }

    /// <summary>Spielt die Vereinssaison mit vollem Konto bis zum Halt „Saisonende“ (14. April).</summary>
    private static GameState PlayToClubSeasonOver(int seed, int[] clubs)
    {
        var game = SaisonwechselZwangsabstiegTests.Create(seed, europe: false, clubs);
        for (var guard = 0; game.Phase == SeasonPhase.Running; guard++)
        {
            Assert.True(guard < 3000, "Vereinssaison nicht beendet");
            foreach (var m in game.Managers)
            {
                m.Money = 10_000_000;
                TestSeason.KeepSquad(game, m);
            }
            game.Continue();
        }
        Assert.Equal(SeasonPhase.ClubSeasonOver, game.Phase);
        Assert.Equal(SeasonCalendar.ClubSeasonEnd + 1, game.CurrentDay);
        return game;
    }
}
