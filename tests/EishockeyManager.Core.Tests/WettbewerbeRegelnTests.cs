using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur wettbewerbe-regeln (Runde 22): Spielrechnung, Wertung und Live-Einzelregeln. Je Befund ein Test mit Fundstelle in em.exe.
/// </summary>
public class WettbewerbeRegelnTests
{
    /// <summary>
    /// #897: [ORIGINAL] Gegentore des Managertorhüters nur im Ligaspiel – 0xA3F7 bricht bei Wettbewerbscode DGROUP 0xD0C2 ≠ 7
    /// ab (Datei 0x0A3FD–0x0A402). Vorher zählte <c>CreditLineup</c> in jedem Wettbewerb.
    /// </summary>
    [Theory]
    [InlineData(MatchCompetition.DebCup, true)]
    [InlineData(MatchCompetition.Playoff, true)]
    [InlineData(MatchCompetition.EuropeanCup, false)]
    [InlineData(MatchCompetition.League, false)]
    public void GoalsAgainst_OnlyInLeague(MatchCompetition competition, bool knockOut)
    {
        var game = TestGames.Create(3);
        MatchEngine.EnsureTeamValues(game);
        var own = game.Clubs[game.Managers[0].ClubIndex];
        var keeper = own.Lineup.Goalkeeper!;
        var opponent = game.Clubs[own.Index == 4 ? 5 : 4];
        var conceded = 0;
        for (var seed = 0; seed < 20; seed++)
        {
            var report = MatchEngine.PlayWithEvents(game, opponent, own, competition, knockOut, new Random(seed));
            conceded += report.HomeGoals;
        }
        Assert.True(conceded > 0);
        Assert.Equal(competition == MatchCompetition.League ? conceded : 0, keeper.GoalsAgainst);
        Assert.True(keeper.GoalieMinutes >= 20 * 60); // Torhüterminuten zählen weiter in jedem Wettbewerb (0x874D)
    }

    /// <summary>
    /// A549: [ORIGINAL] Der Minutenzähler der Minuten-Vorgabe (DGROUP 0x4926 + 8 · Manager, nur 0x8EF2 schreibt: Datei 0x911C,
    /// 0x9166) läuft ins nächste Spiel weiter. Vorher begann jedes Spiel mit einem neuen Zähler (Stand 0).
    /// </summary>
    [Fact]
    public void MinuteCounters_CarryIntoNextMatch()
    {
        var game = TestGames.Create(3);
        MatchEngine.EnsureTeamValues(game);
        var manager = game.Managers[0];
        var own = game.Clubs[manager.ClubIndex];
        own.Tactics.LineChange = LineChangeMode.MinutesPreset;
        var opponent = game.Clubs[own.Index == 4 ? 5 : 4];
        MatchEngine.PlayWithEvents(game, own, opponent, MatchCompetition.League, knockOut: false, new Random(1));
        var team = MatchEngine.LiveTeamOf(game, own, opponent, managed: true, managerMatch: true);
        var counters = team.Squad!.MinuteCounters;
        Assert.Same(MatchEngine.MinuteCountersOf(manager), counters);
        Assert.Contains(counters.Cast<int>(), c => c != 0);
    }

