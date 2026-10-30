using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur finanzen-nachpruefung (Runde 5): unabhängige Nachprüfung der Finanzformeln gegen em.exe und das Karriere-Video.
/// Belege und Ergebnistabelle: docs/finanzen-rekonstruktion.md, Abschnitt „Unabhängige Nachprüfung“.
/// </summary>
public class FinanceNachpruefungTests
{
    private static (GameState Game, Manager Me, Club Club) Game(int level, League league, GameDuration duration = GameDuration.Unbegrenzt)
    {
        var setup = new GameSetup { Duration = duration };
        var clubIndex = 12 * (int)league;
        setup.Managers.Add(new ManagerSetup { Name = "Test", Head = 0, ClubIndex = clubIndex, Level = level, StartLeague = league });
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, 3);
        var me = game.ActiveManager;
        me.Training.Fill(TrainingKind.Free);
        return (game, me, game.Clubs[me.ClubIndex]);
    }

    private static void FixSalaries(Club club, int[] salaries)
    {
        while (club.Players.Count > salaries.Length)
            club.Players.RemoveAt(club.Players.Count - 1);
        Assert.Equal(salaries.Length, club.Players.Count);
        for (var i = 0; i < salaries.Length; i++)
        {
            club.Players[i].MonthlySalary = salaries[i];
            club.Players[i].Insured = false;
        }
    }

    /// <summary>
    /// Karriere-Video ESC Wolfsburg (Oberliga, Level 3, Spielbeginn): vor der Werbeseite (0:05:42, „Ausgaben 87.370 DM“)
    /// werden alle 20 Verträge auf 4 Jahre neu verhandelt (0:02:24–0:03:52). Neue Gehälter laut Kaderliste 0:03:52.
    /// 90.200 (Sockel) + 69.600 (Startstadion) + 3 × 34.104 = 262.112 Drittel = 87.370 DM. Die frühere Restlücke
    /// (73.977 DM) rechnete mit den Gehältern vor der Verhandlung (20.710 DM).
    /// </summary>
    private static readonly int[] WolfsburgRenegotiated =
        [2610, 2610, 1566, 1566, 1740, 1392, 1740, 1740, 1566, 1566, 1392, 1566, 1566, 1566, 1392, 1914, 1914, 1566, 1392, 1740];

    [Fact]
    public void MonthlyExpenses_Wolfsburg_AfterRenegotiation_MatchCareerVideo()
    {
        var (game, me, club) = Game(level: 3, League.Oberliga);
        FixSalaries(club, WolfsburgRenegotiated);
        Assert.Equal(34_104, FinanceRules.MonthlyWages(club));
        Assert.Equal(0, me.Training.WeeklyCost);
        Assert.Equal(87_370, game.MonthlyExpenses(me));
    }

    /// <summary>
    /// em.exe 0x3E86B–0x3E8B5 (Ausgaben mit Flag 1): Managerfeld 0x97EE (Kosten je Trainingstag X, Drittel) wird mit
    /// 0:0x52A um 3 nach LINKS geschoben und X/2 addiert: 8·X + X/2 = 4,25 Wochen à 2·X. Vorher: W/8 + W/2.
    /// </summary>
    [Fact]
    public void MonthlyExpenses_TrainingShare_IsEightAndAHalfTrainingDays()
    {
        var (game, me, _) = Game(level: 3, League.Oberliga);
        var without = game.MonthlyExpenses(me);
        me.Training[0, 0] = TrainingKind.Special;
        var raw = me.Training.RawCostPerTrainingDay;
        Assert.True(raw > 0);
        Assert.Equal(without + (8 * raw + raw / 2) / 3, game.MonthlyExpenses(me));
    }

    /// <summary>
    /// em.exe 0x29FB3–0x29FCD: Ist das Spielende 1994 ([0x5A14] = 0x7CA, Dauer „eine Saison“, 0x2986E ff.), bekommt das
    /// Startstadion 1.200 Sitz- und 2.500 Stehplätze dazu.
    /// </summary>
    [Theory]
    [InlineData(GameDuration.EineSaison, 3_000 + 1_200, 4_500 + 2_500)]
    [InlineData(GameDuration.DreiSaisons, 3_000, 4_500)]
    [InlineData(GameDuration.Unbegrenzt, 3_000, 4_500)]
    public void StartStadium_OneSeasonGame_HasExtraPlaces(GameDuration duration, int seats, int standing)
    {
        var (game, me, _) = Game(level: 3, League.Bundesliga, duration);
        var stadium = game.Finance(me).Stadium;
        Assert.Equal((seats, standing), (stadium.Seats, stadium.Standing));
    }

    /// <summary>
    /// em.exe 0x3F10E–0x3F165 (Monatsabschluss nach Einnahmen, Ausgaben und Zinsen): Kontostand auf
    /// ±500.000.000 Drittel begrenzt.
    /// </summary>
    [Theory]
    [InlineData(400_000_000L, 166_666_666L)]
    [InlineData(-400_000_000L, -166_666_666L)]
    public void MonthEnd_ClampsBalance_ToFiveHundredMillionThirds(long start, long expected)
    {
        var (game, me, _) = Game(level: 3, League.Oberliga);
        me.Money = start;
        var book = game.Finance(me).Book;
        for (var i = 0; i < 60 && book.Current(FinanceCategory.Unterhalt) == 0; i++)
            game.Continue();
        Assert.NotEqual(0, book.Current(FinanceCategory.Unterhalt));
        Assert.Equal(expected, me.Money);
    }

    /// <summary>
    /// em.exe 0x3F63C–0x3F65B: Fällt die Zuschauerzahl unter random(150, 200), wird sie nur bei TEUREN Karten
    /// (16 + 3·(2 − Heimliga) &lt; Preis·3/2) durch random(2, 5) geteilt, sonst bis random(200, 300) aufgefüllt.
    /// Oberliga-Startstadion, Level-Abstand 2; Spannen aus der Nachbildung der Routine.
    /// </summary>
    [Theory]
    [InlineData(30, 40, 5, 50, 100)]
    [InlineData(10, 0, 11, 188, 330)]
    public void Spectators_SmallCrowd_DividedOnlyForExpensiveTickets(int price, int strength, int rank, int min, int max)
    {
        var stadium = Stadium.ForLeague(League.Oberliga);
        stadium.TicketPrice = price;
        var random = new Random(7);
        for (var i = 0; i < 300; i++)
        {
            var z = SpectatorRules.Compute(stadium, strength, strength, rank, rank, 2, 2, 0, 2, random);
            Assert.InRange(z, min, max);
        }
    }

    /// <summary>em.exe 0x3DDB8–0x3DDC5: Ein neuer Eintrittspreis wird nur bei 9 &lt; Preis &lt; 31 übernommen.</summary>
    [Fact]
    public void TicketPrice_RangeIsTenToThirty()
    {
        Assert.Equal((10, 30), (Stadium.MinTicketPrice, Stadium.MaxTicketPrice));
    }

    /// <summary>
    /// Prämien am Saisonende. em.exe 0x55CD3–0x55D02 vergleicht mit DGROUP 0x387 = DEB-Pokalsieger (Titelbild 0x350B6)
    /// und bucht 1.500.000 Drittel = 500.000 DM; der Deutsche Meister und der Europapokalsieger bekommen kein Geld,
    /// 0xE0:0x20 (0x2723C) ist die Fair-Play-Wertung, nicht der Tabellenplatz. Karriere-Video: Play-Off-Halbfinale mit
    /// Pokalsieg 600.000 DM (11:07:00), Deutscher Meister 200.000 DM (11:17:01, nur zwei 100.000er), 1. Platz 2. Liga
    /// „keine“ (0:35:54). Die 100.000er (Topscorer, bester Torhüter, Fair-Play) fehlen im Remake noch (Restlücke).
    /// </summary>
    [Theory]
    [InlineData(League.Bundesliga, 1, new[] { SeasonEvent.GermanChampion }, 0L)]
    [InlineData(League.Bundesliga, 5, new[] { SeasonEvent.CupWinner }, 500_000L)]
    [InlineData(League.Bundesliga, 3, new[] { SeasonEvent.EuropeanCupWinner }, 0L)]
    [InlineData(League.ZweiteLiga, 1, new SeasonEvent[0], 0L)]
    [InlineData(League.ZweiteLiga, 1, new[] { SeasonEvent.Promotion }, 500_000L)]
    [InlineData(League.Oberliga, 1, new[] { SeasonEvent.Promotion }, 300_000L)]
    public void SeasonPremium_CupWinnerAndPromotionOnly(League league, int rank, SeasonEvent[] events, long expected)
    {
        Assert.Equal(expected, GameState.SeasonPremium(league, rank, events));
    }
}
