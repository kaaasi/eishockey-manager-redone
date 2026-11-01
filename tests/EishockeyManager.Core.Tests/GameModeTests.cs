using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur wettbewerbe-weltkugel: Spielmodus der Spielvorbereitung (DGROUP 0xD0B9) und der Modus „nur Weltkugel“.
/// Befund vorher: Das Remake deutete den linken Knopf als „DEB-Pokal an/aus“; ohne Fahne lief die Liga weiter,
/// einen Bundestrainer gab es nur über einen Testaufruf.
/// </summary>
public class GameModeTests
{
    internal static GameState Create(int seed, bool flag, bool globe, params int[] clubs)
    {
        var setup = new GameSetup();
        foreach (var (club, i) in clubs.Select((c, i) => (c, i)))
            setup.Managers.Add(new ManagerSetup { Name = $"M{i}", Head = i, ClubIndex = club });
        if (setup.ClubCompetitions != flag)
            Assert.True(setup.ToggleClubCompetitions());
        if (setup.EuropeAndWorldCup != globe)
            Assert.True(setup.ToggleEuropeAndWorldCup());
        return NewGame.Create(TestGames.CreateDatabase(), setup, seed);
    }

    /// <summary>
    /// Fingerabdruck einer Partie über mehrere Saisons: Titel, Kontostände, Zustand der Zufallsquellen.
    /// Dient als Referenzwert für den Modus 4 (vor und nach der Spur gleich).
    /// </summary>
    internal static string Fingerprint(GameState game, int seasons, Action<GameState>? eachDay = null)
    {
        var parts = new List<string>();
        for (var s = 0; s < seasons; s++)
        {
            for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
            {
                Assert.True(guard < 3000, "Saisonende nicht erreicht");
                eachDay?.Invoke(game);
                game.Continue();
            }
            var record = game.History[^1];
            parts.Add(string.Join(",", record.Titles.OrderBy(t => t.Key).Select(t => $"{t.Key}={t.Value}")));
            parts.Add(string.Join(",", game.Managers.Select(m => m.Money)));
            if (game.Phase == SeasonPhase.GameOver)
                break;
            game.StartNextSeason();
        }
        parts.Add($"R{game.Random.Next()} C{game.CupRandom.Next()}");
        return string.Join("|", parts);
    }

    // ---- (1) Spielmodus aus der Spielvorbereitung ------------------------------------------------------------

