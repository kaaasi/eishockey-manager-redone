using System.Reflection;
using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur tagesabschluss (Runde 26, #1018, SG-01): Tagesabschluss 0x3ED01 und Werbebewertung 0x3F833 je Manager im Hauptweg
/// (Datei 0x55217–0x55251) und in der WM-Schleife (Datei 0x55683–0x5571F). Beleg: tools/reverse-engineering/tagesabschluss/hauptweg.py.
/// Gegen den Stand 56b7f7c rot.
/// </summary>
public class TagesabschlussTests
{
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

    private static GameState TwoManagers(int seed)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "A", Head = 0, ClubIndex = 0, StartLeague = League.Bundesliga });
        setup.Managers.Add(new ManagerSetup { Name = "B", Head = 1, ClubIndex = 1, StartLeague = League.Bundesliga });
        return NewGame.Create(TestGames.CreateDatabase(), setup, seed);
    }

    private static void Call(GameState game, string method, params object[] args) =>
        (typeof(GameState).GetMethod(method, Private, args.Select(a => a.GetType()).ToArray())
            ?? throw new InvalidOperationException($"{method} fehlt")).Invoke(game, args);

    private static int FirstDay(Func<DateOnly, int, bool> match) =>
        Enumerable.Range(1, SeasonCalendar.ClubSeasonEnd - 1)
            .First(d => match(SeasonCalendar.LayoutDate(d), DayCounter.Of(d)));

    private static bool Offended(Stadium stadium, int p) => p switch
    {
        Stadium.SeatsProject => stadium.IsOffended(StadiumProject.Sitzplaetze, default),
        Stadium.StandingProject => stadium.IsOffended(StadiumProject.Stehplaetze, default),
        _ => stadium.IsOffended(StadiumProject.Feature, (StadiumFeature)p),
    };

    private static bool IsMonthEnd(DateOnly date) => date.Day == DateTime.DaysInMonth(date.Year, date.Month);

    /// <summary>
    /// [ORIGINAL] Datei 0x55217–0x55251 (hauptweg.py Teil 1 und 3): je Manager erst 0x3ED01 (11 Würfe Baufirmen 0x3ED20, Angebote
    /// 0x3FB47, Preis der Talentsuche 0x3EEB2/0x3EEC7), dann 0x3F833, dann der nächste Manager; die Börse einmal je Tag (#188).
    /// Orakel am 15. eines Monats (Angebotstag, kein Bautag) aus den Einzelteilen in dieser Folge; vorher erst die Baufirmen aller,
    /// dann die Angebote aller, dann die Bewertungen aller.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void OfferDay_InterleavesPerManager(bool clubDay)
    {
        var day = FirstDay((date, counter) => date.Day == 15 && counter % 7 != 0);
        var game = TwoManagers(11);
        var oracle = TwoManagers(11);
        foreach (var g in new[] { game, oracle })
        {
            g.AdvanceFinance(day - 1, clubDay);
            foreach (var m in g.Managers)
                for (var p = 0; p < Stadium.ProjectCount; p++)
                    g.Finance(m).Stadium.SetOffended(p, true);
        }
        Assert.Equal(oracle.Random.Next(), game.Random.Next());

        game.AdvanceFinance(day, clubDay);

        oracle.FinanceDay = day;
        oracle.RecordRankSlots();
        foreach (var m in oracle.Managers)
        {
            Call(oracle, "ForgiveBuilders", m);
            oracle.RefreshSponsorOffers(m, noDice: false);
            Call(oracle, "RollTalentSearchPrice", m);
            oracle.UpdateSponsorRating(m);
        }
        oracle.AdvanceStocks();

        Assert.True(SponsorRules.IsOfferDay(SeasonCalendar.LayoutDate(day)));
        for (var i = 0; i < 2; i++)
        {
            for (var p = 0; p < Stadium.ProjectCount; p++)
                Assert.Equal(Offended(oracle.Finance(oracle.Managers[i]).Stadium, p), Offended(game.Finance(game.Managers[i]).Stadium, p));
            Assert.Equal(oracle.Finance(oracle.Managers[i]).SponsorRating, game.Finance(game.Managers[i]).SponsorRating);
            Assert.Equal(
                oracle.Ads(oracle.Managers[i]).Offers.SelectMany(k => k.Value.Values.Select(o => (k.Key, o.Sponsor, o.MonthlyAmount))),
                game.Ads(game.Managers[i]).Offers.SelectMany(k => k.Value.Values.Select(o => (k.Key, o.Sponsor, o.MonthlyAmount))));
        }
        Assert.Equal(oracle.Random.Next(), game.Random.Next());
    }

    /// <summary>
    /// [ORIGINAL] Datei 0x5528A (hauptweg.py Teil 1): Die Talentsuche 0x06FA5 würfelt erst nach der Schleife 0x3ED01/0x3F833 aller
    /// Manager. Orakel an einem Takttag (Zähler ≡ 4, kein Angebots- und kein Bautag), Suche von Manager 1 endet; vorher würfelte die
    /// Suche vor den Baufirmen.
    /// </summary>
    [Fact]
    public void TalentSearch_DrawsAfterDayCloseOfAllManagers()
    {
        var day = FirstDay((date, counter) => counter % 7 == 4 && !SponsorRules.IsOfferDay(date) && date.Day != 1);
        var game = TwoManagers(19);
        var oracle = TwoManagers(19);
        foreach (var g in new[] { game, oracle })
        {
            g.AdvanceFinance(day - 1, clubDay: true);
            g.Finance(g.Managers[1]).TalentSearch = new TalentSearch { TicksLeft = 1 };
        }

        game.AdvanceFinance(day, clubDay: true);

        oracle.FinanceDay = day;
        oracle.RecordRankSlots();
        foreach (var m in oracle.Managers)
        {
            Call(oracle, "ForgiveBuilders", m);
            oracle.UpdateSponsorRating(m);
        }
        Call(oracle, "DailyTalentSearch", day);
        oracle.AdvanceStocks();

        Assert.Null(game.Finance(game.Managers[1]).TalentSearch);
        Assert.Equal(oracle.Clubs[1].Players.Count, game.Clubs[1].Players.Count);
        Assert.Equal(oracle.Random.Next(), game.Random.Next());
    }

    /// <summary>
    /// [ORIGINAL] Monatsabschluss 0x3F02C im Tagesabschluss 0x3ED01, Stadionbau 0x3D316 erst danach (Hauptweg Datei 0x552C3 nach
    /// der Schleife 0x55217–0x55251, WM-Schleife Datei 0x556CD nach 0x3ED01/0x3F833 des Managers; hauptweg.py Teil 1 und 2): An einem
    /// Monatsletzten mit Zähler ≡ 0 (mod 7) bucht der Unterhalt die Stufe vor dem fertigen Bau. Vorher Bau vor dem Monatsabschluss.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MonthEndBuildDay_BooksUpkeepBeforeConstruction(bool clubDay)
    {
        var day = FirstDay((date, counter) => IsMonthEnd(date) && counter % 7 == 0);
        var game = TwoManagers(13);
        var plain = TwoManagers(13);
        foreach (var g in new[] { game, plain })
            g.AdvanceFinance(day - 1, clubDay);
        var me = game.Managers[0];
        var stadium = game.Finance(me).Stadium;
        var feature = (StadiumFeature)4;
        var level = stadium.Level(feature);
        stadium.Begin(StadiumProject.Feature, feature, level + 1, 1);
        var upkeep = game.Finance(me).Book.Current(FinanceCategory.Unterhalt);
        Assert.Equal(upkeep, plain.Finance(plain.Managers[0]).Book.Current(FinanceCategory.Unterhalt));

        game.AdvanceFinance(day, clubDay);
        plain.AdvanceFinance(day, clubDay);

        Assert.Equal(level + 1, stadium.Level(feature));
        Assert.NotEqual(upkeep, game.Finance(me).Book.Current(FinanceCategory.Unterhalt));
        Assert.Equal(
            plain.Finance(plain.Managers[0]).Book.Current(FinanceCategory.Unterhalt),
            game.Finance(me).Book.Current(FinanceCategory.Unterhalt));
    }

    /// <summary>
    /// Mehrsaisonprobe (4 Saisonwechsel, passiv/aktiv, zwei Manager, Geld): Beide Manager buchen in jeder Saison Unterhalt und
    /// Werbung, zwei gleiche Läufe ergeben denselben Abdruck und dieselben Kontostände.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FourSeasonChanges_DayClosePerManager(bool active)
    {
        (string Print, long[] Money, int Seasons) Run()
        {
            var game = TwoManagers(17);
            var seasons = 0;
            var print = RngTwinProbeTests.Run(game, active, beforeChange: g =>
            {
                seasons++;
                foreach (var m in g.Managers)
                {
                    Assert.True(g.Finance(m).Book.Current(FinanceCategory.Unterhalt) < 0, "kein Unterhalt gebucht");
                    Assert.True(g.Finance(m).SponsorRating >= 0, "keine Werbebewertung");
                }
            });
            return (print, game.Managers.Select(m => m.Money).ToArray(), seasons);
        }

        var run = Run();
        var again = Run();
        Assert.Equal(run.Print, again.Print);
        Assert.Equal(run.Money, again.Money);
        Assert.Equal(4, run.Seasons);
    }
}
