using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur finanzen-2, Teil „Monatsabschluss“: Grundeinnahmen, Grundausgaben, Stadionunterhalt, Startstadion und
/// Zinslauf gegen die Werte aus em.exe und den Referenzbildern (Belege: docs/finanzen-rekonstruktion.md).
/// Gegen den Stand 5f8e02a rot (dort Pauschalen je Liga).
/// </summary>
public class FinanceMonthlyTests
{
    private static (GameState Game, Manager Me, Club Club) OberligaGame(int level)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Test", Head = 0, ClubIndex = 24, Level = level, StartLeague = League.Oberliga });
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, 3);
        var me = game.ActiveManager;
        return (game, me, game.Clubs[me.ClubIndex]);
    }

    /// <summary>Kader auf genau 20 Spieler mit festen Gehältern und Werten bringen.</summary>
    private static void FixSquad(Club club, int[] salaries, int condition, int technique)
    {
        while (club.Players.Count > salaries.Length)
            club.Players.RemoveAt(club.Players.Count - 1);
        Assert.Equal(salaries.Length, club.Players.Count);
        for (var i = 0; i < salaries.Length; i++)
        {
            var p = club.Players[i];
            p.MonthlySalary = salaries[i];
            p.Insured = false;
            p.Condition = condition;
            p.Technique = technique;
        }
        club.Lineup.AutoFill(club.Players);
    }

    /// <summary>Gehälter des Berliner SC aus dem UI-Video (Verträge, 10:20): 23.200 DM.</summary>
    private static readonly int[] BerlinerSalaries =
        [1500, 1500, 1100, 1300, 1000, 1100, 1100, 1300, 1000, 1000, 1400, 1000, 1000, 1300, 1300, 900, 1100, 1100, 1100, 1100];

    /// <summary>
    /// #78: Startkonto [ORIGINAL] em.exe 0x29BD5: 1.200.000 + 150.000 · (5 − Level) Drittel = 400.000 + 50.000 · (5 − Level) DM.
    /// Belege: Karriere-Video 500.000 DM (Level 3), UI-Video 400.000 DM (Level 5). Vorher immer 400.000 DM.
    /// </summary>
    [Theory]
    [InlineData(0, 650_000)]
    [InlineData(3, 500_000)]
    [InlineData(5, 400_000)]
    public void StartMoney_FollowsLevel_AsInEmExe(int level, long expected)
    {
        Assert.Equal(expected, FinanceRules.StartMoney(level));
        var (_, me, _) = OberligaGame(level);
        Assert.Equal(expected, me.Money);
    }

    [Fact]
    public void BaseExpenses_FollowLevelAndLeague_AsInEmExe()
    {
        // [ORIGINAL] 0x3E819: 130.000 − 10.000 × (5 − Level) − 20 × {2400, 1800, 990} Drittel; 2. Liga −60.000,
        // Bundesliga + 175.000 × 0,75 × 2.
        Assert.Equal(130_000 - 19_800, FinanceRules.MonthlyBaseExpenseThirds(5, League.Oberliga));
        Assert.Equal(110_000 - 19_800, FinanceRules.MonthlyBaseExpenseThirds(3, League.Oberliga));
        Assert.Equal(110_000 - 36_000 + 131_250 - 60_000, FinanceRules.MonthlyBaseExpenseThirds(3, League.ZweiteLiga));
        Assert.Equal(110_000 - 48_000 + 262_500, FinanceRules.MonthlyBaseExpenseThirds(3, League.Bundesliga));
    }

    [Fact]
    public void StadiumUpkeep_IsPerLevelAndPerSeat_AsInEmExe()
    {
        // [ORIGINAL] DGROUP 0x94 / 0xB8: Oberliga-Startstadion (Zustand MITTEL, 2.000/2.500) = 9.600 + 30.000 + 30.000 Drittel.
        var stadium = Stadium.ForLeague(League.Oberliga);
        Assert.Equal(69_600, stadium.MonthlyUpkeepThirds);
        Assert.Equal(23_200, stadium.MonthlyUpkeep);
        stadium.SetLevel(StadiumFeature.Kuehlanlage, 2);
        stadium.SetLevel(StadiumFeature.Kino, 3);
        Assert.Equal(69_600 + 66_000 + 45_000, stadium.MonthlyUpkeepThirds);
    }

    [Theory]
    [InlineData(League.Bundesliga, 3_000, 4_500, 13, 2, 1, 1)]
    [InlineData(League.ZweiteLiga, 2_500, 3_500, 12, 1, 0, 1)]
    [InlineData(League.Oberliga, 2_000, 2_500, 10, 0, 0, 0)]
    public void StartStadium_SeatsPriceAndFeatures_AsAtGameStart(League league, int seats, int standing, int price, int roof, int board, int gastronomy)
    {
        // [ORIGINAL] Spielstart 0x29F2E–0x2A027; Video Oberliga 2.000/2.500 Plätze, 10 DM, OHNE DACH … Zustand MITTEL.
        var stadium = Stadium.ForLeague(league);
        Assert.Equal((seats, standing, price), (stadium.Seats, stadium.Standing, stadium.TicketPrice));
        Assert.Equal(roof, stadium.Level(StadiumFeature.Ueberdachung));
        Assert.Equal(board, stadium.Level(StadiumFeature.Anzeigetafel));
        Assert.Equal(board, stadium.Level(StadiumFeature.Kuehlanlage));
        Assert.Equal(board, stadium.Level(StadiumFeature.Verkehrsanbindung));
        Assert.Equal(gastronomy, stadium.Level(StadiumFeature.Gastronomie));
        Assert.Equal(2, stadium.Level(StadiumFeature.Zustand));
    }

    [Fact]
    public void MonthlyExpenses_BerlinerSc_MatchUiVideoWithinOneReadSalary()
    {
        // UI-Video 6:20 (Berliner SC, Oberliga, Level 5, Startstadion): Anzeige „Ausgaben 83.233 DM“.
        // Formel: (110.200 + 69.600 + 3 × 23.200) / 3 = 83.133 DM; 100 DM Abstand = ein abgelesenes Gehalt.
        var (game, me, club) = OberligaGame(level: 5);
        FixSquad(club, BerlinerSalaries, 30, 30);
        Assert.Equal(23_200, FinanceRules.MonthlyWages(club));
        Assert.Equal(83_133, game.MonthlyExpenses(me));
    }

    [Fact]
    public void MonthlyIncome_IsTeamValuesTimesPlayers_AsInVideo()
    {
        // [ORIGINAL] 0x3E657: 5 × 20 Spieler × 122 = 12.200 Drittel = 4.066 DM (Karriere-Video ESC Wolfsburg 3:42).
        var (game, me, club) = OberligaGame(level: 3);
        FixSquad(club, BerlinerSalaries, 31, 30);
        Assert.Equal(122, FinanceRules.TeamValueSum(game, club));
        Assert.Equal(4_066, game.MonthlyBaseIncome(me));
        Assert.Equal(4_066, game.MonthlyIncome(me) - game.Ads(me).MonthlyIncome);
    }

    [Fact]
    public void EmptySquad_PaysNoWages_NoFallbackLumpSum()
    {
        // [ORIGINAL] 0x3E8DC: Gehälter = Summe über die vorhandenen Spieler; ohne Kader 0 (Restlücke aus Integration A).
        var (game, me, club) = OberligaGame(level: 3);
        club.Players.Clear();
        Assert.Equal(0, FinanceRules.MonthlyWages(club));
        Assert.Equal(game.MonthlyBaseExpenses(me) + game.Finance(me).Stadium.MonthlyUpkeep, game.MonthlyExpenses(me));
    }

    [Fact]
    public void MonthEnd_BooksBaseIncomeBaseExpensesAndUpkeep()
    {
        var (game, me, club) = OberligaGame(level: 3);
        FixSquad(club, BerlinerSalaries, 31, 30);
        var book = game.Finance(me).Book;
        var unterhaltBefore = book.Current(FinanceCategory.Unterhalt);
        var sponsorenBefore = book.Current(FinanceCategory.Sponsoren);
        var gehaelterBefore = book.Current(FinanceCategory.Gehaelter);
        var expectedBase = game.MonthlyBaseExpenses(me);
        var expectedIncome = game.MonthlyBaseIncome(me);

        var day = game.FinanceDay + 1;
        while (!IsLastDayOfMonth(SeasonCalendar.LayoutDate(day)))
            day++;
        game.AdvanceFinanceTo(day);

        Assert.Equal(-(expectedBase + 23_200), book.Current(FinanceCategory.Unterhalt) - unterhaltBefore);
        Assert.Equal(-23_200, book.Current(FinanceCategory.Gehaelter) - gehaelterBefore);
        Assert.Equal(expectedIncome + game.Ads(me).MonthlyIncome, book.Current(FinanceCategory.Sponsoren) - sponsorenBefore);
        Assert.Equal(FinanceRules.StartMoney(me.Level) + book.CurrentTotal, me.Money);
    }

    [Fact]
    public void CreditInterest_AccruesDailyAndIsBookedAtMonthEnd()
    {
        // Karriere-Video (Bank): Kredit 584.000 DM → „−23.360 DM/M.“ = 4 %. [ORIGINAL] Tagesabschluss 0x3ED4F:
        // je Tag (Zinsen)/(Tage + 1) aufs Zinskonto, am Monatsletzten aufs Konto (0x3F02C/0x3F0DA, #134).
        var (game, me, _) = OberligaGame(level: 3);
        game.Finance(me).Bank.Credit = 584_000;
        Assert.Equal(23_360, game.MonthlyDebtInterest(me));
        var book = game.Finance(me).Book;
        var zinsenBefore = book.Current(FinanceCategory.Zinsen);

        var day = game.FinanceDay + 1;
        while (!IsLastDayOfMonth(SeasonCalendar.LayoutDate(day)))
            day++;
        var last = SeasonCalendar.LayoutDate(day);
        // Tag 0 (Spielbeginn) rechnet nichts ab; ab Tag 1 täglich bis einschließlich zum Monatsletzten (Tagesabschluss vor
        // der Monatsbuchung).
        var elapsedDays = last.Day - SeasonCalendar.LayoutDate(game.FinanceDay + 2).Day + 1;
        game.AdvanceFinanceTo(day);

        var booked = zinsenBefore - book.Current(FinanceCategory.Zinsen);
        var perDay = 23_360L * 3 / (DateTime.DaysInMonth(last.Year, last.Month) + 1);
        Assert.Equal(perDay * elapsedDays / 3, booked);
        Assert.Equal(FinanceRules.StartMoney(me.Level) + book.CurrentTotal, me.Money);
    }

    [Fact]
    public void Overdraft_CostsTenPercentPerMonth_InDisplayAndAccrual()
    {
        // [ORIGINAL] 0x3EAD0 (Anzeige) und 0x3EDFA (Tagesabschluss): 10 % eines negativen Kontostands.
        var (game, me, club) = OberligaGame(level: 3);
        FixSquad(club, BerlinerSalaries, 31, 30);
        var neutral = game.MonthlyExpenses(me);
        me.Money = -100_000;
        Assert.Equal(neutral + 10_000, game.MonthlyExpenses(me));
    }

    /// <summary>Tag des Monatsabschlusses (#134, [ORIGINAL] 0x3F02C: Tag = Monatslänge).</summary>
    private static bool IsLastDayOfMonth(DateOnly date) => date.Day == DateTime.DaysInMonth(date.Year, date.Month);
}
