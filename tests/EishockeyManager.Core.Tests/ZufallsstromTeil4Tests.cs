using System.Text;
using System.Text.Json.Nodes;
using EishockeyManager.Core;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur zufallsstrom, Teil 4 (N3 = B „alle Stellen“): Die übrigen eigenen Zufallsquellen entfallen, die Stellen ziehen aus dem
/// gemeinsamen Strom <see cref="GameState.Random"/> an der Stelle des Originals. Jede Probe rechnet die Würfe mit einer Kopie des
/// Stroms nach (Fundstellen in em.exe). Vor Teil 4 rot: gezogen wurde aus <c>CupRandom</c> (Saat aus einem Wurf des Stroms nach
/// der Spielvorbereitung, ohne Gegenstück) bzw. <c>ContractRandom</c> und <c>FinanceRandom</c> (Saat aus Saison und Managern).
/// </summary>
public class ZufallsstromTeil4Tests
{
    private sealed record Steps(Dictionary<NewGame.StartStep, RngState> Stream, Dictionary<int, int> StrengthAtEuropeDraw);

    private static (GameState Game, Steps Steps) Create(int seed, Action<GameSetup>? configure, params int[] clubs)
    {
        var setup = new GameSetup();
        foreach (var (club, i) in clubs.Select((c, i) => (c, i)))
            setup.Managers.Add(new ManagerSetup { Name = $"Trainer {i}", Head = i, ClubIndex = club });
        configure?.Invoke(setup);
        var steps = new Steps([], []);
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, seed, (step, g) =>
        {
            steps.Stream[step] = g.Random.Capture();
            if (step == NewGame.StartStep.EuropeanCup)
                foreach (var club in g.Clubs.Where(c => c.Index >= EuropeanCup.FirstOpponent))
                    steps.StrengthAtEuropeDraw[club.Index] = SpectatorRules.Strength(g, club);
        });
        return (game, steps);
    }

    /// <summary>Auslosung 0x341AB(1, 0) nachgerechnet: Datei 0x343C9 (Verein), 0x34401–0x3441C (schwach), 0x3441E–0x344D0 (doppelt), 0x345DD (deutsche Teilnehmer).</summary>
    private static List<int> SimulateEuropeDraw(Random sim, IReadOnlyDictionary<int, int> strength, int clubCount, IReadOnlyList<int> germans)
    {
        var draw = new List<int>();
        for (var slot = 0; slot < EuropeanCup.Teams; slot++)
        {
            while (true)
            {
                var club = sim.Next(EuropeanCup.FirstOpponent, Math.Min(EuropeanCup.LastOpponent, clubCount - 1) + 1);
                if (strength[club] < EuropeanCup.WeakOpponent && sim.Next(0, 4) != 0)
                    continue; // schwacher Gegner: der Wurf fällt vor der Prüfung auf Doppelte
                if (draw.Contains(club))
                    continue;
                draw.Add(club);
                break;
            }
        }
        foreach (var german in germans)
        {
            int slot;
            do slot = sim.Next(0, EuropeanCup.Teams);
            while (draw[slot] < EuropeanCup.FirstOpponent);
            draw[slot] = german;
        }
        return draw;
    }

    /// <summary>Auslosung 0x341AB(0, 0) nachgerechnet: Managerplätze Datei 0x342D7, Vereine Datei 0x343C9 (random(0, DGROUP 0x7CB)).</summary>
    private static List<int> SimulateDebDraw(Random sim, GameState game)
    {
        var slots = new int?[DebCup.Teams];
        var managerClubs = game.ActiveManagers.Select(m => m.ClubIndex).Distinct().ToList();
        foreach (var club in managerClubs)
        {
            int slot;
            do slot = sim.Next(0, DebCup.Teams);
            while (slots[slot] is not null);
            slots[slot] = club;
        }
        var pool = game.Clubs.Where(c => c.League is not null).Select(c => c.Index).ToList();
        for (var slot = 0; slot < DebCup.Teams; slot++)
        {
            if (slots[slot] is not null)
                continue;
            int club;
            do club = pool[sim.Next(pool.Count)];
            while (slots.Contains(club) || managerClubs.Contains(club));
            slots[slot] = club;
        }
        return slots.Select(s => s!.Value).ToList();
    }

    /// <summary>
    /// Spielstart: Der Europapokal wird in der Spielvorbereitung nach dem Poolaufbau und vor der Managerschleife ausgelost (Datei
    /// 0x298B1 → 0x3139F(0) → 0x30427(2) → 0x341AB(1, 0)) – mit den Teamwerten vor der Wanderung (Datei 0x43A73). Vorher: nach der
    /// Spielvorbereitung aus <c>CupRandom</c>; zwischen Pool und Managerschleife zog nichts.
    /// </summary>
    [Fact]
    public void GameStart_EuropeanCup_DrawnAfterPoolBeforeManagers()
    {
        var (game, steps) = Create(3, null, 2, 14);
        var sim = GameRandom.FromState(steps.Stream[NewGame.StartStep.EuropeanCup]);
        var expected = SimulateEuropeDraw(sim, steps.StrengthAtEuropeDraw, game.Clubs.Count, [EuropeanCup.InitialChampion]);

        Assert.Equal(expected, game.EuropeanCup!.Draw);
        Assert.Equal(sim.Capture(), steps.Stream[NewGame.StartStep.Managers]);
    }

    /// <summary>
    /// Spielstart: Die erste Runde des DEB-Pokals wird zwischen dem Verteilen des Pools (Datei 0x43A83 → 0x057E3) und dem ersten
    /// Listenwechsel (Datei 0x43A9F → 0x494EF) ausgelost (Datei 0x43A8C → 0x341AB(0, 0)). Vorher: vor der Wanderung aus
    /// <c>CupRandom</c>.
    /// </summary>
    [Fact]
    public void GameStart_DebCup_DrawnBetweenPoolDistributionAndFirstListing()
    {
        var (game, steps) = Create(3, null, 2, 14);
        var sim = GameRandom.FromState(steps.Stream[NewGame.StartStep.DebCup]);
        var expected = SimulateDebDraw(sim, game);

        Assert.Equal(expected, game.DebCup!.Draw);
        Assert.Equal(sim.Capture(), steps.Stream[NewGame.StartStep.Listing]);
        Assert.NotEqual(steps.Stream[NewGame.StartStep.Market], steps.Stream[NewGame.StartStep.DebCup]); // 0x057E3 zieht davor
        Assert.NotEqual(steps.Stream[NewGame.StartStep.Listing], steps.Stream[NewGame.StartStep.Turn]); // 0x494EF danach
    }

    /// <summary>
    /// Die Auslosungen des Originals prüfen den Spielmodus nicht (0x3139F, 0x30427, 0x341AB; nur die Anzeige 0x33B83 bei Datei
    /// 0x33BAB): Ohne Europapokal und ohne DEB-Pokal zieht der Spielstart dieselben Würfe, die übrige Folge bleibt gleich.
    /// </summary>
    [Fact]
    public void GameStart_DisabledCups_DrawTheSameRolls()
    {
        var (on, a) = Create(3, null, 2, 14);
        var (off, b) = Create(3, s =>
        {
            s.EuropeAndWorldCup = false;
            s.DebCup = false;
        }, 2, 14);

        Assert.Null(off.EuropeanCup);
        Assert.Null(off.DebCup);
        Assert.NotEqual(b.Stream[NewGame.StartStep.EuropeanCup], b.Stream[NewGame.StartStep.Managers]);
        Assert.NotEqual(b.Stream[NewGame.StartStep.DebCup], b.Stream[NewGame.StartStep.Listing]);
        foreach (var step in a.Stream.Keys)
            Assert.Equal(a.Stream[step], b.Stream[step]);
        Assert.Equal(on.Random.Capture(), off.Random.Capture());
    }

    /// <summary>
    /// Saisonwechsel: Wettbewerbe in der Reihenfolge des Originals – WM (Datei 0x565F6, ohne Würfe), Europapokal (Datei 0x565FF),
    /// DEB-Pokal (Datei 0x56609) – aus dem gemeinsamen Strom. Vorher DEB-Pokal vor Europapokal aus <c>CupRandom</c>.
    /// Angepasst in Spur national Teil 2 (A825, R15-E2 = C): Die Länder mischt 0x35EDD schon am 29. April (Datei 0x5644E,
    /// <see cref="WorldCup.MixForNextSeason"/>); vorher erwartete dieser Test die 40 Würfe hier ([APPROX] des Remakes).
    /// </summary>
    [Fact]
    public void SeasonStart_Cups_DrawInOriginalOrderFromSharedStream()
    {
        var (game, _) = Create(5, null, 2);
        game.WorldCup!.MixForNextSeason(new GameRandom(1)); // Stand nach dem 29. April (eigene Quelle: der Strom hier bleibt)
        var strength = game.Clubs.Where(c => c.Index >= EuropeanCup.FirstOpponent).ToDictionary(c => c.Index, c => SpectatorRules.Strength(game, c));
        var sim = GameRandom.FromState(game.Random.Capture());
        var order = game.WorldCup.NextOrder!.ToList();
        var europe = SimulateEuropeDraw(sim, strength, game.Clubs.Count, EuropeanCup.Entrants(game));
        var deb = SimulateDebDraw(sim, game);

        game.StartCompetitionSeasons();

        Assert.Equal(order, game.WorldCup.Order);
        Assert.Equal(europe, game.EuropeanCup!.Draw);
        Assert.Equal(deb, game.DebCup!.Draw);
        Assert.Equal(sim.Capture(), game.Random.Capture());
    }

    /// <summary>
    /// Pokalspiele ziehen aus dem gemeinsamen Strom: Ein Wurf mehr vor dem Pokaltag gibt andere Ergebnisse. Vorher (eigene Quelle
    /// <c>CupRandom</c>) blieben sie gleich.
    /// </summary>
    [Theory]
    [InlineData(Competition.DebCup)]
    [InlineData(Competition.EuropeanCup)]
    public void CupMatches_FollowSharedStream(Competition kind)
    {
        static List<(int, int, int, int)> Results(GameState g, Competition kind) =>
            (kind == Competition.DebCup ? g.DebCup!.Matches : g.EuropeanCup!.Matches).Select(m => (m.Home, m.Away, m.HomeGoals, m.AwayGoals)).ToList();
        static void ToCupDay(GameState g, Competition kind)
        {
            for (var guard = 0; guard < 400; guard++)
            {
                var days = SeasonCalendar.DaysOf(kind == Competition.DebCup ? DayEvents.DebCup : DayEvents.EuropeanCup);
                if (days.Contains(g.CurrentDay))
                    return;
                g.Continue();
            }
            throw new InvalidOperationException("Kein Pokaltag gefunden.");
        }

        var (a, _) = Create(3, null, 2);
        var (b, _) = Create(3, null, 2);
        ToCupDay(a, kind);
        ToCupDay(b, kind);
        Assert.Equal(a.Random.Capture(), b.Random.Capture());
        // Spur saison-geld Teil 1 (Runde 25, T03, R15-E2 = C): 8 statt 7 Würfe – mit dem Markt-Takt random(0, 3) je Markt-Platz (A829,
        // Datei 0x07215) ergeben 7 zusätzliche Würfe zufällig dieselben Ergebnisse des DEB-Pokaltags; die Probe hängt an der Zahl.
        for (var i = 0; i < 8; i++)
            _ = b.Random.Next();
        a.Continue();
        b.Continue();

        Assert.NotEmpty(Results(a, kind));
        Assert.NotEqual(Results(a, kind), Results(b, kind));
    }

    // ---- ContractRandom ----------------------------------------------------------------------------------------------

    private static int R(Random random, int a, int b) => TransferRules.Random16(random, a, b);

    /// <summary>
    /// Verhandlung „Mach' ich!“ (Datei 0x4AB66): Annahme 0x49A3B (random(85, 115), random(W − 6, W + 4)) und Sperre random(10, 18)
    /// (Datei 0x4AD3E) ziehen aus dem gemeinsamen Strom; die Quelle der Computerkader bleibt unberührt. Vorher aus
    /// <c>ContractRandom</c>; der Strom blieb stehen.
    /// </summary>
    [Fact]
    public void Negotiation_AcceptanceAndLock_DrawFromSharedStream()
    {
        var (game, _) = Create(3, null, 2);
        var manager = game.ActiveManager;
        var club = game.Clubs[manager.ClubIndex];
        var p = club.Players.First(x => x.Role == Formats.PlayerRole.Forward);
        p.ContractYears = 2;
        var value = game.PlayerValueOf(p, club, PlayerValueKind.Salary, new Random(0), manager.Level);
        var offer = Math.Max(1, value / 30); // weit unter dem Wert: 0x49A3B lehnt nach seinen Würfen ab
        var sim = GameRandom.FromState(game.Random.Capture());
        var computer = game.ComputerSquadRandom.Capture();
        var accepted = ContractRules.Accepts(2, p.MonthlySalary * 3, 3, offer * 3, value, game.ContractSeasonRemaining, false, p.Age, sim);

        var result = Contracts.Negotiate(game, manager, p, 3, offer, false, demandFilled: false);

        Assert.False(accepted);
        Assert.Equal(NegotiationResult.NotInterested, result);
        Assert.Equal(R(sim, 10, 18), p.NegotiationLock);
        Assert.Equal(sim.Capture(), game.Random.Capture());
        Assert.Equal(computer, game.ComputerSquadRandom.Capture());
    }

    /// <summary>
    /// Anklicken eines unaufgeforderten Angebots (Datei 0x46E04–0x46ED2): Wert(Preis) und Faktor random(87, 125) bzw.
    /// random(112, 155) aus dem gemeinsamen Strom. Vorher aus <c>ContractRandom</c>.
    /// </summary>
    [Fact]
    public void UnsolicitedOffer_Amount_DrawsFromSharedStream()
    {
        var (game, _) = Create(3, null, 2);
        var manager = game.ActiveManager;
        var club = game.Clubs[manager.ClubIndex];
        var p = club.Players.First(x => x.Role == Formats.PlayerRole.Forward);
        var buyer = game.Clubs[20];
        p.UnsolicitedOffer = buyer;
        var sim = GameRandom.FromState(game.Random.Capture());
        var computer = game.ComputerSquadRandom.Capture();
        var value = game.PlayerValueOf(p, club, PlayerValueKind.Price, sim, manager.Level);
        var expected = UnsolicitedOfferRules.AmountThirds(value, false, sim) / 3;

        var offer = game.OpenUnsolicitedOffer(manager, p);

        Assert.Equal(expected, offer!.Amount);
        Assert.Equal(sim.Capture(), game.Random.Capture());
        Assert.NotEqual(GameRandom.FromState(sim.Capture()).Next(), GameRandom.FromState(computer).Next()); // Probe: zwei verschiedene Folgen
        Assert.Equal(computer, game.ComputerSquadRandom.Capture());
    }

    // ---- FinanceRandom, A464 Rest -------------------------------------------------------------------------------------

    /// <summary>Werbeangebote 0x3FB47 nachgerechnet (Würfe in der Reihenfolge von <see cref="GameState.RefreshSponsorOffers"/>).</summary>
    private static List<(AdSlotKind Kind, int Partner, int Months, int Boards, long Amount, int Option)> SimulateOffers(Random sim, int rating)
    {
        var offers = new List<(AdSlotKind, int, int, int, long, int)>();
        var w = 10 * rating;
        foreach (var kind in Enum.GetValues<AdSlotKind>())
            for (var p = 0; p < Sponsor.For(kind).Length; p++)
            {
                if (SquadSetup.Between(sim, 0, 2 * p / 3) != 0)
                    continue;
                if (SquadSetup.Between(sim, 0, 5 - w / 200) != 0 && SquadSetup.Between(sim, 0, 4) <= p)
                    continue;
                var m = SquadSetup.Between(sim, 20, 22);
                var amount = SponsorRules.OfferAmountThirds(rating, kind, p, m, sim);
                var boards = kind == AdSlotKind.Bande ? SponsorRules.Boards(m, sim) : 1;
                var (months, option) = SponsorRules.Duration(sim);
                offers.Add((kind, p, months, boards, FinanceRules.ToMarks(amount), option));
            }
        return offers;
    }

    /// <summary>
    /// Spielstart, A464 Rest: Werbebewertung 0x3F833 (Datei 0x2A03B) und Werbeangebote 0x3FB47 (Datei 0x2A061) fallen in der
    /// Managerschleife nach Preis und Gehalt aus dem gemeinsamen Strom; danach folgen die Nationalspieler (Datei 0x2A0B5). Vorher
    /// nach der Spielvorbereitung aus <c>FinanceRandom</c>; der Strom zog zwischen Startwerten und Nationalspielern nichts.
    /// </summary>
    [Fact]
    public void GameStart_SponsorOffers_DrawnInManagerLoopFromSharedStream()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Trainer", Head = 0, ClubIndex = 2 });
        RngState? after = null, atNationals = null;
        var rating = -1;
        List<(AdSlotKind, int, int, int, long, int)>? expected = null, actual = null;
        NewGame.Create(TestGames.CreateDatabase(), setup, 3, (step, g) =>
        {
            if (step == NewGame.StartStep.Offers)
            {
                Assert.All(g.Clubs[2].Players, p => Assert.True(p.MonthlySalary > 0)); // nach Preis und Gehalt (Datei 0x29EE4)
                var sim = GameRandom.FromState(g.Random.Capture());
                rating = SponsorRating.Compute(g, g.Managers[0], sim);
                expected = SimulateOffers(sim, rating);
                after = sim.Capture();
            }
            else if (step == NewGame.StartStep.Nationals)
            {
                atNationals = g.Random.Capture();
                actual = g.Ads(g.Managers[0]).Offers.SelectMany(k => k.Value.Values)
                    .Select(o => (o.Kind, o.Sponsor, o.Months, o.Boards, o.MonthlyAmount, o.OptionMonths)).ToList();
                Assert.Equal(rating, g.Finance(g.Managers[0]).SponsorRating);
            }
        });

        Assert.NotEmpty(expected!);
        Assert.Equal(expected, actual);
        Assert.Equal(after, atNationals);
    }

    /// <summary>
    /// Spielstart: Die 90 Börsentage (0x5784A, Datei 0x43AD5–0x43AE0) laufen nach dem ersten Listenwechsel (Datei 0x43A9F) aus dem
    /// gemeinsamen Strom. Vorher beim ersten Zugriff aus <c>FinanceRandom</c>.
    /// </summary>
    [Fact]
    public void GameStart_StockDays_DrawnAfterFirstListingFromSharedStream()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Trainer", Head = 0, ClubIndex = 2 });
        RngState? before = null, atTurn = null;
        List<(long Base, int Price)>? actual = null;
        NewGame.Create(TestGames.CreateDatabase(), setup, 3, (step, g) =>
        {
            if (step == NewGame.StartStep.Stocks)
                before = g.Random.Capture();
            else if (step == NewGame.StartStep.Turn)
            {
                atTurn = g.Random.Capture();
                actual = g.Stocks.Stocks.Select(k => ((long)k.Base, (int)k.Price)).ToList();
            }
        });
        var sim = GameRandom.FromState(before!);
        var expected = StockMarket.Create(sim);

        Assert.Equal(expected.Stocks.Select(k => ((long)k.Base, (int)k.Price)).ToList(), actual);
        Assert.Equal(sim.Capture(), atTurn);
        Assert.NotEqual(before, atTurn);
    }

    /// <summary>
    /// Talentsuche starten (Datei 0x06FA5 ff.): Dauer random(MinTicks, MaxTicks), Zuschlag bei random(0, 3) = 0 – aus dem
    /// gemeinsamen Strom. Vorher aus <c>FinanceRandom</c>.
    /// </summary>
    [Fact]
    public void TalentSearch_Start_DrawsFromSharedStream()
    {
        var (game, _) = Create(3, null, 2);
        var manager = game.ActiveManager;
        manager.Money = 10_000_000;
        var sim = GameRandom.FromState(game.Random.Capture());
        var ticks = R(sim, TalentScout.MinTicks, TalentScout.MaxTicks);
        if (R(sim, 0, 3) == 0)
            ticks += TalentScout.ExtraTicks;

        Assert.Equal(TalentSearchStart.Started, game.StartTalentSearch(manager));

        Assert.Equal(ticks, game.Finance(manager).TalentSearch!.TicksLeft);
        Assert.Equal(sim.Capture(), game.Random.Capture());
    }

    /// <summary>
    /// Börsentag im Tagesabschluss (0x5784A): Kurse aus dem gemeinsamen Strom – ein Zwilling, dessen Börse mit einer Kopie des
    /// Stroms rechnet, hat dieselben Kurse und denselben Stromzustand. Vorher aus <c>FinanceRandom</c>.
    /// </summary>
    [Fact]
    public void StockDay_DrawsFromSharedStream()
    {
        var (a, _) = Create(3, null, 2);
        var (b, _) = Create(3, null, 2);
        var sim = GameRandom.FromState(b.Random.Capture());
        b.Stocks.Advance(sim);

        a.AdvanceStocks();

        Assert.Equal(b.Stocks.Stocks.Select(k => k.Price), a.Stocks.Stocks.Select(k => k.Price));
        Assert.Equal(sim.Capture(), a.Random.Capture());
        Assert.NotEqual(b.Random.Capture(), a.Random.Capture());
    }

    /// <summary>
    /// Spielstand Schema 53 mit den entfallenen Quellen <c>_cupRandom</c> und <c>_financeRandom</c> lädt nach der Migration 53 → 54; der gemeinsame Strom
    /// bleibt.
    /// </summary>
    [Fact]
    public void SaveGame_Schema53_DropsOwnSources()
    {
        var (game, _) = Create(3, null, 2);
        var root = JsonNode.Parse(Encoding.UTF8.GetString(SaveGame.Save(game)))!.AsObject();
        Assert.True(SaveGame.SchemaVersion >= 54); // kaderplatz-werte: Schema 55 hängt eine weitere Migration an (Runde 22)
        var state = root["spiel"]!.AsObject();
        foreach (var field in RemovedFields)
            Assert.False(state.ContainsKey(field));
        // Stand „Schema 53“ nachbauen: die Quellen als eigene Objekte an ihrem Platz in der Feldfolge, Objektnummern fortlaufend
        // in Dateireihenfolge (wie der Schreiber von Schema 53), Verweise umgeschrieben.
        root["schema"] = 53;
        var nextId = -1;
        foreach (var field in RemovedFields)
        {
            var source = state["Random"]!.DeepClone().AsObject();
            source["$id"] = nextId--;
            state[field] = source;
        }
        var fields = state.ToList();
        state.Clear();
        foreach (var (key, value) in fields.OrderBy(f => f.Key switch { "$id" => 0, "$type" => 1, _ => 2 })
                     .ThenBy(f => f.Key, StringComparer.Ordinal))
            state[key] = value;
        var map = new Dictionary<int, int>();
        var next = 0;
        void Number(JsonNode? node)
        {
            if (node is JsonObject obj)
            {
                if (obj["$id"] is JsonValue id)
                {
                    map[id.GetValue<int>()] = next;
                    obj["$id"] = next++;
                }
                else if (obj.Count == 1 && obj["$ref"] is JsonValue target)
                    obj["$ref"] = map[target.GetValue<int>()];
                foreach (var (key, child) in obj.ToArray())
                    if (key is not "$id" and not "$ref")
                        Number(child);
            }
            else if (node is JsonArray array)
                foreach (var child in array.ToArray())
                    Number(child);
        }
        Number(root);

        var loaded = SaveGame.Load(Encoding.UTF8.GetBytes(root.ToJsonString()));

        Assert.True(loaded.Success, loaded.Error);
        Assert.Equal(game.Random.Capture(), loaded.Game!.Random.Capture());
        Assert.Equal(SaveGame.Save(game), SaveGame.Save(loaded.Game));
    }

    private static readonly string[] RemovedFields = ["_cupRandom", "_financeRandom"];
}
