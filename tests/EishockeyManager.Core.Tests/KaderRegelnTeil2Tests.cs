using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur <c>kader-regeln</c>, Runde 22, Teil 2: Gehalt geliehener Spieler (A118) und Forderung aus Stärke und Talent nur noch für
/// Computervereine (A120). [ORIGINAL] Leihe Datei 0x477E7 → 0x42CB1 mit Argument 99: +0x28 = Wert(Gehalt) · random(90, 115) / 100
/// (Datei 0x42F11–0x42F65), gesichert Datei 0x4783A, nach der Kopie des Markt-Platzes (0x47907) zurückgeschrieben (0x47929–0x47933).
/// </summary>
public class KaderRegelnTeil2Tests
{
    private static (GameState Game, Manager Manager, Club Club, TransferListing Listing) Market(int seed)
    {
        var game = TestGames.Create(seed);
        var manager = game.ActiveManager;
        manager.Money = 10_000_000;
        var club = game.Clubs[manager.ClubIndex];
        while (club.Players.Count >= TransferMarket.MaxSquad)
            club.Players.RemoveAt(club.Players.Count - 1);
        var listing = game.TransferMarket.Listings.First(l => !l.IsFreeAgent && l.Seller is null && l.From != club);
        return (game, manager, club, listing);
    }

    /// <summary>
    /// A118: Der geliehene Spieler bekommt Wert(Gehalt) des neuen Kaderplatzes · random(90, 115) / 100, gezogen nach Belastung und
    /// Tendenz des Platzes. Vorher: <see cref="Contracts.Demand"/> (25 · Stärke + 50 · Talent), der Wurf ging verloren.
    /// </summary>
    [Theory]
    [InlineData(3)]
    [InlineData(8)]
    [InlineData(21)]
    public void Loan_SalaryIsValueSalaryTimesRandomFactor(int seed)
    {
        var (game, manager, club, listing) = Market(seed);
        var p = listing.Player;
        var fee = listing.LoanFee * 2;
        long thirds = game.PlayerValueOf(p, club, PlayerValueKind.Salary, new GameRandom(1), manager.Level);
        Assert.True(thirds > 0);

        Assert.Equal(TransferResult.Accepted, game.TransferMarket.Bid(game, manager, listing, fee, loan: true, out _));

        // Geprüft werden Spanne und Formel; den einen Wurf und seine Stelle im Strom prüfen die beiden folgenden Tests.
        Assert.True(p.OnLoan);
        var low = (int)Math.Max(Contracts.MinSalary, thirds * Contracts.LoanSalaryMinPercent / 100 / 3);
        var high = (int)Math.Max(Contracts.MinSalary, thirds * Contracts.LoanSalaryMaxPercent / 100 / 3);
        Assert.InRange(p.MonthlySalary, low, high);
        var factors = Enumerable.Range(Contracts.LoanSalaryMinPercent, Contracts.LoanSalaryMaxPercent - Contracts.LoanSalaryMinPercent + 1)
            .Select(r => (int)Math.Max(Contracts.MinSalary, thirds * r / 100 / 3));
        Assert.Contains(p.MonthlySalary, factors);
    }

    /// <summary>
    /// A118, Formel einzeln: <see cref="Contracts.LoanSalary"/> zieht genau einen Wurf random(90, 115) wie
    /// <see cref="SquadSlotRules.ConsumeLoanSalary"/> und rechnet in Drittel-DM mit 32-Bit-Abschnitt.
    /// </summary>
    [Fact]
    public void LoanSalary_OneDraw_ThirdsTimesFactor()
    {
        var game = TestGames.Create(5);
        var manager = game.ActiveManager;
        var club = game.Clubs[manager.ClubIndex];
        var p = club.Players.First(x => x.Role == PlayerRole.Defender);
        long thirds = game.PlayerValueOf(p, club, PlayerValueKind.Salary, new GameRandom(1), manager.Level);
        for (var seed = 1; seed <= 40; seed++)
        {
            var random = new GameRandom(seed);
            var probe = new GameRandom(seed);
            var factor = TransferRules.Random16(probe, 90, 115);

            var salary = Contracts.LoanSalary(game, manager, p, random);

            Assert.Equal((int)Math.Max(Contracts.MinSalary, thirds * factor / 100 / 3), salary);
            Assert.Equal(probe.Capture(), random.Capture());
        }
    }

