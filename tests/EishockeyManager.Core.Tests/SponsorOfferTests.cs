using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur finanzen-2, Teil „Werbung“: Angebotsformel, Laufzeiten, Bewertung und Angebotsrhythmus aus em.exe
/// (0x3FB47, 0x3F833, 0x3EE6B). Gegen den Stand 5f8e02a rot (dort Spannen je Fläche × Ligafaktor, monatlich).
/// </summary>
public class SponsorOfferTests
{
    private sealed class MinRandom : Random
    {
        public override int Next(int minValue, int maxValue) => minValue;
    }

    private sealed class MaxRandom : Random
    {
        public override int Next(int minValue, int maxValue) => maxValue - 1;
    }

    [Fact]
    public void OfferAmount_FollowsRatingKindAndPartner()
    {
        // Hand nachgerechnet: W = 200, m = 20 → random(4000, 5200) = 4000; Bande × 8/10 = 3200; × 65 × 6 / 100 = 12.480;
        // × 90/100 = 11.232 Drittel = 3.744 DM (UI-Video, Oberliga-Start: Bande 3.454 und 5.464 DM).
        Assert.Equal(11_232, SponsorRules.OfferAmountThirds(20, AdSlotKind.Bande, 0, 20, new MinRandom()));
        Assert.Equal(3_744, FinanceRules.ToMarks(SponsorRules.OfferAmountThirds(20, AdSlotKind.Bande, 0, 20, new MinRandom())));
        // Fernsehen, Partner 4: × 15/10 = 6000 × (65 + 32) × 6 / 100 = 34.920 × 0,9 = 31.428 Drittel = 10.476 DM.
        Assert.Equal(31_428, SponsorRules.OfferAmountThirds(20, AdSlotKind.Fernsehen, 4, 20, new MinRandom()));
        // Trikot zählt den Partner um 3 höher: Partner 0 → (65 + 24); obere Grenze 13·(W·m/10).
        Assert.Equal(5200L * 32 / 10 * 89 * 6 / 100 * 90 / 100, SponsorRules.OfferAmountThirds(20, AdSlotKind.Trikot, 0, 20, new MaxRandom()));
        Assert.True(SponsorRules.OfferAmountThirds(60, AdSlotKind.Bande, 0, 20, new MinRandom()) == 3 * SponsorRules.OfferAmountThirds(20, AdSlotKind.Bande, 0, 20, new MinRandom()));
    }

    [Fact]
    public void Duration_TwelveOrTwentyFourMonths_OptionOneInEight()
    {
        Assert.Equal((12, 12), SponsorRules.Duration(new MinRandom()));
        Assert.Equal((24, 0), SponsorRules.Duration(new MaxRandom()));
        var random = new Random(5);
        var samples = Enumerable.Range(0, 4000).Select(_ => SponsorRules.Duration(random)).ToList();
        Assert.All(samples, d => Assert.Contains(d.Months, new[] { 12, 24 }));
        Assert.All(samples.Where(d => d.Months == 24), d => Assert.Contains(d.OptionMonths, new[] { 0, 12 }));
        Assert.InRange(samples.Count(d => d.OptionMonths > 0), 350, 650);
    }

    [Fact]
    public void StadiumWorth_StartStadiumOberliga()
    {
        // (1,5 × 2000 + 2500) / 2 = 2750; Kühlanlage mind. 1 → 1166; Sitzkomfort fest 2 → 5666; Zustand fest 3 → 6000; / 700 = 22.
        Assert.Equal(22, SponsorRating.StadiumWorth(Stadium.ForLeague(League.Oberliga)));
        // Bundesliga: (4500 + 4500) / 2 = 4500; HALLE (2 − 1) → 4333; Anzeigetafel 2333; Kühlanlage 1166; 5666; 6000;
        // Verkehr 3333; Imbißbude 2333 → 29.664 / 700 = 42.
        Assert.Equal(42, SponsorRating.StadiumWorth(Stadium.ForLeague(League.Bundesliga)));
    }

    [Fact]
    public void Rating_FreshClub_IsModerate_AndBundesligaHigher()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "O", Head = 0, ClubIndex = 24 });
        setup.Managers.Add(new ManagerSetup { Name = "B", Head = 1, ClubIndex = 2 });
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, 3);
        var oberliga = SponsorRating.Compute(game, game.Managers[0]);
        var bundesliga = SponsorRating.Compute(game, game.Managers[1]);
        Assert.InRange(oberliga, 10, 45);
        Assert.True(bundesliga > oberliga, $"Bundesliga {bundesliga} ≤ Oberliga {oberliga}");
        Assert.InRange(bundesliga, 30, 100);
    }

    [Fact]
    public void Offers_RefreshOnFirstAndMidMonth_NotEveryDay()
    {
        var game = TestGames.Create();
        var me = game.ActiveManager;
        var ads = game.Ads(me);
        // Ein Angebot entfernen und beobachten, wann es (durch die Erneuerung) wiederkommen kann.
        ads.Offers[AdSlotKind.Bande].Clear();
        var day = game.FinanceDay + 1;
        var refreshDays = new List<int>();
        for (var d = day; d < day + 70; d++)
        {
            var before = ads.Offers[AdSlotKind.Bande].Count;
            game.AdvanceFinanceTo(d);
            if (ads.Offers[AdSlotKind.Bande].Count != before)
                refreshDays.Add(SeasonCalendar.LayoutDate(d).Day);
        }
        Assert.NotEmpty(refreshDays);
        Assert.All(refreshDays, dayOfMonth => Assert.True(dayOfMonth == 1 || dayOfMonth is 14 or 15, $"Erneuerung am {dayOfMonth}."));
    }

    [Fact]
    public void ExpiredContract_AsksOnlyWithOption()
    {
        var game = TestGames.Create();
        var me = game.ActiveManager;
        var ads = game.Ads(me);
        ads.Offers[AdSlotKind.Trikot][1] = new SponsorOffer(AdSlotKind.Trikot, 1, 1, 1, 8_000, OptionMonths: 24);
        ads.Offers[AdSlotKind.Eisflaeche][2] = new SponsorOffer(AdSlotKind.Eisflaeche, 2, 1, 1, 5_000);
        Assert.NotNull(game.AcceptOffer(me, new AdSlot(AdSlotKind.Trikot), 1));
        Assert.NotNull(game.AcceptOffer(me, new AdSlot(AdSlotKind.Eisflaeche), 2));

        var day = game.FinanceDay + 1;
        while (SeasonCalendar.LayoutDate(day).Day != 1)
            day++;
        game.AdvanceFinanceTo(day);

        var question = Assert.Single(game.Inbox(me), m => m.Kind == MessageKind.SponsorRenewal);
        Assert.Equal(AdSlotKind.Trikot, question.Contract!.Kind);
        Assert.True(game.Answer(me, question, yes: true));
        var renewed = Assert.Single(ads.Contracts, c => c.Kind == AdSlotKind.Trikot);
        Assert.Equal(24, renewed.MonthsLeft);
        Assert.DoesNotContain(ads.Contracts, c => c.Kind == AdSlotKind.Eisflaeche);
    }
}
