using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur <c>transfer-original</c>: Transferregeln nach em.exe Ov 0x118 (<see cref="TransferRules"/>, <c>docs/transfer.md</c>).
/// Zufallsabhängige Regeln werden mit einem Zwillings-Zufallsstrom (<see cref="GameRandom.FromState"/>) nachgerechnet.
/// </summary>
public class TransferOriginalTests
{
    private static Player NewPlayer(PlayerRole role, int c, int t, int f, int age, PlayerTrend trend) =>
        new() { Name = "TEST", Role = role, Condition = c, Technique = t, Form = f, Age = age, Trend = trend };

    /// <summary>
    /// Die sechs Preise der Transferliste im Video (oberflaeche_1102, Spielbeginn: keine Spiele, Tore, Strafen) folgen exakt
    /// aus dem Spielerwert; die Tendenz (Start random(35, 65)) ist je Spieler eine der drei Stufen des Remakes.
    /// Vorher rot: die alte Kalibrierung lag bis 8.000 DM daneben (LAPORTE 339.000 statt 338.000).
    /// </summary>
    [Theory]
    [InlineData(PlayerRole.Goalkeeper, 25, 37, 48, 26, PlayerTrend.Constant, 231_000)] // LANGE
    [InlineData(PlayerRole.Defender, 37, 31, 55, 23, PlayerTrend.Constant, 172_000)] // TROJAN
    [InlineData(PlayerRole.Defender, 16, 20, 53, 18, PlayerTrend.Constant, 150_000)] // HANFT
    [InlineData(PlayerRole.Defender, 47, 40, 47, 23, PlayerTrend.Rising, 205_000)] // J.REIL
    [InlineData(PlayerRole.Forward, 21, 31, 54, 31, PlayerTrend.Falling, 100_000)] // IBELHERR
    [InlineData(PlayerRole.Forward, 76, 79, 47, 18, PlayerTrend.Constant, 338_000)] // LAPORTE
    public void Price_MatchesVideo_Exactly(PlayerRole role, int c, int t, int f, int age, PlayerTrend trend, long video)
    {
        var player = NewPlayer(role, c, t, f, age, trend);
        Assert.Equal(video, TransferMarket.Price(player));
        var game = TestGames.Create();
        Assert.Equal(video / 3, new TransferListing { Player = player, From = game.Clubs[0], Price = video }.LoanFee);
    }

    /// <summary>
    /// Annahme vom Computermarkt (0x4991D): bei 80 % des Preises wird ein Gebot manchmal angenommen (random(75, 85) ≤ 80 und
    /// random(70, 120) &lt; 80). Vorher rot: die alte Regel lehnte alles bis 85 % ab.
    /// </summary>
    [Fact]
    public void Buy_FromComputerMarket_FollowsOriginalAcceptance()
    {
        var accepted = 0;
        for (var seed = 1; seed <= 60; seed++)
        {
            var game = TestGames.Create(seed);
            var manager = game.ActiveManager;
            manager.Money = 10_000_000;
            var listing = game.TransferMarket.Listings[0];
            var offer = listing.Price * 8 / 10;
            var twin = GameRandom.FromState(game.Random.Capture());
            var expected = TransferRules.Accepts(TransferRules.Thirds(listing.Price), TransferRules.Thirds(offer), false, twin);
            if (expected)
                _ = ComputerClubTransferRules.SponsorSubsidy(offer, twin); // Kader-Transfer Teil 3: Zuschuss 0x47A34

            var result = game.TransferMarket.Buy(game, manager, listing, offer, loan: false);

            Assert.Equal(expected ? TransferResult.Accepted : TransferResult.Rejected, result);
            Assert.Equal(twin.Capture(), game.Random.Capture());
            if (expected)
                accepted++;
        }
        Assert.InRange(accepted, 1, 59);
    }

