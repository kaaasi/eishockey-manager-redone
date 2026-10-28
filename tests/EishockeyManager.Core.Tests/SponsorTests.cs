using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>Werbung: Angebote, Verträge, monatliche Einnahmen.</summary>
public class SponsorTests
{
    private static (GameState Game, Manager Me) Create()
    {
        var game = TestGames.Create();
        return (game, game.ActiveManager);
    }

    private static void SetOffer(GameState game, Manager me, AdSlotKind kind, int sponsor, int boards, long amount, int months = 12)
    {
        var ads = game.Ads(me);
        ads.Offers[kind][sponsor] = new SponsorOffer(kind, sponsor, months, boards, amount);
    }

    [Fact]
    public void Partners_MatchTheOriginal()
    {
        Assert.Equal(10, Sponsor.Companies.Length);
        Assert.Equal(5, Sponsor.Stations.Length);
        Assert.Equal("JWE Service GmbH", Sponsor.Companies[0].Name);
        Assert.Equal("premium", Sponsor.Stations[4].Name);
        Assert.All(Sponsor.Companies, c => Assert.NotEmpty(c.Info));
    }

    [Fact]
    public void AcceptOffer_OccupiesBoardsAndRaisesIncome()
    {
        var (game, me) = Create();
        var before = game.MonthlyIncome(me);
        SetOffer(game, me, AdSlotKind.Bande, sponsor: 1, boards: 2, amount: 7_000);

        var contract = game.AcceptOffer(me, new AdSlot(AdSlotKind.Bande, 6), 1);
        Assert.NotNull(contract);
        Assert.Equal([6, 0], contract!.Boards);                 // nächstes freies Feld, ggf. von vorn
        Assert.Equal(before + 7_000, game.MonthlyIncome(me));
        Assert.Null(game.Ads(me).OfferFrom(AdSlotKind.Bande, 1)); // Angebot verbraucht

        SetOffer(game, me, AdSlotKind.Bande, sponsor: 2, boards: 1, amount: 3_000);
        Assert.Null(game.AcceptOffer(me, new AdSlot(AdSlotKind.Bande, 0), 2)); // Feld belegt
    }

    [Fact]
    public void AcceptOffer_FailsWithoutEnoughFreeBoards()
    {
        var (game, me) = Create();
        for (var b = 0; b < Sponsoring.BoardCount - 1; b++)
        {
            SetOffer(game, me, AdSlotKind.Bande, b, 1, 1_000);
            Assert.NotNull(game.AcceptOffer(me, new AdSlot(AdSlotKind.Bande, b), b));
        }
        SetOffer(game, me, AdSlotKind.Bande, 9, 2, 5_000);
        Assert.Null(game.AcceptOffer(me, new AdSlot(AdSlotKind.Bande, 6), 9));
    }

    [Fact]
    public void Contracts_PayMonthlyAndExpire()
    {
        var (game, me) = Create();
        SetOffer(game, me, AdSlotKind.Fernsehen, 0, 1, 15_000, months: 1);
        game.AcceptOffer(me, new AdSlot(AdSlotKind.Fernsehen), 0);
        var sponsorsBefore = game.Finance(me).Book.Current(FinanceCategory.Sponsoren);

        // bis zum nächsten Monatsersten abrechnen
        var day = game.FinanceDay + 1;
        while (SeasonCalendar.LayoutDate(day).Day != 1)
            day++;
        game.AdvanceFinanceTo(day);

        var paid = game.Finance(me).Book.Current(FinanceCategory.Sponsoren) - sponsorsBefore;
        Assert.Equal(15_000 + game.MonthlyBaseIncome(me), paid);
        Assert.Empty(game.Ads(me).Contracts);
    }

    [Fact]
    public void Offers_AtStartForEveryPartner_WithinTheOriginalEnvelope()
    {
        // Spur finanzen-2: Beim Spielstart bekommt jede Art und jeder Partner ein Angebot (em.exe 0x3FBA0);
        // Beträge liegen zwischen den Grenzen der Angebotsformel (0x3FC1A) für die Bewertung des Vereins.
        var (game, me) = Create();
        var ads = game.Ads(me);
        var rating = SponsorRating.Compute(game, me);
        Assert.Equal(4, ads.Offers.Count);
        foreach (var kind in Enum.GetValues<AdSlotKind>())
        {
            Assert.Equal(Sponsor.For(kind).Length, ads.Offers[kind].Count);
            foreach (var offer in ads.Offers[kind].Values)
            {
                var low = FinanceRules.ToMarks(SponsorRules.OfferAmountThirds(rating - 6, kind, offer.Sponsor, 20, new MinRandom()));
                var high = FinanceRules.ToMarks(SponsorRules.OfferAmountThirds(rating + 6, kind, offer.Sponsor, 22, new MaxRandom()));
                Assert.InRange(offer.MonthlyAmount, low, high);
                Assert.Contains(offer.Months, new[] { 12, 24 });
                Assert.Contains(offer.OptionMonths, new[] { 0, 12, 24 });
                Assert.InRange(offer.Boards, 1, kind == AdSlotKind.Bande ? 3 : 1);
            }
        }
    }

    private sealed class MinRandom : Random
    {
        public override int Next(int minValue, int maxValue) => minValue;
    }

    private sealed class MaxRandom : Random
    {
        public override int Next(int minValue, int maxValue) => maxValue - 1;
    }
}
