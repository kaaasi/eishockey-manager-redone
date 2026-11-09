using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Nachprüfung #691 (<c>docs/reviews/2026-11-09-tagestakt-hauptweg-sommerpause.md</c>), Leitstelle Runde 14: Regressionstests zu
/// R688S-01 bis R688S-03.
/// </summary>
/// <remarks>
/// [ORIGINAL] Selbst nachgelesen (<c>tools/reverse-engineering/disasm.py gamedata/em.exe …</c>): WM-Schleife Datei 0x55570–0x55740
/// (Tagesabschluss zum Zähler 232 = 29. April, dann Markierung 255 → Saisonwechsel 0x55743), Saisonende 0x26F20 setzt den Zähler
/// 0x3B7 auf 233 (Datei 0x26F55), Sommerschleife Datei 0x564BC–0x56593 ab Zähler 234 = 1. Mai; Leihe Kaderplatz +0x0C = Verein + 0x80
/// (Datei 0x47920–0x47925); alle Leser des Kontos (DGROUP 0x97EA + 0x1AF · Manager): Vergleiche nur in „Verein“ (0x2E20C), Stadionaufbau
/// (0x3E11A), Stadionauftrag (0x3EADB), Sollzinsen (0x3EE05), Kauf (0x42A8F), Zwangsabstieg (0x55F22–0x55F34), Börse
/// (0x596F2/0x597A6/0x598EA) und Kontogrenze ±500.000.000 Drittel im Monatsabschluss (0x3F10E–0x3F165; R688S2-01) – keine
/// Entlassung wegen Schulden.
/// </remarks>
public class TagestaktSommerpauseReviewTests
{
    private static GameState Import(string file) =>
        GameState.ImportOriginal(OriginalSaveGame.Read(File.ReadAllBytes(OriginalImportTests.OriginalSave(file))),
            ClubDatabase.Load(TestGameData.Load()), 4711);

    private static void PlayToSeasonOver(GameState game)
    {
        for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
        {
            Assert.True(guard < 3000, "Saisonende nicht erreicht");
            if (game.NationalTurn && game.NationalCoach!.Squad.Count == 0)
                game.AutoNominate();
            game.Continue();
        }
    }

    // ---- R688S-01: Sommerpause ab dem 1. Mai, kein Tagesabschluss für den 30. April ----------------------------------

    /// <summary>
    /// R688S-01: Regressionstest (vorher rot: Zähler 233 = 30. April als erster Tag der Sommerpause, fünf Monatsabschlüsse).
    /// [ORIGINAL] Datei 0x26F55 setzt 0x3B7 = 233, Datei 0x564BC zählt auf 234 (1. Mai), die Schleife endet nach dem 9. September
    /// (Datei 0x5657A–0x56593): 132 Tage, vier Monatsabschlüsse (Ende Mai bis August), 19 Bauwochen (Zähler ≡ 0 mod 7).
    /// </summary>
    [Fact]
    public void SummerBreakCounters_FirstOfMayToNinthOfSeptember_FourMonthEnds()
    {
        var counters = GameState.SummerBreakCounters().ToList();
        Assert.Equal(233, GameState.SeasonEndCounter);
        Assert.Equal(234, counters[0]);
        Assert.Equal(365, counters[^1]);
        Assert.Equal(132, counters.Count);
        var dates = counters.Select(c => SeasonCalendar.LayoutDate(c - 1)).ToList();
        Assert.Equal((5, 1), (dates[0].Month, dates[0].Day));
        Assert.Equal((9, 9), (dates[^1].Month, dates[^1].Day));
        Assert.Equal(4, dates.Count(d => d.Day == DateTime.DaysInMonth(d.Year, d.Month)));
        Assert.Equal(19, counters.Count(c => c % 7 == 0));
    }

