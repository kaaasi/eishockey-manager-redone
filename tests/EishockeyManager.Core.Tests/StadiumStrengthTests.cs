using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur geld, Teil 2 (Runde 12): Die Stärke eines Vereins für die Zuschauerformel liest wie das Original die Vereinsfelder
/// 0x18–0x1D (Kondition, Technik, Form von Abwehr und Sturm), nicht mehr die Spielerwerte. [ORIGINAL] Datei 0x4D316 (0x128:0x25):
/// Summe der sechs Felder / 6; Belege A035, docs/spielberechnung.md „Finanzen unverändert“.
/// </summary>
public class StadiumStrengthTests
{
    private static GameState Create()
    {
        var game = TestGames.Create(seed: 4);
        MatchEngine.EnsureTeamValues(game);
        return game;
    }

    private static Club ComputerClub(GameState game) =>
        game.Clubs.First(c => c.League == League.Bundesliga && !game.Managers.Any(m => m.ClubIndex == c.Index) && c.Players.Count > 0);

    /// <summary>Computerverein: (4 · Teamwert + Streuungen + beide Formwerte) / 6 aus den Feldern des Laders und der Wanderung.</summary>
    [Fact]
    public void ComputerClub_IsMeanOfTheSixClubFields()
    {
        var game = Create();
        var club = ComputerClub(game);
        club.TeamValues = new ClubTeamValues(DefenseCondition: 3, AttackCondition: -2, DefenseTechnique: 5, AttackTechnique: 1, DefenseForm: 48, AttackForm: 52, DayForm: 50);
        var value = MatchEngine.TeamValue(club);
        Assert.Equal((4 * value + 3 - 2 + 5 + 1 + 48 + 52) / 6, SpectatorRules.Strength(game, club));
    }

    /// <summary>Die Form des Vereinsfelds 0x1C geht ein (vorher: Mittel der Spielerform), die Spielerwerte nicht.</summary>
    [Fact]
    public void ClubFieldChange_MovesStrength_PlayerValuesDoNot()
    {
        var game = Create();
        var club = ComputerClub(game);
        var before = SpectatorRules.Strength(game, club);
        club.TeamValues = club.TeamValues!.Value with { DefenseForm = club.TeamValues.Value.DefenseForm + 30 };
        Assert.Equal(before + 5, SpectatorRules.Strength(game, club));
        var again = SpectatorRules.Strength(game, club);
        foreach (var player in club.Players)
            player.Form = Math.Min(99, player.Form + 20);
        Assert.Equal(again, SpectatorRules.Strength(game, club));
    }
}
