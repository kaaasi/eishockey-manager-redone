using System.Text.Json.Nodes;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur <c>transfer-vertraege-komplett</c>, Teil 5 (Entscheidungen Nutzer 01.11.2026): Listenwechsel 0x494EF aus dem
/// Spielerpool von 200 Spielern statt aus den Kadern der Computervereine; Gutschrift bei Markt-Angeboten nach 0x49332
/// (Angebot − Angebot / 7 · Restlaufzeit). Vorher rot: Der Markt zog Spieler aus Computerkadern, ein Kauf leerte deren
/// Kader, und ein angenommenes Gebot schrieb den vollen Betrag gut.
/// </summary>
public class TransferVertraegeKomplettTeil5Tests
{
    /// <summary>random(a, b) liefert immer a (Next(min, max) = min).</summary>
    private sealed class Lowest : Random
    {
        public override int Next(int minValue, int maxValue) => minValue;
        public override int Next(int maxValue) => 0;
        public override int Next() => 0;
        public override double NextDouble() => 0;
    }

    /// <summary>
    /// Rot vor Teil 5: Ein Kauf vom Markt nahm den Spieler aus dem Kader eines Computervereins. [ORIGINAL] Computervereine
    /// haben im Original keine Spieler; der Markt-Spieler ist ein Poolspieler (Besitzer 4 → Manager, 0x479B5).
    /// </summary>
    [Fact]
    public void Buy_FromMarket_LeavesComputerSquadsUnchanged()
    {
        var game = TestGames.Create(5);
        var manager = game.ActiveManager;
        manager.Money = 10_000_000;
        var before = game.Clubs.Where(c => c.Index != manager.ClubIndex).ToDictionary(c => c, c => c.Players.ToArray());
        var listing = game.TransferMarket.Listings.First(l => l.Seller is null && !l.IsFreeAgent);
        Assert.Equal(TransferResult.Accepted, game.TransferMarket.Buy(game, manager, listing, listing.Price * 2, loan: false));
        Assert.Contains(listing.Player, game.Clubs[manager.ClubIndex].Players);
        Assert.All(before, kv => Assert.Equal(kv.Value, kv.Key.Players));
    }

    /// <summary>
    /// Rot vor Teil 5: Das Remake schrieb das volle Angebot gut. [ORIGINAL] 0x49332–0x49371: Angebot − Angebot / 7 ·
    /// Restlaufzeit (Drittel-DM), z. B. 300.000 DM bei 2 Jahren → 900.000 − 128.571 · 2 = 642.858 Drittel = 214.286 DM.
    /// </summary>
    [Fact]
    public void Accept_MarketBid_CreditsOfferMinusSeventhPerYear()
    {
        var game = TestGames.Create();
        var manager = game.ActiveManager;
        var club = game.Clubs[manager.ClubIndex];
        var player = club.Players.First(p => p.Role == PlayerRole.Forward);
        player.ContractYears = 2;
        game.TransferMarket.Offer(game, manager, player);
        for (var day = 0; day < 200 && game.TransferMarket.Bids.Count == 0; day++)
            game.TransferMarket.AdvanceDay(game, game.Random);
        var bid = Assert.Single(game.TransferMarket.Bids);
        var expected = (TransferRules.Thirds(bid.Amount) - TransferRules.Thirds(bid.Amount) / 7 * 2) / 3;
        Assert.Equal(expected, TransferMarket.TransferFee(bid));
        Assert.Equal(214_286, UnsolicitedOfferRules.MarketTransferFeeThirds(900_000, 2) / 3);
        var money = manager.Money;
        Assert.Equal(TransferResult.Accepted, game.TransferMarket.Accept(game, bid));
        Assert.Equal(money + expected, manager.Money);
        Assert.Equal(expected, game.Finance(manager).Book.Current(FinanceCategory.Transfers));
    }

