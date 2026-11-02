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
        Assert.Equal(0x16e360L / 3, GameState.CupWinnerPremium);
        Assert.Equal(0xdbba0L / 3, GameState.PromotionPremium(League.Oberliga));
        Assert.Equal(0x493e0L / 3, GameState.SinglePremium);
        // Spur statistiken (#135): Torhüter (0x271A9), Topscorer (0x271EF) und Fair-Play (0x2723C) buchen je 0x493E0 Drittel.
        foreach (var at in new[] { 0x271a9, 0x271ef, 0x2723c })
            Assert.Equal(new byte[] { 0x81, 0x87, 0x89, 0xe2, 0xe0, 0x93 }, executable[at..(at + 6)]);
    }

    [Theory]
    // Unabhängige Nachprüfung (Runde 5): keine Tabellen- und keine Meisterprämie (em.exe 0x55CD3 = DEB-Pokalsieger,
    // 0x2723C = Fair-Play-Wertung; Karriere-Video „1. Platz der Zweite Liga, Prämien: keine“).
    [InlineData(League.Bundesliga, 1, false, false, 0)]
    [InlineData(League.Bundesliga, 3, false, true, 0)]
    [InlineData(League.Oberliga, 1, false, false, 0)]
    [InlineData(League.Oberliga, 1, true, false, 300000)]
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
        // Das DEB-Pokalfinale liegt nach dem Ligaende und setzt seinen Titel erst in der Schleife oben. Seit der
        // Spielberechnung des Originals (Runde 7, R7-02) gewinnt der Oberligaverein 24 mit diesem Seed den Pokal; dessen
        // Prämie (eigener Test) wird hier ausdrücklich herausgerechnet, geprüft bleibt Tabelle/Titel/Aufstieg.
        var cup = record.Titles.GetValueOrDefault(Title.CupWinner, -1) == index ? GameState.CupWinnerPremium : 0;
        // Spur statistiken (#135): Einzelprämien (bester Torhüter, Topscorer, Fair-Play der eigenen Liga) hängen an den
        // Ranglisten des 29. April, nicht an Tabelle oder Titel; sie werden wie der Pokal herausgerechnet (eigene Tests in
        // StatistikenPraemienTests).
        cup += GameState.SinglePremium * record.Managers.Single().Events.Count(e =>
            e is SeasonEvent.BestGoalie or SeasonEvent.TopScorer or SeasonEvent.FairPlay);
        Assert.Equal(expected, record.Managers.Single().Premium - cup);
        Assert.Equal(expected, game.Finance(game.ActiveManager).Book.Current(FinanceCategory.Praemien) - cup);
    }

    private static int HomeGames(GameState game, int club) =>
        game.Results.Count(r => r.Home == club)
        + game.PlayoffResults.Count(r => r.Home == club)
        + game.RelegationGroups.Sum(g => g.Results.Count(r => r.Home == club))
        + game.CupMatches.Count(r => r.Home == club);

    /// <summary>
    /// Mehrsaisonprobe (Spur finanzen-2): vier Saisonwechsel, passiv und aktiv, Oberliga und Bundesliga, drei Seeds,
    /// mit Originaldaten. Sollspanne je Kategorie und daraus der Kontokorridor, siehe <see cref="AssertEnvelope"/>.
    /// </summary>
    [GameDataTheory]
    [InlineData(League.Oberliga, false)]
    [InlineData(League.Oberliga, true)]
    [InlineData(League.Bundesliga, false)]
    [InlineData(League.Bundesliga, true)]
    public void FourSeasonChanges_AccountAndLedgerStayWithinOriginalEnvelope(League league, bool active) =>
        AssertEnvelope(() => ClubDatabase.Load(TestGameData.Load()), league, active);

    /// <summary>Dieselbe Probe mit der synthetischen Datenbank, damit auch die CI den Kontokorridor prüft (#76).</summary>
    [Theory]
    [InlineData(League.Oberliga, false)]
    [InlineData(League.Oberliga, true)]
    [InlineData(League.Bundesliga, false)]
    [InlineData(League.Bundesliga, true)]
    public void FourSeasonChanges_Synthetic_AccountAndLedgerStayWithinOriginalEnvelope(League league, bool active) =>
        AssertEnvelope(TestGames.CreateDatabase, league, active);

    /// <summary>Teamwert-Höchstwert: Kondition und Technik je Abwehr und Sturm sind Spielerwerte 0–99 (0x3E657).</summary>
    private const int MaxTeamValueSum = 4 * Player.MaxValue;

    /// <summary>
    /// Grundausgaben in Drittel-DM, unabhängig von der Produktionsformel aus den Konstanten in em.exe 0x3E811 notiert:
    /// 130.000 − 10.000 · (5 − Level) − 20 · T[Liga] (T = 2400/1800/990) + 175.000 · (100 − 25) / 100 · (2 − Liga)
    /// − 60.000 in der 2. Liga (Preisindex DGROUP 0x2BC = 100).
    /// </summary>
    private static long ExpectedBaseExpenseThirds(int level, League league)
    {
        var tier = (int)league;
        long[] t = [2400, 1800, 990];
        return 130_000 - 10_000L * (5 - level) - 20 * t[tier] + 175_000L * 75 / 100 * (2 - tier) - (tier == 1 ? 60_000 : 0);
    }

    /// <summary>Stadionunterhalt in Drittel-DM (0x3E811): Σ Stufe · U + 15 · Sitzplätze + 12 · Stehplätze.</summary>
    private static long ExpectedUpkeepThirds(Stadium stadium)
    {
        long[] u = [24_000, 7_500, 33_000, 3_000, 4_800, 9_000, 15_000, 15_000, 7_500];
        return Enumerable.Range(0, Stadium.FeatureCount).Sum(i => stadium.Level((StadiumFeature)i) * u[i])
               + 15L * stadium.Seats + 12L * stadium.Standing;
    }

    /// <summary>
    /// Alle möglichen Saisonprämien als Summe der Einzelbeträge aus em.exe (Hauptrunden-Erster 100.000, Aufstieg
    /// 300.000/500.000, Meister 500.000, zwei weitere Titel je 100.000; Bytes in
    /// <see cref="OriginalPremiumAmounts_MatchExecutableInstructions"/>).
    /// </summary>
    private static readonly HashSet<long> PossiblePremiums = BuildPossiblePremiums();

    private static HashSet<long> BuildPossiblePremiums()
    {
        long[] parts = [100_000, 500_000, 100_000, 100_000];
        var sums = new HashSet<long>();
        foreach (var promotion in new long[] { 0, 300_000, 500_000 })
            for (var mask = 0; mask < 1 << parts.Length; mask++)
                sums.Add(promotion + Enumerable.Range(0, parts.Length).Where(i => (mask & 1 << i) != 0).Sum(i => parts[i]));
        return sums;
    }

    /// <summary>
    /// Sollspanne aus den Formeln des Originals statt technischer Alarmgrenzen, je Saison:
    /// <list type="bullet">
    /// <item>Eintritt je Heimspiel zwischen 50 Zuschauern (Untergrenze der Zuschauerformel, 0x3F7A4) und der Kapazität.</item>
    /// <item>Sponsoren höchstens sieben Monatsabschlüsse (1. Oktober bis 1. April) zu je 5 · Spieler · 396 / 3
    ///   (Grundeinnahmen mit höchstmöglichen Teamwerten) plus der größten beobachteten Summe laufender Werbeverträge.
    ///   Passiv: keine Verträge, und mit Kader mindestens 5 · Spieler · 4 / 3 je Monat.</item>
    /// <item>Prämien = eine Summe der Einzelprämien aus em.exe.</item>
    /// <item>Passiv: Unterhalt und Gehälter genau sieben Monatsabschlüsse (Konstanten aus em.exe bzw. Vertragsgehälter),
    ///   alle übrigen Kategorien 0. Damit liegt der Saisonsaldo in einem geschlossenen Korridor.</item>
    /// <item>Aktiv: alle übrigen Kategorien höchstens 0, also Saisonsaldo höchstens Eintritt + Sponsoren + Prämien (Obergrenzen).</item>
    /// <item>Konto = Startkonto + Summe des Finanzbuchs nach jedem Schritt; Entlassungsgrenze nie erreicht.</item>
    /// </list>
    /// Gegenprobe (#76): hundertfache Grundeinnahmen in <c>MonthlyFinances</c> verletzen die Sponsoren-Obergrenze.
    /// </summary>
    private void AssertEnvelope(Func<ClubDatabase> database, League league, bool active)
    {
        const int MonthEnds = 7;
        FinanceCategory[] bounded =
        [
            FinanceCategory.Eintritt, FinanceCategory.Sponsoren, FinanceCategory.Praemien,
            FinanceCategory.Unterhalt, FinanceCategory.Gehaelter,
        ];
        foreach (var seed in new[] { 3, 17, 41 })
        {
            var setup = new GameSetup();
            setup.Managers.Add(new ManagerSetup { Name = "Finanzprobe", ClubIndex = league == League.Oberliga ? 24 : 4 });
            var game = NewGame.Create(database(), setup, seed);
            var manager = game.ActiveManager;
            Assert.Equal(FinanceRules.StartMoney(manager.Level), manager.Money);
            long cumulative = 0;
            // Spur transfer-vertraege-komplett: Ablöse für Spieler, die am Saisonende gehen (0x27DF3), steht im Buch der neuen Saison.
            long compensation = 0;
            long lowest = long.MaxValue, highest = long.MinValue;
            // Teil 5 (transfer-vertraege-komplett): Zinsen für ein negatives Konto am Ende der Vorsaison stehen im Buch der neuen
            // Saison, auch wenn die Ablöse beim Saisonwechsel das Konto wieder ins Plus bringt (Seed 17, Saison 4).
            long previousSeasonEnd = manager.Money;
            // #351 (Leitstelle): Der Zinslauf des letzten Monats der Vorsaison wird erst am Monatsletzten gebucht, also im Buch
            // der neuen Saison (0x3F02C). Deshalb zählen die Tagesstände der letzten 31 Tage der Vorsaison mit (vorher nur der
            // Endstand; Seed 3, Originaldaten, Bundesliga passiv: im April negativ, am Saisonende positiv).
            var recentBalances = new Queue<long>();
            long neutral = 0; // Spur saisonwechsel, Teil 1: Konto gesetzt statt gebucht (siehe unten)
            for (var year = 0; year < 4; year++)
            {
                var club = game.Clubs[manager.ClubIndex];
                var roster = club.Players.Count;
                var moneyAtStart = manager.Money;
                var stadium = game.Finance(manager).Stadium;
                var costLeague = game.Finance(manager).CostLeague;
                var fixedPerMonth = FinanceRules.ToMarks(ExpectedBaseExpenseThirds(manager.Level, costLeague))
                                    + FinanceRules.ToMarks(ExpectedUpkeepThirds(stadium));
                var wagesPerMonth = club.Players.Sum(p => (long)p.MonthlySalary);
                long minTickets = 0, maxTickets = 0;
                var settledHomeGames = 0;
                var maxRoster = roster;
                var minRoster = roster;
                long maxContracts = game.Ads(manager).MonthlyIncome;
                var keptSet = false;
                var seasonLowest = Math.Min(Math.Min(manager.Money, previousSeasonEnd), recentBalances.DefaultIfEmpty(0).Min());
                for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
                {
                    Assert.True(guard < 1000, "Saisonende nicht erreicht");
                    if (active)
                    {
                        if (manager.Money > 800000 && game.Finance(manager).Stadium.Level(StadiumFeature.Gastronomie) == 0)
                            game.StartConstruction(manager, StadiumProject.Feature, StadiumFeature.Gastronomie, 1);
                        foreach (var player in club.Players.Where(p => p.ContractYears == 1))
                            Assert.Equal(NegotiationResult.Accepted, Contracts.Negotiate(game, manager, player, 4,
                                Contracts.Demand(player), false, demandFilled: true));
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
                    maxContracts = Math.Max(maxContracts, game.Ads(manager).Contracts.Sum(c => c.MonthlyAmount));
                    maxRoster = Math.Max(maxRoster, club.Players.Count);
                    var price = stadium.TicketPrice;
                    var capacity = stadium.Capacity;
                    game.Continue();
                    maxRoster = Math.Max(maxRoster, club.Players.Count);
                    minRoster = Math.Min(minRoster, club.Players.Count);
                    var book = game.Finance(manager).Book;
                    // Spur saisonwechsel, Teil 1 (S-K1, Datei 0x562D8): Bleibt der Verein als Letzter oder Vorletzter in der Oberliga,
                    // setzt em.exe das Konto am 29. April auf 400.000 DM, ohne Buchung; danach kommen nur noch die Prämien. Die
                    // neutrale Differenz zählt getrennt vom Finanzbuch (passive Oberligaprobe: Verein 24 endet unten).
                    if (game.Phase is SeasonPhase.SeasonOver or SeasonPhase.GameOver && game.History.Count > year
                        && GameState.KeptInOberliga(game.History[^1], manager.ClubIndex) && !keptSet)
                    {
                        Assert.Equal(GameState.OberligaKeptMoney + book.Current(FinanceCategory.Praemien), manager.Money);
                        neutral += manager.Money - (FinanceRules.StartMoney(manager.Level) + cumulative + neutral + book.CurrentTotal);
                        keptSet = true;
                    }
                    Assert.Equal(FinanceRules.StartMoney(manager.Level) + cumulative + neutral + book.CurrentTotal, manager.Money);
                    var homeGames = HomeGames(game, manager.ClubIndex);
                    minTickets += (homeGames - settledHomeGames) * 50L * price;
                    maxTickets += (homeGames - settledHomeGames) * (long)capacity * price;
                    settledHomeGames = homeGames;
                    lowest = Math.Min(lowest, manager.Money);
                    seasonLowest = Math.Min(seasonLowest, manager.Money);
                    recentBalances.Enqueue(manager.Money);
                    if (recentBalances.Count > 31)
                        recentBalances.Dequeue();
                    highest = Math.Max(highest, manager.Money);
                }
                // #352 (R9S-04, Spur geld Teil 7): Der Ausweg „passiver Manager entlassen → Probe endet“ (Integration Runde 9)
                // wurde am Head nie mehr betreten und hätte jede spätere Entlassung samt Kontoprüfung verdeckt. Jetzt muss
                // jede Saison regulär enden; eine Entlassung nennt Strafen und Kader, damit die Ursache sichtbar wird.
                Assert.True(game.Phase == SeasonPhase.SeasonOver,
                    $"Saison {year + 1}: {game.Phase}, entlassen={manager.Dismissed}, Strafen={game.Finance(manager).Book.Current(FinanceCategory.Strafen)}, Kader={club.Players.Count}");

                previousSeasonEnd = manager.Money;
                var finance = game.Finance(manager).Book;
                output.WriteLine($"seed={seed}; club={club.Name}; start={league}; active={active}; year={year + 1}; league={club.League}; roster={roster}; begin={moneyAtStart}; end={manager.Money}; homeGames={settledHomeGames}; "
                    + string.Join("; ", Enum.GetValues<FinanceCategory>().Select(c => $"{c}={finance.Current(c)}")));

                // Sollspanne je Kategorie aus den Originalformeln:
                var tickets = finance.Current(FinanceCategory.Eintritt);
                Assert.InRange(tickets, minTickets, maxTickets);
                var maxSponsors = MonthEnds * (FinanceRules.ToMarks(5L * maxRoster * MaxTeamValueSum) + maxContracts);
                var sponsors = finance.Current(FinanceCategory.Sponsoren);
                Assert.True(sponsors <= maxSponsors, $"Sponsoren {sponsors} über der Obergrenze {maxSponsors} (Saison {year + 1})");
                var premiums = finance.Current(FinanceCategory.Praemien);
                Assert.Contains(premiums, PossiblePremiums);

                var total = finance.CurrentTotal;
                if (!active)
                {
                    Assert.Equal(0, maxContracts);
                    var minSponsors = MonthEnds * FinanceRules.ToMarks(5L * minRoster * 4);
                    Assert.True(sponsors >= minSponsors, $"Sponsoren {sponsors} unter {minSponsors}");
                    Assert.Equal(-MonthEnds * fixedPerMonth, finance.Current(FinanceCategory.Unterhalt));
                    Assert.Equal(-MonthEnds * wagesPerMonth, finance.Current(FinanceCategory.Gehaelter));
                    // Spur heimspiel-statistik (#136): Mit gehaltenem Kader rutscht der passive Oberligist ab Saison 3 ins Minus;
                    // dann fallen Überziehungszinsen an (0x3ED4F: 10 % des negativen Kontos je Monat, höchstens zwölf Monate).
                    var interest = finance.Current(FinanceCategory.Zinsen);
                    Assert.InRange(interest, seasonLowest < 0 ? 12 * (seasonLowest / 10 - 1) : 0, 0);
                    foreach (var category in Enum.GetValues<FinanceCategory>().Except(bounded).Except([FinanceCategory.Zinsen]))
                        Assert.True(finance.Current(category) == (category == FinanceCategory.Transfers ? compensation : 0),
                            $"{category} = {finance.Current(category)} beim passiven Manager");
                    // Kontokorridor der Saison aus den Spannen oben.
                    var fixedCosts = MonthEnds * (fixedPerMonth + wagesPerMonth);
                    Assert.InRange(total - interest - compensation, minTickets + minSponsors - fixedCosts, maxTickets + maxSponsors + PossiblePremiums.Max() - fixedCosts);
                }
                else
                {
                    Assert.True(finance.Current(FinanceCategory.Unterhalt) <= -MonthEnds * fixedPerMonth, "Unterhalt nach Ausbau nicht kleiner");
                    foreach (var category in Enum.GetValues<FinanceCategory>().Except([FinanceCategory.Eintritt, FinanceCategory.Sponsoren, FinanceCategory.Praemien]))
                        Assert.True(finance.Current(category) - (category == FinanceCategory.Transfers ? compensation : 0) <= 0,
                            $"{category} = {finance.Current(category)} beim aktiven Manager");
                    Assert.True(total - compensation <= maxTickets + maxSponsors + premiums, $"Saisonsaldo {total} über dem Korridor");
                }

                cumulative += total;
                // Spur heimspiel-statistik (#136): Der passive Manager verlängert die angebotenen Verträge erst nach dem
                // letzten Monatsabschluss (Gehälter der Saison bleiben fest), sonst ist sein Kader ab Saison 3 leer und
                // „Weniger als neun Spieler“ (200.000 DM je Spiel) führt zur Entlassung.
                if (!active)
                    TestSeason.KeepSquad(game, manager);
                game.StartNextSeason();
                Assert.Equal(1994 + year, game.Season);
                compensation = finance.Current(FinanceCategory.Transfers);
                Assert.True(compensation >= 0);
                Assert.Equal(compensation, finance.CurrentTotal);
                Assert.Equal(total, finance.PreviousTotal);
                if (active) Assert.Equal(roster, club.Players.Count);
            }
            output.WriteLine($"seed={seed}; start={league}; active={active}; lowest={lowest}; highest={highest}; end={manager.Money}");
            Assert.False(manager.Dismissed, "Manager entlassen");
            Assert.True(lowest > Manager.DebtLimit, $"Konto unter der Entlassungsgrenze: {lowest}");
        }
    }
}
