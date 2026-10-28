using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>Befunde R3A-02 (#61) und R3A-03 (#62) aus docs/reviews/2026-10-28-integration-runde-3a.md.</summary>
public class IntegrationRound3ReviewTests
{
    /// <summary>
    /// R3A-02: Heimrecht mit unabhängigen Erwartungen statt mit <see cref="CupTie.Pairing"/> selbst prüfen –
    /// Spiel 1 und 3 beim zuerst Gezogenen, Spiel 2 beim anderen.
    /// </summary>
    [Fact]
    public void R3A02_DebCupHomeRight_AlternatesFirstSecondFirst()
    {
        var tie = new CupTie(7, 11);
        Assert.Equal((7, 11), CupTie.Pairing(tie, 0));
        Assert.Equal((11, 7), CupTie.Pairing(tie, 1));
        Assert.Equal((7, 11), CupTie.Pairing(tie, 2));
    }

    /// <summary>R3A-03: Eine zweite Ernennung in derselben Saison bucht keinen zweiten Etat und leert den Kader nicht.</summary>
    [Fact]
    public void R3A03_SecondAppointment_DoesNotBookBudgetAgainOrResetSquad()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Trainer", Head = 0, ClubIndex = 2 });
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, 3);
        var manager = game.ActiveManager;
        var coach = game.AppointNationalCoach(manager);
        game.AutoNominate();
        var squad = coach.Squad.ToArray();
        Assert.NotEmpty(squad);
        var money = manager.Money;

        var again = game.AppointNationalCoach(manager);

        Assert.Same(coach, again);
        Assert.Equal(money, manager.Money);
        Assert.Equal(squad, again.Squad);
    }
}
