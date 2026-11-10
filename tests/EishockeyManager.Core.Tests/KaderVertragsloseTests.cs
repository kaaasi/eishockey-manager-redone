using System.Text;
using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur kader, Teil 3 (R204, #699 Punkt 4, R15-D4 = A „wie im Original“): Ein Spieler, dessen Vertrag in einem Managerverein
/// endet, kommt nicht auf die Transferliste. [ORIGINAL] Vertragsende ohne Verlängerung („… kehrt Ihnen den Rücken“, Datei
/// 0x27DE3–0x27E9D) gibt nur den Kaderplatz frei (0x108:0x48 = Datei 0x425B6, schreibt Pool +0x1A nicht); der Listenwechsel
/// 0x494EF zieht nur Poolplätze mit Besitzer 6. Beleg FILE44 → FILE46: KJELLBERG (Pool 196) ist in FILE46 weder im Kader noch auf
/// der Liste, Pool +0x1A bleibt 0 (Manager). Vorher rot: Das Remake setzte solche Spieler beim Saisonwechsel zusätzlich auf die
/// freien der zehn Listenplätze (Preis 0, „verpflichtbar“).
/// Fixlauf nach Review #741 (R15-E3 = C, Nutzer 10.11.2026): Der Spieler ist wie im Original ganz verloren – weder im Kader noch
/// vertragslos (vorher rot: <see cref="TransferMarket.FreeAgents"/> enthielt ihn, Computervereine konnten ihn ziehen); sein
/// Poolplatz bleibt vergeben (Pool +0x1A = Manager, FILE46 KJELLBERG Pool 196).
/// </summary>
public class KaderVertragsloseTests
{
    private static GameState Import(string file, int seed) => GameState.ImportOriginal(
        OriginalSaveGame.Read(File.ReadAllBytes(OriginalImportTests.OriginalSave(file))), ClubDatabase.Load(TestGameData.Load()), seed);

    private static void PlayToSeasonOver(GameState game)
    {
        for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
        {
            Assert.True(guard < 5000, "Saisonende nicht erreicht");
            if (game.NationalTurn && game.NationalCoach!.Squad.Count == 0)
                game.AutoNominate();
            game.Continue();
        }
    }

    private static void AssertNoFreeAgentListed(GameState game)
    {
        Assert.DoesNotContain(game.TransferMarket.Listings, l => l.IsFreeAgent);
        var free = game.TransferMarket.FreeAgents.ToHashSet();
        Assert.DoesNotContain(game.TransferMarket.Listings, l => free.Contains(l.Player));
    }

    /// <summary>
    /// Regressionstest R204 (synthetisch): Vertragsende im Managerkader → vertragslos, aber weder beim Saisonstart noch über den
    /// Listenwechsel auf der Liste, also für keinen Manager verpflichtbar.
    /// </summary>
    [Fact]
    public void ExpiredContractInManagedClub_NotOnTransferList()
    {
        var game = TestGames.Create();
        LifecycleRegressionTests.FinishSeason(game);
        var club = game.Clubs[game.ActiveManager.ClubIndex];
        var player = club.Players.First(p => p.Role == PlayerRole.Forward);
        player.ContractYears = 1;
        game.StartNextSeason();
        Assert.DoesNotContain(player, club.Players);
        AssertLost(game, player);
        AssertNoFreeAgentListed(game);
        for (var i = 0; i < 50; i++)
        {
            game.TransferMarket.ChangeList(game, game.Random);
            Assert.DoesNotContain(game.TransferMarket.Listings, l => l.Player == player);
            Assert.All(game.TransferMarket.Listings, l => Assert.True(l.FromPool || l.Seller is not null, l.Player.Name));
        }
        // Auch die Kader-KI der Computervereine findet ihn nicht mehr (R15-E3 = C).
        for (var i = 0; i < 4; i++)
        {
            LifecycleRegressionTests.FinishSeason(game);
            game.StartNextSeason();
        }
        AssertLost(game, player);
    }

    /// <summary>R15-E3 = C: weder in einem Kader noch vertragslos, im Ruhestand oder sonst im Spiel.</summary>
    private static void AssertLost(GameState game, Player player)
    {
        Assert.DoesNotContain(player, game.TransferMarket.FreeAgents);
        Assert.DoesNotContain(game.Clubs, c => c.Players.Contains(player));
        Assert.DoesNotContain(player, game.RetiredPlayers);
        Assert.DoesNotContain(player, game.AllPlayers);
    }

    /// <summary>
    /// Regressionstest R204 am Original-Stand FILE44 (14.04.1994) → FILE46 (10.09.1994): Nach dem Saisonwechsel mit „Abbruch“ in
    /// der Verhandlung (KJELLBERG geht, Bild <c>file46_4</c>) ist die Liste genau die von FILE46 – kein Vertragsloser darauf.
    /// </summary>
    [GameDataFact]
    public void File44_SeasonChange_ListEqualsFile46_WithoutFreeAgents()
    {
        var game = Import("FILE44.HOK", 4711);
        var club = game.Clubs[game.Managers[0].ClubIndex];
        PlayToSeasonOver(game);
        game.StartNextSeason(); // ohne Oberfläche: offene Verhandlungen enden wie „Abbruch“
        var expected = Import("FILE46.HOK", 4711).TransferMarket.Listings.Select(l => l.Player.Name).Order().ToList();
        Assert.Equal(expected, game.TransferMarket.Listings.Select(l => l.Player.Name).Order());
        Assert.DoesNotContain(club.Players, p => p.Name == "KJELLBERG");
        AssertNoFreeAgentListed(game);
    }

    /// <summary>
    /// Regressionstest R15-E3 = C am Original-Stand FILE44 → FILE46: KJELLBERG ist nach dem Saisonwechsel ganz verloren (in FILE46
    /// in keinem Kader, nicht vertragslos), sein Poolplatz 196 bleibt vergeben (Pool +0x1A = 0, Manager) mit Alter wie in FILE46
    /// (32 → 33). Vorher rot: KJELLBERG vertragslos, Platz 196 frei und umbenannt.
    /// </summary>
    [GameDataFact]
    public void File44_SeasonChange_Kjellberg_LostLikeFile46()
    {
        var game = Import("FILE44.HOK", 4711);
        var kjellberg = game.Clubs[game.Managers[0].ClubIndex].Players.Single(p => p.Name == "KJELLBERG");
        var slot = game.TransferMarket.Pool.Single(s => ReferenceEquals(s.Player, kjellberg));
        Assert.Equal(196, slot.Index);
        PlayToSeasonOver(game);
        game.StartNextSeason();
        AssertLost(game, kjellberg);
        var raw46 = OriginalSaveGame.Read(File.ReadAllBytes(OriginalImportTests.OriginalSave("FILE46.HOK")));
        var entry = OriginalImport.PoolEntry(raw46, 196);
        Assert.Equal(0, entry[0x1A]); // Besitzer: Manager 0
        Assert.Equal(kjellberg.Name, Encoding.Latin1.GetString(entry[..0x12]).TrimEnd('\0'));
        var file46 = Import("FILE46.HOK", 4711);
        Assert.DoesNotContain(file46.Clubs, c => c.Players.Any(p => p.Name == "KJELLBERG"));
        Assert.DoesNotContain(file46.TransferMarket.FreeAgents, p => p.Name == "KJELLBERG");
        Assert.Same(kjellberg, slot.Player);
        Assert.Equal(PoolOwner.Taken, slot.Owner);
        Assert.Equal(entry[0x14], slot.Player.Age);
    }

    /// <summary>
    /// Mehrsaisonprobe (AGENTS.md §5): vier Saisonwechsel ab FILE44, passiv („Abbruch“ in jeder Verhandlung, alle Spieler mit
    /// Vertragsende gehen) und aktiv (alle verlängert). An jedem Spieltag und nach jedem Wechsel steht kein Vertragsloser auf der
    /// Liste; jeder Spieler, der den Managerverein bei Vertragsende verlassen hat, ist ganz verloren (R15-E3 = C) und taucht in
    /// keinem Kader wieder auf.
    /// </summary>
    [GameDataTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void File44_FourSeasonChanges_FreeAgentsNeverListed(bool active)
    {
        var game = Import("FILE44.HOK", 4711);
        var manager = game.Managers[0];
        var club = game.Clubs[manager.ClubIndex];
        var departed = new HashSet<Player>();
        for (var season = 0; season < 4; season++)
        {
            for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
            {
                Assert.True(guard < 5000, "Saisonende nicht erreicht");
                if (game.NationalTurn && game.NationalCoach!.Squad.Count == 0)
                    game.AutoNominate();
                game.Continue();
                AssertNoFreeAgentListed(game);
            }
            var before = club.Players.Where(p => p.Loan is null).ToList(); // Leihe vom Markt endet frei (A115, VALLIERE)
            game.StartNextSeasonWithRenewals();
            foreach (var p in game.PendingRenewals(manager))
            {
                if (active)
                {
                    manager.Money = Math.Max(manager.Money, 100_000_000);
                    game.NegotiateRenewal(manager, p, 2, Contracts.DemandAfterYears(game, manager, p, 2) ?? 2 * p.MonthlySalary, false,
                        demandFilled: true);
                }
                else
                    game.CancelRenewal(manager, p);
            }
            Assert.Empty(game.PendingRenewals(manager));
            AssertNoFreeAgentListed(game);
            departed.UnionWith(before.Where(p => !club.Players.Contains(p) && !game.RetiredPlayers.Contains(p)));
            foreach (var p in departed)
                AssertLost(game, p);
            var all = game.AllPlayers.ToList();
            Assert.Equal(all.Count, all.Distinct().Count());
        }
        Assert.Equal(1997, game.Season);
        if (!active)
            Assert.NotEmpty(departed);
    }
}
