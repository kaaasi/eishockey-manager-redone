namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur zusehen-rest, Teil 2 (A837/A840): Die Regeln der Managerpaarung hängen an der Reihenliste DGROUP 0x233 (0x52B91,
/// Merker [bp-0xF] Datei 0x37E7B/0x38277; 0x977F Datei 0x097E6), nicht am Manager. Am Vereinsfeld 0x16 bleiben Verletzungen
/// 0x38B8F (Datei 0x37ECF), Strafspieler 0x378C5 (ohne Reihe aus dem ganzen Kader, Datei 0x379A3) und die Neuzählung 0x38A36.
/// </summary>
public sealed class ZusehenRestReiheTests
{
    private static GameState Game(int seed, params int[] managerClubs)
    {
        var setup = new GameSetup();
        foreach (var club in managerClubs)
            setup.Managers.Add(new ManagerSetup { Name = $"M{club}", Head = 0, ClubIndex = club });
        return NewGame.Create(TestGames.CreateDatabase(), setup, seed);
    }

    private static MatchRules Play(GameState game, Club home, Club away)
    {
        MatchEngine.LastRules = null;
        MatchEngine.PlayWithEvents(game, home, away, MatchCompetition.League, knockOut: false, new Random(5));
        return Assert.IsType<MatchRules>(MatchEngine.LastRules);
    }

    /// <summary>A837: Managerverein mit Markierung aus (Modus 0) bekommt keine Reihe und spielt ohne Managerregeln.</summary>
    [Fact]
    public void ManagerClubWithoutRow_IsNoManagerMatch()
    {
        var game = Game(3, 0);
        var club = game.Clubs[game.Managers[0].ClubIndex];
        var opponent = game.Clubs.First(c => c.Index != club.Index && c.League == club.League);
        Assert.True(Play(game, club, opponent).ManagerMatch);
        game.SetWatchMode(club, evict: true, GameState.WatchOff);
        var next = game.Clubs.First(c => c.Index != club.Index && c.Index != opponent.Index && c.League == club.League);
        Assert.False(Play(game, next, club).ManagerMatch);
        Assert.Null(game.LiveScoreboard!.Rows.FirstOrDefault(r => r.HomeClub == club.Index || r.AwayClub == club.Index));
    }

    /// <summary>A840: Ein markierter Computerverein bekommt mit der Reihe die Managerregeln für die ganze Paarung.</summary>
    [Fact]
    public void MarkedComputerClub_GetsManagerMatch()
    {
        var game = Game(3, 0);
        var managerClub = game.Managers[0].ClubIndex;
        var computers = game.Clubs.Where(c => c.Index != managerClub && c.Index < NationalTeams.First).Take(3).ToArray();
        Assert.False(Play(game, computers[0], computers[1]).ManagerMatch);
        game.SetWatchMode(computers[2], evict: false, GameState.WatchLive);
        Assert.True(computers[2].WatchedLive);
        var other = game.Clubs.First(c => c.Index != managerClub && !computers.Contains(c) && c.Index < NationalTeams.First);
        Assert.True(Play(game, other, computers[2]).ManagerMatch);
    }

    /// <summary>
    /// A840: Ohne Reihe liest 0x977F die Vereinsfelder (kein Kader für die Reihenwerte, keine Taktik je Minute), Strafen und
    /// Verletzungen behalten den Kader (Vereinsfeld 0x16).
    /// </summary>
    [Fact]
    public void ManagerClubWithoutRow_KeepsInjuriesAndPenaltyPlayers()
    {
        var game = Game(3, 0);
        var club = game.Clubs[game.Managers[0].ClubIndex];
        var opponent = game.Clubs.First(c => c.Index != club.Index && c.League == club.League);
        var listed = MatchEngine.LiveTeamOf(game, club, opponent, managed: true, managerMatch: true);
        var unlisted = MatchEngine.LiveTeamOf(game, club, opponent, managed: true, managerMatch: false);
        Assert.NotNull(listed.Squad);
        Assert.NotNull(listed.Tactics);
        Assert.Null(unlisted.Squad);
        Assert.Null(unlisted.Tactics);
        Assert.NotNull(unlisted.PenaltyPlayers);
        Assert.Equal(0, unlisted.InjuryManager);
        Assert.Equal(listed.InjuryManager, unlisted.InjuryManager);
    }

    /// <summary>
    /// A840: [ORIGINAL] 0x378C5 Datei 0x3797F–0x379AB – ein aufgestellter Spieler außerhalb der Reihe auf dem Eis scheidet nur
    /// in der Paarung mit Reihe aus. Ohne Reihe trifft die Strafe auch Spieler anderer Reihen.
    /// </summary>
    [Fact]
    public void PenaltyPlayerWithoutRow_ComesFromWholeLineup()
    {
        var game = Game(3, 0);
        var club = game.Clubs[game.Managers[0].ClubIndex];
        var opponent = game.Clubs.First(c => c.Index != club.Index && c.League == club.League);
        int OffIce(bool managerMatch)
        {
            var count = 0;
            for (var seed = 0; seed < 300; seed++)
            {
                var team = MatchEngine.LiveTeamOf(game, club, opponent, managed: true, managerMatch);
                if (OriginalMinutes.PenalizedPlayer(team, new Random(seed)) is { } p && team.Line(p.Part) != p.Line)
                    count++;
            }
            return count;
        }
        Assert.Equal(0, OffIce(managerMatch: true));
        Assert.True(OffIce(managerMatch: false) > 50);
    }

    /// <summary>
    /// A837: Ohne Reihe wählt 0x541D9 keine Torschützen (nur mit Reihe, Datei 0xAB47–0xAB90), Verletzungen gibt es weiter.
    /// Spielt dieselbe Paarung über viele Zufallsfolgen.
    /// </summary>
    [Fact]
    public void ManagerClubWithoutRow_NoScorersButInjuries()
    {
        var injuries = 0;
        var goals = 0;
        for (var seed = 1; seed <= 60; seed++)
        {
            var game = Game(seed, 0);
            var club = game.Clubs[game.Managers[0].ClubIndex];
            game.SetWatchMode(club, evict: true, GameState.WatchOff);
            var opponent = game.Clubs.First(c => c.Index != club.Index && c.League == club.League);
            var report = MatchEngine.PlayWithEvents(game, club, opponent, MatchCompetition.League, knockOut: false, new Random(seed));
            Assert.False(MatchEngine.LastRules!.Value.ManagerMatch);
            Assert.All(report.Events.Where(e => e.Kind == MatchEventKind.Goal), e => Assert.Null(e.Player));
            goals += report.HomeGoals;
            injuries += report.Events.Count(e => e.Kind == MatchEventKind.Injury && e.Side == 0);
        }
        Assert.True(goals > 0);
        Assert.True(injuries > 0, "Verletzungen 0x38B8F hängen am Vereinsfeld 0x16, nicht an der Reihe");
    }

    /// <summary>
    /// Mehrsaisonprobe (AGENTS.md §5): Manager 0 passiv mit Markierung aus, Manager 1 aktiv (verlängert, markiert); vier
    /// Saisonwechsel. Der Verein von Manager 0 bekommt eine Reihe nur gegen den markierten Verein von Manager 1; Tore ohne
    /// Reihe zählen bei seinen Spielern nicht (<see cref="ManagerClubWithoutRow_NoScorersButInjuries"/>). Speichern/Laden
    /// bleibt bytegleich.
    /// </summary>
    [Fact]
    public void MultiSeason_ManagerClubWithoutRow()
    {
        var game = Game(7, 2, 5);
        var passive = game.Managers[0];
        var active = game.Managers[1];
        for (var change = 0; change < 4; change++)
        {
            game.SetWatchMode(game.Clubs[passive.ClubIndex], evict: true, GameState.WatchOff);
            var activeRows = 0;
            var before = game.Clubs[passive.ClubIndex].Players.Concat(game.Clubs[active.ClubIndex].Players)
                .ToDictionary(p => p, p => p.Goals);
            int Scored(Manager m) => game.Clubs[m.ClubIndex].Players.Where(before.ContainsKey).Sum(p => p.Goals - before[p]);
            for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
            {
                Assert.True(guard < 5000, "Saisonende nicht erreicht");
                foreach (var manager in game.Managers)
                    manager.Money = 10_000_000; // keine Entlassung
                TestSeason.KeepSquad(game, active, extendExpired: true);
                game.Continue();
                if (game.LiveScoreboard is not { } board)
                    continue;
                // Eine Reihe mit dem Verein von Manager 0 gibt es nur gegen den markierten Verein von Manager 1.
                Assert.All(board.Rows.Where(r => r.HomeClub == passive.ClubIndex || r.AwayClub == passive.ClubIndex),
                    r => Assert.True(r.HomeClub == active.ClubIndex || r.AwayClub == active.ClubIndex));
                activeRows += board.Rows.Count(r => r.HomeClub == active.ClubIndex || r.AwayClub == active.ClubIndex);
            }
            Assert.True(activeRows > 0);
            Assert.True(Scored(active) > 0);
            game.StartNextSeason();
        }
        Assert.Equal(GameState.FirstSeason + 4, game.Season);
        var loaded = SaveGame.Load(SaveGame.Save(game));
        Assert.True(loaded.Success, loaded.Error);
        Assert.Equal(SaveGame.Save(game), SaveGame.Save(loaded.Game!));
    }
}