    /// <summary>
    /// [ORIGINAL] Datei 0x29838 ff.: Bits der Knöpfe (Bit 0 Fahne, Bit 1 Weltkugel) → DGROUP 0xD0B9:
    /// 1 → 2, 3 → 4, 2 → 1; 0 → 0 (ohne Modus nimmt „Los jetzt!“ keinen Klick an, Datei 0x295DF).
    /// </summary>
    [Theory]
    [InlineData(true, true, 3, GameMode.Full)]
    [InlineData(true, false, 1, GameMode.ClubOnly)]
    [InlineData(false, true, 2, GameMode.WorldOnly)]
    public void Mode_FromButtons_LikeOriginal(bool flag, bool globe, int bits, GameMode mode)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "A", Head = 0 });
        if (!flag) Assert.True(setup.ToggleClubCompetitions());
        if (!globe) Assert.True(setup.ToggleEuropeAndWorldCup());
        Assert.Equal(bits, setup.ModeBits);
        Assert.Equal(mode, setup.Mode);
        Assert.Equal((byte)(bits switch { 1 => 2, 2 => 1, _ => 4 }), (byte)setup.Mode!.Value);
        Assert.True(setup.CanStart);
    }

    [Fact]
    public void Mode_Defaults_BothButtons_Mode4()
    {
        var setup = new GameSetup();
        Assert.Equal(3, setup.ModeBits); // [ORIGINAL] Datei 0x28191: Vorgabe 3
        Assert.Equal(GameMode.Full, setup.Mode);
    }

    [Fact]
    public void Mode_NoButton_CannotStart()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "A", Head = 0 });
        setup.ToggleClubCompetitions();
        setup.ToggleEuropeAndWorldCup();
        Assert.Equal(0, setup.ModeBits);
        Assert.Null(setup.Mode);
        Assert.False(setup.CanStart);
    }

    /// <summary>[ORIGINAL] Datei 0x28717 ff.: Die Fahne lässt sich nur mit höchstens einem Manager umschalten.</summary>
    [Fact]
    public void Flag_LockedWithSeveralManagers()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "A", Head = 0 });
        setup.Managers.Add(new ManagerSetup { Name = "B", Head = 1 });
        Assert.False(setup.ToggleClubCompetitions());
        Assert.True(setup.ClubCompetitions);
        Assert.True(setup.ToggleEuropeAndWorldCup());
        Assert.False(setup.EuropeAndWorldCup);
    }

    [Theory]
    [InlineData(true, true, true, true, true)]
    [InlineData(true, false, true, false, false)]
    [InlineData(false, true, false, false, true)]
    public void Competitions_FollowMode(bool flag, bool globe, bool deb, bool europe, bool world)
    {
        var game = Create(3, flag, globe, 2);
        Assert.Equal(deb, game.DebCup is not null);
        Assert.Equal(europe, game.EuropeanCup is not null);
        Assert.Equal(world, game.WorldCup is not null);
    }

    // ---- (2) Modus „nur Weltkugel“: Saison über Continue() ohne Liga und Vereinsbuchungen ------------------

    /// <summary>
    /// Regressionstest (vor dem Fix rot): [ORIGINAL] Datei 0x54CEE ff. – im Modus 1 springt der Tageszähler ohne
    /// Tagesverarbeitung bis zum ersten Termin mit Ereignis 0x10 (WM), Manager 0 wird Bundestrainer
    /// (DGROUP 0xD0BA, Etat 0x554A6). Keine Liga, keine Buchungen des Vereins; die WM läuft vollständig.
    /// </summary>
    [Fact]
    public void WorldOnly_SeasonRunsThroughWorldCup_WithoutLeagueOrClubBookings()
    {
        var game = Create(7, false, true, 2);
        var manager = game.Managers[0];
        var money = manager.Money;
        Assert.Equal(GameMode.WorldOnly, game.Mode);
        Assert.Null(game.NationalCoach);
        Assert.Equal(0, game.CurrentDay);

        // Erstes „und tschüß“: Sprung zum ersten WM-Termin, Ernennung, Halt vor dem ersten Spiel.
        var first = game.Continue();
        var firstWorldCupDay = SeasonCalendar.DaysOf(DayEvents.WorldCup)[0];
        Assert.Equal(firstWorldCupDay, game.CurrentDay);
        Assert.DoesNotContain(first, s => s is MatchesStep);
        var coach = Assert.IsType<NationalCoach>(game.NationalCoach);
        Assert.Equal(0, coach.Manager);
        Assert.Equal(NationalTeams.Germany, coach.Nation);
        Assert.Equal(NationalCoach.FirstBudget, game.NationalAccount);
        Assert.Empty(game.WorldCup!.Matches);

        // Kader wählen und die WM durchspielen: Halt an jedem Spieltag Deutschlands.
        game.AutoNominate();
        var steps = new List<SeasonStep>();
        for (var guard = 0; game.Phase == SeasonPhase.Running; guard++)
        {
            Assert.True(guard < 50, "Saisonende nicht erreicht");
            steps.AddRange(game.Continue());
        }
        Assert.Equal(SeasonPhase.SeasonOver, game.Phase);
        var wmDays = steps.OfType<MatchesStep>().Where(s => s.Competition == Competition.WorldCup).Select(s => s.Day).ToList();
        Assert.Equal(SeasonCalendar.DaysOf(DayEvents.WorldCup), wmDays);
        Assert.True(steps.OfType<MatchesStep>().Where(s => s.Competition == Competition.WorldCup)
            .Take(WorldCup.GroupDates).All(s => s.ManagerInvolved), "Deutschland spielt in jeder Gruppenrunde");
        Assert.DoesNotContain(steps, s => s is MatchesStep { Competition: not Competition.WorldCup });
        Assert.Equal(12, game.WorldCup.Ranking.Count);

        // Keine Liga, keine Vereinsbuchungen, kein Ligarückblick.
        Assert.Equal(0, game.PlayedMatchDays);
        Assert.Empty(game.Results);
        Assert.Equal(money, manager.Money);
        Assert.Equal(0, game.Finance(manager).Book.CurrentTotal);
        var record = game.History[^1];
        Assert.Empty(record.Managers);
        Assert.Empty(record.LeftOberliga);
        Assert.All(record.PromotedInto.Values, Assert.Empty);
        Assert.True(record.Titles.ContainsKey(Title.WorldChampion));
        Assert.False(record.Titles.ContainsKey(Title.GermanChampion));
    }

    /// <summary>
    /// Mehrsaisonprobe Modus 1, 4 Saisonwechsel, passiver und aktiver Bundestrainer: Ernennung in jeder Saison,
    /// Nationalkonto = Summe seiner Buchungen, Vereinskonto unverändert, Etat aus dem WM-Platz der Vorsaison.
    /// </summary>
    [Theory]
    [InlineData(false, 7)]
    [InlineData(true, 7)]
    [InlineData(true, 19)]
    public void WorldOnly_MultiSeason_AppointedEachSeason_NationalAccountIsSumOfBookings(bool active, int seed)
    {
        var game = Create(seed, false, true, 5);
        var manager = game.Managers[0];
        var money = manager.Money;
        var expectedBudget = NationalCoach.FirstBudget;
        for (var change = 0; change <= 4; change++)
        {
            Assert.Null(game.NationalCoach);
            game.Continue();
            Assert.NotNull(game.NationalCoach);
            Assert.Equal(change + 1, game.NationalBookings.Count(b => b.Kind == NationalBookingKind.Budget));
            Assert.Equal(expectedBudget, game.NationalBookings.Last(b => b.Kind == NationalBookingKind.Budget).Amount);
            if (active)
            {
                game.AutoNominate();
                for (var opponent = NationalTeams.First; opponent <= NationalTeams.Last; opponent++)
                    if (opponent != NationalTeams.Germany)
                        game.PlayFriendly(opponent, home: opponent % 2 == 0);
                Assert.NotEmpty(game.NationalCoach!.Friendlies);
            }
            while (game.Phase == SeasonPhase.Running)
                game.Continue();
            Assert.Equal(game.NationalAccount, game.NationalBookings.Sum(b => b.Amount));
            Assert.Equal(money, manager.Money);
            Assert.Equal(0, game.PlayedMatchDays);
            expectedBudget = NationalCoach.BudgetFor(game.WorldCup!.Ranking.ToList().IndexOf(NationalTeams.Germany) + 1);
            if (change < 4)
                game.StartNextSeason();
        }
        Assert.Equal(5, game.History.Count);
        if (active)
            Assert.Contains(game.NationalBookings, b => b.Kind == NationalBookingKind.Friendly);
        else
            Assert.Equal(game.NationalAccount, game.NationalBookings.Where(b => b.Kind == NationalBookingKind.Budget).Sum(b => b.Amount));
    }

    // ---- (3) Referenzwerte im Modus 4 unverändert ----------------------------------------------------------

    /// <summary>
    /// Referenzwerte im Modus 4, aufgenommen auf main vor dieser Spur (Base f642ef8): Titel, Kontostände und
    /// Zufallsquellen nach 4 Saisonwechseln, passiver Manager (Verein 2) und aktiver Manager (Verein 30, nimmt Angebote an).
    /// Spur bundestrainer-modus4 (#123) hat sie neu aufgenommen: Verein 2 steht jede Saison unter den ersten fünf, sein
    /// Manager wird am 13. April Bundestrainer (Datei 0x553A6); Deutschland spielt die WM dann mit dessen (leerem) Kader
    /// statt mit ausgewürfelten Werten. Damit ändern sich WM-Ergebnisse und die Pokal-Zufallsquelle (Auslosungen der
    /// Folgesaison, dadurch Pokaltitel und Kontostände ab Saison 2); die Hauptzufallsquelle (R…) bleibt gleich.
    /// Gegenprobe ohne Ernennung ergab die alten Werte unverändert.
    /// Spur finanzen-nachpruefung (Runde 5): Titel und Zufallsquellen unverändert. Manager 0 hat ab Saison 1 100.000 DM
    /// weniger (Verein 2 ist Europapokalsieger; das Original zahlt dafür nichts, em.exe 0x55D86); Manager 1 hat in Saison 5
    /// 5.800 DM weniger (Zuschauerformel 0x3F63C, je Commit getrennt geprüft).
    /// Spur finanzen-werbung (#132–#134): Titel, Hauptzufallsquelle (R…) und Pokal-Zufallsquelle (C…) unverändert; nur
    /// die Kontostände beider Manager verschieben sich (Werbebewertung, Werbeangebote und Monatsabschluss am
    /// Monatsletzten; die tägliche Neuberechnung der Bewertung zieht aus FinanceRandom).
    /// Spur heimspiel-statistik (#136, „Weniger als neun Spieler“, em.exe 0x54420): Ohne Eingriff laufen die Startverträge
    /// von Manager 0 nach 1994 aus; mit leerem Kader zahlt er ab 1995 je Spiel 200.000 DM und wird im Frühjahr 1995
    /// entlassen, die Probe verlöre den Manager (und damit die Ernennung zum Bundestrainer). Manager 0 hält deshalb seinen
    /// Kader (<see cref="TestSeason.KeepSquad"/>: angebotene Verlängerungen zur Forderung, eigener Generator), greift sonst
    /// nicht ein. Ursache je Stelle (Vergleichslauf mit Zustand und allen vier Zufallsquellen nach jedem Schritt): 1993
    /// gleich; 1994 nur das Konto von Manager 0 (+6.500 DM: Gehalt der Verlängerung zur Forderung + 100 statt bisherigem
    /// Gehalt); ab Saisonwechsel 1995 hat Verein 2 19 statt 0 Spieler, zuerst ändert sich die Vertragsquelle (Entscheidungen
    /// zum Saisonwechsel), danach FinanceRandom (Tag 3), Hauptquelle (Tag 7) und Pokalquelle (Tag 21) – ab 1995 neue Titel,
    /// Kontostände und Zufallsstände. Gegenprobe ohne Kaderhalten: 1993/1994 gleich, Manager 0 ab 1995 entlassen.
    /// Spur kader-start (#168, em.exe 0x29DA4): Startwerte der Manager-Kader aus Tabelle DGROUP 0x4B42 (Verein 2: Basis 81
    /// statt 65, Verein 30: 31 statt 55). Vergleichslauf gegen 15d0733 nach jedem Tag: nach dem Start nur beide
    /// Manager-Kader verschieden (Computervereine, Pool, Zufallsquellen gleich); zuerst das Konto von Manager 1 an Tag 3
    /// (Zuschauer aus den Teamwerten, 0x4D316), ab Tag 40 die Spielstärke von Verein 2 (±1), ab Tag 45 die Pokalquelle,
    /// ab Tag 91 die Hauptquelle – neue Titel ab 1993 (Pokalsieger, Weltmeister) und neue Kontostände.
    /// Spur boerse-start (#169, Kursmodell nach em.exe 0x5784A, anderer Verbrauch von FinanceRandom): Titel, Haupt- (R…) und
    /// Pokalquelle (C…) unverändert; nur die Kontostände beider Manager in allen fünf Saisons (Zuschauer und Werbung ziehen
    /// aus FinanceRandom). Mit der Börse nach dem Tagesabschluss aller Manager (#188) erneut nur die Kontostände.
    /// Spur spielberechnung (Teil 3, #186): alle Werte neu – Liga, Pokale und WM rechnen mit der Berechnung des Originals
    /// (<c>MatchEngine.PlayWithEvents</c>): andere Ergebnisse ab dem ersten Spieltag, damit Titel, Kontostände und beide
    /// Zufallsquellen. Ermittelt auf dem Branch (Basis <c>15d0733</c>), Nachziehen in der Integration durch die Leitstelle.
    /// Spur spielerwert (Runde 8): Werbebewertung mit dem Spielerwert nach 0x49D36 – ab der dritten Saison andere Kontostände
    /// beider Manager; Titel, Haupt- und Pokalquelle unverändert. Gegenprobe wie in <c>RngTwinProbeTests</c>.
    /// Spur statistiken (Runde 8, #135, Einzelprämien 0x27184): Titel, Haupt- (R…) und Pokalquelle (C…) unverändert; nur
    /// Kontostände – Manager 0 bekommt 1993 und 1994 je eine Einzelprämie (+100.000 DM, kumuliert +100.000/+200.000 DM ab
    /// Saison 1/2), Manager 1 eine in Saison 5 (+100.000 DM). Gegenprobe mit abgeschalteten Einzelprämien: alte Werte.
    /// Ermittelt auf dem Branch (Basis <c>10eb6c5</c>), Nachziehen in der Integration durch die Leitstelle.
    /// Spur spielstaerke-manager (#204, Lader der Teamwerte 0x2EF1B und Tagesform 0x4F527): alle Werte neu – gestreute
    /// Teamwerte der Computervereine und Züge aus der Hauptquelle ab dem ersten Spiel ändern Ergebnisse, Titel, Konten und
    /// beide Zufallsquellen. Ermittelt auf dem Branch (Basis <c>10eb6c5</c>).
    /// Integration Runde 8 (Leitstelle, auf <c>421d96f</c> mit allen fünf Spuren): Titel, Haupt- und Pokalquelle gleich dem
    /// Branch spielstaerke-manager; nur die Kontostände weichen ab, durch Einzelprämien (statistiken) und Werbebewertung
    /// (spielerwert), die beide keinen Spielverlauf ändern.
    /// spielstaerke-manager-2 (Basis <c>99fa995</c>): Managerpaarungen rechnen mit den Spielern der Managervereine (0x98F8),
    /// Energie und Reihenwechsel je Minute (0x87F7, 0x8EF2, 0x80CB), Seitentausch random(0, 1) und Strafspieler 0x378C5 –
    /// andere Ergebnisse und Zufallszüge ab dem ersten Spiel der Manager, daher neue Titel, Konten und Zufallsquellen.
    /// Integration Runde 8, Welle 3 (Leitstelle, R8W3I-01 #269): alle Werte neu (Ergebnisse der Managerpaarungen). Der Kader
    /// von Manager 0 schrumpft in beiden Ständen gleich (20, 20, 19, 16, dann 13 bzw. 12 Spieler); mit 12 Spielern ist der
    /// Verein 1997 nicht spielfähig (Strafen 9,2 Mio. DM) – daher das negative Konto in Saison 5, kein neuer Geldfehler.
    /// Spur wettbewerbe-komplett Teil 1: Konten ab Saison 2 neu (Manager 0 −4.056 DM in 1994, Manager 1 ab 1996). Ursache: Die
    /// Startordnung der Folgesaison (Platzliste der Vorsaison, 0x5CCB) ändert bei Gleichstand nach dem 1. Spieltag den
    /// Tabellenplatz, der die Zuschauer bestimmt (FinanceStadium); Titel und Zufallsquellen unverändert.
    /// </summary>
    [Fact]
    public void Mode4_ReferenceValues_Unchanged()
    {
        var game = Create(11, true, true, 2, 30);
        var print = Fingerprint(game, 5, g =>
        {
            TestSeason.KeepSquad(g, g.Managers[0]); // Spur heimspiel-statistik (#136), siehe oben
            ActiveSecondManager(g);
        });
        Assert.Equal(Mode4Reference, print);
    }

    internal const string Mode4Reference =
        "WorldChampion=54,CupWinner=24,GermanChampion=1,EuropeanCupWinner=2|2225350,405129|WorldChampion=52,CupWinner=24,GermanChampion=0,EuropeanCupWinner=1|3416286,294365|WorldChampion=52,CupWinner=24,GermanChampion=24,EuropeanCupWinner=1|4149410,83083|WorldChampion=52,CupWinner=25,GermanChampion=0,EuropeanCupWinner=1|4675784,-123270|WorldChampion=52,CupWinner=1,GermanChampion=13,EuropeanCupWinner=1|-6962100,-359761|R409663842 C1843401354";

    /// <summary>Aktiver Manager (wie in den Mehrsaisonproben der Spur kader): verlängert jeden angebotenen Vertrag.</summary>
    internal static void ActiveSecondManager(GameState game)
    {
        var club = game.Clubs[game.Managers[1].ClubIndex];
        foreach (var p in club.Players.Where(p => p.Notice == ContractNotice.Offered && p.ContractYears == 1))
            Contracts.Negotiate(game, game.Managers[1], p, p.NoticeYears, Contracts.Demand(p) + 100, false, demandFilled: true);
    }
}

