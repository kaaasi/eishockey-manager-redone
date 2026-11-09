using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Integration Runde 14, Spur kader-start: Befunde aus dem Review <c>docs/reviews/2026-11-09-integration-kader-start-r14.md</c>
/// (R14KS-01 Speichern mit gelistetem Spieler, R14KS-02 Saisonzähler und Heilung gelisteter Spieler).
/// </summary>
public class KaderStartRunde14FixTests
{
    private static (GameState Game, Manager Manager, Club Club) Create(int seed = 3)
    {
        var game = TestGames.Create(seed);
        var manager = game.ActiveManager;
        return (game, manager, game.Clubs[manager.ClubIndex]);
    }

    private static Player ListableDefender(Club club)
    {
        Assert.True(club.ApplyAutoLineup());
        return club.Players.First(p => p.Role == PlayerRole.Defender && !p.OnLoan && TransferMarket.CanLeave(club, p));
    }

    private static GameState RoundTrip(GameState game)
    {
        var loaded = SaveGame.Load(SaveGame.Save(game));
        Assert.True(loaded.Success, loaded.Error);
        return loaded.Game!;
    }

    /// <summary>R14KS-01: Der Spieler auf dem eigenen Markt-Platz (A380) gehört zum Spielstand; Sichern, Laden, Zurückziehen.</summary>
    [Fact]
    public void ListedOwnPlayer_SaveLoad_ListingKept_WithdrawAfterLoad()
    {
        var (game, manager, club) = Create();
        var p = ListableDefender(club);
        Assert.Equal(TransferResult.Accepted, game.TransferMarket.Offer(game, manager, p));

        Assert.Contains(p, game.AllPlayers);
        Assert.Same(p, game.FindPlayer(p.Id));
        Assert.Empty(game.ValidatePlayerIdentity());

        var loaded = RoundTrip(game);
        var loadedClub = loaded.Clubs[club.Index];
        var own = loaded.TransferMarket.OwnListingsOutsideSquad(loadedClub).ToList();
        Assert.Equal(game.TransferMarket.OwnListingsOutsideSquad(club).Select(l => l.Player.Id), own.Select(l => l.Player.Id));
        var lp = Assert.Single(own).Player;
        Assert.Same(lp, loaded.FindPlayer(p.Id));
        Assert.DoesNotContain(lp, loadedClub.Players);
        Assert.Equal(SaveGame.Save(game), SaveGame.Save(loaded)); // Rundreise bytegleich

        Assert.Equal(TransferResult.Accepted, loaded.TransferMarket.Withdraw(own[0]));
        Assert.Contains(lp, loadedClub.Players);
        Assert.Empty(loaded.ValidatePlayerIdentity());
        RoundTrip(loaded);
    }

    /// <summary>
    /// R14KS-02: Nicht verkaufte Spieler kehren vor dem Löschen der Saisonzähler zurück. [ORIGINAL] Saisonschleife 0x26F20:
    /// Rückkehr Datei 0x27547–0x27665 in der Satzschleife (bis 0x279ED–0x27A09), Löschen der Zähler der Managersätze ab Datei
    /// 0x27A0C – der Rückkehrer steht dann schon im Kader.
    /// </summary>
    [Fact]
    public void ListedOverSeasonEnd_ReturnsWithSeasonCountersReset_ClubGamesKept()
    {
        var (game, manager, club) = Create();
        var p = ListableDefender(club);
        p.ContractYears = 3;
        Assert.Equal(TransferResult.Accepted, game.TransferMarket.Offer(game, manager, p));
        RunToSeasonOver(game);
        Assert.Contains(game.TransferMarket.OwnListingsOutsideSquad(club), l => l.Player == p);
        var forward = club.Players.First(q => q.Role == PlayerRole.Forward);
        foreach (var q in new[] { p, forward })
            (q.Games, q.CupGames, q.Goals, q.Penalties2, q.Assists) = (7, 2, 3, 4, 5);
        var clubGames = p.ClubGames(CompetitionSlot.League);

        game.StartNextSeason();

        Assert.Contains(p, club.Players);
        Assert.Equal((0, 0, 0, 0, 0), (forward.Games, forward.CupGames, forward.Goals, forward.Penalties2, forward.Assists));
        Assert.Equal((0, 0, 0, 0, 0), (p.Games, p.CupGames, p.Goals, p.Penalties2, p.Assists));
        Assert.Equal(clubGames, p.ClubGames(CompetitionSlot.League)); // Spiele beim Verein übertragen, nicht gelöscht
    }

