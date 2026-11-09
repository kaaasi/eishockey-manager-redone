using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur nationalteam, Runde 14, Teil 4 (#664, R14NT-08): WM-Schleife des Originals (Datei 0x55570–0x55740) für alle Spielarten
/// mit Vereinssaison – auch ohne Bundestrainer (Modus 2, Modus 4 ohne Manager unter den ersten fünf).
/// </summary>
/// <remarks>
/// [ORIGINAL] Ein Tag der Schleife (selbst nachgelesen mit <c>tools/reverse-engineering/disasm.py</c>):
/// <list type="number">
/// <item>Datei 0x55587–0x555D6: nur mit Bundestrainer und Tagesbyte ≠ 0 sein Menü und 0x952C(5, 0).</item>
/// <item>Datei 0x555DD–0x555F3: Modus ≠ 2 und Tagesbyte ≠ 0 → WM-Spiele 0x30E7B(1). 0x36B5D setzt dabei DGROUP 0xA70E := 0
///   (Datei 0x36B7A).</item>
/// <item>Datei 0x5561A–0x5565C: Tageszähler DGROUP 0x3B7 ≡ 0 oder 4 (mod 7) → Tagesablauf 0x07169 für Manager DGROUP 0xA70E –
///   an einem WM-Spieltag also Manager 0, sonst 5 (Datei 0x55581/0x555BC), und 0x07169 endet bei 0xA70E ≥ Zahl der Manager
///   sofort (Datei 0x07172–0x0717B → 0x07C0C). Der Zähler steht an den Spielen von Tag d auf d + 1 (<see cref="DayCounter"/>;
///   FILE18 14.04.1994 = Tag 216: 217), geprüft wird vor dem Weiterzählen (Datei 0x55670). Im WM-Zeitraum (Tage 216–230,
///   Zähler 217–231) trifft das die WM-Tage 216, 220 und 230 (Zähler 217, 221, 231; Review #674, R14NT4-01).</item>
/// <item>Datei 0x55670–0x55721: nächster Tag, je Manager 0x3ED01 (Tagesabschluss), 0x3F833 (Werbebewertung), am Zähler ≡ 0
///   (mod 7) 0x3D316 (Stadionbau), je Kaderplatz 0x07C12 (Heilung bei Zähler ≡ 0 mod 7). Keine Wanderung 0x9D1B, keine Talentsuche 0x06FA5,
///   kein Listenwechsel 0x494EF (Datei 0x54E71 nur im Hauptweg).</item>
/// </list>
/// </remarks>
public class NationalteamRunde14Teil4Tests
{
    /// <summary>Zustand am ersten Tag nach der Vereinssaison (14. April), verglichen mit dem Saisonende.</summary>
    private sealed record Window(long[] Training, long[] TrainingCost, Player[] Listings, string TeamValues)
    {
        /// <summary>Stand nehmen; vorher eine Einheit „Special“ für alle Manager, damit jeder Trainingstag Geld kostet (Zähler).</summary>
        public static Window Take(GameState game)
        {
            foreach (var m in game.Managers)
                PaidTraining(m);
            return Snapshot(game);
        }

        private static Window Snapshot(GameState game) => new(
            [.. game.Managers.Select(m => game.Finance(m).Book.Current(FinanceCategory.Training))],
            [.. game.Managers.Select(m => game.PrepareTraining(m).CostPerTrainingDay)],
            [.. game.TransferMarket.Listings.Select(l => l.Player)],
            LeagueTeamValues(game));

        public long[] TrainingDelta(GameState game) =>
            [.. game.Managers.Select((m, i) => game.Finance(m).Book.Current(FinanceCategory.Training) - Training[i])];
    }

    /// <summary>Plan mit einer Einheit „Special“ (Art 0 kostet, Datei 46E:0C89); sonst nur freie Arten.</summary>
    private static void PaidTraining(Manager manager)
    {
        manager.Training.Fill(TrainingKind.GoalDrill);
        manager.Training[0, 0] = TrainingKind.Special;
    }

    /// <summary>Teamwerte aller Ligavereine (die Wanderung 0x9D1B würfelt deren Form 0x1C/0x1D neu).</summary>
    private static string LeagueTeamValues(GameState game) =>
        string.Join(";", game.Clubs.Where(c => c.League is not null).Select(c => c.TeamValues?.ToString() ?? "-"));

    /// <summary>Erster Verein der 2. Liga (Manager dort kommt nie unter die ersten fünf der 1. Liga).</summary>
    private static int SecondLeagueClub(int skip = 0) =>
        Enumerable.Range(0, 200).Where(i => NewGame.DatabaseLeague(i) == League.ZweiteLiga).Skip(skip).First();

    private static void TopUp(GameState game)
    {
        foreach (var m in game.Managers)
            m.Money = Math.Max(m.Money, 10_000_000);
    }

    private static void PlayToSeasonOver(GameState game, Action<GameState>? eachTurn = null)
    {
        for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
        {
            Assert.True(guard < 3000, "Saisonende nicht erreicht");
            TopUp(game);
            eachTurn?.Invoke(game);
            game.Continue();
        }
        Assert.Equal(SeasonPhase.SeasonOver, game.Phase);
    }

    /// <summary>
    /// #664 (1), Modus 2 („nur Fahne“): Regressionstest (vorher rot: Training an den Trainingstagen, Listenwechsel und
    /// Wanderung bis zum 29. April). [ORIGINAL] Datei 0x553FF → 0x554CF: Ohne Ernennung (Modus 2, Datei 0x55436) würfelt das
    /// Original die Werte Deutschlands und läuft dann ebenfalls in die Schleife 0x55570. Ohne WM-Spiele bleibt DGROUP 0xA70E = 5,
    /// 0x07169 tut also nichts: kein Training, kein Markt, keine Aufstellungsautomatik; keine Wanderung.
    /// </summary>
    [Theory]
    [InlineData(3)]
    [InlineData(11)]
    public void ClubOnly_WorldCupPeriod_NoTrainingNoMarketNoDrift(int seed)
    {
        var game = GameModeTests.Create(seed, true, false, 2);
        Assert.Equal(GameMode.ClubOnly, game.Mode);
        NationalCoachMode4Tests.PlayToClubSeasonEnd(game);
        Assert.Null(game.NationalCoach);
        var injured = game.Clubs[2].Players[0];
        injured.InjuredGames = 3; // Heilung 0x07C12 läuft weiter: Tage 216, 223, 230 (Zähler 217, 224, 231 ≡ 0 mod 7)
        var before = Window.Take(game);
        var strains = SetListingStrains(game);

        PlayToSeasonOver(game);

        Assert.All(before.TrainingDelta(game), d => Assert.Equal(0, d));
        Assert.Equal(before.Listings, game.TransferMarket.Listings.Select(l => l.Player));
        Assert.Empty(game.TransferMarket.Bids);
        Assert.Equal(before.TeamValues, LeagueTeamValues(game));
        Assert.Equal(0, injured.InjuredGames);
        Assert.Equal(strains, ListingStrains(game)); // kein Markt-Verfall 0x071BB ohne WM-Spiele
    }

    /// <summary>
    /// #664 (1), Modus 4 ohne Bundestrainer (zwei Manager in der 2. Liga): Regressionstest (vorher rot: beide trainierten an den
    /// Trainingstagen, die Wanderung lief). [ORIGINAL] Ohne Manager unter den ersten fünf (Datei 0x55440–0x554BF) keine Ernennung,
    /// dieselbe Schleife; an den WM-Spieltagen 216, 220 und 230 (Zähler 217, 221, 231 ≡ 0/4 mod 7) läuft nach den Spielen 0x07169
    /// für Manager 0 (DGROUP 0xA70E = 0 aus 0x36B5D), also dreimal sein Training und der Markt-Verfall (Datei 0x071BB) und keins
    /// für Manager 1 (Review #674, R14NT4-01: vorher zweimal an 224/228).
    /// </summary>
    [Theory]
    [InlineData(3)]
    [InlineData(11)]
    public void Mode4_NoCoach_TrainingOnlyManagerZeroAfterWorldCupDriftDays(int seed)
    {
        var game = GameModeTests.Create(seed, true, true, SecondLeagueClub(), SecondLeagueClub(1));
        Assert.Equal(GameMode.Full, game.Mode);
        TopUp(game);
        NationalCoachMode4Tests.PlayToClubSeasonEnd(game);
        Assert.Null(game.NationalCoach);
        var before = Window.Take(game);
        Assert.True(before.TrainingCost[0] > 0);
        var strains = SetListingStrains(game);

        PlayToSeasonOver(game);

        Assert.Equal([-3 * before.TrainingCost[0], 0], before.TrainingDelta(game));
        Assert.Equal(Decayed(strains, 3), ListingStrains(game));
        Assert.Equal(before.Listings, game.TransferMarket.Listings.Select(l => l.Player)); // kein Listenwechsel (R14NT4-05)
        Assert.Equal(before.TeamValues, LeagueTeamValues(game));
        Assert.Equal(GameState.FinalWorldCupStage, game.WorldCupStage);
        Assert.NotNull(game.WorldCup!.Winner);
    }

    /// <summary>
    /// #664 (2), Modus 4 mit Bundestrainer (Manager 0 ernannt): Regressionstest (vorher rot: Training an den Trainingstagen des
    /// Remakes). [ORIGINAL] Wie ohne Bundestrainer: 0x07169 nur nach den WM-Spielen der Tage 216, 220 und 230 (Datei 0x5565C,
    /// Zähler 217, 221, 231; R14NT4-01).
    /// [APPROX] Manager 0 auch dann, wenn die letzte Strafe des Spieltags Deutschland traf (0x35818 schreibt dann 0xA70E :=
    /// Feld +0x16 − 1 = 5, Datei 0x35AB8–0x35AC6; 0x07169 täte nichts).
    /// </summary>
    [Theory]
    [InlineData(3)]
    [InlineData(11)]
    public void Mode4_Coach_TrainingOnlyAfterWorldCupDriftDays(int seed)
    {
        var game = GameModeTests.Create(seed, true, true, 2);
        ManagedSquads.PlayAtTopStrength(game, 0);
        NationalCoachMode4Tests.PlayToClubSeasonEnd(game);
        Assert.Equal(0, game.NationalCoach?.Manager);
        var before = Window.Take(game);

        PlayToSeasonOver(game, g =>
        {
            if (g.NationalTurn && g.NationalCoach!.Squad.Count == 0)
                g.AutoNominate();
        });

        Assert.Equal([-3 * before.TrainingCost[0]], before.TrainingDelta(game));
        Assert.Equal(before.TeamValues, LeagueTeamValues(game));
        Assert.Equal(GameState.FinalWorldCupStage, game.WorldCupStage);
    }

    /// <summary>
    /// Mehrsaisonprobe zu #664 (AGENTS.md §5: 4 Saisonwechsel, passiver und aktiver Manager), Modus 4 mit zwei Managern: Manager
    /// 0 bei Verein 2 (aktiv mit Höchstwerten, dann meist Bundestrainer), Manager 1 in der 2. Liga (nie Bundestrainer). In jeder
    /// Saison: WM vollständig, im WM-Zeitraum genau drei Trainings von Manager 0 (Tage 216, 220, 230; R14NT4-01) und keins von Manager 1, keine Wanderung der
    /// Ligavereine; Speichern/Laden stabil.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Mode4_FourSeasonChanges_WorldCupLoopWithAndWithoutCoach(bool active)
    {
        var game = GameModeTests.Create(5, true, true, 2, SecondLeagueClub());
        var coachSeasons = 0;
        for (var season = 0; season < 5; season++)
        {
            for (var guard = 0; game.CurrentDay <= SeasonCalendar.ClubSeasonEnd; guard++)
            {
                Assert.True(guard < 3000, "13. April nicht erreicht");
                TopUp(game);
                if (active)
                {
                    TestSeason.KeepSquad(game, game.Managers[0], extendExpired: true);
                    ManagedSquads.PlayAtTopStrength(game, 0);
                }
                game.Continue();
            }
            Assert.NotEqual(1, game.NationalCoach?.Manager);
            coachSeasons += game.NationalCoach is null ? 0 : 1;
            var before = Window.Take(game);
            PlayToSeasonOver(game, g =>
            {
                if (active && g.NationalTurn && g.NationalCoach!.Squad.Count == 0)
                    g.AutoNominate();
            });

            Assert.Equal([-3 * before.TrainingCost[0], 0], before.TrainingDelta(game));
            Assert.Equal(before.TeamValues, LeagueTeamValues(game));
            Assert.Equal(GameState.FinalWorldCupStage, game.WorldCupStage);
            var saved = SaveGame.Save(game);
            Assert.Equal(saved, SaveGame.Save(SaveGame.Load(saved).Game!));
            if (season < 4)
                game.StartNextSeason();
        }
        if (active)
            Assert.True(coachSeasons > 0, "aktiv: nie Bundestrainer");
    }

    /// <summary>
    /// „Nur Weltkugel“ über eine WM, Automatik an und aus (Regressionsschutz). Der Modus läuft im Original durch dieselbe Schleife
    /// (Datei 0x54D38 → 0x553FF → 0x55570, R14NT4-04); Training des Managers gibt es dort im Remake nicht ([APPROX], siehe
    /// <c>CupsNational.WorldCupLoop.cs</c>; Konto FILE19 → FILE20 unverändert). Mit Automatik stellt jedes Menü des Bundestrainers neu auf,
    /// ohne (Aufstellung im ersten Zug geleert) bleibt sie über alle Termine leer; die WM endet mit Stufe 7, kein Training.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WorldOnly_OneWorldCup_AutoOnOff(bool auto)
    {
        var game = GameModeTests.Create(7, false, true, 2);
        Assert.Equal(GameMode.WorldOnly, game.Mode);
        PaidTraining(game.Managers[0]);
        var training = game.Finance(game.Managers[0]).Book.Current(FinanceCategory.Training);
        var turns = 0;
        for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
        {
            Assert.True(guard < 100, "Saisonende nicht erreicht");
            if (game.NationalCoachMenu && game.NationalCoach is { } coach)
            {
                game.OnMainMenu(game.Managers[0], isReturn: false);
                if (turns++ == 0)
                {
                    game.AutoNominate();
                    game.OnMainMenu(game.Managers[0], isReturn: true);
                    if (!auto)
                        coach.ClearFormation();
                }
                Assert.Equal(auto, coach.AutoLineupEnabled);
                Assert.Equal(auto, coach.Formation.Goalkeeper is not null);
            }
            game.Continue();
        }
        Assert.Equal(SeasonPhase.SeasonOver, game.Phase);
        Assert.True(turns >= SeasonCalendar.DaysOf(DayEvents.WorldCup).Count());
        Assert.Equal(GameState.FinalWorldCupStage, game.WorldCupStage);
        Assert.Equal(training, game.Finance(game.Managers[0]).Book.Current(FinanceCategory.Training));
    }

    // ---- Review #674 (R14NT4-01 … -05): Tageszähler 0x3B7, Markt-Verfall, Heilung, Talentsuche ------------------------------

    /// <summary>Belastung (Kaderplatz +0x13) der Marktspieler in Listenreihenfolge.</summary>
    private static int[] ListingStrains(GameState game) => [.. game.TransferMarket.Listings.Select(l => l.Player.Strain)];

    /// <summary>Belastung der Marktspieler setzen: der erste 60 (fällt einmal auf 50, dann nicht mehr), die übrigen 120.</summary>
    private static int[] SetListingStrains(GameState game)
    {
        var listings = game.TransferMarket.Listings;
        Assert.True(listings.Count >= 2, "zu wenige Marktspieler");
        for (var i = 0; i < listings.Count; i++)
            listings[i].Player.Strain = i == 0 ? 60 : 120;
        return ListingStrains(game);
    }

    /// <summary>
    /// Erwartete Belastung nach <paramref name="runs"/> Ausführungen von 0x07169 für Manager 0. [ORIGINAL] Datei 0x0718F–0x071CC:
    /// +0x13 &gt; 0x38 → −10 (Byte-Suche <c>26 80 7F 13 38</c>, einziger Treffer 0x071BB).
    /// </summary>
    private static int[] Decayed(int[] strains, int runs) =>
        [.. strains.Select(s => Enumerable.Range(0, runs).Aggregate(s, (v, _) => v > 0x38 ? v - 10 : v))];

    /// <summary>
    /// R14NT4-01/-04 (Review #674): Regressionstest (vorher rot: kein Markt-Verfall, 0x07169 erst nach den Spielen der Tage 224
    /// und 228; „nur Weltkugel“ ganz ohne Schleifentag). Muster der Original-Spielstände (Modus 1, <c>emtool hok dump</c>):
    /// FILE18 (14.04., Zähler 217) → FILE48 (16.04., 219): alle Marktspieler E −6/−7 = −10 roh, eine Ausführung nach den Spielen
    /// von Tag 216; FILE19 (217) → FILE20 (28.04., 231): zwei Ausführungen (Zähler 217, 221), die dritte (231) erst nach dem
    /// gesicherten Stand; die Liste bleibt gleich. Modus 4 mit Bundestrainer genauso (dieselbe Schleife 0x55570).
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WorldCupLoop_MarketDecay_LikeFile18To48AndFile19To20(bool worldOnly)
    {
        GameState game;
        if (worldOnly)
        {
            game = GameModeTests.Create(7, false, true, 2);
            Assert.Equal(GameMode.WorldOnly, game.Mode);
            game.Continue(); // Sprung zum ersten WM-Tag (Datei 0x54CEE–0x54D38)
        }
        else
        {
            game = GameModeTests.Create(3, true, true, 2);
            ManagedSquads.PlayAtTopStrength(game, 0);
            NationalCoachMode4Tests.PlayToClubSeasonEnd(game);
            Assert.Equal(0, game.NationalCoach?.Manager);
        }
        Assert.Equal(GameState.FirstWorldCupDay, game.CurrentDay);
        Assert.Equal(216, game.CurrentDay);
        var players = game.TransferMarket.Listings.Select(l => l.Player).ToList();
        var strains = SetListingStrains(game);
        var seen = new Dictionary<int, int[]>();
        for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
        {
            Assert.True(guard < 200, "Saisonende nicht erreicht");
            TopUp(game);
            if (game.NationalTurn && game.NationalCoach!.Squad.Count == 0)
                game.AutoNominate();
            seen[game.CurrentDay] = ListingStrains(game);
            game.Continue();
        }

        Assert.Equal(Decayed(strains, 1), seen[218]); // FILE18 → FILE48
        Assert.Equal(Decayed(strains, 2), seen[230]); // FILE19 → FILE20
        Assert.Equal(Decayed(strains, 3), ListingStrains(game));
        Assert.Equal(players, game.TransferMarket.Listings.Select(l => l.Player)); // kein Listenwechsel 0x494EF (R14NT4-05)
    }

    /// <summary>
    /// R14NT4-01 (Heilung): Regressionstest (vorher rot: Heilung an den Remake-Tagen ≡ 0 mod 7). [ORIGINAL] 0x07C12 heilt bei
    /// (Zähler 0x3B7 − [0x6AF]) ≡ 0 (mod 7); [0x6AF] ist 0 (DGROUP-Startwert, Datei 0x16DD0 + 0x6AF; nur Leser 0x07C3B,
    /// 0x43A93, 0x565A7, die den Zähler zum Spiel- und Saisonstart auf [0x6AF] setzen). Schritt (d) heilt nach dem Weiterzählen
    /// (Remake-Tage 216, 223, 230: Zähler 217, 224, 231), Schritt (c) für den Kader von Manager 0 vor dem Weiterzählen nach den
    /// WM-Spielen der Tage 216 und 230 (Zähler 217, 231). Ein Spieler von Manager 0 heilt also fünf Wochen, einer ohne Manager
    /// drei.
    /// </summary>
    [Fact]
    public void Mode4_NoCoach_HealingFollowsDayCounter()
    {
        var game = GameModeTests.Create(3, true, true, SecondLeagueClub(), SecondLeagueClub(1));
        TopUp(game);
        NationalCoachMode4Tests.PlayToClubSeasonEnd(game);
        Assert.Null(game.NationalCoach);
        var own = game.Clubs[game.Managers[0].ClubIndex].Players[^1];
        var other = game.Clubs[SecondLeagueClub(2)].Players[^1];
        own.InjuredGames = 6;
        other.InjuredGames = 4;

        PlayToSeasonOver(game);

        Assert.Equal(1, own.InjuredGames);
        Assert.Equal(1, other.InjuredGames);
    }

    /// <summary>
    /// R14NT4-02 (Review #674): Regressionstest (Mutation M5 – Talentsuche 0x06FA5 und Jugend 0x39DEC in der Schleife – blieb
    /// grün). [ORIGINAL] 0x06FA5 hat nur den Aufrufer Datei 0x5528A im Hauptweg, in 0x55570–0x55740 kein Aufruf: Eine laufende
    /// Suche zählt über die WM nicht herunter, mit und ohne Bundestrainer.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Mode4_WorldCupLoop_TalentSearchStands(bool coach)
    {
        var game = coach
            ? GameModeTests.Create(3, true, true, 2)
            : GameModeTests.Create(3, true, true, SecondLeagueClub(), SecondLeagueClub(1));
        if (coach)
            ManagedSquads.PlayAtTopStrength(game, 0);
        TopUp(game);
        NationalCoachMode4Tests.PlayToClubSeasonEnd(game);
        Assert.Equal(coach, game.NationalCoach is not null);
        foreach (var m in game.Managers)
            game.Finance(m).TalentSearch = new TalentSearch { TicksLeft = 3 };

        PlayToSeasonOver(game, g =>
        {
            if (g.NationalTurn && g.NationalCoach!.Squad.Count == 0)
                g.AutoNominate();
        });

        Assert.All(game.Managers, m => Assert.Equal(3, game.Finance(m).TalentSearch?.TicksLeft));
    }

    /// <summary>
    /// #675 (R14NT4-03, Review #674): Regressionstest (vorher rot: Takttage des Hauptwegs mit dem Remake-Tag, 0, 4, 7, 11, 14, 18 …).
    /// [ORIGINAL] Datei 0x55204 zählt DGROUP 0x3B7 nach den Spielen weiter, Talentsuche 0x06FA5, Wanderung 0x9D1B und 0x07169
    /// (Datei 0x55253–0x552D3) prüfen den neuen Wert ≡ 0/4 (mod 7). Im Tageslauf <c>OnDayManager(e)</c> ist das Zähler e + 1:
    /// Takt an den Tagen 3, 6, 10, 13, 17, 20 … FILE24 (Tag 14, Zähler 15) → FILE25 (Tag 17, 18): ein Takt (Markt-Verfall
    /// E −6/−7) nach den Spielen von Tag 16, im Remake vor den Spielen von Tag 17 (das Remake rechnet den Tageslauf nach dem Menü).
    /// </summary>
    [Fact]
    public void MainPath_TalentSearchTicks_FollowDayCounter()
    {
        Assert.Equal(217, DayCounter.Of(216));
        Assert.Equal([3, 6, 10, 13, 17, 20, 24, 27], Enumerable.Range(0, 30).Where(TalentScout.IsTick));

        var game = TestGames.Create(7);
        var me = game.ActiveManager;
        game.Finance(me).TalentSearch = new TalentSearch { TicksLeft = 1000 };
        var expected = 1000;
        for (var guard = 0; game.FinanceDay < 60; guard++)
        {
            Assert.True(guard < 100, "Tag 60 nicht erreicht");
            me.Money = Math.Max(me.Money, 10_000_000);
            var from = game.FinanceDay;
            game.Continue();
            expected -= Enumerable.Range(from + 1, game.FinanceDay - from).Count(e => (e + 1) % 7 is 0 or 4);
            Assert.Equal(expected, game.Finance(me).TalentSearch!.TicksLeft);
        }
    }
}
