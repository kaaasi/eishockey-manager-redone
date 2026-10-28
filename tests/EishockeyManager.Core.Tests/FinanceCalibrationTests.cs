using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;
using Xunit.Abstractions;

namespace EishockeyManager.Core.Tests;

public class FinanceCalibrationTests(ITestOutputHelper output)
{
    [GameDataFact]
    public void OriginalPremiumAmounts_MatchExecutableInstructions()
    {
        var executable = File.ReadAllBytes(Path.Combine(TestGameData.Load().RootDirectory, "em.exe"));
        // Unabhängige Bytes der Konto-Buchungen, nicht aus der Portierungsformel erzeugt.
        Assert.Equal(new byte[] { 0x81, 0x87, 0xea, 0x97, 0x60, 0xe3, 0x83, 0x97, 0xec, 0x97, 0x16 },
            executable[0x55cfc..0x55d07]);
        Assert.Equal(new byte[] { 0x81, 0x87, 0xea, 0x97, 0xa0, 0xbb, 0x83, 0x97, 0xec, 0x97, 0x0d },
            executable[0x2711e..0x27129]);
        Assert.Equal(new byte[] { 0x81, 0x87, 0xea, 0x97, 0xe0, 0x93, 0x83, 0x97, 0xec, 0x97, 0x04 },
            executable[0x271bf..0x271ca]);
        Assert.Equal(0x16e360L / 3, GameState.ChampionPremium);
        Assert.Equal(0xdbba0L / 3, GameState.PromotionPremium(League.Oberliga));
        Assert.Equal(0x493e0L / 3, GameState.OtherTitlePremium);
    }

