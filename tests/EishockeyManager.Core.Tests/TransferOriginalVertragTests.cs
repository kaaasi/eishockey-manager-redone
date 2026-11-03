using System.Text.Json.Nodes;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur <c>transfer-original-vertrag</c>: Vertragsverhandlung 0x4A1DB, Annahme 0x49A3B, Sperren nach gescheiterter Verhandlung
/// (Kaderplatz +0x18) und nach abgelehntem Gebot (+0x1A), Reihenfolge Geld/Unfair (R8-C02), Zugfolge der Werbebewertung (SW-01).
/// Zufallsabhängige Regeln werden mit einem Zwillings-Zufallsstrom nachgerechnet, der die Züge unabhängig vom Code nachzieht.
/// </summary>
public class TransferOriginalVertragTests
{
    private static (GameState Game, Manager Manager, Club Club, Player Player) Setup(int seed = 3)
    {
        var game = TestGames.Create(seed);
        var manager = game.ActiveManager;
        var club = game.Clubs[manager.ClubIndex];
        var player = club.Players.First(p => p.Role == PlayerRole.Forward);
        player.ContractYears = 2;
        return (game, manager, club, player);
    }

    private static int SalaryValue(GameState game, Club club, Player p, int level = 3) =>
        game.PlayerValueOf(p, club, PlayerValueKind.Salary, new Random(0), level);

    private static int R(Random random, int a, int b) => SquadSetup.Between(random, Math.Min(a, b), Math.Max(a, b));

    /// <summary>
    /// Forderung 0x4A81B–0x4A9F2, von Hand nachgerechnet: Wert 9.000 Drittel, Faktor 100, Rest 100 %, 2 Jahre:
    /// U = 200, W = (33 + 122) · 8 / 10 = 124, S = 9.000 · 128 / 100 = 11.520; nach dem Saisonende mit Alter 27:
    /// · 125 / 100 = 14.400. Nie unter dem bisherigen Gehalt.
    /// </summary>
    [Theory]
    [InlineData(9_000, 100, 100, 2, false, 27, 0, 11_520)]
    [InlineData(9_000, 100, 100, 2, true, 27, 0, 14_400)]
    [InlineData(9_000, 85, 0, 1, false, 30, 0, 7_650 * 101 / 100)]
    [InlineData(9_000, 115, 50, 4, false, 30, 0, 10_350 * 148 / 100)]
    [InlineData(9_000, 100, 100, 2, false, 27, 20_000, 20_000)]
    public void Demand_FollowsOriginal(int value, int factor, int remaining, int years, bool after, int age, int current, int expected) =>
        Assert.Equal(expected, ContractRules.DemandThirds(value, factor, remaining, years, after, age, current));

    /// <summary>Restanteil 100 − 100 · nächster Spieltag / 44 (0x4A851): Spielbeginn 98, nach 22 Spieltagen 48, Ende −2.</summary>
    [Theory]
    [InlineData(1, 98)]
    [InlineData(23, 48)]
    [InlineData(45, -2)]
    public void SeasonRemaining_FollowsOriginal(int next, int expected) =>
        Assert.Equal(expected, ContractRules.SeasonRemaining(next));

    /// <summary>
    /// Annahme 0x49A3B mit Zwillingsstrom: Züge random(85, 115), random(W − 6, W + 4) und nach dem Saisonende
    /// random(132 − Alter, 152 − Alter), sonst keine. Vorher rot: das Remake nahm ab einer Forderung aus Stärke und Talent an.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Accepts_DrawsLikeOriginal(bool after)
    {
        var accepted = 0;
        for (var seed = 1; seed <= 200; seed++)
        {
            var random = new GameRandom(seed);
            var twin = GameRandom.FromState(random.Capture());
            var salary = 6_000 + seed * 60;
            const int value = 9_000, remaining = 50, years = 3, age = 29;

            var u = (years - 1) * 100 + remaining;
            var d = value * R(twin, 85, 115) / 100;
            var w = (u / 6 + 110) * 8 / 10;
            d = d * R(twin, w - 6, w + 4) / 100;
            if (after)
                d = d * R(twin, 132 - age, 152 - age) / 100;
            var expected = d < 100_000 && salary > d;

            Assert.Equal(expected, ContractRules.Accepts(2, 3_000, years, salary, value, remaining, after, age, random));
            Assert.Equal(twin.Capture(), random.Capture());
            if (expected)
                accepted++;
        }
        Assert.InRange(accepted, 1, 199);
    }