    /// <summary>
    /// [ORIGINAL] Pool zu Spielbeginn (0x5AF50, 0x5AAA0, 0x5ACA0): 199 Plätze, Art nach Index, Alter 18–33, Talent 0–9; Namen
    /// 1–178 aus Bundesligavereinen ohne Manager (nach Doppeltreffern 0–35), 179–199 aus Nationalteams; keine Doppelten.
    /// </summary>
    [Fact]
    public void Pool_AtStart_FollowsOriginalLayout()
    {
        var game = TestGames.Create(7);
        var pool = game.TransferMarket.Pool;
        // Die synthetische Datenbank hat keine Nationalkader; dann behalten 179–199 ihren Bundesliganamen.
        var nationalSquads = game.Clubs.Skip(NationalTeams.First).Take(12).Any(c => c.Players.Count > 0);
        Assert.Equal(Enumerable.Range(1, 199), pool.Select(s => s.Index));
        Assert.All(pool, s =>
        {
            Assert.Equal(TransferPoolRules.RoleOf(s.Index), s.Player.Role);
            Assert.InRange(s.Player.Age, 18, 33);
            Assert.InRange(s.Player.Talent, 0, 9);
            Assert.NotEqual(PoolOwner.Taken, s.Owner);
            Assert.DoesNotContain(s.Player, game.Clubs.SelectMany(c => c.Players));
            if (s.Index < 179)
            {
                Assert.InRange(s.Club, 0, 35);
                Assert.NotEqual(game.ActiveManager.ClubIndex, s.Club);
            }
            else if (nationalSquads)
                Assert.InRange(s.Club, NationalTeams.First, NationalTeams.Last);
        });
        Assert.True(pool.Count(s => s.Index < 179 && s.Club < 12) > 60, "Namen vor allem aus der Bundesliga");
        Assert.Equal(178, pool.Take(178).Select(s => s.Player.Name).Distinct().Count());
        Assert.Equal(21, pool.Skip(178).Select(s => s.Player.Name).Distinct().Count());
        Assert.Equal([20, 76, 83, 4, 7, 9], new[] { (1, 20), (21, 96), (97, 179), (180, 183), (184, 190), (191, 199) }
            .Select(r => pool.Count(s => s.Index >= r.Item1 && s.Index <= r.Item2)));
        Assert.Equal(PlayerRole.Goalkeeper, TransferPoolRules.RoleOf(183));
        Assert.Equal(PlayerRole.Defender, TransferPoolRules.RoleOf(184));
        Assert.Equal(PlayerRole.Forward, TransferPoolRules.RoleOf(191));
        Assert.Empty(game.ValidatePlayerIdentity());
    }

