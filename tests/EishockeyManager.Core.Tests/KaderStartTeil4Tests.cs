using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur kader-start, Teil 4 (Runde 14): Saisonende-Abgang aus re-runde-14 Sitzung 4 (FILE44 → FILE46) – Leihende A115
/// (Datei 0x27547–0x27665, R14RD2-03), Saisonzähler A088 (Datei 0x27A0C–0x27AA8), Zahlen des Saisonendes (Prämie, Gehalt,
/// Ablöse) und der Listenpreis eigener Markt-Plätze (FILE40 → FILE41, Datei 0x470C4).
/// </summary>
public class KaderStartTeil4Tests
{
    /// <summary>Zwei Manager (Vereine 5 und 6): Verleiher und Entleiher.</summary>
    private static (GameState Game, Club Origin, Club Borrower) TwoManagers() =>
        TwoManagers(GameModeTests.Create(3, true, true, 5, 6));

    private static (GameState Game, Club Origin, Club Borrower) TwoManagers(GameState game) =>
        (game, game.Clubs[game.Managers[0].ClubIndex], game.Clubs[game.Managers[1].ClubIndex]);

    /// <summary>Leihe wie <see cref="TransferMarket"/> (Join mit Leihvertrag 1 Jahr), danach der Leih-Kaderplatz wie angegeben.</summary>
    private static Player Lend(Club origin, Club borrower, PlayerRole role, int years, int salary, int premium)
    {
        var p = origin.Players.Last(q => q.Role == role && !q.OnLoan);
        p.Age = 25; // kein Laufbahnende
        origin.RemovePlayer(p);
        PlayerFactory.Join(borrower, p, 1, loan: true, new PlayerLoan(origin, 4, 999, true));
        (p.ContractYears, p.MonthlySalary, p.InsurancePremium) = (years, salary, premium);
        return p;
    }

    private static void KeepAll(GameState game)
    {
        foreach (var m in game.Managers)
            TestSeason.KeepSquad(game, m, extendExpired: true);
    }

    private static void FinishSeason(GameState game)
    {
        for (var guard = 0; game.Phase != SeasonPhase.SeasonOver; guard++)
        {
            Assert.True(guard < 2000, "Saisonende nicht erreicht");
            foreach (var m in game.Managers)
                m.Money = Math.Max(m.Money, 10_000_000);
            game.Continue();
            MessageLog.For(game).Collect(game);
            KeepAll(game);
        }
    }

    /// <summary>
    /// [ORIGINAL] A115: Leihe vom Markt (Pool +0x1A ≥ 4) endet am Saisonende frei (+0x1A = 6, Datei 0x275FF) – VALLIERE FILE44 →
    /// FILE46, weder im Kader noch im Markt. Vorher kehrte der Spieler mit dem Stammvertrag zum Computerverein zurück.
    /// </summary>
    [Fact]
    public void MarketLoan_EndsFree_NotBackToComputerClub()
    {
        var game = TestGames.Create(3);
        var club = game.Clubs[game.ActiveManager.ClubIndex];
        var donor = game.Clubs.First(c => c != club && c.Index < 48 && c.Players.Count(p => p.Role == PlayerRole.Forward) > 3);
        var p = donor.Players.Last(q => q.Role == PlayerRole.Forward);
        p.Age = 32; // nach dem Altern 33 > SquadRules.FreeAgentMaxAge: das Auffüllen der Computervereine zieht ihn nicht
        donor.RemovePlayer(p);
        PlayerFactory.Join(club, p, 1, loan: true, new PlayerLoan(donor, 3, 4_321, false), managerSquad: true);
        FinishSeason(game);
        var poolBefore = game.TransferMarket.FreeAgents.Contains(p);
        Assert.False(poolBefore);

        game.StartNextSeason();

        Assert.False(p.OnLoan);
        Assert.Null(p.Loan);
        Assert.DoesNotContain(p, club.Players);
        // Frei heißt (Review R14KS-03): in keinem Kader, unter den Vertragslosen, ohne Vertrag (Besitzer frei, +0x1A = 6). Das
        // Auffüllen der Computervereine (Kader-KI) zieht ihn wegen des Alters nicht; eine Rückkehr in den Kader des
        // Computervereins (auch mit Vertrag 0) ist damit rot.
        Assert.DoesNotContain(game.Clubs, c => c.Players.Contains(p));
        Assert.Single(game.TransferMarket.FreeAgents, q => q == p);
        Assert.Equal((0, 0, false), (p.ContractYears, p.MonthlySalary, p.Insured));
    }