    /// <summary>
    /// Vorentscheidungen ohne Zufall (0x49A3B): gleiche Laufzeit und mehr bzw. weniger Gehalt; kürzere Laufzeit über 120 % bzw.
    /// bis 105 %; Forderung ab 100.000 Drittel nie.
    /// </summary>
    [Fact]
    public void Accepts_ShortCuts_DrawNothing()
    {
        var random = new GameRandom(5);
        var state = random.Capture();
        Assert.True(ContractRules.Accepts(2, 3_000, 2, 3_003, 9_000, 50, false, 25, random));
        Assert.False(ContractRules.Accepts(2, 3_000, 2, 2_997, 9_000, 50, false, 25, random));
        Assert.True(ContractRules.Accepts(3, 3_000, 2, 3_601, 9_000, 50, false, 25, random));
        Assert.False(ContractRules.Accepts(3, 3_000, 2, 3_150, 9_000, 50, false, 25, random));
        Assert.Equal(state, random.Capture());
        Assert.False(ContractRules.Accepts(2, 3_000, 4, 99_999_999, 200_000, 50, false, 25, random)); // D ≥ 100.000
    }

    /// <summary>
    /// „Mach' ich!“ (0x4AB66): über 4 Jahre „SOOOO lange…“; kürzer „So dumm ist … leider nicht...“; beides sperrt den Spieler
    /// random(10, 18) Tage. Vorher rot: das Remake erlaubte 5 Jahre und kannte weder Meldung noch Sperre.
    /// </summary>
    [Fact]
    public void Negotiate_TooLongAndTooShort_FailAndLock()
    {
        var (game, manager, _, p) = Setup();
        var twin = GameRandom.FromState(game.ContractRandom.Capture());

        Assert.Equal(NegotiationResult.TooLong, Contracts.Negotiate(game, manager, p, 5, 0, false, false));
        Assert.Equal(R(twin, 10, 18), p.NegotiationLock);
        Assert.Equal(["SOOOO lange", "verpflichtet", "sich kein", "Eishockey-Spieler !"], Contracts.Message(NegotiationResult.TooLong, p));

        p.NegotiationLock = 0;
        Assert.Equal(NegotiationResult.TooShort, Contracts.Negotiate(game, manager, p, 1, 0, false, false));
        Assert.Equal(R(twin, 10, 18), p.NegotiationLock);
        Assert.Equal(["So dumm", "ist", p.Name, "leider nicht..."], Contracts.Message(NegotiationResult.TooShort, p));
        Assert.Equal(2, p.ContractYears);
    }

    /// <summary>
    /// Gesperrt: Eingaben werden mit „ist im Moment nicht verhandlungsbereit.“ abgewiesen (0x4A704), ohne neue Sperre; die
    /// Sperre zählt täglich herunter (0x76F6). „Mach' ich!“ ohne Eingaben ändert nichts und gelingt (0x4ABFA).
    /// </summary>
    [Fact]
    public void Lock_RefusesInput_AndCountsDownDaily()
    {
        var (game, manager, _, p) = Setup();
        p.NegotiationLock = 3;
        Assert.Equal(NegotiationResult.NotWilling, Contracts.Availability(game, p));
        Assert.Equal(NegotiationResult.NotWilling, Contracts.Negotiate(game, manager, p, 3, 0, false, false));
        Assert.Equal(3, p.NegotiationLock);
        Assert.Equal(NegotiationResult.Accepted, Contracts.Negotiate(game, manager, p, 0, 0, insured: true, false));
        Assert.True(p.Insured);

        var day = game.Day;
        while (game.Day < day + 3)
            game.Continue();
        Assert.Equal(0, p.NegotiationLock);
        Assert.Null(Contracts.Availability(game, p));
    }

