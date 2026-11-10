using System.Reflection;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur tagesablauf (Runde 15, #699 Punkte 1–3): Tag 0 mit Tagesabschluss vor dem ersten Menü, Listenwechsel-Wurf je Managerzug
/// und Neuaufstellung des Bundestrainers am Tag mit Ereignis vor den Menüs.
/// </summary>
/// <remarks>
/// [ORIGINAL] Selbst nachgelesen (<c>tools/reverse-engineering/disasm.py gamedata/em.exe 0x54D38 0x54EF8</c>, <c>… 0x55073 0x55090</c>,
/// <c>… 0x551F0 0x55240</c>): Der Hauptweg liest das Tagesbyte über DGROUP 0xA969 (Datei 0x54D41); Spiel- und Saisonstart lassen
/// Zähler und Index auf 0 (Datei 0x43A93, 0x565A7), das Tagesbyte 0 springt bei Datei 0x54D5C nach 0x55073 und weiter zum
/// Tageswechsel Datei 0x55204 (Zähler 1 = 10. September, je Manager 0x3ED01/0x3F833 bei Datei 0x5522B/0x55230), erst dann folgen
/// die Neuaufstellungen (Datei 0x54D68–0x54DBA, je Satz 0x426BC und 0x952C(Satz, 0)) und je Manager der Wurf (Datei 0x54E65–0x54E7C)
/// vor dessen Menü (Datei 0x54EB2), Schleife Datei 0x54E3F–0x54EF4.
/// </remarks>
public sealed class TagesablaufRunde15Tests
{
    private static GameState TwoManagers(int seed)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "A", Head = 0, ClubIndex = 2 });
        setup.Managers.Add(new ManagerSetup { Name = "B", Head = 1, ClubIndex = 5 });
        return NewGame.Create(TestGames.CreateDatabase(), setup, seed);
    }

    private static GameState Reload(GameState game) => SaveGame.Load(SaveGame.Save(game)).Game!;

    /// <summary>
    /// Teil 1: Ein neues Spiel steht im ersten Menü schon nach dem Tageswechsel zu Tag 0 (Zähler 1, FILE01: 0x3B7 = 1); der
    /// Finanztag ist 0. Vorher rot: <see cref="GameState.DayOpened"/> = false, Finanztag −1 (Tageswechsel erst nach dem Menü).
    /// </summary>
    [Theory]
    [InlineData(3)]
    [InlineData(17)]
    public void NewGame_OpensDayZero_BeforeFirstMenu(int seed)
    {
        var game = TestGames.Create(seed);
        Assert.Equal(0, game.CurrentDay);
        Assert.True(game.DayOpened);
        Assert.Equal(0, game.FinanceDay);
    }

    /// <summary>
    /// Teil 1: Der Tagesabschluss 0x3ED01 rechnet auch für Tag 0 (10. September) das Zinskonto: (1 % Sparbuch) · 3 / (30 + 1)
    /// Drittel-DM (Datei 0x3ED4F–0x3EE6B). Vorher rot: an Finanztag 0 kein Tagesabschluss (Zinskonto 0).
    /// </summary>
    [Fact]
    public void DayZero_DayClose_AccruesInterest()
    {
        var game = TestGames.Create(3);
        var manager = game.Managers[0];
        var finance = game.Finance(manager);
        game.FinanceDay = -1;
        finance.Bank.Savings = 3_100_000;
        finance.InterestAccrualThirds = 0;
        finance.ManagerLoanAccrualThirds = 0;

        game.AdvanceFinanceTo(0);

        Assert.Equal(3_100_000 / 100 * FinanceRules.ThirdsPerMark / 31, finance.InterestAccrualThirds);
    }

    /// <summary>
    /// Teil 1, Saisonwechsel: Nach <see cref="GameState.StartNextSeason"/> ist Tag 0 der neuen Saison geöffnet (Datei 0x565A7 setzt
    /// den Zähler auf 0, der Hauptweg wechselt vor dem ersten Menü auf Zähler 1). Das Zinskonto enthält damit den 1. bis
    /// 10. September (Sommerpause 1.–9., Tag 0 = 10.). Vorher rot: neun Tage, Tag 0 nicht geöffnet.
    /// </summary>
    [Fact]
    public void SeasonStart_OpensDayZero_InterestIncludesTenthOfSeptember()
    {
        var game = TestGames.Create(5);
        TestSeason.PlayRegularSeason(game);
        while (game.Phase is not (SeasonPhase.SeasonOver or SeasonPhase.GameOver))
            game.Continue();
        Assert.Equal(SeasonPhase.SeasonOver, game.Phase);
        var manager = game.Managers[0];
        var finance = game.Finance(manager);
        finance.Bank.Savings = 3_000_000;
        finance.Bank.Credit = 0;
        game.ManagerLoans.Clear();
        manager.Money = 50_000_000;

        game.StartNextSeason();

        Assert.True(game.DayOpened);
        Assert.Equal(0, game.FinanceDay);
        Assert.Equal(10 * (3_000_000 / 100 * FinanceRules.ThirdsPerMark / 31), finance.InterestAccrualThirds);
    }

    /// <summary>
    /// Mehrsaisonprobe Teil 1 (AGENTS.md §5: 4 Saisonwechsel, passiv und aktiv, zwei Manager): Nach jedem Saisonwechsel steht das
    /// erste Menü nach dem Tageswechsel zu Tag 0 (Zähler 1, Finanztag 0, Werbebewertung des Tagesabschlusses 0x3F833); Speichern
    /// und Laden im ersten Menü sind bytegleich und rechnen Tag 0 kein zweites Mal; der erste „Tag weiter“ spielt Tag 0 und
    /// öffnet den nächsten Tag. Vorher rot: Tag 0 nach dem Saisonwechsel nicht geöffnet, Finanztag −1.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FourSeasonChanges_DayZeroOpenedBeforeFirstMenu(bool active)
    {
        var game = TwoManagers(7);
        for (var change = 0; change < 4; change++)
        {
            for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
            {
                Assert.True(guard < 2000, "Saisonende nicht erreicht");
                foreach (var m in game.Managers)
                {
                    m.Money = Math.Max(m.Money, 100_000_000);
                    if (active)
                        TestSeason.KeepSquad(game, m, extendExpired: true);
                }
                game.Continue();
            }
            Assert.Equal(SeasonPhase.SeasonOver, game.Phase);
            game = Reload(game);

            game.StartNextSeason();

            Assert.Equal(SeasonPhase.Running, game.Phase);
            Assert.Equal(0, game.CurrentDay);
            Assert.True(game.DayOpened);
            Assert.Equal(0, game.FinanceDay);
            foreach (var m in game.Managers)
                Assert.Equal(SponsorRating.Compute(game, m), game.Finance(m).SponsorRating);
            var saved = SaveGame.Save(game);
            var loaded = Reload(game);
            Assert.True(loaded.DayOpened);
            Assert.Equal(0, loaded.FinanceDay);
            Assert.Equal(saved, SaveGame.Save(loaded));
            Assert.Empty(loaded.ValidatePlayerIdentity());
            game = loaded;

            for (var guard = 0; game.CurrentDay == 0; guard++) // Hot-Seat: erst Weitergeben an Manager 1, dann Tag 0 spielen
            {
                Assert.True(guard < 4);
                game.Continue();
            }
            Assert.True(game.DayOpened);
            Assert.Equal(game.CurrentDay, game.FinanceDay);
        }
    }

    /// <summary>
    /// Teil 2: Im Hot-Seat würfelt das Original den Listenwechsel von Manager 1 erst nach dem Menü von Manager 0 (Datei
    /// 0x54E3F–0x54EF4: Wurf 0x54E65–0x54E7C, dann Menü 0x54EB2, dann der nächste Manager). Das Weitergeben des Zugs zieht also
    /// genau einen Wurf aus der Hauptquelle. Prüfung: Stand im Menü von Manager 0 sichern, Zug weitergeben; der geladene Stand mit
    /// einem Wurf von Hand muss dieselbe Hauptquelle und dieselbe Transferliste haben. Vorher rot: alle Würfe vor dem ersten Menü,
    /// das Weitergeben zieht nichts.
    /// </summary>
    [Theory]
    [InlineData(3)]
    [InlineData(11)]
    [InlineData(29)]
    public void HotSeat_ListChangeRollOfSecondManager_AfterFirstMenu(int seed)
    {
        var game = TwoManagers(seed);
        for (var guard = 0; guard < 40 && !(game.IsListChangeDay(game.CurrentDay) && game.CurrentManager == 0); guard++)
            game.Continue();
        Assert.True(game.IsListChangeDay(game.CurrentDay));
        Assert.Equal(0, game.CurrentManager);
        var probe = Reload(game);

        Assert.Empty(game.Continue()); // Zug an Manager 1
        Assert.Equal(1, game.CurrentManager);
        probe.TransferMarket.ListChangeTurn(probe);

        Assert.Equal(probe.Random.Next(), game.Random.Next());
        Assert.Equal(probe.TransferMarket.Listings.Select(l => l.Player.Id), game.TransferMarket.Listings.Select(l => l.Player.Id));
    }

    /// <summary>
    /// Teil 2, Rundreise: Die Verteilung der Würfe auf die Managerzüge braucht kein neues Spielstandsfeld (sie folgt aus
    /// <see cref="GameState.CurrentManager"/> und dem Tag). Ein Lauf, der vor jedem Zug sichert und lädt, gleicht dem Lauf ohne
    /// Unterbrechung (Tag, Manager am Zug, Hauptquelle).
    /// </summary>
    [Fact]
    public void HotSeat_DayAfterPass_SameAsUninterruptedRun()
    {
        var a = TwoManagers(7);
        var b = Reload(a);
        for (var i = 0; i < 6; i++)
        {
            a.Continue();
            b = Reload(b);
            b.Continue();
        }
        Assert.Equal(a.CurrentDay, b.CurrentDay);
        Assert.Equal(a.CurrentManager, b.CurrentManager);
        Assert.Equal(a.Random.Next(), b.Random.Next());
    }

    /// <summary>
    /// Teil 3: Am Tag mit Ereignis stellt der Tageswechsel den Bundestrainer (Satz 5) vor den Menüs mit der gesicherten
    /// Einstellung neu auf (Datei 0x54D68–0x54DBA: 0x426BC bei 0x54D9E, 0x952C(5, 0) bei 0x54DAA), nach den Menüs nicht mehr
    /// (Datei 0x54F9C–0x54FBF: nur 0x952C(5, 1)). Vorher rot: Neuaufstellung erst nach den Menüs (<c>OnDayNationalTeam</c>).
    /// </summary>
    [Fact]
    public void NationalCoach_EventDayLineup_BeforeMenus()
    {
        var game = TestGames.Create(3);
        var coach = game.AppointNationalCoach(game.Managers[0]);
        game.AutoNominate();
        Assert.True(coach.AutoLineupEnabled);
        var eventDay = Enumerable.Range(1, 100).First(game.IsListChangeDay);

        void Step(string name, bool restores)
        {
            coach.AutoFormation();
            Assert.NotNull(coach.Formation[LineupSlot.Goalkeeper]);
            coach.Formation.Clear(LineupSlot.Goalkeeper); // Störung ohne Abschalten der Einstellung
            var method = typeof(GameState).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!;
            method.Invoke(game, method.GetParameters().Length == 1 ? [eventDay] : []);
            if (restores)
                Assert.NotNull(coach.Formation[LineupSlot.Goalkeeper]);
            else
                Assert.Null(coach.Formation[LineupSlot.Goalkeeper]);
        }

        Step("MainPathEventDayLineups", restores: true);
        Step("OnDayNationalTeam", restores: false);
    }

    /// <summary>
    /// Abnahme Teil 1 mit Originaldaten: Neustarts im ersten Menü (FILE01, FILE10, FILE27: Zähler 0x3B7 = 1) und der erste Tag einer
    /// Folgesaison (FILE14) importieren mit geöffnetem Tag 0 und Finanztag 0; die Werbebewertung des HOK (Managersatz +0x173) ist
    /// die des Tagesabschlusses 0x3F833 zu Tag 0 (Datei 0x55230).
    /// </summary>
    [GameDataTheory]
    [InlineData("FILE01.HOK")]
    [InlineData("FILE10.HOK")]
    [InlineData("FILE27.HOK")]
    [InlineData("FILE14.HOK")]
    public void Original_DayZero_OpenedWithDayClose(string file)
    {
        var save = OriginalSaveGame.Read(File.ReadAllBytes(OriginalImportTests.OriginalSave(file)));
        Assert.Equal(1, BitConverter.ToUInt16(save.Block("03B7"), 0));
        var game = GameState.ImportOriginal(save, ClubDatabase.Load(TestGameData.Load()), 4711);
        Assert.Equal(0, game.CurrentDay);
        Assert.True(game.DayOpened);
        Assert.Equal(0, game.FinanceDay);
        foreach (var manager in game.Managers)
            Assert.Equal(game.Finance(manager).SponsorRating, SponsorRating.Compute(game, manager));
    }

    /// <summary>
    /// Abnahme Teil 1, FILE02 → FILE03 (drei Spieltage bis 20.09.1993, Konto 500.000 → 737.354 DM): Im Original ist die Änderung
    /// reiner Eintritt (237.354 DM = 13.962 Zuschauer zu 17 DM; bis zum Monatsletzten bucht der Tagesabschluss kein Zinskonto,
    /// HOK 0x02A0 = 0). Das Remake bucht auf demselben Weg nur Eintritt und hält das Zinskonto auf 0. [APPROX] Die Zuschauerzahl
    /// hängt vom Zufall ab (FinanceRandom, R15-D2: kein bitgenauer Modus), deshalb kein exakter Kontovergleich.
    /// </summary>
    [GameDataFact]
    public void Original_File02ToFile03_OnlyGateReceipts()
    {
        var database = ClubDatabase.Load(TestGameData.Load());
        var file02 = OriginalSaveGame.Read(File.ReadAllBytes(OriginalImportTests.OriginalSave("FILE02.HOK")));
        var file03 = OriginalSaveGame.Read(File.ReadAllBytes(OriginalImportTests.OriginalSave("FILE03.HOK")));
        long Money(OriginalSaveGame save) => BitConverter.ToInt32(save.Block("965B"), 0x18F) / 3;
        Assert.Equal(500_000, Money(file02));
        Assert.Equal(737_354, Money(file03));
        Assert.Equal(0, (737_354 - 500_000) % 17);
        Assert.Equal(0, BitConverter.ToInt32(file03.Block("02A0"), 0));

        var game = GameState.ImportOriginal(file02, database, 4711);
        var manager = game.Managers[0];
        var target = OriginalImport.Day(file03);
        for (var guard = 0; guard < 20 && game.CurrentDay < target; guard++)
            game.Continue();
        Assert.Equal(target, game.CurrentDay);
        var book = game.Finance(manager).Book;
        Assert.Equal(manager.Money - 500_000, book.Current(FinanceCategory.Eintritt));
        Assert.Equal(book.Current(FinanceCategory.Eintritt), book.CurrentTotal);
        Assert.Equal(0, game.Finance(manager).InterestAccrualThirds);
    }
}
