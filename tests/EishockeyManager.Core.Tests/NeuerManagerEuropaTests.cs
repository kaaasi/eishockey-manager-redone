using EishockeyManager.Core;
using EishockeyManager.Tests.Support;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// #859 (Review <c>docs/archiv/reviews/runde-19/2026-11-14-neuer-manager.md</c> P3-1): Der Zufallsplatz eines neuen Managers schließt die
/// deutschen Europapokal-Teilnehmer aus ([ORIGINAL] Datei 0x29CCC, 0x45CD0(r, 1): Zeile 1 der Tabelle DGROUP 0x5CFB). In
/// <see cref="NeuerManagerTests"/> führt Manager 0 den Verein 2, der zugleich einziger deutscher Teilnehmer ist – dort fällt der
/// Ausschluss mit dem Managerausschluss zusammen. Hier führt Manager 0 Verein 5; Verein 2 (Deutscher Meister zu Spielbeginn,
/// <c>EuropeanCup.InitialChampion</c>) darf nie gezogen werden.
/// </summary>
public class NeuerManagerEuropaTests
{
    [Fact]
    public void AddManager_DrawnPlace_NeverEuropeanCupClub_WhenManagerClubDiffers()
    {
        var hits = new HashSet<int>();
        for (var seed = 1; seed <= 80; seed++)
        {
            var setup = new GameSetup();
            setup.Managers.Add(new ManagerSetup { Name = "Test", Head = 0, ClubIndex = 5 });
            setup.StartLeague = League.Bundesliga;
            var game = NewGame.Create(TestGames.CreateDatabase(), setup, seed);
            TestSeason.PlayMatchDay(game);
            TestSeason.PlayMatchDay(game);
            var german = game.EuropeanCup!.Draw.Where(c => game.Clubs[c].League == League.Bundesliga).ToList();
            Assert.Contains(2, german);
            Assert.DoesNotContain(game.Managers[0].ClubIndex, german);

            var r = game.AddManager(new ManagerSetup { Name = "Neu", Head = 4, ClubIndex = 20 }).ClubIndex;

            Assert.DoesNotContain(r, german);
            Assert.Equal(League.Bundesliga, game.Clubs[r].League);
            hits.Add(r);
        }
        Assert.True(hits.Count >= 6, $"Zu wenige verschiedene Plätze: {string.Join(",", hits)}");
    }
}
