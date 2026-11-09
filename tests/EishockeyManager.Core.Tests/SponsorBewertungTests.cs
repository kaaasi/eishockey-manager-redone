using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur geld, Teil 2 (Runde 11): Werbebewertung mit dem Ø-Platz der letzten Spieltage (A036, em.exe 0x3F915–0x3F98E),
/// Angebote auch bei laufendem Vertrag (A039, 0x3FB47) und Monatseinnahme aus den Vereinsfeldern (A035, 0x3E657).
/// </summary>
public class SponsorBewertungTests
{
    // ---------- A036: Ø-Platz (reine Regel) ----------

    /// <summary>Inventur P1 Nr. 4: Platzverlauf 1, 1, 1, 1, 10 → (1 + 1 + 1 + 1 + 10) / 5 = 2 (ganzzahlig, `div bx`).</summary>
    [Fact]
    public void AverageRank_LastFiveFields_IntegerDivision()
    {
        // N = 10: Felder 5 … 9; Feld 9 = Platz 10 (Halbbyte 9).
        Assert.Equal(2, SponsorRating.AverageRank(10, i => i == 9 ? 9 : 0));
        // Ältere Felder zählen nicht.
        Assert.Equal(1, SponsorRating.AverageRank(10, i => i < 5 ? 11 : 0));
    }

    /// <summary>[ORIGINAL] 0x3F97A/0x3F98E: Summe 0 (vor dem zweiten Spieltag kein Feld i &gt; 0) → Ø 6.</summary>
    [Theory]
    [InlineData(1)]
    [InlineData(0)]
    public void AverageRank_NoField_IsSix(int counter) =>
        Assert.Equal(6, SponsorRating.AverageRank(counter, _ => 3));

    /// <summary>
    /// Randfall n ≤ 5: Feld 0 und negative Indizes werden übersprungen, der Teiler N − max(0, N − 5) zählt sie mit
    /// (N = 3: Felder 1 und 2, Plätze 4 und 5 → 9 / 3 = 3).
    /// </summary>
    [Fact]
    public void AverageRank_EarlySeason_DivisorCountsSkippedFields()
    {
        var read = new List<int>();
        Assert.Equal(3, SponsorRating.AverageRank(3, i => { read.Add(i); return i + 2; }));
        Assert.Equal([1, 2], read);
        // N = 2: nur Feld 1, Teiler 2.
        Assert.Equal(3, SponsorRating.AverageRank(2, _ => 5));
    }

    // ---------- A036: im Spiel ----------

    /// <summary>
    /// Regression (vorher rot: Ø über die letzten ≤ 5 Saisons, in der ersten Saison 6): Die Bewertung nimmt die Plätze
    /// der laufenden Saison – Felder N − 5 … N − 1, Feld i = Platz nach Spieltag i + 1; Feld N − 1 (noch nicht gespielt)
    /// ist in der ersten Saison 0 (Platz 1).
    /// </summary>
    [Fact]
    public void Rating_UsesRanksOfCurrentSeason()
    {
        var game = TestGames.Create(seed: 3);
        var m = game.Managers[0];
        var club = game.Clubs[m.ClubIndex];
        int expectedAverage;
        do
        {
            TestSeason.PlayMatchDay(game);
            var history = game.RankHistory(club.Index);
            var n = history.Count + 1;
            var window = Enumerable.Range(n - 5, 5).Where(i => i > 0).Select(i => i < history.Count ? history[i] : 1).ToList();
            expectedAverage = window.Sum() / (n - Math.Max(0, n - 5));
        }
        while (expectedAverage == 6 || game.PlayedMatchDays < 6);
        Assert.Equal(expectedAverage, game.SponsorAverageRank(club));

        var finance = game.Finance(m);
        var tier = (int)finance.CostLeague;
        var p = game.CurrentParticipation(club.Index);
        var values = club.Players.Select(pl => (long)game.PlayerValueOf(pl, club, PlayerValueKind.Price, new Random(0), m.Level)).ToList();
        var random = new Random(7);
        var s = SponsorRating.CompetitionScore(tier, p.Championship, p.Cup, p.CupRound, p.Europe, p.EuropeRound) + 12L * (11 - expectedAverage);
        s += SponsorRating.AverageValueScore(values);
        var stars = SponsorRating.StarCount(values, random);
        s += (long)SquadSetup.Between(random, 20, 50) * stars + 3L * SponsorRating.StadiumWorth(finance.Stadium);
        Assert.Equal(SponsorRating.Finish(s, tier), SponsorRating.Compute(game, m, new Random(7)));
    }

