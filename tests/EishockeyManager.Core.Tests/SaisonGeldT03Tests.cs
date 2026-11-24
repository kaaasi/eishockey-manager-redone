using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur saison-geld, Teil 1 (T03: A661, A829, A843, A935; Beleg docs/analyse/re-abweichungen.md T03): Zeitpunkt und Reihenfolge
/// der Würfe im Markt-Takt und am Saisonwechsel wie em.exe. Vor der Spur rot: Gebotsverfall nur je Gebot, Oberliga-Zug am
/// 13. April, große Wanderung erst in <see cref="GameState.StartNextSeason"/> nach der Mischung.
/// </summary>
public class SaisonGeldT03Tests
{
    private static GameState Create(int seed, params int[] clubs)
    {
        var setup = new GameSetup { Duration = GameDuration.Unbegrenzt };
        for (var i = 0; i < clubs.Length; i++)
            setup.Managers.Add(new ManagerSetup { Name = $"M{i}", Head = i, ClubIndex = clubs[i] });
        return NewGame.Create(TestGames.CreateDatabase(), setup, seed);
    }

    private static GameRandom Copy(GameRandom random) => GameRandom.FromState(random.Capture());

    /// <summary>Spielt bis <paramref name="until"/> erfüllt ist (Manager behalten Kader und Geld, der Test prüft den Ablauf).</summary>
    private static void PlayUntil(GameState game, Func<GameState, bool> until)
    {
        for (var guard = 0; !until(game); guard++)
        {
            Assert.True(guard < 3000, "Zielzustand nicht erreicht");
            foreach (var m in game.Managers)
            {
                m.Money = 10_000_000;
                TestSeason.KeepSquad(game, m);
            }
            if (game.Phase == SeasonPhase.SeasonOver)
                game.StartNextSeason();
            else
                game.Continue();
        }
    }

    /// <summary>
    /// A829: 0x07169 im Lauf von Manager 0 (Datei 0x0718F): je Markt-Platz Verfall, Sperrwurf, dann <c>random(0, 3)</c> für
    /// <b>jeden</b> Platz (Datei 0x07215), erst danach die Prüfung des offenen Gebots (Datei 0x07227–0x07247). Ohne Sperren,
    /// Gebote und eigene Plätze zieht der Markt-Teil also genau einen Wurf je Platz. Vorher rot: kein Wurf.
    /// </summary>
    [Fact]
    public void MarketPart_RollsExpiryForEveryPlace_EvenWithoutBid()
    {
        var game = Create(3, 2);
        var day = game.CurrentDay;
        Assert.Empty(game.TransferMarket.Bids);
        Assert.All(game.TransferMarket.Listings, l => Assert.Equal(0, l.RejectedBy & 0x7F));
        Assert.True(game.TransferMarket.Listings.Count >= 3);
        var sim = Copy(game.Random);
        foreach (var _ in game.TransferMarket.Listings)
            TransferRules.Random16(sim, 0, 3);

        game.DailyRunMarket(day, game.Managers[0]);

        Assert.Equal(sim.Capture(), game.Random.Capture());
    }

    /// <summary>
    /// A829: Sperrwurf und Verfallswurf verschränkt je Platz (Satzfolge), Gebot fällt bei <c>random(0, 3)</c> = 0. Der Zwilling
    /// rechnet die Folge Platz für Platz von Hand. Vorher rot: erst alle Sperrwürfe, dann ein Wurf je Gebot.
    /// </summary>
    [Fact]
    public void MarketPart_LockAndExpiryInterleavedPerPlace()
    {
        var checkedRuns = 0;
        for (var seed = 1; seed <= 8; seed++)
        {
            // Gebote auf Plätzen von Manager 1: Der Lauf von Manager 0 würfelt danach keine Angebote (zweite Schleife nur eigene Plätze).
            var game = Create(seed, 2, 5);
            var other = game.Managers[1];
            var offered = game.Clubs[other.ClubIndex].Players.Where(p => p.Role == PlayerRole.Forward).Take(2).ToList();
            foreach (var p in offered)
                game.TransferMarket.Offer(game, other, p);
            for (var day = 0; day < 400 && game.TransferMarket.Bids.Count < 2; day++)
                game.TransferMarket.AdvanceDay(game, game.Random);
            if (game.TransferMarket.Bids.Count < 2)
                continue;
            var day0 = game.CurrentDay;
            var listings = game.TransferMarket.Listings.ToList();
            var withBid = game.TransferMarket.Bids.Select(b => b.Listing).ToHashSet();
            var free = listings.Where(l => !withBid.Contains(l)).ToList();
            free[0].RejectedBy = 0x81;
            withBid.First().RejectedBy = 0x81;

            var sim = Copy(game.Random);
            var expectedLocks = new List<int>();
            var expectedBids = new List<TransferListing>();
            foreach (var listing in listings)
            {
                var locked = (listing.RejectedBy & 0x7F) == 1;
                expectedLocks.Add(locked && TransferRules.Random16(sim, 0, 2) == 0 ? listing.RejectedBy ^ 1 : listing.RejectedBy);
                if (TransferRules.Random16(sim, 0, 3) != 0 && withBid.Contains(listing))
                    expectedBids.Add(listing);
            }

            game.DailyRunMarket(day0, game.Managers[0]);

            Assert.Equal(expectedLocks, listings.Select(l => l.RejectedBy).ToList());
            Assert.Equal(expectedBids.ToHashSet(), game.TransferMarket.Bids.Select(b => b.Listing).ToHashSet());
            Assert.Equal(sim.Capture(), game.Random.Capture());
            checkedRuns++;
        }
        Assert.True(checkedRuns >= 3, $"nur {checkedRuns} Aufbauten mit zwei Geboten");
    }

