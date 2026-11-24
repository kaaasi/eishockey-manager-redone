using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur wettbewerbe-regeln (Runde 22), Teil 3 und A553: Taktikschreiber und Live-Einzelregeln. Je Befund ein Test mit Fundstelle
/// in em.exe.
/// </summary>
public class WettbewerbeRegelnLiveTests
{
    private static (GameState Game, Club Own, Club Opponent) ManagedPair()
    {
        var game = TestGames.Create(3);
        MatchEngine.EnsureTeamValues(game);
        var own = game.Clubs[game.Managers[0].ClubIndex];
        var opponent = game.Clubs[own.Index == 4 ? 5 : 4];
        return (game, own, opponent);
    }

    /// <summary>
    /// A553: [ORIGINAL] 0x8365 schreibt Vereinsfeld 0x26 nur ohne Manager bzw. bei „AUTOM.“ (Datei 0x8443–0x847D), die
    /// Angriffsbits 0x2C nie (Datei 0x8481–0x84BD). Ein Managerverein mit „SEMI“/„MANUELL“ beginnt also mit den Feldern vom Ende
    /// des Vorspiels bzw. vom letzten Verlassen des Taktikbildschirms. Vorher (rot): eigene Werte beim Stand 0:0 (Grundwert +
    /// „Unentschieden“, eigener Angriff).
    /// </summary>
    [Theory]
    [InlineData(TacticMode.Semi)]
    [InlineData(TacticMode.Manual)]
    public void ManagedSemiOrManual_StartsWithMatchFields(TacticMode mode)
    {
        var (game, own, opponent) = ManagedPair();
        var t = own.Tactics;
        t.EffortMode = mode;
        t.AttackMode = mode;
        t.SetEffort(0, ScoreSituation.Base, 50);
        t.SetEffort(0, ScoreSituation.Tied, 10);
        t.SetAttack(0, ScoreSituation.Base, 5);
        t.SetAttack(0, ScoreSituation.Tied, 1);
        t.RecordMatchFields(33, 2);

        var team = MatchEngine.LiveTeamOf(game, own, opponent, managed: true, managerMatch: true);
        var other = MatchEngine.LiveTeamOf(game, opponent, own, managed: false, managerMatch: true);
        MatchEngine.PrepareMatch(game, own, opponent, team, other);

        Assert.Equal((33, 2), (team.Effort, team.AttackLevel));
    }

    /// <summary>
    /// A553: [ORIGINAL] Der Taktikbildschirm setzt beim Aufbau 0x26 = 0x2D (Datei 0x48003–0x48007) und beim Verlassen in
    /// 0x48E62 (Aufruf Datei 0x48E19) 0x2D = 0x26, 0x26 = 0x2D + 0x29 (Datei 0x4902B–0x4903E): Vereinsfeld 0x26 = Grundwert +
    /// Zuschlag „Unentschieden“. Die Angriffsbits schreibt er nicht.
    /// </summary>
    [Theory]
    [InlineData(TacticMode.Semi)]
    [InlineData(TacticMode.Manual)]
    public void TacticsLeft_WritesBasePlusTiedEffort_KeepsAttack(TacticMode mode)
    {
        var (game, own, opponent) = ManagedPair();
        var t = own.Tactics;
        t.EffortMode = mode;
        t.AttackMode = mode;
        t.SetEffort(0, ScoreSituation.Base, 50);
        t.SetEffort(0, ScoreSituation.Tied, 10);
        t.SetAttack(0, ScoreSituation.Base, 5);
        t.RecordMatchFields(33, 2);

        game.OnTacticsLeft(own);

        Assert.Equal((60, 2), (t.MatchEffort, t.MatchAttack));
        var team = MatchEngine.LiveTeamOf(game, own, opponent, managed: true, managerMatch: true);
        var other = MatchEngine.LiveTeamOf(game, opponent, own, managed: false, managerMatch: true);
        MatchEngine.PrepareMatch(game, own, opponent, team, other);
        Assert.Equal((60, 2), (team.Effort, team.AttackLevel));
    }

