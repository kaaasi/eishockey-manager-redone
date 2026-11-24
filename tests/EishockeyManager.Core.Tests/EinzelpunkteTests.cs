using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>Spur <c>einzelpunkte</c> (Runde 22, Welle 2): kleine Befunde der Liste V, je Befund eine Fundstelle in em.exe.</summary>
public class EinzelpunkteTests
{
    /// <summary>
    /// A104 (Review #884 P2-2), Datei 0x4DB6: Gegentore und Minuten gehen an die ersten Torhüter des Vereins in
    /// <b>Poolreihenfolge</b>, nicht an den aufgestellten. Vorher bekam der aufgestellte Torhüter die Zähler zuerst.
    /// </summary>
    private static int PoolIndex(TransferMarket market, Player p) =>
        market.Pool.FirstOrDefault(s => ReferenceEquals(s.Player, p))?.Index ?? int.MaxValue;

    [Fact]
    public void ComputerClub_GoalsAgainstGoToFirstGoalieInPoolOrder()
    {
        var game = TestGames.Create();
        var club = game.Clubs.First(c => c.Index < MatchEngine.FirstForeignClub
            && !game.Managers.Any(m => m.ClubIndex == c.Index) && c.Players.Count(p => p.Role == PlayerRole.Goalkeeper) >= 2);
        var goalies = club.Players.Where(p => p.Role == PlayerRole.Goalkeeper).ToList();
        var market = game.TransferMarket;
        var inPool = goalies.OrderBy(p => PoolIndex(market, p)).ToList();
        // Der Aufgestellte (hier: ein anderer als der erste im Pool) darf nicht bevorzugt werden.
        var other = goalies.First(p => !ReferenceEquals(p, inPool[0]));
        club.Lineup.Place(LineupSlot.Goalkeeper, other);
        var opponent = game.Clubs.First(c => c.League == club.League && c != club);

        var random = new Random(3);
        for (var i = 0; i < 200; i++)
            MatchEngine.CreditPlayers(game, club, opponent, MatchCompetition.League, new MatchReport { HomeGoals = 1, AwayGoals = 3 }, random);

        Assert.True(inPool[0].GoalieMinutes > other.GoalieMinutes, $"{inPool[0].GoalieMinutes} / {other.GoalieMinutes}");
        Assert.True(inPool[0].GoalsAgainst > other.GoalsAgainst);
    }

    /// <summary>
    /// A672, Datei 0x42718–0x42777: Ohne freien Torhüter bekommt der Feldspieler aus der Torhütersuche +0x15 = 1 (erste
    /// Abwehrreihe). Vorher blieb er ohne Platz (Position 0) und stand in der Spielrechnung auf der Reserve.
    /// </summary>
    [Fact]
    public void AutoLineup_WithoutGoalie_FieldPlayerStandsInFirstDefenceLine()
    {
        var game = TestGames.Create();
        var club = game.Clubs[game.Managers[0].ClubIndex];
        foreach (var g in club.Players.Where(p => p.Role == PlayerRole.Goalkeeper))
            g.InjuredGames = 5;
        Assert.True(club.ApplyAutoLineup());

        Assert.Null(club.Lineup.Goalkeeper);
        var squad = LiveSquad.Create(club, 1, null);
        // #939 (feldspieler-sechs, R15-E2 = C): Der Feldspieler im Tor ist im Original ein dritter Spieler der Abwehrhälfte in Reihe 1
        // (Gitter Datei 0x40478–0x404D7) und steht jetzt auf dem sechsten Feldspielerplatz statt ohne Platz.
        var standIn = Assert.Single(squad.Players, p => p.Position == 1 && !p.Goalie && club.Lineup.FindSlot(p.Player) is not null
            && club.Lineup.Block(0).All(b => !ReferenceEquals(b, p.Player)));
        Assert.True(club.IsDressed(standIn.Player));
        Assert.Same(standIn.Player, club.Lineup.GoalieStandIn);
        // Nach einem Eingriff von Hand ist er wieder Reserve.
        club.Lineup.ClearAll();
        Assert.Equal(0, club.Lineup.PositionOf(standIn.Player));
    }

