using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Talentsuche nach em.exe (Spur geld, Teil 6; R-K1): Start Datei 0x465F1, Preis Datei 0x3EEA2–0x3EF4E, Zähler und Ergebnis
/// Datei 0x06FA5, Talent Datei 0x06D7F, Grenze Datei 0x3CCEB. Vorher: Rabatt nach Jugendförderung, Grenze 30 schon beim
/// Start, 4 Wochen, Chance nach Jugendplatz, Talentwerte aus der Vereinsstärke (A050, A127–A132, A242–A246, F011, F012, F089).
/// </summary>
public class TalentSearchTests
{
    /// <summary>[ORIGINAL] DGROUP 0x20D Startwert 270.000 Drittel = 90.000 DM; „Rabatt:“ gehört zum Stadion (A050, A242).</summary>
    [Fact]
    public void Price_StartsAt90000_WithoutDiscountFromYouthFunding()
    {
        var game = TestGames.Create();
        var me = game.ActiveManager;
        game.SetYouthFunding(me, YouthTeamKind.Schueler, 10_000);
        game.SetYouthFunding(me, YouthTeamKind.Jugend, 5_000);
        Assert.Equal(90_000, game.TalentSearchPrice(me));
    }

    /// <summary>[ORIGINAL] Datei 0x469A8–0x469B7: nur mit Konto &gt; Preis; der Preis wird sofort gebucht (Datei 0x469CA).</summary>
    [Fact]
    public void Start_NeedsMoreMoneyThanThePrice()
    {
        var game = TestGames.Create();
        var me = game.ActiveManager;
        var price = game.TalentSearchPrice(me);
        me.Money = price;
        Assert.Equal(TalentSearchStart.NotEnoughMoney, TalentScout.Search(game, me));
        Assert.Null(game.Finance(me).TalentSearch);
        Assert.Equal(price, me.Money);

        me.Money = price + 1;
        Assert.Equal(TalentSearchStart.Started, TalentScout.Search(game, me));
        Assert.Equal(1, me.Money);
        Assert.Equal(-price, game.Finance(me).Book.Current(FinanceCategory.Talentsuche));
        Assert.Equal(TalentSearchStart.AlreadyRunning, game.StartTalentSearch(me));
        Assert.Equal(1, me.Money);
    }

    /// <summary>[ORIGINAL] Beim Start prüft 0x465F1 den Kader nicht (A246); die Grenze 28 greift erst beim Ergebnis.</summary>
    [Fact]
    public void Start_DoesNotCheckTheSquad()
    {
        var game = TestGames.Create();
        var me = game.ActiveManager;
        me.Money = 1_000_000;
        var club = game.Clubs[me.ClubIndex];
        while (club.Players.Count < 30)
            club.Players.Add(new Player { Name = "X", Role = PlayerRole.Forward });
        Assert.Equal(TalentSearchStart.Started, game.StartTalentSearch(me));
    }

    /// <summary>[ORIGINAL] Datei 0x469D2–0x46A10: Zähler random(3, 10), mit random(0, 3) = 0 zusätzlich + 6 (3 … 16).</summary>
    [Fact]
    public void Start_CounterIsThreeToTen_PlusSixWithOneInFour()
    {
        var seen = new SortedSet<int>();
        var extra = 0;
        var game = TestGames.Create();
        var me = game.ActiveManager;
        for (var i = 0; i < 200; i++)
        {
            me.Money = 1_000_000;
            game.Finance(me).TalentSearch = null;
            Assert.Equal(TalentSearchStart.Started, game.StartTalentSearch(me));
            var ticks = game.Finance(me).TalentSearch!.TicksLeft;
            Assert.InRange(ticks, 3, 16);
            seen.Add(ticks);
            if (ticks > 10)
                extra++;
        }
        Assert.Equal(3, seen.Min);
        Assert.Equal(16, seen.Max);
        Assert.InRange(extra, 20, 80); // Erwartung 200 · 1/4 · 6/8 = 37,5
    }

