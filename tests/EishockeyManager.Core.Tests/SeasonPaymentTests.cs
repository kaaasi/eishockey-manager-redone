using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Regressionstests (Runde 2, Spur wirtschaft): Jede Zahlung genau einmal im Finanzbuch –
/// Saisonprämien über <see cref="GameState.Book"/>, Heimspiele auch in Play-Offs und Aufstiegsrunde abgerechnet.
/// </summary>
public class SeasonPaymentTests
{
    /// <summary>Zwei Manager: der stärkste Bundesligist (Play-Offs) und der schwächste (Aufstiegsrunde).</summary>
    private static GameState TwoBundesligaManagers(int seed)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Oben", Head = 0, ClubIndex = 0 });
        setup.Managers.Add(new ManagerSetup { Name = "Unten", Head = 1, ClubIndex = 11 });
        return NewGame.Create(TestGames.CreateDatabase(), setup, seed);
    }

    private static int HomeGames(GameState game, int club) =>
        game.Results.Count(r => r.Home == club)
        + game.PlayoffResults.Count(r => r.Home == club)
        + game.RelegationGroups.Sum(g => g.Results.Count(r => r.Home == club));

    private static int PostseasonHomeGames(GameState game, int club) =>
        game.PlayoffResults.Count(r => r.Home == club) + game.RelegationGroups.Sum(g => g.Results.Count(r => r.Home == club));

    [Fact]
    public void EveryHomeGame_IncludingPlayoffsAndRelegation_IsSettledExactlyOnce()
    {
        var postseason = 0;
        for (var seed = 1; seed <= 3; seed++)
        {
            var game = TwoBundesligaManagers(seed);
            var paidCalls = new int[game.Managers.Count];
            while (game.Phase == SeasonPhase.Running)
            {
                var before = game.Managers.Select(m => game.Finance(m).Book.Current(FinanceCategory.Eintritt)).ToArray();
                game.Continue(); // höchstens ein Spieltag je Manager pro Aufruf
                for (var i = 0; i < game.Managers.Count; i++)
                {
                    var delta = game.Finance(game.Managers[i]).Book.Current(FinanceCategory.Eintritt) - before[i];
                    Assert.True(delta >= 0);
                    if (delta > 0)
                        paidCalls[i]++;
                }
            }

            for (var i = 0; i < game.Managers.Count; i++)
            {
                var club = game.Managers[i].ClubIndex;
                Assert.Equal(HomeGames(game, club), paidCalls[i]);
                postseason += PostseasonHomeGames(game, club);
            }
        }
        Assert.True(postseason > 0, "Kein Heimspiel in Play-Offs oder Aufstiegsrunde – Test prüft nichts");
    }

    [Fact]
    public void SeasonPremium_IsBookedOnce_InOwnCategory()
    {
        var premiums = 0L;
        for (var seed = 1; seed <= 3; seed++)
        {
            var game = TwoBundesligaManagers(seed);
            while (game.Phase == SeasonPhase.Running || game.Phase == SeasonPhase.ClubSeasonOver)
                game.Continue();

            var record = game.History[^1];
            foreach (var result in record.Managers)
            {
                var manager = game.Managers[result.Manager];
                var book = game.Finance(manager).Book;
                Assert.Equal(result.Premium, book.Current(FinanceCategory.Praemien));
                // passiver Manager: keine neutralen Umbuchungen, also Kontostand = Start + Finanzbuch
                Assert.Equal(GameState.StartMoney + book.CurrentTotal, manager.Money);
                premiums += result.Premium;
            }

            game.StartNextSeason();
            foreach (var result in record.Managers)
            {
                var book = game.Finance(game.Managers[result.Manager]).Book;
                Assert.Equal(result.Premium, book.Previous(FinanceCategory.Praemien)); // Vorsaison im Finanzbuch
                Assert.Equal(0, book.Current(FinanceCategory.Praemien));
            }
        }
        Assert.True(premiums > 0, "Keine Prämie gezahlt – Test prüft nichts");
    }
}