    [Theory]
    [InlineData(League.Bundesliga, 1, false, false, 100000)] // Hauptrunden-Erster auch in der Bundesliga (0xE0:0x20, Spur finanzen-2)
    [InlineData(League.Bundesliga, 3, false, true, 500000)]
    [InlineData(League.Oberliga, 1, false, false, 100000)]
    [InlineData(League.Oberliga, 1, true, false, 400000)]
    [InlineData(League.Oberliga, 2, false, false, 0)]
    [InlineData(League.Oberliga, 2, true, false, 300000)]
    [InlineData(League.ZweiteLiga, 2, true, false, 500000)]
    public void Premium_DependsOnTitlesAndPromotion_NotEstimatedTableAwards(
        League league, int rank, bool promoted, bool champion, long expected)
    {
        var setup = new GameSetup();
        var index = (int)league * 12;
        setup.Managers.Add(new ManagerSetup { Name = "Prämie", ClubIndex = index });
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, 3);
        for (var guard = 0; game.Phase == SeasonPhase.Running; guard++)
        {
            Assert.True(guard < 1000);
            game.Continue();
        }
        var record = game.CurrentRecord!;
        // Kontrollierte sportliche Ergebnisse, anschließend echter Saisonabschluss über Continue().
        var table = record.FinalTables[league].Where(c => c != index).ToList();
        table.Insert(rank - 1, index);
        ((Dictionary<League, IReadOnlyList<int>>)record.FinalTables)[league] = table;
        record.Titles.Clear();
        if (champion) record.Titles[Title.GermanChampion] = index;
        foreach (var list in record.PromotedInto.Values) list.Clear();
        if (promoted) record.PromotedInto[(League)((int)league - 1)].Add(index);
        while (game.Phase == SeasonPhase.ClubSeasonOver) game.Continue();
        Assert.Equal(expected, record.Managers.Single().Premium);
        Assert.Equal(expected, game.Finance(game.ActiveManager).Book.Current(FinanceCategory.Praemien));
    }

    private static int HomeGames(GameState game, int club) =>
        game.Results.Count(r => r.Home == club)
        + game.PlayoffResults.Count(r => r.Home == club)
        + game.RelegationGroups.Sum(g => g.Results.Count(r => r.Home == club))
        + game.CupMatches.Count(r => r.Home == club);

    /// <summary>
    /// Mehrsaisonprobe (Spur finanzen-2): vier Saisonwechsel, passiv und aktiv, Oberliga und Bundesliga, drei Seeds.
    /// Sollspanne aus den Formeln des Originals statt technischer Alarmgrenzen: Eintritt je Heimspiel zwischen
    /// 50 Zuschauern (Untergrenze der Zuschauerformel, 0x3F7A4) und der Kapazität; Grundausgaben, Unterhalt und
    /// Gehälter genau sieben Monatsabschlüsse (1. Oktober bis 1. April); Konto = Summe des Finanzbuchs nach jedem Schritt.
    /// </summary>
    [GameDataTheory]
    [InlineData(League.Oberliga, false)]
    [InlineData(League.Oberliga, true)]
    [InlineData(League.Bundesliga, false)]
    [InlineData(League.Bundesliga, true)]
    public void FourSeasonChanges_AccountAndLedgerStayWithinOriginalEnvelope(League league, bool active)
    {
        const int MonthEnds = 7;
        foreach (var seed in new[] { 3, 17, 41 })
        {
            var setup = new GameSetup();
            setup.Managers.Add(new ManagerSetup { Name = "Finanzprobe", ClubIndex = league == League.Oberliga ? 24 : 4 });
            var game = NewGame.Create(ClubDatabase.Load(TestGameData.Load()), setup, seed);
            var manager = game.ActiveManager;
            long cumulative = 0;
            long lowest = long.MaxValue, highest = long.MinValue;
            for (var year = 0; year < 4; year++)
            {
                var club = game.Clubs[manager.ClubIndex];
                var roster = club.Players.Count;
                var moneyAtStart = manager.Money;
                var stadium = game.Finance(manager).Stadium;
                var fixedPerMonth = game.MonthlyBaseExpenses(manager) + stadium.MonthlyUpkeep;
                var wagesPerMonth = FinanceRules.MonthlyWages(club);
                long minTickets = 0, maxTickets = 0;
                var settledHomeGames = 0;
                for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
                {
                    Assert.True(guard < 1000, "Saisonende nicht erreicht");
                    if (active)
                    {
                        if (manager.Money > 800000 && game.Finance(manager).Stadium.Level(StadiumFeature.Gastronomie) == 0)
                            game.StartConstruction(manager, StadiumProject.Feature, StadiumFeature.Gastronomie, 1);
                        foreach (var player in club.Players.Where(p => p.ContractYears == 1))
                            Assert.Equal(NegotiationResult.Accepted, Contracts.Offer(player, 4,
                                Contracts.Demand(player), false, game.PlayedMatchDays, new Random(1)));
                        club.Lineup.AutoFill(club.Players);
                        foreach (var kind in Enum.GetValues<AdSlotKind>())
                        {
                            var ads = game.Ads(manager);
                            var slot = new AdSlot(kind, kind == AdSlotKind.Bande ? ads.FreeBoards().FirstOrDefault(-1) : 0);
                            if (slot.Index < 0 || ads.ContractAt(slot) is not null) continue;
                            var offer = ads.Offers[kind].Values.OrderByDescending(o => o.MonthlyAmount).FirstOrDefault();
                            if (offer is not null) game.AcceptOffer(manager, slot, offer.Sponsor);
                        }
                    }
                    var price = stadium.TicketPrice;
                    var capacity = stadium.Capacity;
                    game.Continue();
                    var book = game.Finance(manager).Book;
                    Assert.Equal(GameState.StartMoney + cumulative + book.CurrentTotal, manager.Money);
                    var homeGames = HomeGames(game, manager.ClubIndex);
                    minTickets += (homeGames - settledHomeGames) * 50L * price;
                    maxTickets += (homeGames - settledHomeGames) * (long)capacity * price;
                    settledHomeGames = homeGames;
                    lowest = Math.Min(lowest, manager.Money);
                    highest = Math.Max(highest, manager.Money);
                }
                Assert.Equal(SeasonPhase.SeasonOver, game.Phase);
                var finance = game.Finance(manager).Book;
                output.WriteLine($"seed={seed}; club={club.Name}; start={league}; active={active}; year={year + 1}; league={club.League}; roster={roster}; begin={moneyAtStart}; end={manager.Money}; homeGames={settledHomeGames}; "
                    + string.Join("; ", Enum.GetValues<FinanceCategory>().Select(c => $"{c}={finance.Current(c)}")));

                // Sollspanne aus den Originalformeln:
                Assert.InRange(finance.Current(FinanceCategory.Eintritt), minTickets, maxTickets);
                if (!active)
                {
                    Assert.Equal(-MonthEnds * fixedPerMonth, finance.Current(FinanceCategory.Unterhalt));
                    Assert.Equal(-MonthEnds * wagesPerMonth, finance.Current(FinanceCategory.Gehaelter));
                    Assert.Equal(0, finance.Current(FinanceCategory.Zinsen));
                    Assert.Equal(0, finance.Current(FinanceCategory.Bau));
                }
                else
                    Assert.True(finance.Current(FinanceCategory.Unterhalt) <= -MonthEnds * fixedPerMonth, "Unterhalt nach Ausbau nicht kleiner");
                Assert.True(finance.Current(FinanceCategory.Sponsoren) >= 0);
                Assert.True(finance.Current(FinanceCategory.Praemien) >= 0);

                var total = finance.CurrentTotal;
                cumulative += finance.CurrentTotal;
                game.StartNextSeason();
                Assert.Equal(1994 + year, game.Season);
                Assert.Equal(0, finance.CurrentTotal);
                Assert.Equal(total, finance.PreviousTotal);
                if (active) Assert.Equal(roster, club.Players.Count);
            }
            output.WriteLine($"seed={seed}; start={league}; active={active}; lowest={lowest}; highest={highest}; end={manager.Money}");
            // Kein passiver Manager rutscht in die Entlassungsgrenze (−1,5 Mio. DM); Belege der Größenordnung in
            // docs/finanzen-rekonstruktion.md (Karriere-Video: Oberliga-Saisons mit Werbung zwischen 100.000 und 750.000 DM).
            Assert.True(lowest > Manager.DebtLimit, $"Konto unter der Entlassungsgrenze: {lowest}");
        }
    }
}