    /// <summary>[ORIGINAL] Datei 0x07003–0x0701E: Erfolg mit random(0, 5 − ⌊(5 − Level) / 2⌋) = 0 (A243).</summary>
    [Theory]
    [InlineData(0, 4)]
    [InlineData(1, 4)]
    [InlineData(2, 5)]
    [InlineData(3, 5)]
    [InlineData(4, 6)]
    [InlineData(5, 6)]
    public void SuccessChance_DependsOnLevel(int level, int faces) => Assert.Equal(faces - 1, TalentScout.SuccessDie(level));

    /// <summary>
    /// [ORIGINAL] Tageslauf 0x54C9F ruft 0x06FA5 an Tagen ≡ 0 oder 4 (mod 7) (Datei 0x55253–0x5528A): Der Zähler sinkt genau um
    /// die Zahl dieser Tage; bei 0 kommt genau eine Nachricht oder (Grenze) keine.
    /// </summary>
    [Fact]
    public void Counter_FallsOnDaysZeroAndFourModSeven_UntilTheResult()
    {
        var game = TestGames.Create(7);
        var me = game.ActiveManager;
        me.Money = 1_000_000;
        TestSeason.PlayMatchDay(game);
        Assert.Equal(TalentSearchStart.Started, game.StartTalentSearch(me));
        var club = game.Clubs[me.ClubIndex];
        var before = club.Players.ToList();
        var ticks = game.Finance(me).TalentSearch!.TicksLeft;
        for (var guard = 0; game.Finance(me).TalentSearch is { } search; guard++)
        {
            Assert.True(guard < 100, "Suche endet nicht");
            var from = game.FinanceDay;
            game.Continue();
            var due = Enumerable.Range(from + 1, game.FinanceDay - from).Count(TalentScout.IsTick);
            if (game.Finance(me).TalentSearch is { } still)
                Assert.Equal(ticks - due, still.TicksLeft);
            else
                Assert.True(due >= ticks, $"Ende nach {due} Takttagen, Zähler {ticks}");
            ticks = game.Finance(me).TalentSearch?.TicksLeft ?? 0;
        }
        var reports = game.Inbox(me).Where(m => m.Lines[0].StartsWith("Ihr")).ToList();
        var report = Assert.Single(reports);
        if (report.Lines[0] == "Ihre Talentsuche war ein Erfolg.")
            Assert.Equal(["Ihre Talentsuche war ein Erfolg.", "Ein junger Spieler stößt zu Ihrem Kader."], report.Lines);
        else
        {
            Assert.Equal(["Ihr Talentspäher gibt die", "Suche ohne Erfolg auf... Pech..."], report.Lines);
            Assert.Equal(before, club.Players);
        }
    }

    /// <summary>
    /// [ORIGINAL] Datei 0x06D7F: Talent aus einem freien Poolplatz (Besitzer 6) mit dessen Name, Talent und Rolle; Alter
    /// random(18, 21), Form random(45, 55), Technik und Kondition random(s − 5, s + 1); Gehalt Wert(Gehalt) · 80 / 100,
    /// Laufzeit random(2, 3). A118, A120, A127–A132, F011.
    /// </summary>
    [Fact]
    public void Talent_ComesFromThePool_WithOriginalValues()
    {
        var ages = new SortedSet<int>();
        var game = TestGames.Create();
        var me = game.ActiveManager;
        var club = game.Clubs[me.ClubIndex];
        for (var i = 0; i < 40; i++)
        {
            var free = game.TransferMarket.Pool.Where(s => s.Owner == PoolOwner.Free).Select(s => s.Player).ToHashSet();
            Assert.NotEmpty(free);
            var poolTalent = free.ToDictionary(p => p, p => (p.Name, p.Talent, p.Role));

            var talent = game.JoinScoutedTalent(me, 60);

            Assert.NotNull(talent);
            Assert.Contains(talent, free);
            Assert.Equal(poolTalent[talent], (talent.Name, talent.Talent, talent.Role));
            var slot = Assert.Single(game.TransferMarket.Pool, s => s.Player == talent);
            Assert.Equal(PoolOwner.Taken, slot.Owner);
            Assert.Equal(club.Index, slot.Club);
            Assert.Contains(talent, club.Players);
            Assert.InRange(talent.Age, 18, 21);
            ages.Add(talent.Age);
            Assert.InRange(talent.Form, 45, 55);
            Assert.InRange(talent.Technique, 55, 61);
            Assert.InRange(talent.Condition, 55, 61);
            Assert.InRange(talent.ContractYears, 2, 3);
            Assert.Equal(Contracts.ValueSalary(game, me, talent) * 80 / 100, talent.MonthlySalary);
            Assert.NotEqual(0, talent.Id);
            Assert.Empty(PlayerFactory.Validate(club, talent));
            club.Players.Remove(talent); // Platz für den nächsten Fund (Grenze 28)
        }
        Assert.Equal([18, 19, 20, 21], ages);
    }