    /// <summary>
    /// [ORIGINAL] A115 (R14RD2-03): Leihe zwischen Managern – Rückkehr mit einer Kopie des Leih-Kaderplatzes (0x48:0x2BD4 alt → neu):
    /// Laufzeit heruntergezählt, Gehalt und Versicherung aus der Leihe, nicht der Stammvertrag; nach Spielerart einsortiert.
    /// </summary>
    [Fact]
    public void ManagerLoan_ReturnsWithLoanSlotContract_SortedIn()
    {
        var (game, origin, borrower) = TwoManagers();
        var p = Lend(origin, borrower, PlayerRole.Defender, years: 3, salary: 12_345, premium: 678);
        FinishSeason(game);

        game.StartNextSeason();

        Assert.Contains(p, origin.Players);
        Assert.DoesNotContain(p, borrower.Players);
        Assert.False(p.OnLoan);
        Assert.Null(p.Loan);
        Assert.Equal((2, 12_345, 678), (p.ContractYears, p.MonthlySalary, p.InsurancePremium));
        var at = origin.Players.IndexOf(p);
        Assert.All(origin.Players.Take(at), q => Assert.True(q.Role <= PlayerRole.Defender));
        Assert.All(origin.Players.Skip(at + 1), q => Assert.True(q.Role >= PlayerRole.Defender));
        Assert.Equal(Enumerable.Range(1, origin.Players.Count), origin.Players.Select(q => q.Number));
    }

    /// <summary>
    /// Leih-Kaderplatz mit 1 Jahr (wie jede Leihe des Remakes): nach dem Herunterzählen 0 → beim Verleiher wird verhandelt
    /// (Schleife ab Datei 0x27B64 über alle Kaderplätze der Manager); die Liste zeigt bis dahin das Leihgehalt (A362).
    /// </summary>
    [Fact]
    public void ManagerLoan_ExpiringLoanSlot_NegotiatedAtOrigin()
    {
        var (game, origin, borrower) = TwoManagers();
        var p = Lend(origin, borrower, PlayerRole.Forward, years: 1, salary: 12_345, premium: 0);
        FinishSeason(game);

        game.StartNextSeasonWithRenewals();

        Assert.Contains(p, origin.Players);
        Assert.True(GameState.IsRenewalPending(p));
        Assert.Contains(p, game.PendingRenewals(game.Managers[0]));
        Assert.DoesNotContain(p, game.PendingRenewals(game.Managers[1]));
        Assert.Equal(Math.Max(Contracts.MinSalary, game.RenewalSalaryThirds(game.Managers[0], p) / 3), p.MonthlySalary);
        Assert.Equal(12_345, game.SalaryBeforeRenewal(p));
    }

    /// <summary>Ohne Oberfläche endet die Verhandlung wie „Abbruch“: der Rückkehrer geht gegen Ablöse an den Verleiher.</summary>
    [Fact]
    public void ManagerLoan_ExpiringLoanSlot_WithoutScreen_LeavesOrigin()
    {
        var (game, origin, borrower) = TwoManagers();
        var p = Lend(origin, borrower, PlayerRole.Forward, years: 1, salary: 12_345, premium: 0);
        FinishSeason(game);

        game.StartNextSeason();

        Assert.DoesNotContain(p, origin.Players);
        Assert.DoesNotContain(p, borrower.Players);
        Assert.False(p.OnLoan);
        Assert.Equal(0, p.ContractYears);
    }