    /// <summary>Nach dem Saisonende (Tageszähler &gt; 232) gilt die Sperre nicht (0x4A70A).</summary>
    [Fact]
    public void Lock_DoesNotApplyAfterSeasonEnd()
    {
        var (game, _, _, p) = Setup();
        LifecycleRegressionTests.FinishSeason(game);
        p.NegotiationLock = 5;
        Assert.True(game.AfterSeasonEnd);
        Assert.Null(Contracts.Availability(game, p));
    }

    /// <summary>
    /// Ausgeliehen (0x4A692): „… ist leider von &lt;Verein&gt; nur ausgeliehen.“ Ruhestand: „… wird sich mit Ablauf des Vertrages
    /// zur Ruhe setzen.“
    /// </summary>
    [Fact]
    public void Availability_LoanAndRetirement()
    {
        var (game, manager, club, p) = Setup();
        var origin = game.Clubs.First(c => c != club && c.Players.Count > 0);
        p.OnLoan = true;
        p.Loan = new PlayerLoan(origin, 2, 900, false);
        Assert.Equal(NegotiationResult.Loaned, Contracts.Availability(game, p));
        Assert.Equal([p.Name, "ist leider von", origin.Name, "nur ausgeliehen."], Contracts.Message(NegotiationResult.Loaned, p));
        p.OnLoan = false;
        p.Loan = null;
        p.Notice = ContractNotice.Retiring;
        Assert.Equal(NegotiationResult.Retiring, Contracts.Negotiate(game, manager, p, 3, 0, false, false));
        Assert.Equal(0, p.NegotiationLock);
    }

    /// <summary>
    /// Eingabe der Laufzeit füllt das Gehalt mit der Forderung (Faktor des Tages, Restanteil, Kaderplatz 3 nur nach dem
    /// Saisonende), „Mach' ich!“ nimmt sie ohne Zufall an (0x4ABF4). Im Level 5 bleibt das Feld leer (0xED8 = 0).
    /// </summary>
    [Fact]
    public void DemandAfterYears_IsAcceptedDirectly()
    {
        var (game, manager, club, p) = Setup();
        var value = SalaryValue(game, club, p, manager.Level);
        var expected = Math.Max(ContractRules.DemandThirds(value, game.ContractTurnFactor, ContractRules.SeasonRemaining(1), 3,
            false, 0, p.MonthlySalary * 3), p.MonthlySalary * 3) / 3;
        var demand = Contracts.DemandAfterYears(game, manager, p, 3);
        Assert.Equal(expected, demand);
        Assert.InRange(game.ContractTurnFactor, 85, 115);
        Assert.Equal(game.ContractTurnFactor, game.ContractTurnFactor); // rein

        var state = game.ContractRandom.Capture();
        Assert.Equal(NegotiationResult.Accepted, Contracts.Negotiate(game, manager, p, 3, demand!.Value, false, demandFilled: true));
        Assert.Equal(state, game.ContractRandom.Capture());
        Assert.Equal((3, demand.Value), (p.ContractYears, p.MonthlySalary));

        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Test", Head = 0, ClubIndex = 2, Level = 5 });
        var hard = NewGame.Create(TestGames.CreateDatabase(), setup, 3);
        var hardManager = hard.Managers[0];
        Assert.Null(Contracts.DemandAfterYears(hard, hardManager, hard.Clubs[hardManager.ClubIndex].Players[0], 4));
    }

    /// <summary>
    /// Eigenes Gehalt (Merker gelöscht): 0x49A3B entscheidet mit dem Vertragszufall; abgelehnt → „… ist nicht an Ihrem Angebot
    /// interessiert.“ und Sperre.
    /// </summary>
    [Fact]
    public void OwnSalary_DecidedByOriginalAcceptance()
    {
        var results = new HashSet<NegotiationResult>();
        for (var seed = 1; seed <= 40; seed++)
        {
            var (game, manager, club, p) = Setup(seed);
            var value = SalaryValue(game, club, p, manager.Level);
            var offer = value * (100 + seed * 2) / 300; // 102–180 % des Werts um die Grenze (Rest 98 %, 3 Jahre: W = 127)
            var twin = GameRandom.FromState(game.ContractRandom.Capture());
            var expected = ContractRules.Accepts(2, p.MonthlySalary * 3, 3, offer * 3, value, game.ContractSeasonRemaining, false,
                p.Age, twin);
            var result = Contracts.Negotiate(game, manager, p, 3, offer, false, demandFilled: false);
            if (!expected)
                Assert.Equal(R(twin, 10, 18), p.NegotiationLock);
            Assert.Equal(expected ? NegotiationResult.Accepted : NegotiationResult.NotInterested, result);
            Assert.Equal(twin.Capture(), game.ContractRandom.Capture());
            results.Add(result);
        }
        Assert.Equal(2, results.Count);
        Assert.Equal(["X", "ist nicht an", "Ihrem Angebot", "interessiert."],
            Contracts.Message(NegotiationResult.NotInterested, new Player { Name = "X", Role = PlayerRole.Forward }));
    }

