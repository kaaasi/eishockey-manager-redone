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
    public void Offers_AreRenewedAndScaleWithLeague()
    {
        var (game, me) = Create();
        var ads = game.Ads(me);
        Assert.Equal(4, ads.Offers.Count);
        var all = ads.Offers.Values.SelectMany(o => o.Values).ToList();
        Assert.NotEmpty(all);
        Assert.All(all, o =>
        {
            var (min, max) = SponsorRules.Range(o.Kind);
            var factor = SponsorRules.LeagueFactor(game.Clubs[me.ClubIndex].League);
            Assert.InRange(o.MonthlyAmount, (long)(min * o.Boards * factor), (long)(max * o.Boards * factor) + 1);
        });
    }
}