    /// <summary>
    /// R14KS-02 (Rand): [ORIGINAL] Tageslauf 0x07169 ruft die Heilung 0x7C12 auch für die Markt-Plätze (Satz 4, DGROUP 0x7623 +
    /// i · 0x34) mit Pool +0x1A = Manager (Datei 0x72A0–0x72CB), danach für die Kaderplätze (Datei 0x7B4E).
    /// </summary>
    [Fact]
    public void ListedOwnPlayer_InjuryHealsLikeSquad()
    {
        var (game, manager, club) = Create();
        var p = ListableDefender(club);
        Assert.Equal(TransferResult.Accepted, game.TransferMarket.Offer(game, manager, p));
        p.InjuredGames = 2;

        game.HealInjuries(7);
        Assert.Equal(1, p.InjuredGames);
        game.HealInjuries(8); // kein Heiltag
        Assert.Equal(1, p.InjuredGames);
        game.HealInjuries(14);
        Assert.Equal(0, p.InjuredGames);
    }

    /// <summary>
    /// Mehrsaisonprobe mit Speichern und Laden (R14KS-01; AGENTS.md §5): vier Saisonwechsel, passiv und aktiv (je Saison zwei
    /// Spieler auf die Liste, einer davon zurückgezogen); in jeder Saison mitten in der Saison und am Saisonende sichern und laden.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FourSeasonChanges_WithListedPlayers_SaveAndLoadEachSeason(bool active)
    {
        var (game, manager, club) = Create(5);
        var index = game.Managers.ToList().IndexOf(manager);
        for (var season = 0; season < 4; season++)
        {
            if (active)
            {
                var candidates = club.Players.Where(p => !p.OnLoan && p.Role != PlayerRole.Goalkeeper
                                                         && TransferMarket.CanLeave(club, p)).Take(2).ToArray();
                foreach (var c in candidates)
                    Assert.Equal(TransferResult.Accepted, game.TransferMarket.Offer(game, manager, c));
                if (candidates.Length == 2)
                    Assert.Equal(TransferResult.Accepted, game.TransferMarket.Withdraw(game.TransferMarket.ListingOf(candidates[1])!));
            }
            for (var guard = 0; game.Phase != SeasonPhase.SeasonOver; guard++)
            {
                Assert.True(guard < 2000, "Saisonende nicht erreicht");
                game.Continue();
                MessageLog.For(game).Collect(game);
                TestSeason.KeepSquad(game, manager, extendExpired: true);
                if (guard == 30)
                    (game, manager, club) = Reload(game, index);
            }
            (game, manager, club) = Reload(game, index);
            game.StartNextSeason();
            Assert.Empty(game.TransferMarket.OwnListingsOutsideSquad(club));
            Assert.Empty(game.ValidatePlayerIdentity());
            Assert.False(manager.Dismissed);
        }
    }

    private static (GameState, Manager, Club) Reload(GameState game, int managerIndex)
    {
        var club = game.Clubs[game.Managers[managerIndex].ClubIndex];
        var own = game.TransferMarket.OwnListingsOutsideSquad(club).Select(l => l.Player.Id).ToList();
        var loaded = RoundTrip(game);
        var manager = loaded.Managers[managerIndex];
        var loadedClub = loaded.Clubs[manager.ClubIndex];
        Assert.Equal(own, loaded.TransferMarket.OwnListingsOutsideSquad(loadedClub).Select(l => l.Player.Id));
        Assert.Empty(loaded.ValidatePlayerIdentity());
        return (loaded, manager, loadedClub);
    }

    private static void RunToSeasonOver(GameState game)
    {
        for (var guard = 0; game.Phase != SeasonPhase.SeasonOver; guard++)
        {
            Assert.True(guard < 2000, "Saisonende nicht erreicht");
            game.Continue();
            MessageLog.For(game).Collect(game);
            TestSeason.KeepSquad(game, game.ActiveManager, extendExpired: true);
        }
    }
}
