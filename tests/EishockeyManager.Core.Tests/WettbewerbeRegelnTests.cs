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
}
