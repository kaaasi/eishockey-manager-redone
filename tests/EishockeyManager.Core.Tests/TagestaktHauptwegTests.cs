using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Leitstelle Runde 14, #678/#679 (Fix <c>fix/tagestakt-hauptweg</c>): Tagesablauf 0x07169 im Hauptweg nur an den Takttagen des
/// Tageszählers DGROUP 0x3B7, Listenwechsel 0x494EF nur in den Managerzügen der Tage mit Ereignis, Stadionbau 0x3D316 am Zähler ≡ 0.
/// </summary>
/// <remarks>
/// [ORIGINAL] Selbst nachgelesen (<c>tools/reverse-engineering/disasm.py gamedata/em.exe 0x551F0 0x552E0</c>,
/// <c>… 0x07169 0x07380</c>, <c>… 0x54D38 0x54EF4</c>):
/// <list type="bullet">
/// <item>Datei 0x55204 zählt 0x3B7 weiter; Datei 0x55253–0x55288: nur bei Zähler ≡ 0 oder ≡ 4 (Zähler + 3 ≡ 0) mod 7 weiter zu
///   Talentsuche 0x06FA5, Wanderung 0x9D1B und je Manager (DGROUP 0xA70E = 0 … Zahl − 1) 0x07169 (Datei 0x552A6), danach bei
///   Zähler ≡ 0 der Stadionbau 0x3D316 (Datei 0x552AB–0x552C3). Sonst springt Datei 0x55288 nach 0x552D5.</item>
/// <item>0x07169: nur für Manager 0 (Datei 0x0718F–0x07197) die Schleife über die Markt-Plätze mit Markt-Verfall (0x071BB), Lösen
///   der Sperre (0x071CC) und Gebotsverfall (0x07215–0x0726A); für jeden Manager der Angebotswurf auf seine Markt-Plätze
///   (0x0727C–0x073CE), die Verträge seiner Kaderplätze bis Zähler 233 (0x073E3–0x07AEF), Heilung, Training und
///   Aufstellungsautomatik (0x07AF2–0x07C07).</item>
/// <item>Listenwechsel: Datei 0x54E65–0x54E7C, random(0, Zahl der Manager + 2) = 0, im Zug jedes Managers (Schleife 0x54E3F–0x54EF4),
///   die Tage mit Tagesbyte 0 oder 255 überspringen diesen Teil (Datei 0x54D56–0x54D65 → 0x55073).</item>
/// </list>
/// </remarks>
public class TagestaktHauptwegTests
{
    private static int[] Ticks(int from, int toInclusive) =>
        [.. Enumerable.Range(from + 1, Math.Max(0, toInclusive - from)).Where(e => (e + 1) % 7 is 0 or 4)];

    private static void TopUp(GameState game)
    {
        foreach (var m in game.Managers)
            m.Money = Math.Max(m.Money, 10_000_000);
    }

    /// <summary>Plan mit einer Einheit „Special“ (Art 0 kostet), damit jeder Trainingstag im Finanzbuch steht.</summary>
    private static void PaidTraining(Manager manager)
    {
        manager.Training.Fill(TrainingKind.GoalDrill);
        manager.Training[0, 0] = TrainingKind.Special;
    }

