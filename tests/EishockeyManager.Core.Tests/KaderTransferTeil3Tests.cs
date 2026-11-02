using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur <c>kader-transfer</c>, Teil 3: Transferfolgen beim Computerverein (Inventur F005, F006; P1 Nr. 6). Teamwerte des
/// Käufers nach einem Verkauf (em.exe 0x058DC) und Sponsorzuschuss nach einem Kauf vom Computermarkt (0x063BE).
/// Vorher rot: Verkäufe änderten keine Teamwerte, nach einem Kauf gab es keinen Zuschuss.
/// </summary>
public class KaderTransferTeil3Tests
{
    /// <summary>
    /// 0x058DC: a = Mittel, d = (s − a + 10) / 3, Teil random(0, 1), d auf 1 … 10, dann Kondition und Technik des Teils je
    /// + random(d − 1, d + 1) – Züge in dieser Reihenfolge und Grenzen.
    /// </summary>
    [Theory]
    [InlineData(80, 1, 11, 9, 10)] // d = 30 / 3 = 10
    [InlineData(65, 0, 4, 6, 5)] // d = 15 / 3 = 5
    [InlineData(20, 0, 0, 2, 1)] // d = −30 / 3 → 1
    [InlineData(48, 1, 1, 1, 1)] // s − a + 10 = −2 → −2 / 3 = 0 (gegen null) → 1
    public void StrengthenBuyer_FollowsOriginal(int playerStrength, int part, int condition, int technique, int d)
    {
        var values = new ClubTeamValues(2, -2, 1, -1, 50, 50, 50); // Mittel (60·4 + 0) / 4 = 60
        var random = new Scripted(part, condition, technique);

        var after = ComputerClubTransferRules.StrengthenBuyer(values, 60, playerStrength, random);

        Assert.Equal([(0, 1), (d - 1, d + 1), (d - 1, d + 1)], random.Calls);
        var expected = part == 0
            ? values with { DefenseCondition = 2 + condition, DefenseTechnique = 1 + technique }
            : values with { AttackCondition = -2 + condition, AttackTechnique = -1 + technique };
        Assert.Equal(expected, after);
    }

    /// <summary>
    /// 0x063BE: random(0, 6) ≠ 0 → kein Zuschuss (ein Zug); sonst Gebot · random(25, 65) / 30000 · 300 Drittel-DM.
    /// Goldwert FILE05 → FILE07: Gebot 400.000 DM, Zuschuss 220.000 DM (Faktor 55).
    /// </summary>
    [Fact]
    public void SponsorSubsidy_FollowsOriginal()
    {
        var none = new Scripted(3);
        Assert.Null(ComputerClubTransferRules.SponsorSubsidy(400_000, none));
        Assert.Equal([(0, 6)], none.Calls);

        var gold = new Scripted(0, 55);
        Assert.Equal(220_000, ComputerClubTransferRules.SponsorSubsidy(400_000, gold));
        Assert.Equal([(0, 6), (25, 65)], gold.Calls);

        // Volle 100 DM: 12.345 DM · 3 · 25 / 30000 = 30 → 30 · 300 Drittel = 3.000 DM.
        Assert.Equal(3_000, ComputerClubTransferRules.SponsorSubsidy(12_345, new Scripted(0, 25)));
    }

    /// <summary>
    /// Gebot auf einen eigenen Spieler am Markt angenommen (0x47483): Kondition und Technik genau eines Teils des bietenden
    /// Vereins steigen um je d − 1 … d + 1.
    /// </summary>
    [Fact]
    public void MarketBidAccepted_StrengthensBidder()
    {
        var game = TestGames.Create();
        MatchEngine.LoadTeamValues(game);
        var manager = game.ActiveManager;
        var market = game.TransferMarket;
        var club = game.Clubs[manager.ClubIndex];
        var player = club.Players.First(p => p.Role == PlayerRole.Forward);
        market.Offer(game, manager, player);
        for (var day = 0; day < 40 && market.Bids.Count == 0; day++)
            market.AdvanceDay(game, game.Random);
        var bid = Assert.Single(market.Bids);
        var before = bid.Buyer.TeamValues!.Value;

        Assert.Equal(TransferResult.Accepted, market.Accept(game, bid));

        AssertStrengthened(before, bid.Buyer.TeamValues!.Value, bid.Buyer.Strength, player.Strength);
    }

