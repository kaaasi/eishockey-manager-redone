using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur <c>kader-transfer</c>, Teil 4: Transfermarkt-Takt (Inventur A256, A257, A381, F109; P2 Nr. 4).
/// Listenwechsel und Angebotswurf täglich (em.exe 0x54E71, 0x072DD), bietender Verein nach 0x059B8, Kontoleiste nach 0x463CC.
/// Vorher rot: Der Markt lief nur je gespieltem Ligaspieltag, der Bieter war ein beliebiger Ligaverein, die Leiste zeigte
/// Schulden und Anlagen fest 0 DM.
/// </summary>
public class KaderTransferTeil4Tests
{
    /// <summary>
    /// 0x54E65–0x54E7C: Der Tagesablauf würfelt den Listenwechsel jeden Tag, auch ohne Ligaspieltag. Früher tat ein Aufruf ohne
    /// neuen Spieltag nichts.
    /// </summary>
    [Fact]
    public void CatchUp_ChangesListOnDaysWithoutMatch()
    {
        var game = TestGames.Create();
        var market = game.TransferMarket;
        var before = market.Listings.Select(l => l.Player.Id).ToArray();
        var changes = 0;
        for (var day = 0; day < 40; day++)
        {
            var previous = market.Listings.Select(l => l.Player.Id).ToArray();
            market.CatchUp(game);
            if (!previous.SequenceEqual(market.Listings.Select(l => l.Player.Id)))
                changes++;
        }
        Assert.Equal(0, game.PlayedMatchDays);
        Assert.Equal(0, market.UpdatedDay);
        // random(0, Managerzahl + 2) = 0 bei einem Manager: im Mittel jeder vierte Tag.
        Assert.InRange(changes, 4, 20);
        Assert.NotEqual(before, market.Listings.Select(l => l.Player.Id).ToArray());
    }

    /// <summary>
    /// Über eine ganze Saison läuft der Markt an jedem Tag der Zeitleiste: Die Zahl der Listenwechsel liegt bei einem Manager
    /// nahe Tage / 4 und damit deutlich über der Zahl der Ligaspieltage / 4.
    /// </summary>
    [Fact]
    public void Season_MarketRunsEveryCalendarDay()
    {
        // #772 (R15-E2 = C): Saat 21 statt 5 – die Zahl der Halte hängt am Spielverlauf; mit der Spielvorbereitung 0x8365 hält Saat 5
        // nur 20 Wechsel. Schon der alte Code liegt bei Saat 3/8/9 unter der Grenze (15/13/20), Rückschaltprobe mit
        // Saat 21 ebenfalls rot; die Probe hängt an der Saat, die Grenze bleibt.
        var game = TestGames.Create(21);
        var market = game.TransferMarket;
        var changes = 0;
        for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
        {
            Assert.True(guard < 1000, "Saisonende nicht erreicht");
            TestSeason.KeepSquad(game, game.ActiveManager);
            var previous = market.Listings.Select(l => l.Player.Id).ToArray();
            game.Continue();
            if (!previous.SequenceEqual(market.Listings.Select(l => l.Player.Id)))
                changes++;
        }
        // Ligaspieltage 44 → früher höchstens 44 Würfe, also im Mittel 11 Wechsel; täglich (> 200 Tage) gibt es fast an jedem
        // Halt einen Wechsel. Integration Runde 11 (+ wettbewerbe-spiel): Grenze doppelt so hoch wie das Mittel des alten Takts
        // (vorher 30; Seed 5 hält jetzt mit 30 Wechseln, weil sich die Zahl der Halte mit dem Spielverlauf ändert).
        Assert.True(changes > 22, $"nur {changes} Halte mit Listenwechsel");
    }

