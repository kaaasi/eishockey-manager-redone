using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

public class DeadlineRegressionTests
{
    [Fact]
    public void R208_AnswerChecksDeadlineAtPausedUnprocessedDay()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "A", ClubIndex = 2 });
        setup.Managers.Add(new ManagerSetup { Name = "B", ClubIndex = 17 });
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, 3);
        LifecycleRegressionTests.FinishSeason(game);
        var request = game.RequestLoan(0, 1, 10_000)!;
        game.StartNextSeason();
        while (game.Day < 3)
            game.Continue();
        Assert.True(request.IsQuestion);
        var requestAtThree = game.RequestLoan(0, 1, 11_000)!;
        while (game.Day < 10)
            game.Continue();
        // Tag 10 ist noch nicht verarbeitet. Die Frage von Tag 3 ist exakt sieben Tage alt.
        Assert.Equal(10, game.Day);
        Assert.Equal(9, game.FinanceDay);
        Assert.True(requestAtThree.IsQuestion);
        var money = game.Managers.Select(m => m.Money).ToArray();
        Assert.False(game.Answer(game.Managers[1], requestAtThree, true));
        Assert.Equal(money, game.Managers.Select(m => m.Money));
    }

    [Fact]
    public void R208_RequestOnDay231ExpiresInNextSeasonAndCannotBeAnswered()
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "A", ClubIndex = 2 });
        setup.Managers.Add(new ManagerSetup { Name = "B", ClubIndex = 17 });
        var game = NewGame.Create(TestGames.CreateDatabase(), setup, 3);
        LifecycleRegressionTests.FinishSeason(game);
        Assert.Equal(231, game.Day);
        var request = game.RequestLoan(0, 1, 10_000)!;

        game.StartNextSeason();
        Assert.True(request.IsQuestion); // Ein Tag alt, noch nicht pauschal löschen.
        for (var guard = 0; game.Day < 28; guard++)
        {
            Assert.True(guard < 100);
            game.Continue();
        }
        Assert.False(request.IsQuestion);
        var money = game.Managers.Select(m => m.Money).ToArray();
        Assert.False(game.Answer(game.Managers[1], request, true));
        Assert.Equal(money, game.Managers.Select(m => m.Money));
    }
}