    [Fact]
    public void Accepts_Thresholds()
    {
        const int price = 900_000; // 300.000 DM
        for (var seed = 0; seed < 200; seed++)
        {
            Assert.False(TransferRules.Accepts(price, price * 74 / 100, false, new Random(seed))); // unter 75 % nie
            Assert.True(TransferRules.Accepts(price, price * 131 / 100, false, new Random(seed))); // über 130 % immer
            Assert.True(TransferRules.Accepts(price, price / 3 * 131 / 100, true, new Random(seed))); // Leihe: Preis / 3
            Assert.False(TransferRules.Accepts(price, price / 3 * 74 / 100, true, new Random(seed)));
        }
        // Bit 31 (+0x2C) zählt nicht zum Preis.
        Assert.True(TransferRules.Accepts(price | int.MinValue, price * 131 / 100, false, new Random(1)));
    }

    /// <summary>Zugfolge 0x4991D: random(75, 85), random(110, 130), random(70, 120) – nur so weit wie nötig.</summary>
    [Fact]
    public void Accepts_DrawsInOriginalOrder()
    {
        const int price = 900_000;
        for (var seed = 0; seed < 300; seed++)
        {
            foreach (var offer in new[] { 650_000, 720_000, 800_000, 900_000, 1_050_000, 1_150_000, 1_200_000 })
            {
                var r = new Random(seed);
                var twin = new Random(seed);
                var actual = TransferRules.Accepts(price, offer, false, r);
                bool expected;
                if (price * SquadSetup.Between(twin, 75, 85) / 100 > offer)
                    expected = false;
                else if (price * SquadSetup.Between(twin, 110, 130) / 100 < offer)
                    expected = true;
                else
                    expected = SquadSetup.Between(twin, 70, 120) < offer * 100 / price;
                Assert.Equal(expected, actual);
                Assert.Equal(twin.Next(), r.Next());
            }
        }
    }

    /// <summary>
    /// Gebot für den Spieler eines anderen Managers (0x47582): unter 60 % oder über 140 % des frischen Werts „unfair“.
    /// Vorher rot: 150 % wurden angenommen.
    /// </summary>
    [Fact]
    public void Buy_FromOtherManager_OutsideSixtyToHundredFortyPercent_IsUnfair()
    {
        var game = TwoManagerGame();
        var seller = game.Managers[1];
        var buyer = game.Managers[0];
        buyer.Money = 10_000_000;
        var sellerClub = game.Clubs[seller.ClubIndex];
        var player = sellerClub.Players.First(p => TransferMarket.CanLeave(sellerClub, p));
        Assert.Equal(TransferResult.Accepted, game.TransferMarket.Offer(game, seller, player));
        var listing = game.TransferMarket.ListingOf(player)!;
        var value = PlayerValue.Compute(PlayerValue.Inputs(player, sellerClub), PlayerValueKind.Price, (_, _) => 0) / 3;

        Assert.Equal(TransferResult.Unfair, game.TransferMarket.Buy(game, buyer, listing, value * 3 / 2, loan: false));
        Assert.Equal(TransferResult.Unfair, game.TransferMarket.Buy(game, buyer, listing, value / 2, loan: false));
        Assert.Equal(TransferResult.Unfair, game.TransferMarket.Buy(game, buyer, listing, value / 3 * 3 / 2, loan: true));
        Assert.Contains(listing, game.TransferMarket.Listings);
        Assert.Equal(TransferResult.Accepted, game.TransferMarket.Buy(game, buyer, listing, value * 14 / 10, loan: false));
    }

    [Theory]
    [InlineData(300_000, 180_000, true)]
    [InlineData(300_000, 179_999, false)]
    [InlineData(300_000, 420_000, true)]
    [InlineData(300_000, 420_001, false)]
    public void IsFair_Bounds(int value, int offer, bool fair) => Assert.Equal(fair, TransferRules.IsFair(value, offer, false));