    /// <summary>
    /// [ORIGINAL] Der Platzverlauf wird nur beim Spielstart geleert: In der zweiten Saison liest das Fenster als Feld N − 1
    /// den Platz der Vorsaison nach Spieltag N; nach dem letzten Spieltag (N = 45) das untere Halbbyte der Strafminuten.
    /// </summary>
    [Fact]
    public void StaleField_KeepsPreviousSeason_AndFieldBehindIsPenaltyMinutes()
    {
        var game = TestGames.Create(seed: 4);
        var me = game.ActiveManager;
        var club = game.Clubs[me.ClubIndex];
        var league = club.League!.Value;
        TestSeason.PlayRegularSeason(game);
        game.Continue(); // ein Tagesabschluss nach dem letzten Spieltag
        var previous = game.RankHistory(club.Index).ToList();
        Assert.Equal(game.Schedules[league].Count, previous.Count);
        for (var i = 0; i < previous.Count; i++)
            Assert.Equal(previous[i] - 1, club.StoredRankSlot(i));
        if (previous.Count == GameState.RankSlotCount)
            Assert.Equal(club.LeaguePenaltyMinutes & 0x0F, game.RankSlot(club, GameState.RankSlotCount));

        while (game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver)
            game.Continue();
        TestSeason.KeepSquad(game, me);
        game.StartNextSeason();
        for (var md = 0; md < 3; md++)
            TestSeason.PlayMatchDay(game);
        Assert.Equal(4, game.LeagueMatchDayCounter(club.League!.Value));
        Assert.Equal(previous[3] - 1, game.RankSlot(club, 3));
        var current = game.RankHistory(club.Index);
        Assert.Equal(current[1] - 1, game.RankSlot(club, 1));
        Assert.Equal((current[1] + current[2] + previous[3]) / 4, game.SponsorAverageRank(club));
    }

    // ---------- A039 ----------

    /// <summary>
    /// Regression (vorher rot: Partner mit laufendem Vertrag dieser Art bekam kein Angebot): [ORIGINAL] 0x3FB47 würfelt für
    /// alle Partner, ohne die Verträge zu lesen.
    /// </summary>
    [Fact]
    public void Offers_AlsoFromPartnerWithRunningContract()
    {
        var game = TestGames.Create();
        var me = game.ActiveManager;
        var ads = game.Ads(me);
        ads.Contracts.Clear();
        ads.Contracts.Add(new SponsorContract { Kind = AdSlotKind.Trikot, Sponsor = 2, Boards = [], MonthlyAmount = 1000, MonthsLeft = 12 });
        ads.Contracts.Add(new SponsorContract { Kind = AdSlotKind.Bande, Sponsor = 0, Boards = [0], MonthlyAmount = 1000, MonthsLeft = 12 });
        game.RefreshSponsorOffers(me, noDice: true);
        Assert.NotNull(ads.OfferFrom(AdSlotKind.Trikot, 2));
        Assert.NotNull(ads.OfferFrom(AdSlotKind.Bande, 0));
        Assert.Equal(Sponsor.For(AdSlotKind.Trikot).Length, ads.Offers[AdSlotKind.Trikot].Count);
    }

    // ---------- A035 ----------

    /// <summary>
    /// Regression (vorher rot: Teamwerte aus den Spielern): [ORIGINAL] 0x3E657 nimmt die Vereinsfelder 0x18–0x1B, im
    /// Remake Datenbankstärke + Abweichung je Feld. Ändert sich ein Vereinsfeld, folgt die Einnahme; Spieler nicht.
    /// </summary>
    [Fact]
    public void MonthlyIncome_FollowsClubTeamValues()
    {
        var game = TestGames.Create();
        var me = game.ActiveManager;
        var club = game.Clubs[me.ClubIndex];
        club.TeamValues = new ClubTeamValues(2, -3, 5, 1, 50, 50, 50);
        var sum = 4 * club.Strength + 2 - 3 + 5 + 1;
        Assert.Equal(sum, FinanceRules.TeamValueSum(game, club));
        Assert.Equal(5L * club.Players.Count * sum / FinanceRules.ThirdsPerMark, game.MonthlyBaseIncome(me));

        club.TeamValues = club.TeamValues.Value with { AttackTechnique = 11 };
        Assert.Equal(sum + 10, FinanceRules.TeamValueSum(game, club));
        foreach (var p in club.Players)
            p.Condition = Math.Max(1, p.Condition - 20);
        Assert.Equal(sum + 10, FinanceRules.TeamValueSum(game, club));
    }

    /// <summary>[APPROX] Vor dem ersten Spiel (Teamwerte noch nicht gezogen) zählt die Abweichung 0: 4 × Datenbankstärke.</summary>
    [Fact]
    public void MonthlyIncome_WithoutDrawnValues_UsesClubStrength()
    {
        var game = TestGames.Create();
        var club = game.Clubs[game.ActiveManager.ClubIndex];
        club.TeamValues = null;
        Assert.Equal(4 * club.Strength, FinanceRules.TeamValueSum(game, club));
    }

    // ---------- Mehrsaisonprobe ----------

