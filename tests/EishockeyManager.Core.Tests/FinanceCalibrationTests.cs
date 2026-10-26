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
    [InlineData(League.Bundesliga, 1, false, false, 0)]
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

    [GameDataTheory]
    [InlineData(League.Oberliga, false)]
    [InlineData(League.Oberliga, true)]
    [InlineData(League.Bundesliga, false)]
    [InlineData(League.Bundesliga, true)]
    public void FourSeasonChanges_AccountAndLedgerStayWithinDocumentedEnvelope(League league, bool active)
    {
        foreach (var seed in new[] { 3, 17, 41 })
        {
            var setup = new GameSetup();
            setup.Managers.Add(new ManagerSetup { Name = "Finanzprobe", ClubIndex = league == League.Oberliga ? 24 : 4 });
            var game = NewGame.Create(ClubDatabase.Load(TestGameData.Load()), setup, seed);
            var manager = game.ActiveManager;
            long cumulative = 0;
            for (var year = 0; year < 4; year++)
            {
                var club = game.Clubs[manager.ClubIndex];
                var roster = club.Players.Count;
                var moneyAtStart = manager.Money;
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
                    game.Continue();
                    var book = game.Finance(manager).Book;
                    Assert.Equal(GameState.StartMoney + cumulative + book.CurrentTotal, manager.Money);
                }
                Assert.Equal(SeasonPhase.SeasonOver, game.Phase);
                var finance = game.Finance(manager).Book;
                output.WriteLine($"seed={seed}; club={club.Name}; start={league}; active={active}; year={year + 1}; league={club.League}; roster={roster}; begin={moneyAtStart}; end={manager.Money}; "
                    + string.Join("; ", Enum.GetValues<FinanceCategory>().Select(c => $"{c}={finance.Current(c)}")));
                // Vorläufige technische Schranken, keine nachgewiesene DOS-Balance:
                // passiv höchstens 3 Mio. Oberliga bzw. 7 Mio. Bundesliga nach vier Jahren;
                // aktiv zusätzlich bewusst angenommene Werbung und Gastronomie.
                Assert.InRange(manager.Money, -1500000L, active ? 15000000L : league == League.Oberliga ? 3000000L : 7000000L);
                var total = finance.CurrentTotal;
                cumulative += finance.CurrentTotal;
                game.StartNextSeason();
                Assert.Equal(1994 + year, game.Season);
                Assert.Equal(0, finance.CurrentTotal);
                Assert.Equal(total, finance.PreviousTotal);
                if (active) Assert.Equal(roster, club.Players.Count);
            }
        }
    }
}