    /// <summary>
    /// A553: [ORIGINAL] 0x48E62 schreibt ohne Blick auf den Modus; bei „AUTOM.“ stehen in 0x27–0x2B/0x2D die Zeilen der
    /// Automatik (<see cref="Tactics.ShownEffort"/>). Das Feld liest danach die Stärke 0x368B1 der Vorbereitung.
    /// </summary>
    [Fact]
    public void TacticsLeft_Auto_WritesShownAutoLine()
    {
        var (game, own, _) = ManagedPair();
        var t = own.Tactics;
        t.EffortMode = TacticMode.Auto;
        t.RecordMatchFields(33, 4);

        game.OnTacticsLeft(own);

        var expected = t.ShownEffort(0, ScoreSituation.Base) + t.ShownEffort(0, ScoreSituation.Tied);
        Assert.Equal((expected, 4), (t.MatchEffort, t.MatchAttack));
    }

    /// <summary>Nur Managervereine (der Bildschirm zeigt den Verein des Managers am Zug); Computervereine bleiben unverändert.</summary>
    [Fact]
    public void TacticsLeft_ComputerClub_Unchanged()
    {
        var (game, _, opponent) = ManagedPair();
        opponent.Tactics.RecordMatchFields(33, 4);

        game.OnTacticsLeft(opponent);

        Assert.Equal((33, 4), (opponent.Tactics.MatchEffort, opponent.Tactics.MatchAttack));
    }

    /// <summary>
    /// A101: [ORIGINAL] Ist ein Verein zum Zusehen markiert (Vereinsfeld 0x34 Bit 7, Datei 0x390CB/0x390FA), schießt 0x3901B
    /// Schuss für Schuss: erster Schütze random(0, 1) (Datei 0x39021), Treffer random(0, 10) &gt; 6 (Datei 0x39429–0x39443),
    /// mindestens fünf Runden, dann ganze Runden bis ungleich (Datei 0x394EA–0x39547). Vorher (rot): immer random(2, 5) je Seite.
    /// </summary>
    [Fact]
    public void Shootout_Watched_ShotByShot()
    {
        var below = 0;
        for (var seed = 0; seed < 200; seed++)
        {
            var actual = OriginalMinutes.Shootout(new Random(seed), watched: true);
            var r = new Random(seed);
            var first = r.Next(0, 2);
            var goals = new int[2];
            var shots = new int[2];
            var side = first;
            while (true)
            {
                if (r.Next(0, 11) > 6)
                    goals[side]++;
                shots[side]++;
                side ^= 1;
                if (side == first && shots[first] >= 5 && goals[0] != goals[1])
                    break;
            }
            Assert.Equal((goals[0], goals[1]), actual);
            Assert.Equal(shots[0], shots[1]);
            if (Math.Min(actual.Home, actual.Away) < 2)
                below++;
        }
        Assert.True(below > 0, "random(2, 5) gäbe nie weniger als zwei Treffer");
    }

    /// <summary>A101: Ohne Markierung bleibt random(2, 5) je Seite (Datei 0x39105–0x39179), Heim zuerst, bis ungleich.</summary>
    [Fact]
    public void Shootout_NotWatched_Unchanged()
    {
        for (var seed = 0; seed < 50; seed++)
        {
            var actual = OriginalMinutes.Shootout(new Random(seed));
            var r = new Random(seed);
            r.Next(0, 2);
            int h, a;
            do
            {
                h = r.Next(2, 6);
                a = r.Next(2, 6);
            }
            while (h == a);
            Assert.Equal((h, a), actual);
        }
    }