    /// <summary>
    /// R688S-01 am Original-Stand: FILE44 (14.04.1994, 18.027.183 DM) bis zum Bildschirm „Saisonende“ ändert sich das Konto nur um die
    /// Prämie (10. Platz, 100.000 DM): kein April-Monatsabschluss, weder vor dem Kaderwechsel noch danach (vorher rot: der 30. April
    /// war der erste Tag der Sommerpause und buchte den April mit dem neuen Kader). Die Bücher der neuen Saison zeigen genau vier
    /// Gehaltsabschlüsse des Sommerkaders.
    /// <para>
    /// Zum DOSBox-Bild <c>file46_2</c> (17.523.876 DM, also 603.307 DM darunter): FILE44 ist im Menü des Markierungstags gesichert
    /// (Datei 0x55350, nach dem Tageswechsel 0x55204); nach dem Laden läuft dieser Tageswechsel im Original erneut (Zähler 218 =
    /// 15. April, Bild <c>file46_1</c>), die WM-Schleife steht einen Tag später und schließt zuletzt den 30. April ab – mit dem
    /// April-Abschluss des 25er-Kaders. Diese Eigenheit des Ladens spielt der Import nicht nach ([APPROX], <c>RunSummerBreak</c>);
    /// die Größenordnung des Unterschieds ist der Monatsabschluss des FILE44-Kaders (Gegenprobe unten).
    /// </para>
    /// </summary>
    [GameDataFact]
    public void File44_SeasonEndScreen_AccountIsFile44PlusPremium_NoAprilMonthEnd()
    {
        var game = Import("FILE44.HOK");
        var me = game.Managers[0];
        var club = game.Clubs[me.ClubIndex];
        Assert.Equal(18_027_183, me.Money);
        var aprilWages = FinanceRules.MonthlyWages(game, club); // 25 Spieler, mit KJELLBERG und VALLIERE
        var oldBook = game.Finance(me).Book;
        var wagesBefore = oldBook.Current(FinanceCategory.Gehaelter);

        PlayToSeasonOver(game);

        Assert.Equal(SeasonPhase.SeasonOver, game.Phase);
        Assert.Equal(100_000, oldBook.Current(FinanceCategory.Praemien));
        Assert.Equal(wagesBefore, oldBook.Current(FinanceCategory.Gehaelter)); // kein Gehaltsabschluss zwischen 14. und 29. April
        Assert.Equal(18_127_183, me.Money);
        // Gegenprobe zur Abweichung von file46_2: ein Monatsabschluss des 25er-Kaders (Gehälter, Unterhalt, Grundeinnahmen) liegt in
        // der Größenordnung der 603.307 DM, die das DOSBox-Bild tiefer steht ([APPROX] Rest: Posten ohne Kategoriesummen im HOK).
        var aprilUpkeep = FinanceRules.ToMarks(FinanceRules.MonthlyBaseExpenseThirds(me.Level, game.Finance(me).CostLeague))
                          + game.Finance(me).Stadium.MonthlyUpkeep;
        Assert.InRange(aprilWages + aprilUpkeep, 603_307 * 7 / 10, 603_307);

        game.StartNextSeason();

        var summerWages = FinanceRules.MonthlyWages(game, club); // 24 Spieler, ohne KJELLBERG und VALLIERE
        Assert.True(summerWages < aprilWages);
        Assert.Equal(-4 * summerWages, game.Finance(me).Book.Current(FinanceCategory.Gehaelter));
    }

    // ---- R688S-02: Leihe aus dem HOK-Import ------------------------------------------------------------------------------

    /// <summary>
    /// R688S-02 (A115) am Original-Stand: FILE44 VALLIERE ist geliehen (Kaderplatz +0x0C = 0xB9: Vereinssatz 57 des Spielstands, ein
    /// Land; Pool +0x1A = 4).
    /// Regressionstest (vorher rot: <c>OnLoan</c> false, VALLIERE ging am Saisonende über <c>Leave</c> gegen 130.500 DM Ablöse).
    /// [ORIGINAL] FILE46: VALLIERE weder im Kader noch im Markt (Pool +0x1A = 6, Datei 0x275FF), nur die Ablöse für KJELLBERG
    /// (<c>file46_5</c>: 155.500 DM).
    /// </summary>
    [GameDataFact]
    public void File44_Valliere_ImportedAsLoan_EndsFreeWithoutCompensation()
    {
        var save = OriginalSaveGame.Read(File.ReadAllBytes(OriginalImportTests.OriginalSave("FILE44.HOK")));
        var database = ClubDatabase.Load(TestGameData.Load());
        var game = GameState.ImportOriginal(save, database, 4711);
        var me = game.Managers[0];
        var club = game.Clubs[me.ClubIndex];
        var valliere = Assert.Single(club.Players, p => p.Name == "VALLIERE");
        Assert.True(valliere.OnLoan);
        Assert.NotNull(valliere.Loan);
        var lender = game.Clubs[OriginalImport.ClubIndices(save, database)[0xB9 & 0x7F]];
        Assert.Same(lender, valliere.Loan!.Origin);
        Assert.Equal(OriginalImport.ClubName(save.Block("far:9657"), 57), lender.Name);
        Assert.True(lender.Index >= NewGame.SelectableClubs, $"Verleiher {lender.Name} ist kein Land"); // Pool +0x1A = 4: vom Markt
        Assert.Equal(1, valliere.ContractYears);
        Assert.Single(club.Players, p => p.OnLoan); // nur VALLIERE
        Assert.All(game.Managers.Select(m => game.Clubs[m.ClubIndex]).SelectMany(c => c.Players), p => Assert.Equal(p.OnLoan, p.Loan is not null));

        PlayToSeasonOver(game);
        game.StartNextSeason();

        Assert.DoesNotContain(game.Clubs, c => c.Players.Contains(valliere));
        Assert.Contains(valliere, game.TransferMarket.FreeAgents);
        Assert.False(valliere.OnLoan);
        Assert.Null(valliere.Loan);
        // Ablöse nur für KJELLBERG (Wert(Preis) / 2, Datei 0x27DE3; Original 155.500 DM, Remake mit eigenem Zufall ≈ 158.000 DM).
        Assert.InRange(game.Finance(me).Book.Current(FinanceCategory.Transfers), 120_000, 200_000);
        Assert.DoesNotContain(club.Players, p => p.Name == "KJELLBERG");
    }

