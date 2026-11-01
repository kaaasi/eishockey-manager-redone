using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur <c>transfer-vertraege-komplett</c>, Teil 1: Kauf mit anschließender Vertragsverhandlung (em.exe Datei 0x4781B–0x47874,
/// Aufruf 0x4786A) und Verhandlung ausgelaufener Verträge am Saisonende (0x27D05–0x27EE4). Vorher rot: Der Kauf schloss ohne
/// Verhandlung ab, ausgelaufene Verträge endeten ohne Rückfrage und ohne Ablöse.
/// </summary>
public class TransferVertraegeKomplettTests
{
    private static (GameState Game, Manager Manager, Club Club, TransferListing Listing) Market(int seed = 3)
    {
        var game = TestGames.Create(seed);
        var manager = game.ActiveManager;
        manager.Money = 10_000_000;
        var listing = game.TransferMarket.Listings.First(l => l.Seller is null && !l.IsFreeAgent);
        return (game, manager, game.Clubs[manager.ClubIndex], listing);
    }

    /// <summary>[ORIGINAL] 0x4781B–0x4786A: nach der Annahme erst die Verhandlung; vorher kein Geld, kein Wechsel.</summary>
    [Fact]
    public void Purchase_AcceptedBid_WaitsForContract()
    {
        var (game, manager, club, listing) = Market();
        var p = listing.Player;
        var from = listing.From;
        Assert.Equal(TransferResult.Accepted,
            game.TransferMarket.Bid(game, manager, listing, listing.Price * 2, loan: false, out var purchase));
        Assert.NotNull(purchase);
        Assert.Equal(10_000_000, manager.Money);
        Assert.Contains(p, from.Players);
        Assert.DoesNotContain(p, club.Players);
        Assert.Contains(listing, game.TransferMarket.Listings);
        Assert.Equal(SquadSetup.StartContractYears, purchase.Years);
        Assert.Equal(Contracts.Demand(p), purchase.Salary);

        // „Mach' ich!“ mit 3 Jahren zur Forderung im Feld (0x4ABF4: angenommen ohne Zufall)
        var salary = Contracts.DemandAfterYears(game, manager, p, 3, purchase.Salary)!.Value;
        Assert.True(salary >= purchase.Salary);
        Assert.Equal(NegotiationResult.Accepted, game.TransferMarket.Sign(game, purchase, 3, salary, insured: true, true));
        Assert.True(p.Insured);
        Assert.Contains(p, club.Players);
        Assert.DoesNotContain(p, from.Players);
        Assert.Equal(10_000_000 - listing.Price * 2, manager.Money);
        Assert.Equal(3, p.ContractYears);
        Assert.Equal(salary, p.MonthlySalary);
        Assert.DoesNotContain(listing, game.TransferMarket.Listings);
    }

    /// <summary>[ORIGINAL] 0x47874 → 0x47A3E: scheitert die Verhandlung, findet der Transfer nicht statt; der Spieler bleibt
    /// auf dem Markt, das Geld beim Käufer. Die Sperre des Dialogs (random(10, 18), 0x4AD3E) wird gezogen und verfällt.</summary>
    [Fact]
    public void Purchase_FailedNegotiation_TransferDoesNotTakePlace()
    {
        var (game, manager, club, listing) = Market();
        var p = listing.Player;
        game.TransferMarket.Bid(game, manager, listing, listing.Price * 2, loan: false, out var purchase);
        var contractRandom = game.ContractRandom.Capture();
        Assert.Equal(NegotiationResult.TooLong, game.TransferMarket.Sign(game, purchase!, 5, 0, false, false));
        Assert.NotEqual(contractRandom, game.ContractRandom.Capture());
        Assert.DoesNotContain(p, club.Players);
        Assert.Contains(listing, game.TransferMarket.Listings);
        Assert.Equal(10_000_000, manager.Money);
        Assert.Equal(0, p.NegotiationLock);
        Assert.Equal(["Der Transfer", "findet nicht statt."], TransferMarket.NotTakingPlace);
    }

    /// <summary>Die Leihe ruft keine Verhandlung (Datei 0x47851–0x4785B): sofort abgeschlossen.</summary>
    [Fact]
    public void Loan_NoNegotiation()
    {
        var (game, manager, club, listing) = Market();
        Assert.Equal(TransferResult.Accepted,
            game.TransferMarket.Bid(game, manager, listing, listing.LoanFee * 2, loan: true, out var purchase));
        Assert.Null(purchase);
        Assert.Contains(listing.Player, club.Players);
        Assert.True(listing.Player.OnLoan);
    }

    /// <summary>Ohne Oberfläche (<see cref="TransferMarket.Buy"/>): „Mach' ich!“ ohne Eingaben, Vertrag des Wechsels.</summary>
    [Fact]
    public void Buy_WithoutInput_SignsBaseContract()
    {
        var (game, manager, club, listing) = Market();
        var p = listing.Player;
        var demand = Contracts.Demand(p);
        Assert.Equal(TransferResult.Accepted, game.TransferMarket.Buy(game, manager, listing, listing.Price * 2, false));
        Assert.Contains(p, club.Players);
        Assert.Equal(SquadSetup.StartContractYears, p.ContractYears);
        Assert.Equal(demand, p.MonthlySalary);
    }

    private static void PlaySeason(GameState game)
    {
        for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
        {
            Assert.True(guard < 1000, "Saisonende nicht erreicht");
            foreach (var m in game.Managers)
                m.Money = Math.Max(m.Money, 5_000_000);
            game.Continue();
        }
        Assert.Equal(SeasonPhase.SeasonOver, game.Phase);
    }

    /// <summary>Saisonende mit einem Spieler, dessen Vertrag ausläuft (letztes Jahr, ohne Ankündigung).</summary>
    private static (GameState Game, Manager Manager, Club Club, Player Player) SeasonEnd(int seed = 3)
    {
        var game = TestGames.Create(seed);
        PlaySeason(game);
        var manager = game.ActiveManager;
        var club = game.Clubs[manager.ClubIndex];
        foreach (var other in club.Players)
        {
            other.ContractYears = 3;
            other.Notice = ContractNotice.None;
        }
        var p = club.Players.First(x => x.Role == PlayerRole.Forward && !x.OnLoan);
        p.ContractYears = 1;
        return (game, manager, club, p);
    }

    /// <summary>
    /// [ORIGINAL] 0x27D15–0x27D70: nach dem Herunterzählen Laufzeit 0 → Angebot „1 Jahr“ (+0x18 = 101) und Gehalt
    /// Wert(Gehalt) · (147 − Alter) / 100; die Verhandlung läuft „nach dem Saisonende“ (0x3B7 = 233).
    /// </summary>
    [Fact]
    public void SeasonEnd_ExpiredContract_WaitsForNegotiation()
    {
        var (game, manager, club, p) = SeasonEnd();
        game.StartNextSeasonWithRenewals();
        Assert.Contains(p, club.Players);
        Assert.True(GameState.IsRenewalPending(p));
        Assert.Equal([p], game.PendingRenewals(manager));
        Assert.True(game.AfterSeasonEnd);
        Assert.Equal(1, Contracts.CurrentYears(p));
        var value = game.PlayerValueOf(p, club, PlayerValueKind.Salary, new Random(0), manager.Level);
        Assert.Equal(value * (147 - p.Age) / 100 / 3, p.MonthlySalary);
        Assert.Equal(ContractRules.SeasonRemaining(ContractRules.SeasonMatchDays + 1), game.ContractSeasonRemaining);
    }

    /// <summary>[ORIGINAL] 0x27D99–0x27EDF: „Mach' ich!“ ohne Eingaben nimmt das Angebot an – 1 Jahr zum neuen Gehalt,
    /// „… bleibt Ihnen auch die nächste Saison erhalten“.</summary>
    [Fact]
    public void SeasonEnd_Accept_PlayerStays()
    {
        var (game, manager, club, p) = SeasonEnd();
        game.StartNextSeasonWithRenewals();
        var salary = p.MonthlySalary;
        var money = manager.Money;
        var (result, outcome) = game.NegotiateRenewal(manager, p, 0, 0, false, false)!.Value;
        Assert.Equal(NegotiationResult.Accepted, result);
        Assert.True(outcome.Stays);
        Assert.Contains(p, club.Players);
        Assert.Equal(1, p.ContractYears);
        Assert.Equal(salary, p.MonthlySalary);
        Assert.Equal(ContractNotice.None, p.Notice);
        Assert.Equal(money, manager.Money);
        Assert.False(game.AfterSeasonEnd);
        Assert.Equal([[p.Name, "bleibt Ihnen auch", "die nächste Saison erhalten"]], outcome.Messages);
    }

    /// <summary>
    /// [ORIGINAL] 0x27DAD–0x27E9D: „Abbruch“ bzw. abgelehnt → „… kehrt Ihrem Verein den Rücken...“, Ablöse Wert(Preis) / 2
    /// (Drittel-DM, Anzeige / 3), Kaderplatz frei. Ohne Eingabe am ersten Tag der neuen Saison ebenso.
    /// </summary>
    [Theory]
    [InlineData("cancel")]
    [InlineData("reject")]
    [InlineData("continue")]
    public void SeasonEnd_PlayerLeaves_WithCompensation(string how)
    {
        var (game, manager, club, p) = SeasonEnd();
        game.StartNextSeasonWithRenewals();
        var value = game.PlayerValueOf(p, club, PlayerValueKind.Price, new Random(0), manager.Level);
        var expected = (long)(value / 2) / 3;
        Assert.True(expected > 0);
        var money = manager.Money;
        var transfers = game.Finance(manager).Book.Current(FinanceCategory.Transfers);
        ContractRenewal? outcome = null;
        switch (how)
        {
            case "cancel":
                outcome = game.CancelRenewal(manager, p);
                break;
            case "reject":
                var (result, o) = game.NegotiateRenewal(manager, p, 5, 0, false, false)!.Value;
                Assert.Equal(NegotiationResult.TooLong, result);
                outcome = o;
                break;
            default:
                game.Continue();
                break;
        }
        Assert.DoesNotContain(p, club.Players);
        Assert.Contains(p, game.TransferMarket.FreeAgents);
        Assert.Equal(0, p.ContractYears);
        Assert.Equal(transfers + expected, game.Finance(manager).Book.Current(FinanceCategory.Transfers));
        if (outcome is not null)
        {
            Assert.Equal(money + expected, manager.Money);
            Assert.False(outcome.Stays);
            Assert.Equal(expected, outcome.Compensation);
            Assert.Equal([p.Name, "kehrt Ihrem Verein", "den Rücken..."], outcome.Messages[0]);
            Assert.Equal(["Sie erhalten eine", "Ablösesumme von", $"{expected:N0} DM".Replace(',', '.')], outcome.Messages[1]);
        }
        Assert.Null(game.CancelRenewal(manager, p));
    }

    /// <summary>Ohne Oberfläche (<see cref="GameState.StartNextSeason"/>) enden ausgelaufene Verträge sofort wie „Abbruch“.</summary>
    [Fact]
    public void SeasonEnd_WithoutScreen_LeavesImmediatelyWithCompensation()
    {
        var (game, manager, club, p) = SeasonEnd();
        game.StartNextSeason();
        Assert.DoesNotContain(p, club.Players);
        Assert.Contains(p, game.TransferMarket.FreeAgents);
        Assert.True(game.Finance(manager).Book.Current(FinanceCategory.Transfers) > 0);
        Assert.False(game.HasPendingRenewals);
    }

    /// <summary>Verhandlung am Saisonende mit Laufzeit: Forderung nach dem Saisonende (· (152 − Alter) / 100, ohne Untergrenze).</summary>
    [Fact]
    public void SeasonEnd_DemandAfterSeason()
    {
        var (game, manager, club, p) = SeasonEnd();
        game.StartNextSeasonWithRenewals();
        var value = game.PlayerValueOf(p, club, PlayerValueKind.Salary, new Random(0), manager.Level);
        var age = club.Players.Count > 3 ? club.Players[3].Age : p.Age;
        var expected = ContractRules.DemandThirds(value, game.ContractTurnFactor, game.ContractSeasonRemaining, 2, true, age, 0) / 3;
        Assert.Equal(expected, Contracts.DemandAfterYears(game, manager, p, 2));
        var (result, outcome) = game.NegotiateRenewal(manager, p, 2, expected, false, demandFilled: true)!.Value;
        Assert.Equal(NegotiationResult.Accepted, result);
        Assert.True(outcome.Stays);
        Assert.Equal(2, p.ContractYears);
        Assert.Equal(expected, p.MonthlySalary);
    }

    /// <summary>
    /// Mehrsaisonprobe (AGENTS.md §5): vier Saisonwechsel mit Verhandlung am Saisonende, passiv („Abbruch“ bzw. erster Tag)
    /// und aktiv („Mach' ich!“). Kein Spieler doppelt oder verloren, Konto = Startgeld + Buchungen, aktiver Kader hält sich.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MultiSeason_FourChanges_RenewalsAtSeasonEnd(bool active)
    {
        var game = TestGames.Create(11);
        var manager = game.ActiveManager;
        var club = game.Clubs[manager.ClubIndex];
        var start = club.Players.Count;
        var renewed = 0;
        var left = 0;
        for (var change = 0; change < 4; change++)
        {
            PlaySeason(game);
            var before = game.Clubs.SelectMany(c => c.Players).Concat(game.TransferMarket.FreeAgents).Concat(game.RetiredPlayers)
                .ToHashSet();
            game.StartNextSeasonWithRenewals();
            foreach (var p in game.PendingRenewals(manager))
            {
                if (active)
                {
                    var (result, outcome) = game.NegotiateRenewal(manager, p, 0, 0, false, false)!.Value;
                    Assert.Equal(NegotiationResult.Accepted, result);
                    Assert.True(outcome.Stays);
                    renewed++;
                }
                else
                {
                    left++;
                    if (left % 2 == 0)
                        Assert.False(game.CancelRenewal(manager, p)!.Stays);
                }
            }
            game.Continue(); // offene Verhandlungen enden hier
            Assert.False(game.HasPendingRenewals);
            Assert.All(club.Players, p => Assert.True(p.ContractYears > 0, $"{p.Name}: Laufzeit 0 im Kader"));
            var all = game.Clubs.SelectMany(c => c.Players).Concat(game.TransferMarket.FreeAgents).Concat(game.RetiredPlayers)
                .ToList();
            Assert.Equal(all.Count, all.Distinct().Count());
            Assert.Empty(before.Except(all));
        }
        if (active)
        {
            Assert.True(renewed > 0, "keine Verhandlung am Saisonende");
            Assert.True(club.Players.Count >= start - 3, $"aktiver Kader schrumpft: {club.Players.Count}");
        }
        else
            Assert.True(left > 0, "kein Vertrag ausgelaufen");
    }
}
