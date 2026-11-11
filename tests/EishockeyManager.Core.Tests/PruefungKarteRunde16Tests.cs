using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur pruefung-karte Teil 2 (Runde 16): gesammelte P3 aus den Reviews der Runde.
/// </summary>
public class PruefungKarteRunde16Tests
{
    /// <summary>
    /// #780 (R765N-01, <c>docs/reviews/2026-11-11-pool-nachpruefung.md</c>): Export eines neuen Spiels im Modus „nur Weltkugel“.
    /// Dort steht der Kader von Satz 0 nicht im Pool (<see cref="TransferMarket.SeatManagerSquads"/>), der Export vergibt ihm über
    /// <c>AddUnseatedSquadPlayers</c> ([APPROX], <c>OriginalExport.Squads.cs</c>) die Plätze wie im Original FILE01 (Torhüter 2–3,
    /// Verteidiger 22–31, Stürmer 98–110, Besitzer 0). Vorher rot mit der Mutation <c>if (missing.Count &gt; 0)</c> →
    /// <c>if (false)</c>: alle 25 Kaderplätze mit Poolindex 0.
    /// </summary>
    [GameDataFact]
    public void WorldOnly_NewGame_Export_SquadOfRecord0OnFile01Slots()
    {
        var raw = OriginalExportTests.RawDatabase();
        var setup = new GameSetup();
        setup.AddManager(new ManagerSetup { Name = "Joerg", Head = 6, ClubIndex = 2 });
        Assert.True(setup.ToggleClubCompetitions());
        Assert.Equal(GameMode.WorldOnly, setup.Mode);
        var game = NewGame.Create(ClubDatabase.Parse(raw), setup, 5);
        Assert.Equal(GameMode.WorldOnly, game.Mode);
        var squad = game.Clubs[game.Managers[0].ClubIndex].Players;
        Assert.Equal(25, squad.Count);

        var save = OriginalSaveGame.Read(OriginalExport.Export(game, raw));

        var indices = Enumerable.Range(0, squad.Count).Select(k => (int)OriginalImport.SquadSlot(save, 0, k)[0x0F]).ToArray();
        int[] expected = [2, 3, .. Enumerable.Range(22, 10), .. Enumerable.Range(98, 13)];
        Assert.Equal(expected, indices.Order().ToArray());
        foreach (var index in indices)
            Assert.Equal(0, OriginalImport.PoolEntry(save, index)[0x1A]); // Besitzer Manager 0
        // Jeder Kaderplatz zeigt auf den Poolsatz seines Spielers (Art und Name).
        var roundTrip = GameState.ImportOriginal(save, ClubDatabase.Parse(raw), 5);
        Assert.Empty(roundTrip.ValidatePlayerIdentity());
        Assert.Equal(squad.Select(p => p.Role).Order(), roundTrip.Clubs[roundTrip.Managers[0].ClubIndex].Players.Select(p => p.Role).Order());
    }
}
