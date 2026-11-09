using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur kader-start, Teil 2c (Runde 14): A380 – ein eigener Spieler auf der Transferliste verlässt den Kader
/// (re-runde-14, FILE40 → FILE41; em.exe Datei 0x46FDD–0x470DE), Zurückziehen (0x477A9), Gehalt (0x3E8DC) und Rückkehr am
/// Saisonende (A091/A380, FILE44 → FILE46).
/// </summary>
public class KaderStartTeil2cTests
{
    private static (GameState Game, Manager Manager, Club Club) Create(int seed = 3)
    {
        var game = TestGames.Create(seed);
        var manager = game.ActiveManager;
        return (game, manager, game.Clubs[manager.ClubIndex]);
    }

    /// <summary>Ein Verteidiger mit Platz im Kader und in der Aufstellung, der gehen darf.</summary>
    private static Player ListableDefender(Club club)
    {
        Assert.True(club.ApplyAutoLineup());
        var p = club.Players.First(p => p.Role == PlayerRole.Defender && !p.OnLoan && club.Lineup.Contains(p)
                                        && TransferMarket.CanLeave(club, p));
        Assert.True(club.IsDressed(p));
        return p;
    }

    [Fact]
    public void Offer_PlayerLeavesSquad_SlotsMoveUp_ListingWithSeller()
    {
        var (game, manager, club) = Create();
        var p = ListableDefender(club);
        var count = club.Players.Count;
        var index = club.Players.IndexOf(p);
        var next = club.Players[index + 1];

        Assert.Equal(TransferResult.Accepted, game.TransferMarket.Offer(game, manager, p));

        Assert.DoesNotContain(p, club.Players);
        Assert.False(club.Lineup.Contains(p)); // „eingesetzt“ +0x0A = 0 (Datei 0x47093)
        Assert.Equal(count - 1, club.Players.Count);
        Assert.Same(next, club.Players[index]); // Plätze rücken auf (0x108:0x48)
        Assert.Equal(Enumerable.Range(1, club.Players.Count), club.Players.Select(q => q.Number));
        var listing = game.TransferMarket.ListingOf(p);
        Assert.NotNull(listing);
        Assert.Same(manager, listing.Seller);
        Assert.Same(club, listing.From);
        Assert.Equal(game.TransferMarket.OwnListingsOutsideSquad(club), [listing]);
    }

    [Fact]
    public void Withdraw_ReturnsPlayerByRole_NotInLineup_ContractUnchanged()
    {
        var (game, manager, club) = Create();
        var p = ListableDefender(club);
        var (years, salary) = (p.ContractYears, p.MonthlySalary);
        Assert.Equal(TransferResult.Accepted, game.TransferMarket.Offer(game, manager, p));
        var listing = game.TransferMarket.ListingOf(p)!;

        Assert.Equal(TransferResult.Accepted, game.TransferMarket.Withdraw(listing));

        Assert.Null(game.TransferMarket.ListingOf(p));
        Assert.Contains(p, club.Players);
        Assert.False(club.Lineup.Contains(p));
        Assert.False(club.IsDressed(p)); // +0x0A = 0 schon beim Aufsetzen (Datei 0x47093)
        // [ORIGINAL] 0x42CB1: vor dem ersten Spieler mit höherer Art (hinter dem letzten Verteidiger).
        var at = club.Players.IndexOf(p);
        Assert.Equal(club.Players.FindIndex(q => q.Role > PlayerRole.Defender), at + 1);
        Assert.Equal(at + 1, p.Number);
        Assert.Equal((years, salary), (p.ContractYears, p.MonthlySalary)); // Platz unverändert zurückkopiert (0x47907)
        Assert.Equal(TransferResult.Impossible, game.TransferMarket.Withdraw(listing));
    }

    [Fact]
    public void Withdraw_FullSquad_StaysOnMarket()
    {
        var (game, manager, club) = Create();
        var p = ListableDefender(club);
        Assert.Equal(TransferResult.Accepted, game.TransferMarket.Offer(game, manager, p));
        var listing = game.TransferMarket.ListingOf(p)!;
        var donor = game.Clubs.First(c => c != club && c.Index < 52 && c.Players.Count > 0);
        while (club.Players.Count < TransferMarket.MaxSquad)
        {
            var q = donor.Players[^1];
            donor.RemovePlayer(q);
            PlayerFactory.InsertSlot(club, q, managerSquad: true);
        }
        club.RenumberPlayers();

        Assert.Equal(TransferResult.SquadFull, game.TransferMarket.Withdraw(listing));
        Assert.Same(listing, game.TransferMarket.ListingOf(p));
        Assert.DoesNotContain(p, club.Players);
    }

    [Fact]
    public void ListedPlayer_KeepsCostingSalary()
    {
        var (game, manager, club) = Create();
        var p = ListableDefender(club);
        var wages = FinanceRules.MonthlyWages(game, club);
        var insurance = FinanceRules.MonthlyInsurance(game, club);
        var expenses = game.MonthlyExpenses(manager);

        Assert.Equal(TransferResult.Accepted, game.TransferMarket.Offer(game, manager, p));

        // [ORIGINAL] 0x3E8DC: Kaderplätze und Markt-Plätze mit Pool +0x1A = Manager.
        Assert.Equal(wages, FinanceRules.MonthlyWages(game, club));
        Assert.Equal(insurance, FinanceRules.MonthlyInsurance(game, club));
        Assert.Equal(expenses, game.MonthlyExpenses(manager));
        Assert.Equal(wages - p.MonthlySalary, FinanceRules.MonthlyWages(club));
    }

