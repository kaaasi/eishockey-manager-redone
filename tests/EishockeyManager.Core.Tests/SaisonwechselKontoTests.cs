using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur saisonwechsel, Teil 3 (E5/S-K1): Konto am 29. April. [ORIGINAL] Datei 0x5629D–0x562DE (docs/analyse/re-runde-12.md,
/// „E5“): genau drei Bedingungen – Verein unter den letzten zwei der Oberliga (Gruppe 2, Datei 0x562C6), Verein eines
/// Managers (+0x16 ≠ 0), kein Zwangsabstieg (Bit 0x04). Keine Kontoprüfung: hohes Guthaben wird auf 400.000 DM gekürzt,
/// Schulden werden gehoben; Bundesliga und 2. Liga nie.
/// </summary>
public class SaisonwechselKontoTests
{
    private static GameState Create(int seed, params int[] clubs)
    {
        var setup = new GameSetup { Duration = GameDuration.Unbegrenzt, EuropeAndWorldCup = true };
        for (var i = 0; i < clubs.Length; i++)
            setup.Managers.Add(new ManagerSetup { Name = $"M{i}", Head = i, ClubIndex = clubs[i] });
        return NewGame.Create(TestGames.CreateDatabase(), setup, seed);
    }

    /// <summary>Ergebnisse so umschreiben, dass <paramref name="last"/> in seiner Liga Letzter, <paramref name="secondLast"/> Vorletzter wird.</summary>
    private static void ForceBottom(GameState game, League league, int last, int secondLast)
    {
        int Goals(int club, int other, int original) =>
            club == last ? 0 : club == secondLast ? (other == last ? 1 : 0) : other == last || other == secondLast ? 10 : original;
        for (var i = 0; i < game.Results.Count; i++)
            if (game.Results[i] is { } r && r.League == league && (r.Involves(last) || r.Involves(secondLast)))
                game.Results[i] = r with { HomeGoals = Goals(r.Home, r.Away, r.HomeGoals), AwayGoals = Goals(r.Away, r.Home, r.AwayGoals) };
    }

    private static SeasonRecord PlaySeason(GameState game, League league, int last, int secondLast, long money)
    {
        for (var guard = 0; !game.RegularSeasonOver; guard++)
        {
            Assert.True(guard < 2000, "Hauptrunde nicht beendet");
            foreach (var m in game.Managers)
            {
                m.Money = 10_000_000;
                TestSeason.KeepSquad(game, m);
            }
            TestSeason.PlayMatchDay(game);
            ForceBottom(game, league, last, secondLast);
        }
        for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
        {
            Assert.True(guard < 1000, "Saisonende nicht erreicht");
            foreach (var m in game.Managers)
                m.Money = money;
            game.Continue();
        }
        return game.History[^1];
    }

    private static int Other(GameState game, League league, params int[] except) =>
        game.LeagueClubs(league).Select(c => c.Index).First(c => !except.Contains(c));

    [Fact]
    public void OberligaLast_HighBalance_CutTo400k()
    {
        var game = Create(seed: 41, 30);
        var record = PlaySeason(game, League.Oberliga, 30, Other(game, League.Oberliga, 30), money: 2_000_000);

        Assert.True(GameState.KeptInOberliga(record, 30));
        Assert.Equal(GameState.OberligaKeptMoney + record.Managers[0].Premium, game.Managers[0].Money);
    }

    [Fact]
    public void OberligaSecondLast_Debts_RaisedTo400k()
    {
        var game = Create(seed: 43, 30);
        var record = PlaySeason(game, League.Oberliga, Other(game, League.Oberliga, 30), 30, money: -500_000);

        Assert.True(GameState.KeptInOberliga(record, 30));
        Assert.Equal(GameState.OberligaKeptMoney + record.Managers[0].Premium, game.Managers[0].Money);
    }

    [Theory]
    [InlineData(2_000_000)]
    [InlineData(-500_000)]
    public void SecondLeagueLast_MoneyUnchanged(long money)
    {
        // Gruppe 1 (2. Liga): Datei 0x562C6 springt bei Gruppe < 2 am Setzen vorbei; normaler Tausch, Konto bleibt.
        var game = Create(seed: 45, 20);
        Assert.Equal(League.ZweiteLiga, game.Clubs[20].League);
        var record = PlaySeason(game, League.ZweiteLiga, 20, Other(game, League.ZweiteLiga, 20), money);

        Assert.Equal(20, record.FinalTables[League.ZweiteLiga][^1]);
        Assert.False(GameState.KeptInOberliga(record, 20));
        Assert.Equal(money + record.Managers[0].Premium, game.Managers[0].Money);
    }
}