    private static GameState TwoManagers(int seed)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "A", Head = 0, ClubIndex = 2 });
        setup.Managers.Add(new ManagerSetup { Name = "B", Head = 1, ClubIndex = 5 });
        return NewGame.Create(TestGames.CreateDatabase(), setup, seed);
    }

    /// <summary>Alle Manager des Hot-Seat bestätigen, bis die Zeitleiste weiterläuft.</summary>
    private static void Continue(GameState game)
    {
        var day = game.FinanceDay;
        for (var guard = 0; guard < 10 && game.FinanceDay == day; guard++)
            game.Continue();
    }

    /// <summary>
    /// #678: Trainingstage sind die Takttage des Zählers (Tageslauf <c>OnDayManager(e)</c> mit Zähler e + 1 ≡ 0/4), nicht Mittwoch
    /// und Sonntag. Regressionstest (vorher rot: 0, 3, 7, 10, 14 …).
    /// </summary>
    [Fact]
    public void TrainingDays_AreDayCounterTicks()
    {
        Assert.Equal([3, 6, 10, 13, 17, 20, 24, 27], Enumerable.Range(0, 30).Where(GameState.IsTrainingDay));
        Assert.All(Enumerable.Range(0, SeasonCalendar.SeasonEnd + 1),
            d => Assert.Equal(MatchEngine.IsDriftDay(DayCounter.Of(d)), GameState.IsTrainingDay(d)));
    }

    /// <summary>
    /// #678: Im Hauptweg trainieren beide Manager genau an den Takttagen (0x07169 je Manager, Datei 0x552A6); Kosten je Takt einmal.
    /// Regressionstest (vorher rot: Training Mittwoch und Sonntag). Gegenprobe: Mit der alten Regel weicht die Zahl der
    /// Trainingstage in den ersten 60 Tagen ab (siehe <see cref="TrainingDays_AreDayCounterTicks"/>).
    /// </summary>
    [Theory]
    [InlineData(3)]
    [InlineData(11)]
    public void MainPath_TwoManagers_TrainOnlyOnTicks(int seed)
    {
        var game = TwoManagers(seed);
        foreach (var m in game.Managers)
            PaidTraining(m);
        var costs = game.Managers.Select(m => game.PrepareTraining(m).CostPerTrainingDay).ToArray();
        Assert.All(costs, c => Assert.True(c > 0));
        var start = game.FinanceDay;
        var books = game.Managers.Select(m => game.Finance(m).Book.Current(FinanceCategory.Training)).ToArray();
        for (var guard = 0; game.FinanceDay < 60; guard++)
        {
            Assert.True(guard < 200, "Tag 60 nicht erreicht");
            TopUp(game);
            Continue(game);
            var ticks = Ticks(start, game.FinanceDay).Length;
            for (var i = 0; i < game.Managers.Count; i++)
                Assert.Equal(books[i] - ticks * costs[i], game.Finance(game.Managers[i]).Book.Current(FinanceCategory.Training));
        }
    }

    /// <summary>
    /// #678: Markt-Verfall 0x071BB im Hauptweg (nur im Lauf von Manager 0): Belastung +0x13 &gt; 0x38 sinkt je Takt um 10.
    /// Regressionstest (vorher rot: kein Verfall im Hauptweg). [ORIGINAL] FILE24 (Tag 14, Zähler 15) → FILE25 (Tag 17, Zähler 18):
    /// ein Takt (18 ≡ 4), alle sechs Marktspieler E −6/−7 (69 → 62, 71 → 64, 54 → 47, 60 → 54, 75 → 68, 64 → 58), Liste gleich.
    /// Verglichen wird nur über Schritte, in denen die Liste unverändert blieb.
    /// </summary>
    [Theory]
    [InlineData(3)]
    [InlineData(7)]
    public void MainPath_MarketDecay_OncePerTick(int seed)
    {
        var game = TestGames.Create(seed);
        var checkedSteps = 0;
        for (var guard = 0; game.FinanceDay < 80; guard++)
        {
            Assert.True(guard < 200, "Tag 80 nicht erreicht");
            TopUp(game);
            var listings = game.TransferMarket.Listings.Where(l => l.Seller is null).ToList();
            foreach (var l in listings)
                l.Player.Strain = 120;
            var players = game.TransferMarket.Listings.Select(l => l.Player).ToList();
            var from = game.FinanceDay;
            game.Continue();
            if (!players.SequenceEqual(game.TransferMarket.Listings.Select(l => l.Player)))
                continue;
            var ticks = Ticks(from, game.FinanceDay).Length;
            Assert.All(listings, l => Assert.Equal(Math.Max(120 - 10 * ticks, 50), l.Player.Strain));
            checkedSteps++;
        }
        Assert.True(checkedSteps >= 3, $"nur {checkedSteps} Schritte ohne Listenwechsel");
    }

    /// <summary>
    /// #678: Listenwechsel 0x494EF nur in den Managerzügen der Tage mit Ereignis des Hauptwegs (Tagesbyte ∉ {0, 255}), nicht am
    /// Eintrittstag der WM-Schleife (Datei 0x54E71 liegt im übersprungenen Teil) und nicht in der Schleife.
    /// </summary>
    [Fact]
    public void ListChangeDays_OnlyEventDaysOfMainPath()
    {
        var game = TestGames.Create(3);
        for (var d = 0; d <= SeasonCalendar.SeasonEnd; d++)
        {
            var events = SeasonCalendar.Events(d);
            var expected = d < SeasonCalendar.ClubSeasonEnd && events is not (DayEvents.None or DayEvents.SeasonEnd);
            Assert.Equal(expected, game.IsListChangeDay(d));
        }
    }

    /// <summary>
    /// Nachprüfung #682 (R14KS9-01): <see cref="TransferMarket.CatchUpManagerDay"/> mit zwei Managern. Der Lauf von Manager 1 löscht
    /// keine Gebote (Gebotsverfall nur Manager 0, Datei 0x0718F–0x07197) und würfelt nur auf seine eigenen Markt-Plätze
    /// (Datei 0x072A0–0x072BE). Absicherung (schon grün, Befund betraf fehlende Abdeckung).
    /// </summary>
    [Fact]
    public void CatchUpManagerDay_TwoManagers_SecondNeitherExpiresNorBidsOnForeignListings()
    {
        var game = TwoManagers(5);
        var (m0, m1) = (game.Managers[0], game.Managers[1]);
        var p0 = Listable(game.Clubs[m0.ClubIndex]);
        var p1 = Listable(game.Clubs[m1.ClubIndex]);
        Assert.Equal(TransferResult.Accepted, game.TransferMarket.Offer(game, m0, p0));
        Assert.Equal(TransferResult.Accepted, game.TransferMarket.Offer(game, m1, p1));

        for (var i = 0; i < 300 && !game.TransferMarket.Bids.Any(b => b.Listing.Player == p0); i++)
            game.TransferMarket.CatchUpManagerDay(game, m0);
        var bid0 = Assert.Single(game.TransferMarket.Bids, b => b.Listing.Player == p0);
        Assert.DoesNotContain(game.TransferMarket.Bids, b => b.Listing.Player == p1);

        for (var i = 0; i < 300; i++)
        {
            game.TransferMarket.CatchUpManagerDay(game, m1);
            Assert.Contains(bid0, game.TransferMarket.Bids);
            Assert.All(game.TransferMarket.Bids.Where(b => b != bid0), b => Assert.Same(m1, b.Listing.Seller));
        }
        Assert.Contains(game.TransferMarket.Bids, b => b.Listing.Player == p1);

        // Gegenprobe: Der Lauf von Manager 0 lässt Gebote verfallen (random(0, 3) = 0).
        for (var i = 0; i < 100 && game.TransferMarket.Bids.Contains(bid0); i++)
            game.TransferMarket.CatchUpManagerDay(game, m0);
        Assert.DoesNotContain(bid0, game.TransferMarket.Bids);
    }

    /// <summary>
    /// #678: Schritt (c) der WM-Schleife (Datei 0x5565C, 0x07169 für Manager 0 nach den WM-Spielen der Tage 216, 220, 230) würfelt
    /// Angebote auf die Markt-Plätze von Manager 0 (Datei 0x072DD) und keine auf die von Manager 1; Verträge (Datei 0x073E3–0x07AEF)
    /// nur für die Kaderplätze von Manager 0 – die Spieler von Manager 1 bekommen in der Schleife weder Meldung noch Angebot.
    /// Regressionstest (vorher rot: kein Angebotswurf in der Schleife; Verträge für alle Manager – R688-02: mit Sperre 50 bei Manager 1
    /// rot, Mutation „<c>AnnounceContractEnds(day)</c> in (c)“). Modus 4, Manager 0 wird
    /// Bundestrainer (die Zeitleiste hält an den WM-Tagen), Manager 1 in der 2. Liga.
    /// </summary>
    [Fact]
    public void WorldCupLoop_StepC_MarketAndContractsOnlyManagerZero()
    {
        var bidSeen = false;
        foreach (var seed in new[] { 3, 11 })
        {
            var second = Enumerable.Range(0, 200).First(i => NewGame.DatabaseLeague(i) == League.ZweiteLiga);
            var game = GameModeTests.Create(seed, true, true, 2, second);
            ManagedSquads.PlayAtTopStrength(game, 0);
            TopUp(game);
            NationalCoachMode4Tests.PlayToClubSeasonEnd(game);
            Assert.Equal(0, game.NationalCoach?.Manager);
            void Step()
            {
                TopUp(game);
                if (game.NationalTurn && game.NationalCoach!.Squad.Count == 0)
                    game.AutoNominate();
                game.Continue();
            }
            for (var guard = 0; game.CurrentDay <= SeasonCalendar.ClubSeasonEnd + 1; guard++)
            {
                Assert.True(guard < 20, "Tag 216 nicht verlassen");
                Step();
            }
            Assert.True(game.CurrentDay < SeasonCalendar.SeasonEnd);
            var (m0, m1) = (game.Managers[0], game.Managers[1]);
            var p0 = Listable(game.Clubs[m0.ClubIndex]);
            var p1 = Listable(game.Clubs[m1.ClubIndex]);
            Assert.Equal(TransferResult.Accepted, game.TransferMarket.Offer(game, m0, p0));
            Assert.Equal(TransferResult.Accepted, game.TransferMarket.Offer(game, m1, p1));
            // R688-02 (Review #688): Sperre 50 für alle Spieler von Manager 1 – ohne sie bliebe die Mutation „Verträge aller Manager“
            // in (c) grün (Sperre 0 zählt nicht, Meldung und Angebot fallen in den wenigen Takten selten).
            foreach (var p in game.Clubs[m1.ClubIndex].Players)
                p.NegotiationLock = 50;
            var squad1 = game.Clubs[m1.ClubIndex].Players.ToDictionary(p => p, p => (p.Notice, p.NegotiationLock, p.UnsolicitedOffer));
            for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
            {
                Assert.True(guard < 200, "Saisonende nicht erreicht");
                Step();
                Assert.DoesNotContain(game.TransferMarket.Bids, b => b.Listing.Player == p1);
                bidSeen |= game.TransferMarket.Bids.Any(b => b.Listing.Player == p0);
            }
            Assert.All(squad1, e => Assert.Equal(e.Value, (e.Key.Notice, e.Key.NegotiationLock, e.Key.UnsolicitedOffer)));
        }
        Assert.True(bidSeen, "kein Angebot auf den Markt-Platz von Manager 0 in der WM-Schleife");
    }

    /// <summary>
    /// Mehrsaisonprobe zu #678/#679 (AGENTS.md §5: 4 Saisonwechsel, passiv und aktiv), Modus 2 („nur Fahne“, keine WM-Spiele, also
    /// kein Schritt (c)), zwei Manager: Je Saison trainiert jeder Manager genau an den Takttagen bis einschließlich Tag 216
    /// (Eintrittstag, Zähler 217), danach nicht mehr; Speichern/Laden bytegleich, Spieler eindeutig. Aktiv: Kader gehalten, je
    /// Saison ein eigener Spieler auf dem Markt.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FourSeasonChanges_TwoManagers_TrainingOnTicks(bool active)
    {
        var game = GameModeTests.Create(7, true, false, 2, 30);
        Assert.Equal(GameMode.ClubOnly, game.Mode);
        var ticks = Ticks(0, SeasonCalendar.ClubSeasonEnd + 1).Length;
        for (var change = 0; change < 4; change++)
        {
            foreach (var m in game.Managers)
                PaidTraining(m);
            var costs = game.Managers.Select(m => game.PrepareTraining(m).CostPerTrainingDay).ToArray();
            if (active)
            {
                var club = game.Clubs[game.Managers[0].ClubIndex];
                Assert.Equal(TransferResult.Accepted, game.TransferMarket.Offer(game, game.Managers[0], Listable(club)));
            }
            for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
            {
                Assert.True(guard < 2000, "Saisonende nicht erreicht");
                TopUp(game);
                if (active)
                    foreach (var m in game.Managers)
                        TestSeason.KeepSquad(game, m, extendExpired: true);
                game.Continue();
            }
            for (var i = 0; i < game.Managers.Count; i++)
                Assert.Equal(-ticks * costs[i], game.Finance(game.Managers[i]).Book.Current(FinanceCategory.Training));
            var saved = SaveGame.Save(game);
            var loaded = SaveGame.Load(saved);
            Assert.True(loaded.Success, loaded.Error);
            Assert.Equal(saved, SaveGame.Save(loaded.Game!));
            game = loaded.Game!;
            Assert.Empty(game.ValidatePlayerIdentity());
            game.StartNextSeason();
        }
    }

    private static Player Listable(Club club)
    {
        Assert.True(club.ApplyAutoLineup());
        return club.Players.Where(p => !p.OnLoan && p.Role != PlayerRole.Goalkeeper && TransferMarket.CanLeave(club, p))
            .OrderByDescending(p => p.Strength).First();
    }

    /// <summary>
    /// #679: Stadionbau 0x3D316 am Tag mit Zähler ≡ 0 (mod 7), Datei 0x552C3 (Hauptweg) und 0x556B5–0x556CD (Schleife): im Tageslauf
    /// <c>OnDayManager(e)</c> mit e + 1 ≡ 0, also samstags statt montags. Regressionstest (vorher rot: Bauwoche am Montag).
    /// </summary>
    [Fact]
    public void Construction_AdvancesOnCounterZeroDays()
    {
        var game = TestGames.Create();
        var me = game.ActiveManager;
        me.Money = 100_000_000;
        var stadium = game.Finance(me).Stadium;
        var level = stadium.Level(StadiumFeature.Verkehrsanbindung);
        Assert.True(game.StartConstruction(me, StadiumProject.Feature, StadiumFeature.Verkehrsanbindung, level + 1));
        var weeks = stadium.WeeksLeft(StadiumProject.Feature, StadiumFeature.Verkehrsanbindung);
        var start = game.FinanceDay;
        for (var guard = 0; game.FinanceDay < 100; guard++)
        {
            Assert.True(guard < 200, "Tag 100 nicht erreicht");
            me.Money = Math.Max(me.Money, 100_000_000);
            game.Continue();
            var builds = Enumerable.Range(start + 1, game.FinanceDay - start).Count(e => DayCounter.Of(e) % 7 == 0);
            Assert.Equal(weeks - builds, stadium.WeeksLeft(StadiumProject.Feature, StadiumFeature.Verkehrsanbindung));
        }
    }
}
