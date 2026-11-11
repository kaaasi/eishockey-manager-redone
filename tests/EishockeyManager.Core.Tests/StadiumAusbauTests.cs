using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur geld, Teil 1 (Runde 11, G1; Inventur A041–A043, A047, F090): Stadionausbau nach em.exe Datei 0x3D7F8 (Ausbau im
/// Stadionbildschirm) und Datei 0x3D316 (wöchentlicher Baufortschritt), Belege in docs/analyse/re-geld.md „Stadion“.
/// Die ersten vier Tests nutzen nur die bisherige Schnittstelle (StartConstruction, Finanzbuch, Stufen) und waren auf dem
/// Base-Stand rot.
/// </summary>
public class StadiumAusbauTests
{
    /// <summary>Startkonto reicht nicht für jeden Ausbau (Konto ≥ Kosten, Datei 0x3E107); die Proben setzen es hoch.</summary>
    private const long Rich = 50_000_000;

    private static readonly int[] PossibleDiscounts = [.. Enumerable.Range(0, 11)];

    private static void AssertDiscounted(long fullCost, long booked) =>
        Assert.Contains(-booked, PossibleDiscounts.Select(r => fullCost - fullCost * r / 100));

    /// <summary>A041: Kosten (Ziel − jetzt) · K, K aus DGROUP 0xC0 (Kino 520.000 DM je Stufe), linear; gebucht nach Rabatt.</summary>
    [Fact]
    public void Cost_IsTargetMinusCurrentTimesTable()
    {
        var game = TestGames.Create();
        var me = game.ActiveManager;
        me.Money = Rich;
        Assert.Equal(0, game.Finance(me).Stadium.Level(StadiumFeature.Kino));
        Assert.True(game.StartConstruction(me, StadiumProject.Feature, StadiumFeature.Kino, 2));
        AssertDiscounted(2 * 520_000, game.Finance(me).Book.Current(FinanceCategory.Bau));
    }

    /// <summary>A043: Plätze in Schritten zu 500, Sitzplätze 180.000 DM, Stehplätze 135.000 DM je Schritt.</summary>
    [Fact]
    public void Places_CostPer500AsInTable()
    {
        var game = TestGames.Create();
        var me = game.ActiveManager;
        me.Money = Rich;
        Assert.True(game.StartConstruction(me, StadiumProject.Sitzplaetze, default, 2));
        AssertDiscounted(2 * 180_000, game.Finance(me).Book.Current(FinanceCategory.Bau));
    }

    /// <summary>A047: je Projekt eine Baustelle, alle Projekte gleichzeitig; jedes wird fertig und gemeldet.</summary>
    [Fact]
    public void SeveralProjects_BuildInParallel()
    {
        // Spur pool (#743, R15-E2 = C): Die Zeitleiste hält nicht an jedem Tag, und der Nachrichtenspeicher räumt nach drei Tagen;
        // eine Meldung ohne Halt in diesem Fenster sammelt MessageLog nicht. Saat 3 hatte den Halt nur zufällig (mit den Managerkadern
        // im Pool: Zustand am Tag 41 fertig, nächster Halt Tag 45). Saat 1 mit Halt nach jeder der drei Meldungen.
        var game = TestGames.Create(1);
        var me = game.ActiveManager;
        me.Money = Rich;
        var stadium = game.Finance(me).Stadium;
        var seats = stadium.Seats;
        var standing = stadium.Standing;
        var zustand = stadium.Level(StadiumFeature.Zustand);
        Assert.True(game.StartConstruction(me, StadiumProject.Feature, StadiumFeature.Zustand, zustand + 1));
        Assert.True(game.StartConstruction(me, StadiumProject.Sitzplaetze, default, 1));
        Assert.True(game.StartConstruction(me, StadiumProject.Stehplaetze, default, 1));
        for (var i = 0; i < 80 && (stadium.Seats == seats || stadium.Standing == standing || stadium.Level(StadiumFeature.Zustand) == zustand); i++)
            TestSeason.PlayMatchDay(game);
        Assert.Equal(zustand + 1, stadium.Level(StadiumFeature.Zustand));
        Assert.Equal(seats + 500, stadium.Seats);
        Assert.Equal(standing + 500, stadium.Standing);
        Assert.Equal(3, MessageLog.For(game).All.Count(m => m.Lines[0].StartsWith("Der Stadion-Ausbau (", StringComparison.Ordinal)));
    }

    /// <summary>A042: Verkehrsanbindung 40–60 Wochen (± 2), also nach 15 Bauwochen sicher noch nicht fertig (vorher 6 Wochen).</summary>
    [Fact]
    public void LongProject_IsNotDoneAfterFifteenWeeks()
    {
        var game = TestGames.Create();
        var me = game.ActiveManager;
        me.Money = Rich;
        var stadium = game.Finance(me).Stadium;
        var level = stadium.Level(StadiumFeature.Verkehrsanbindung);
        var startDay = game.FinanceDay;
        Assert.True(game.StartConstruction(me, StadiumProject.Feature, StadiumFeature.Verkehrsanbindung, level + 1));
        for (var guard = 0; Mondays(startDay, game.FinanceDay) < 15; guard++)
        {
            Assert.True(guard < 100 && !game.RegularSeasonOver, "15 Bauwochen nicht erreicht");
            TestSeason.PlayMatchDay(game);
        }
        Assert.Equal(level, stadium.Level(StadiumFeature.Verkehrsanbindung));
    }

    private static int Mondays(int fromExclusive, int toInclusive) =>
        Enumerable.Range(fromExclusive + 1, Math.Max(0, toInclusive - fromExclusive))
            .Count(d => d > 0 && SeasonCalendar.LayoutDate(d).DayOfWeek == DayOfWeek.Monday);

    /// <summary>
    /// A042: Bauzeit w = random(lo, hi) einmal, Restwochen w + random(0, 4) − 2; Rabatt 0–10 %, mit random(0, 2) = 0 dagegen 0.
    /// Mit festem Zufall über 3.000 Ziehungen: alle Werte in den Spannen, jede Spanne voll ausgeschöpft, Anteil Rabatt 0 nahe
    /// 1/3 + 2/3 · 1/11 ≈ 39,4 %.
    /// </summary>
    [Fact]
    public void Draws_StayInOriginalRanges()
    {
        var random = new GameRandom(11);
        var weeks = new HashSet<int>();
        var left = new HashSet<int>();
        var discounts = new int[11];
        for (var i = 0; i < 3_000; i++)
        {
            var (w, r) = Stadium.DrawOffer(random, StadiumProject.Feature, StadiumFeature.Kino);
            weeks.Add(w);
            discounts[r]++;
            left.Add(Stadium.DrawWeeksLeft(random, w));
        }
        Assert.Equal(Enumerable.Range(14, 5), weeks.Order());
        Assert.Equal(Enumerable.Range(12, 9), left.Order());
        Assert.All(discounts, n => Assert.True(n > 0));
        Assert.InRange(discounts[0] / 3_000.0, 0.36, 0.43);
    }

    /// <summary>
    /// Bauzeiten je Projekt aus DGROUP 0xEC/0xED (Bytes lo, hi), alle 11 Zeilen in Projektreihenfolge (R11G-02; die Tabelle
    /// wurde im Review #455 gegen die Datei geprüft).
    /// </summary>
    [Theory]
    [InlineData(StadiumProject.Feature, StadiumFeature.Ueberdachung, 20, 30)]
    [InlineData(StadiumProject.Feature, StadiumFeature.Anzeigetafel, 12, 17)]
    [InlineData(StadiumProject.Feature, StadiumFeature.Kuehlanlage, 16, 25)]
    [InlineData(StadiumProject.Feature, StadiumFeature.Sitzkomfort, 6, 9)]
    [InlineData(StadiumProject.Feature, StadiumFeature.Zustand, 5, 8)]
    [InlineData(StadiumProject.Feature, StadiumFeature.Verkehrsanbindung, 40, 60)]
    [InlineData(StadiumProject.Feature, StadiumFeature.Gastronomie, 17, 24)]
    [InlineData(StadiumProject.Feature, StadiumFeature.Kino, 14, 18)]
    [InlineData(StadiumProject.Feature, StadiumFeature.Bowlingbahn, 10, 15)]
    [InlineData(StadiumProject.Sitzplaetze, default(StadiumFeature), 12, 17)]
    [InlineData(StadiumProject.Stehplaetze, default(StadiumFeature), 10, 14)]
    public void BuildTime_FromTable(StadiumProject project, StadiumFeature feature, int lo, int hi) =>
        Assert.Equal((lo, hi), Stadium.BuildTime(project, feature));

    /// <summary>
    /// R11G-01: Bauende genau nach den Restwochen. Datei 0x3D340–0x3D365: Restwochen 0 überspringen, 1 abziehen, bei 0 fertig.
    /// Bei n Restwochen ist das Projekt nach n − 1 Wochen noch im Bau und nach der n-ten Woche fertig – nicht eine Woche früher.
    /// </summary>
    [Theory]
    [InlineData(StadiumProject.Feature, StadiumFeature.Kino)]
    [InlineData(StadiumProject.Sitzplaetze, default(StadiumFeature))]
    [InlineData(StadiumProject.Stehplaetze, default(StadiumFeature))]
    public void ConstructionEnds_InTheWeekWhenWeeksLeftReachZero(StadiumProject project, StadiumFeature feature)
    {
        var game = TestGames.Create();
        var me = game.ActiveManager;
        me.Money = Rich;
        var stadium = game.Finance(me).Stadium;
        Assert.True(game.StartConstruction(me, project, feature, stadium.FirstTarget(project, feature)));
        var weeks = stadium.WeeksLeft(project, feature);
        Assert.True(weeks >= 3);
        for (var week = 1; week < weeks; week++)
        {
            Assert.Empty(stadium.AdvanceWeek());
            Assert.True(stadium.IsUnderConstruction(project, feature));
            Assert.Equal(weeks - week, stadium.WeeksLeft(project, feature));
        }
        var done = Assert.Single(stadium.AdvanceWeek());
        Assert.Equal(project, done.Project);
        Assert.False(stadium.IsUnderConstruction(project, feature));
        Assert.Equal(0, stadium.WeeksLeft(project, feature));
        Assert.Empty(stadium.AdvanceWeek());
    }

    /// <summary>Nach „Ja“: Restwochen in [w − 2, w + 2]; fertig genau nach so vielen Bauwochen.</summary>
    [Fact]
    public void WeeksLeft_WithinQuotedTimePlusMinusTwo()
    {
        var game = TestGames.Create();
        var me = game.ActiveManager;
        me.Money = Rich;
        var stadium = game.Finance(me).Stadium;
        var quote = game.QuoteConstruction(me, StadiumProject.Feature, StadiumFeature.Kino, 1)!;
        Assert.InRange(quote.Weeks, 14, 18);
        Assert.True(game.AnswerConstruction(me, quote, accept: true));
        var left = stadium.WeeksLeft(StadiumProject.Feature, StadiumFeature.Kino);
        Assert.InRange(left, quote.Weeks - 2, quote.Weeks + 2);
        Assert.Equal(1, stadium.TargetLevel(StadiumFeature.Kino));
        Assert.Equal(0, stadium.Level(StadiumFeature.Kino));
    }

    /// <summary>
    /// „Nein“: keine Baustelle, kein Geld, Baufirma beleidigt (kein neues Angebot); der Tagesabschluss gibt sie mit
    /// random(0, 10) = 0 wieder frei (Datei 0x3ED19–0x3ED48).
    /// </summary>
    [Fact]
    public void No_OffendsBuilderUntilDailyRandomClearsIt()
    {
        var game = TestGames.Create();
        var me = game.ActiveManager;
        me.Money = Rich;
        var stadium = game.Finance(me).Stadium;
        var money = me.Money;
        var quote = game.QuoteConstruction(me, StadiumProject.Feature, StadiumFeature.Kino, 1)!;
        Assert.True(stadium.IsOffended(StadiumProject.Feature, StadiumFeature.Kino));
        Assert.False(game.AnswerConstruction(me, quote, accept: false));
        Assert.Equal(money, me.Money);
        Assert.False(stadium.IsUnderConstruction(StadiumProject.Feature, StadiumFeature.Kino));
        Assert.Null(game.QuoteConstruction(me, StadiumProject.Feature, StadiumFeature.Kino, 1));
        Assert.NotNull(game.QuoteConstruction(me, StadiumProject.Feature, StadiumFeature.Bowlingbahn, 1)); // andere Projekte frei

        for (var i = 0; i < 40 && stadium.IsOffended(StadiumProject.Feature, StadiumFeature.Kino); i++)
            TestSeason.PlayMatchDay(game);
        Assert.False(stadium.IsOffended(StadiumProject.Feature, StadiumFeature.Kino));
        Assert.NotNull(game.QuoteConstruction(me, StadiumProject.Feature, StadiumFeature.Kino, 1));
    }

    /// <summary>Angebot nur, wenn das Konto die Kosten (vor Rabatt) deckt; sonst kein Zufall, kein Merker.</summary>
    [Fact]
    public void Quote_NeedsMoneyForFullCost()
    {
        var game = TestGames.Create();
        var me = game.ActiveManager;
        var stadium = game.Finance(me).Stadium;
        me.Money = 519_999;
        var rng = game.FinanceRandom.Capture();
        Assert.Null(game.QuoteConstruction(me, StadiumProject.Feature, StadiumFeature.Kino, 1));
        Assert.False(stadium.IsOffended(StadiumProject.Feature, StadiumFeature.Kino));
        Assert.Equal(rng, game.FinanceRandom.Capture());
        me.Money = 520_000;
        Assert.NotNull(game.QuoteConstruction(me, StadiumProject.Feature, StadiumFeature.Kino, 1));
    }

    /// <summary>Sitz- und Stehplätze zusammen höchstens 25.000; eine Platzart im Bau zählt mit der Zielzahl.</summary>
    [Fact]
    public void Places_TogetherAtMost25000()
    {
        var game = TestGames.Create();
        var me = game.ActiveManager;
        me.Money = Rich;
        var stadium = game.Finance(me).Stadium;
        stadium.Seats = 12_000;
        stadium.Standing = 12_000;
        Assert.Equal(2, stadium.MaxTarget(StadiumProject.Sitzplaetze, default));
        Assert.Null(stadium.Cost(StadiumProject.Sitzplaetze, default, 3));
        Assert.True(game.StartConstruction(me, StadiumProject.Stehplaetze, default, 1));
        Assert.Equal(12_500, stadium.StandingTarget);
        Assert.Equal(1, stadium.MaxTarget(StadiumProject.Sitzplaetze, default));
        Assert.True(game.StartConstruction(me, StadiumProject.Sitzplaetze, default, 1));
        Assert.Equal(0, stadium.MaxTarget(StadiumProject.Sitzplaetze, default));
        Assert.False(stadium.CanSelect(StadiumProject.Sitzplaetze, default));
    }

    /// <summary>Höchste Stufe: kein Ausbau mehr (Bowlingbahn hat zwei Stufen).</summary>
    [Fact]
    public void MaxedFeature_CannotBeSelected()
    {
        var stadium = new Stadium();
        stadium.SetLevel(StadiumFeature.Bowlingbahn, 1);
        Assert.False(stadium.CanSelect(StadiumProject.Feature, StadiumFeature.Bowlingbahn));
        Assert.Null(stadium.Cost(StadiumProject.Feature, StadiumFeature.Bowlingbahn, 2));
    }

    /// <summary>
    /// Mehrsaisonprobe (Geld) über vier Saisonwechsel: Der aktive Manager beginnt jeden Spieltag jeden bezahlbaren Ausbau
    /// (mehrere Baustellen parallel), der passive keinen. Geprüft: Konto = Start + Finanzbuch aller Saisons, „Bau“ = Summe der
    /// angenommenen Angebote nach Rabatt, je fertigem Projekt eine Meldung, Stufen fallen nie, Plätze ≤ 25.000.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FourSeasonChanges_ConstructionKeepsLedgerConsistent(bool active)
    {
        var game = TestGames.Create(seed: 5);
        var me = game.ActiveManager;
        me.Money = Rich;
        var stadium = game.Finance(me).Stadium;
        long cumulative = 0;
        var started = 0;
        var startMoney = me.Money;
        var levels = Enumerable.Range(0, Stadium.FeatureCount).Select(i => stadium.Level((StadiumFeature)i)).ToArray();
        for (var year = 0; year < 4; year++)
        {
            long built = 0;
            var keptSet = false;
            for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
            {
                Assert.True(guard < 1000, "Saisonende nicht erreicht");
                if (active)
                    built += StartAffordable(game, me, ref started);
                game.Continue();
                MessageLog.For(game).Collect(game); // der Nachrichtenspeicher räumt nach drei Tagen (R-K6)
                // Integration Runde 11 (+ wettbewerbe-spiel): Endet der Verein als Letzter oder Vorletzter in der Oberliga, setzt
                // em.exe das Konto am 29. April auf 400.000 DM, ohne Buchung (S-K1, Datei 0x562D8; Seed 5 passiv/aktiv in Saison
                // 4). Die neutrale Differenz zählt außerhalb des Finanzbuchs.
                if (!keptSet && game.Phase is SeasonPhase.SeasonOver && game.History.Count > year
                    && GameState.KeptInOberliga(game.History[^1], me.ClubIndex))
                {
                    // WS-05 (Review #468): wie FinanceCalibrationTests – Konto = 400.000 DM + Prämien des 29. April ff.
                    Assert.Equal(GameState.OberligaKeptMoney + game.Finance(me).Book.Current(FinanceCategory.Praemien), me.Money);
                    cumulative += me.Money - (startMoney + cumulative + game.Finance(me).Book.CurrentTotal);
                    keptSet = true;
                }
                Assert.Equal(startMoney + cumulative + game.Finance(me).Book.CurrentTotal, me.Money);
                for (var i = 0; i < Stadium.FeatureCount; i++)
                {
                    Assert.True(stadium.Level((StadiumFeature)i) >= levels[i]);
                    levels[i] = stadium.Level((StadiumFeature)i);
                }
                Assert.True(stadium.Seats + stadium.Standing <= Stadium.MaxPlaces);
            }
            Assert.Equal(SeasonPhase.SeasonOver, game.Phase);
            Assert.Equal(-built, game.Finance(me).Book.Current(FinanceCategory.Bau));
            cumulative += game.Finance(me).Book.CurrentTotal;
            TestSeason.KeepSquad(game, me);
            game.StartNextSeason();
        }
        var reports = MessageLog.For(game).All.Count(m => m.Lines[0].StartsWith("Der Stadion-Ausbau (", StringComparison.Ordinal));
        if (active)
        {
            Assert.True(started >= 5, $"nur {started} Ausbauten");
            // Der Nachrichtenspeicher räumt nach drei Tagen (pixel-marker Teil 3): Eine Meldung, die in einem Lauf mehrerer Tage ohne
            // Zug entsteht, kann vor der Beobachtung verschwinden – nie mehr Meldungen als fertige Projekte.
            Assert.InRange(reports, 1, started - stadium.Constructions.Count());
        }
        else
        {
            Assert.Equal(0, started);
            Assert.Equal(0, reports);
        }
    }

    private static long StartAffordable(GameState game, Manager me, ref int started)
    {
        long built = 0;
        var stadium = game.Finance(me).Stadium;
        foreach (var (project, feature) in Enumerable.Range(0, Stadium.FeatureCount)
                     .Select(i => (StadiumProject.Feature, (StadiumFeature)i))
                     .Append((StadiumProject.Sitzplaetze, default)).Append((StadiumProject.Stehplaetze, default)))
        {
            // Reserve für Gehälter: nur bauen, solange 2 Mio. DM übrig bleiben.
            if (!stadium.CanSelect(project, feature)
                || stadium.Cost(project, feature, stadium.FirstTarget(project, feature)) is not { } cost || me.Money - cost < 2_000_000)
                continue;
            var quote = game.QuoteConstruction(me, project, feature, stadium.FirstTarget(project, feature))!;
            Assert.True(game.AnswerConstruction(me, quote, accept: true));
            built += quote.DiscountedCost;
            started++;
        }
        return built;
    }
}

/// <summary>Migration Schema 22 → 23 (Spur geld, Teil 1): eine laufende Baustelle des alten Formats bleibt erhalten.</summary>
public class StadiumAusbauMigrationTests
{
    [Theory]
    [InlineData("Feature", "Kino", 2, 5)]
    [InlineData("Sitzplaetze", "Ueberdachung", 2, 3)]
    [InlineData("Stehplaetze", "Ueberdachung", 1, 1)]
    [InlineData(null, null, 0, 0)]
    public void Schema22_OldConstruction_IsMigrated(string? project, string? feature, int target, int weeks)
    {
        var game = TestGames.Create();
        var me = game.ActiveManager;
        var stadium = game.Finance(me).Stadium;
        var json = System.Text.Json.Nodes.JsonNode.Parse(SaveGame.Save(game))!.AsObject();
        json["schema"] = 22;
        var node = json["manager"]![0]!["FinanceData"]!["Stadium"]!.AsObject();
        foreach (var key in new[] { "SeatsTarget", "StandingTarget", "_offended", "_targets", "_weeksLeft" })
            Assert.True(node.Remove(key));
        node["Construction"] = project is null ? null : new System.Text.Json.Nodes.JsonObject
        {
            ["$id"] = 999_999, ["Cost"] = 1, ["Feature"] = feature, ["Project"] = project, ["Target"] = target, ["WeeksLeft"] = weeks,
        };

        var result = SaveGame.Load(System.Text.Encoding.UTF8.GetBytes(json.ToJsonString()));
        Assert.True(result.Success, result.Error);
        var loaded = result.Game!.Finance(result.Game.ActiveManager).Stadium;
        switch (project)
        {
            case "Feature":
                Assert.Equal(target, loaded.TargetLevel(StadiumFeature.Kino));
                Assert.Equal(weeks, loaded.WeeksLeft(StadiumProject.Feature, StadiumFeature.Kino));
                break;
            case "Sitzplaetze":
                Assert.Equal(stadium.Seats + 500 * target, loaded.SeatsTarget);
                Assert.Equal(weeks, loaded.WeeksLeft(StadiumProject.Sitzplaetze, default));
                break;
            case "Stehplaetze":
                Assert.Equal(stadium.Standing + 1_000 * target, loaded.StandingTarget);
                Assert.Equal(weeks, loaded.WeeksLeft(StadiumProject.Stehplaetze, default));
                break;
            default:
                Assert.Equal(SaveGame.Save(game), SaveGame.Save(result.Game));
                break;
        }
        Assert.Equal(project is null ? 0 : 1, loaded.Constructions.Count());
    }
}
