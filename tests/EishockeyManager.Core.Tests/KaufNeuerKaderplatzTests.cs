using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur v-rest, Teil 1 (A951, #948): Startgehalt und Verhandlung nach einem Kauf vom Markt rechnen am neuen Kaderplatz.
/// [ORIGINAL] Datei 0x477E7 legt den Platz an (0x42CB1: Tendenz neu, Spiele beim Verein 0, Strafen 0, Stärken aus dem Pool),
/// 0x42F11 rechnet daran das Gehalt, 0x4786A ruft den Dialog 0x4A1DB mit diesem Platz; der Wert 0x4A0A7 liest dessen Felder.
/// Vorher rot: Startgehalt, Forderung, Prämie und Annahme sahen Tendenz, Vereinsspiele und Strafen des Markt-Platzes.
/// Erwartete Werte kommen aus einem Zwillingsspiel, in dem der Kauf abgeschlossen ist (der Platz liegt dann am Spieler).
/// </summary>
public class KaufNeuerKaderplatzTests
{
    private const int Seed = 4;

    /// <summary>Markt-Platz mit vielen Vereinsspielen, hoher Tendenz und Strafen – alles, was der neue Platz nicht hat.</summary>
    private static (GameState Game, Manager Manager, Club Club, TransferListing Listing) MarketGame()
    {
        var game = TestGames.Create(Seed);
        var manager = game.ActiveManager;
        manager.Money = 100_000_000;
        var club = game.Clubs[manager.ClubIndex];
        var listing = game.TransferMarket.Listings.First(l => l.Seller is null && l.From != club && !l.IsFreeAgent
            && l.Player.Role != PlayerRole.Goalkeeper);
        var p = listing.Player;
        p.ClubGamesCarry = 400;
        p.ClubCupGamesCarry = 50;
        p.SetTrendScore(68, p.TrendControl);
        p.Penalties5 = 4;
        p.Penalties2 = 3;
        return (game, manager, club, listing);
    }

    private static TransferPurchase Accept(GameState game, Manager manager, TransferListing listing)
    {
        Assert.Equal(TransferResult.Accepted,
            game.TransferMarket.Bid(game, manager, listing, listing.Price * 2, loan: false, out var purchase));
        return purchase!;
    }

    private static int SalaryThirds(GameState game, Manager manager, Player p) =>
        game.PlayerValueOf(p, game.Clubs[manager.ClubIndex], PlayerValueKind.Salary, new Random(0), manager.Level);

    /// <summary>Wert(Gehalt) in Drittel-DM am neuen Kaderplatz: Zwillingsspiel, Kauf mit „Mach' ich!“ ohne Eingaben.</summary>
    private static int NewSlotSalaryThirds()
    {
        var (game, manager, _, listing) = MarketGame();
        var purchase = Accept(game, manager, listing);
        Assert.Equal(NegotiationResult.Accepted, game.TransferMarket.Sign(game, purchase, 0, 0, insured: false, false));
        return SalaryThirds(game, manager, listing.Player);
    }

    /// <summary>A951: Startgehalt = Wert(Gehalt) des neuen Platzes (Datei 0x42F11), nicht des Markt-Platzes.</summary>
    [Fact]
    public void Purchase_StartSalary_IsValueOfNewSlot()
    {
        var (game, manager, _, listing) = MarketGame();
        var oldThirds = SalaryThirds(game, manager, listing.Player);
        var purchase = Accept(game, manager, listing);
        var expected = NewSlotSalaryThirds() / 3;
        Assert.NotEqual(oldThirds / 3, expected);
        Assert.Equal(expected, purchase.Salary);
        Assert.Equal(expected, Contracts.ValueSalary(game, purchase));
        Assert.Equal(NegotiationResult.Accepted, game.TransferMarket.Sign(game, purchase, 0, 0, insured: false, false));
        Assert.Equal(expected, listing.Player.MonthlySalary);
        Assert.Equal(expected, Contracts.ValueSalary(game, manager, listing.Player)); // am angelegten Platz gleich
    }

    /// <summary>A951: Forderung nach Eingabe der Laufzeit (0x4A81B) und Prämie (0x4AA9C) im Fenster des Kaufs am neuen Platz.</summary>
    [Fact]
    public void Purchase_DemandAndInsuranceOffer_UseNewSlot()
    {
        var (game, manager, club, listing) = MarketGame();
        var purchase = Accept(game, manager, listing);
        var thirds = NewSlotSalaryThirds();
        var expectedDemand = ContractRules.DemandThirds(thirds, game.ContractTurnFactor, game.ContractSeasonRemaining, 3,
            game.AfterSeasonEnd, club.Players[3].Age, purchase.Salary * 3) / 3;
        Assert.Equal(expectedDemand, Contracts.DemandAfterYears(game, purchase, 3));
        Assert.Equal(Math.Max(1, thirds / 2 / 3), Contracts.InsuranceOffer(game, purchase));
        Assert.NotEqual(Contracts.InsuranceOffer(game, manager, listing.Player), Contracts.InsuranceOffer(game, purchase));
    }

    /// <summary>
    /// A951: Die Annahme 0x49A3B im Fenster des Kaufs vergleicht mit Wert(Gehalt) des neuen Platzes. Gewähltes Gehalt liegt über
    /// jeder Schwelle des neuen Platzes (Wert · 115 % · (w + 4) %) und unter jeder des Markt-Platzes (Wert · 85 % · (w − 6) %):
    /// angenommen; vorher „nicht interessiert“.
    /// </summary>
    [Fact]
    public void Purchase_Negotiation_ComparesWithNewSlot()
    {
        var (game, manager, _, listing) = MarketGame();
        var oldThirds = SalaryThirds(game, manager, listing.Player);
        var purchase = Accept(game, manager, listing);
        var newThirds = NewSlotSalaryThirds();
        const int years = 2;
        var w = (((years - 1) * 100 + game.ContractSeasonRemaining) / 6 + 110) * 8 / 10;
        var maxNew = (long)newThirds * 115 / 100 * (w + 4) / 100;
        var minOld = (long)oldThirds * 85 / 100 * (w - 6) / 100;
        var salary = (int)(maxNew / 3 + 1);
        Assert.True(salary * 3L <= minOld, $"Probe trennt nicht: neu {maxNew}, alt {minOld}");
        Assert.False(game.AfterSeasonEnd);
        Assert.Equal(NegotiationResult.Accepted,
            game.TransferMarket.Sign(game, purchase, years, salary, insured: false, demandFilled: false));
        Assert.Equal((years, salary), (listing.Player.ContractYears, listing.Player.MonthlySalary));
    }
}