    /// <summary>
    /// Unaufgefordertes Angebot verkauft (0x46FB3): Verein &lt; 0x40 wird stärker; ein Europapokal-Gegner nicht (Prüfung
    /// 0x46F89), sein Zufall bleibt unberührt.
    /// </summary>
    [Fact]
    public void UnsolicitedOfferSold_StrengthensDomesticBuyerOnly()
    {
        var game = TestGames.Create();
        MatchEngine.LoadTeamValues(game);
        var manager = game.ActiveManager;
        var club = game.Clubs[manager.ClubIndex];
        // Schwächster Verein, stärkster Spieler: d ≥ 2, die Wirkung ist also nie null.
        var buyer = game.Clubs.Where(c => c.Index != manager.ClubIndex && c.Index < NationalTeams.First
            && game.Managers.All(m => m.ClubIndex != c.Index)).MinBy(c => c.Strength)!;
        var player = club.Players.MaxBy(p => p.Strength)!;
        var before = buyer.TeamValues!.Value;
        Assert.True(player.Strength - buyer.Strength >= -4, $"{player.Strength} gegen {buyer.Strength}");

        Assert.Equal(TransferResult.Accepted, game.AcceptUnsolicitedOffer(new UnsolicitedOffer(manager, player, buyer, 10_000)));
        AssertStrengthened(before, buyer.TeamValues!.Value, buyer.Strength, player.Strength);
        Assert.NotEqual(before, buyer.TeamValues!.Value);

        if (game.Clubs.FirstOrDefault(c => c.Index >= UnsolicitedOfferRules.FirstForeignClub) is not { } foreign)
            return;
        var second = club.Players.First(p => p.Role == PlayerRole.Forward);
        var foreignBefore = foreign.TeamValues;
        var state = game.Random.Capture();
        Assert.Equal(TransferResult.Accepted, game.AcceptUnsolicitedOffer(new UnsolicitedOffer(manager, second, foreign, 10_000)));
        Assert.Equal(foreignBefore, foreign.TeamValues);
        Assert.Equal(state, game.Random.Capture());
    }

    /// <summary>Vor dem Ziehen der Teamwerte (neues Spiel ohne Spiel) bleibt der Käufer ohne Werte und kein Zufall fällt.</summary>
    [Fact]
    public void WithoutTeamValues_NothingChanges()
    {
        var game = TestGames.Create();
        var manager = game.ActiveManager;
        var club = game.Clubs[manager.ClubIndex];
        var buyer = game.Clubs.First(c => c.Index != manager.ClubIndex && c.Index < NationalTeams.First
            && game.Managers.All(m => m.ClubIndex != c.Index));
        Assert.Null(buyer.TeamValues);
        var state = game.Random.Capture();
        var player = club.Players.First(p => p.Role == PlayerRole.Forward);
        Assert.Equal(TransferResult.Accepted, game.AcceptUnsolicitedOffer(new UnsolicitedOffer(manager, player, buyer, 10_000)));
        Assert.Null(buyer.TeamValues);
        Assert.Equal(state, game.Random.Capture());
    }