    /// <summary>
    /// Originaldaten: Pool mit der Originaldatenbank wie in FILE05 (Manager Köln, Verein 2). Im Original stehen freie Plätze
    /// 1–178 auf Vereinen 0–23 ohne den Managerverein, überwiegend 0–11 (0x5AAA0, dann Umverteilung 0x057E3), 179–199 auf
    /// Nationalteams 52–63; jeder Name ist ein Datenbankspieler (bei seinem Verein nur die Nationalspieler und die nicht
    /// umverteilten Plätze). Das Remake hält dieselben Bereiche.
    /// </summary>
    [GameDataFact]
    public void Pool_OriginalDatabase_MatchesFile05Layout()
    {
        var save = OriginalSaveGame.Read(File.ReadAllBytes(TransferOriginalTests.OriginalSave("FILE05.HOK")));
        var original = save.Block("far:8253");
        var free = Enumerable.Range(1, 199)
            .Select(i => (Index: i, Owner: original[i * OriginalSaveGame.PoolRecordLength + 0x1A],
                Club: original[i * OriginalSaveGame.PoolRecordLength + 0x1D]))
            .Where(e => e.Owner is 4 or 6).ToList();
        Assert.All(free.Where(e => e.Index < 179), e => Assert.InRange(e.Club, 0, 23));
        Assert.All(free.Where(e => e.Index < 179), e => Assert.NotEqual(2, e.Club));
        Assert.All(free.Where(e => e.Index >= 180), e => Assert.InRange(e.Club, NationalTeams.First, NationalTeams.Last));
        var share = free.Count(e => e.Index < 179 && e.Club < 12) / (double)free.Count(e => e.Index < 179);
        Assert.True(share > 0.9, $"FILE05: Anteil 0–11 {share:P0}");

        var database = ClubDatabase.Load(TestGameData.Load());
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Joerg", ClubIndex = 2 });
        var game = NewGame.Create(database, setup, 5);
        var pool = game.TransferMarket.Pool;
        Assert.All(pool.Where(s => s.Index < 179), s => Assert.InRange(s.Club, 0, 35));
        Assert.All(pool.Where(s => s.Index < 179), s => Assert.NotEqual(2, s.Club));
        Assert.All(pool.Where(s => s.Index >= 179), s => Assert.InRange(s.Club, NationalTeams.First, NationalTeams.Last));
        // [APPROX] Im Original belegen die Managerkader rund 25 Plätze (DGROUP 0x381), nur die übrigen bekommen Namen; das Remake
        // benennt alle 178 und trifft öfter einen schon vergebenen Namen (dann 0–35). Mit Seed 5: 141 von 178.
        Assert.True(pool.Count(s => s.Index < 179 && s.Club < 12) / 178.0 > 0.7, "Remake: Anteil 0–11");
        // Spur kader-transfer, Teil 5: Die Umverteilung beim Spielstart (0x057E3) hängt die Spieler an Vereine der Managerliga;
        // nur die Nationalspieler und die nicht umverteilten Plätze stehen noch beim Verein ihres Namens (FILE05: 8 von 154).
        Assert.All(pool, s => Assert.Contains(database.Clubs, c => c.Players.Any(d => d.Name == s.Player.Name)));
        Assert.All(pool.Where(s => s.Index >= 179),
            s => Assert.True(database[s.Club].Players.Any(d => d.Name == s.Player.Name), s.Player.Name + "@" + s.Club));
        // Entscheidung Nutzer 01.11.2026: Die Datenbankspieler des Pools fehlen den Computerkadern – jeder Name nur einmal.
        var squadNames = game.Clubs.SelectMany(c => c.Players).Select(p => p.Name).ToList();
        Assert.Empty(pool.Select(s => s.Player.Name).Intersect(squadNames));
        Assert.Equal(squadNames.Count, squadNames.Distinct().Count());
        Assert.All(game.Clubs.Where(c => c.League is not null), c => Assert.InRange(c.Players.Count, 18, 25));
        Assert.Equal(178, pool.Take(178).Select(s => s.Player.Name).Distinct().Count());
        Assert.Equal(21, pool.Skip(178).Select(s => s.Player.Name).Distinct().Count());
    }

    /// <summary>
    /// [ORIGINAL] Startliste = Listenwechsel ab leerem Markt: jeder der 10 Würfe trifft mit (10 − n) / 10 einen freien Platz,
    /// im Mittel 6,5 Einträge (Video 6, FILE05 7). Alle aus dem Pool, Besitzer „Markt“, Form 45–55, Stärken 10–99.
    /// </summary>
    [Fact]
    public void StartList_FromPool_AboutSixToSeven()
    {
        var counts = new List<int>();
        for (var seed = 1; seed <= 30; seed++)
        {
            var game = TestGames.Create(seed);
            var market = game.TransferMarket;
            counts.Add(market.Listings.Count);
            Assert.All(market.Listings, l =>
            {
                Assert.True(l.FromPool);
                var slot = market.Pool.Single(s => s.Player == l.Player);
                Assert.Equal(PoolOwner.Market, slot.Owner);
                Assert.Same(game.Clubs[slot.Club], l.From);
                Assert.InRange(l.Player.Form, 45, 55);
                Assert.InRange(l.Player.Condition, 10, 99);
                Assert.InRange(l.Player.Technique, 10, 99);
                Assert.Equal(TransferMarket.Price(l.Player), l.Price);
            });
            Assert.Equal(market.Listings.Count, market.Pool.Count(s => s.Owner == PoolOwner.Market));
        }
        Assert.All(counts, c => Assert.InRange(c, 1, TransferPoolRules.MarketSlots));
        Assert.InRange(counts.Average(), 5.5, 7.5);
    }