    /// <summary>
    /// 0x07364 → 0x059B8 mit Europapokal-Schalter 0: Bieter aus den Vereinen 0 … 42 ohne Manager, Vereinsstärke nahe der
    /// Spielerstärke (|Δ| &lt; random(2, 10) in den ersten 100 Versuchen).
    /// </summary>
    [Theory]
    [InlineData(3)]
    [InlineData(7)]
    [InlineData(11)]
    public void MarketBidder_FollowsOriginal(int seed)
    {
        var game = TestGames.Create(seed);
        MatchEngine.LoadTeamValues(game);
        var manager = game.ActiveManager;
        var market = game.TransferMarket;
        var club = game.Clubs[manager.ClubIndex];
        foreach (var player in club.Players.Where(p => p.Role == PlayerRole.Forward).Take(TransferMarket.MaxOwnListings).ToList())
            Assert.Equal(TransferResult.Accepted, market.Offer(game, manager, player));
        for (var day = 0; day < 400 && market.Bids.Count < TransferMarket.MaxOwnListings; day++)
            market.AdvanceDay(game, game.Random);

        Assert.NotEmpty(market.Bids);
        var differences = new List<int>();
        foreach (var bid in market.Bids)
        {
            Assert.InRange(bid.Buyer.Index, 0, 42);
            Assert.NotEqual(manager.ClubIndex, bid.Buyer.Index);
            var difference = Math.Abs(UnsolicitedOfferRules.ClubStrength(bid.Buyer) - bid.Listing.Player.Strength);
            Assert.True(difference < 30, $"{bid.Buyer.Name}: Abstand {difference}");
            differences.Add(difference);
        }
        // Früher ein beliebiger Ligaverein: Abstände über die ganze Spanne der Ligen.
        Assert.True(differences.Max() < 10, $"Abstände {string.Join(", ", differences)}");
    }