    /// <summary>
    /// A456: [ORIGINAL] 0x53C57 ruft 0x541D9 auch für vergebene Chancen (Art 0, Datei 0x5406C–0x5407F) und meldet Schütze
    /// [0xE2A7] und Vorlage [0xE2A6] (Datei 0x54089–0x54096). Die Anzeigereihe der Managerpaarung führt sie an jeder Chance des
    /// Managervereins; der Gegner ohne Kader hat keinen. Vorher (rot): vergebene Chancen ohne Schützen.
    /// </summary>
    [Fact]
    public void MissedChances_OfManagedClub_HaveShooter()
    {
        const int managed = 2;
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "M", Head = 0, ClubIndex = managed });
        var game = Core.NewGame.Create(TestGames.CreateDatabase(), setup, 5);
        var league = game.Clubs[managed].League!.Value;
        var (home, away) = game.Schedules[league][0].Matches.First(m => m.Home == managed || m.Away == managed);
        MatchEngine.PlayWithEvents(game, game.Clubs[home], game.Clubs[away], MatchCompetition.League, knockOut: false,
            new GameRandom(3));
        var row = game.LiveScoreboard!.RowOf(managed)!;
        var side = home == managed ? 0 : 1;
        var own = row.ChanceList.Where(c => c.Side == side && c.Minute <= 60).ToList();
        Assert.Contains(own, c => !c.Goal);
        Assert.All(own, c => Assert.NotNull(c.Shooter));
        Assert.All(own, c => Assert.Contains(game.Clubs[managed].Players, p => p.Name == c.Shooter));
        Assert.All(row.ChanceList.Where(c => c.Side != side), c => Assert.Null(c.Shooter));
    }

    private static (GameState Game, Club Club, LiveMatchDay Day, LiveTeam Team) LiveManagedMatch(MatchCompetition competition)
    {
        const int managed = 2;
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "M", Head = 0, ClubIndex = managed });
        var game = Core.NewGame.Create(TestGames.CreateDatabase(), setup, 5);
        var league = game.Clubs[managed].League!.Value;
        var (home, away) = game.Schedules[league][0].Matches.First(m => m.Home == managed || m.Away == managed);
        var day = LiveMatchDay.Begin(game, [new LiveFixture(game.Clubs[home], game.Clubs[away], competition,
            KnockOut: false, new GameRandom(7))]);
        for (var i = 0; i < 10; i++)
            day.Step();
        var club = game.Clubs[managed];
        club.AutoLineupEnabled = false;
        return (game, club, day, day.MatchOf(managed)!.TeamOf(managed)!);
    }

    /// <summary>
    /// A677: [ORIGINAL] Spiele zählen die Aufgestellten bei Spielbeginn (0x544C0 setzt +0x1A Bit 7, Datei 0x54910–0x54973) und
    /// jeder im Spiel neu Aufgestellte ohne Bit 7 (Datei 0x415B7–0x41622). Herausgenommene behalten ihr Spiel. Vorher (rot):
    /// gezählt wurde die Aufstellung am Spielende – der Herausgenommene ohne Spiel.
    /// S07 (Spur aufstellung Runde 25, R15-E2 = C): Die Aufgestellten zählen schon vor den Minuten (Datei 0x54910–0x5499E), in Minute 10
    /// also bereits gezählt, am Spielende nicht noch einmal; vorher erwartete der Test ihr Spiel erst am Ende. Rückschaltprobe: src/ von
    /// 4433148 + dieser Test → rot (Herausgenommener und übrige Aufgestellte + 1 am Ende).
    /// </summary>
    [Fact]
    public void Games_StartLineupAndPlacedInMatch()
    {
        var (_, club, day, team) = LiveManagedMatch(MatchCompetition.League);
        var takenOut = club.Lineup[LineupSlot.Forward(2, 0)]!;
        var bench = club.Players.First(p => p.Available && p.Role == PlayerRole.Forward && !club.Lineup.Contains(p));
        var others = club.Lineup.Players.Where(p => p != takenOut).ToList();
        var before = club.Players.ToDictionary(p => p, p => p.Games);
        int? line = 2;
        Assert.Equal(LineupResult.TakenOut, club.ClickPlayerInMatch(takenOut, ref line, right: false));
        Assert.Equal(LineupResult.Assigned, club.ClickPlayerInMatch(bench, ref line, right: false));
        LiveLineup.Apply(team, club);
        day.Finish();

        Assert.Equal(before[takenOut], takenOut.Games);
        Assert.Equal(before[bench] + 1, bench.Games);
        Assert.All(others, p => Assert.Equal(before[p], p.Games));
        Assert.All(club.Players.Where(p => p != bench && p != takenOut && !others.Contains(p)),
            p => Assert.Equal(before[p], p.Games));
    }

    /// <summary>
    /// A677: [ORIGINAL] Das Aufstellen im Spiel bucht nach DGROUP 0xD0C2: 7 Liga, 6 DEB-Pokal, sonst Feld 2 (Europapokal), Datei
    /// 0x415CD–0x41606 – im Play-off-Spiel also +0x08, die Aufgestellten bei Spielbeginn in der Liga (CompetitionSlots).
    /// </summary>
    [Theory]
    [InlineData(MatchCompetition.DebCup, 1, 0)]
    [InlineData(MatchCompetition.Playoff, 0, 1)]
    public void Games_PlacedInMatch_CompetitionField(MatchCompetition competition, int cup, int europe)
    {
        var (_, club, day, team) = LiveManagedMatch(competition);
        var bench = club.Players.First(p => p.Available && p.Role == PlayerRole.Forward && !club.Lineup.Contains(p));
        var (cupBefore, europeBefore) = (bench.CupGames, bench.EuropeGames);
        club.ClearLineupSlotInMatch(LineupSlot.Forward(2, 0));
        int? line = 2;
        Assert.Equal(LineupResult.Assigned, club.ClickPlayerInMatch(bench, ref line, right: false));
        LiveLineup.Apply(team, club);
        day.Finish();
        Assert.Equal((cupBefore + cup, europeBefore + europe), (bench.CupGames, bench.EuropeGames));
    }

    /// <summary>Live-Paarung des Managervereins bis zur ersten Minute mit einem Feldspieler auf seiner Strafbank.</summary>
    private static (Club Club, LiveMatchDay Day, LiveTeam Team, LivePlayer Boxed) FirstPenalty()
    {
        for (var seed = 1; seed < 40; seed++)
        {
            const int managed = 2;
            var setup = new GameSetup();
            setup.Managers.Add(new ManagerSetup { Name = "M", Head = 0, ClubIndex = managed });
            var game = Core.NewGame.Create(TestGames.CreateDatabase(), setup, 5);
            var league = game.Clubs[managed].League!.Value;
            var (home, away) = game.Schedules[league][0].Matches.First(m => m.Home == managed || m.Away == managed);
            var day = LiveMatchDay.Begin(game, [new LiveFixture(game.Clubs[home], game.Clubs[away], MatchCompetition.League,
                KnockOut: false, new GameRandom(seed))]);
            var team = day.MatchOf(managed)!.TeamOf(managed)!;
            while (day.Running && day.Minute < 60)
            {
                day.Step();
                if (team.Box.Occupants.FirstOrDefault(o => o is not null) is { } boxed)
                {
                    game.Clubs[managed].AutoLineupEnabled = false;
                    return (game.Clubs[managed], day, team, boxed);
                }
            }
        }
        throw new InvalidOperationException("keine Strafe eines Feldspielers in 39 Saaten");
    }

    /// <summary>
    /// A808: [ORIGINAL] Eine Strafe setzt +0x0A = 0, Status +0x09 Bit 0 und +0x0D = Strafcode &amp; 7 (Datei 0x35BFD–0x35C0C);
    /// das Freiwerden 0x35691 löscht beides (Datei 0x35791–0x3579A); im Spiel zählt +0x0D nicht herunter (Abzüge 0x5488C/0x5490C
    /// in der Spielabrechnung). Die Abfrage liefert die Strafminuten des Ereignisses, solange er auf der Strafbank sitzt.
    /// </summary>
    [Fact]
    public void PenaltyBox_Query_MinutesWhileBoxed()
    {
        var (club, day, team, boxed) = FirstPenalty();
        var penalty = day.MatchOf(club.Index)!.Report.Events.Last(e => e.Kind == MatchEventKind.Penalty && e.Player == boxed.Player);
        Assert.Equal(penalty.PenaltyMinutes, MatchEngine.PenaltyBoxMinutes(club, boxed.Player));
        Assert.All(club.Players.Where(p => team.Box.Occupants.All(o => o?.Player != p)),
            p => Assert.Equal(0, MatchEngine.PenaltyBoxMinutes(club, p)));
        while (team.Box.Occupants.Contains(boxed) && day.Minute < 60)
        {
            Assert.Equal(penalty.PenaltyMinutes, MatchEngine.PenaltyBoxMinutes(club, boxed.Player)); // kein Herunterzählen
            day.Step();
        }
        if (!team.Box.Occupants.Contains(boxed))
            Assert.Equal(0, MatchEngine.PenaltyBoxMinutes(club, boxed.Player));
    }

    /// <summary>
    /// A808: [ORIGINAL] Der Klick der Aufstellung auf einen Spieler mit Status 1 meldet nur „Gesperrt.“ und bricht ab, für beide
    /// Tasten (Datei 0x40DAF–0x40E2A). Vorher (rot): der Bestrafte ließ sich herausnehmen bzw. umstellen.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PenaltyBox_ClickInMatch_OnlyMessage(bool right)
    {
        var (club, _, _, boxed) = FirstPenalty();
        var slots = LineupSlot.All.Select(s => club.Lineup[s]).ToList();
        var dressed = club.Dressed.ToList();
        int? line = boxed.Player.Role == PlayerRole.Goalkeeper ? Lineup.GoalkeeperLine : 3;
        Assert.Equal(LineupResult.Suspended, club.ClickPlayerInMatch(boxed.Player, ref line, right));
        Assert.Equal(slots, LineupSlot.All.Select(s => club.Lineup[s]).ToList());
        Assert.Equal(dressed, club.Dressed.ToList());
    }
}