    /// <summary>
    /// Leihe vom Computermarkt: in etwa jedem siebten Fall wird ein Zuschuss angeboten (volle 100 DM, 25–65 % des Betrags);
    /// „Aber immer!“ bucht ihn als Transfereinnahme, „Lieber nicht.“ nichts.
    /// </summary>
    [Fact]
    public void ComputerMarketLoan_OffersSubsidy_AnswerBooks()
    {
        var offered = 0;
        var accepted = false;
        var declined = false;
        for (var seed = 1; seed <= 60 && !(accepted && declined); seed++)
        {
            var game = TestGames.Create(seed);
            var manager = game.ActiveManager;
            manager.Money = 100_000_000;
            var market = game.TransferMarket;
            foreach (var listing in market.Listings.Where(l => l.Seller is null && !l.IsFreeAgent && l.LoanFee > 0).ToList())
            {
                var fee = listing.LoanFee;
                if (market.Bid(game, manager, listing, fee * 2, loan: true, out _, out var pending) != TransferResult.Accepted)
                    continue;
                if (pending is not { } subsidy)
                    continue;
                offered++;
                Assert.Same(manager, subsidy.Manager);
                Assert.Equal(0, subsidy.Amount % 100);
                Assert.InRange(subsidy.Amount, fee * 2 * 25 / 100 - 100, fee * 2 * 65 / 100);
                var money = manager.Money;
                var transfers = game.Finance(manager).Book.Current(FinanceCategory.Transfers);
                var accept = !accepted;
                if (accept)
                    game.AcceptSponsorSubsidy(subsidy);
                var gain = accept ? subsidy.Amount : 0;
                Assert.Equal(money + gain, manager.Money);
                Assert.Equal(transfers + gain, game.Finance(manager).Book.Current(FinanceCategory.Transfers));
                accepted |= accept;
                declined |= !accept;
                break;
            }
        }
        Assert.True(accepted && declined, $"angeboten: {offered}");
    }