    private static GameState TwoManagers(int firstClub, int secondClub)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Eins", Head = 0, ClubIndex = firstClub });
        setup.Managers.Add(new ManagerSetup { Name = "Zwei", Head = 1, ClubIndex = secondClub });
        return NewGame.Create(TestGames.CreateDatabase(), setup, 3);
    }

    /// <summary>
    /// A191: [ORIGINAL] Sind beide Managervereine nicht spielfähig, verliert der Verein des Managers mit dem kleineren Index
    /// (0x544C0, Schleife über die Manager: Heimzweig Datei 0x5457D–0x5459C → 1, Gastzweig ab 0x54767 → 2). Vorher immer 0:2.
    /// </summary>
    [Theory]
    [InlineData(false, 0, 2)]
    [InlineData(true, 2, 0)]
    public void BothShortHanded_FirstManagerLoses(bool firstManagerAway, int homeGoals, int awayGoals)
    {
        var game = TwoManagers(2, 3);
        MatchEngine.EnsureTeamValues(game);
        var (first, second) = (game.Clubs[game.Managers[0].ClubIndex], game.Clubs[game.Managers[1].ClubIndex]);
        first.ForfeitsMatch = second.ForfeitsMatch = true;
        var (home, away) = firstManagerAway ? (second, first) : (first, second);
        var report = MatchEngine.PlayWithEvents(game, home, away, MatchCompetition.League, knockOut: false, new Random(1));
        Assert.True(report.Forfeit);
        Assert.Equal((homeGoals, awayGoals), (report.HomeGoals, report.AwayGoals));
    }

    /// <summary>
    /// A105/A836: [ORIGINAL] Der Lader 0x2EF1B nimmt für den Sturm Datenbankbyte 1 (Datei 0x2F004–0x2F009, Byte k je Teil); bei
    /// den Europapokal-Gegnern weicht es von Byte 0 ab (kb\23.vga 0x16C). Vorher lag der Sturm um Byte 0.
    /// </summary>
    [Fact]
    public void Loader_AttackFromSecondDatabaseByte()
    {
        var data = new byte[ClubDatabase.FileLength];
        var template = TestGames.CreateDatabase();
        for (var i = 0; i < ClubDatabase.TotalClubs; i++)
        {
            var at = ClubDatabase.StrengthOffset + i * 3;
            data[at] = template.Clubs[i].Strengths.Defense;
            data[at + 1] = template.Clubs[i].Strengths.Attack;
        }
        for (var i = 0; i < ClubDatabase.ClubsWithSquad; i++)
            System.Text.Encoding.ASCII.GetBytes($"VEREIN {i}").CopyTo(data, ClubDatabase.SquadOffset + i * ClubDatabase.SquadRecordLength);
        const int european = 70;
        data[ClubDatabase.StrengthOffset + european * 3] = 79;
        data[ClubDatabase.StrengthOffset + european * 3 + 1] = 40;
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Test", Head = 0, ClubIndex = 2 });
        var game = NewGame.Create(ClubDatabase.Parse(data), setup, 3);
        var club = game.Clubs[european];
        Assert.Equal(79, club.Strength);
        var (defense, attack) = MatchEngine.ClubFieldValues(club);
        Assert.InRange(defense.Condition, 79 - 7, 79 + 7);
        Assert.InRange(defense.Technique, 79 - 7, 79 + 7);
        Assert.InRange(attack.Condition, 40 - 4, 40 + 4);
        Assert.InRange(attack.Technique, 40 - 4, 40 + 4);
    }

    /// <summary>
    /// A838: [ORIGINAL] Endete das letzte Spiel mit herausgenommenem Torhüter (Vereinsfeld 0x2C Bit 3), setzt 0x84D7(0) zu
    /// Spieltagsbeginn Abwehrreihe 2 (Datei 0x855D–0x857D), danach löscht 0x8365 das Bit (Datei 0x84C7). Vorher begann der Verein
    /// mit der gespeicherten Reihe.
    /// </summary>
    [Fact]
    public void PulledGoalieAtEnd_NextMatchStartsWithDefenseLineTwo()
    {
        var game = TestGames.Create(3);
        MatchEngine.EnsureTeamValues(game);
        var (club, other) = (game.Clubs[4], game.Clubs[5]);
        club.Tactics.RecordMatchLines(0, 1);
        club.Tactics.RecordGoaliePulled(true);
        other.Tactics.RecordMatchLines(0, 1);
        MatchEngine.PlayWithEvents(game, club, other, MatchCompetition.League, knockOut: false, new Random(2));
        // Paarung ohne Manager: Reihen bleiben stehen, also steht die Startreihe am Ende noch da.
        Assert.Equal(2, club.Tactics.MatchDefenseLine);
        Assert.False(club.Tactics.GoaliePulledAtEnd);
        Assert.Equal(0, other.Tactics.MatchDefenseLine);

        var loaded = SaveGame.Load(SaveGame.Save(game)).Game!;
        Assert.False(loaded.Clubs[4].Tactics.GoaliePulledAtEnd);
        club.Tactics.RecordGoaliePulled(true);
        Assert.True(SaveGame.Load(SaveGame.Save(game)).Game!.Clubs[4].Tactics.GoaliePulledAtEnd);
    }

    /// <summary>
    /// #925 (A873), schneller Weg der Play-offs: [ORIGINAL] bei eigenem Gleichstand genau ein Zusatzwurf random(0, 2) je Seite
    /// (Vergleich 0x37748 mit Marke), der Gleichstand kann bleiben und zählt für den Gast (0x3166E); der DEB-Pokal würfelt weiter bis
    /// ungleich. Vorher würfelten auch die Play-offs bis ungleich.
    /// </summary>
    [Fact]
    public void FastPath_PlayoffTie_OneRollAndTieCanStay()
    {
        var side = new MatchSide(50);
        int stayed = 0, rolled = 0;
        for (var seed = 0; seed < 3000; seed++)
        {
            var plain = OriginalMinutes.FastCupResultWithTie(side, side, true, true, false, true, knockOut: false, new Random(seed));
            if (plain.Home != plain.Away)
                continue;
            var playoff = OriginalMinutes.FastCupResultWithTie(side, side, true, true, false, true, knockOut: true, new Random(seed));
            Assert.True(playoff.Shootout);
            Assert.Equal(plain.Home, playoff.Tie);
            rolled++;
            if (playoff.Home == playoff.Away)
                stayed++;
            var cup = OriginalMinutes.FastCupResultWithTie(side, side, true, true, false, false, knockOut: true, new Random(seed));
            Assert.NotEqual(cup.Home, cup.Away);
        }
        Assert.True(rolled > 50);
        Assert.InRange(stayed, rolled / 6, rolled / 2); // x = y mit 1/3
    }

    /// <summary>
    /// #925 (A873): [ORIGINAL] Die erste gerechnete Play-off-Paarung eines Termins würfelt auch bei klarem Ergebnis dazu, weil
    /// 0x32AA3 die noch nicht gerechneten (0:0) als Gleichstand meldet (Datei 0x376E1–0x377A3); die Zusatztore zählen, bis ungleich.
    /// </summary>
    [Fact]
    public void FastPath_PlayoffExtraRoll_OnClearResult()
    {
        var side = new MatchSide(50);
        int changed = 0, clear = 0;
        for (var seed = 0; seed < 500; seed++)
        {
            var plain = OriginalMinutes.FastCupResultWithTie(side, side, true, true, false, true, knockOut: true, new Random(seed));
            if (plain.Shootout)
                continue;
            clear++;
            var extra = OriginalMinutes.FastCupResultWithTie(side, side, true, true, false, true, knockOut: true, new Random(seed),
                playoffExtraRoll: true);
            Assert.False(extra.Shootout);
            Assert.NotEqual(extra.Home, extra.Away);
            Assert.InRange(extra.Home - plain.Home, 0, 2);
            Assert.InRange(extra.Away - plain.Away, 0, 2);
            if ((extra.Home, extra.Away) != (plain.Home, plain.Away))
                changed++;
        }
        Assert.True(clear > 100);
        Assert.True(changed > clear / 2);
    }

    /// <summary>#925 (A873): Nur die erste Paarung eines Play-off-Termins bekommt den Zusatzwurf (Reihenfolge der Plätze).</summary>
    [Fact]
    public void FirstPlayoffPairingOfDate_OnlyBeforeAnyGameOfTheDate()
    {
        var game = TestGames.Create(3);
        var playoffs = new Playoffs([10, 11, 12, 13, 14, 15, 16, 17]);
        typeof(GameState).GetProperty(nameof(GameState.Playoffs))!.SetValue(game, playoffs);
        var round = playoffs.Rounds[0];
        Assert.True(MatchEngine.FirstPlayoffPairingOfDate(game, game.Clubs[10], game.Clubs[17]));
        Assert.True(MatchEngine.FirstPlayoffPairingOfDate(game, game.Clubs[11], game.Clubs[16]));
        round[0].Games.Add(new KnockoutMatch(1, 10, 17, 3, 1));
        Assert.False(MatchEngine.FirstPlayoffPairingOfDate(game, game.Clubs[11], game.Clubs[16]));
        Assert.False(MatchEngine.FirstPlayoffPairingOfDate(game, game.Clubs[13], game.Clubs[14]));
        foreach (var s in round.Skip(1))
            s.Games.Add(new KnockoutMatch(1, s.HomeClub, s.AwayClub, 2, 1));
        Assert.True(MatchEngine.FirstPlayoffPairingOfDate(game, game.Clubs[17], game.Clubs[10]));
    }

    private static int InjuriesOfSecondManager(bool firstManagerShortHanded)
    {
        var game = TwoManagers(2, 3);
        MatchEngine.EnsureTeamValues(game);
        if (firstManagerShortHanded)
            game.Clubs[2].Lineup.ClearAll();
        Assert.Equal(firstManagerShortHanded, game.IsShortHanded(game.Clubs[2]));
        var (own, opponent) = (game.Clubs[3], game.Clubs[6]);
        var injuries = 0;
        for (var seed = 0; seed < 150; seed++)
        {
            foreach (var p in own.Players)
                p.InjuredGames = 0;
            own.AutoLineup();
            var report = MatchEngine.PlayWithEvents(game, own, opponent, MatchCompetition.League, knockOut: false, new Random(seed));
            injuries += report.Events.Count(e => e.Kind == MatchEventKind.Injury);
        }
        return injuries;
    }

    /// <summary>
    /// A071: [ORIGINAL] 0x38B8F prüft den Managerwert des Managers am Zug [0xA70E] (Datei 0x38C1C–0x38C2E), nicht den des
    /// geprüften Managers. Nach einer Minute mit einem Gast ohne Manager steht [0xA70E] auf 0 (0x87F7, Datei 0x08834); ist
    /// Manager 0 heute nicht spielfähig (Managerwert 100), verletzt sich bei Manager 1 nur noch in Minuten direkt nach einer
    /// eigenen Strafe (0x35ABC setzt [0xA70E] = Manager 1). Vorher prüfte das Remake den eigenen Wert (gleiche Rate).
    /// </summary>
    [Fact]
    public void Injuries_UseTurnManagerValue()
    {
        var normal = InjuriesOfSecondManager(firstManagerShortHanded: false);
        var blocked = InjuriesOfSecondManager(firstManagerShortHanded: true);
        Assert.True(normal >= 6, $"{normal}");
        Assert.True(blocked * 3 < normal, $"{blocked} gegen {normal}");
    }

    /// <summary>
    /// A071: Schreiber von [0xA70E] – 0x87F7 (Datei 0x08834: Vereinsfeld 0x16, beim Manager − 1) und jede gegebene Strafe
    /// (Datei 0x35ABC, vor der Spielerwahl).
    /// </summary>
    [Fact]
    public void TurnManager_WrittenByEnergyAndPenalty()
    {
        var game = TwoManagers(2, 3);
        MatchEngine.EnsureTeamValues(game);
        var (home, away) = (game.Clubs[3], game.Clubs[6]);
        var h = MatchEngine.LiveTeamOf(game, home, away, managed: true, managerMatch: true);
        var a = MatchEngine.LiveTeamOf(game, away, home, managed: false, managerMatch: true);
        Assert.Equal(1, h.ManagerSlot);
        Assert.Null(a.ManagerSlot);
        var register = MatchEngine.TurnManagerOf(game, MatchCompetition.League, liveDisplay: false);
        h.TurnManager = a.TurnManager = register;
        register.Manager = 5;
        OriginalMinutes.DrainEnergy(h, new Random(1));
        Assert.Equal(1, register.Manager);
        OriginalMinutes.DrainEnergy(a, new Random(1));
        Assert.Equal(0, register.Manager);
        var penalties = 0;
        for (var seed = 0; seed < 400 && penalties == 0; seed++)
        {
            register.Manager = 0;
            var events = new List<MatchEvent>();
            if (OriginalMinutes.Penalty(h, a, 0, 5, chained: true, events, new Random(seed)) > 0)
            {
                penalties++;
                Assert.Equal(1, register.Manager);
            }
            else
            {
                Assert.Equal(0, register.Manager);
            }
        }
        Assert.Equal(1, penalties);
    }

    private static (int, int)[] YouthOf(GameState game) =>
        game.Finance(game.Managers[0]).Youth.Select(t => (t.Strength, t.Position)).ToArray();

    /// <summary>
    /// Halt an einem Ligaspieltag ab Tag 6 (nicht dem letzten), an dem die Jugendrechnung des Tages Stärke oder Platz ändert.
    /// </summary>
    private static GameState HaltOnLeagueDay(int seed)
    {
        var game = TestGames.Create(seed);
        var me = game.Managers[0];
        foreach (var kind in Enum.GetValues<YouthTeamKind>())
            game.SetYouthFunding(me, kind, YouthTeam.MaxFunding(kind));
        var leagueDays = SeasonCalendar.DaysOf(DayEvents.League).ToList();
        for (var guard = 0; ; guard++)
        {
            Assert.True(guard < 400);
            me.Money = Math.Max(me.Money, 5_000_000);
            game.Continue();
            var at = leagueDays.IndexOf(game.CurrentDay);
            if (game.CurrentDay <= 5 || at < 0 || at + 1 >= leagueDays.Count)
                continue;
            var probe = SaveGame.Load(SaveGame.Save(game)).Game!; // nur ein Halt, an dem die Jugend des Tages etwas bewegt
            probe.DailyYouth(probe.CurrentDay);
            if (!YouthOf(probe).SequenceEqual(YouthOf(game)))
                return game;
        }
    }

    /// <summary>
    /// A384/A670: [ORIGINAL] Jugendteams nach den Managermenüs, vor den Ligaspielen (Datei 0x55003–0x5502C: Kalenderbits 0–2,
    /// je Manager 0x39DEC bei Datei 0x5501C, dann 0x35EA4 bei Datei 0x55039) – an jedem Tag, nicht nur an Tag 0. Vom Halt eines
    /// Ligaspieltags bis zum nächsten Halt (ohne Ligaspieltag dazwischen) rechnet die Zeitleiste also genau die Jugend dieses
    /// Tags, aus dem Zufallszustand des Menüs. Vorher lief sie im Tageswechsel vor dem Menü: am Halt schon gerechnet, dafür die des nächsten Tags vor dessen
    /// Halt.
    /// </summary>
    [Fact]
    public void Youth_RunsAfterTheMenus()
    {
        var game = HaltOnLeagueDay(5);
        var saved = SaveGame.Save(game);
        var expected = SaveGame.Load(saved).Game!;
        expected.DailyYouth(expected.CurrentDay);
        var played = SaveGame.Load(saved).Game!;
        played.Continue();
        Assert.DoesNotContain(SeasonCalendar.DaysOf(DayEvents.League), d => d > game.CurrentDay && d < played.CurrentDay);
        Assert.NotEqual(YouthOf(game), YouthOf(expected));
        Assert.Equal(YouthOf(expected), YouthOf(played));
    }

    /// <summary>
    /// A384/A670, Migration 56 → 57: Ein älterer Stand nach dem Tageswechsel eines Ligaspieltags hat dessen Jugend schon gerechnet
    /// – sie läuft nicht noch einmal (<c>_youthOfDayDone</c>).
    /// </summary>
    [Fact]
    public void Youth_OldSaveAfterDayOpen_NotTwice()
    {
        var game = HaltOnLeagueDay(5);
        var json = System.Text.Json.Nodes.JsonNode.Parse(SaveGame.Save(game))!.AsObject();
        json["schema"] = 56;
        json["spiel"]!.AsObject().Remove("_youthOfDayDone");
        var old = SaveGame.Load(System.Text.Encoding.UTF8.GetBytes(json.ToJsonString())).Game!;
        old.Continue();
        Assert.Equal(YouthOf(game), YouthOf(old)); // bis zum nächsten Halt keine weitere Jugendrechnung
        var fresh = SaveGame.Load(SaveGame.Save(game)).Game!;
        fresh.Continue();
        Assert.NotEqual(YouthOf(game), YouthOf(fresh));
    }
}
