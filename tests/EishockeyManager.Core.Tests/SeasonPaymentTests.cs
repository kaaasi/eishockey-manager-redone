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
        + game.RelegationGroups.Sum(g => g.Results.Count(r => r.Home == club))
        + game.CupMatches.Count(r => r.Home == club); // DEB-Pokal (Paket „wettbewerbe“)

    [Fact]
    public void EveryHomeGame_IncludingPlayoffsAndRelegation_IsSettledExactlyOnce()
    {
        var playoffs = 0;
        var relegation = 0;
        var cup = 0;
        for (var seed = 1; seed <= 3; seed++)
        {
            var game = TwoBundesligaManagers(seed);
            // Nachfrage liegt sicher über diesen kleinen Kapazitäten: jede Begegnung hat einen bekannten
            // Betrag, unabhängig vom Zuschauer-Zufall. Verschiedene Stadien decken auch falsche Zuordnungen auf.
            for (var i = 0; i < game.Managers.Count; i++)
            {
                var manager = game.Managers[i];
                manager.Money = 10_000_000; // Keine Entlassung durch die absichtlich kleinen Teststadien.
                var stadium = game.Finance(manager).Stadium;
                stadium.Seats = 11 + 6 * i;
                stadium.Standing = 0;
                stadium.TicketPrice = 7 + 6 * i;
            }
            while (game.Phase == SeasonPhase.Running)
            {
                game.Continue();
                for (var i = 0; i < game.Managers.Count; i++)
                {
                    var manager = game.Managers[i];
                    var finance = game.Finance(manager);
                    var stadium = finance.Stadium;
                    var matches = HomeGames(game, manager.ClubIndex);
                    // Exakter Betrag für jede bislang ausgetragene Begegnung statt „positiver Continue-Schritt“.
                    // Eine zweite SettleHomeMatch-Buchung desselben Spiels verletzt schon diese Gleichheit.
                    Assert.Equal((long)matches * stadium.Capacity * stadium.TicketPrice,
                        finance.Book.Current(FinanceCategory.Eintritt));
                }
            }

            for (var i = 0; i < game.Managers.Count; i++)
            {
                var club = game.Managers[i].ClubIndex;
                playoffs += game.PlayoffResults.Count(r => r.Home == club);
                relegation += game.RelegationGroups.Sum(g => g.Results.Count(r => r.Home == club));
                cup += game.CupMatches.Count(r => r.Home == club);
            }
        }
        Assert.True(playoffs > 0, "Kein Heimspiel in Play-Offs – Test prüft nichts");
        Assert.True(relegation > 0, "Kein Heimspiel in Aufstiegsrunde – Test prüft nichts");
        Assert.True(cup > 0, "Kein Heimspiel im DEB-Pokal – Test prüft nichts");
    }

    [Fact]
    public void SeasonPremium_IsBookedOnce_InOwnCategory()
    {
        // Seit Runde 3 (Finanzen Teil 1) gibt es Prämien nur noch für Titel und Aufstieg, nicht für
        // Tabellenplätze. Welche Seeds einen Titel für einen Manager-Verein ergeben, hängt von allen
        // Wettbewerben ab; deshalb mindestens drei Seeds und weiter, bis eine Prämie geprüft wurde.
        var premiums = 0L;
        // Spur spielberechnung: bis 40 Seeds – die Rechnung des Originals folgt der Stärke enger, Titel der beiden
        // Managervereine (stärkster und schwächster der Liga) sind seltener.
        for (var seed = 1; seed <= 3 || (premiums == 0 && seed <= 40); seed++)
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
                Assert.Equal(FinanceRules.StartMoney(manager.Level) + book.CurrentTotal, manager.Money);
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