    /// <summary>Tageswurf 0x072DD: w = Wert / 10000; Angebot bei random(0, 180) &lt; w und random(0, 2) ≠ 0.</summary>
    [Fact]
    public void BidRoll_FollowsOriginal()
    {
        for (var seed = 0; seed < 300; seed++)
        {
            foreach (var value in new[] { 0, 90_000, 600_000, 1_305_000, 3_000_000 })
            {
                var r = new Random(seed);
                var twin = new Random(seed);
                var expected = SquadSetup.Between(twin, 0, 180) < value / 10_000 && SquadSetup.Between(twin, 0, 2) != 0;
                Assert.Equal(expected, TransferRules.BidRoll(value, r));
                Assert.Equal(twin.Next(), r.Next());
            }
        }
        Assert.False(TransferRules.BidRoll(9_999, new Random(5)));
    }

    /// <summary>
    /// Höhe des Angebots 0x47347: V + random(V/20, V/10) − V/15, dann · random(85, 115) / 30000 · 300 – Grenzen von random
    /// als 16-Bit-Wörter (bei großen Werten abgeschnitten, 0xC037 tauscht und rechnet modulo).
    /// </summary>
    [Fact]
    public void BidAmount_FollowsOriginal()
    {
        for (var seed = 0; seed < 300; seed++)
        {
            var v = 300_000;
            var twin = new Random(seed);
            var a = v + (SquadSetup.Between(twin, v / 20, v / 10) - v / 15);
            var expected = a * SquadSetup.Between(twin, 85, 115) / 30_000 * 300;
            Assert.Equal(expected, TransferRules.BidAmountThirds(v, new Random(seed)));
            Assert.Equal(0, expected % 300);
        }
        // V = 1.305.000: V/10 = 130.500 → Wort −572, V/20 = 65.250 → Wort −286; random(−572, −286).
        for (var seed = 0; seed < 50; seed++)
            Assert.InRange(TransferRules.Random16(new Random(seed), 65_250, 130_500), -572, -286);
    }

    [Fact]
    public void Random16_SwapsAndTruncates()
    {
        for (var seed = 0; seed < 50; seed++)
        {
            Assert.InRange(TransferRules.Random16(new Random(seed), 100, 95), 95, 100);
            Assert.Equal(SquadSetup.Between(new Random(seed), 75, 85), TransferRules.Random16(new Random(seed), 75, 85));
            // Spanne über 32767 (idiv mit negativem Teiler im Original)
            Assert.InRange(TransferRules.Random16(new Random(seed), -20_000, 20_000), -20_000, 20_000);
        }
    }

    /// <summary>
    /// Angebote für eigene Spieler auf dem Markt: Höhe nach 0x47347 (Wert mit Angebot · random(95, 100) / 100, dann
    /// · (1 + [1/20, 1/10] − 1/15) · [85, 115] %, auf 100 DM). Vorher rot: 80–110 % des Preises auf volle 1.000 DM.
    /// </summary>
    [Fact]
    public void OwnListing_Bids_FollowOriginalAmount()
    {
        var notThousands = 0;
        for (var seed = 1; seed <= 12; seed++)
        {
            var game = TestGames.Create(seed);
            var manager = game.ActiveManager;
            var club = game.Clubs[manager.ClubIndex];
            var player = club.Players.OrderByDescending(p => p.Condition + p.Technique).First(p => TransferMarket.CanLeave(club, p));
            Assert.Equal(TransferResult.Accepted, game.TransferMarket.Offer(game, manager, player));
            var market = game.TransferMarket;
            for (var day = 0; day < 300 && market.Bids.Count == 0; day++)
                market.AdvanceDay(game, game.Random);

            var bid = Assert.Single(market.Bids);
            Assert.Same(player, bid.Listing.Player);
            Assert.Equal(0, bid.Amount % 100);
            if (bid.Amount % 1000 != 0)
                notThousands++;
            long value = TransferRules.PriceThirds(player);
            // Untergrenze (Teil 5 nachgerechnet, Seed 11): Wert · 95 % auf 3000 Drittel abgerundet, random(V/20, V/10) mit
            // 16-Bit-Grenzen (0x47379, bei V ab 655.360 Drittel abgeschnitten, also bis −32.768), − V/15, · 85 %, 100 DM.
            var low = value * 95 / 100 - 3000;
            Assert.InRange(bid.Amount, (low - 32_768 - low / 15) * 85 / 100 / 3 - 100, value * 104 / 100 * 115 / 100 / 3);
        }
        Assert.True(notThousands > 0);
    }