    /// <summary>
    /// Verlängerungsangebot des Spielers (Kaderplatz +0x18 &gt; 99): Der Dialog rechnet mit der angebotenen Laufzeit, „Mach'
    /// ich!“ ohne Eingabe nimmt es zum bisherigen Gehalt an (0x4A3AF, 0x4ABFA).
    /// </summary>
    [Fact]
    public void OfferedExtension_AcceptedWithoutInput()
    {
        var (game, manager, _, p) = Setup();
        p.ContractYears = 1;
        p.Notice = ContractNotice.Offered;
        p.NoticeYears = 3;
        var salary = p.MonthlySalary;
        Assert.Equal(3, Contracts.CurrentYears(p));
        Assert.Equal(NegotiationResult.Accepted, Contracts.Negotiate(game, manager, p, 0, 0, false, false));
        Assert.Equal((3, salary), (p.ContractYears, p.MonthlySalary));
        Assert.Equal(3, Contracts.CurrentYears(p)); // eingelöst: jetzt die eigene Laufzeit
    }

    /// <summary>
    /// Abgelehntes Gebot vom Computermarkt sperrt den Spieler für diesen Manager (0x472F4): weiteres Gebot → „Angebot wurde
    /// bereits abgelehnt.“ ohne Zufall. Vorher rot: das Remake erlaubte beliebig viele Gebote.
    /// </summary>
    [Fact]
    public void RejectedBid_LocksListing()
    {
        var (game, manager, _, _) = Setup();
        manager.Money = 10_000_000;
        var listing = game.TransferMarket.Listings.First(l => l.Seller is null && !l.IsFreeAgent);
        Assert.Equal(TransferResult.Rejected, game.TransferMarket.Buy(game, manager, listing, listing.Price / 2, false));
        Assert.Equal(0x81, listing.RejectedBy);
        Assert.True(TransferMarket.IsLocked(game, manager, listing));
        var state = game.Random.Capture();
        Assert.Equal(TransferResult.AlreadyRejected, game.TransferMarket.Buy(game, manager, listing, listing.Price * 2, false));
        Assert.Equal(state, game.Random.Capture());
        Assert.Equal(["Angebot wurde", "bereits abgelehnt."], TransferMarket.Message(TransferResult.AlreadyRejected));
    }

    /// <summary>
    /// Tagesablauf 0x071CC: allein Manager 0 gesperrt → random(0, 2) = 0 hebt seine Sperre auf (Bit 0x80 bleibt); sind
    /// weitere Manager gesperrt oder nur andere, bleibt alles (und es wird nicht gezogen).
    /// </summary>
    [Fact]
    public void ReleaseBidLocks_OnlyManagerZeroAlone()
    {
        var (game, _, _, _) = Setup();
        var market = game.TransferMarket;
        var listings = market.Listings.Take(3).ToList();
        listings[0].RejectedBy = 0x81;
        listings[1].RejectedBy = 0x83;
        listings[2].RejectedBy = 0x82;
        for (var seed = 1; seed < 30; seed++)
        {
            listings[0].RejectedBy = 0x81;
            var random = new GameRandom(seed);
            var twin = GameRandom.FromState(random.Capture());
            market.ReleaseBidLocks(random);
            Assert.Equal(R(twin, 0, 2) == 0 ? 0x80 : 0x81, listings[0].RejectedBy);
            Assert.Equal(twin.Capture(), random.Capture());
            Assert.Equal((0x83, 0x82), (listings[1].RejectedBy, listings[2].RejectedBy));
        }
    }

