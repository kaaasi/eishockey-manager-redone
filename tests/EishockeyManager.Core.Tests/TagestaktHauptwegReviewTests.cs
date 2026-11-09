using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Review #688 (<c>docs/reviews/2026-11-09-tagestakt-hauptweg.md</c>), Leitstelle Runde 14: Regressionstests zu R688-02 bis -07.
/// </summary>
/// <remarks>
/// [ORIGINAL] Selbst nachgelesen (<c>tools/reverse-engineering/disasm.py gamedata/em.exe …</c>): Hauptweg Datei 0x55204–0x552D3
/// (Tagesabschluss, Takt) vor den Zügen Datei 0x54D68–0x54EF4 und dem Menü 0x44E22; 0x07169 Datei 0x0718F–0x07C07 für jeden Platz
/// 0 … [0x386] − 1; Sommerpause Datei 0x56476–0x56598; Tendenz Datei 0x07B64–0x07BFC.
/// </remarks>
public class TagestaktHauptwegReviewTests
{
    private static GameState TwoManagers(int seed)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "A", Head = 0, ClubIndex = 2 });
        setup.Managers.Add(new ManagerSetup { Name = "B", Head = 1, ClubIndex = 5 });
        return NewGame.Create(TestGames.CreateDatabase(), setup, seed);
    }

    private static void TopUp(GameState game)
    {
        foreach (var m in game.Managers)
            m.Money = Math.Max(m.Money, 100_000_000);
    }

    /// <summary>Alle Manager bestätigen, bis die Zeitleiste am nächsten Halt steht.</summary>
    private static void NextHalt(GameState game)
    {
        var day = game.CurrentDay;
        for (var guard = 0; guard < 10 && game.CurrentDay == day && game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
            game.Continue();
    }

    /// <summary>Takttage, deren Tageswechsel zwischen den Halten <paramref name="from"/> und <paramref name="to"/> lag.</summary>
    private static int Ticks(int from, int to) => Enumerable.Range(from + 1, Math.Max(0, to - from)).Count(GameState.IsDailyRunDay);

    private static void PlayToSeasonOver(GameState game)
    {
        for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
        {
            Assert.True(guard < 3000, "Saisonende nicht erreicht");
            TopUp(game);
            if (game.NationalTurn && game.NationalCoach!.Squad.Count == 0)
                game.AutoNominate();
            game.Continue();
        }
    }

    private static GameState Import(string file) =>
        GameState.ImportOriginal(OriginalSaveGame.Read(File.ReadAllBytes(OriginalImportTests.OriginalSave(file))),
            ClubDatabase.Load(TestGameData.Load()), 4711);

    /// <summary>Gastronomie + 1 beauftragen, Bauzeit des Angebots fest (gewürfelt wären es weniger; das Projekt soll nicht fertig werden).</summary>
    private static void Order(GameState game, Manager me, int weeks)
    {
        var level = game.Finance(me).Stadium.Level(StadiumFeature.Gastronomie);
        var quote = game.QuoteConstruction(me, StadiumProject.Feature, StadiumFeature.Gastronomie, level + 1);
        Assert.NotNull(quote);
        Assert.True(game.AnswerConstruction(me, quote with { Weeks = weeks }, accept: true));
    }

    // ---- R688-02: Verträge je Manager im Hauptweg --------------------------------------------------------------

    /// <summary>
    /// R688-02 (Hauptweg): Die Verhandlungssperre +0x18 (Datei 0x076E5–0x076F6) zählt in 0x07169 nur für die Kaderplätze des
    /// Managers am Zug (Datei 0x07402), also je Takt genau um 1 – bei zwei Managern nicht doppelt. Regressionstest für die Mutation
    /// „<c>AnnounceContractEnds(day)</c> ohne Manager“ (im Review nur am Abdruck <c>Mode4Reference</c> rot).
    /// </summary>
    [Fact]
    public void MainPath_TwoManagers_NegotiationLockCountsOncePerTick()
    {
        var game = TwoManagers(5);
        var players = game.Managers.SelectMany(m => game.Clubs[m.ClubIndex].Players).ToList();
        foreach (var p in players)
            p.NegotiationLock = 50;
        var excluded = new HashSet<Player>();
        while (game.CurrentDay < 45)
        {
            TopUp(game);
            NextHalt(game);
            excluded.UnionWith(players.Where(p => p.Notice != ContractNotice.None));
        }
        var ticks = Ticks(0, game.CurrentDay);
        Assert.InRange(ticks, 10, 14);
        var counted = players.Where(p => !excluded.Contains(p)).ToList();
        Assert.True(counted.Count > 30, $"nur {counted.Count} Spieler ohne Meldung");
        Assert.All(counted, p => Assert.Equal(50 - ticks, p.NegotiationLock));
    }

    // ---- R688-03: Sommerpause ----------------------------------------------------------------------------------

    /// <summary>
    /// R688-03: [ORIGINAL] Zähler der Sommerpause (Datei 0x564BC, Schleife 0x564D2–0x56593 bis zum 9. September): 233 … 365 für den
    /// 30. April bis 9. September, davon 19 mit Zähler ≡ 0 (mod 7).
    /// </summary>
    [Fact]
    public void SummerBreak_CountersFromSeasonEndToNinthOfSeptember()
    {
        var counters = GameState.SummerBreakCounters().ToList();
        Assert.Equal(233, counters[0]);
        Assert.Equal(365, counters[^1]);
        Assert.Equal(133, counters.Count);
        Assert.Equal(19, counters.Count(c => c % 7 == 0));
        Assert.Equal((4, 29), (SeasonCalendar.LayoutDate(SeasonCalendar.SeasonEnd).Month, SeasonCalendar.LayoutDate(SeasonCalendar.SeasonEnd).Day));
    }

    /// <summary>
    /// R688-03: Regressionstest (vorher rot: kein Stadionbau und keine Heilung zwischen Saisonende und neuer Saison). [ORIGINAL] Je
    /// Tag der Sommerpause mit Zähler ≡ 0 der Stadionbau 0x3D316 (Datei 0x5650D) und die Heilung 0x07C12 der Kaderplätze (Datei
    /// 0x5653C): 19 Wochen.
    /// </summary>
    [Fact]
    public void SummerBreak_AdvancesConstructionAndHealsSquad()
    {
        var game = TestGames.Create();
        var me = game.ActiveManager;
        PlayToSeasonOver(game);
        Assert.Equal(SeasonPhase.SeasonOver, game.Phase);
        TopUp(game);
        var stadium = game.Finance(me).Stadium;
        Order(game, me, 30);
        var weeks = stadium.WeeksLeft(StadiumProject.Feature, StadiumFeature.Gastronomie);
        Assert.True(weeks > 19, $"Bauzeit {weeks} Wochen");
        var injured = game.Clubs[me.ClubIndex].Players[^1];
        injured.InjuredGames = 25;

        game.StartNextSeason();

        Assert.Equal(weeks - 19, stadium.WeeksLeft(StadiumProject.Feature, StadiumFeature.Gastronomie));
        Assert.Equal(6, injured.InjuredGames);
    }

    /// <summary>
    /// R688-03/-04 am Original-Stand: FILE44 (14.04.1994, Zähler 217) → FILE44A (10.09.1994). Im Original vergehen bis zum ersten Tag
    /// der neuen Saison 21 Bauwochen: Takte 224 und 231 der Saison (der Takt 217 liegt vor dem Menü, in dem FILE44 gesichert ist)
    /// und 19 der Sommerpause; das Restaurant „ca. 23 Wochen“ steht dort bei 1 Woche. Regressionstest (vorher rot: 20 → 17, also
    /// drei Wochen).
    /// </summary>
    [GameDataFact]
    public void File44_GastronomyOrderedOnFourteenthOfApril_Loses21WeeksUntilNewSeason()
    {
        var game = Import("FILE44.HOK");
        Assert.Equal(216, game.CurrentDay);
        Assert.True(game.DayOpened);
        var me = game.Managers[0];
        TopUp(game);
        var stadium = game.Finance(me).Stadium;
        Order(game, me, 30);
        var weeks = stadium.WeeksLeft(StadiumProject.Feature, StadiumFeature.Gastronomie);
        Assert.True(weeks > 21, $"Bauzeit {weeks} Wochen");

        PlayToSeasonOver(game);
        game.StartNextSeason();

        Assert.Equal(weeks - 21, stadium.WeeksLeft(StadiumProject.Feature, StadiumFeature.Gastronomie));
    }

    // ---- R688-04: Takt vor dem Halt ----------------------------------------------------------------------------

    /// <summary>
    /// R688-04: Regressionstest (vorher rot: Takt und Tagesabschluss des Halttags erst nach dessen Menü). [ORIGINAL] Datei
    /// 0x55204–0x552D3 nach den Spielen des Vortags, vor dem Menü 0x44E22: Am Halt ist der Tageswechsel gerechnet (Training,
    /// Markt-Verfall, Tagesabschluss); geprüft auch an Halttagen, die selbst Takttage sind.
    /// </summary>
    [Fact]
    public void MainPath_TickOfHaltDay_RunsBeforeHalt()
    {
        var game = TestGames.Create(3);
        var tickHalts = 0;
        for (var guard = 0; game.CurrentDay < 100; guard++)
        {
            Assert.True(guard < 200, "Tag 100 nicht erreicht");
            TopUp(game);
            var listings = game.TransferMarket.Listings.Where(l => l.Seller is null).ToList();
            foreach (var l in listings)
                l.Player.Strain = 120;
            var players = game.TransferMarket.Listings.Select(l => l.Player).ToList();
            var from = game.CurrentDay;
            NextHalt(game);
            var halt = game.CurrentDay;
            Assert.True(game.DayOpened);
            Assert.Equal((game.Season, halt), game.TrainedUntil);
            Assert.Equal(halt, game.FinanceDay);
            if (!players.SequenceEqual(game.TransferMarket.Listings.Select(l => l.Player)))
                continue;
            Assert.All(listings, l => Assert.Equal(Math.Max(120 - 10 * Ticks(from, halt), 50), l.Player.Strain));
            if (GameState.IsDailyRunDay(halt))
                tickHalts++;
        }
        Assert.True(tickHalts >= 3, $"nur {tickHalts} Halte an Takttagen");
    }

    /// <summary>
    /// R688-04 am Original-Stand: FILE24 (Tag 14, Zähler 15) → FILE25 (Tag 17, Zähler 18). Im Menü von FILE25 ist der Markt schon
    /// einmal verfallen (Takt 18 vor dem Menü): je Marktspieler Belastung − 10, Liste gleich. Regressionstest (vorher rot: kein
    /// Verfall zwischen den beiden Menüs).
    /// </summary>
    [GameDataFact]
    public void File24_ToMenuOfDay17_MarketDecayedOnceLikeFile25()
    {
        var game = Import("FILE24.HOK");
        Assert.Equal(14, game.CurrentDay);
        var expected = Import("FILE25.HOK").TransferMarket.Listings.Where(l => l.Seller is null)
            .ToDictionary(l => l.Player.Name, l => l.Player.Strain);
        var before = game.TransferMarket.Listings.Where(l => l.Seller is null).ToDictionary(l => l.Player.Name, l => l.Player.Strain);
        Assert.Equal(expected.Keys.Order(), before.Keys.Order());
        Assert.All(before, e => Assert.Equal(e.Value > GameState.MarketDecayAbove ? e.Value - 10 : e.Value, expected[e.Key]));
        while (game.CurrentDay < 17)
        {
            TopUp(game);
            NextHalt(game);
        }
        Assert.Equal(17, game.CurrentDay);
        var now = game.TransferMarket.Listings.Where(l => l.Seller is null).ToDictionary(l => l.Player.Name, l => l.Player.Strain);
        foreach (var (name, strain) in now.Where(e => before.ContainsKey(e.Key)))
            Assert.Equal(expected[name], strain);
        Assert.Contains(now.Keys, before.ContainsKey);
    }

    // ---- R688-05: Liste über den Saisonwechsel -----------------------------------------------------------------

    /// <summary>
    /// R688-05: Regressionstest (vorher rot: der Saisonwechsel würfelte eine neue Liste). [ORIGINAL] Der Saisonwechsel ruft 0x494EF
    /// nicht (Aufrufer nur Datei 0x43A9F und 0x54E7C): Die Markt-Plätze bleiben, ihre Spieler altern um ein Jahr.
    /// </summary>
    [Fact]
    public void SeasonChange_KeepsMarketList_PlayersAgeOneYear()
    {
        var game = TestGames.Create(5);
        PlayToSeasonOver(game);
        var market = game.TransferMarket.Listings.Where(l => l.FromPool).Select(l => (l.Player, l.Player.Age, l.Price)).ToList();
        Assert.NotEmpty(market);

        game.StartNextSeason();

        var listed = game.TransferMarket.Listings.Where(l => l.FromPool).ToList();
        Assert.Equal(market.Select(m => m.Player), listed.Select(l => l.Player));
        Assert.Equal(market.Select(m => m.Age + 1), listed.Select(l => l.Player.Age));
        Assert.Equal(market.Select(m => m.Price), listed.Select(l => l.Price));
    }

    /// <summary>
    /// R688-05 am Original-Stand: FILE44 (14.04.1994) → FILE46 (10.09.1994). Liste gleich bis auf den eigenen HOHENADL, der in den
    /// Kader zurückkehrt; die WM-Schleife wechselt die Liste ebenfalls nicht.
    /// </summary>
    [GameDataFact]
    public void File44_SeasonChange_KeepsListLikeFile46()
    {
        var game = Import("FILE44.HOK");
        PlayToSeasonOver(game);
        game.StartNextSeason();
        // [APPROX] R204: Vertragslose aus Managervereinen (hier KJELLBERG, VALLIERE …) setzt das Remake zusätzlich auf die Liste;
        // verglichen werden die Markt-Plätze aus dem Pool.
        var expected = Import("FILE46.HOK").TransferMarket.Listings.Select(l => l.Player.Name).Order().ToList();
        Assert.Equal(expected, game.TransferMarket.Listings.Where(l => l.FromPool).Select(l => l.Player.Name).Order());
        Assert.DoesNotContain("HOHENADL", expected);
        Assert.Contains(game.Clubs[game.Managers[0].ClubIndex].Players, p => p.Name == "HOHENADL");
    }

    // ---- R688-06: entlassener Manager 0 ------------------------------------------------------------------------

    /// <summary>
    /// R688-06: Regressionstest (vorher rot: kein Markt-Verfall, wenn Manager 0 entlassen ist). [ORIGINAL] 0x07169 läuft für jeden
    /// Platz 0 … [0x386] − 1 (Datei 0x55298–0x552D3); der Markt-Teil hängt nur an DGROUP 0xA70E = 0 (Datei 0x0718F–0x07197).
    /// </summary>
    [Fact]
    public void MainPath_ManagerZeroDismissed_MarketStillDecaysPerTick()
    {
        var game = TwoManagers(3);
        game.Managers[0].Dismissed = true;
        game.StartTurns();
        var checkedSteps = 0;
        for (var guard = 0; game.CurrentDay < 80 && game.Phase == SeasonPhase.Running; guard++)
        {
            Assert.True(guard < 200, "Tag 80 nicht erreicht");
            TopUp(game);
            var listings = game.TransferMarket.Listings.Where(l => l.Seller is null).ToList();
            foreach (var l in listings)
                l.Player.Strain = 120;
            var players = game.TransferMarket.Listings.Select(l => l.Player).ToList();
            var from = game.CurrentDay;
            NextHalt(game);
            if (!players.SequenceEqual(game.TransferMarket.Listings.Select(l => l.Player)) || Ticks(from, game.CurrentDay) == 0)
                continue;
            Assert.All(listings, l => Assert.Equal(Math.Max(120 - 10 * Ticks(from, game.CurrentDay), 50), l.Player.Strain));
            checkedSteps++;
        }
        Assert.True(checkedSteps >= 3, $"nur {checkedSteps} Schritte mit Takt ohne Listenwechsel");
    }

    // ---- R688-07: Tendenz je Takt ------------------------------------------------------------------------------

    /// <summary>
    /// R688-07 (Inventur A134): Regressionstest (vorher rot: Tendenz ohne Änderung). [ORIGINAL] Datei 0x07BB1–0x07BC8: Das untere
    /// Halbbyte des Steuerbytes +0x14 zählt je Lauf von 0x07169 um eins herunter, unabhängig vom Wurf; der Wert bleibt in 30 … 68.
    /// Hier mit 13 Läufen Vorrat: nach n Takten 13 − n, Schritt und Richtung unverändert.
    /// </summary>
    [Fact]
    public void MainPath_TrendControlCountsDownPerTick()
    {
        var game = TestGames.Create(3);
        var club = game.Clubs[game.ActiveManager.ClubIndex];
        const int control = 0x80 | (1 << 4) | 13;
        foreach (var p in club.Players)
            p.SetTrendScore(50, control);
        while (Ticks(0, game.CurrentDay) < 8)
        {
            TopUp(game);
            NextHalt(game);
        }
        var ticks = Ticks(0, game.CurrentDay);
        Assert.InRange(ticks, 8, 12);
        Assert.All(club.Players, p =>
        {
            Assert.Equal(0x90 | (13 - ticks), p.TrendControl);
            Assert.InRange(p.TrendScore, 50, 50 + ticks);
        });
        Assert.Contains(club.Players, p => p.TrendScore > 50);
    }

    /// <summary>
    /// R688-07: Ablauf des Zählers. [ORIGINAL] Datei 0x07BCC–0x07BD4: Erreicht das Halbbyte 0, zieht 0x4907A Wert und Steuerbyte neu
    /// (35 … 65, 1 … 13); sonst Begrenzung 30 … 68 (0x4BD35). Plätze ohne Wert (0) bleiben [APPROX] unverändert.
    /// </summary>
    [Fact]
    public void AdvanceTrend_ClampsAndRerollsAtZero()
    {
        var random = new GameRandom(7);
        var p = new Player { Name = "T", Role = PlayerRole.Forward };
        for (var i = 0; i < 200; i++)
        {
            p.SetTrendScore(68, 0x80 | (3 << 4) | 5);
            GameState.AdvanceTrend(p, random);
            Assert.Equal(68, p.TrendScore);
            Assert.Equal(0xB4, p.TrendControl);
            p.SetTrendScore(31, (3 << 4) | 5);
            GameState.AdvanceTrend(p, random);
            Assert.InRange(p.TrendScore, 30, 31);
            p.SetTrendScore(40, (3 << 4) | 1);
            GameState.AdvanceTrend(p, random);
            Assert.InRange(p.TrendScore, 35, 65);
            Assert.InRange(p.TrendControl & 0x0F, 1, 13);
        }
        var unknown = new Player { Name = "U", Role = PlayerRole.Forward };
        GameState.AdvanceTrend(unknown, random);
        Assert.Equal((0, 0), (unknown.TrendScore, unknown.TrendControl));
    }
}