    // ---- R688S-03: keine Entlassung wegen Schulden, Zwangsabstieg wie im Original ----------------------------------------

    /// <summary>
    /// R688S-03: Regressionstest (vorher rot: Entlassung nach acht Wochen unter −1,5 Mio. DM). [ORIGINAL] em.exe kennt keine
    /// Entlassung wegen Schulden (siehe Klassenkommentar); das Konto ist im Monatsabschluss auf ±500.000.000 Drittel begrenzt
    /// (Datei 0x3F10E–0x3F165, <see cref="FinanceRules.BalanceLimit"/>), die Folge von Schulden ist der Zwangsabstieg am 29. April
    /// (Konto &lt; −2.000.000 Drittel, Datei 0x55F22–0x55F87). Mehrsaisonprobe: passiver Bundesliga-Manager ohne Werbung und ohne
    /// Pflege über vier Saisonwechsel – nie entlassen, Spiel läuft weiter, Zwangsabstieg tritt ein.
    /// </summary>
    [Theory]
    [InlineData(5)]
    [InlineData(41)]
    public void PassiveBundesligaManager_NeverDismissed_ForcedRelegationInstead(int seed)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Ganz passiv", ClubIndex = 4 });
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, seed);
        var manager = game.ActiveManager;
        long lowest = manager.Money;
        for (var year = 0; year < 4; year++)
        {
            for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
            {
                Assert.True(guard < 1000, "Saisonende nicht erreicht");
                game.Continue();
                Assert.False(manager.Dismissed, $"Saison {year + 1}, Tag {game.CurrentDay}: entlassen mit {manager.Money} DM");
                lowest = Math.Min(lowest, manager.Money);
            }
            Assert.Equal(SeasonPhase.SeasonOver, game.Phase);
            game.StartNextSeason();
        }
        Assert.Equal(4, game.History.Count);
        Assert.False(manager.Dismissed);
        Assert.True(lowest < -1_500_000, $"Probe ohne Schulden: tiefster Stand {lowest} DM");
        Assert.True(lowest >= -FinanceRules.BalanceLimit, $"Konto unter der Grenze des Originals: {lowest}");
        Assert.Contains(game.History, r => r.Managers.Any(m => m.Manager == 0 && m.Events.Contains(SeasonEvent.ForcedRelegation)));
    }

    /// <summary>
    /// R688S-03: Konto tief im Minus über acht Wochen hinaus – keine Entlassung, kein Spielende (vorher rot nach 56 Tagen).
    /// Einnahmen eines Heimspiels dürfen die Schulden nicht abbauen, deshalb wird das Konto je Halt festgehalten.
    /// </summary>
    [Fact]
    public void DeepDebts_NoDismissal_GameContinues()
    {
        var game = TestGames.Create();
        var me = game.ActiveManager;
        for (var guard = 0; game.Phase == SeasonPhase.Running && game.CurrentDay < 120; guard++)
        {
            Assert.True(guard < 500);
            me.Money = -2_000_000;
            var steps = game.Continue();
            Assert.Empty(steps.OfType<DismissalStep>());
        }
        Assert.True(game.CurrentDay >= 120);
        Assert.False(me.Dismissed);
        Assert.False(game.IsGameOver);
        Assert.Equal(SeasonPhase.Running, game.Phase);
    }
}
