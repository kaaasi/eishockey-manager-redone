using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Integration Runde 8, Welle 3 (Leitstelle): Anschlüsse, die laut Plan die Leitstelle setzt.
/// </summary>
public class IntegrationRunde8Welle3Tests
{
    /// <summary>
    /// #193: Nach einem Freundschaftsspiel schreibt das Original die Teamwerte des Nationalteams aus der Aufstellung des
    /// Bundestrainers (0x952C mit Manager 5, Aufruf 0x555D6). Die Länderwerte Deutschlands folgen danach der Aufstellung.
    /// </summary>
    [Fact]
    public void Friendly_SetsGermanyRatingsFromCoachLineup()
    {
        var game = GameModeTests.Create(7, false, true, 2);
        game.Continue();
        game.AutoNominate();
        var coach = game.NationalCoach!;
        Assert.Equal(NationalTeams.Germany, coach.Nation);
        FillFormation(coach);
        var before = game.NationalRatings(NationalTeams.Germany);
        Assert.Equal(FriendlyResult.Played, game.PlayFriendly(52, home: true));

        var (defense, attack, _) = MatchEngine.LineupTeamValues(coach.Formation);
        Assert.NotEqual(before, game.NationalRatings(NationalTeams.Germany));
        Assert.Equal((defense.Condition, defense.Technique, attack.Condition, attack.Technique),
            game.NationalRatings(NationalTeams.Germany));
    }

    /// <summary>[APPROX] Ohne eigene Aufstellung (Automatik) bleiben die Länderwerte stehen.</summary>
    [Fact]
    public void Friendly_WithoutFormation_KeepsGermanyRatings()
    {
        var game = GameModeTests.Create(7, false, true, 2);
        game.Continue();
        game.AutoNominate();
        Assert.Equal(0, game.NationalCoach!.Formation.Count);
        var before = game.NationalRatings(NationalTeams.Germany);
        Assert.Equal(FriendlyResult.Played, game.PlayFriendly(52, home: true));
        Assert.Equal(before, game.NationalRatings(NationalTeams.Germany));
    }

    private static void FillFormation(NationalCoach coach)
    {
        var keepers = coach.Rows.Where(p => p.Role == PlayerRole.Goalkeeper && p.Available).ToList();
        var defenders = coach.Rows.Where(p => p.Role == PlayerRole.Defender && p.Available).ToList();
        var forwards = coach.Rows.Where(p => p.Role == PlayerRole.Forward && p.Available).ToList();
        Assert.Equal(LineupResult.Assigned, coach.Assign(LineupSlot.Goalkeeper, keepers[0]));
        for (var b = 0; b < 3; b++)
        {
            for (var i = 0; i < 2 && b * 2 + i < defenders.Count; i++)
                Assert.Equal(LineupResult.Assigned, coach.Assign(LineupSlot.Defender(b, i), defenders[b * 2 + i]));
            for (var i = 0; i < 3 && b * 3 + i < forwards.Count; i++)
                Assert.Equal(LineupResult.Assigned, coach.Assign(LineupSlot.Forward(b, i), forwards[b * 3 + i]));
        }
    }
}
