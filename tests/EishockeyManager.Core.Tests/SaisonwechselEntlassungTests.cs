using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur saisonwechsel, Runde 12 Teil 1 (Review #441, SW-02): Werden am 29. April alle Manager entlassen, endet das Spiel
/// erst nach dem Saisonabschluss – die Ewige Tabelle enthält die gespielte Saison wie im Fall, dass nur ein Teil der Manager
/// an diesem Tag entlassen wird (Hot-Seat).
/// </summary>
public class SaisonwechselEntlassungTests
{
    [Fact]
    public void AllManagersDismissedOnSeasonEnd_EternalTableContainsSeason()
    {
        var (game, steps) = PlayUntilDismissalOnSeasonEnd(seed: 71, clubs: [2]);

        var dismissal = Assert.Single(steps.OfType<DismissalStep>());
        Assert.Equal(SeasonCalendar.SeasonEnd, dismissal.Day);
        Assert.True(game.IsGameOver);
        var record = Assert.Single(game.History);
        Assert.Empty(record.Managers); // entlassen: kein Platz, keine Prämie (wie bei Entlassung eines Teils der Manager)

        // Meister und Bundesligisten aus der gespielten Saison stehen in der Ewigen Tabelle (vorher: leer).
        var champion = record.Titles[Title.GermanChampion];
        Assert.Equal(1, game.Eternal(champion).Championships);
        foreach (var club in record.FinalTables[League.Bundesliga])
            Assert.True(game.Eternal(club).Points > 0, $"Verein {club} fehlt in der Ewigen Tabelle");
    }

    [Fact]
    public void AllManagersDismissedOnSeasonEnd_SameEternalTableAsPartialDismissal()
    {
        // Gleicher Ablauf mit zwei Managern: am 29. April wird nur Manager 0 entlassen (Spiel läuft weiter) bzw. beide.
        var (partial, _) = PlayUntilDismissalOnSeasonEnd(seed: 73, clubs: [2, 14], dismissed: [0]);
        var (all, _) = PlayUntilDismissalOnSeasonEnd(seed: 73, clubs: [2, 14], dismissed: [0, 1]);
        Assert.False(partial.IsGameOver);
        Assert.True(all.IsGameOver);
        for (var club = 0; club < NewGame.SelectableClubs; club++)
            Assert.Equal(partial.Eternal(club).Points, all.Eternal(club).Points);
    }

    /// <summary>
    /// Spielt die Vereinssaison mit vollem Konto und setzt danach das Konto der Manager <paramref name="dismissed"/> (Vorgabe: alle)
    /// so tief, dass die Entlassung genau am 29. April fällt (Schuldentage bis dahin, Testaufbau).
    /// </summary>
    private static (GameState Game, List<SeasonStep> Steps) PlayUntilDismissalOnSeasonEnd(int seed, int[] clubs, int[]? dismissed = null)
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
        var days = SeasonCalendar.SeasonEnd - game.CurrentDay + 1; // Tage bis einschließlich 29. April
        Assert.True(days > 1 && days < Manager.DebtDaysUntilDismissal, "Testaufbau: Entlassung muss auf den 29. April fallen");
        foreach (var i in dismissed ?? Enumerable.Range(0, game.Managers.Count).ToArray())
        {
            game.Managers[i].Money = -5_000_000;
            game.Managers[i].DebtDays = Manager.DebtDaysUntilDismissal - days;
        }
        var steps = new List<SeasonStep>();
        for (var guard = 0; game.Phase == SeasonPhase.ClubSeasonOver; guard++)
        {
            Assert.True(guard < 100, "Saisonende nicht erreicht");
            steps.AddRange(game.Continue());
        }
        return (game, steps);
    }
}