    /// <summary>
    /// Original-Spielstände FILE03/05/07: Preis (+0x2C) und Gehalt (+0x28) aller Plätze des Transfermarkts (Satz 4) sind
    /// genau der Spielerwert aus den gespeicherten Eingängen (Hinweis Review #247). Die Liste zeigt +0x2C / 3 als „(K)“ und
    /// (+0x2C / 3) / 3 als „(L)“ (0x4628B, 0x462EB).
    /// </summary>
    [GameDataFact]
    public void OriginalSaves_MarketPricesAndSalaries_AreThePlayerValue()
    {
        var checkedSlots = 0;
        foreach (var name in new[] { "FILE03.HOK", "FILE05.HOK", "FILE07.HOK" })
        {
            var save = OriginalSaveGame.Read(File.ReadAllBytes(OriginalSave(name)));
            var squad = save.Block("5DC3");
            var pool = save.Block("far:8253");
            var level = (sbyte)save.Block("0ED8")[0];
            for (var i = 0; i < OriginalSaveGame.SquadSlotsPerManager; i++)
            {
                var slot = squad.AsSpan(4 * 0x618 + i * OriginalSaveGame.SquadSlotLength, OriginalSaveGame.SquadSlotLength);
                if (!slot.ContainsAnyExcept((byte)0))
                    continue;
                var player = pool.AsSpan(slot[0x0F] * OriginalSaveGame.PoolRecordLength, OriginalSaveGame.PoolRecordLength);
                var inputs = new PlayerValueInputs
                {
                    Penalties5 = slot[0x00],
                    Penalties2 = slot[0x01],
                    Goals0 = slot[0x03],
                    Goals1 = slot[0x04],
                    Goals2 = slot[0x05],
                    Trend = slot[0x0E],
                    Condition = slot[0x10],
                    Technique = slot[0x11],
                    Games0 = BitConverter.ToUInt16(slot[0x1C..]),
                    Games1 = BitConverter.ToUInt16(slot[0x1E..]),
                    Games2 = BitConverter.ToUInt16(slot[0x20..]),
                    OfferClub = (slot[0x09] & 0x80) != 0 ? slot[0x16] : null,
                    Age = player[0x14],
                    Goalkeeper = player[0x18] == 0,
                    ClubClass = OriginalClass(player[0x1D]),
                    LevelValue = level,
                };
                Assert.Null(inputs.OfferClub);
                var price = BitConverter.ToInt32(slot[0x2C..]) & 0x7FFF_FFFF;
                Assert.Equal(price, PlayerValue.Compute(inputs, PlayerValueKind.Price, (_, _) => throw new InvalidOperationException()));
                Assert.Equal(BitConverter.ToInt32(slot[0x28..]), PlayerValue.Compute(inputs, PlayerValueKind.Salary, (_, _) => 0));
                checkedSlots++;
            }
        }
        Assert.Equal(20, checkedSlots);
    }