    /// <summary>
    /// A847, Datei 0x29D23 (einziger Schreiber), 0x5D351: „eine Saison, ein Manager“ setzt das Managerbyte 0x9744 = 1; die
    /// Bestenliste zählt es als viertes Pokalbyte mit 150 Summenpunkten. Vorher fehlten diese Punkte.
    /// </summary>
    [Fact]
    public void OneSeasonSingleManager_SetsFlag_HighscoreCountsIt()
    {
        var setup = new GameSetup { Duration = GameDuration.EineSaison };
        setup.Managers.Add(new ManagerSetup { Name = "Test", Head = 0, ClubIndex = 5, StartLeague = League.Bundesliga });
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, 3);
        Assert.True(game.Managers[0].OneSeasonFlag);
        var with = HighscoreRules.Points(HighscoreRules.Inputs(game, 0));
        game.Managers[0].OneSeasonFlag = false;
        var without = HighscoreRules.Points(HighscoreRules.Inputs(game, 0));
        Assert.InRange(with - without, 7, 8); // 150 Summenpunkte, geteilt (etwa 7 Punkte)

        var multi = new GameSetup { Duration = GameDuration.EineSaison };
        multi.Managers.Add(new ManagerSetup { Name = "A", Head = 0, ClubIndex = 5, StartLeague = League.Bundesliga });
        multi.Managers.Add(new ManagerSetup { Name = "B", Head = 1, ClubIndex = 6, StartLeague = League.Bundesliga });
        Assert.All(NewGame.Create(TestGames.CreateDatabase(), multi, 3).Managers, m => Assert.False(m.OneSeasonFlag));
    }

    /// <summary>
    /// A492, Datei 0x47CA0 (Knopf 0 → DGROUP 0x375 + 2 · m bzw. 0x37F des Bundestrainers) und 0x48E62 Schritt 5: Die Nationalaufstellung
    /// schaltet „automatisch aufstellen“ nur im Taktikbildschirm ein; beim Verlassen folgt die Neuaufstellung, nur wenn die
    /// Einstellung dort eingeschaltet wurde. Vorher schaltete „A“ in der Aufstellung ein.
    /// </summary>
    [Fact]
    public void NationalCoach_TacticsSwitch_LinesUpOnLeaveOnlyIfSwitchedOn()
    {
        var game = TestGames.Create();
        game.AppointNationalCoach(game.Managers[0]);
        game.AutoNominate();
        var coach = game.NationalCoach!;
        coach.BeginTurn(); // Zug des Bundestrainers: die aktuelle Einstellung (0x37F) gilt
        coach.SwitchOffAutoLineup();
        var slot = LineupSlot.All.First(s => coach.Formation[s] is not null);
        coach.ClearSlot(slot);

        coach.CloseTactics(autoAtOpen: false); // Einstellung aus: nichts
        Assert.Null(coach.Formation[slot]);

        coach.SetAutoLineup(true);
        coach.CloseTactics(autoAtOpen: true); // war schon an: nichts
        Assert.Null(coach.Formation[slot]);

        coach.CloseTactics(autoAtOpen: false); // im Bildschirm eingeschaltet: Neuaufstellung
        Assert.NotNull(coach.Formation[slot]);
    }

    /// <summary>
    /// A421, Datei 0x32650–0x3266B, 0x44CEB–0x44D63: Nach dem letzten Europapokal-Termin (Termin 12) findet die Suche des Originals keinen
    /// Treffer mehr, die Werbebewertung zählt keine Europapokal-Teilnahme. Vorher zählte das Remake Gruppe und Endrunde bis zum
    /// Saisonwechsel.
    /// </summary>
    [Fact]
    public void Participation_Europe_EndsAfterLastDate()
    {
        var game = TestGames.Create();
        var dates = SeasonCalendar.DaysOf(DayEvents.EuropeanCup).ToList();
        var inCup = game.EuropeanCup!.Draw.ToList();
        Assert.NotEmpty(inCup);
        Assert.Contains(inCup, c => game.CurrentParticipation(c).Europe); // vor dem ersten Termin: Gruppe zählt
        TestSeason.PlayRegularSeason(game);
        Assert.True(game.Day > dates[^1]);
        Assert.All(game.Clubs, c => Assert.False(game.CurrentParticipation(c.Index).Europe));
    }

    /// <summary>
    /// A671, Datei 0x56287–0x56334 (Prüfung Vereinsfeld 0x16 ohne Modusprüfung, Datei 0x296EC; Probe FILE20 Verein 7): Im Modus „nur
    /// Weltkugel“ setzt der Saisonwechsel das Konto von Satz 0 auf 400.000 DM, wenn sein Verein in der Oberliga Letzter bleibt.
    /// Vorher blieb das Konto stehen.
    /// </summary>
    [Fact]
    public void WorldOnly_ManagerClubKeptInOberliga_AccountSetTo400k()
    {
        const int club = 35; // letzter Platz der Oberliga in der Startordnung (im Modus ohne Vereinssaison werden keine Spiele gerechnet)
        var game = GameModeTests.Create(41, flag: false, globe: true, club);
        Assert.True(game.WorldOnly);
        game.Managers[0].Money = 5_000_000;
        for (var guard = 0; game.History.Count == 0; guard++)
        {
            Assert.True(guard < 3000, "Saisonende nicht erreicht");
            game.Managers[0].Money = 5_000_000;
            game.Continue();
        }
        Assert.Contains(club, game.History[^1].LeftOberliga);
        Assert.Equal(GameState.OberligaKeptMoney, game.Managers[0].Money);
    }

    /// <summary>
    /// Review #943 P3-4 (Verdacht, beantwortet): Das Original nimmt den Zweig 0x56238 nur bei freiem Statusbit 0x04 (Zwangsabstieg). Das Remake
    /// prüft das Bit nicht, weil es im Zweig nie gesetzt sein kann: Im Modus „nur Weltkugel“ gibt es keinen Zwangsabstieg
    /// (<c>ApplyForcedRelegation</c> endet dort sofort), und Oberligisten steigen nie zwangsweise ab (Ligaprüfung davor). Selbst mit
    /// tiefen Schulden bleibt das Ergebnis gleich: Konto 400.000 DM, kein Zwangsabstiegsereignis.
    /// </summary>
    [Fact]
    public void WorldOnly_ManagerInDebt_NoForcedRelegation_SameAsWithoutBit()
    {
        const int club = 35;
        var game = GameModeTests.Create(41, flag: false, globe: true, club);
        Assert.True(game.WorldOnly);
        for (var guard = 0; game.History.Count == 0; guard++)
        {
            Assert.True(guard < 3000, "Saisonende nicht erreicht");
            game.Managers[0].Money = -50_000_000;
            game.Continue();
        }
        Assert.Contains(club, game.History[^1].LeftOberliga);
        Assert.Equal(GameState.OberligaKeptMoney, game.Managers[0].Money);
        Assert.All(game.History[^1].Managers, m => Assert.DoesNotContain(SeasonEvent.ForcedRelegation, m.Events));
    }

    /// <summary>
    /// A698, Datei 0x566E4–0x56718, 0x5677A–0x56793, 0x364B7–0x3653A, 0x51115 (Probe FILE14 8257: TEV MIESBACH Platzfeld 0, Ewige Punkte
    /// 15; FILE13: 0): Ein Nachrücker in die Oberliga ohne Ligavergangenheit bekommt 12 − 0 + 3 = 15 Punkte. Vorher 0.
    /// </summary>
    [Fact]
    public void EternalTable_OberligaReplacement_GetsFifteenPoints()
    {
        var game = TestGames.Create();
        TestSeason.PlayRegularSeason(game);
        for (var guard = 0; game.History.Count == 0; guard++)
        {
            Assert.True(guard < 1000, "Saisonende nicht erreicht");
            game.Continue();
        }
        var record = game.History[0];
        var replacements = record.PromotedInto[League.Oberliga].Where(c => game.Clubs[c].StoredTableRank == 0 && !record.LeftOberliga.Contains(c)).ToList();
        var newcomers = replacements.Where(c => c >= 36).ToList();
        Assert.NotEmpty(newcomers); // Nachrücker aus den Vereinen ohne Liga (Plätze 36–41)
        Assert.All(newcomers, c => Assert.Equal(15, game.Eternal(c).Points));
    }

    /// <summary>
    /// Review #943 P3-2: Migration 58 → 59 → 60 → 61 (<c>SaveGame.Migration.Einzelpunkte.cs</c>). Ein gespeicherter Stand nach einer Saison
    /// (Saisonrückblick mit <c>InnerMoves</c>, Werbevertrag, Aufstellungen) verliert die vier Felder und trägt Schema 58; Laden gelingt,
    /// Felder stehen mit den Vorgaben da (<c>OneSeasonFlag</c> false, kein Feldspieler im Tor, nicht kündbar, keine Platzverschiebung),
    /// und Speichern → Laden → Speichern ist byte-gleich (Objektnummern der neuen Liste bleiben stabil). Rückmutation: Migration 60 → 61
    /// ohne <c>Renumber</c> oder ohne <c>InnerMoves</c> → Laden scheitert bzw. der Stand ändert sich beim erneuten Speichern.
    /// </summary>
    [Fact]
    public void Migration_58To61_AddsFieldsWithDefaults_RoundtripStable()
    {
        var game = TestGames.Create(3);
        var ads = game.Ads(game.Managers[0]);
        ads.Contracts.Clear();
        ads.Contracts.Add(Contract(AdSlotKind.Trikot, 1, [], monthsLeft: 60));
        TestSeason.PlayRegularSeason(game);
        for (var guard = 0; game.History.Count == 0; guard++)
        {
            Assert.True(guard < 1000, "Saisonende nicht erreicht");
            game.Continue();
        }
        var root = System.Text.Json.Nodes.JsonNode.Parse(SaveGame.Save(game))!.AsObject();
        Assert.Equal(SaveGame.SchemaVersion, (int)root["schema"]!);
        var removed = new Dictionary<string, int>();
        Strip(root, removed);
        Assert.All(new[] { "OneSeasonFlag", "_sixthSkater", "Cancellable", "InnerMoves" }, k => Assert.True(removed.GetValueOrDefault(k) > 0, k));
        root["schema"] = 58;

        var result = SaveGame.Load(System.Text.Encoding.UTF8.GetBytes(root.ToJsonString()));

        Assert.True(result.Success, result.Error);
        var loaded = result.Game!;
        Assert.All(loaded.Managers, m => Assert.False(m.OneSeasonFlag));
        Assert.All(loaded.Ads(loaded.Managers[0]).Contracts, c => Assert.False(c.Cancellable));
        Assert.NotEmpty(loaded.History);
        Assert.All(loaded.History, r => Assert.Empty(r.InnerMoves));
        Assert.Empty(loaded.ValidatePlayerIdentity());
        var again = SaveGame.Save(loaded);
        var twice = SaveGame.Load(again);
        Assert.True(twice.Success, twice.Error);
        Assert.Equal(again, SaveGame.Save(twice.Game!));
    }

    private static void Strip(System.Text.Json.Nodes.JsonNode? node, Dictionary<string, int> removed)
    {
        switch (node)
        {
            case System.Text.Json.Nodes.JsonObject obj:
                foreach (var key in new[] { "OneSeasonFlag", "_sixthSkater", "Cancellable", "InnerMoves" })
                    if (obj.Remove(key))
                        removed[key] = removed.GetValueOrDefault(key) + 1;
                foreach (var child in obj.Select(kv => kv.Value).ToList())
                    Strip(child, removed);
                break;
            case System.Text.Json.Nodes.JsonArray array:
                foreach (var child in array)
                    Strip(child, removed);
                break;
        }
    }

    private static SponsorContract Contract(AdSlotKind kind, int sponsor, int[] boards, bool cancellable = false, int monthsLeft = 6) => new()
    {
        Kind = kind, Sponsor = sponsor, Boards = boards, MonthlyAmount = 1_000, MonthsLeft = monthsLeft, Cancellable = cancellable,
    };

    /// <summary>
    /// A582, Datei 0x2BAF3/0x2BECD/0x2BB3E: Ein „Kündbar.“-Vertrag (Bit 0x80 der Option) zählt als freie Fläche bzw. freie Bande und wird
    /// beim Annehmen eines Angebots überschrieben. Vorher kannte das Remake das Bit nicht: die Fläche blieb belegt.
    /// </summary>
    [Fact]
    public void CancellableContract_CountsAsFree_AndIsOverwrittenByAcceptedOffer()
    {
        var game = TestGames.Create();
        var manager = game.Managers[0];
        var ads = game.Ads(manager);
        ads.Contracts.Clear();
        ads.Contracts.Add(Contract(AdSlotKind.Trikot, 1, [], cancellable: true));
        ads.Contracts.Add(Contract(AdSlotKind.Bande, 2, [0, 1], cancellable: true));
        ads.Contracts.Add(Contract(AdSlotKind.Bande, 3, [2], cancellable: false));

        Assert.True(ads.IsFree(new AdSlot(AdSlotKind.Trikot)));
        Assert.True(ads.IsFree(new AdSlot(AdSlotKind.Bande, 0)));
        Assert.False(ads.IsFree(new AdSlot(AdSlotKind.Bande, 2)));
        Assert.Equal([0, 1, 3, 4, 5, 6], ads.FreeBoards().ToArray());

        ads.Offers[AdSlotKind.Trikot] = new() { [4] = new SponsorOffer(AdSlotKind.Trikot, 4, 12, 0, 5_000) };
        var accepted = game.AcceptOffer(manager, new AdSlot(AdSlotKind.Trikot), 4);
        Assert.NotNull(accepted);
        Assert.False(accepted!.Cancellable);
        Assert.Single(ads.Contracts, c => c.Kind == AdSlotKind.Trikot);
        Assert.Equal(4, ads.ContractAt(new AdSlot(AdSlotKind.Trikot))!.Sponsor);

        ads.Offers[AdSlotKind.Bande] = new() { [5] = new SponsorOffer(AdSlotKind.Bande, 5, 12, 1, 700) };
        Assert.NotNull(game.AcceptOffer(manager, new AdSlot(AdSlotKind.Bande, 1), 5));
        // Spur saison-geld Teil 2 (F06/A888, R15-E2 = C): [ORIGINAL] das Angebot geht in das erste freie oder kündbare Feld ab Feld 0
        // (Datei 0x2BAD9, 0x2BADE–0x2BB6D), nicht in das angeklickte Feld 1: Feld 0 ist überschrieben, Feld 1 gehört weiter dem
        // kündbaren Vertrag, Feld 2 dem festen.
        Assert.Equal(5, ads.ContractAt(new AdSlot(AdSlotKind.Bande, 0))!.Sponsor);
        Assert.Equal(2, ads.ContractAt(new AdSlot(AdSlotKind.Bande, 1))!.Sponsor);
        Assert.Equal(3, ads.ContractAt(new AdSlot(AdSlotKind.Bande, 2))!.Sponsor);
    }

    /// <summary>
    /// A582, Datei 0x2713F: Beim Aufstieg (Zweig der Aufstiegsprämie 0x270A8) setzt das Original in allen elf Verträgen Bit 0x80; ohne
    /// Aufstieg bleibt es aus. Testaufbau wie SaisonSatzfolgeSaisonendeTests (Verein 12 gewinnt jedes Spiel 9:0).
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Promotion_MarksAllSponsorContractsCancellable(bool promote)
    {
        const int club = 12;
        var setup = new GameSetup { Duration = GameDuration.Unbegrenzt, EuropeAndWorldCup = true };
        setup.Managers.Add(new ManagerSetup { Name = "M0", Head = 0, ClubIndex = club, StartLeague = (League)(club / 12) });
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, 4);
        var ads = game.Ads(game.Managers[0]);
        ads.Contracts.Clear();
        // Mindestens 13 Restmonate: Verträge mit 6 Monaten laufen bis zum Saisonende aus, dann ist die Liste leer und der Test prüft nichts
        // (Review #943 P2-1; Rückmutation MarkContractsCancellable → {} wird rot).
        ads.Contracts.Add(Contract(AdSlotKind.Trikot, 1, [], monthsLeft: 60));
        ads.Contracts.Add(Contract(AdSlotKind.Bande, 2, [0, 1], monthsLeft: 60));

        for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
        {
            Assert.True(guard < 5000, "Saisonende nicht erreicht");
            foreach (var m in game.Managers)
            {
                m.Money = 10_000_000;
                TestSeason.KeepSquad(game, m);
            }
            if (game.RegularSeasonOver)
                game.Continue();
            else
                TestSeason.PlayMatchDay(game);
            if (!promote)
                continue;
            for (var i = 0; i < game.Results.Count; i++)
                if (game.Results[i] is var r && r.Involves(club))
                    game.Results[i] = r.Home == club ? r with { HomeGoals = 9, AwayGoals = 0 } : r with { HomeGoals = 0, AwayGoals = 9 };
            foreach (var group in game.RelegationGroups.Where(g => g.Upper == League.Bundesliga))
                for (var i = 0; i < group.Results.Count; i++)
                    if (group.Results[i] is var r && r.Involves(club))
                        group.Results[i] = r.Home == club ? r with { HomeGoals = 9, AwayGoals = 0 } : r with { HomeGoals = 0, AwayGoals = 9 };
        }

        var events = game.History[^1].Managers.Single().Events;
        Assert.Equal(promote, events.Contains(SeasonEvent.Promotion));
        Assert.NotEmpty(ads.Contracts);
        Assert.All(ads.Contracts, c => Assert.Equal(promote, c.Cancellable));
    }

    /// <summary>
    /// #897, Datei 0x57F96/0x57C64: Zeile 3 der Finanzübersicht = Σ Darlehen mit den anderen Managern (vorzeichenbehaftet), Zeile 4 =
    /// Σ ihrer Monatszinsen (Darlehen · 25 / 1000, je Satz abgeschnitten). Vorher Forderungen bzw. Schulden getrennt.
    /// </summary>
    [Fact]
    public void FinanceOverview_ManagerRows_AreSignedSumAndInterest()
    {
        var setup = new GameSetup();
        for (var i = 0; i < 3; i++)
            setup.Managers.Add(new ManagerSetup { Name = $"M{i}", Head = i, ClubIndex = 2 + 3 * i });
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, 3);
        var me = game.Managers[0];
        game.LendBetweenManagers(lender: 0, borrower: 1, 50_000);  // M0 hat 50.000 verliehen
        game.LendBetweenManagers(lender: 2, borrower: 0, 30_100);  // M0 schuldet M2 30.100

        var v = game.FinanceOverviewLoans(me);
        Assert.Equal(50_000 - 30_100, v.ManagerLoans);
        Assert.Equal(50_000 * 25 / 1000 + (-30_100) * 25 / 1000, v.ManagerInterest); // 1.250 − 752 (−752,5 abgeschnitten)
        Assert.Equal(498, v.ManagerInterest);
    }

    /// <summary>
    /// #897, Datei 0x57F96/0x57C64 (Satz 4): Zeile 6 = Darlehen Bank als ein Saldo, Zeile 7 = Monatszins (Saldo · 1 / 100, bei
    /// negativem Saldo · 4 / 100). Vorher Sparbuch und Kredit als zwei Zeilen.
    /// </summary>
    [Theory]
    [InlineData(100_000L, 0L, 100_000L, 1_000L)]
    [InlineData(0L, 100_000L, -100_000L, -4_000L)]
    [InlineData(50_000L, 70_000L, -20_000L, -800L)]
    [InlineData(0L, 0L, 0L, 0L)]
    public void FinanceOverview_BankRows_AreBalanceAndInterest(long savings, long credit, long loan, long interest)
    {
        var game = TestGames.Create();
        var bank = game.Finance(game.Managers[0]).Bank;
        (bank.Savings, bank.Credit) = (savings, credit);

        var v = game.FinanceOverviewLoans(game.Managers[0]);
        Assert.Equal(loan, v.BankLoan);
        Assert.Equal(interest, v.BankInterest);
        Assert.Equal(0, v.ManagerLoans); // ein Manager: keine anderen Sätze
    }

    /// <summary>
    /// A665, Datei 0x5636D (Platztausch 0x36169): Steht der Zwangsabsteiger (Verein 3, Bundesliga) schon auf Platz 4 der Gruppentabelle und
    /// überschreibt Platz 3, steht er doppelt in den Plätzen 3/4. Der Tausch setzt dann Verein 12 (Zweiter) auf den Platz von 3, Verein 11
    /// (Erster) auf den Platz von 12 – ein Verein der 2. Liga wechselt innerhalb der Liga – und 3 auf den Platz von 11. Die Startordnung der 2. Liga
    /// war vorher [10, 11, 3, 13] (12 → 3), richtig ist [10, 3, 11, 13].
    /// </summary>
    [Fact]
    public void ForcedRelegationAlreadyInBottomPlaces_MovesClubWithinLowerLeague()
    {
        int[] places = [11, 12, 5, 3]; // Verein 11 und 12 der 2. Liga vorn, Bundesligisten 5 und 3 hinten
        GameState.InsertForcedRelegation(places, 0, 3, c => c < 10);
        Assert.Equal([11, 12, 3, 3], places); // Platz 3 mit dem Zwangsabsteiger überschrieben, er steht jetzt doppelt

        var moves = GameState.InnerMoves(places, c => c < 10);
        Assert.Equal([(11, 3), (12, 11)], moves);

        var (promoted, relegated) = GameState.RelegationExchange(places, c => c < 10);
        Assert.Equal([12], promoted);
        Assert.Equal([3], relegated);

        var record = new SeasonRecord
        {
            Season = 1,
            FinalTables = new Dictionary<League, IReadOnlyList<int>>
            {
                [League.Bundesliga] = Enumerable.Range(0, 4).ToList(),
                [League.ZweiteLiga] = new[] { 10, 11, 12, 13 },
                [League.Oberliga] = new[] { 20, 21, 22, 23 },
            },
        };
        record.PromotedInto[League.Bundesliga].AddRange(promoted);
        record.RelegatedInto[League.ZweiteLiga].AddRange(relegated);
        record.InnerMoves.AddRange(moves.Select(m => new InnerMove(League.ZweiteLiga, m.Slot, m.Club)));

        Assert.Equal([10, 3, 11, 13], LeagueTable.SuccessorOrder(record, League.ZweiteLiga, [10, 11, 3, 13]));
        // Ohne Sonderfall (Zwangsabsteiger nicht unten) bleibt es bei den Paaren: keine Platzverschiebung gespeichert.
        Assert.Empty(GameState.InnerMoves([11, 12, 5, 3], c => c < 10));
    }
}