    /// <summary>
    /// [ORIGINAL] Zahlen des Saisonendes FILE44 → FILE46 (re-runde-14, „Saisonende-Ablauf“): KJELLBERG, Alter 33, Wert(Gehalt) 16.800 DM
    /// → Verhandlung 19.152 DM (<c>file46_3</c>), Wert(Preis) 311.000 DM → Ablöse 155.500 DM (<c>file46_5</c>); „10. Platz der
    /// Bundesliga“ mit einem Symbol → 100.000 DM Prämien (<c>file46_2</c>), keine Tabellenprämie.
    /// </summary>
    [Fact]
    public void SeasonEndNumbers_File44To46()
    {
        Assert.Equal(3 * 19_152, GameState.RenewalSalaryThirds(3 * 16_800, 33));
        Assert.Equal(155_500, GameState.RenewalCompensation(3 * 311_000L));
        Assert.Equal(0, GameState.Premium(League.Bundesliga, 10));
        Assert.Equal(100_000, GameState.SeasonPremium(League.Bundesliga, 10, [SeasonEvent.TopScorer]));
    }

    /// <summary>Verhandlung am Saisonende: Gehalt = Wert(Gehalt) · (147 − Alter nach dem Altern) / 100, Alter 32 → 33 wie KJELLBERG.</summary>
    [Fact]
    public void RenewalSalary_UsesAgeAfterAgeing()
    {
        var game = TestGames.Create(3);
        var manager = game.ActiveManager;
        var club = game.Clubs[manager.ClubIndex];
        FinishSeason(game);
        var p = club.Players.First(q => q.Role == PlayerRole.Forward && !q.OnLoan);
        p.Age = 32;
        p.ContractYears = 1;
        p.Notice = ContractNotice.None;
        var before = p.MonthlySalary;

        game.StartNextSeasonWithRenewals();

        Assert.Equal(33, p.Age);
        Assert.True(GameState.IsRenewalPending(p));
        var value = game.PlayerValueOf(p, club, PlayerValueKind.Salary, new GameRandom(0), manager.Level);
        Assert.Equal(Math.Max(Contracts.MinSalary, value * 114 / 100 / 3), p.MonthlySalary);
        Assert.Equal(before, game.SalaryBeforeRenewal(p));
    }

    /// <summary>
    /// [ORIGINAL] A088 (FILE44 → FILE46, Satz 0): Strafen +0x00/+0x01, Tore +0x03/+0x04 und Spiele +0x06/+0x07 → 0; die Zähler beim
    /// Verein (+0x1C–+0x26) bleiben.
    /// </summary>
    [Fact]
    public void SeasonChange_ResetsSeasonCounters_KeepsClubCounters()
    {
        var game = TestGames.Create(3);
        var club = game.Clubs[game.ActiveManager.ClubIndex];
        FinishSeason(game);
        var p = club.Players.First(q => q.Role == PlayerRole.Forward && !q.OnLoan && q.ContractYears > 1);
        p.Penalties2 = 4;
        p.Penalties5 = 1;
        p.CountGame(CompetitionSlot.Cup);
        p.CountGoal(CompetitionSlot.Cup);
        p.CountGoal(CompetitionSlot.League);
        var clubGames = Enumerable.Range(0, CompetitionSlots.Count).Select(i => p.ClubGames((CompetitionSlot)i)).ToArray();
        var clubGoals = Enumerable.Range(0, CompetitionSlots.Count).Select(i => p.ClubGoals((CompetitionSlot)i)).ToArray();
        Assert.True(clubGames[0] > 0 && clubGames[1] > 0 && clubGoals[1] > 0);

        game.StartNextSeason();

        Assert.Contains(p, club.Players);
        Assert.Equal((0, 0), (p.Penalties5, p.Penalties2));
        Assert.Equal((0, 0), (p.SeasonGoals(CompetitionSlot.League), p.SeasonGoals(CompetitionSlot.Cup)));
        Assert.Equal((0, 0), (p.SeasonGames(CompetitionSlot.League), p.SeasonGames(CompetitionSlot.Cup)));
        Assert.Equal((0, 0), (p.Games, p.Goals));
        Assert.Equal(clubGames, Enumerable.Range(0, CompetitionSlots.Count).Select(i => p.ClubGames((CompetitionSlot)i)));
        Assert.Equal(clubGoals, Enumerable.Range(0, CompetitionSlots.Count).Select(i => p.ClubGoals((CompetitionSlot)i)));
    }