    /// <summary>
    /// [ORIGINAL] 0x494FE–0x49549: Je Platz k = 0 … 9 wird ein Markt-Platz entfernt, die folgenden rücken auf und k zählt
    /// weiter (jeder zweite bleibt); Plätze eines Managers bleiben. Mit random(a, b) = a: der erste Wurf trifft Platz 0
    /// (räumen, Pool 180 setzen), der zweite findet Pool 180 besetzt und bricht nach 1500 Versuchen ab (0x4962A).
    /// </summary>
    [Fact]
    public void ChangeList_PhaseOne_RemovesEveryOtherMarketSlot_KeepsManagerSlots()
    {
        // Spur kader-transfer, Teil 5: Die Umverteilung beim Spielstart (0x057E3) verschiebt die erste Liste; Startwert mit
        // freiem Pool 180 suchen (vorher fest 4).
        var game = Enumerable.Range(4, 40).Select(seed => TestGames.Create(seed))
            .First(g => g.TransferMarket.Pool[179].Owner == PoolOwner.Free);
        var manager = game.ActiveManager;
        var market = game.TransferMarket;
        var own = game.Clubs[manager.ClubIndex].Players.First(p => p.Role == PlayerRole.Forward);
        Assert.Equal(TransferResult.Accepted, market.Offer(game, manager, own));
        var before = market.Listings.ToList();

        var model = before.ToList();
        for (var k = 0; k < TransferPoolRules.MarketSlots; k++)
            if (k < model.Count && model[k].Seller is null)
                model.RemoveAt(k);
        var removed = before.Except(model).ToList();
        Assert.NotEmpty(removed);
        Assert.Equal(PoolOwner.Free, market.Pool[179].Owner); // Vorbedingung: Pool 180 frei

        market.ChangeList(game, new Lowest());

        // Phase 2 mit cx = 0: Markt-Platz 0 räumen und Pool 180 anhängen; beim nächsten Markt-Platz 0 ist Pool 180 besetzt →
        // Abbruch nach 1500 Versuchen; ein Managerplatz an Stelle 0 lässt den Wurf ohne Folge.
        var expected = model.ToList();
        var added = false;
        for (var j = 0; j < TransferPoolRules.MarketSlots - model.Count(l => l.Seller is not null); j++)
        {
            if (expected.Count == 0 || expected[0].Seller is not null)
                continue;
            removed.Add(expected[0]);
            expected.RemoveAt(0);
            if (added)
                break;
            added = true;
        }
        var after = market.Listings.ToList();
        Assert.Equal(expected, after.Take(expected.Count));
        if (added)
        {
            var placed = Assert.Single(after.Skip(expected.Count));
            Assert.Same(market.Pool[179].Player, placed.Player);
            Assert.Equal(PoolOwner.Market, market.Pool[179].Owner);
        }
        Assert.Contains(after, l => l.Player == own);
        Assert.All(removed, l => Assert.Equal(PoolOwner.Free, market.Pool.Single(s => s.Player == l.Player).Owner));
    }

    /// <summary>
    /// [ORIGINAL] 0x4981E: Grundstärken aus Vereinsfeld 0x19 bzw. 0x1B + random(10 · t, 14) − 10, Technik mit random(0, 4) = 0
    /// + random(12, 20), begrenzt auf 10 … 99; t = 1 nur bei Nationalteams. Mit random(a, b) = a: Verein −10 bzw. +2 (Technik
    /// mit Zuschlag 12), Nationalteam ±0.
    /// </summary>
    [Fact]
    public void MarketStrengths_FromClubFields()
    {
        var game = TestGames.Create();
        var club = game.Clubs[5];
        var nation = game.Clubs[NationalTeams.First];
        var p = new Player { Name = "X", Role = PlayerRole.Forward };
        TransferPoolRules.RollMarketStrengths(p, club, new Lowest());
        var values = MatchEngine.TeamValuesOf(club);
        Assert.Equal(Math.Clamp(club.Strength + values.AttackCondition - 10, 10, 99), p.Condition);
        Assert.Equal(Math.Clamp(club.Strength + values.AttackTechnique - 10 + 12, 10, 99), p.Technique);
        TransferPoolRules.RollMarketStrengths(p, nation, new Lowest());
        // Spur kader-transfer, Teil 4 (A209): Nationalteams aus der Ländertabelle DGROUP 0x4C4E (Sturm), nicht der Datenbankstärke.
        var (_, _, ca, ta) = NationalTeamRatings.Of(nation.Index);
        Assert.Equal(Math.Clamp(ca, 10, 99), p.Condition);
        Assert.Equal(Math.Clamp(ta + 12, 10, 99), p.Technique);
        Assert.Equal(180, TransferPoolRules.DrawIndex(new Lowest())); // random(0, 5) = 0 → random(180, 199)
    }

