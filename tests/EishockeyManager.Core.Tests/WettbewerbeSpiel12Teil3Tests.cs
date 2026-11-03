using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur wettbewerbe-spiel, Runde 12, Teil 3 (A174; Entscheidung Nutzer 02.11.2026, Review #440): Computervereine spielen ohne
/// Kaderbezug. [ORIGINAL] 0x977F liest für Computervereine (und Managervereine außerhalb der Managerpaarung) nur die
/// Vereinsfelder 0x18–0x1D; das Original führt für Computervereine keine Spieler (docs/spielberechnung.md §7, §12, §15).
/// Befund vorher (Base 3ddabf7 + D4): <see cref="MatchEngine.TeamValue(Club)"/> verschob die Datenbankstärke eines
/// Computervereins um die Veränderung seiner Remake-Aufstellung gegenüber dem Spielbeginn (<c>ReferenceRating</c>) – eine
/// Verletzung oder ein Verkauf beim Computerverein änderte dessen Spielstärke, im Original nicht.
/// </summary>
public class WettbewerbeSpiel12Teil3Tests
{
    private static GameState Game(int seed = 3)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Test", Head = 0, ClubIndex = 2 });
        return NewGame.Create(TestGames.CreateDatabase(), setup, seed);
    }

    private static Club ComputerClub(GameState game) =>
        game.Clubs.First(c => c.League == League.Bundesliga && !MatchEngine.IsManaged(game, c) && c.Players.Count > 0
                              && c.Strength is > 10 and < 90);

    /// <summary>Halbe Aufstellung leeren (stärkste Spieler zuerst) – vorher sank der Teamwert dadurch deutlich.</summary>
    private static void WeakenLineup(Club club)
    {
        foreach (var player in club.Lineup.Players.OrderByDescending(TeamStrength.PlayerValue).Take(8).ToList())
            club.Lineup.Remove(player);
    }

    /// <summary>Vorher rot: Teamwert = Stärke + (Rating − ReferenceRating) · 2 sank mit der geleerten Aufstellung.</summary>
    [Fact]
    public void ComputerClub_TeamValue_IgnoresLineup()
    {
        var game = Game();
        var club = ComputerClub(game);
        WeakenLineup(club);
        Assert.Equal(club.Strength, MatchEngine.TeamValue(game, club));
        Assert.Equal(club.Strength, MatchEngine.ClubFieldValues(club).Defense.Condition - MatchEngine.TeamValuesOf(club).DefenseCondition);
    }

    /// <summary>Minutenrechnung und Taktikautomatik: Reihenwerte und Gegnerstärke des Computervereins bleiben gleich.</summary>
    [Fact]
    public void ComputerClub_LiveTeam_IgnoresLineup()
    {
        var game = Game();
        MatchEngine.LoadTeamValues(game);
        var club = ComputerClub(game);
        var managed = game.Clubs[game.Managers[0].ClubIndex];
        var before = MatchEngine.LiveTeamOf(game, club, managed, managed: false, managerMatch: true);
        var opponentBefore = MatchEngine.LiveTeamOf(game, managed, club, managed: true, managerMatch: true);
        WeakenLineup(club);
        var after = MatchEngine.LiveTeamOf(game, club, managed, managed: false, managerMatch: true);
        var opponentAfter = MatchEngine.LiveTeamOf(game, managed, club, managed: true, managerMatch: true);
        Assert.Equal((before.Defense, before.Attack, before.TacticsStrength), (after.Defense, after.Attack, after.TacticsStrength));
        Assert.Equal(opponentBefore.TacticsOpponentStrength, opponentAfter.TacticsOpponentStrength);
    }

    /// <summary>
    /// Managervereine behalten ihren Bezug zur Aufstellung ([APPROX] wie bisher, im Original Reihenwerte aus den Spielern
    /// 0x98F8): Gegenprobe, dass die Unterscheidung nicht alle Vereine trifft.
    /// </summary>
    [Fact]
    public void ManagedClub_TeamValue_FollowsLineup()
    {
        var game = Game();
        var club = game.Clubs[game.Managers[0].ClubIndex];
        var before = MatchEngine.TeamValue(game, club);
        WeakenLineup(club);
        Assert.True(MatchEngine.TeamValue(game, club) < before);
    }

    /// <summary>
    /// Mehrsaisonprobe (vier Saisonwechsel, passiv und aktiv): An jedem Spieltag spielen alle Computervereine mit ihrer
    /// Datenbankstärke, auch wenn sich ihre Aufstellungen gegenüber dem Spielbeginn verändern (der Fall tritt in der Probe auf);
    /// der Managerverein behält den Aufstellungsbezug. Ohne die Änderung rot (Teamwert ≠ Stärke bei veränderter Aufstellung).
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MultiSeason_ComputerClubsPlayWithoutSquad(bool active)
    {
        var game = GameModeTests.Create(5, true, true, 2, 30);
        var manager = game.Managers[0];
        var changedLineups = 0;
        for (var change = 0; change <= 4; change++)
        {
            for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
            {
                Assert.True(guard < 1000, "Saisonende nicht erreicht");
                if (active)
                {
                    manager.Money = 10_000_000;
                    TestSquadKeeper.KeepPlayable(game, manager);
                }
                game.Continue();
                foreach (var club in game.Clubs.Where(c => c.Players.Count > 0 && !MatchEngine.IsManaged(game, c)))
                {
                    Assert.Equal(Math.Clamp(club.Strength, 0, 100), MatchEngine.TeamValue(game, club));
                    if (Math.Abs(TeamStrength.Rating(club.Lineup) - club.ReferenceRating) > 0.5)
                        changedLineups++;
                }
            }
            if (game.Phase != SeasonPhase.SeasonOver)
                break;
            if (change < 4)
            {
                if (!active)
                    TestSeason.KeepSquad(game, manager);
                game.StartNextSeason();
            }
        }
        Assert.Equal(5, game.History.Count);
        Assert.True(changedLineups > 0, "keine veränderte Aufstellung eines Computervereins");
    }
}