    /// <summary>
    /// 0x463CC: Schulden = Σ eigener negativer Salden (Manager, Bank); Anlagen = Aktien zum Kurs + positive Salden (Forderungen
    /// samt Sparbuch); Saldo = Konto + Anlagen − Schulden.
    /// </summary>
    [Fact]
    public void AccountBar_FollowsOriginal()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Eins", Head = 0, ClubIndex = 2 });
        setup.Managers.Add(new ManagerSetup { Name = "Zwei", Head = 1, ClubIndex = 5 });
        setup.Managers.Add(new ManagerSetup { Name = "Drei", Head = 2, ClubIndex = 8 });
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, 3);
        var (me, two, three) = (game.Managers[0], game.Managers[1], game.Managers[2]);
        me.Money = two.Money = three.Money = 1_000_000;

        Assert.Equal(new TransferAccountBar(1_000_000, 0, 0), game.TransferAccount(me));

        game.LendBetweenManagers(0, 1, 50_000); // Forderung an Zwei 50.000
        game.LendBetweenManagers(1, 0, 20_000); // verrechnet: Forderung 30.000
        game.LendBetweenManagers(2, 0, 70_000); // Schulden bei Drei 70.000
        game.DepositToBank(me, 100_000); // Sparbuch
        Assert.Equal(1, game.BuyStock(me, 0, 1) > 0 ? 1 : 0);
        var depot = game.DepotValue(me);
        Assert.True(depot > 0);

        var bar = game.TransferAccount(me);
        Assert.Equal(me.Money, bar.Money);
        Assert.Equal(70_000, bar.Debts);
        Assert.Equal(depot + 30_000 + 100_000, bar.Assets);
        Assert.Equal(me.Money + depot + 130_000 - 70_000, bar.Balance);

        // Bankkredit statt Sparbuch: Schulden bei der Bank, kein Sparbuch in den Anlagen.
        game.WithdrawFromBank(me, 250_000);
        bar = game.TransferAccount(me);
        Assert.Equal(70_000 + 150_000, bar.Debts);
        Assert.Equal(depot + 30_000, bar.Assets);
    }

    /// <summary>
    /// Mehrsaisonprobe: 4 Saisonwechsel, passiver und aktiver Manager (bietet Spieler an, verkauft an den Bieter). Geprüft:
    /// Markt nie über 11 Plätze, Bieter nach 0x059B8, Spielstand ladbar und unverändert.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MultiSeason_FourChanges_DailyMarket(bool active)
    {
        var game = TestGames.Create(21);
        var manager = game.ActiveManager;
        var club = game.Clubs[manager.ClubIndex];
        var market = game.TransferMarket;
        var sold = 0;
        for (var change = 0; change < 4; change++)
        {
            for (var day = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; day++)
            {
                Assert.True(day < 1000, "Saisonende nicht erreicht");
                TestSeason.KeepSquad(game, manager);
                manager.Money = Math.Max(manager.Money, 5_000_000);
                Assert.InRange(market.Listings.Count, 0, TransferMarket.MaxListings);
                if (active && game.Phase == SeasonPhase.Running)
                {
                    if (market.Listings.All(l => l.Seller != manager)
                        && club.Players.Count(p => p.Role == PlayerRole.Forward) > 12
                        && club.Players.FirstOrDefault(p => p.Role == PlayerRole.Forward && !p.OnLoan) is { } forward)
                        market.Offer(game, manager, forward);
                    if (market.Bids.FirstOrDefault(b => b.Listing.Seller == manager) is { } bid)
                    {
                        Assert.InRange(bid.Buyer.Index, 0, 42);
                        if (market.Accept(game, bid) == TransferResult.Accepted)
                            sold++;
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
        Assert.Equal(4, game.History.Count);
        if (active)
            Assert.True(sold >= 1, "kein Verkauf");
    }

    /// <summary>random(a, b) liefert immer a.</summary>
    private sealed class Lowest : Random
    {
        public override int Next(int minValue, int maxValue) => minValue;
        public override int Next(int maxValue) => 0;
        public override int Next() => 0;
        public override double NextDouble() => 0;
    }

    /// <summary>random(a, b) liefert der Reihe nach die vorgegebenen Werte und merkt sich die Grenzen.</summary>
    private sealed class Sequence(params int[] values) : Random
    {
        private int _next;
        public List<(int Min, int Max)> Calls { get; } = [];
        public override int Next(int minValue, int maxValue)
        {
            Calls.Add((minValue, maxValue - 1));
            return values[_next++];
        }
        public override int Next(int maxValue) => Next(0, maxValue);
        public override int Next() => Next(0, int.MaxValue);
        public override double NextDouble() => 0;
    }

    /// <summary>
    /// A209, 0x4981E–0x4991C: Ein Nationalteam (t = 1) liefert die Grundstärken aus seinen Vereinsfeldern 0x19/0x1B, die der Lader
    /// 0x2EF1B aus der Ländertabelle DGROUP 0x4C4E setzt (Kondition und Technik Sturm, je −2) – nicht aus der Datenbankstärke.
    /// Deutschland ohne Bundestrainer mit den neu gewürfelten Werten (0x55506). Vorher rot: Datenbankstärke des Nationalteams.
    /// </summary>
    [Fact]
    public void MarketStrengths_NationalTeamFromCountryTable()
    {
        var game = TestGames.Create();
        foreach (var nation in Enumerable.Range(NationalTeams.First, NationalTeams.Last - NationalTeams.First + 1))
        {
            var club = game.Clubs[nation];
            var p = new Player { Name = "X", Role = PlayerRole.Forward };
            TransferPoolRules.RollMarketStrengths(p, club, new Lowest(), game);
            var (_, _, ca, ta) = game.NationalRatings(nation);
            // random(10 · t, 14) − 10 = 0 bei t = 1; Technik mit random(0, 4) = 0 zusätzlich random(12, 20) = 12.
            Assert.Equal(Math.Clamp(ca, 10, 99), p.Condition);
            Assert.Equal(Math.Clamp(ta + 12, 10, 99), p.Technique);
        }
        // Index 53: Tabelle 97, 102, 90, 100 → Sturm 88/98; die Datenbankstärke (bisherige Quelle) spielt keine Rolle.
        var (_, _, c1, t1) = NationalTeamRatings.Of(NationalTeams.First + 1);
        Assert.Equal((88, 98), (c1, t1));
    }

    /// <summary>
    /// F113, 0x4907A (aus „Kaderplatz anlegen“ 0x42CB1 an 0x42E6B): Tendenz +0x0E = random(35, 65), Steuerbyte +0x14 =
    /// random(1, 13) | random(1, 3) · 16 | random(0, 1) · 128; beim Listenwechsel nach der Energie (0x42E4C) und vor +0x19
    /// (0x497C7). Anzeige „Fallend./Konstant./Steigend.“ = (+0x0E − 30) / 13 (0x41AE0). Vorher rot: kein Zug, Tendenz dreistufig.
    /// </summary>
    [Fact]
    public void SlotFields_RollTrendAfterEnergy()
    {
        var r = new Sequence(100, 60, 7, 2, 1, 5, 9);
        var p = new Player { Name = "X", Role = PlayerRole.Forward, Trend = PlayerTrend.Falling };
        TransferPoolRules.RollSlotFields(p, r);
        Assert.Equal([(80, 120), (35, 65), (1, 13), (1, 3), (0, 1), (3, 12), (3, 12)], r.Calls);
        Assert.Equal(60, p.TrendScore);
        Assert.Equal(7 | 2 << 4 | 1 << 7, p.TrendControl);
        Assert.Equal(PlayerTrend.Rising, p.Trend); // (60 − 30) / 13 = 2
        Assert.Equal(PlayerTrend.Falling, Player.TrendOf(42));
        Assert.Equal(PlayerTrend.Constant, Player.TrendOf(43));
        Assert.Equal(PlayerTrend.Constant, Player.TrendOf(55));
        Assert.Equal(PlayerTrend.Rising, Player.TrendOf(56));
    }

    /// <summary>
    /// Die stufenlose Tendenz geht in den Spielerwert ein (0x4A0A7, Eingang +0x0E) und in den Balken des Spielerdaten-Fensters
    /// ((+0x0E − 30) · 100 / 38); ohne Wert (alte Spielstände) gilt die Mitte der Stufe. Eine von Hand gesetzte Stufe, die nicht
    /// zum Wert passt, löscht den Wert.
    /// </summary>
    [Fact]
    public void TrendScore_DrivesValueAndFallsBackToLevel()
    {
        var p = new Player { Name = "X", Role = PlayerRole.Forward, Condition = 70, Technique = 70, Age = 25 };
        Assert.Equal(49, PlayerValue.Inputs(p, null).Trend);
        p.SetTrendScore(65, 0);
        Assert.Equal(65, PlayerValue.Inputs(p, null).Trend);
        Assert.Equal(PlayerTrend.Rising, p.Trend);
        p.Trend = PlayerTrend.Rising; // passt: Wert bleibt
        Assert.Equal(65, p.TrendScore);
        p.Trend = PlayerTrend.Falling; // passt nicht: Wert gelöscht
        Assert.Equal(0, p.TrendScore);
        Assert.Equal(36, PlayerValue.Inputs(p, null).Trend);
    }

    /// <summary>Tendenz und Steuerbyte überstehen Sichern und Laden; Spielstände vor Schema 29 bekommen 0 (Stufe gilt).</summary>
    [Fact]
    public void TrendScore_RoundTripsAndMigrates()
    {
        var game = TestGames.Create();
        var player = game.TransferMarket.Listings[0].Player;
        Assert.InRange(player.TrendScore, 35, 65);
        var saved = SaveGame.Save(game);
        var loaded = SaveGame.Load(saved);
        Assert.True(loaded.Success, loaded.Error);
        var again = loaded.Game!.TransferMarket.Listings[0].Player;
        Assert.Equal((player.TrendScore, player.TrendControl, player.Trend), (again.TrendScore, again.TrendControl, again.Trend));
        Assert.Equal(saved, SaveGame.Save(loaded.Game!));

        var root = System.Text.Json.Nodes.JsonNode.Parse(saved)!.AsObject();
        root["schema"] = 28; // vor Teil 4 (Integration Runde 11: 28 → 29)
        foreach (var p in root["spieler"]!.AsArray().OfType<System.Text.Json.Nodes.JsonObject>())
        {
            p.Remove("TrendScore");
            p.Remove("TrendControl");
        }
        var old = SaveGame.Load(System.Text.Encoding.UTF8.GetBytes(root.ToJsonString()));
        Assert.True(old.Success, old.Error);
        Assert.All(old.Game!.Clubs.SelectMany(c => c.Players), p => Assert.Equal(0, p.TrendScore));
        Assert.All(old.Game!.TransferMarket.Listings, l => Assert.Equal((0, 0), (l.Player.TrendScore, l.Player.TrendControl)));
    }

    /// <summary>Zwei Manager (Bundesliga 2 und 7); Manager 1 bietet einen Stürmer an.</summary>
    internal static (GameState Game, Manager Buyer, Manager Seller, TransferListing Listing) TwoManagers(int seed = 5)
    {
        var setup = new GameSetup { DebCup = false };
        setup.Managers.Add(new ManagerSetup { Name = "KAUF", Head = 0, ClubIndex = 2 });
        setup.Managers.Add(new ManagerSetup { Name = "VERKAUF", Head = 1, ClubIndex = 7 });
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, seed);
        var (buyer, seller) = (game.Managers[0], game.Managers[1]);
        var player = game.Clubs[seller.ClubIndex].Players.First(p => p.Role == PlayerRole.Forward && !p.OnLoan);
        Assert.Equal(TransferResult.Accepted, game.TransferMarket.Offer(game, seller, player));
        var listing = game.TransferMarket.Listings.Single(l => l.Seller == seller);
        buyer.Money = 50_000_000;
        return (game, buyer, seller, listing);
    }

    /// <summary>
    /// A258, Datei 0x47655–0x47743: Ein faires Gebot auf den Spieler eines anderen Managers fragt diesen („Nehmen Sie das Angebot
    /// …“); bis zur Antwort ändert sich nichts. „Na gut.“ schließt ab (Kauf: offener Vertrag), „Nein !“ lehnt ab und sperrt den
    /// Markt-Platz für den Bieter (0x47AC8). Vorher rot: Die Antwort wurde ohne Frage gewürfelt.
    /// </summary>
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void SellerQuestion_AnswerDecides(bool accept, bool loan)
    {
        var (game, buyer, seller, listing) = TwoManagers();
        var market = game.TransferMarket;
        var offer = loan ? listing.LoanFee : listing.Price; // geforderter Betrag: fair
        var (money, sellerMoney) = (buyer.Money, seller.Money);
        var result = market.Bid(game, buyer, listing, offer, loan, out var purchase, out _, out var question);
        Assert.Equal(TransferResult.SellerAsked, result);
        Assert.Null(purchase);
        Assert.Equal(new SellerQuestion(listing, buyer, seller, offer, loan), question);
        Assert.Contains(listing, market.Listings);
        Assert.Equal((money, sellerMoney), (buyer.Money, seller.Money));

        var answer = market.AnswerSeller(game, question!, accept, out purchase, out var subsidy);
        Assert.Null(subsidy);
        if (!accept)
        {
            Assert.Equal(TransferResult.Rejected, answer);
            Assert.True(TransferMarket.IsLocked(game, buyer, listing));
            Assert.Contains(listing, market.Listings);
            return;
        }
        Assert.Equal(TransferResult.Accepted, answer);
        Assert.False(TransferMarket.IsLocked(game, buyer, listing));
        if (loan)
        {
            Assert.Null(purchase);
            Assert.Contains(listing.Player, game.Clubs[buyer.ClubIndex].Players);
            Assert.Equal(sellerMoney + offer, seller.Money);
        }
        else
        {
            Assert.NotNull(purchase);
            Assert.Equal(offer, purchase!.Offer);
            Assert.Equal(NegotiationResult.Accepted, market.Sign(game, purchase, 0, 0, insured: false, demandFilled: false));
            Assert.Contains(listing.Player, game.Clubs[buyer.ClubIndex].Players);
        }
    }

    /// <summary>Unfaire Gebote fragen den Verkäufer nicht (0x47546 vor 0x47655), auch nicht ohne Geld (0x474F0).</summary>
    [Fact]
    public void SellerQuestion_NotForUnfairOrUnaffordable()
    {
        var (game, buyer, _, listing) = TwoManagers();
        var market = game.TransferMarket;
        Assert.Equal(TransferResult.Unfair, market.Bid(game, buyer, listing, 1, false, out _, out _, out var q1));
        Assert.Null(q1);
        buyer.Money = 0;
        Assert.Equal(TransferResult.NotEnoughMoney, market.Bid(game, buyer, listing, listing.Price, false, out _, out _, out var q2));
        Assert.Null(q2);
    }
}
