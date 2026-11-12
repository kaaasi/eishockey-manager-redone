using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur kader-rest (Runde 17), Teil 2 – Transfermarkt: A253 (Verkaufszweig Datei 0x46DD8 und 0x46FD4–0x470DE ohne
/// Mindestbesetzung je Art), F089 (Besitzzählung 0x3CCEB auch beim Kauf, 0x42CF7–0x42D44, mit eigenen Spielern auf dem Markt),
/// A244 (nach dem Start der Talentsuche keine Meldung, 0x46A14–0x46A38).
/// </summary>
public class KaderRestTransferTests
{
    private static (GameState Game, Manager Manager, Club Club, TransferMarket Market) NewGame()
    {
        var game = TestGames.Create();
        var manager = game.ActiveManager;
        return (game, manager, game.Clubs[manager.ClubIndex], game.TransferMarket);
    }

    /// <summary>
    /// [ORIGINAL] Datei 0x46FD4–0x470DE: Der Verkauf prüft keine Mindestbesetzung je Art. Vorher [APPROX] 2/4/6 → „Der Transfer
    /// findet nicht statt.“ schon beim ersten Torhüter von zwei. Ohne Torhüter läuft der Spieltag weiter.
    /// </summary>
    [Fact]
    public void Offer_AllGoalkeepers_NoMinimumPerRole_MatchDayStillRuns()
    {
        var (game, manager, club, market) = NewGame();
        var goalies = club.Players.Where(p => p.Role == PlayerRole.Goalkeeper).ToArray();
        Assert.InRange(goalies.Length, 1, TransferMarket.MaxOwnListings);
        foreach (var g in goalies)
            Assert.Equal(TransferResult.Accepted, market.Offer(game, manager, g));
        Assert.DoesNotContain(club.Players, p => p.Role == PlayerRole.Goalkeeper);

        TestSeason.PlayMatchDay(game);
        Assert.True(game.PlayedMatchDays >= 1);
    }

    /// <summary>
    /// [ORIGINAL] Datei 0x46DD8: Kaderplatz +0x18 Bit 0x80 → „Dieser Spieler hört / am Saisonende auf.“ (DGROUP 0x12BD), vor der
    /// Grenze von drei eigenen Markt-Plätzen (0x47027). Vorher ging der Spieler auf den Markt.
    /// </summary>
    [Fact]
    public void Offer_Retiring_RefusedBeforeOwnListingLimit()
    {
        var (game, manager, club, market) = NewGame();
        var forwards = club.Players.Where(p => p.Role == PlayerRole.Forward && !p.OnLoan).ToArray();
        var retiring = forwards[0];
        retiring.Notice = ContractNotice.Retiring;
        Assert.Equal(TransferResult.Retiring, market.Offer(game, manager, retiring));
        Assert.Contains(retiring, club.Players);
        Assert.Equal(["Dieser Spieler hört", "am Saisonende auf."], TransferMarket.Message(TransferResult.Retiring));

        for (var i = 1; i <= TransferMarket.MaxOwnListings; i++)
            Assert.Equal(TransferResult.Accepted, market.Offer(game, manager, forwards[i]));
        Assert.Equal(TransferResult.Retiring, market.Offer(game, manager, retiring));
    }

    /// <summary>
    /// [ORIGINAL] Datei 0x47040–0x470E4: Pool +0x1A ≠ Manager → „Dieser Spieler ist / nur ausgeliehen.“ (DGROUP 0x11F1), nach der
    /// Grenze von drei eigenen Markt-Plätzen. Vorher „Der Transfer findet nicht statt.“
    /// </summary>
    [Fact]
    public void Offer_LoanedIn_OnlyLoaned_AfterOwnListingLimit()
    {
        var (game, manager, club, market) = NewGame();
        var other = game.Clubs.First(c => c != club && c.Index < 52);
        var loaned = new Player
        {
            Name = "LEIHE", Role = PlayerRole.Forward, Condition = 50, Technique = 50, Form = 50, Energy = 100, OnLoan = true,
            Loan = new PlayerLoan(other, 1, 1_000, false),
        };
        club.Players.Add(loaned);
        Assert.Equal(TransferResult.OnlyLoaned, market.Offer(game, manager, loaned));
        Assert.Equal(["Dieser Spieler ist", "nur ausgeliehen."], TransferMarket.Message(TransferResult.OnlyLoaned));

        foreach (var p in club.Players.Where(p => p.Role == PlayerRole.Forward && !p.OnLoan).Take(TransferMarket.MaxOwnListings).ToArray())
            Assert.Equal(TransferResult.Accepted, market.Offer(game, manager, p));
        Assert.Equal(TransferResult.TooManyOwnListings, market.Offer(game, manager, loaned));
    }

    /// <summary>
    /// [ORIGINAL] Datei 0x3CCEB: Eigene Spieler auf dem Markt (Satz 4, Pool +0x1A = Manager) zählen zum Besitz. Seit A380 stehen
    /// sie außerhalb des Kaders; vorher fehlten sie in <see cref="GameState.OwnedPlayers"/>.
    /// </summary>
    [Fact]
    public void OwnedPlayers_CountsOwnListingsOutsideSquad()
    {
        var (game, manager, club, market) = NewGame();
        var before = game.OwnedPlayers(manager);
        var p = club.Players.First(q => q.Role == PlayerRole.Forward && !q.OnLoan);
        Assert.Equal(TransferResult.Accepted, market.Offer(game, manager, p));
        Assert.DoesNotContain(p, club.Players);
        Assert.Equal(before, game.OwnedPlayers(manager));
    }

    /// <summary>
    /// [ORIGINAL] Datei 0x42D1E–0x42D44: Kauf und Leihe brechen bei mehr als 28 Spielern im Besitz ab (0xF8:0x75 = 0x3CCEB), auch
    /// wenn im eigenen Kader noch Plätze frei sind. Vorher zählte nur der eigene Kader.
    /// </summary>
    [Fact]
    public void Bid_SquadFull_CountsOwnListingsAndLoanedOut()
    {
        var (game, manager, club, market) = NewGame();
        manager.Money = 100_000_000;
        var listing = market.Listings.First(l => l.Seller is null && l.From != club && !l.IsFreeAgent);
        var sold = club.Players.First(q => q.Role == PlayerRole.Forward && !q.OnLoan);
        Assert.Equal(TransferResult.Accepted, market.Offer(game, manager, sold));
        while (club.Players.Count < TransferMarket.MaxSquad - 1)
            club.Players.Add(new Player { Name = "X", Role = PlayerRole.Forward });
        Assert.Equal(TransferMarket.MaxSquad - 1, club.Players.Count);
        Assert.Equal(TransferMarket.MaxSquad, game.OwnedPlayers(manager));

        Assert.Equal(TransferResult.SquadFull, market.Bid(game, manager, listing, listing.Price * 2, loan: false, out _));
        Assert.Equal(TransferResult.SquadFull, market.Bid(game, manager, listing, listing.LoanFee * 2, loan: true, out _));

        Assert.Equal(TransferResult.Accepted, market.Withdraw(market.ListingOf(sold)!));
        club.Players.Remove(sold);
        var other = game.Clubs.First(c => c != club && c.Index < 52 && c != listing.From);
        other.Players.Add(new Player
        {
            Name = "LEIHE", Role = PlayerRole.Forward, OnLoan = true, Loan = new PlayerLoan(club, 1, 1_000, false),
        });
        Assert.Equal(TransferMarket.MaxSquad, game.OwnedPlayers(manager));
        Assert.Equal(TransferResult.SquadFull, market.Bid(game, manager, listing, listing.Price * 2, loan: false, out _));
        Assert.True(TransferMarket.IsSquadFull(game, manager, listing.From));
        // [ORIGINAL] 0x42CF7–0x42D1C: Spieler des eigenen Vereins (Pool +0x1D) ohne Besitzzählung, nur freie Plätze.
        Assert.False(TransferMarket.IsSquadFull(game, manager, club));
    }

    /// <summary>Eigenen Spieler auf den Markt stellen und den Kader bis MaxSquad − 1 füllen: 29 Besitz bei 28 Kaderplätzen.</summary>
    private static void FillOwnedToLimit(GameState game, Manager manager, Club club)
    {
        var sold = club.Players.First(q => q.Role == PlayerRole.Forward && !q.OnLoan);
        Assert.Equal(TransferResult.Accepted, game.TransferMarket.Offer(game, manager, sold));
        while (club.Players.Count < TransferMarket.MaxSquad - 1)
            club.Players.Add(new Player { Name = "X", Role = PlayerRole.Forward });
        Assert.True(club.Players.Count < TransferMarket.MaxSquad);
        Assert.Equal(TransferMarket.MaxSquad, game.OwnedPlayers(manager));
    }

    /// <summary>
    /// Review #799 P3-2 (F089): Auch der Vertrag nach angenommenem Gebot prüft die Besitzgrenze (0x42CB1 sucht einen freien Platz
    /// unter den 29 Besitzplätzen, eigene Markt-Plätze zählen mit). Rückmutation in Transfer.Purchase.cs auf
    /// <c>Players.Count &gt;= MaxSquad</c> → Vertrag kommt zustande → rot.
    /// </summary>
    [Fact]
    public void Sign_SquadFull_CountsOwnListings()
    {
        var (game, manager, club, market) = NewGame();
        manager.Money = 100_000_000;
        var listing = market.Listings.First(l => l.Seller is null && l.From != club && !l.IsFreeAgent);
        Assert.Equal(TransferResult.Accepted, market.Bid(game, manager, listing, listing.Price * 2, loan: false, out var purchase));
        FillOwnedToLimit(game, manager, club);

        Assert.Equal(NegotiationResult.Unfair, market.Sign(game, purchase!, 0, 0, insured: false, demandFilled: false));
        Assert.DoesNotContain(listing.Player, club.Players);
        Assert.Contains(listing, market.Listings);
        Assert.Equal(100_000_000, manager.Money);
    }

    /// <summary>
    /// Review #799 P3-2 (F089): Die Antwort „Na gut.“ des verkaufenden Managers prüft die Besitzgrenze des Bieters erneut
    /// (<c>[APPROX]</c> Zwischenprüfung, siehe <see cref="TransferMarket.AnswerSeller"/>). Rückmutation in
    /// Transfer.SellerQuestion.cs auf <c>Players.Count &gt;= MaxSquad</c> → Transfer schließt ab → rot.
    /// </summary>
    [Fact]
    public void AnswerSeller_SquadFull_CountsOwnListings()
    {
        var (game, buyer, seller, listing) = KaderTransferTeil4Tests.TwoManagers();
        var market = game.TransferMarket;
        var club = game.Clubs[buyer.ClubIndex];
        Assert.Equal(TransferResult.SellerAsked, market.Bid(game, buyer, listing, listing.Price, loan: false, out _, out _, out var question));
        FillOwnedToLimit(game, buyer, club);
        var (money, sellerMoney) = (buyer.Money, seller.Money);

        Assert.Equal(TransferResult.SquadFull, market.AnswerSeller(game, question!, accept: true, out var purchase, out _));
        Assert.Null(purchase);
        Assert.Contains(listing, market.Listings);
        Assert.Equal((money, sellerMoney), (buyer.Money, seller.Money));
    }

    /// <summary>
    /// [ORIGINAL] Datei 0x46A14–0x46A38: Nach dem Start der Talentsuche zeigt das Original keine Meldung. Vorher „Ihr 'Spion' geht
    /// auf Talentsuche.“
    /// </summary>
    [Fact]
    public void TalentSearch_Started_NoMessage()
    {
        Assert.Empty(TalentScout.Message(TalentSearchStart.Started));
        Assert.Equal(["Sie haben nicht", "genug Geld !"], TalentScout.Message(TalentSearchStart.NotEnoughMoney));
    }

    /// <summary>
    /// Mehrsaisonprobe (4 Saisonwechsel, passiv und aktiv): Der aktive Manager setzt laufend Torhüter und Verteidiger auf den
    /// Markt, auch den letzten (A253); Spieltage, Saisonwechsel (Rückkehr der Markt-Plätze) und Besitzzählung (F089) bleiben
    /// stimmig.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MultiSeason_FourChanges_SellingWithoutMinimum(bool active)
    {
        var game = TestGames.Create(5);
        var start = game.Season;
        var manager = game.Managers[0];
        var withoutGoalie = 0;
        RngTwinProbeTests.Run(game, active, Check);
        Assert.Equal(start + 4, game.Season);
        if (active)
            Assert.True(withoutGoalie > 0, "nie ohne Torhüter");

        void Check(GameState g)
        {
            var club = g.Clubs[manager.ClubIndex];
            if (active && g.Phase == SeasonPhase.Running)
                foreach (var p in club.Players.Where(p => p.Role != PlayerRole.Forward && !p.OnLoan
                                                          && p.Notice != ContractNotice.Retiring).ToArray())
                    if (g.TransferMarket.Offer(g, manager, p) != TransferResult.Accepted)
                        break;
            if (!club.Players.Any(p => p.Role == PlayerRole.Goalkeeper))
                withoutGoalie++;
            Assert.InRange(club.Players.Count, 0, TransferMarket.MaxSquad);
            var listed = g.TransferMarket.OwnListingsOutsideSquad(club).Count();
            Assert.InRange(listed, 0, TransferMarket.MaxOwnListings);
            Assert.Equal(club.Players.Count + listed + g.Clubs.Where(c => c != club).Sum(c => c.Players.Count(p => p.Loan?.Origin == club)),
                g.OwnedPlayers(manager));
        }
    }
}
