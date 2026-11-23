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

    /// <summary>
    /// A961 (R23-E1 = A, Nutzer 23.11.2026): Die Manager mit kleinerem Index als der nicht spielfähige durchlaufen in 0x544C0 vor
    /// dem Abbruch den ganzen Managerblock. [ORIGINAL] Kaderplatzschleife Datei 0x548A8–0x549BB: Aufgestellte (+0x0A ≠ 0, +0x15 ≠ 0)
    /// zählen ein Spiel im Feld des Wettbewerbs (Datei 0x54918–0x5499E; 1 = Pokal, 8 = Europapokal, sonst Liga), danach Belastung
    /// aller Kaderplätze auf 50 … 150 (Datei 0x549A2–0x549BB). Der nicht spielfähige Manager zählt nichts (Ende 0x547BD).
    /// Vorher rot: Bei einer Spielwertung zählte kein Manager Spiele oder begrenzte die Belastung (<c>MatchEngine.Credit</c> endet).
    /// </summary>
    [Theory]
    [InlineData(MatchCompetition.League)]
    [InlineData(MatchCompetition.DebCup)]
    public void AwayShortHanded_LowerIndexHomeManager_CountsGamesAndClampsStrain(MatchCompetition competition)
    {
        var game = TwoManagers(2, 3);
        var (m0, m1) = (game.Managers[0], game.Managers[1]);
        var (home, away) = (game.Clubs[m0.ClubIndex], game.Clubs[m1.ClubIndex]);
        away.ForfeitsMatch = true;
        var lineup = home.Lineup.Players.ToHashSet();
        Assert.NotEmpty(lineup);
        var bench = home.Players.Where(p => !lineup.Contains(p)).ToList();
        Assert.NotEmpty(bench);
        var (low, high) = (home.Players.First(lineup.Contains), bench[0]);
        (low.Strain, high.Strain) = (10, 200);
        foreach (var p in away.Players)
            p.Strain = 10;
        var homeBefore = home.Players.ToDictionary(p => p, p => (p.Games, p.CupGames, p.EuropeGames));
        var awayBefore = away.Players.Select(p => (p.Games, p.CupGames, p.EuropeGames)).ToList();

        var report = MatchEngine.PlayWithEvents(game, home, away, competition, knockOut: false, new Random(1));
        Assert.True(report.Forfeit);

        var cup = competition == MatchCompetition.DebCup ? 1 : 0;
        foreach (var p in home.Players)
        {
            var (games, cupGames, europeGames) = homeBefore[p];
            var counted = lineup.Contains(p) ? 1 : 0;
            Assert.Equal((games + counted, cupGames + counted * cup, europeGames), (p.Games, p.CupGames, p.EuropeGames));
            Assert.InRange(p.Strain, TrainingRules.MinStrain, TrainingRules.MaxStrain);
        }
        Assert.Equal((TrainingRules.MinStrain, TrainingRules.MaxStrain), (low.Strain, high.Strain));
        Assert.Equal(awayBefore, away.Players.Select(p => (p.Games, p.CupGames, p.EuropeGames)).ToList());
        Assert.All(away.Players, p => Assert.Equal(10, p.Strain));
    }

    /// <summary>
    /// A961: Hat der nicht spielfähige Manager den kleineren Index, endet 0x544C0 bei ihm (Datei 0x54589 bzw. 0x547AA): Kein Verein
    /// der Paarung zählt Spiele oder begrenzt die Belastung.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LowerIndexShortHanded_NoGamesNoStrainClamp(bool firstManagerAway)
    {
        var game = TwoManagers(2, 3);
        var (first, second) = (game.Clubs[game.Managers[0].ClubIndex], game.Clubs[game.Managers[1].ClubIndex]);
        first.ForfeitsMatch = true;
        var (home, away) = firstManagerAway ? (second, first) : (first, second);
        var players = home.Players.Concat(away.Players).ToList();
        foreach (var p in players)
            p.Strain = 10;
        var before = players.Select(p => p.Games).ToList();

        var report = MatchEngine.PlayWithEvents(game, home, away, MatchCompetition.League, knockOut: false, new Random(1));
        Assert.True(report.Forfeit);
        Assert.Equal(before, players.Select(p => p.Games).ToList());
        Assert.All(players, p => Assert.Equal(10, p.Strain));
    }
}
