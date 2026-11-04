using System.Text.Json.Nodes;
using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur finanzen-werbung (Runde 6): Regressionstests zu den Befunden der unabhängigen Finanz-Nachprüfung
/// NP-01 (#132, Werbebewertung 0x3F833), NP-02 (#133, Werbeflächen, Angebote, Vertragsende) und NP-03 (#134,
/// Monatsabschluss am Monatsletzten). Gegen den Stand 1e4731b rot (Assert bzw. fehlende Bausteine).
/// </summary>
public class FinanceWerbungTests
{
    private sealed class MinRandom : Random
    {
        public override int Next(int minValue, int maxValue) => minValue;
    }

    private sealed class MaxRandom : Random
    {
        public override int Next(int minValue, int maxValue) => maxValue - 1;
    }

    /// <summary>Tag der Zeitleiste zu einem Datum der Saison (Layout 10.09.1989 – 29.04.1990).</summary>
    private static int DayOf(int month, int day)
    {
        var start = SeasonCalendar.LayoutDate(0);
        return new DateOnly(month >= 9 ? start.Year : start.Year + 1, month, day).DayNumber - start.DayNumber;
    }

    // ---- #134 Monatsabschluss -------------------------------------------------------------------

    /// <summary>#134: Nach dem 31. Januar ist die Januarabrechnung gebucht, nicht erst am 1. Februar (0x3F02C).</summary>
    [Fact]
    public void MonthlySettlement_RunsOnLastDayOfMonth()
    {
        var game = TestGames.Create(3);
        var me = game.ActiveManager;
        game.AdvanceFinanceTo(DayOf(1, 30));
        var before = game.Finance(me).Book.Current(FinanceCategory.Gehaelter);
        game.AdvanceFinanceTo(DayOf(1, 31));
        var afterLastDay = game.Finance(me).Book.Current(FinanceCategory.Gehaelter);
        Assert.True(afterLastDay < before, "Am 31. Januar keine Gehälter gebucht.");
        game.AdvanceFinanceTo(DayOf(2, 1));
        Assert.Equal(afterLastDay, game.Finance(me).Book.Current(FinanceCategory.Gehaelter));
    }

    // ---- #132 Bewertung -------------------------------------------------------------------------

    /// <summary>
    /// #132: Die Wettbewerbsterme sind 0/1 für „gerade im Wettbewerb“, keine ewigen Titelzähler. Bundesliga (t = 0)
    /// ohne laufende Wettbewerbe: 2 · (80 · 0 + 170) = 340, nicht 2 · (240 + 170) = 820 bei drei Meistertiteln.
    /// </summary>
    [Fact]
    public void CompetitionTerms_AreCurrentParticipation_NotEternalTitles()
    {
        Assert.Equal(340, SponsorRating.CompetitionScore(0, inChampionship: false, inCup: false, cupRound: 0, inEurope: false, europeRound: 0));
        Assert.Equal(2 * (80 + 170), SponsorRating.CompetitionScore(0, inChampionship: true, inCup: false, cupRound: 0, inEurope: false, europeRound: 0));
        // Pokal ausgelost (Runde 2), nicht mehr dabei: 32 · (0 − 2) = −64; dabei: 32 · (3 − 2) = 32.
        Assert.Equal(2 * 170 - 64, SponsorRating.CompetitionScore(0, false, inCup: false, cupRound: 2, false, 0));
        Assert.Equal(2 * 170 + 32, SponsorRating.CompetitionScore(0, false, inCup: true, cupRound: 2, false, 0));
        // Europapokal: 47 · (Runde + 1).
        Assert.Equal(170 + 47 * 4, SponsorRating.CompetitionScore(1, false, false, 0, inEurope: true, europeRound: 3));
    }

