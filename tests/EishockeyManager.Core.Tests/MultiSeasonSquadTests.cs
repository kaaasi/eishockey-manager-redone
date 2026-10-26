using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Integration Runde 2, Welle B: Mit Vertragsablauf (Spur „lebenszyklus“) waren ab der dritten Saison alle Kader leer,
/// weil jeder Startvertrag zwei Jahre läuft und Computervereine nie verlängert haben.
/// </summary>
public class MultiSeasonSquadTests
{
    private static void PlayToSeasonEnd(GameState game)
    {
        for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
        {
            Assert.True(guard < 1000, "Saisonende nicht erreicht");
            foreach (var manager in game.Managers)
                manager.Money = 10_000_000; // keine Entlassung im passiven Testlauf
            game.Continue();
        }
        Assert.Equal(SeasonPhase.SeasonOver, game.Phase);
    }

    [Fact]
    public void ComputerClubs_KeepTheirSquads_OverThreeSeasonChanges()
    {
        var game = TestGames.Create();
        var managed = game.Managers.Select(m => m.ClubIndex).ToHashSet();
        var computer = game.Clubs.Where(c => c.Players.Count > 0 && !managed.Contains(c.Index)).ToArray();
        var squads = computer.ToDictionary(c => c, c => c.Players.ToArray());
        var ages = squads.Values.SelectMany(p => p).ToDictionary(p => p, p => p.Age);

        for (var change = 0; change < 3; change++)
        {
            PlayToSeasonEnd(game);
            game.StartNextSeason();
        }

        // Angepasst in Runde 3 (Spur „kader“, R2-12): Die Überbrückung „Computervereine verlängern immer“ ist durch
        // die Kader-KI ersetzt. Spieler können nun aufhören, vertragslos werden und Neuzugänge kommen hinzu; geprüft
        // wird weiter, dass kein Computerkader schrumpft und jeder Spieler genau einmal je Wechsel altert.
        var retired = game.RetiredPlayers.ToHashSet();
        foreach (var club in computer)
        {
            Assert.Equal(squads[club].Length, club.Players.Count);
            Assert.All(club.Players, p => Assert.InRange(p.ContractYears, Contracts.MinYears, Contracts.MaxYears));
        }
        foreach (var (player, age) in ages)
        {
            if (retired.Contains(player))
                Assert.InRange(player.Age, age + 1, age + 3); // altert bis zum Laufbahnende
            else
                Assert.Equal(age + 3, player.Age);
        }
    }
}