/// <summary>Spur wettbewerbe-weltkugel ↔ speichern: Spielmodus und Nationalbuch im Spielstand (Schema 3).</summary>
public class GameModeSaveGameTests
{
    [Fact]
    public void WorldOnly_RoundTrip_KeepsModeCoachAndBookings()
    {
        var game = GameModeTests.Create(7, false, true, 2);
        game.Continue();
        game.AutoNominate();
        Assert.Equal(FriendlyResult.Played, game.PlayFriendly(52, home: true));
        var bytes = SaveGame.Save(game);
        var loaded = SaveGame.Load(bytes).Game!;
        Assert.Equal(GameMode.WorldOnly, loaded.Mode);
        Assert.Equal(game.NationalBookings, loaded.NationalBookings);
        Assert.Equal(game.NationalAccount, loaded.NationalBookings.Sum(b => b.Amount));
        Assert.Equal(bytes, SaveGame.Save(loaded));
    }

    /// <summary>Schema 2 → 3: Fahne gedrückt (Schema 2 kannte nur Modi mit Liga), Kontostand als Übertragsbuchung.</summary>
    [Theory]
    [InlineData(0L)]
    [InlineData(275_000L)]
    public void SchemaTwoSave_IsMigrated(long account)
    {
        var game = TestGames.Create(3);
        game.Setup.DebCup = false;
        var root = System.Text.Json.Nodes.JsonNode.Parse(SaveGame.Save(game))!.AsObject();
        root["schema"] = 2;
        var spiel = root["spiel"]!.AsObject();
        spiel.Remove("NationalBookings");
        spiel["Setup"]!.AsObject().Remove("ClubCompetitions");
        spiel["NationalAccount"] = account;

        var result = SaveGame.Load(System.Text.Encoding.UTF8.GetBytes(root.ToJsonString()));
        Assert.True(result.Success, result.Error);
        var loaded = result.Game!;
        Assert.True(loaded.Setup.ClubCompetitions);
        Assert.False(loaded.Setup.DebCup);
        Assert.Equal(GameMode.Full, loaded.Mode);
        Assert.Equal(account, loaded.NationalAccount);
        Assert.Equal(account, loaded.NationalBookings.Sum(b => b.Amount));
        Assert.Equal(account == 0 ? 0 : 1, loaded.NationalBookings.Count);
        var again = SaveGame.Save(loaded);
        Assert.Equal(again, SaveGame.Save(SaveGame.Load(again).Game!));
    }
}
