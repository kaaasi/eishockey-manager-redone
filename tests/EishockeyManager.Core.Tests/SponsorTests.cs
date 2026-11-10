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

        // bis zum nächsten Monatsersten: Buchung am Monatsletzten, Ablauf am 1. (#133, #134)
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
        // Spur finanzen-2/finanzen-werbung: Bei einem Spiel über eine Saison bekommt beim Spielstart jede Art und jeder
        // Partner ein Angebot ohne Würfeln (em.exe 0x2A051, #133); Beträge liegen zwischen den Grenzen der Angebotsformel
        // (0x3FC1A) für die beim Start gespeicherte Bewertung (0x97CE).
        // Spur tagesablauf (#699 Punkt 2, R15-E1 = A): Im ersten Menü ist der Tageswechsel zu Tag 0 gerechnet; dessen Tagesabschluss
        // bewertet neu (0x3F833, Datei 0x55230). Die Angebote tragen weiter die Startbewertung, die aktuelle Bewertung ist die von
        // Tag 0 (vorher: beide gleich, weil Tag 0 erst nach dem ersten Menü lief).
        var setup = new GameSetup { Duration = GameDuration.EineSaison };
        setup.Managers.Add(new ManagerSetup { Name = "Test", Head = 0, ClubIndex = 2 });
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, 3);
        var me = game.ActiveManager;
        var ads = game.Ads(me);
        Assert.Equal(SponsorRating.Compute(game, me), game.Finance(me).SponsorRating);
        var rating = ads.Offers.Values.First().Values.First().Rating;
        Assert.InRange(rating, 0, 100);
        Assert.Equal(Enum.GetValues<AdSlotKind>().Length, ads.Offers.Count);
        foreach (var kind in Enum.GetValues<AdSlotKind>())
        {
            Assert.Equal(Sponsor.For(kind).Length, ads.Offers[kind].Count);
            foreach (var offer in ads.Offers[kind].Values)
            {
                var low = FinanceRules.ToMarks(SponsorRules.OfferAmountThirds(rating, kind, offer.Sponsor, 20, new MinRandom()));
                var high = FinanceRules.ToMarks(SponsorRules.OfferAmountThirds(rating, kind, offer.Sponsor, 22, new MaxRandom()));
                Assert.InRange(offer.MonthlyAmount, low, high);
                Assert.Equal(rating, offer.Rating);
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
