using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur saisonwechsel, Teil 3 (S-K4, P2 Nr. 13): Ewige Tabelle nach dem Austausch – Ligapunkte der nächsten Saison, Platz der
/// gespielten (em.exe Datei 0x563AF → 0x5667A; DOSBox-Beleg `file14_3`, docs/analyse/re-saisonwechsel.md §4).
/// </summary>
public class SaisonwechselEwigeTabelleTests
{
    private static bool HasTitle(SeasonRecord record, int club) => record.Titles.Values.Contains(club);

    [Fact]
    public void PromotedAndRelegated_GetPointsOfTheirNextLeague()
    {
        // Aufsteiger aus der 2. Liga: 40 + 12 − Platzfeld (Weißwasser, Platzfeld 1: 51); Absteiger aus der Bundesliga:
        // 20 + 12 − Platzfeld (Platzfeld 10: 22); Oberliga-Absteiger: 0. Vorher: Liga der gespielten Saison (35 bzw. 42 bzw. 12 − p).
        var game = SaisonwechselZwangsabstiegTests.Create(seed: 61, clubs: 30);
        var record = SaisonwechselZwangsabstiegTests.PlaySeason(game, _ => 1_000_000);
        var checkedClubs = 0;
        foreach (var club in record.PromotedInto[League.Bundesliga].Where(c => !HasTitle(record, c)))
        {
            var p = record.FinalTables[League.ZweiteLiga].ToList().IndexOf(club);
            Assert.Equal(40 + 12 - p, game.Eternal(club).Points);
            checkedClubs++;
        }
        foreach (var club in record.RelegatedInto[League.ZweiteLiga].Where(c => !HasTitle(record, c)))
        {
            var p = record.FinalTables[League.Bundesliga].ToList().IndexOf(club);
            Assert.Equal(20 + 12 - p, game.Eternal(club).Points);
            checkedClubs++;
        }
        foreach (var club in record.LeftOberliga.Where(c => !GameState.KeptInOberliga(record, c) && !HasTitle(record, c)))
        {
            Assert.Equal(0, game.Eternal(club).Points);
            checkedClubs++;
        }
        Assert.True(checkedClubs >= 2, "Testaufbau: kein Wechsel ohne Titel");
    }

    [Theory]
    [InlineData(League.Bundesliga, 1, false, false, false, 51)]  // ES Weißwasser: Bundesliga-Punkte für Platzfeld 1
    [InlineData(League.ZweiteLiga, 0, false, false, false, 35)] // Erster der Oberliga, aufgestiegen: 20 + 12 + 3
    [InlineData(League.Bundesliga, 0, false, false, false, 52)] // Erster der 2. Liga, aufgestiegen: kein +3 (Index < 12)
    [InlineData(League.Oberliga, 11, false, false, false, 1)]   // Krefelder EV: Letzter, in der Oberliga geblieben
    public void EternalPoints_DosBoxValues(League next, int rank0, bool champion, bool cup, bool europe, int expected) =>
        Assert.Equal(expected, GameState.EternalPoints(next, rank0, champion, cup, europe));

    [Fact]
    public void ManagerKeptInOberliga_OnePoint()
    {
        var game = SaisonwechselZwangsabstiegTests.Create(seed: 63, clubs: 30);
        var record = PlayWithManagerLast(game);
        Assert.True(GameState.KeptInOberliga(record, 30));
        Assert.Equal(1, game.Eternal(30).Points); // [ORIGINAL] file14_3: Krefelder EV 1 P. = 12 − 11
    }

    private static SeasonRecord PlayWithManagerLast(GameState game)
    {
        // Verein 30 verliert alle Oberligaspiele (Testaufbau).
        for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
        {
            Assert.True(guard < 3000);
            foreach (var m in game.Managers)
            {
                m.Money = 10_000_000;
                TestSeason.KeepSquad(game, m);
            }
            if (game.RegularSeasonOver)
                game.Continue();
            else
                TestSeason.PlayMatchDay(game);
            for (var i = 0; i < game.Results.Count; i++)
                if (game.Results[i] is { League: League.Oberliga } r && r.Involves(30))
                    game.Results[i] = r.Home == 30 ? r with { HomeGoals = 0, AwayGoals = 10 } : r with { HomeGoals = 10, AwayGoals = 0 };
        }
        return game.History[^1];
    }

    [Fact]
    public void ForcedRelegation_CountsWithSecondLeaguePoints()
    {
        // Zwangsabstieg (Teil 2) läuft vor der Ewigen Tabelle (em.exe 0x55F17 vor 0x563AF): Platz 9 der Bundesliga, danach
        // 2. Liga → 20 + 12 − 8 = 24 statt 40 + 12 − 8 = 44.
        var game = SaisonwechselZwangsabstiegTests.Create(seed: 41, clubs: 5);
        var record = SaisonwechselZwangsabstiegTests.PlaySeason(game, _ => -700_000, League.Bundesliga, null, 5);
        Assert.Contains(SeasonEvent.ForcedRelegation, record.Managers[0].Events);
        Assert.Equal(5, record.FinalTables[League.Bundesliga][8]);
        Assert.False(HasTitle(record, 5));
        Assert.Equal(24, game.Eternal(5).Points);
    }
}