    /// <summary>
    /// Saisonwechsel des Pools: freie Plätze altern um ein Jahr (0x274A7) und bekommen neue Namen (0x5ACA0); ein gekaufter
    /// Spieler im Managerkader behält seinen Platz. Sichern und Laden bleibt bytegleich, die IDs stimmen.
    /// </summary>
    [Fact]
    public void SeasonChange_AgesAndRenamesFreeSlots_KeepsBoughtPlayer()
    {
        var game = TestGames.Create(6);
        var manager = game.ActiveManager;
        manager.Money = 10_000_000;
        var listing = game.TransferMarket.Listings.First(l => l.FromPool);
        Assert.Equal(TransferResult.Accepted, game.TransferMarket.Buy(game, manager, listing, listing.Price * 2, loan: false));
        var bought = game.TransferMarket.Pool.Single(s => s.Player == listing.Player);
        Assert.Equal(PoolOwner.Taken, bought.Owner);
        bought.Player.ContractYears = 3;
        LifecycleRegressionTests.FinishSeason(game);
        var ages = game.TransferMarket.Pool.ToDictionary(s => s.Index, s => (s.Player, s.Player.Age, s.Owner));

        game.StartNextSeason();

        foreach (var slot in game.TransferMarket.Pool.Where(s => s != bought))
            Assert.Equal(ages[slot.Index].Age + 1, slot.Player.Age);
        Assert.Same(listing.Player, bought.Player);
        Assert.Equal(PoolOwner.Taken, bought.Owner);
        Assert.Contains(listing.Player, game.Clubs[manager.ClubIndex].Players);
        Assert.Empty(game.ValidatePlayerIdentity());
        var saved = SaveGame.Save(game);
        var loaded = SaveGame.Load(saved);
        Assert.True(loaded.Success, loaded.Error);
        Assert.Equal(saved, SaveGame.Save(loaded.Game!));
        Assert.Equal(game.TransferMarket.Pool.Select(s => (s.Index, s.Player.Id, s.Player.Name, s.Club, s.Owner)),
            loaded.Game!.TransferMarket.Pool.Select(s => (s.Index, s.Player.Id, s.Player.Name, s.Club, s.Owner)));
    }

    /// <summary>
    /// Poolgröße flexibel (Wunsch Nutzer 01.11.2026): 200 ist das Original; andere Größen teilen im Verhältnis des Originals
    /// auf. Ein Pool mit 400 Plätzen lässt sich bauen, ziehen und sichern.
    /// </summary>
    [Fact]
    public void Layout_IsFlexible_OriginalAnd400()
    {
        var original = TransferPoolLayout.For(200);
        Assert.Same(TransferPoolLayout.Original, original);
        Assert.Equal((179, 180, 199), (original.LeagueLast, original.FirstNational, original.Last));
        Assert.Equal((21, 97), original.LeagueRoleStarts);
        Assert.Equal((179, 184, 191), original.NationalNaming);
        var big = TransferPoolLayout.For(400);
        Assert.Equal(399, big.LeagueLast + big.NationalGoalkeepers + big.NationalDefenders + big.NationalForwards);
        Assert.Equal((40, 152, 167, 8, 14, 18), (big.LeagueGoalkeepers, big.LeagueDefenders, big.LeagueForwards,
            big.NationalGoalkeepers, big.NationalDefenders, big.NationalForwards));

        var game = TestGames.Create(9);
        game.TransferMarket.ReservePool(game, 9, big);
        Assert.Equal(399, game.TransferMarket.Pool.Count);
        Assert.Equal(big, game.TransferMarket.PoolLayout);
        Assert.All(game.TransferMarket.Pool, s => Assert.Equal(big.RoleOf(s.Index), s.Player.Role));
        foreach (var slot in game.TransferMarket.Pool.Where(s => s.Player.Id == 0))
            Assert.Fail("Poolspieler ohne ID");
        for (var k = 0; k < 20; k++)
            game.TransferMarket.ChangeList(game, game.Random);
        Assert.Contains(game.TransferMarket.Listings, l => game.TransferMarket.Pool.Single(s => s.Player == l.Player).Index > 199);
        Assert.Empty(game.ValidatePlayerIdentity());
        var saved = SaveGame.Save(game);
        var loaded = SaveGame.Load(saved);
        Assert.True(loaded.Success, loaded.Error);
        Assert.Equal(big, loaded.Game!.TransferMarket.PoolLayout);
    }

