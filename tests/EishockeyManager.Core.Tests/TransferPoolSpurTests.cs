using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Runde 22, Spur transfer-pool: A391 (Poolplatz eines nach Vertragsende verlorenen Spielers), #897 Poolplatz 179, A713, A731,
/// #897 <c>ChangeList</c>, A864. Fundstellen je Test.
/// </summary>
public class TransferPoolSpurTests
{
    private static GameState Create(int seed = 1)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "M5", ClubIndex = 5, StartLeague = League.Bundesliga });
        return NewGame.Create(TestGames.CreateDatabase(), setup, seed);
    }

    private static void PlayToSeasonOver(GameState game)
    {
        for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
        {
            Assert.True(guard < 20_000, "Saisonende nicht erreicht");
            if (game.NationalTurn && game.NationalCoach!.Squad.Count == 0)
                game.AutoNominate();
            game.Continue();
        }
    }

    /// <summary>
    /// A391: Der Platz eines nach Vertragsende verlorenen Spielers altert in jedem späteren Saisonwechsel weiter.
    /// [ORIGINAL] Datei 0x274A7 (<c>inc</c> Pool +0x14) liegt in der Schleife über alle 200 Poolplätze (0x2744B–0x27232 mit
    /// Zähler [bp−2] bis 0xC8) und überspringt einen Platz nur bei DGROUP 0xA70E ≠ 0 (Datei 0x2748E–0x27494); Kaderplatz und
    /// Besitzer spielen dort keine Rolle. Beleg FILE44 → FILE46: KJELLBERG (Pool 196, +0x1A = 0) Alter 32 → 33 im Saisonwechsel
    /// des Vertragsendes. Im Saisonwechsel des Vertragsendes altert ihn der Kaderlauf, danach nur noch der Poolplatz.
    /// Rot vorher: Alter blieb nach dem Vertragsende stehen.
    /// </summary>
    [Fact]
    public void A391_LostPlayer_KeepsAgingEverySeason()
    {
        var game = Create();
        var club = game.Clubs[game.Managers[0].ClubIndex];
        foreach (var p in club.Players)
            (p.ContractYears, p.Age) = (3, 25);
        var lost = club.Players[3];
        var index = game.TransferMarket.Pool.First(s => ReferenceEquals(s.Player, lost)).Index;
        lost.ContractYears = 1;

        PlayToSeasonOver(game);
        game.StartNextSeason();
        var slot = game.TransferMarket.Pool[index - 1];
        Assert.Same(lost, slot.Player);
        Assert.Equal(PoolOwner.Taken, slot.Owner);
        Assert.Equal(26, lost.Age); // einmal im Saisonwechsel des Vertragsendes (Kaderlauf)

        PlayToSeasonOver(game);
        game.StartNextSeason();
        Assert.Same(lost, game.TransferMarket.Pool[index - 1].Player);
        Assert.Equal(27, lost.Age); // danach nur der Poolplatz (A391)

        PlayToSeasonOver(game);
        game.StartNextSeason();
        Assert.Equal(28, lost.Age);
    }

    /// <summary>
    /// #897 Poolplatz 179: kein Fehler, das Original trifft ihn auch. [ORIGINAL] Die Soll-Rechnung 0x05521 (aus 0x057E3 und 0x07C7E)
    /// zieht <c>random(1, 0xB3)</c> (Datei 0x0512B–0x05133: <c>push 0xB3; push 1</c>, Aufruf 0x99F:0x847) – 1 … 179 einschließlich –
    /// und gibt einem gezogenen Inlandsspieler über 0x059B8 einen neuen Verein; nur die Schleife von 0x057E3 selbst läuft bis 0xB2
    /// (Datei 0x057E3 + 0xEC: <c>cmp [bp−1], 0xB3; jnc</c>). Gegenproben: FILE05 Platz 179 beim Nationalteam 57, FILE41/FILE46 Platz
    /// 179 bei Ligavereinen 2/6 (<c>docs/transfer.md</c>, „Soll je Liga“). Das Remake bildet beides ab; Plätze ab 180 bleiben immer.
    /// </summary>
    [Fact]
    public void PoolSlot179_IsReachedByQuotaDraw_Not180Up()
    {
        var hit179 = false;
        for (var seed = 1; seed <= 300 && !hit179; seed++)
        {
            var game = Create(seed);
            var pool = game.TransferMarket.Pool;
            var before = pool.Select(s => s.Club).ToArray();
            game.TransferMarket.PoolQuotas(game, new Random(seed));
            hit179 |= pool[178].Club != before[178];
            for (var i = 179; i < pool.Count; i++)
                Assert.Equal(before[i], pool[i].Club); // Plätze 180 … 199
        }
        Assert.True(hit179, "Platz 179 wird von der Soll-Rechnung getroffen (random(1, 0xB3))");
    }

    /// <summary>
    /// A713 (Review P2-1): Die vierte Gruppe der Verteilung am Saisonwechsel hält immer sieben Vereine ohne Liga, auch nach einem
    /// Austausch der Oberliga. [ORIGINAL] Datei 0x07C7E, DGROUP 0x7C6/0x7C7 = Satzpositionen 36 … 42 nach der Umordnung 0x35EDD
    /// (0 … 11, 12 … 23, 24 … 35, 36 … 42; <c>docs/transfer.md</c> „Saisonwechsel“); Positionen 43 … 47 und Nationalteams gehören
    /// zu keiner Gruppe (−1). [APPROX] Das Remake wählt die sieben Vereine ohne Liga mit dem niedrigsten Index (A715).
    /// Rot vorher: nach dem Tausch (Verein 20 ohne Liga, Verein 38 in der Liga) hatte Gruppe 3 nur noch sechs Vereine, Verein 20
    /// lag in keiner Gruppe.
    /// </summary>
    [Fact]
    public void A713_FourthGroup_IsSevenClubsWithoutLeague_AlsoAfterExchange()
    {
        var game = Create();
        AssertFourthGroup(game, expectMovedGroup: false);
        // Austausch wie beim Saisonwechsel: ein Oberligist steigt ab (Index < 36), ein Verein ohne Liga (Index 36 … 42) steigt auf.
        var down = game.Clubs.First(c => c.League == League.Oberliga && c.Index < NewGame.SelectableClubs && c.Index != 5);
        var up = game.Clubs.First(c => c.League is null && c.Index is >= 36 and <= 42);
        (down.League, up.League) = (null, League.Oberliga);
        AssertFourthGroup(game, expectMovedGroup: true);
        Assert.Equal(3, TransferMarket.PoolGroup(game, down.Index));
        Assert.NotEqual(3, TransferMarket.PoolGroup(game, up.Index));
    }

    private static void AssertFourthGroup(GameState game, bool expectMovedGroup)
    {
        var withoutLeague = game.Clubs.Where(c => c.League is null && c.Index < PoolDistributionRules.NoLeagueClubs).ToList();
        Assert.True(withoutLeague.Count > PoolDistributionRules.NoLeagueCount, "Es gibt Vereine ohne Liga ohne Gruppe");
        var grouped = withoutLeague.Where(c => TransferMarket.PoolGroup(game, c.Index) == 3).Select(c => c.Index).ToList();
        Assert.Equal(withoutLeague.Take(PoolDistributionRules.NoLeagueCount).Select(c => c.Index), grouped);
        Assert.All(withoutLeague.Skip(PoolDistributionRules.NoLeagueCount), c => Assert.Equal(-1, TransferMarket.PoolGroup(game, c.Index)));
        foreach (var club in game.Clubs.Where(c => c.League is { } && c.Index < PoolDistributionRules.NoLeagueClubs))
            Assert.Equal((int)club.League!.Value, TransferMarket.PoolGroup(game, club.Index));
        Assert.Equal(-1, TransferMarket.PoolGroup(game, NationalTeams.First));
        Assert.Equal(expectMovedGroup, grouped.Any(i => i < 36));
    }

    /// <summary>
    /// #897 <c>ChangeList</c>: Der Listenwechsel schreibt in den neuen Markt-Platz +0x28 = Wert(Gehalt), nicht 0. [ORIGINAL] Datei
    /// 0x497A1 (<c>call 0x118:0x70</c> mit Art 1 auf den Kaderplatz [0xA70E]/di, Ergebnis nach +0x28/+0x2A, 0x497BE/0x497C3), danach
    /// ohne Zufall; Gegenprobe an den Originalständen (<c>HokImportWerteTests</c>: an allen Ständen +0x28 jedes Poolangebots ≠ 0,
    /// Wert(Gehalt) mit dem Verein des Platzes und dem Level des ziehenden Managers). Der Vertrag bleibt 0 (0x4970A).
    /// Rot vorher: Gehalt 0.
    /// </summary>
    [Fact]
    public void ChangeList_NewMarketSlot_GetsSalaryValue()
    {
        var game = Create(3);
        var level = game.ActiveManager.Level;
        for (var round = 0; round < 6; round++)
            game.TransferMarket.ChangeList(game, game.Random);
        var listings = game.TransferMarket.Listings.Where(l => l.FromPool).ToList();
        Assert.NotEmpty(listings);
        foreach (var l in listings)
        {
            var expected = Math.Max(Contracts.MinSalary,
                game.PlayerValueOf(l.Player, l.From, PlayerValueKind.Salary, new Random(1), level) / 3);
            Assert.Equal(expected, l.Player.MonthlySalary);
            Assert.True(l.Player.MonthlySalary > 0);
            Assert.Equal(0, l.Player.ContractYears);
        }
    }

    /// <summary>
    /// A864: Der Tageslauf 0x07169 würfelt je Kaderplatz eines Managers eine Verletzung außerhalb des Spiels. [ORIGINAL] Datei
    /// 0x07462–0x07548: Platz mit +0x0D = 0 (nicht verletzt) und +0x09 = 0 (kein Angebot), x = max(0x28, 0x140 − 2 · Belastung +
    /// 0x32 an Tagen ohne Ereignis), Wurf random(0, 30 · (5 − Level) + x + 100) = 0 (0x7479–0x7495) → Verletzung 0x05B1A mit
    /// Schalter 0 (alle 28 Arten, 0x74A9), Meldung „Verletzung außerhalb des Spieles:“ / Name + Art (DGROUP 0xD30, 0x490F), bei
    /// Versicherung 0x07089 (0x752D–0x7542), vor dem Ruhestandswurf. Rot vorher: keine Verletzung, kein Wurf im Strom.
    /// </summary>
    [Fact]
    public void A864_InjuryOutsideMatch_RollsPerSlot()
    {
        var game = Create(2);
        var manager = game.Managers[0];
        var club = game.Clubs[manager.ClubIndex];
        foreach (var p in club.Players)
        {
            p.InsurancePremium = 100;
            p.Strain = 100;
        }
        var kinds = new HashSet<int>();
        var injuries = 0;
        var day = 10; // Tag ohne Ereignis liegt in der Saison; der Wurf hängt nur an x
        var money = manager.Money;
        var lines = 0;
        for (var run = 0; run < 3000; run++)
        {
            game.AnnounceContractEnds(day, manager);
            lines += game.Inbox(manager).Count(m => m.Lines.Length > 0 && m.Lines[0] == "Verletzung außerhalb des Spieles:");
            game.Finance(manager).Inbox.Clear();
            foreach (var p in club.Players)
            {
                if (p.InjuredGames == 0)
                    continue;
                injuries++;
                kinds.Add(p.InjuryKind);
                Assert.InRange(p.InjuryKind, 0, 27);
                p.InjuredGames = 0; // geheilt, damit der nächste Lauf wieder würfelt
                p.InjuryKind = InjuryKinds.Unknown;
            }
        }
        Assert.InRange(injuries, 100, 700); // 25 Plätze · 3000 Läufe / ~300
        Assert.True(kinds.Count > 5, "mehrere Arten");
        Assert.Equal(injuries, lines);
        Assert.True(manager.Money > money - 10_000_000); // Zahlungen der Versicherung gebucht (Einnahme)
    }

    /// <summary>
    /// A864: Verletzte Plätze und Plätze mit einem unaufgeforderten Angebot (+0x09 ≠ 0) würfeln nicht (Datei 0x7462–0x7476).
    /// </summary>
    [Fact]
    public void A864_InjuredOrOfferedSlots_DoNotRoll()
    {
        var game = Create(2);
        var manager = game.Managers[0];
        var club = game.Clubs[manager.ClubIndex];
        var other = game.Clubs.First(c => c.Index != club.Index && c.League is not null);
        foreach (var (p, i) in club.Players.Select((p, i) => (p, i)))
        {
            p.Strain = 100;
            if (i % 2 == 0)
                (p.InjuredGames, p.InjuryKind) = (1_000, 5);
            else
                p.UnsolicitedOffer = other;
        }
        for (var run = 0; run < 3000; run++)
        {
            // Der Verfall des Angebots (Datei 0x0762D) folgt im selben Lauf auf den Verletzungswurf: neu setzen.
            foreach (var p in club.Players.Where((p, i) => i % 2 == 1))
                p.UnsolicitedOffer = other;
            game.AnnounceContractEnds(10, manager);
        }
        Assert.All(club.Players.Where((p, i) => i % 2 == 0), p => Assert.Equal((1_000, 5), (p.InjuredGames, p.InjuryKind)));
        Assert.All(club.Players.Where((p, i) => i % 2 == 1), p => Assert.Equal(0, p.InjuredGames));
    }

    /// <summary>
    /// Mehrsaisonprobe (AGENTS.md §5, vier Saisonwechsel, passiver und aktiver Manager): Verlorene Pool-Spieler (Besitzer „vergeben“,
    /// in keinem Verein) altern in jedem Saisonwechsel genau um ein Jahr (A391, Datei 0x274A7), die Verletzung außerhalb des Spiels
    /// (A864) tritt im Tageslauf auf, der Pool behält über alle Wechsel seine Plätze ohne doppelte Spieler.
    /// Rückschaltprobe: src/ von ce658d4 → rot (Alter bleibt stehen, keine Meldung „Verletzung außerhalb des Spieles:“).
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MultiSeason_FourChanges_LostPlayersAgeAndInjuriesOutsideMatchOccur(bool active)
    {
        var game = TestGames.Create(3);
        var before = new Dictionary<Player, int>();
        var poolSize = game.TransferMarket.Pool.Count;
        var season = game.Season;
        var changes = 0;
        var compared = 0;
        var outside = 0;
        RngTwinProbeTests.Run(game, active,
            afterStep: g =>
            {
                foreach (var m in g.Managers)
                {
                    outside += g.Inbox(m).Count(x => x.Lines.Length > 0 && x.Lines[0] == "Verletzung außerhalb des Spieles:");
                    g.Finance(m).Inbox.Clear();
                }
                if (g.Season == season)
                    return;
                season = g.Season;
                var pool = g.TransferMarket.Pool;
                Assert.Equal(poolSize, pool.Count);
                // A713 (Review P2-1): Gruppe 3 hat nach jedem Austausch sieben Vereine, kein Poolplatz ≤ 179 liegt bei einem Verein ohne
                // Liga und ohne Gruppe.
                Assert.Equal(PoolDistributionRules.NoLeagueCount,
                    g.Clubs.Count(c => c.Index < PoolDistributionRules.NoLeagueClubs && TransferMarket.PoolGroup(g, c.Index) == 3));
                Assert.All(pool.Take(179).Where(x => x.Club < PoolDistributionRules.NoLeagueClubs && g.Clubs[x.Club].League is null),
                    x => Assert.Equal(3, TransferMarket.PoolGroup(g, x.Club)));
                Assert.Equal(pool.Count, pool.Select(x => x.Player).Distinct().Count());
                foreach (var slot in pool.Where(x => x.Owner == PoolOwner.Taken && before.ContainsKey(x.Player)
                                                     && !g.Clubs.Any(c => c.Players.Contains(x.Player))))
                {
                    Assert.Equal(before[slot.Player] + 1, slot.Player.Age);
                    compared++;
                }
            },
            beforeChange: g =>
            {
                changes++;
                before.Clear();
                foreach (var slot in g.TransferMarket.Pool.Where(x => x.Owner == PoolOwner.Taken && !g.Clubs.Any(c => c.Players.Contains(x.Player))))
                    before[slot.Player] = slot.Player.Age;
            });
        Assert.Equal(4, changes);
        Assert.True(compared > 0, "Kein verlorener Pool-Spieler verglichen");
        Assert.True(outside > 0, "Keine Verletzung außerhalb des Spieles in vier Saisons");
    }
    private static (GameState Game, TransferBid Bid) WithBid(int seed)
    {
        var game = TestGames.Create(seed);
        var manager = game.ActiveManager;
        var club = game.Clubs[manager.ClubIndex];
        var player = club.Players.OrderByDescending(p => p.Condition + p.Technique).First(p => TransferMarket.CanLeave(club, p));
        Assert.Equal(TransferResult.Accepted, game.TransferMarket.Offer(game, manager, player));
        for (var day = 0; day < 300 && game.TransferMarket.Bids.Count == 0; day++)
            game.TransferMarket.AdvanceDay(game, game.Random);
        return (game, Assert.Single(game.TransferMarket.Bids));
    }

    /// <summary>
    /// A731: Die Höhe des Angebots für einen eigenen Marktspieler fällt erst beim Anklicken. [ORIGINAL] Datei 0x47347 (call
    /// 0x49D36, Art 0) und 0x47353–0x47379 liegen im Klickzweig der Transferliste (Sprünge 0x4718E/0x471AB → 0x47314, Test
    /// Bit 0x80 bei 0x47334); der Tageswurf 0x072DD setzt nur das Bit. Das Bit fällt nach der Antwort (0x4740B): ein Wurf je
    /// Angebot. Vorher rot: Höhe stand schon nach dem Tageswurf fest.
    /// </summary>
    [Fact]
    public void A731_BidAmount_IsRolledOnClick_NotWhenOfferArises()
    {
        var (game, bid) = WithBid(3);
        Assert.False(bid.IsRolled);
        Assert.Equal(0, bid.Amount);

        game.TransferMarket.RollBid(game, bid);
        Assert.True(bid.IsRolled);
        Assert.True(bid.Amount > 0);
        Assert.Equal(0, bid.Amount % 100);
    }

    /// <summary>A731: Ein zweiter Aufruf zieht keinen Zufall und ändert die Höhe nicht (ein Wurf je Angebot).</summary>
    [Fact]
    public void A731_RollBid_IsOncePerOffer_AndConsumesRandomOnlyOnce()
    {
        var (once, bidOnce) = WithBid(3);
        var (twice, bidTwice) = WithBid(3);
        once.TransferMarket.RollBid(once, bidOnce);
        twice.TransferMarket.RollBid(twice, bidTwice);
        var amount = bidTwice.Amount;
        twice.TransferMarket.RollBid(twice, bidTwice);
        Assert.Equal(amount, bidTwice.Amount);
        Assert.Equal(bidOnce.Amount, bidTwice.Amount);
        Assert.Equal(once.Random.Next(), twice.Random.Next());
    }

    /// <summary>A731: Das Entstehen des Angebots verbraucht für die Höhe keinen Zufall (Strom wie ohne Wurf bis zum Anklicken).</summary>
    [Fact]
    public void A731_OfferArising_DoesNotDrawAmount_StreamContinuesAfterClick()
    {
        var (a, bidA) = WithBid(5);
        var (b, bidB) = WithBid(5);
        Assert.Equal(a.Random.Next(), b.Random.Next()); // gleicher Stand ohne Anklicken
        a.TransferMarket.RollBid(a, bidA);
        Assert.NotEqual(0, bidA.Amount);
        Assert.False(bidB.IsRolled);
    }
}