    /// <summary>
    /// Mehrsaisonprobe (Geld) über vier Saisonwechsel: Der aktive Manager nimmt jeden Tag für jede freie Fläche das beste
    /// Angebot an (auch von Partnern mit laufendem Vertrag), der passive keines. Geprüft: Konto = Start + Finanzbuch,
    /// gespeicherter Platzverlauf = Tabelle, Bewertung 0 … 100, Monatseinnahme = Teamwerte × Spieler, Feld N − 1 nach dem
    /// Saisonwechsel = Platz der Vorsaison.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FourSeasonChanges_RatingAndIncomeStayConsistent(bool active)
    {
        var game = TestGames.Create(seed: 6);
        var me = game.ActiveManager;
        me.Money = 20_000_000;
        var startMoney = me.Money;
        long cumulative = 0;
        long neutral = 0;
        var signed = 0;
        var club = game.Clubs[me.ClubIndex];
        IReadOnlyList<int> previous = [];
        for (var year = 0; year < 4; year++)
        {
            var keptSet = false;
            for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
            {
                Assert.True(guard < 1000, "Saisonende nicht erreicht");
                if (active)
                    signed += SignBest(game, me);
                // #688 (Sommerpause wie im Original): fünf Monatsabschlüsse ohne Spiele je Sommer; die Probe prüft Bewertung und
                // Einnahmen – Konto halten (neutral, ohne Buchung), sonst Entlassung nach der Schuldenregel.
                var topUp = Math.Max(0, 5_000_000 - me.Money);
                me.Money += topUp;
                neutral += topUp;
                game.Continue();
                // Integration Runde 11 (geld + saisonwechsel + kader-transfer): Bleibt der Verein als Letzter oder Vorletzter in
                // der Oberliga, setzt em.exe das Konto am 29. April auf 400.000 DM ohne Buchung (S-K1, Datei 0x562D8); danach
                // nur noch die Prämien. Die neutrale Differenz zählt getrennt vom Finanzbuch (wie FinanceCalibrationTests).
                if (!keptSet && game.Phase is SeasonPhase.SeasonOver or SeasonPhase.GameOver && game.History.Count > year
                    && GameState.KeptInOberliga(game.History[^1], club.Index))
                {
                    Assert.Equal(GameState.OberligaKeptMoney + game.Finance(me).Book.Current(FinanceCategory.Praemien), me.Money);
                    neutral += me.Money - (startMoney + cumulative + neutral + game.Finance(me).Book.CurrentTotal);
                    keptSet = true;
                }
                Assert.Equal(startMoney + cumulative + neutral + game.Finance(me).Book.CurrentTotal, me.Money);
                Assert.InRange(game.Finance(me).SponsorRating, 0, 100);
                var sum = 4 * club.Strength + (club.TeamValues is { } v ? v.DefenseCondition + v.AttackCondition + v.DefenseTechnique + v.AttackTechnique : 0);
                Assert.Equal(5L * club.Players.Count * sum / FinanceRules.ThirdsPerMark, game.MonthlyBaseIncome(me));
                var history = game.RankHistory(club.Index);
                if (game.RankSlotsRecorded == game.PlayedMatchDays)
                    for (var i = 0; i < Math.Min(history.Count, GameState.RankSlotCount); i++)
                        Assert.Equal(history[i] - 1, club.StoredRankSlot(i));
                var n = game.LeagueMatchDayCounter(club.League!.Value);
                if (year > 0 && n - 1 < previous.Count)
                    Assert.Equal(previous[n - 1] - 1, game.RankSlot(club, n - 1));
            }
            previous = game.RankHistory(club.Index).ToList();
            cumulative += game.Finance(me).Book.CurrentTotal;
            TestSeason.KeepSquad(game, me);
            game.StartNextSeason();
        }
        if (active)
            Assert.True(signed >= 10, $"nur {signed} Verträge");
        else
            Assert.Equal(0, signed);
    }

    private static int SignBest(GameState game, Manager me)
    {
        var ads = game.Ads(me);
        var signed = 0;
        foreach (var kind in Enum.GetValues<AdSlotKind>())
        {
            var slots = kind == AdSlotKind.Bande ? ads.FreeBoards().Select(b => new AdSlot(kind, b)).ToList() : [new AdSlot(kind)];
            foreach (var slot in slots)
            {
                if (ads.ContractAt(slot) is not null || !ads.Offers.TryGetValue(kind, out var offers) || offers.Count == 0)
                    continue;
                var best = offers.Values.OrderByDescending(o => o.MonthlyAmount).First();
                if (game.AcceptOffer(me, slot, best.Sponsor) is not null)
                    signed++;
            }
        }
        return signed;
    }
}

/// <summary>Migration Schema 23 → 24 (Spur geld, Teil 2): Platzverlauf je Verein und Zähler.</summary>
public class SponsorBewertungMigrationTests
{
    [Fact]
    public void Schema23_GetsEmptyRankSlots()
    {
        var game = TestGames.Create();
        var json = System.Text.Json.Nodes.JsonNode.Parse(SaveGame.Save(game))!.AsObject();
        json["schema"] = 23;
        Assert.True(json["spiel"]!.AsObject().Remove("RankSlotsRecorded"));
        foreach (var club in json["vereine"]!.AsArray())
            Assert.True(club!.AsObject().Remove("_rankSlots"));

        var result = SaveGame.Load(System.Text.Encoding.UTF8.GetBytes(json.ToJsonString()));
        Assert.True(result.Success, result.Error);
        Assert.Equal(SaveGame.Save(game), SaveGame.Save(result.Game!));
    }
}