    /// <summary>
    /// Originaldaten, vier Saisonwechsel: Kein Poolspieler teilt seinen Namen mit einem Spieler in Kader, bei den Vertragslosen
    /// oder im Ruhestand (Vorschlag 1, Entscheidung Nutzer 01.11.2026); jeder Spieler hat eine eigene ID.
    /// </summary>
    [GameDataFact]
    public void OriginalDatabase_FourChanges_NamesUniqueAndIdsDistinct()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Joerg", ClubIndex = 2 });
        var game = NewGame.Create(ClubDatabase.Load(TestGameData.Load()), setup, 5);
        var startIds = game.NextPlayerId;
        // #357 (R9T-P3-03): neu benannte Plätze (0x5ACA0 ohne Tausch: neuer Spieler mit Alter, Talent und Rolle des alten,
        // TransferMarket.Rename). Ausgenommen sind Spieler, die beim Tausch aus einem Computerkader kamen – darunter Talente, die
        // PlayerFactory.TalentName mit dem Namen eines vorhandenen Spielers anlegt (bekannte Lücke, nicht Teil dieser Probe).
        var renamed = new HashSet<Player>(ReferenceEqualityComparer.Instance);
        for (var change = 0; change < 4; change++)
        {
            LifecycleRegressionTests.FinishSeason(game);
            var before = game.TransferMarket.Pool.Select(s => s.Player).ToList();
            var previous = game.TransferMarket.Pool.ToDictionary(s => s.Index, s => s.Player);
            var outsideBefore = game.AllPlayers.Except(before).ToHashSet(ReferenceEqualityComparer.Instance);
            game.StartNextSeason();
            foreach (var slot in game.TransferMarket.Pool)
            {
                var old = previous[slot.Index];
                if (!ReferenceEquals(old, slot.Player) && !outsideBefore.Contains(slot.Player) && !game.AllPlayers.Contains(old)
                    && (old.Age, old.Talent, old.Role) == (slot.Player.Age, slot.Player.Talent, slot.Player.Role))
                    renamed.Add(slot.Player);
            }
            // Neue Namen (0x5ACA0): Tausch mit Spielern der Computerkader, die Kadergröße bleibt.
            Assert.True(game.TransferMarket.Pool.Count(s => !before.Contains(s.Player)) > 100, "kaum neue Namen");
            // Gleiche Namen gibt es nur durch Talente des Remakes (PlayerFactory.TalentName übernimmt bei einteiligen Namen den
            // Namen eines vorhandenen Spielers – unverändert, nicht Teil dieser Spur); verglichen werden deshalb die Spieler vom
            // Spielstart (Datenbank und Pool).
            var outside = game.Clubs.SelectMany(c => c.Players).Concat(game.TransferMarket.FreeAgents).Concat(game.RetiredPlayers)
                .Where(p => p.Id < startIds).Select(p => p.Name).ToHashSet();
            Assert.DoesNotContain(game.TransferMarket.PoolPlayers, p => p.Id < startIds && outside.Contains(p.Name));
            // R9T-P2-01 (#346): auch Poolspieler mit neuer ID prüfen (umbenannte Plätze). Ohne Tausch Pool ↔ Computerkader
            // wären es über 100 Treffer je Saison. #357: Statt einer Schwelle (gemessen 0 / 0 / 1 / 3 bzw. 0 / 4 / 1 / 3, je nach
            // Zufallsverlauf) bleiben die aus Computerkadern getauschten Spieler außen vor; ein neu benannter Platz trifft nie.
            var renamedClashes = game.TransferMarket.PoolPlayers
                .Where(p => p.Id >= startIds && renamed.Contains(p) && outside.Contains(p.Name)).Select(p => p.Name).ToList();
            Assert.True(renamedClashes.Count == 0,
                $"Saison {change + 1}: neu benannte Poolspieler mit dem Namen eines Kaderspielers: {string.Join(", ", renamedClashes)}");
            Assert.True(game.TransferMarket.Pool.Count == game.TransferMarket.Pool.Select(s => s.Player.Name).Distinct().Count(),
                string.Join("; ", game.TransferMarket.Pool.GroupBy(s => s.Player.Name).Where(g => g.Count() > 1).Select(g => string.Join(",", g.Select(s => $"{s.Index}:{s.Owner}:{s.Club}:{g.Key}")))));
            var ids = game.AllPlayers.Select(p => p.Id).ToList();
            Assert.Equal(ids.Count, ids.Distinct().Count());
            Assert.Empty(game.ValidatePlayerIdentity());
        }
    }

    /// <summary>
    /// R9T-P1-01 (#345): Ein am Code von Schema 19 erzeugter Spielstand (Integration Runde 9 vor transfer-vertraege-komplett,
    /// <c>integration/runde-9-optionen-wirkung</c> @ b1a8657; <c>TestGames.Create(8)</c>, zehn Spieltage, synthetische
    /// Datenbank) lädt über die Migrationen 19 → 22, spielt einen Saisonwechsel weiter und sichert bytegleich. Vorher fehlte
    /// <c>_databaseNames</c> in der Migration 21 → 22 („Feld _databaseNames in TransferMarket fehlt“).
    /// </summary>
    [Fact]
    public void Migration_RealSchema19Save_LoadsAndPlaysOn()
    {
        using var stream = typeof(TransferVertraegeKomplettTeil5Tests).Assembly
            .GetManifestResourceStream("Fixtures.schema19-synthetisch.ems.gz")!;
        using var unzip = new System.IO.Compression.GZipStream(stream, System.IO.Compression.CompressionMode.Decompress);
        using var bytes = new MemoryStream();
        unzip.CopyTo(bytes);
        Assert.Equal(19, JsonNode.Parse(bytes.ToArray())!["schema"]!.GetValue<int>());

        var loaded = SaveGame.Load(bytes.ToArray());
        Assert.True(loaded.Success, loaded.Error);
        var game = loaded.Game!;
        Assert.Empty(game.ValidatePlayerIdentity());
        for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
        {
            Assert.True(guard < 1000, "Saisonende nicht erreicht");
            foreach (var m in game.Managers)
                m.Money = Math.Max(m.Money, 10_000_000);
            game.Continue();
        }
        game.StartNextSeason();
        Assert.NotEmpty(game.TransferMarket.Pool);
        Assert.Equal(game.TransferMarket.Pool.Count, game.TransferMarket.Pool.Select(s => s.Player.Name).Distinct().Count());
        Assert.Empty(game.ValidatePlayerIdentity());
        var saved = SaveGame.Save(game);
        var again = SaveGame.Load(saved);
        Assert.True(again.Success, again.Error);
        Assert.Equal(saved, SaveGame.Save(again.Game!));
    }

    /// <summary>Migration 21 → 22: Ein Stand ohne Pool lädt; der nächste Listenwechsel baut den Pool mit neuen IDs.</summary>
    [Fact]
    public void Migration_FromSchema21_BuildsPoolOnFirstChange()
    {
        var game = TestGames.Create(8);
        var root = JsonNode.Parse(SaveGame.Save(game))!.AsObject();
        root["schema"] = 21;
        var market = Find(root, o => o.ContainsKey("_listings") && o.ContainsKey("_pool"));
        market.Remove("_pool");
        market.Remove("_databaseNames");
        if (market["_listings"] is JsonObject listObject && listObject["items"] is JsonArray items)
            items.Clear();
        else
            market["_listings"] = new JsonArray();

        var loaded = SaveGame.Load(System.Text.Encoding.UTF8.GetBytes(root.ToJsonString()));
        Assert.True(loaded.Success, loaded.Error);
        var old = loaded.Game!;
        Assert.Empty(old.TransferMarket.Pool);
        Assert.Empty(old.TransferMarket.Listings);
        var next = old.NextPlayerId;
        old.TransferMarket.ChangeList(old, old.Random);
        Assert.Equal(199, old.TransferMarket.Pool.Count);
        Assert.All(old.TransferMarket.Pool, s => Assert.True(s.Player.Id >= next));
        Assert.NotEmpty(old.TransferMarket.Listings);
        Assert.Empty(old.ValidatePlayerIdentity());
        var saved = SaveGame.Save(old);
        Assert.True(SaveGame.Load(saved).Success);

        static JsonObject Find(JsonNode node, Func<JsonObject, bool> match)
        {
            if (node is JsonObject obj)
            {
                if (match(obj))
                    return obj;
                foreach (var (_, child) in obj)
                    if (child is not null && TryFind(child, match) is { } hit)
                        return hit;
            }
            if (node is JsonArray array)
                foreach (var child in array)
                    if (child is not null && TryFind(child, match) is { } hit)
                        return hit;
            throw new InvalidOperationException("Markt nicht gefunden.");
        }

        static JsonObject? TryFind(JsonNode node, Func<JsonObject, bool> match)
        {
            try { return Find(node, match); }
            catch (InvalidOperationException) { return null; }
        }
    }

    /// <summary>
    /// Mehrsaisonprobe (Kader, Geld; 4 Saisonwechsel): passiv bleibt der Markt reiner Pool, aktiv kauft der Manager je Saison
    /// einen Poolspieler und nimmt Gebote an (Gutschrift nach 0x49332). Pool immer 199 Plätze, Markt höchstens 10 Plätze plus
    /// gehaltene, Computerkader nie durch Käufe verkleinert, Identitäten gültig, Sichern/Laden bytegleich.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MultiSeason_FourChanges_PoolMarket(bool active)
    {
        var game = TestGames.Create(13);
        var manager = game.ActiveManager;
        var club = game.Clubs[manager.ClubIndex];
        var market = game.TransferMarket;
        int bought = 0, sold = 0;
        for (var change = 0; change < 4; change++)
        {
            var boughtThisSeason = false;
            for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
            {
                Assert.True(guard < 1000, "Saisonende nicht erreicht");
                TestSeason.KeepSquad(game, manager);
                if (manager.Money < 2_000_000)
                    manager.Money = 2_000_000;
                Assert.Equal(199, market.Pool.Count);
                Assert.True(market.Listings.Count(l => l.FromPool) <= TransferPoolRules.MarketSlots);
                Assert.All(market.Listings.Where(l => l.FromPool),
                    l => Assert.DoesNotContain(l.Player, game.Clubs.SelectMany(c => c.Players)));
                if (active && game.Phase == SeasonPhase.Running)
                {
                    foreach (var bid in market.Bids.Where(b => b.Listing.Seller == manager).ToList())
                    {
                        var before = manager.Money;
                        var fee = TransferMarket.TransferFee(bid);
                        if (market.Accept(game, bid) == TransferResult.Accepted)
                        {
                            Assert.Equal(before + fee, manager.Money);
                            sold++;
                        }
                    }
                    if (market.Listings.All(l => l.Seller != manager)
                        && club.Players.Where(p => !p.OnLoan && TransferMarket.CanLeave(club, p)).MinBy(p => p.Strength) is { } offer)
                        market.Offer(game, manager, offer);
                    if (!boughtThisSeason && club.Players.Count < TransferMarket.MaxSquad
                        && market.Listings.FirstOrDefault(l => l.FromPool) is { } listing)
                    {
                        var squads = game.Clubs.Where(c => c != club).Sum(c => c.Players.Count);
                        Assert.Equal(TransferResult.Accepted, market.Buy(game, manager, listing, listing.Price * 135 / 100, false));
                        Assert.Equal(squads, game.Clubs.Where(c => c != club).Sum(c => c.Players.Count));
                        bought++;
                        boughtThisSeason = true;
                    }
                }
                game.Continue();
            }
            game.StartNextSeason();
            Assert.Empty(game.ValidatePlayerIdentity());
            Assert.All(market.Pool.Where(s => s.Owner == PoolOwner.Taken),
                s => Assert.Contains(s.Player, game.Clubs[manager.ClubIndex].Players));
            var saved = SaveGame.Save(game);
            var loaded = SaveGame.Load(saved);
            Assert.True(loaded.Success, loaded.Error);
            Assert.Equal(saved, SaveGame.Save(loaded.Game!));
        }
        if (active)
        {
            Assert.True(bought >= 4, "kein Kauf je Saison");
            Assert.True(sold > 0, "kein Verkauf");
        }
        else
        {
            Assert.Equal(0, bought + sold);
            Assert.DoesNotContain(market.Pool, s => s.Owner == PoolOwner.Taken);
        }
    }
}