    [Fact]
    public void SoldListedPlayer_GoesToBuyer_SellerPaid()
    {
        var (game, manager, club) = Create();
        var p = ListableDefender(club);
        Assert.Equal(TransferResult.Accepted, game.TransferMarket.Offer(game, manager, p));
        for (var i = 0; game.TransferMarket.Bids.Count == 0 && i < 500; i++)
            game.TransferMarket.AdvanceDay(game, game.Random);
        var bid = Assert.Single(game.TransferMarket.Bids);
        var money = manager.Money;

        Assert.Equal(TransferResult.Accepted, game.TransferMarket.Accept(game, bid));

        Assert.Contains(p, bid.Buyer.Players);
        Assert.DoesNotContain(p, club.Players);
        Assert.Equal(money + TransferMarket.TransferFee(bid), manager.Money);
        Assert.Empty(game.TransferMarket.OwnListingsOutsideSquad(club));
    }

    [Fact]
    public void SeasonChange_UnsoldListedPlayerReturnsByRole_NotInLineup_ContractCountedDown()
    {
        var (game, manager, club) = Create();
        var p = ListableDefender(club);
        p.ContractYears = 3;
        Assert.Equal(TransferResult.Accepted, game.TransferMarket.Offer(game, manager, p));
        RunToSeasonOver(game);
        TestSeason.KeepSquad(game, manager, extendExpired: true);
        // Kein Gebot annehmen: der Spieler bleibt bis zum Saisonende auf der Liste.
        Assert.DoesNotContain(p, club.Players);

        game.StartNextSeason();

        Assert.Contains(p, club.Players);
        Assert.Null(game.TransferMarket.ListingOf(p));
        Assert.Equal(2, p.ContractYears); // wie alle Kaderspieler heruntergezählt (Datei 0x27500)
        var at = club.Players.IndexOf(p);
        Assert.All(club.Players.Take(at), q => Assert.True(q.Role <= PlayerRole.Defender));
        Assert.Equal(Enumerable.Range(1, club.Players.Count), club.Players.Select(q => q.Number));
    }

    /// <summary>
    /// Mehrsaisonprobe (AGENTS.md §5): vier Saisonwechsel, passiver Manager (nichts auf der Liste) und aktiver Manager (je Saison
    /// ein Spieler auf die Liste, einer zurückgezogen bzw. bis zum Saisonende dort). Kein Spieler steht in zwei Kadern oder
    /// zugleich im Kader und auf einem eigenen Markt-Platz; nach jedem Wechsel ist die eigene Liste leer.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FourSeasonChanges_ListedPlayersReturn_NoDuplicates(bool active)
    {
        var (game, manager, club) = Create(5);
        for (var season = 0; season < 4; season++)
        {
            Player? listed = null;
            if (active)
            {
                var candidates = club.Players.Where(p => !p.OnLoan && p.Role != PlayerRole.Goalkeeper
                                                         && TransferMarket.CanLeave(club, p)).Take(2).ToArray();
                foreach (var c in candidates)
                    Assert.Equal(TransferResult.Accepted, game.TransferMarket.Offer(game, manager, c));
                if (candidates.Length == 2)
                    Assert.Equal(TransferResult.Accepted, game.TransferMarket.Withdraw(game.TransferMarket.ListingOf(candidates[1])!));
                listed = candidates.FirstOrDefault();
            }
            for (var guard = 0; game.Phase != SeasonPhase.SeasonOver; guard++)
            {
                Assert.True(guard < 2000, "Saisonende nicht erreicht");
                manager.Money = Math.Max(manager.Money, 5_000_000); // #688 (Sommerpause wie im Original): fünf Monatsabschlüsse ohne Spiele je Sommer; die Probe prüft Rückkehr von der Liste, nicht das Konto – Konto halten, sonst Entlassung nach der Schuldenregel.
                game.Continue();
                MessageLog.For(game).Collect(game);
                TestSeason.KeepSquad(game, manager, extendExpired: true);
                AssertConsistent(game, club);
            }
            game.StartNextSeason();
            AssertConsistent(game, club);
            Assert.Empty(game.TransferMarket.OwnListingsOutsideSquad(club));
            if (listed is not null && !game.Clubs.Where(c => c != club).Any(c => c.Players.Contains(listed)))
                Assert.True(club.Players.Contains(listed) || listed.ContractYears == 0, $"Saison {season}: {listed.Name} fehlt");
            Assert.False(manager.Dismissed);
        }
    }

    private static void AssertConsistent(GameState game, Club club)
    {
        var squads = game.Clubs.SelectMany(c => c.Players).ToArray();
        Assert.Equal(squads.Length, squads.Distinct().Count());
        foreach (var l in game.TransferMarket.OwnListingsOutsideSquad(club))
            Assert.DoesNotContain(l.Player, squads);
        Assert.Equal(Enumerable.Range(1, club.Players.Count), club.Players.Select(q => q.Number));
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
