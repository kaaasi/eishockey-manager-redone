using System.Reflection;
using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur saison-geld Teil 2 (Runde 25): F06 (A888, Banden je Feld) und F08 (A512 Baufirmen, A526 Sommerpause je Manager,
/// A509 Bau ohne Ziel). Belege: docs/analyse/re-abweichungen.md Abschnitte F06 und F08. Gegen den Stand eaf8621 rot.
/// </summary>
public class SaisonGeldTeil2Tests
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

    private static Sponsoring Boards(GameState game, Manager me, params SponsorContract[] contracts)
    {
        var ads = game.Ads(me);
        ads.Contracts.Clear();
        ads.Contracts.AddRange(contracts);
        ads.Offers[AdSlotKind.Bande] = new() { [6] = new SponsorOffer(AdSlotKind.Bande, 6, 12, 1, 900) };
        return ads;
    }

    private static bool Offended(Stadium stadium, int p) => p switch
    {
        Stadium.SeatsProject => stadium.IsOffended(StadiumProject.Sitzplaetze, default),
        Stadium.StandingProject => stadium.IsOffended(StadiumProject.Stehplaetze, default),
        _ => stadium.IsOffended(StadiumProject.Feature, (StadiumFeature)p),
    };

    private static SponsorContract Board(int sponsor, long amount, bool cancellable, params int[] boards) =>
        new() { Kind = AdSlotKind.Bande, Sponsor = sponsor, Boards = boards, MonthlyAmount = amount, MonthsLeft = 12, Cancellable = cancellable };

    // ---- F06 / A888 ------------------------------------------------------------------------------

    /// <summary>[ORIGINAL] 0x3E657 (Datei 0x3E70C–0x3E734): Bandenvertrag über zwei Felder zu je 300 bringt 600 (vorher 300).</summary>
    [Fact]
    public void BoardContract_PaysPerField()
    {
        var game = TestGames.Create(3);
        var ads = Boards(game, game.ActiveManager, Board(1, 300, false, 0, 1));
        Assert.Equal(600, ads.MonthlyIncome);
        ads.Contracts.Add(new SponsorContract { Kind = AdSlotKind.Trikot, Sponsor = 2, Boards = [], MonthlyAmount = 1_000 });
        Assert.Equal(1_600, ads.MonthlyIncome);
    }

    /// <summary>
    /// [ORIGINAL] Datei 0x2BAD9/0x2BADE–0x2BB6D: Felder 0/1 kündbar (Nr 1), 2 fest, 3 … 6 frei; Klick auf Feld 3, Angebot über eine
    /// Bande → Feld 0 neu, Feld 1 alt mit seinem Betrag, Feld 3 frei; Einnahme 900 + 300 + 500.
    /// </summary>
    [Fact]
    public void BoardOffer_TakesFirstFreeOrCancellableFieldFromZero()
    {
        var game = TestGames.Create(3);
        var me = game.ActiveManager;
        var ads = Boards(game, me, Board(1, 300, true, 0, 1), Board(2, 500, false, 2));
        var contract = game.AcceptOffer(me, new AdSlot(AdSlotKind.Bande, 3), 6);
        Assert.NotNull(contract);
        Assert.Equal([0], contract.Boards);
        Assert.Equal(6, ads.ContractAt(new AdSlot(AdSlotKind.Bande, 0))!.Sponsor);
        var rest = ads.ContractAt(new AdSlot(AdSlotKind.Bande, 1))!;
        Assert.Equal((1, 300L, true), (rest.Sponsor, rest.MonthlyAmount, rest.Cancellable));
        Assert.Null(ads.ContractAt(new AdSlot(AdSlotKind.Bande, 3)));
        Assert.Equal(1_700, ads.MonthlyIncome);
    }

    /// <summary>
    /// [ORIGINAL] Datei 0x2BA9D–0x2BAD1, Knopf 0x2BF86–0x2BFD2: Das Original prüft nicht, ob das angeklickte Feld frei ist, nur die
    /// Zahl freier oder kündbarer Felder. Klick auf das fest belegte Feld 0 → angenommen in Feld 2.
    /// </summary>
    [Fact]
    public void BoardOffer_ClickOnFixedField_AcceptedIntoFirstFree()
    {
        var game = TestGames.Create(3);
        var me = game.ActiveManager;
        var ads = Boards(game, me, Board(1, 300, false, 0, 1));
        var contract = game.AcceptOffer(me, new AdSlot(AdSlotKind.Bande, 0), 6);
        Assert.NotNull(contract);
        Assert.Equal([2], contract.Boards);
        Assert.Equal(1, ads.ContractAt(new AdSlot(AdSlotKind.Bande, 0))!.Sponsor);
    }

    /// <summary>Knopf 0x2BF86: Banden des Angebots &gt; freie oder kündbare Felder → keine Annahme.</summary>
    [Fact]
    public void BoardOffer_TooFewFreeFields_Rejected()
    {
        var game = TestGames.Create(3);
        var me = game.ActiveManager;
        var ads = Boards(game, me, Board(1, 300, false, 0, 1, 2, 3, 4, 5));
        ads.Offers[AdSlotKind.Bande] = new() { [6] = new SponsorOffer(AdSlotKind.Bande, 6, 12, 2, 900) };
        Assert.Null(game.AcceptOffer(me, new AdSlot(AdSlotKind.Bande, 6), 6));
    }

    // ---- F08 / A512, A526, A509 ------------------------------------------------------------------

    /// <summary>[ORIGINAL] Datei 0x3ED19–0x3ED48: random(0, 10) für alle 11 Projekte, auch ohne gesetzten Merker (vorher 0 Würfe).</summary>
    [Fact]
    public void ForgiveBuilders_DrawsElevenTimesWithoutFlags()
    {
        var game = TestGames.Create(3);
        var me = game.ActiveManager;
        var replica = GameRandom.FromState(game.Random.Capture());
        Call(game, "ForgiveBuilders", me);
        for (var p = 0; p < Stadium.ProjectCount; p++)
            SquadSetup.Between(replica, 0, 10);
        Assert.Equal(replica.Next(), game.Random.Next());
    }

    /// <summary>
    /// [ORIGINAL] Sommerschleife Datei 0x564D2–0x56593 (f08_sommerpause.py): je Tag und je Manager verschränkt – Baufirmen, Werbeangebote
    /// und Talentsuche von Manager 0, dann dieselben von Manager 1, danach die Börse (einmal je Tag, #188). Orakel am 15. Mai (Angebotstag)
    /// aus den Einzelteilen in dieser Folge; vorher erst die Baufirmen aller, dann die Angebote aller.
    /// </summary>
    [Fact]
    public void SummerDay_InterleavesPerManager()
    {
        var counter = GameState.SummerBreakCounters().First(c => SeasonCalendar.LayoutDate(c - 1) is { Month: 5, Day: 15 });
        var date = SeasonCalendar.LayoutDate(counter - 1);
        Assert.NotEqual(0, counter % 7); // kein Bautag
        var game = TwoManagers(5);
        var oracle = TwoManagers(5);
        foreach (var g in new[] { game, oracle })
            foreach (var m in g.Managers)
                for (var p = 0; p < Stadium.ProjectCount; p++)
                    g.Finance(m).Stadium.SetOffended(p, true);

        Call(game, "RunSummerDay", counter);

        foreach (var m in oracle.Managers)
        {
            Call(oracle, "ForgiveBuilders", m);
            oracle.RefreshSponsorOffers(m, noDice: false);
            Call(oracle, "RollTalentSearchPrice", m);
        }
        oracle.AdvanceStocks();

        Assert.True(SponsorRules.IsOfferDay(date));
        for (var i = 0; i < 2; i++)
        {
            for (var p = 0; p < Stadium.ProjectCount; p++)
                Assert.Equal(Offended(oracle.Finance(oracle.Managers[i]).Stadium, p), Offended(game.Finance(game.Managers[i]).Stadium, p));
            Assert.Equal(
                oracle.Ads(oracle.Managers[i]).Offers[AdSlotKind.Bande].Values.Select(o => o.MonthlyAmount),
                game.Ads(game.Managers[i]).Offers[AdSlotKind.Bande].Values.Select(o => o.MonthlyAmount));
        }
        Assert.Equal(oracle.Random.Next(), game.Random.Next());
    }

    /// <summary>
    /// [ORIGINAL] A509, Datei 0x3D316 (Meldung 0x4B2BA aus Datei 0x3D447; f08_stadionbau.py): Restwoche 0 ohne Ziel setzt Stufe = 0
    /// bzw. Plätze = 0 und meldet (vorher blieb die Stufe).
    /// </summary>
    [Fact]
    public void Construction_WithoutTarget_SetsLevelToZero()
    {
        var game = TestGames.Create(3);
        var stadium = game.Finance(game.ActiveManager).Stadium;
        stadium.Begin(StadiumProject.Feature, (StadiumFeature)4, 5, 1);
        stadium.AdvanceWeek();
        Assert.Equal(5, stadium.Level((StadiumFeature)4));
        stadium.ImportConstruction(4, 0, 1);
        stadium.ImportConstruction(Stadium.SeatsProject, 0, 1);
        var done = stadium.AdvanceWeek();
        Assert.Equal(0, stadium.Level((StadiumFeature)4));
        Assert.Equal(0, stadium.Seats);
        Assert.Equal(2, done.Count);
    }
}