    /// <summary>Kauf vom Markt eines anderen Managers (Rückfrage 0x4774E): kein Zuschuss, kein Zug dafür.</summary>
    [Fact]
    public void PurchaseFromManager_NoSubsidy()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "A", Head = 0, ClubIndex = 2, Level = 1 });
        setup.Managers.Add(new ManagerSetup { Name = "B", Head = 1, ClubIndex = 17, Level = 1 });
        for (var seed = 1; seed <= 20; seed++)
        {
            var game = NewGame.Create(TestGames.CreateDatabase(), setup, seed);
            var (a, b) = (game.Managers[0], game.Managers[1]);
            b.Money = 100_000_000;
            var market = game.TransferMarket;
            var player = game.Clubs[a.ClubIndex].Players.First(p => p.Role == PlayerRole.Forward);
            Assert.Equal(TransferResult.Accepted, market.Offer(game, a, player));
            var listing = market.ListingOf(player)!;
            var state = game.Random.Capture();
            if (market.Bid(game, b, listing, listing.LoanFee, loan: true, out _, out var subsidy) == TransferResult.Accepted)
            {
                Assert.Null(subsidy);
                Assert.Equal(state, game.Random.Capture()); // kein Wurf random(0, 6)
                return;
            }
        }
        Assert.Fail("keine Leihe zwischen Managern angenommen");
    }

    /// <summary>
    /// Mehrsaisonprobe (4 Saisonwechsel, passiv und aktiv): aktiv kauft der Manager je Saison vom Computermarkt (Zuschuss
    /// angenommen) und verkauft einen Spieler über ein Gebot am Markt (Teamwerte des Käufers nach 0x058DC); der Spielstand
    /// bleibt im Rundlauf gleich (kein neues Feld). Passiv nie ein Zuschuss.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MultiSeason_FourChanges_SubsidyAndSales(bool active)
    {
        var game = TestGames.Create(21);
        var manager = game.ActiveManager;
        var club = game.Clubs[manager.ClubIndex];
        var market = game.TransferMarket;
        var (bought, sold, subsidies) = (0, 0, 0L);
        for (var change = 0; change < 4; change++)
        {
            var boughtThisSeason = false;
            Player? selling = null;
            for (var day = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; day++)
            {
                Assert.True(day < 1000, "Saisonende nicht erreicht");
                TestSeason.KeepSquad(game, manager);
                manager.Money = Math.Max(manager.Money, 5_000_000);
                if (active && game.Phase == SeasonPhase.Running)
                {
                    if (!boughtThisSeason && club.Players.Count < TransferMarket.MaxSquad
                        && market.Listings.FirstOrDefault(l => !l.IsFreeAgent && l.Seller is null && l.From.Index != manager.ClubIndex) is { } listing
                        && market.Bid(game, manager, listing, listing.Price * 2, loan: false, out var purchase) == TransferResult.Accepted
                        && market.Sign(game, purchase!, 0, 0, insured: false, demandFilled: false, out var subsidy) == NegotiationResult.Accepted)
                    {
                        bought++;
                        boughtThisSeason = true;
                        if (subsidy is not null)
                        {
                            var money = manager.Money;
                            game.AcceptSponsorSubsidy(subsidy);
                            Assert.Equal(money + subsidy.Amount, manager.Money);
                            subsidies += subsidy.Amount;
                        }
                    }
                    if (selling is null && club.Players.FirstOrDefault(p => p.Role == PlayerRole.Forward && !p.OnLoan) is { } forward
                        && market.Offer(game, manager, forward) == TransferResult.Accepted)
                        selling = forward;
                    if (market.Bids.FirstOrDefault(b => b.Listing.Seller == manager) is { } bid)
                    {
                        var buyer = bid.Buyer;
                        var before = buyer.TeamValues;
                        if (market.Accept(game, bid) == TransferResult.Accepted)
                        {
                            sold++;
                            if (before is { } values)
                                AssertStrengthened(values, buyer.TeamValues!.Value, buyer.Strength, bid.Listing.Player.Strength);
                        }
                    }
                }
                game.Continue();
            }
            game.StartNextSeason();
            var saved = SaveGame.Save(game);
            var loaded = SaveGame.Load(saved);
            Assert.True(loaded.Success, loaded.Error);
            Assert.Equal(saved, SaveGame.Save(loaded.Game!));
        }
        if (active)
        {
            Assert.True(bought >= 3, $"nur {bought} Käufe");
            Assert.True(sold >= 1, "kein Verkauf");
        }
        else
            Assert.Equal(0, subsidies);
    }

    private static void AssertStrengthened(ClubTeamValues before, ClubTeamValues after, int clubStrength, int playerStrength)
    {
        var mean = (4 * clubStrength + before.DefenseCondition + before.AttackCondition + before.DefenseTechnique
            + before.AttackTechnique) / 4;
        var d = Math.Clamp((playerStrength - mean + 10) / 3, 1, 10);
        Assert.Equal((before.DefenseForm, before.AttackForm, before.DayForm), (after.DefenseForm, after.AttackForm, after.DayForm));
        var defense = (after.DefenseCondition - before.DefenseCondition, after.DefenseTechnique - before.DefenseTechnique);
        var attack = (after.AttackCondition - before.AttackCondition, after.AttackTechnique - before.AttackTechnique);
        var (changed, kept) = defense == (0, 0) ? (attack, defense) : (defense, attack);
        Assert.Equal((0, 0), kept);
        Assert.InRange(changed.Item1, d - 1, d + 1);
        Assert.InRange(changed.Item2, d - 1, d + 1);
    }

    /// <summary>Zufall mit vorgegebenen Ergebnissen; prüft die Grenzen und merkt sich die Aufrufe (a, b) von random(a, b).</summary>
    private sealed class Scripted(params int[] values) : Random
    {
        private int _i;

        public List<(int, int)> Calls { get; } = [];

        public override int Next(int minValue, int maxValue)
        {
            Calls.Add((minValue, maxValue - 1));
            var v = values[_i++];
            Assert.InRange(v, minValue, maxValue - 1);
            return v;
        }
    }
}
