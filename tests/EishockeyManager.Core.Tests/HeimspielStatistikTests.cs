using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur heimspiel-statistik (Runde 6, #136): Zuschauerstatistik je Heimspiel, „Weniger als neun Spieler“ und die
/// Spielbedeutung der Zuschauerformel. Belege: Kommentare in <c>StadiumStatistics.cs</c> und
/// <c>docs/finanzen-rekonstruktion.md</c>, Abschnitt „Heimspiel-Statistik“. Gegen Base 1e4731b rot.
/// </summary>
public class HeimspielStatistikTests
{
    private static (GameState Game, Manager Me, Club Club) Game(League league = League.Oberliga)
    {
        var setup = new GameSetup();
        var clubIndex = 12 * (int)league;
        setup.Managers.Add(new ManagerSetup { Name = "Test", Head = 0, ClubIndex = clubIndex, StartLeague = league });
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, 3);
        var me = game.ActiveManager;
        return (game, me, game.Clubs[me.ClubIndex]);
    }

    /// <summary>Lässt nur <paramref name="count"/> verfügbare Spieler in der Aufstellung.</summary>
    private static void KeepInLineup(Club club, int count)
    {
        // Teil 8 (A068): weniger Aufgestellte gibt es nur nach einem Eingriff von Hand; der schaltet die automatische Aufstellung ab (Datei 0x40ADE), sonst stellt der Tageslauf wieder voll auf (0x07C07).
        club.AutoLineupEnabled = false;
        foreach (var player in club.Lineup.Players.Skip(count).ToList())
            club.Lineup.Remove(player);
        Assert.Equal(count, club.Lineup.Count);
    }

    private static int HomeLeagueGames(GameState game, Club club) => game.Results.Count(r => r.Home == club.Index);

    [Fact]
    public void LeagueHomeGames_FillTheStatistics()
    {
        var (game, me, club) = Game();
        while (HomeLeagueGames(game, club) < 3)
            TestSeason.PlayMatchDay(game);

        var stats = game.Finance(me).HomeStatistics;
        Assert.Equal(3, stats.Games);
        Assert.Equal(3, stats.Curve.Count);
        Assert.InRange(stats.Record, stats.Minimum, game.Finance(me).Stadium.Capacity);
        Assert.InRange(stats.Average!.Value, stats.Minimum, stats.Record);
        var opponents = game.Results.Where(r => r.Home == club.Index).Select(r => r.Away).ToList();
        Assert.Contains(stats.RecordOpponent, opponents);
        Assert.Contains(stats.MinimumOpponent, opponents);
        Assert.Equal(game.Finance(me).LastHomeGame!.Value.Spectators / 1000, stats.Curve[^1]);
    }

    [Fact]
    public void CupHomeGame_BooksTickets_ButNoStatistics()
    {
        // [ORIGINAL] 0x54658: Pokale, Play-Off-/Aufstiegsrunden und WM übergeben eine Teilnehmerzahl → keine Statistik.
        var (game, me, club) = Game();
        var before = me.Money;
        game.SettleHomeMatch(club.Index, club.Index + 1, 1, importance: 1);

        Assert.True(me.Money > before);
        Assert.Equal(0, game.Finance(me).HomeStatistics.Games);
    }

    [Fact]
    public void Statistics_TieTakesNewOpponent_AndAverageRoundsDown()
    {
        var stats = new StadiumStatistics();
        Assert.Null(stats.Average);
        Assert.Equal(StadiumStatistics.InitialMinimum, stats.Minimum);
        stats.Add(4_500, 7);
        stats.Add(1_046, 8);
        stats.Add(4_500, 9); // Rekord ≤ Zuschauer → neuer Gegner (0x546CE)
        stats.Add(1_046, 10); // Minuskulisse ≥ Zuschauer → neuer Gegner (0x5471B)
        stats.Add(3_000, 11);

        Assert.Equal(4_500, stats.Record);
        Assert.Equal(9, stats.RecordOpponent);
        Assert.Equal(1_046, stats.Minimum);
        Assert.Equal(10, stats.MinimumOpponent);
        Assert.Equal(14_092, stats.Total);
        Assert.Equal(2_818, stats.Average); // 14.092 / 5 = 2.818,4
        Assert.Equal(new[] { 4, 1, 4, 1, 3 }, stats.Curve);

        stats.NewSeason();
        Assert.Equal(0, stats.Games);
        Assert.Equal(0, stats.Total);
        Assert.Equal(0, stats.Record);
        Assert.Equal(StadiumStatistics.InitialMinimum, stats.Minimum);
        Assert.Empty(stats.Curve);
        Assert.Equal(9, stats.RecordOpponent); // Gegner bleiben wie im Original stehen (0x55A86)
    }

    [Fact]
    public void ShortHanded_AtHome_PaysPenalty_NoTickets_NoStatistics()
    {
        var (game, me, club) = Game();
        KeepInLineup(club, 8);
        var before = me.Money;
        game.SettleHomeMatch(club.Index, club.Index + 1, 1);

        Assert.Equal(before - 200_000, me.Money);
        Assert.Equal(-200_000, game.Finance(me).Book.Current(FinanceCategory.Strafen));
        Assert.Equal(0, game.Finance(me).Book.Current(FinanceCategory.Eintritt));
        Assert.Contains(game.Inbox(me), m => m.Lines.Contains("Weniger als neun Spieler!"));
        Assert.Equal(0, game.Finance(me).HomeStatistics.Games);
    }

    [Fact]
    public void ShortHanded_Away_PaysPenaltyToo()
    {
        // [ORIGINAL] 0x547AA: dieselbe Prüfung für den Manager des Gastvereins.
        var (game, me, club) = Game();
        KeepInLineup(club, 8);
        var before = me.Money;
        game.SettleHomeMatch(club.Index + 1, club.Index, 1);

        Assert.Equal(before - 200_000, me.Money);
        Assert.Contains(game.Inbox(me), m => m.Lines.Contains("Weniger als neun Spieler!"));
    }

    [Fact]
    public void ShortHanded_DismissedManager_PaysNoPenalty()
    {
        // [APPROX] Ein entlassener Manager führt den Verein nicht mehr: keine Meldung, kein Abzug (sonst zahlt er nach der
        // Entlassung je Spiel weiter, Mehrsaisonprobe Modus 4). Eintritt wie bisher.
        var (game, me, club) = Game();
        KeepInLineup(club, 8);
        me.Dismissed = true;
        var before = me.Money;
        game.SettleHomeMatch(club.Index + 1, club.Index, 1);
        game.SettleHomeMatch(club.Index, club.Index + 1, 1);

        Assert.True(me.Money >= before);
        Assert.Equal(0, game.Finance(me).Book.Current(FinanceCategory.Strafen));
        Assert.DoesNotContain(game.Inbox(me), m => m.Lines.Contains("Weniger als neun Spieler!"));
    }

    [Fact]
    public void ShortHanded_LeagueMatches_ScoredZeroTwo()
    {
        // [ORIGINAL] #165, Spieltagsablauf 0x36DC5–0x36DE9: Spielabrechnung liefert 1 (Heimmanager nicht spielfähig) →
        // Eintrag 0:2, 2 (Gastmanager) → 2:0, ohne Spielberechnung.
        var (game, _, club) = Game();
        KeepInLineup(club, 8);
        while (game.Results.Count(r => r.Home == club.Index) < 2 || game.Results.Count(r => r.Away == club.Index) < 2)
            TestSeason.PlayMatchDay(game);

        Assert.All(game.Results.Where(r => r.Home == club.Index), r => Assert.Equal((0, 2), (r.HomeGoals, r.AwayGoals)));
        Assert.All(game.Results.Where(r => r.Away == club.Index), r => Assert.Equal((2, 0), (r.HomeGoals, r.AwayGoals)));
        Assert.Equal(8, club.Lineup.Count);
    }

    [Fact]
    public void ShortHanded_CupMatch_LostZeroTwo_WithoutPenaltyShootout()
    {
        // [ORIGINAL] 0x36F9C ff.: dieselbe Wertung für die Spiele mit Teilnehmerzahl (Pokale, Play-Off/Aufstieg).
        var (game, _, club) = Game();
        KeepInLineup(club, 8);
        for (var guard = 0; game.DebCup!.Matches.All(m => !m.Involves(club.Index)); guard++)
        {
            Assert.True(guard < 500, "kein Pokalspiel des Managervereins");
            game.Continue();
        }
        var match = game.DebCup.Matches.First(m => m.Involves(club.Index));
        Assert.Equal(match.Home == club.Index ? (0, 2) : (2, 0), (match.HomeGoals, match.AwayGoals));
        Assert.Null(match.PenaltyWinner);
        Assert.NotEqual(club.Index, match.Winner);
    }

    [Fact]
    public void FullLineup_LeagueMatchesComputedNormally()
    {
        var (game, _, club) = Game();
        KeepInLineup(club, 9);
        while (game.Results.Count(r => r.Involves(club.Index)) < 6)
            TestSeason.PlayMatchDay(game);
        Assert.Contains(game.Results.Where(r => r.Involves(club.Index)),
            r => (r.HomeGoals, r.AwayGoals) is not (0, 2) and not (2, 0));
    }

    [Fact]
    public void NinePlayers_AreEnough_InjuredDoNotCount()
    {
        var (game, me, club) = Game();
        KeepInLineup(club, 9);
        var before = me.Money;
        game.SettleHomeMatch(club.Index, club.Index + 1, 1);
        Assert.True(me.Money > before);

        club.Lineup.Players.First().InjuredGames = 2;
        Assert.True(game.IsShortHanded(club));
    }

    [Theory]
    // [ORIGINAL] 0x545CD–0x54601: Bonus = [Code 6] + 2·[Code 3] + [Code 1]; Europapokal 0x31490: Code 3 für die
    // Termine 0–5 (Gruppen), Code 5 ab Termin 6 (Endrunde, [0x7AF] > 5) → dort kein Bonus.
    [InlineData(0, 2)]
    [InlineData(5, 2)]
    [InlineData(6, 0)]
    [InlineData(11, 0)]
    public void EuropeanCup_FinalRound_HasNoSpectatorBonus(int date, int importance) =>
        Assert.Equal(importance, SpectatorRules.EuropeanCupImportance(date));

    [Fact]
    public void NewSeason_ResetsStatistics()
    {
        var (game, me, club) = Game();
        while (game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver)
        {
            me.Money = 10_000_000;
            game.Continue();
        }
        Assert.True(game.Finance(me).HomeStatistics.Games > 0);
        game.StartNextSeason();
        Assert.Equal(0, game.Finance(me).HomeStatistics.Games);
        Assert.Equal(StadiumStatistics.InitialMinimum, game.Finance(me).HomeStatistics.Minimum);
    }

    /// <summary>
    /// Mehrsaisonprobe über vier Saisonwechsel. Manager 0 passiv (Bundesliga, verlängert nichts), Manager 1 aktiv
    /// (Oberliga: verlängert alle Verträge, Eintritt jede Saison +1 DM, am 5. Ligaspieltag nur acht Aufgestellte).
    /// Je Saison: Heimspiele der Statistik = Ligaheimspiele ohne die Spiele mit Strafe; Strafen = 200.000 DM je Meldung.
    /// <para>
    /// Begründete Änderung: Der passive Manager verliert mit dem Ablauf der Startverträge seinen Kader (bestehendes
    /// Verhalten des Remakes) und zahlt danach wie im Original (0x544C0) 200.000 DM je Spiel, ohne Eintritt und
    /// Statistik. Vorher spielte er mit leerer Aufstellung weiter und kassierte Eintritt.
    /// </para>
    /// </summary>
    [Fact]
    public void MultiSeason_FourChanges_PassiveAndActiveManager()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Passiv", Head = 0, ClubIndex = 2, StartLeague = League.Bundesliga });
        setup.Managers.Add(new ManagerSetup { Name = "Aktiv", Head = 1, ClubIndex = 30, StartLeague = League.Oberliga });
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, 5);
        var passive = game.Managers[0];
        var active = game.Managers[1];
        var firstSeason = game.Season;
        var passiveLostSquad = false;

        int Messages(Manager m) => MessageLog.For(game).Of(m).Count(x => x.Lines.Contains(GameState.ShortHandedMessage));

        for (var change = 0; change < 4; change++)
        {
            var activeClub = game.Clubs[active.ClubIndex];
            var passiveClub = game.Clubs[passive.ClubIndex];
            foreach (var player in activeClub.Players)
                player.ContractYears = Math.Max(player.ContractYears, 2);
            var stadium = game.Finance(active).Stadium;
            stadium.TicketPrice = Math.Min(Stadium.MaxTicketPrice, stadium.TicketPrice + 1);
            var penaltyHomeGames = new Dictionary<Manager, int> { [passive] = 0, [active] = 0 };
            var messagesBefore = game.Managers.ToDictionary(m => m, Messages);

            for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
            {
                Assert.True(guard < 2000, "Saisonende nicht erreicht");
                foreach (var manager in game.Managers)
                    manager.Money = 10_000_000; // keine Entlassung
                var reduce = game.PlayedMatchDays == 4 && !game.RegularSeasonOver;
                if (reduce)
                    KeepInLineup(activeClub, 8);
                var played = game.PlayedMatchDays;
                var shortHanded = game.Managers.ToDictionary(m => m, m => game.IsShortHanded(game.Clubs[m.ClubIndex]));
                if (reduce)
                    TestSeason.PlayMatchDay(game);
                else
                    game.Continue();
                MessageLog.For(game).Collect(game);
                // Spielberechnung Teil 5: Der Tageslauf heilt Verletzte (0x7C12, Teil 3) vor der Prüfung 0x36DC5 desselben
                // Tages; ein vorher nicht spielfähiger Verein kann am Spieltag wieder spielen. Mit dem geänderten
                // Zufallsverbrauch (Penaltyschießen 0x3901B) kommt das hier vor: nur zählen, was auch danach nicht spielfähig ist.
                foreach (var manager in game.Managers)
                    shortHanded[manager] &= game.IsShortHanded(game.Clubs[manager.ClubIndex]);
                foreach (var manager in game.Managers)
                {
                    var step = game.Results.Where(r => r.MatchDay > played && r.MatchDay <= game.PlayedMatchDays
                        && r.Involves(manager.ClubIndex)).ToList();
                    if (shortHanded[manager])
                    {
                        // #165: vor dem Spieltag nicht spielfähig → 0:2 daheim, 2:0 auswärts
                        Assert.All(step, r => Assert.Equal(r.Home == manager.ClubIndex ? (0, 2) : (2, 0), (r.HomeGoals, r.AwayGoals)));
                    }
                    if (shortHanded[manager])
                        penaltyHomeGames[manager] += step.Count(r => r.Home == manager.ClubIndex);
                    // [ORIGINAL] 0x544C0: Die Abrechnung läuft über die Manager in Indexreihenfolge und endet bei der ersten
                    // Strafe – ist der Gast ein nicht spielfähiger Managerverein mit kleinerem Index, bekommt der Heimverein
                    // weder Eintritt noch Statistik. Begründete Änderung (#204, Wanderung der Teamwerte): Mit dem geänderten
                    // Zufallsverlauf steigt der aktive Verein in Saison 4 in die Bundesliga auf und trifft auf den Verein
                    // des passiven Managers, der ohne Kader 2:0 verliert; vorher kam diese Paarung nicht vor.
                    else
                        penaltyHomeGames[manager] += step.Count(r => r.Home == manager.ClubIndex && game.Managers
                            .TakeWhile(o => o != manager).Any(o => o.ClubIndex == r.Away && shortHanded[o]));
                }
                if (reduce)
                    activeClub.AutoLineup();
            }

            foreach (var manager in game.Managers)
            {
                var stats = game.Finance(manager).HomeStatistics;
                var club = game.Clubs[manager.ClubIndex];
                Assert.Equal(HomeLeagueGames(game, club) - penaltyHomeGames[manager], stats.Games);
                Assert.Equal(stats.Games, stats.Curve.Count);
                if (stats.Games > 0)
                {
                    Assert.InRange(stats.Average!.Value, stats.Minimum, stats.Record);
                    Assert.True(stats.Record <= game.Finance(manager).Stadium.Capacity);
                }
                // Nachrichtenspeicher wie im Original (pixel-marker Teil 3): eine Meldung im Lauf mehrerer Tage ohne Zug kann vor der
                // Beobachtung wieder aus dem Speicher laufen (3 Tage, Datei 0x54F07); die Strafen im Finanzbuch zählen vollständig.
                var messages = Messages(manager) - messagesBefore[manager];
                var fines = -game.Finance(manager).Book.Current(FinanceCategory.Strafen) / 200_000L;
                Assert.Equal(0, game.Finance(manager).Book.Current(FinanceCategory.Strafen) % 200_000L);
                Assert.InRange(messages, 0, fines);
            }
            Assert.True(game.Finance(active).Book.Current(FinanceCategory.Strafen) <= -200_000L, "Strafe am 5. Spieltag fehlt");
            Assert.True(game.Finance(active).HomeStatistics.Games > 0);
            if (passiveClub.Players.Count == 0)
            {
                passiveLostSquad = true;
                Assert.Equal(0, game.Finance(passive).HomeStatistics.Games);
                Assert.Equal(0, game.Finance(passive).Book.Current(FinanceCategory.Eintritt));
            }

            game.StartNextSeason();
            Assert.All(game.Managers, m => Assert.Equal(0, game.Finance(m).HomeStatistics.Games));
        }
        Assert.Equal(firstSeason + 4, game.Season);
        Assert.True(passiveLostSquad, "Der passive Manager sollte seinen Kader im Lauf der Probe verlieren.");
    }

    [Fact]
    public void Statistics_SurviveSaveAndLoad()
    {
        var (game, me, club) = Game();
        while (HomeLeagueGames(game, club) < 2)
            TestSeason.PlayMatchDay(game);
        var bytes = SaveGame.Save(game);
        var loaded = SaveGame.Load(bytes);
        Assert.True(loaded.Success, loaded.Error);
        var stats = loaded.Game!.Finance(loaded.Game.ActiveManager).HomeStatistics;
        Assert.Equal(2, stats.Games);
        Assert.Equal(game.Finance(me).HomeStatistics.Record, stats.Record);
        Assert.Equal(bytes, SaveGame.Save(loaded.Game));
    }

    [Fact]
    public void SchemaSevenSave_IsMigrated_WithEmptyStatistics()
    {
        // Ein Spielstand von Schema 7 (finanzen-werbung, Integration Runde 6) kennt das Feld nicht; nachgebildet mit einem Stand ohne Heimspiel (Feld null),
        // damit die Objektnummern fortlaufend bleiben.
        var (game, me, _) = Game();
        game.Finance(me);
        var root = System.Text.Json.Nodes.JsonNode.Parse(SaveGame.Save(game))!.AsObject();
        root["schema"] = 7;
        foreach (var manager in root["manager"]!.AsArray())
            Assert.True(manager!["FinanceData"]!.AsObject().Remove("_homeStatistics"));

        var result = SaveGame.Load(System.Text.Encoding.UTF8.GetBytes(root.ToJsonString()));
        Assert.True(result.Success, result.Error);
        Assert.Equal(SaveGame.Save(game), SaveGame.Save(result.Game!));
        var stats = result.Game!.Finance(result.Game.ActiveManager).HomeStatistics;
        Assert.Equal(0, stats.Games);
        Assert.Equal(StadiumStatistics.InitialMinimum, stats.Minimum);
    }

    [Fact]
    public void SchemaEightSave_IsMigrated_ForfeitFlagFalse()
    {
        // Schema 8 kennt den Merker ForfeitsMatch (#165) nicht; er wird false und vor dem nächsten Spiel neu gesetzt.
        var (game, _, club) = Game();
        TestSeason.PlayMatchDay(game);
        var root = System.Text.Json.Nodes.JsonNode.Parse(SaveGame.Save(game))!.AsObject();
        root["schema"] = 8;
        foreach (var node in root["vereine"]!.AsArray())
            Assert.True(node!.AsObject().Remove("ForfeitsMatch"));

        var result = SaveGame.Load(System.Text.Encoding.UTF8.GetBytes(root.ToJsonString()));
        Assert.True(result.Success, result.Error);
        Assert.Equal(SaveGame.Save(game), SaveGame.Save(result.Game!));

        // Nach dem Laden: weniger als neun Aufgestellte → das nächste Spiel wird gewertet.
        var loaded = result.Game!;
        KeepInLineup(loaded.Clubs[club.Index], 8);
        var before = loaded.Results.Count(r => r.Involves(club.Index));
        while (loaded.Results.Count(r => r.Involves(club.Index)) == before)
            TestSeason.PlayMatchDay(loaded);
        var next = loaded.Results.Last(r => r.Involves(club.Index));
        Assert.Equal(next.Home == club.Index ? (0, 2) : (2, 0), (next.HomeGoals, next.AwayGoals));
    }

    /// <summary>
    /// Karriere-Video ESC Wolfsburg (Oberliga, Level 3, 11 DM, Saison 1: Schnitt 3.049, Minuskulisse 1.046, Rekord
    /// 4.500 = ausverkauft). Ein passiver Lauf erreicht die Form der Statistik (22 Ligaheimspiele, Rekord bis zur
    /// Kapazität 4.500), aber nicht den Schnitt des aktiven Video-Managers (Aufstiegssaison) – exakte Werte hängen am
    /// Zufall und an der Mannschaftsstärke; die Abweichung ist in docs/finanzen-rekonstruktion.md vermerkt.
    /// </summary>
    [GameDataFact]
    public void Wolfsburg_FirstSeason_HasTheShapeOfTheCareerVideo()
    {
        var database = ClubDatabase.Load(TestGameData.Load());
        var wolfsburg = database.Clubs.First(c => c.Name.Contains("Wolfsburg", StringComparison.OrdinalIgnoreCase)).Index;
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Test", Head = 0, ClubIndex = wolfsburg, StartLeague = League.Oberliga, Level = 3 });
        var game = NewGame.Create(database, setup, seed: 1);
        var me = game.ActiveManager;
        game.Finance(me).Stadium.TicketPrice = 11;
        while (!game.RegularSeasonOver)
        {
            me.Money = 10_000_000;
            game.Continue();
        }

        var stats = game.Finance(me).HomeStatistics;
        Assert.Equal(22, stats.Games);
        Assert.Equal(4_500, game.Finance(me).Stadium.Capacity);
        Assert.InRange(stats.Record, stats.Average!.Value, 4_500);
        Assert.InRange(stats.Minimum, 50, stats.Average.Value);
    }

    /// <summary>Belege direkt aus em.exe: Abzug, Meldung, Spielbedeutung und Startwert der Minuskulisse.</summary>
    [GameDataFact]
    public void OriginalBytes_MatchTheRemakeConstants()
    {
        var exe = File.ReadAllBytes(Path.Combine(TestGameData.Load().RootDirectory, "em.exe"));
        // 0x5448D: mov dx,3 / mov ax,0D40h → 0x30D40 = 200.000, · [0x79D] (3) / [0x7A1] (1), sub vom Konto 0x97EA.
        Assert.Equal(new byte[] { 0xba, 0x03, 0x00, 0xb8, 0x40, 0x0d }, exe[0x5448d..0x54493]);
        Assert.Equal(0x30d40, GameState.ShortHandedPenalty);
        // 0x96A8: Managerwert 0x974B = 100 bei weniger als neun (cmp al,9 bei 0x9698).
        Assert.Equal(new byte[] { 0x3c, 0x09 }, exe[0x9698..0x969a]);
        Assert.Equal(GameState.MinimumPlayers, exe[0x9699]);
        // Meldung DGROUP 0x54E9 (DGROUP ab Datei 0x16DD0).
        var text = System.Text.Encoding.ASCII.GetBytes(GameState.ShortHandedMessage);
        Assert.Equal(text, exe[(0x16dd0 + 0x54e9)..(0x16dd0 + 0x54e9 + text.Length)]);
        // Spielbedeutung: Vergleiche mit Code 6, 3 und 1 (0x545CD, 0x545DC, 0x545F0), Code 3 verdoppelt (shl al,1).
        Assert.Equal(new byte[] { 0x80, 0x3e, 0xc2, 0xd0, 0x06 }, exe[0x545cd..0x545d2]);
        Assert.Equal(new byte[] { 0x80, 0x3e, 0xc2, 0xd0, 0x03 }, exe[0x545dc..0x545e1]);
        Assert.Equal(new byte[] { 0xd0, 0xe0 }, exe[0x545ea..0x545ec]);
        Assert.Equal(new byte[] { 0x80, 0x3e, 0xc2, 0xd0, 0x01 }, exe[0x545f0..0x545f5]);
        // Europapokal 0x31490: Code 3, ab [0x7AF] > 5 Code 5.
        Assert.Equal(new byte[] { 0xc6, 0x06, 0xc2, 0xd0, 0x03, 0x80, 0x3e, 0xaf, 0x07, 0x05 }, exe[0x31490..0x3149a]);
        Assert.Equal(EuropeanCup.GroupDates - 1, exe[0x31499]);
        // Minuskulisse-Start 0x869F/0x0001 (0x55AD5, 0x55ADB) = 99.999.
        Assert.Equal(new byte[] { 0xc7, 0x87, 0xe6, 0x97, 0x9f, 0x86 }, exe[0x55adb..0x55ae1]);
        Assert.Equal(0x1869f, StadiumStatistics.InitialMinimum);
    }
}