    /// <summary>
    /// A661: Der Oberliga-Zug <c>random(36, 41)</c> fällt am 29. April im Austausch aller Gruppen (Datei 0x56275, 0x56137–0x5636D),
    /// nicht am 13. April. Vorher rot: Absteiger und Nachrücker schon in der Phase „Vereinssaison vorbei“ eingetragen.
    /// </summary>
    [Fact]
    public void OberligaDraw_On29April_NotOn13April()
    {
        var game = Create(5, 2);
        PlayUntil(game, g => g.Phase == SeasonPhase.ClubSeasonOver);
        var record = Assert.IsType<SeasonRecord>(game.CurrentRecord);
        Assert.Empty(record.LeftOberliga);
        Assert.Empty(record.PromotedInto[League.Oberliga]);
        var candidates = game.OutsidePlaces().Take(GameState.OutsideDrawPlaces).ToHashSet();
        var bottom = record.FinalTables[League.Oberliga].TakeLast(2).Reverse().ToList();

        PlayUntil(game, g => g.Phase == SeasonPhase.SeasonOver);

        Assert.Equal(bottom, record.LeftOberliga);
        Assert.Equal(2, record.PromotedInto[League.Oberliga].Distinct().Count());
        Assert.All(record.PromotedInto[League.Oberliga], c => Assert.Contains(c, candidates));
    }

    /// <summary>
    /// A843/A935: Die große Wanderung (0x09D1B Modus 10, Datei 0x56448) läuft am 29. April vor der Mischung, nicht im Saisonwechsel.
    /// Vorher rot: Teamwerte am Bildschirm „Saisonende“ unverändert, gewandert erst in <see cref="GameState.StartNextSeason"/>.
    /// </summary>
    [Fact]
    public void SeasonDrift_On29April_NotInStartNextSeason()
    {
        var game = Create(7, 2);
        List<ClubTeamValues?>? before = null;
        for (var guard = 0; game.Phase != SeasonPhase.SeasonOver; guard++)
        {
            Assert.True(guard < 3000, "Saisonende nicht erreicht");
            foreach (var m in game.Managers)
            {
                m.Money = 10_000_000;
                TestSeason.KeepSquad(game, m);
            }
            before = game.Clubs.Select(c => c.TeamValues).ToList();
            game.Continue();
        }
        var atSeasonEnd = game.Clubs.Select(c => c.TeamValues).ToList();
        // Vor dem letzten Schritt (bis 28. April, ohne Bundestrainer ab dem 14. April) gibt es keine Wanderung mehr; am 29. April
        // würfelt die große Wanderung die Form jedes Vereins neu.
        var formChanged = game.Clubs.Where(c => c.Index < NewGame.SelectableClubs && c.TeamValues is not null)
            .Count(c => before![c.Index] != atSeasonEnd[c.Index]);
        Assert.True(formChanged > 40, $"nur {formChanged} Vereine gewandert");

        game.StartNextSeason();

        // Managervereine: Teamwerte aus der Aufstellung (0x952C) im Saisonwechsel, keine Wanderung.
        bool Computer(Club c) => !game.IsClubManaged(c.Index);
        Assert.Equal(atSeasonEnd.Where((_, i) => Computer(game.Clubs[i])), game.Clubs.Where(Computer).Select(c => c.TeamValues));
    }