    /// <summary>
    /// R8-C02: Beim Gebot an einen anderen Manager prüft das Original das Geld (0x474F0) vor der Unfair-Grenze (0x47546).
    /// Vorher rot: unfair und zu wenig Geld meldete „unfair“.
    /// </summary>
    [Fact]
    public void BidToOtherManager_MoneyCheckedBeforeUnfair()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "A", Head = 0, ClubIndex = 2 });
        setup.Managers.Add(new ManagerSetup { Name = "B", Head = 1, ClubIndex = 3 });
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, 3);
        var (a, b) = (game.Managers[0], game.Managers[1]);
        var player = game.Clubs[b.ClubIndex].Players.First(p => p.Role == PlayerRole.Forward);
        Assert.Equal(TransferResult.Accepted, game.TransferMarket.Offer(game, b, player));
        var listing = game.TransferMarket.ListingOf(player)!;
        a.Money = 1_000;
        Assert.Equal(TransferResult.NotEnoughMoney, game.TransferMarket.Buy(game, a, listing, listing.Price * 3, false));
        a.Money = 100_000_000;
        Assert.Equal(TransferResult.Unfair, game.TransferMarket.Buy(game, a, listing, listing.Price * 3, false));
    }

    /// <summary>
    /// SW-01: Werbebewertung mit einem Spieler mit Angebot. Zugfolge 0x3F9E7/0x3FA56: Wert aller Spieler (mit Angebot
    /// random(95, 100)), dann je Spieler Wert erneut und random(35, 55), zuletzt random(20, 50). Die Mutation „zweite Schleife
    /// ohne Neuaufruf“ fällt hier auf.
    /// </summary>
    [Fact]
    public void SponsorRating_SecondPassRecomputesValueWithOffer()
    {
        var (game, manager, club, player) = Setup();
        game.TransferMarket.Offer(game, manager, player);
        for (var day = 0; day < 60 && game.TransferMarket.Bids.Count == 0; day++)
            game.TransferMarket.AdvanceDay(game, game.Random);
        Assert.Single(game.TransferMarket.Bids);
        foreach (var pl in club.Players)
            (pl.Goals, pl.Games) = (30, 500);

        var twin = new Random(11);
        var finance = game.Finance(manager);
        var tier = (int)finance.CostLeague;
        var p = game.CurrentParticipation(club.Index);
        long Value(Player pl) => game.PlayerValueOf(pl, club, PlayerValueKind.Price, twin, manager.Level);
        var s = SponsorRating.CompetitionScore(tier, p.Championship, p.Cup, p.CupRound, p.Europe, p.EuropeRound) + 12L * (11 - 6);
        var first = club.Players.Select(Value).ToList();
        var average = first.Sum(v => v / 5500) / first.Count;
        s += average;
        var stars = 0;
        foreach (var pl in club.Players)
        {
            var second = Value(pl);
            var threshold = average * SquadSetup.Between(twin, 35, 55) / 10;
            if ((ulong)(second / 7000) > (ulong)threshold && stars < 4)
                stars++;
        }
        s += (long)SquadSetup.Between(twin, 20, 50) * stars + 3L * SponsorRating.StadiumWorth(finance.Stadium);

        var random = new Random(11);
        Assert.Equal(SponsorRating.Finish(s, tier), SponsorRating.Compute(game, manager, random));
        Assert.Equal(twin.Next(), random.Next()); // gleiche Zahl an Zügen
    }

    /// <summary>
    /// Migration 14 → 15: „RejectedOfferDay“ entfällt, Sperre 0; Markt-Einträge ohne Gebotssperre. Laden ergibt denselben
    /// Spielstand wie das Original.
    /// </summary>
    [Fact]
    public void Migration_FromSchema14()
    {
        var (game, _, _, p) = Setup();
        var json = JsonNode.Parse(SaveGame.Save(game))!.AsObject();
        json["schema"] = 14;
        foreach (var player in json["spieler"]!.AsArray().OfType<JsonObject>())
        {
            player.Remove("NegotiationLock");
            player["RejectedOfferDay"] = 3;
        }
        var listings = 0;
        Strip(json);
        Assert.True(listings > 0);

        var result = SaveGame.Load(System.Text.Encoding.UTF8.GetBytes(json.ToJsonString()));
        Assert.True(result.Success, result.Error);
        var loaded = result.Game!;
        Assert.All(loaded.Clubs.SelectMany(c => c.Players), pl => Assert.Equal(0, pl.NegotiationLock));
        Assert.All(loaded.TransferMarket.Listings, l => Assert.Equal(0, l.RejectedBy));
        Assert.Equal(SaveGame.Save(game), SaveGame.Save(loaded));
        Assert.NotNull(p);

        void Strip(JsonNode? node)
        {
            if (node is JsonObject obj)
            {
                if (obj.Remove("RejectedBy"))
                    listings++;
                foreach (var (_, child) in obj.ToArray())
                    Strip(child);
            }
            else if (node is JsonArray array)
                foreach (var child in array)
                    Strip(child);
        }
    }

    /// <summary>
    /// Mehrsaisonprobe (4 Saisonwechsel, passiver und aktiver Manager). Aktiv: verhandelt jeden Spieltag mit dem ersten Spieler
    /// im letzten Vertragsjahr (eigene Laufzeit 2–5, Gehalt um die Forderung) und bietet für den ersten Markt-Spieler 80 %.
    /// Sperren bleiben 0–18, der Kader bleibt gültig, Spielstand bleibt speicherbar; zwei gleiche Läufe sind gleich. Das Konto
    /// prüft die Probe nicht: Die Geldsperre (10 Mio. je Tag) hält den aktiven Manager im Amt; ohne sie kauft er sich in den
    /// Dispo (80 % täglich, Abstieg nach Saison 2, Gehälter über dem Eintritt, Dispozinsen 0x3ED4F; R9S-05, #353).
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FourSeasonChanges_StayConsistent(bool active)
    {
        string Run()
        {
            var game = TestGames.Create(7);
            var manager = game.Managers[0];
            var log = new List<string>();
            var rng = new Random(3);
            for (var season = 0; season < 4; season++)
            {
                for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
                {
                    Assert.True(guard < 500);
                    // Integration Runde 9 (Leitstelle): keine Entlassung – die Probe prüft Sperren und Wiederholbarkeit, nicht das
                    // Konto. Auf dem integrierten Stand kauft der aktive Manager sich bis 1996 über die Entlassungsgrenze
                    // (Phase GameOver am letzten Tag, StartNextSeason scheiterte).
                    manager.Money = Math.Max(manager.Money, 10_000_000);
                    var club = game.Clubs[manager.ClubIndex];
                    TestSeason.KeepSquad(game, manager, extendExpired: false); // die Probe verhandelt selbst mit dem Kader
                    if (active)
                    {
                        if (club.Players.FirstOrDefault(pl => pl.ContractYears <= 1 && Contracts.Availability(game, pl) is null) is { } p)
                        {
                            var years = rng.Next(2, 6);
                            var salary = (Contracts.DemandAfterYears(game, manager, p, Math.Min(years, 4)) ?? p.MonthlySalary)
                                         * rng.Next(80, 121) / 100;
                            log.Add($"{game.Day}:{p.Id}:{Contracts.Negotiate(game, manager, p, years, salary, false, false)}");
                        }
                        if (game.TransferMarket.Listings.FirstOrDefault(l => l.Seller is null && !l.IsFreeAgent) is { } listing)
                            log.Add($"{game.Day}:{game.TransferMarket.Buy(game, manager, listing, listing.Price * 8 / 10, false)}");
                    }
                    game.Continue();
                    foreach (var pl in game.Clubs.SelectMany(c => c.Players))
                        Assert.InRange(pl.NegotiationLock, 0, 18);
                    Assert.All(game.TransferMarket.Listings, l => Assert.Equal(0, l.RejectedBy & ~0x81));
                }
                Assert.Equal(SeasonPhase.SeasonOver, game.Phase);
                log.Add($"S{season}:{manager.Money}:{game.Clubs[manager.ClubIndex].Players.Count}");
                Assert.NotEmpty(SaveGame.Save(game));
                game.StartNextSeason();
            }
            Assert.Contains(log, l => active ? l.Contains("NotInterested") || l.Contains("TooLong") : l.StartsWith('S'));
            return string.Join("|", log);
        }

        Assert.Equal(Run(), Run());
    }
}