    /// <summary>
    /// Mehrsaisonprobe (Kader und Geld, 4 Saisonwechsel): passiver Manager und aktiver Manager, der eigene Spieler anbietet,
    /// Angebote annimmt und vom Markt kauft. Geprüft: Geldfluss genau zum Betrag, Preise auf 1.000 DM, Angebote auf 100 DM,
    /// Kader in den Grenzen, Markt nach jedem Wechsel neu.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MultiSeason_Transfers_StayConsistent(bool active)
    {
        var game = TestGames.Create(21);
        var manager = game.ActiveManager;
        var club = game.Clubs[manager.ClubIndex];
        var market = game.TransferMarket;
        int bought = 0, sold = 0;
        for (var change = 0; change < 4; change++)
        {
            for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
            {
                Assert.True(guard < 1000, "Saisonende nicht erreicht");
                TestSeason.KeepSquad(game, manager);
                if (manager.Money < 2_000_000)
                    manager.Money = 2_000_000; // keine Entlassung im Testlauf
                if (active && game.Phase == SeasonPhase.Running)
                    Act();
                Assert.All(market.Listings.Where(l => !l.IsFreeAgent), l => Assert.Equal(0, l.Price % 1000));
                Assert.All(market.Bids, b => Assert.Equal(0, b.Amount % 100));
                Assert.InRange(club.Players.Count, 1, TransferMarket.MaxSquad);
                game.Continue();
            }
            game.StartNextSeason();
            Assert.Empty(market.Bids);
            Assert.DoesNotContain(market.Listings, l => l.Seller is not null);
        }
        if (active)
        {
            Assert.True(bought > 0, "kein Kauf");
            Assert.True(sold > 0, "kein Verkauf");
        }
        else
            Assert.Equal(0, bought + sold);

        void Act()
        {
            foreach (var bid in market.Bids.Where(b => b.Listing.Seller == manager).ToList())
            {
                var before = manager.Money;
                var fee = TransferMarket.TransferFee(bid); // vor dem Wechsel: Restlaufzeit des Verkäufers
                if (market.Accept(game, bid) == TransferResult.Accepted)
                {
                    Assert.Equal(before + fee, manager.Money); // Teil 5: 0x49332
                    sold++;
                }
            }
            if (market.Listings.Count(l => l.Seller == manager) < 2 && market.Listings.Count < TransferMarket.MaxListings)
            {
                var offer = club.Players.Where(p => !p.OnLoan && TransferMarket.CanLeave(club, p) && market.ListingOf(p) is null)
                    .OrderBy(p => p.Condition + p.Technique).FirstOrDefault();
                if (offer is not null)
                    market.Offer(game, manager, offer);
            }
            if (club.Players.Count < 20 && market.Listings.FirstOrDefault(l => l.Seller is null && !l.IsFreeAgent) is { } listing)
            {
                var amount = listing.Price * 135 / 100;
                var before = manager.Money;
                var result = market.Buy(game, manager, listing, amount, loan: false);
                Assert.Equal(TransferResult.Accepted, result); // über 130 % immer angenommen
                Assert.Equal(before - amount, manager.Money);
                Assert.Contains(listing.Player, club.Players);
                bought++;
            }
        }
    }

    /// <summary>Spielklasse nach Vereinsindex. [ORIGINAL] em.exe 0x34FD5 mit DGROUP 0x7C1 = 12, 0x7C2 = 24, 0x6AD = 52.</summary>
    private static int OriginalClass(int club) => club < 12 || club > 52 ? 1 : club < 24 ? 2 : 4;

    private static GameState TwoManagerGame()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "A", Head = 0, ClubIndex = 2 });
        setup.Managers.Add(new ManagerSetup { Name = "B", Head = 1, ClubIndex = 5 });
        return NewGame.Create(TestGames.CreateDatabase(), setup, 11);
    }

    /// <summary>Original-Spielstand: <c>EM_SAVEGAMES</c> oder <c>spielstaende</c> neben dem aufgelösten <c>gamedata</c>.</summary>
    internal static string OriginalSave(string name)
    {
        var folder = Environment.GetEnvironmentVariable("EM_SAVEGAMES");
        if (string.IsNullOrEmpty(folder))
        {
            var root = TestGameData.Load().RootDirectory;
            var real = Directory.ResolveLinkTarget(root.TrimEnd('/', '\\'), true)?.FullName ?? root;
            folder = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(real).TrimEnd('/', '\\'))!, "spielstaende");
        }
        var path = Path.Combine(folder, name);
        return File.Exists(path) ? path
            : throw new FileNotFoundException($"Original-Spielstand {name} fehlt in {folder} (Ordner per EM_SAVEGAMES angeben).");
    }
}
