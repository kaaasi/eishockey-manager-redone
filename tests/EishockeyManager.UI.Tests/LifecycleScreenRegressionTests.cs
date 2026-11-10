using EishockeyManager.Tests.Support;
using EishockeyManager.Core;
using EishockeyManager.UI.Screens.Team;

namespace EishockeyManager.UI.Tests;

public class LifecycleScreenRegressionTests
{
    [GameDataFact]
    public void R204_OriginalDataLoanAndContractEndAreConsistentAfterContinueThroughSeason()
    {
        var driver = FlowTests.CreateDriver();
        driver.StartNewGame();
        var game = driver.Host.Game!;
        game.ActiveManager.Money = 10_000_000;
        var listing = game.TransferMarket.Listings.First();
        var player = listing.Player;
        var origin = listing.From;
        player.Age = 27;
        player.ContractYears = 3;
        Assert.Equal(TransferResult.Accepted,
            // Über 130 % der Leihgebühr nimmt der Computermarkt immer an (em.exe 0x4991D, Spur transfer-original).
            game.TransferMarket.Buy(game, game.ActiveManager, listing, listing.LoanFee * 135 / 100, true));
        var club = game.Clubs[game.ActiveManager.ClubIndex];
        var expired = club.Players.First(p => !p.OnLoan);
        expired.ContractYears = 1;
        for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
        {
            Assert.True(guard < 500);
            game.Continue();
        }
        game.StartNextSeason();
        Assert.Equal(28, player.Age);
        Assert.False(player.OnLoan);
        // Integration Runde 9 (Leitstelle): Der Markt kommt jetzt aus dem Spielerpool (transfer-vertraege-komplett Teil 5,
        // 0x494EF). Ein geliehener Poolspieler kehrt in seinen Poolplatz zurück; ob dieser Platz beim Saisonwechsel in den
        // Kader seines Vereins wechselt, hängt am Tausch Pool ↔ Computerkader. Stimmig sind beide Ausgänge (im Verein mit um ein
        // Jahr verkürztem Vertrag oder im Pool ohne Vertrag – nie in beiden, nie beim Manager); der Ablauf ist aber
        // deterministisch, geprüft wird deshalb genau der beobachtete (#353, R9S-05). Bis Teil 4 der Spur kader-transfer: Pool
        // (Verein 9); seit Teil 5 (Poolumverteilung beim Spielstart, andere erste Liste): Nationalspieler, Tausch in den Kader von
        // Verein 59 (0x5ACA0 Art 0). Ändert sich der Zufallsverlauf, hier den neuen Ausgang eintragen und begründen.
        // Integration Runde 11 (geld + saisonwechsel + kader-transfer): anderer Zufallsverlauf der Hauptquelle, jetzt der
        // zweite stimmige Ausgang – zurück im Poolplatz (Herkunft Verein 59), ohne Vertrag, in keinem Kader.
        // Runde 12, wettbewerbe-spiel Teil 2 (A184, Belastung + 1 je Spielminute und Grenzen nach dem Spiel ändern Ergebnisse
        // und Zufallsverlauf): wieder der erste stimmige Ausgang – im Kader von Verein 59 mit um ein Jahr verkürztem Vertrag.
        // Spur kader-transfer, Runde 12, Teil 2 (U9): fester Spielplan, anderer Spielverlauf – der Tausch Pool ↔ Computerkader
        // erreicht den Spieler jetzt: Vereinskader von Verein 59 (Rückkehr), mit laufendem Vertrag, in keinem Poolplatz.
        // Integration Runde 12 (wettbewerbe-spiel + kader-transfer): Ausgang wie bei wettbewerbe-spiel Teil 2 (Prüfung unten).
        // Runde 13, kader-start Teil 2 (U15-a, L13): Verein 42 tauscht mit Start-Liga Oberliga in die Oberliga (Datei 0x29CF2,
        // Teamwerte des Platzes, Oberliga-Kader) – anderer Zufallsverlauf, wieder der zweite stimmige Ausgang (Poolplatz).
        // Teil 3 (R-K5): Satzfolge vor der Spielvorbereitung, Zufallsplatz als Satzplatz – wieder der erste Ausgang (Verein 59).
        // Runde 14, kader-start Teil 4 (A115, re-runde-14 FILE44 → FILE46 VALLIERE): Eine Leihe vom Markt endet am Saisonende frei
        // (Pool +0x1A = 6, Datei 0x275FF) – der Spieler kehrt nicht mehr mit seinem Vertrag zum Herkunftsverein zurück. Stimmig ist
        // damit nur noch: in keinem Kader mit dem alten Vertrag (3 → 2 Jahre), höchstens in einem Kader (neu verpflichtet).
        // Runde 15, Spur tagesablauf (#699 Punkt 2, R15-E1 = A): Tag 0 läuft vor dem ersten Menü, samt Listenwechsel-Wurf von
        // Manager 0 (Datei 0x54E65–0x54E7C, Tag 0 hat Ereignis) und anderem Zufallsverlauf – der erste Markt-Platz kommt jetzt
        // von Verein 24 (vorher 59); die Prüfungen danach unverändert.
        Assert.Equal(24, origin.Index);
        Assert.False(origin.Players.Contains(player) && player.ContractYears == 2);
        Assert.True(game.Clubs.Count(c => c.Players.Contains(player)) <= 1);
        Assert.DoesNotContain(player, club.Players);
        Assert.DoesNotContain(expired, club.Players);
        Assert.All(game.Clubs, c => Assert.All(c.Lineup.Players, p => Assert.Contains(p, c.Players)));
    }

    [GameDataFact]
    public void R204_OpeningLineupDoesNotCleanOrChangeTheLineup()
    {
        var driver = FlowTests.CreateDriver();
        driver.StartNewGame();
        var club = driver.Host.Game!.Clubs[driver.Host.Game.ActiveManager.ClubIndex];
        var player = club.Lineup.Players.First();
        player.InjuredGames = 1;
        var before = club.Lineup.Players.ToArray();
        driver.Host.Show(new LineupScreen(driver.Host, driver.Host.Current!));
        Assert.Equal(before, club.Lineup.Players);
    }
}