    /// <summary>
    /// [ORIGINAL] Listenpreis eines eigenen Markt-Platzes = Kaderplatz +0x2C (Kaufpreis, Datei 0x470C4): HOHENADL FILE40 316.000 DM →
    /// FILE41 Markt 316.000 DM, <c>file40_04</c> „316.000 DM (K)“ und „105.333 DM (L)“. Ohne Kaufpreis (Altstand) der Spielerwert.
    /// </summary>
    [Fact]
    public void Offer_ListPriceIsSlotPrice_LoanFeeThird()
    {
        var game = TestGames.Create(3);
        var manager = game.ActiveManager;
        var club = game.Clubs[manager.ClubIndex];
        var defenders = club.Players.Where(q => q.Role == PlayerRole.Defender && !q.OnLoan).ToArray();
        var p = defenders[^1];
        Assert.True(p.PurchasePrice > 0); // Spielstart: Wert(Preis) / 3 (Datei 0x29EB4)
        p.PurchasePrice = 316_000;

        Assert.Equal(TransferResult.Accepted, game.TransferMarket.Offer(game, manager, p));

        var listing = game.TransferMarket.ListingOf(p)!;
        Assert.Equal(316_000, listing.Price);
        Assert.Equal(105_333, listing.LoanFee);

        var q = defenders[^2];
        q.PurchasePrice = 0;
        Assert.Equal(TransferResult.Accepted, game.TransferMarket.Offer(game, manager, q));
        Assert.Equal(TransferMarket.Price(q), game.TransferMarket.ListingOf(q)!.Price);
    }

    /// <summary>
    /// Mehrsaisonprobe (AGENTS.md §5): vier Saisonwechsel mit zwei Managern, passiv (keine Leihe) und aktiv (je Saison zwei Leihen
    /// von Manager 0 an Manager 1, eine mit 1 Jahr, eine mit 2 Jahren, dazu eine Leihe vom Markt). Nach jedem Wechsel ist niemand mehr
    /// ausgeliehen, kein Spieler steht doppelt, die Nummern sind die Kaderplätze und die Rückkehrer mit Restlaufzeit stehen beim
    /// Verleiher.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FourSeasonChanges_LoansEnd_NoDuplicates(bool active)
    {
        var (game, origin, borrower) = TwoManagers(GameModeTests.Create(5, true, true, 5, 6));
        for (var season = 0; season < 4; season++)
        {
            var keep = new List<Player>();
            if (active)
            {
                if (origin.Players.Count(p => p.Role == PlayerRole.Forward) > 8 && borrower.Players.Count < 27)
                {
                    Lend(origin, borrower, PlayerRole.Forward, 1, 10_000, 0);
                    keep.Add(Lend(origin, borrower, PlayerRole.Forward, 2, 11_000, 500));
                }
                var donor = game.Clubs.First(c => !game.Managers.Any(m => m.ClubIndex == c.Index) && c.Index < 48
                                                 && c.Players.Count(p => p.Role == PlayerRole.Defender) > 4);
                var market = donor.Players.Last(p => p.Role == PlayerRole.Defender);
                market.Age = 25;
                donor.RemovePlayer(market);
                if (borrower.Players.Count < 29)
                    PlayerFactory.Join(borrower, market, 1, loan: true, new PlayerLoan(donor, 2, market.MonthlySalary, false));
                else
                    PlayerFactory.Join(donor, market, 2, loan: false, managerSquad: false);
            }
            FinishSeason(game);
            game.StartNextSeason();

            var squads = game.Clubs.SelectMany(c => c.Players).ToArray();
            Assert.Equal(squads.Length, squads.Distinct().Count());
            Assert.DoesNotContain(squads, p => p.OnLoan || p.Loan is not null);
            foreach (var club in new[] { origin, borrower })
                Assert.Equal(Enumerable.Range(1, club.Players.Count), club.Players.Select(q => q.Number));
            foreach (var p in keep)
                Assert.True(origin.Players.Contains(p) || game.RetiredPlayers.Contains(p), $"Saison {season}: {p.Name} fehlt");
            Assert.All(game.Managers, m => Assert.False(m.Dismissed));
        }
    }
}
