using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Review-Befund R7-03 (#201): Spiele des Nationalteams (Freundschaftsspiel, WM) zählen Spiele, Tore, Vorlagen, Strafen
/// und Torhüterwerte bei den Spielern der Aufstellung des Bundestrainers – wie bei einem Managerverein.
/// Gegenprobe aus dem Review: vorher blieben alle Kadersummen 0.
/// </summary>
public class IntegrationRunde7NationalCountersTests
{
    [Fact]
    public void Friendly_CreditsTheNationalLineup()
    {
        var game = GameModeTests.Create(7, false, true, 2);
        game.Continue();
        game.AutoNominate();
        var coach = game.NationalCoach!;
        var lineup = coach.Lineup();
        var opponent = coach.Nation == NationalTeams.First ? NationalTeams.First + 1 : NationalTeams.First;
        Assert.Equal(FriendlyResult.Played, game.PlayFriendly(opponent, home: true));

        var match = coach.Friendlies[^1];
        var own = match.Home == coach.Nation ? match.HomeGoals : match.AwayGoals;
        var against = match.Home == coach.Nation ? match.AwayGoals : match.HomeGoals;
        Assert.All(lineup.Players, p => Assert.True(p.Games > 0, $"{p.Name} ohne Spiel"));
        Assert.Equal(against, lineup.Goalkeeper!.GoalsAgainst);
        Assert.Contains(lineup.Goalkeeper.GoalieMinutes, new[] { 60, 70 });
        Assert.InRange(lineup.Players.Sum(p => p.Goals), 0, own);
        Assert.True(coach.Squad.Sum(m => m.Player.GoalieMinutes) > 0);
    }

    [Fact]
    public void WorldCup_CreditsTheNationalLineup()
    {
        var game = GameModeTests.Create(7, false, true, 2);
        game.Continue();
        game.AutoNominate();
        var coach = game.NationalCoach!;
        var keeper = coach.Lineup().Goalkeeper!;
        for (var guard = 0; keeper.GoalieMinutes == 0 && guard < 400 && game.NationalCoach is not null; guard++)
            game.Continue();
        Assert.True(keeper.GoalieMinutes >= 60, "WM-Spiel des Bundestrainers zählt keine Torhüterminuten");
        Assert.True(coach.Lineup().Players.Sum(p => p.Games) > 0);
    }
}
