using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>Befunde PR35-01 und PR35-02 aus docs/reviews/2026-10-26-pr35-review.md.</summary>
public class WaveBReviewRegressionTests
{
    /// <summary>
    /// PR35-01: Stammverein eines Leihspielers ist voll (99 Rückennummern), seine Verträge werden als Computerverein
    /// verlängert. Früher scheiterte der Saisonwechsel danach an der Rückennummer und der Leihspieler war verloren.
    /// </summary>
    [Fact]
    public void PR3501_LoanReturnToFullComputerClub_DoesNotLosePlayerOrAbortSeasonChange()
    {
        var game = TestGames.Create();
        LifecycleRegressionTests.FinishSeason(game);
        var listing = game.TransferMarket.Listings.First(l => l.Seller is null && !l.IsFreeAgent);
        var origin = listing.From;
        var player = listing.Player;
        player.ContractYears = 1;
        Assert.Equal(TransferResult.Accepted,
            game.TransferMarket.Buy(game, game.ActiveManager, listing, listing.LoanFee * 135 / 100, true)); // 135 %: im Original immer angenommen (über random(110, 130) %, 0x4991D; R8-C01)
        var random = new Random(1);
        while (origin.Players.Count < PlayerFactory.MaxNumber)
        {
            var talent = PlayerFactory.CreateTalent("TALENT", PlayerRole.Forward, 50, random);
            PlayerFactory.Join(origin, talent, 1, false);
        }
        foreach (var p in origin.Players)
            p.ContractYears = 1;
        var everyone = game.Clubs.SelectMany(c => c.Players).Concat(game.TransferMarket.FreeAgents).ToHashSet();
        var season = game.Season;

        game.StartNextSeason();

        Assert.Equal(season + 1, game.Season);
        Assert.False(player.OnLoan);
        Assert.Null(player.Loan);
        var squads = game.Clubs.SelectMany(c => c.Players).ToArray();
        var free = game.TransferMarket.FreeAgents.ToArray();
        Assert.Equal(1, squads.Count(p => p == player) + free.Count(p => p == player));
        // Kein Spieler verloren: Rücktritte werden 1:1 ersetzt; Computervereine füllen freigestellte Positionen wieder auf,
        // die Freigestellten bleiben als vereinslose Spieler – es können also Spieler dazukommen (Spur spielberechnung:
        // anderer Zufallsverbrauch, Verein 12 gibt hier auch einen Torhüter und einen Verteidiger frei).
        Assert.True(squads.Concat(free).Distinct().Count() >= everyone.Count);
        Assert.True(origin.Players.Count <= PlayerFactory.MaxNumber);
        Assert.Equal(origin.Players.Count, origin.Players.Select(p => p.Number).Distinct().Count());
    }

    /// <summary>
    /// PR35-02: Zwei eigene Verteidiger mit Geboten bei fünf Verteidigern. Nach dem ersten Verkauf ist das zweite Gebot
    /// nicht mehr ausführbar; es muss mit Ergebnis abgelehnt und entfernt werden, statt still offen zu bleiben.
    /// </summary>
    [Fact]
    public void PR3502_BidThatBecameInvalid_IsRejectedWithResultAndRemoved()
    {
        // A380 (kader-start Teil 2c): Der Spieler auf der Liste verlässt den Kader, die Mindestbesetzung greift schon beim
        // Aufsetzen. Ungültig wird ein Gebot nun, wenn der verkaufende Manager den Verein gewechselt hat.
        var game = TestGames.Create();
        var market = game.TransferMarket;
        var manager = game.ActiveManager;
        var club = game.Clubs[manager.ClubIndex];
        foreach (var extra in club.Players.Where(p => p.Role == PlayerRole.Defender).Skip(5).ToArray())
            club.RemovePlayer(extra);
        var defenders = club.Players.Where(p => p.Role == PlayerRole.Defender).Take(2).ToArray();
        Assert.Equal(TransferResult.Accepted, market.Offer(game, manager, defenders[0]));
        // A253 (kader-rest Teil 2): keine Mindestbesetzung beim Verkauf (Datei 0x46FD4–0x470DE); bis Runde 17 Impossible.
        Assert.Equal(TransferResult.Accepted, market.Offer(game, manager, defenders[1]));
        Assert.Equal(TransferResult.Accepted, market.Withdraw(market.ListingOf(defenders[1])!));
        Assert.Equal(4, club.Players.Count(p => p.Role == PlayerRole.Defender));
        for (var i = 0; market.Bids.Count(b => b.Listing.Seller == manager) < 1 && i < 500; i++)
            market.AdvanceDay(game, game.Random);
        var bid = Assert.Single(market.Bids, b => b.Listing.Seller == manager);

        var home = manager.ClubIndex;
        manager.ClubIndex = game.Clubs.First(c => c != club && c != bid.Buyer && c.Index < 52).Index;
        var money = manager.Money;
        Assert.Equal(TransferResult.Impossible, market.Accept(game, bid));
        manager.ClubIndex = home;

        Assert.Equal(money, manager.Money);
        Assert.DoesNotContain(bid, market.Bids);
        Assert.Equal(TransferResult.Impossible, market.Accept(game, bid)); // Wiederholung ebenfalls ohne Wirkung
    }
}
