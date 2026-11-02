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
        var game = TestGames.Create(5);
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
        // Ligaspieltage 44 → früher höchstens 44 Würfe; täglich (> 200 Tage) gibt es fast an jedem Halt einen Wechsel.
        Assert.True(changes > 30, $"nur {changes} Halte mit Listenwechsel");
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
}