    /// <summary>
    /// #132 am Spielstand: Ewige Meistertitel ändern die Bewertung nicht (gleicher Zufall). 2. Liga ohne Kader, damit
    /// die Bewertung unter der Kappung 100 bleibt (vorher +3 · 80 / 12 · 85 / 100).
    /// </summary>
    [Fact]
    public void Rating_IgnoresEternalChampionships()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Z", Head = 0, ClubIndex = 14, StartLeague = League.ZweiteLiga });
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, 3);
        var me = game.ActiveManager;
        Assert.Equal(League.ZweiteLiga, game.Clubs[me.ClubIndex].League);
        game.Clubs[me.ClubIndex].Players.Clear();
        var plain = SponsorRating.Compute(game, me, new Random(1));
        // Ewige Tabelle hat keinen Setter: Eintrag wie nach drei Meisterschaften anlegen.
        var eternal = (Dictionary<int, EternalEntry>)typeof(GameState)
            .GetField("_eternal", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(game)!;
        eternal[me.ClubIndex] = new EternalEntry { Club = me.ClubIndex, Championships = 3, CupWins = 2, EuropeanCupWins = 1 };
        Assert.Equal(3, game.Eternal(me.ClubIndex).Championships);
        Assert.Equal(plain, SponsorRating.Compute(game, me, new Random(1)));
    }

    /// <summary>
    /// #132: Star-Spieler, wenn Wert/7000 > Ø(Wert/5500) · rnd(35, 55)/10 (0x3FA24–0x3FAC3). Drei Spieler mit
    /// Wert/5500 = 100 und Wert/7000 = 78: Schwelle mindestens 350 → kein Star (vorher 3).
    /// </summary>
    [Fact]
    public void Stars_CompareWithSquadAverage()
    {
        long[] values = [550_000, 550_000, 550_000];
        Assert.Equal(78, values[0] / 7000);
        Assert.Equal(0, SponsorRating.StarCount(values, new MinRandom()));
        // Ein Ausreißer zählt erst bei genug Spielern: Ø = (5 · 1 + 909) / 6 = 152, Schwelle 532 < 5.000.000/7000 = 714.
        long[] mixed = [10_000, 10_000, 10_000, 10_000, 10_000, 5_000_000];
        Assert.Equal(1, SponsorRating.StarCount(mixed, new MinRandom()));
    }

    /// <summary>
    /// #132: Ende in 16 Bit ohne Vorzeichen, nur nach oben auf 100 gekappt (0x3FAC5–0x3FB2E). S = −12 in der Oberliga
    /// (2 − Liga = 0): 65.524 / 12 = 5.460; · 85 = 464.100 → 16 Bit 5.348; / 100 = 53 (nicht 0).
    /// </summary>
    [Fact]
    public void Finish_WrapsInSixteenBits()
    {
        Assert.Equal(53, SponsorRating.Finish(-12, tier: 2));
        // Bundesliga: (5.460 + 20) · 85 = 465.800 → 16 Bit 7.048; / 100 = 70.
        Assert.Equal(70, SponsorRating.Finish(-12, tier: 0));
        Assert.Equal(100, SponsorRating.Finish(12_000, tier: 0));
        Assert.Equal(8, SponsorRating.Finish(126, tier: 2));
    }

    // ---- #133 Werbeflächen und Angebote ---------------------------------------------------------

    /// <summary>
    /// #133: Faktoren DGROUP 0x4F05 = 8, 20, 32, 15, 15: Fernsehen 32 mit Partner + 3. R = 50, m = 20, p = 0,
    /// Zufall minimal: 10.000 · 32/10 · (65 + 24) · 6/100 · 90/100 = 153.792 Drittel (vorher 52.650).
    /// </summary>
    [Fact]
    public void OfferAmount_TelevisionFactor32WithPartnerOffset()
    {
        Assert.Equal(153_792, SponsorRules.OfferAmountThirds(50, AdSlotKind.Fernsehen, 0, 20, new MinRandom()));
        // Trikot 20 ohne Versatz: 10.000 · 20/10 · 65 · 6/100 · 90/100 = 70.200.
        Assert.Equal(70_200, SponsorRules.OfferAmountThirds(50, AdSlotKind.Trikot, 0, 20, new MinRandom()));
        // Eisfläche 15, beide Tafeln.
        Assert.Equal(52_650, SponsorRules.OfferAmountThirds(50, AdSlotKind.Eisflaeche, 0, 20, new MinRandom()));
        Assert.Equal(52_650, SponsorRules.OfferAmountThirds(50, AdSlotKind.Eisflaeche2, 0, 20, new MinRandom()));
    }

    /// <summary>#133: Eisfläche als zwei Felder – nur mit Überdachung HALLE bzw. Kühlanlage MEGAFROST (0x2B56A).</summary>
    [Fact]
    public void IceSlots_RequireHallAndMegafrost()
    {
        var game = TestGames.Create(3);
        var me = game.ActiveManager;
        var stadium = game.Finance(me).Stadium;
        stadium.SetLevel(StadiumFeature.Ueberdachung, 1);
        stadium.SetLevel(StadiumFeature.Kuehlanlage, 1);
        Assert.False(game.IsAdSlotAvailable(me, new AdSlot(AdSlotKind.Eisflaeche)));
        Assert.False(game.IsAdSlotAvailable(me, new AdSlot(AdSlotKind.Eisflaeche2)));
        stadium.SetLevel(StadiumFeature.Ueberdachung, 2);
        Assert.True(game.IsAdSlotAvailable(me, new AdSlot(AdSlotKind.Eisflaeche)));
        Assert.False(game.IsAdSlotAvailable(me, new AdSlot(AdSlotKind.Eisflaeche2)));
        stadium.SetLevel(StadiumFeature.Kuehlanlage, 2);
        Assert.True(game.IsAdSlotAvailable(me, new AdSlot(AdSlotKind.Eisflaeche2)));

        stadium.SetLevel(StadiumFeature.Ueberdachung, 0);
        game.Ads(me).Offers[AdSlotKind.Eisflaeche] = new() { [0] = new SponsorOffer(AdSlotKind.Eisflaeche, 0, 12, 1, 1_000) };
        Assert.Null(game.AcceptOffer(me, new AdSlot(AdSlotKind.Eisflaeche), 0));
    }

    /// <summary>
    /// #133: Startangebote ohne Würfeln nur bei Spieldauer eine Saison (0x2A051); sonst würfelt auch der Start –
    /// Partner 9 bleibt mit Wahrscheinlichkeit 6/7 beim (leeren) alten Angebot.
    /// </summary>
    [Fact]
    public void StartOffers_DicedUnlessOneSeasonGame()
    {
        int MissingPartnerNine(GameDuration duration)
        {
            var missing = 0;
            for (var seed = 1; seed <= 20; seed++)
            {
                var setup = new GameSetup { Duration = duration };
                setup.Managers.Add(new ManagerSetup { Name = "T", Head = 0, ClubIndex = 2 });
                var game = NewGame.Create(TestGames.CreateDatabase(), setup, seed);
                if (game.Ads(game.ActiveManager).OfferFrom(AdSlotKind.Bande, 9) is null)
                    missing++;
            }
            return missing;
        }
        Assert.Equal(0, MissingPartnerNine(GameDuration.EineSaison));
        Assert.True(MissingPartnerNine(GameDuration.Unbegrenzt) >= 10);
    }

    /// <summary>#133: Erneuerung am Monatsletzten und am Tag ⌊Monatslänge/2⌋ (0x3EE6B): Januar 15. und 31., nicht 1.</summary>
    [Fact]
    public void OfferDays_LastDayAndMidMonth()
    {
        Assert.True(SponsorRules.IsOfferDay(new DateOnly(1990, 1, 15)));
        Assert.True(SponsorRules.IsOfferDay(new DateOnly(1990, 1, 31)));
        Assert.False(SponsorRules.IsOfferDay(new DateOnly(1990, 1, 1)));
        Assert.True(SponsorRules.IsOfferDay(new DateOnly(1990, 2, 14)));
        Assert.True(SponsorRules.IsOfferDay(new DateOnly(1990, 2, 28)));
    }

    /// <summary>#133: Der Angebotsbetrag nutzt die gespeicherte Bewertung (Feld 0x97CE), sie wird nicht neu berechnet.</summary>
    [Fact]
    public void Offers_UseStoredRating()
    {
        var game = TestGames.Create(3);
        var me = game.ActiveManager;
        game.Finance(me).SponsorRating = 0;
        game.RefreshSponsorOffers(me, noDice: true);
        var offer = game.Ads(me).OfferFrom(AdSlotKind.Bande, 0)!;
        Assert.Equal(0, offer.MonthlyAmount);
        Assert.Equal(0, offer.Rating);
    }

    /// <summary>
    /// #133: Laufzeit −1 am 1. (0x3EF52), Buchung am Monatsletzten. Ein 12-Monats-Vertrag ohne Option zahlt zwölfmal und
    /// endet still; ein Vertrag mit Option zahlt nach Ablauf weiter und meldet nur „bittet Sie an den Verhandlungstisch“.
    /// </summary>
    [Fact]
    public void Contracts_DecrementOnFirst_BookOnLastDay_OptionKeepsPaying()
    {
        var game = TestGames.Create(3);
        var me = game.ActiveManager;
        var ads = game.Ads(me);
        ads.Offers[AdSlotKind.Trikot] = new() { [3] = new SponsorOffer(AdSlotKind.Trikot, 3, 2, 1, 9_000) };
        ads.Offers[AdSlotKind.Fernsehen] = new() { [1] = new SponsorOffer(AdSlotKind.Fernsehen, 1, 2, 1, 6_000, OptionMonths: 12) };
        var shirt = game.AcceptOffer(me, new AdSlot(AdSlotKind.Trikot), 3)!;
        var tv = game.AcceptOffer(me, new AdSlot(AdSlotKind.Fernsehen), 1)!;
        game.Finance(me).Inbox.Clear();

        var start = game.FinanceDay;
        var sponsors = game.Finance(me).Book.Current(FinanceCategory.Sponsoren);
        game.AdvanceFinanceTo(DayOf(9, 29));
        Assert.Equal(sponsors, game.Finance(me).Book.Current(FinanceCategory.Sponsoren));
        game.AdvanceFinanceTo(DayOf(9, 30));
        var september = game.Finance(me).Book.Current(FinanceCategory.Sponsoren) - sponsors - game.MonthlyBaseIncome(me);
        Assert.Equal(15_000, september);
        game.AdvanceFinanceTo(DayOf(10, 1));
        Assert.Equal((1, 1), (shirt.MonthsLeft, tv.MonthsLeft));
        game.AdvanceFinanceTo(DayOf(11, 1));
        Assert.DoesNotContain(shirt, ads.Contracts);
        Assert.Contains(tv, ads.Contracts);
        Assert.Equal(0, tv.MonthsLeft);
        Assert.True(game.AwaitsRenewal(tv));
        var message = Assert.Single(game.Inbox(me));
        Assert.Equal(MessageKind.Info, message.Kind);
        Assert.Equal(["Ein Werbepartner mit Verlängerungsoption", "bittet Sie an den Verhandlungstisch."], message.Lines);

        var beforeNovember = game.Finance(me).Book.Current(FinanceCategory.Sponsoren);
        game.AdvanceFinanceTo(DayOf(11, 30));
        Assert.Equal(6_000 + game.MonthlyBaseIncome(me), game.Finance(me).Book.Current(FinanceCategory.Sponsoren) - beforeNovember);
        Assert.True(start < DayOf(9, 29));
        Assert.Equal(DayOf(1, 31) + 1, DayOf(2, 1));
    }

    /// <summary>
    /// #133: Verhandlung erst beim Klick im Werbebildschirm, Frage nur bei rnd(90, 120) · Bewertung_alt / 100 &lt;
    /// Bewertung_neu (0x2B773). Bewertung bei Abschluss 50, jetzt 40: höchstens 45 &lt; 40 nie → immer Absage.
    /// </summary>
    [Fact]
    public void Renewal_OnlyIfRatingRoseEnough()
    {
        Assert.False(SponsorRules.RenewalOffered(50, 40, new MinRandom()));
        Assert.False(SponsorRules.RenewalOffered(50, 40, new MaxRandom()));
        Assert.True(SponsorRules.RenewalOffered(50, 46, new MinRandom()));
        Assert.False(SponsorRules.RenewalOffered(50, 60, new MaxRandom()));
        Assert.True(SponsorRules.RenewalOffered(50, 61, new MaxRandom()));
    }

    /// <summary>#133: Absage löscht den Vertrag; Zusage verlängert um die Option, die danach entfällt.</summary>
    [Fact]
    public void Renewal_AnswerExtendsOrDeletes()
    {
        var game = TestGames.Create(3);
        var me = game.ActiveManager;
        var ads = game.Ads(me);
        var contract = new SponsorContract { Kind = AdSlotKind.Fernsehen, Sponsor = 2, Boards = [], MonthlyAmount = 7_000, OptionMonths = 24, SignedRating = 0 };
        ads.Contracts.Add(contract);
        Assert.True(game.AwaitsRenewal(contract));
        var outcome = game.OpenRenewal(me, contract);
        Assert.Equal(RenewalOutcome.Question, outcome);
        Assert.True(game.AnswerRenewal(me, contract, yes: true));
        Assert.Equal((24, 0), (contract.MonthsLeft, contract.OptionMonths));
        Assert.False(game.AwaitsRenewal(contract));

        var second = new SponsorContract { Kind = AdSlotKind.Trikot, Sponsor = 1, Boards = [], MonthlyAmount = 7_000, OptionMonths = 12, SignedRating = 100 };
        ads.Contracts.Add(second);
        Assert.Equal(RenewalOutcome.Declined, game.OpenRenewal(me, second));
        Assert.DoesNotContain(second, ads.Contracts);
    }

    // ---- Spielstand ------------------------------------------------------------------------------

    /// <summary>
    /// Schema 6 → 7: gespeicherte Bewertung −1 (beim nächsten Gebrauch berechnet), Bewertung in Angeboten und Verträgen 0;
    /// danach lädt der Spielstand und rechnet weiter. Ein Schema-7-Spielstand übersteht Sichern und Laden unverändert.
    /// </summary>
    [Fact]
    public void SchemaSix_IsMigrated()
    {
        var game = TestGames.Create(3);
        var me = game.ActiveManager;
        game.Finance(me).Stadium.SetLevel(StadiumFeature.Kuehlanlage, 2);
        game.Ads(me).Offers[AdSlotKind.Eisflaeche2] = new() { [4] = new SponsorOffer(AdSlotKind.Eisflaeche2, 4, 12, 1, 3_000, 12, 30) };
        var contract = game.AcceptOffer(me, new AdSlot(AdSlotKind.Eisflaeche2), 4)!;
        Assert.Equal(30, contract.SignedRating);
        var saved = SaveGame.Save(game);
        var reloaded = SaveGame.Load(saved);
        Assert.True(reloaded.Success, reloaded.Error);
        Assert.Equal(saved, SaveGame.Save(reloaded.Game!));

        var root = JsonNode.Parse(saved)!.AsObject();
        root["schema"] = 6;
        var removed = 0;
        void Strip(JsonNode? node)
        {
            switch (node)
            {
                case JsonObject obj:
                    foreach (var key in new[] { "SponsorRating", "Rating", "SignedRating" })
                        if (obj.Remove(key))
                            removed++;
                    foreach (var (_, child) in obj.ToArray())
                        Strip(child);
                    break;
                case JsonArray array:
                    foreach (var child in array.ToArray())
                        Strip(child);
                    break;
            }
        }
        Strip(root);
        Assert.True(removed > 3);
        var result = SaveGame.Load(System.Text.Encoding.UTF8.GetBytes(root.ToJsonString()));
        Assert.True(result.Success, result.Error);
        var loaded = result.Game!;
        var lme = loaded.ActiveManager;
        Assert.Equal(-1, loaded.Finance(lme).SponsorRating);
        Assert.Equal(0, Assert.Single(loaded.Ads(lme).Contracts).SignedRating);
        Assert.All(loaded.Ads(lme).Offers.Values.SelectMany(o => o.Values), o => Assert.Equal(0, o.Rating));
        loaded.AdvanceFinanceTo(loaded.FinanceDay + 20);
        Assert.InRange(loaded.Finance(lme).SponsorRating, 0, 100);
    }

    // ---- Mehrsaisonprobe -------------------------------------------------------------------------

    /// <summary>
    /// Mehrsaisonprobe (4 Saisonwechsel, passiv und aktiv): Der aktive Manager baut HALLE und MEGAFROST, nimmt nach jedem
    /// Schritt jedes mögliche Angebot an und verhandelt jeden abgelaufenen Vertrag mit Option (Ja). Geprüft wird: Bewertung
    /// stets 0–100; „Sponsoren“ wird nur gebucht, wenn im Schritt ein Monatsletzter lag (#134); Verträge nur auf nutzbaren
    /// Flächen, Restlaufzeit ≥ 0, auf Verhandlung wartend nur mit Option (#133).
    /// Leitstelle Runde 8 (R8-A02): Ob in einem Lauf ein Vertrag mit Option ausläuft, hängt vom Zufall ab; früher wurde der
    /// Seed bei jeder Änderung des Zufallsverbrauchs verschoben (11 → 12 → 15 → 16). Die Verhandlung prüft jetzt
    /// <see cref="ActiveManager_NegotiatesOptions_AcrossSeeds"/> über mehrere Seeds; diese Probe prüft je Seed die Regeln.
    /// </summary>
    [Theory]
    [InlineData(false, 3)]
    [InlineData(true, 3)]
    [InlineData(true, 16)]
    public void FourSeasonChanges_SponsoringStaysConsistent(bool active, int seed) => RunSponsoringProbe(active, seed);

    /// <summary>
    /// R8-A02: Der aktive Manager verhandelt über mehrere Seeds mindestens eine Option (#133). Robust gegen geänderten
    /// Zufallsverbrauch: Es genügt ein Seed aus sechs (auf dem Stand Runde 8 verhandeln u. a. 16, 18, 20).
    /// </summary>
    [Fact]
    public void ActiveManager_NegotiatesOptions_AcrossSeeds()
    {
        var negotiated = Enumerable.Range(15, 6).Sum(seed => RunSponsoringProbe(true, seed));
        Assert.True(negotiated > 0, "in keinem der Seeds 15–20 eine Verhandlung über eine Option");
    }

    private static int RunSponsoringProbe(bool active, int seed)
    {
        var game = TestGames.Create(seed);
        var me = game.ActiveManager;
        if (active)
        {
            game.Finance(me).Stadium.SetLevel(StadiumFeature.Ueberdachung, 2);
            game.Finance(me).Stadium.SetLevel(StadiumFeature.Kuehlanlage, 2);
        }
        var lastDay = game.FinanceDay;
        var lastSeason = game.Season;
        var lastSponsors = game.Finance(me).Book.Current(FinanceCategory.Sponsoren);
        var bookings = 0;
        var signed = 0;
        var negotiated = 0;

        RngTwinProbeTests.Run(game, active, g =>
        {
            var finance = g.Finance(me);
            Assert.InRange(finance.SponsorRating, 0, 100);
            var sponsors = finance.Book.Current(FinanceCategory.Sponsoren);
            if (g.Season == lastSeason && sponsors != lastSponsors)
            {
                var monthEnd = Enumerable.Range(lastDay + 1, Math.Max(0, g.FinanceDay - lastDay))
                    .Select(SeasonCalendar.LayoutDate).Any(d => d.Day == DateTime.DaysInMonth(d.Year, d.Month));
                Assert.True(monthEnd, $"Sponsoren gebucht ohne Monatsletzten (Tage {lastDay + 1}–{g.FinanceDay}).");
                bookings++;
            }
            (lastDay, lastSeason, lastSponsors) = (g.FinanceDay, g.Season, sponsors);

            foreach (var contract in finance.Sponsoring.Contracts)
            {
                Assert.True(contract.MonthsLeft >= 0);
                Assert.True(contract.MonthsLeft > 0 || contract.OptionMonths > 0, "abgelaufener Vertrag ohne Option");
                Assert.True(g.IsAdSlotAvailable(me, new AdSlot(contract.Kind, contract.Boards.FirstOrDefault())));
            }
            if (!active)
                return;
            foreach (var contract in finance.Sponsoring.Contracts.Where(g.AwaitsRenewal).ToList())
            {
                negotiated++;
                if (g.OpenRenewal(me, contract) == RenewalOutcome.Question)
                    Assert.True(g.AnswerRenewal(me, contract, yes: true));
                Assert.False(g.AwaitsRenewal(contract));
            }
            foreach (var kind in Enum.GetValues<AdSlotKind>())
                foreach (var offer in finance.Sponsoring.Offers.GetValueOrDefault(kind)?.Values.ToList() ?? [])
                {
                    var slot = kind == AdSlotKind.Bande ? new AdSlot(kind, finance.Sponsoring.FreeBoards().FirstOrDefault(-1)) : new AdSlot(kind);
                    if (slot.Index >= 0 && g.AcceptOffer(me, slot, offer.Sponsor) is not null)
                        signed++;
                }
        });

        Assert.True(bookings > 4, $"nur {bookings} Schritte mit Monatsbuchung");
        if (active)
        {
            Assert.True(signed > 10, $"nur {signed} Verträge");
            Assert.Contains(game.Ads(me).Contracts, c => c.Kind is AdSlotKind.Eisflaeche or AdSlotKind.Eisflaeche2);
        }
        else
            Assert.Empty(game.Ads(me).Contracts);
        return negotiated;
    }
}
