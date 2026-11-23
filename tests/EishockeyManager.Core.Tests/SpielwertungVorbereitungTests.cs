using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur v-rest, Teil 2 (A913, #948): Die Spielabrechnung 0x544C0 läuft auch vor einer Spielwertung und prüft die Manager in
/// Indexreihenfolge. [ORIGINAL] Heimverein und Managerwert 100 → Ende (Datei 0x54589), sonst Zuschauer (Datei 0x5464A); Gastverein
/// und Managerwert 100 → Ende (Datei 0x547AA); danach die eingespielte Reihe des Managers (Datei 0x548A8, random(0, 4) je
/// Kaderplatz). Manager mit kleinerem Index als der nicht spielfähige ziehen also Zuschauer und Reihen vor dem Eintrag 0:2/2:0.
/// Vorher rot: Die Spielwertung zog vor dem Eintrag nichts (Zuschauer erst in der Abrechnung, keine Reihen).
/// Erwartete Züge aus einem Zwillingsstrom (R21-D2 = A: erwartete Werte aus der Originalreihenfolge).
/// </summary>
public class SpielwertungVorbereitungTests
{
    private static GameState TwoManagers(int firstClub, int secondClub)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Eins", Head = 0, ClubIndex = firstClub });
        setup.Managers.Add(new ManagerSetup { Name = "Zwei", Head = 1, ClubIndex = secondClub });
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, 3);
        MatchEngine.EnsureTeamValues(game);
        return game;
    }

    private static GameRandom Copy(GameRandom random) => GameRandom.FromState(random.Capture());

    /// <summary>
    /// Manager 0 hat den Heimverein, der Gastverein von Manager 1 ist nicht spielfähig: Manager 0 zieht Zuschauer und Reihen vor
    /// dem Eintrag 2:0; die Abrechnung bucht diese Zuschauer und danach die Strafe für Manager 1.
    /// </summary>
    [Fact]
    public void AwayShortHanded_LowerIndexHomeManager_RollsGateAndLines()
    {
        var game = TwoManagers(2, 3);
        var (m0, m1) = (game.Managers[0], game.Managers[1]);
        var (home, away) = (game.Clubs[m0.ClubIndex], game.Clubs[m1.ClubIndex]);
        away.ForfeitsMatch = true;
        foreach (var p in home.Players)
            p.PlayedLine = 0;

        var twin = Copy(game.Random);
        var tableMatchDay = game.PlayedMatchDays + 1;
        var spectators = game.Spectators(home, away, game.Finance(m0).Stadium, tableMatchDay, twin, 0, m0.Level);
        var players = home.Players.ToList();
        var expectedLines = players.Select((p, i) =>
                i < OriginalSaveGame.SquadSlotsPerManager && TransferRules.Random16(twin, 0, 4) == 0
                    ? home.Lineup.PositionOf(p) & 0x7F
                    : 0)
            .ToList();

        var report = MatchEngine.PlayWithEvents(game, home, away, MatchCompetition.League, knockOut: false, new Random(1));
        Assert.True(report.Forfeit);
        Assert.Equal((2, 0), (report.HomeGoals, report.AwayGoals));
        Assert.Equal(twin.Capture(), game.Random.Capture());
        Assert.Equal(expectedLines, players.Select(p => p.PlayedLine).ToList());
        Assert.Contains(expectedLines, l => l != 0);

        var (money0, money1) = (m0.Money, m1.Money);
        var state = game.Random.Capture();
        game.SettleHomeMatch(home.Index, away.Index, tableMatchDay, 0, leagueMatch: true);
        Assert.Equal(state, game.Random.Capture()); // vorbereitet: die Abrechnung zieht nicht mehr
        Assert.Equal((long)spectators * game.Finance(m0).Stadium.TicketPrice, m0.Money - money0);
        Assert.Equal(-GameState.ShortHandedPenalty, m1.Money - money1);
        Assert.Equal(game.GateCounts.Prepared, game.GateCounts.Settled);
        Assert.Equal(0, game.UnsettledGates);
    }

    /// <summary>
    /// Der nicht spielfähige Manager hat den kleineren Index: 0x544C0 endet bei ihm (Heim Datei 0x54589, Gast Datei 0x547AA), kein
    /// Zug, keine Zuschauer für den späteren Manager; die Strafe zahlt nur er.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LowerIndexShortHanded_NoDraws(bool firstManagerAway)
    {
        var game = TwoManagers(2, 3);
        var (m0, m1) = (game.Managers[0], game.Managers[1]);
        var (first, second) = (game.Clubs[m0.ClubIndex], game.Clubs[m1.ClubIndex]);
        first.ForfeitsMatch = true;
        var (home, away) = firstManagerAway ? (second, first) : (first, second);
        var state = game.Random.Capture();

        var report = MatchEngine.PlayWithEvents(game, home, away, MatchCompetition.League, knockOut: false, new Random(1));
        Assert.True(report.Forfeit);
        Assert.Equal(state, game.Random.Capture());

        var (money0, money1) = (m0.Money, m1.Money);
        game.SettleHomeMatch(home.Index, away.Index, game.PlayedMatchDays + 1, 0, leagueMatch: true);
        Assert.Equal(state, game.Random.Capture());
        Assert.Equal(-GameState.ShortHandedPenalty, m0.Money - money0);
        Assert.Equal(0, m1.Money - money1);
        Assert.Equal(0, game.UnsettledGates);
    }
}