    /// <summary>
    /// A843/A935: Reihenfolge der Wanderung am 29. April = Satzfolge der alten Saison, Aufsteiger auf dem Platz ihres Absteigers
    /// (0x36169 mit Schalter 1), dann die Plätze ohne Liga nach dem Oberliga-Austausch; Grenzen nach der Liga des neuen Platzes.
    /// </summary>
    [Fact]
    public void SeasonDriftOrder_PromotedTakeSlotOfRelegatedPartner()
    {
        var game = Create(11, 2);
        PlayUntil(game, g => g.Phase == SeasonPhase.ClubSeasonOver);
        var oldOrder = Enum.GetValues<League>().SelectMany(game.LeagueSlots).ToList();
        PlayUntil(game, g => g.Phase == SeasonPhase.SeasonOver);
        var record = game.History[^1];

        var order = game.SeasonEndDriftOrder(record);

        Assert.Equal(NewGame.SelectableClubs, order.Distinct().Count());
        // Bundesliga: Wer bleibt, behält seinen Satzplatz; die Aufsteiger stehen auf den Plätzen der Absteiger.
        var promoted = record.PromotedInto[League.Bundesliga];
        Assert.NotEmpty(promoted);
        var stays = record.FinalTables[League.Bundesliga].Where(c => record.NextLeague(c, League.Bundesliga) == League.Bundesliga).ToHashSet();
        for (var i = 0; i < GameState.ClubsPerLeague; i++)
            if (stays.Contains(oldOrder[i]))
                Assert.Equal(oldOrder[i], order[i]);
            else
                Assert.Contains(order[i], promoted);
        // Grenzen des neuen Platzes: Aufsteiger in die Bundesliga liegen danach in 69 … 94.
        foreach (var club in promoted.Select(c => game.Clubs[c]))
        {
            var v = club.TeamValues!.Value;
            Assert.InRange(club.Strength + v.DefenseCondition, 69, 94);
            Assert.InRange(club.Strength + v.AttackTechnique, 69, 94);
        }
    }

    /// <summary>
    /// Mehrsaisonprobe (4 Saisonwechsel, passiver und aktiver Manager): je Saison am 13. April kein Oberliga-Zug, am 29. April zwei
    /// Absteiger und zwei verschiedene Nachrücker aus den Plätzen 36–41; nach der Wanderung liegen die Kondition- und Technikwerte jedes
    /// Ligavereins in den Grenzen seiner neuen Liga, <see cref="GameState.StartNextSeason"/> wandert nicht mehr; Ligen 12/12/12.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FourSeasonChanges_DrawAndDriftOn29April(bool active)
    {
        var game = TestGames.Create(13);
        var clubSeasonOverSeen = 0;
        string? candidates = null;
        RngTwinProbeTests.Run(game, active,
            afterStep: g =>
            {
                if (g.Phase == SeasonPhase.ClubSeasonOver && g.CurrentRecord is { } r)
                {
                    Assert.Empty(r.LeftOberliga);
                    candidates = string.Join(",", g.OutsidePlaces().Take(GameState.OutsideDrawPlaces));
                    clubSeasonOverSeen++;
                }
            },
            beforeChange: g =>
            {
                var record = g.CurrentRecord!;
                Assert.Equal(2, record.LeftOberliga.Count);
                Assert.Equal(2, record.PromotedInto[League.Oberliga].Distinct().Count());
                if (candidates is not null)
                    Assert.All(record.PromotedInto[League.Oberliga].Where(c => !record.LeftOberliga.Contains(c)),
                        c => Assert.Contains(c.ToString(), candidates.Split(',')));
                foreach (var club in g.Clubs.Where(c => c.Index < NewGame.SelectableClubs && c.TeamValues is not null && !g.IsClubManaged(c.Index)))
                {
                    var (min, max) = MatchEngine.DriftBounds(record.NextLeague(club.Index, club.League));
                    var v = club.TeamValues!.Value;
                    foreach (var d in new[] { v.DefenseCondition, v.AttackCondition, v.DefenseTechnique, v.AttackTechnique })
                        Assert.InRange(club.Strength + d, min, max);
                }
                candidates = null;
            });
        Assert.True(clubSeasonOverSeen >= 4, $"nur {clubSeasonOverSeen} Halte nach dem 13. April");
        foreach (var league in Enum.GetValues<League>())
            Assert.Equal(GameState.ClubsPerLeague, game.LeagueClubs(league).Count());
    }
}