    /// <summary>
    /// [ORIGINAL] Datei 0x3CCEB/0x06D8D: Besitzt der Manager 28 Spieler (eigener Kader und verliehene), kommt kein Talent und
    /// keine Nachricht (F089).
    /// </summary>
    [Fact]
    public void Talent_NotWith28OwnedPlayers_CountingLoanedOut()
    {
        var game = TestGames.Create();
        var me = game.ActiveManager;
        var club = game.Clubs[me.ClubIndex];
        var other = game.Clubs.First(c => c != club);
        while (club.Players.Count < 27)
            club.Players.Add(new Player { Name = "X", Role = PlayerRole.Forward });
        while (club.Players.Count > 27)
            club.Players.RemoveAt(club.Players.Count - 1);
        Assert.Equal(27, game.OwnedPlayers(me));
        other.Players.Add(new Player { Name = "LEIHE", Role = PlayerRole.Forward, OnLoan = true,
            Loan = new PlayerLoan(club, 2, 1_000, false) });
        Assert.Equal(28, game.OwnedPlayers(me));

        Assert.Null(game.JoinScoutedTalent(me, 60));
        Assert.Equal(27, club.Players.Count);

        other.Players.RemoveAt(other.Players.Count - 1);
        Assert.NotNull(game.JoinScoutedTalent(me, 60));
        Assert.Equal(28, club.Players.Count);
    }

    /// <summary>
    /// Im Ablauf: Bei 28 eigenen Spielern endet jede Suche ohne Talent; ein Erfolg bleibt dann ohne Nachricht, nur der
    /// Misserfolg meldet sich (Datei 0x07043–0x07045).
    /// </summary>
    [Fact]
    public void Result_With28Players_IsSilentOnSuccess()
    {
        var silent = 0;
        var runs = 0;
        var game = TestGames.Create();
        var me = game.ActiveManager;
        var club = game.Clubs[me.ClubIndex];
        for (var i = 0; i < 24 && game.Phase == SeasonPhase.Running; i++)
        {
            me.Money = 1_000_000;
            while (club.Players.Count < TalentScout.MaxOwned)
                club.Players.Add(new Player { Name = "X", Role = PlayerRole.Forward });
            TestSeason.KeepSquad(game, me);
            var before = club.Players.ToList();
            var startDay = game.Day;
            Assert.Equal(TalentSearchStart.Started, game.StartTalentSearch(me));
            game.Finance(me).TalentSearch!.TicksLeft = 1; // Ergebnis am nächsten Takttag
            for (var guard = 0; game.Finance(me).TalentSearch is not null; guard++)
            {
                Assert.True(guard < 100);
                game.Continue();
            }
            Assert.Equal(before, club.Players);
            Assert.DoesNotContain(game.Inbox(me), m => m.Lines[0] == "Ihre Talentsuche war ein Erfolg.");
            runs++;
            if (!game.Inbox(me).Any(m => m.Lines[0] == "Ihr Talentspäher gibt die" && m.Day > startDay))
                silent++;
        }
        Assert.True(silent > 0, $"Kein Erfolg in {runs} Läufen");
    }