    /// <summary>
    /// Der Zug am gemeinsamen Strom bleibt wie bisher (Belastung, Tendenz, ein Wurf Leihgehalt, Zuschussfrage): Zwilling aus
    /// <see cref="SquadSlotRules.Roll"/> und <see cref="SquadSlotRules.ConsumeLoanSalary"/> wie in <c>KaderTransferTeil3Tests</c>.
    /// </summary>
    [Fact]
    public void Loan_RandomConsumptionUnchanged()
    {
        var (game, manager, _, listing) = Market(3);
        var plain = GameRandom.FromState(game.Random.Capture());
        var withSalary = GameRandom.FromState(game.Random.Capture());
        _ = SquadSlotRules.Roll(plain);
        SquadSlotRules.ConsumeLoanSalary(plain);
        _ = SquadSlotRules.Roll(withSalary);
        _ = Contracts.LoanSalary(game, manager, listing.Player, withSalary);
        Assert.Equal(plain.Capture(), withSalary.Capture());
    }

    /// <summary>
    /// A120: Kaderplätze der Manager tragen nach Kauf, Leihe und Talentsuche nie <see cref="Contracts.Demand"/>; nur Computervereine
    /// behalten die Forderung ([APPROX], ohne Gehaltsfeld im Original). Mehrere Leihen über Zufallsfolgen: mindestens eine weicht
    /// von der Forderung ab (vorher alle gleich).
    /// </summary>
    [Fact]
    public void Loan_SalaryNotDemand()
    {
        var differs = 0;
        for (var seed = 1; seed <= 12; seed++)
        {
            var (game, manager, _, listing) = Market(seed);
            var p = listing.Player;
            if (game.TransferMarket.Bid(game, manager, listing, listing.LoanFee * 2, loan: true, out _) != TransferResult.Accepted)
                continue;
            if (p.MonthlySalary != Contracts.Demand(p))
                differs++;
        }
        Assert.True(differs > 0);
    }

    /// <summary>
    /// Mehrsaisonprobe (4 Saisonwechsel, passiv und aktiv): aktiv leiht der Manager je Saison einen Spieler vom Computermarkt; sein
    /// Gehalt liegt in der Spanne Wert(Gehalt) · 90…115 % zum Zeitpunkt der Leihe, der Spielstand bleibt im Rundlauf gleich und alle
    /// Manager behalten denselben Level (Teil 1).
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MultiSeason_FourChanges_LoanSalaries(bool active)
    {
        var game = TestGames.Create(21);
        var manager = game.ActiveManager;
        var club = game.Clubs[manager.ClubIndex];
        var market = game.TransferMarket;
        var loans = 0;
        for (var change = 0; change < 4; change++)
        {
            var loanedThisSeason = false;
            var loanedPlayers = new List<Player>();
            for (var day = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; day++)
            {
                Assert.True(day < 1000, "Saisonende nicht erreicht");
                TestSeason.KeepSquad(game, manager);
                manager.Money = Math.Max(manager.Money, 5_000_000);
                if (active && !loanedThisSeason && game.Phase == SeasonPhase.Running && club.Players.Count < TransferMarket.MaxSquad
                    && market.Listings.FirstOrDefault(l => !l.IsFreeAgent && l.Seller is null && l.From.Index != manager.ClubIndex) is { } listing)
                {
                    var p = listing.Player;
                    long thirds = game.PlayerValueOf(p, club, PlayerValueKind.Salary, new GameRandom(1), manager.Level);
                    if (market.Bid(game, manager, listing, listing.LoanFee * 2, loan: true, out _) == TransferResult.Accepted)
                    {
                        loans++;
                        loanedThisSeason = true;
                        loanedPlayers.Add(p);
                        Assert.InRange(p.MonthlySalary, (int)Math.Max(Contracts.MinSalary, thirds * 90 / 100 / 3),
                            (int)Math.Max(Contracts.MinSalary, thirds * 115 / 100 / 3));
                    }
                }
                game.Continue();
            }
            game.StartNextSeason();
            Assert.All(game.Managers, m => Assert.Equal(game.Setup.Level, m.Level));
            // #941 (Review #940 P3-2): Nach Ablauf der Leihe ist der Spieler nicht mehr im Managerkader, nicht mehr verliehen
            // und trägt kein Leihgehalt mehr.
            foreach (var returned in loanedPlayers)
            {
                Assert.DoesNotContain(returned, club.Players);
                Assert.False(returned.OnLoan);
                Assert.Null(returned.Loan);
            }
            var saved = SaveGame.Save(game);
            var loaded = SaveGame.Load(saved);
            Assert.True(loaded.Success, loaded.Error);
            Assert.Equal(saved, SaveGame.Save(loaded.Game!));
        }
        if (active)
            Assert.True(loans >= 3, $"nur {loans} Leihen");
        else
            Assert.Equal(0, loans);
    }
}
