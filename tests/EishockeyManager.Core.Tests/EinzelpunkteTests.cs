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
        var standIn = Assert.Single(squad.Players, p => p.Position == 1 && !p.Goalie && club.Lineup.FindSlot(p.Player) is null);
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
}