    /// <summary>
    /// [ORIGINAL] Datei 0x3EEA2–0x3EF4E: an Monatsmitte und Monatsletztem mit 1/2 neuer Preis 60.000 + 10.000 · random(0, 10).
    /// </summary>
    [Fact]
    public void Price_ChangesOnOfferDays_Between60000And160000()
    {
        var prices = new HashSet<long>();
        for (var seed = 0; seed < 4; seed++)
        {
            var game = TestGames.Create(seed);
            var me = game.ActiveManager;
            for (var guard = 0; guard < 400 && game.Phase == SeasonPhase.Running; guard++)
            {
                var from = game.FinanceDay;
                var price = game.TalentSearchPrice(me);
                game.Continue();
                var offerDays = Enumerable.Range(from + 1, Math.Max(0, game.FinanceDay - from))
                    .Count(d => d > 0 && SponsorRules.IsOfferDay(SeasonCalendar.LayoutDate(d)));
                if (offerDays == 0)
                    Assert.Equal(price, game.TalentSearchPrice(me));
                prices.Add(game.TalentSearchPrice(me));
            }
        }
        Assert.All(prices, p => Assert.True(p is >= 60_000 and <= 160_000 && p % 10_000 == 0, $"Preis {p}"));
        Assert.True(prices.Count >= 6, $"nur {prices.Count} Preise");
    }

    /// <summary>
    /// Mehrsaisonprobe (4 Saisonwechsel, passiv und aktiv): Der aktive Manager schickt den „Spion“, sooft es geht. Jede Suche
    /// ist genau einmal gebucht, der Besitz bleibt bei höchstens 28 (+ Zugänge anderer Wege), alle Talente sind vollständig,
    /// und zwei gleiche Läufe ergeben denselben Stand.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FourSeasonChanges_TalentSearch(bool active)
    {
        string Run()
        {
            var game = TestGames.Create(11);
            var me = game.ActiveManager;
            var club = game.Clubs[me.ClubIndex];
            var log = new List<string>();
            long paid = 0;
            var starts = 0;
            for (var change = 0; change < 4; change++)
            {
                for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
                {
                    Assert.True(guard < 1000, "Saisonende nicht erreicht");
                    me.Money = Math.Max(me.Money, 1_000_000);
                    TestSeason.KeepSquad(game, me);
                    if (active)
                    {
                        var price = game.TalentSearchPrice(me);
                        if (game.StartTalentSearch(me) == TalentSearchStart.Started)
                        {
                            paid += price;
                            starts++;
                        }
                    }
                    var squad = club.Players.ToList();
                    game.Continue();
                    foreach (var talent in club.Players.Except(squad).Where(p => p.Age is >= 18 and <= 21 && p.ContractYears is 2 or 3))
                        Assert.Empty(PlayerFactory.Validate(club, talent));
                }
                Assert.Equal(-paid, game.Finance(me).Book.Current(FinanceCategory.Talentsuche));
                paid = 0;
                game.StartNextSeason();
                log.Add($"{change}:{club.Players.Count}:{game.OwnedPlayers(me)}:{game.TalentSearchPrice(me)}:{starts}");
            }
            if (active)
                Assert.True(starts >= 4, $"nur {starts} Suchen");
            else
                Assert.Equal(0, starts);
            Assert.DoesNotContain(game.Inbox(me), m => m.Lines[0] == "Ihr Talentspäher gibt die" && !active);
            return string.Join(";", log);
        }

        Assert.Equal(Run(), Run());
    }
}

public class TalentSearchMigrationTests
{
    /// <summary>
    /// [APPROX] Schema 25 kennt weder den Preis je Manager noch den Takt zweimal je Woche: Preis 90.000 DM, Restzähler =
    /// 2 · Restwochen.
    /// </summary>
    [Fact]
    public void Schema25_GetsStartPrice_AndTwiceTheWeeks()
    {
        var game = TestGames.Create();
        var me = game.ActiveManager;
        me.Money = 1_000_000;
        Assert.Equal(TalentSearchStart.Started, game.StartTalentSearch(me));
        game.Finance(me).TalentSearch!.TicksLeft = 6;
        var json = System.Text.Json.Nodes.JsonNode.Parse(SaveGame.Save(game))!.AsObject();
        json["schema"] = 25;
        var finance = json["manager"]![0]!["FinanceData"]!.AsObject();
        Assert.True(finance.Remove("TalentPrice"));
        var search = finance["TalentSearch"]!.AsObject();
        Assert.True(search.Remove("TicksLeft"));
        search["WeeksLeft"] = 3;

        var result = SaveGame.Load(System.Text.Encoding.UTF8.GetBytes(json.ToJsonString()));
        Assert.True(result.Success, result.Error);
        Assert.Equal(SaveGame.Save(game), SaveGame.Save(result.Game!));
    }
}
