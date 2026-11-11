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

    /// <summary>
    /// #778 (R766-P3-3, <c>docs/reviews/2026-11-11-abgleich-rest.md</c>): Der Kaderverlust in <see cref="SaisonwechselSchema22Tests"/>
    /// ohne <c>extendExpired</c> ist kein Fehler. Es gehen nur Spieler, deren Vertrag am Saisonende ausläuft: Verlängert wird im Lauf
    /// der Saison nur, wer sich meldet ([ORIGINAL] Datei 0x076FA–0x07951, je Takt random(0, Grenze) = 0 und random(0, 3) = 0); die
    /// übrigen kommen in die Verhandlung am Saisonende (Datei 0x27B64/0x27D05), die <see cref="GameState.StartNextSeason"/> ohne
    /// Oberfläche wie „Abbruch“ abschließt (Spieler geht). Die Manager der Probe kaufen nicht nach, also schrumpft der Kader
    /// (Fixture, Saat 22: 20 → 10 → 6/5 → 5); die Probe hält ihn deshalb mit <c>extendExpired</c> wie die übrigen Mehrsaisonproben.
    /// </summary>
    [Fact]
    public void Schema22Fixture_SquadLoss_OnlyExpiringContracts()
    {
        var loaded = SaveGame.Load(SaisonwechselSchema22Tests.LoadFixture());
        Assert.True(loaded.Success, loaded.Error);
        var game = loaded.Game!;
        var lost = 0;
        for (var change = 0; change < 4; change++)
        {
            for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
            {
                Assert.True(guard < 3000, $"Saisonende {change + 1} nicht erreicht");
                foreach (var m in game.Managers)
                    TestSeason.KeepSquad(game, m);
                game.Continue();
            }
            var before = game.Managers.ToDictionary(m => m, m => game.Clubs[m.ClubIndex].Players.ToList());
            var expiring = before.Values.SelectMany(l => l).Where(p => p.ContractYears <= 1)
                .ToHashSet(ReferenceEqualityComparer.Instance);
            game.StartNextSeason();
            foreach (var (m, players) in before)
            {
                var after = game.Clubs[m.ClubIndex].Players.ToHashSet(ReferenceEqualityComparer.Instance);
                foreach (var gone in players.Where(p => !after.Contains(p)))
                {
                    Assert.True(expiring.Contains(gone), $"Saisonwechsel {change + 1}: {gone.Name} ging mit Laufzeit {gone.ContractYears}");
                    lost++;
                }
            }
        }
        Assert.True(lost >= 20, $"Probe ohne Kaderverlust ({lost})"); // sonst prüft der Test nichts
    }
}
