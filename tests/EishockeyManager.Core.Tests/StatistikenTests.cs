using System.Text.Json.Nodes;
using EishockeyManager.Core;
using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur statistiken (Runde 8, #135): Ranglisten „Die besten …“ (em.exe 0x25E30), Fair-Play-Tabelle (0x2A1E0) und die
/// Strafminuten der Ligaspiele (0x35967). Belege in <c>StatisticsPlayers.cs</c> und <c>StatisticsClub.cs</c>.
/// Gegen Base 10eb6c5 rot (Ranglisten, Fair-Play und Strafminutenzähler fehlten).
/// </summary>
public class StatistikenTests
{
    private static GameState NewGame1(int seed = 3)
    {
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Test", Head = 0, ClubIndex = 5, StartLeague = League.Bundesliga });
        return NewGame.Create(TestGames.CreateDatabase(), setup, seed);
    }

    private static Player Skater(GameState game, int club, int nth = 0) =>
        game.Clubs[club].Players.Where(p => p.Role != PlayerRole.Goalkeeper).ElementAt(nth);

    private static Player Goalie(GameState game, int club, int nth = 0) =>
        game.Clubs[club].Players.Where(p => p.Role == PlayerRole.Goalkeeper).ElementAt(nth);

    private static void Set(Player p, int games, int goals, int assists)
    {
        p.Games = games;
        p.Goals = goals;
        p.Assists = assists;
    }

    [Fact]
    public void Scorer_SortsByPoints_TieByFewerGames_AndNeedsTwoPoints()
    {
        var game = NewGame1();
        var a = Skater(game, 1);
        var b = Skater(game, 2);
        var c = Skater(game, 3);
        var d = Skater(game, 4);
        var e = Skater(game, 6);
        Set(a, 10, 3, 3);   // 6 Punkte, 10 Spiele
        Set(b, 8, 1, 5);    // 6 Punkte, 8 Spiele → vor a
        Set(c, 5, 7, 0);    // 7 Punkte
        Set(d, 5, 1, 0);    // 1 Punkt → nicht in der Liste
        Set(e, 5, 0, 2);    // 2 Punkte → gerade drin

        var skaters = PlayerStatistics.Ranking(game, League.Bundesliga, SkaterRanking.Scorer).Where(x => !x.IsGoalie).ToList();
        Assert.Equal([c, b, a, e], skaters.Select(x => x.Player));
        Assert.Equal([1, 2, 3, 4], skaters.Select(x => x.Rank));
        Assert.Same(c, PlayerStatistics.TopScorer(game, League.Bundesliga)!.Player);
    }

    [Fact]
    public void GoalsAndAssistsModes_UseOnlyTheirCounter_ButListByPoints()
    {
        var game = NewGame1();
        var a = Skater(game, 1);
        var b = Skater(game, 2);
        Set(a, 5, 4, 1);
        Set(b, 5, 1, 6);

        Assert.Equal([a, b], PlayerStatistics.Ranking(game, League.Bundesliga, SkaterRanking.Goals).Select(x => x.Player));
        Assert.Equal([b, a], PlayerStatistics.Ranking(game, League.Bundesliga, SkaterRanking.Assists).Select(x => x.Player));
        Assert.Equal(4, PlayerStatistics.Key(a, SkaterRanking.Goals));
        Assert.Equal(1, PlayerStatistics.Key(a, SkaterRanking.Assists));
        Assert.Equal(5, PlayerStatistics.Key(a, SkaterRanking.Scorer));
    }

    [Fact]
    public void Goalies_From400Minutes_ByGoalsAgainstPerMinute_399AtTheEnd()
    {
        var game = NewGame1();
        var g1 = Goalie(game, 1);
        var g2 = Goalie(game, 2);
        var g3 = Goalie(game, 3);
        var g4 = Goalie(game, 4);
        (g1.GoalsAgainst, g1.GoalieMinutes) = (20, 600);   // 200 Hundertstel je Spiel
        (g2.GoalsAgainst, g2.GoalieMinutes) = (10, 400);   // 150 → vorn
        (g3.GoalsAgainst, g3.GoalieMinutes) = (0, 399);    // aufgenommen, aber Schlüssel −99.999
        (g4.GoalsAgainst, g4.GoalieMinutes) = (0, 398);    // nicht aufgenommen

        var goalies = PlayerStatistics.Ranking(game, League.Bundesliga, SkaterRanking.Scorer).Where(x => x.IsGoalie).ToList();
        Assert.Equal([g2, g1, g3], goalies.Select(x => x.Player));
        Assert.Equal(-150, PlayerStatistics.Key(g2, SkaterRanking.Scorer));
        Assert.Equal(PlayerStatistics.NoGoalieKey, PlayerStatistics.Key(g3, SkaterRanking.Scorer));
        Assert.Equal(150, PlayerStatistics.GoalsAgainstPerGameHundredths(g2));
        Assert.Same(g2, PlayerStatistics.BestGoalie(game, League.Bundesliga)!.Player);
    }

    [Fact]
    public void Ranking_OnlyTheGivenLeague()
    {
        var game = NewGame1();
        var bundesliga = Skater(game, 1);
        var zweite = Skater(game, 13);
        Set(bundesliga, 5, 2, 0);
        Set(zweite, 5, 30, 30);

        Assert.Equal([bundesliga], PlayerStatistics.Ranking(game, League.Bundesliga, SkaterRanking.Scorer).Select(x => x.Player));
        Assert.Equal([zweite], PlayerStatistics.Ranking(game, League.ZweiteLiga, SkaterRanking.Scorer).Select(x => x.Player));
        Assert.Null(PlayerStatistics.BestGoalie(game, League.Bundesliga));
    }

    [Fact]
    public void GoalsPerGame_InHundredths()
    {
        var game = NewGame1();
        var a = Skater(game, 1);
        Set(a, 15, 7, 0);
        Assert.Equal(46, PlayerStatistics.GoalsPerGameHundredths(a));
        Set(a, 0, 3, 0);
        Assert.Equal(300, PlayerStatistics.GoalsPerGameHundredths(a));
    }

    [Fact]
    public void FairPlay_AscendingByPenaltyMinutes_ExchangeSortOrderOnTies()
    {
        var game = NewGame1();
        foreach (var club in game.LeagueClubs(League.Bundesliga))
            club.LeaguePenaltyMinutes = 100 + club.Index;
        game.Clubs[7].LeaguePenaltyMinutes = 20;
        game.Clubs[3].LeaguePenaltyMinutes = 20;
        game.Clubs[9].LeaguePenaltyMinutes = 10;

        var table = game.FairPlayTable(League.Bundesliga);
        Assert.Equal(12, table.Count);
        // Austauschsortierung wie 0x2A21D: Platz 1 bekommt bei Gleichstand der kleinere Index, dahinter ist sie nicht
        // stabil (i = 0 holt erst 3, dann 9; i = 1 tauscht 1 ↔ 0 ↔ 7 – Verein 7 landet vor Verein 3).
        Assert.Equal([9, 7, 3, 0, 1], table.Take(5).Select(r => r.Club.Index));
        Assert.Equal(Enumerable.Range(1, 12), table.Select(r => r.Rank));
        Assert.Equal(0, table[0].Games);
        Assert.Equal(1000, table[0].PerGameHundredths); // ohne Spiele durch 1
    }

    [Fact]
    public void PenaltyMinutes_CountOnlyLeagueMatches_AndMatchTheReports()
    {
        var game = NewGame1();
        for (var i = 0; i < 4; i++)
            TestSeason.PlayMatchDay(game);
        var total = game.Clubs.Sum(c => c.LeaguePenaltyMinutes);
        Assert.True(total > 0, "Keine Strafminute in vier Spieltagen – Test prüft nichts");
        Assert.All(game.Clubs.Where(c => c.League is null), c => Assert.Equal(0, c.LeaguePenaltyMinutes));
        var row = game.FairPlayTable(League.Bundesliga)[0];
        Assert.Equal(4, row.Games);
        Assert.Equal(row.PenaltyMinutes * 100 / 4, row.PerGameHundredths);
    }

    [Fact]
    public void PenaltyMinutes_SurviveSaveAndLoad()
    {
        var game = NewGame1();
        TestSeason.PlayMatchDay(game);
        game.Clubs[2].LeaguePenaltyMinutes = 77;
        var bytes = SaveGame.Save(game);
        var loaded = SaveGame.Load(bytes);
        Assert.True(loaded.Success, loaded.Error);
        Assert.Equal(77, loaded.Game!.Clubs[2].LeaguePenaltyMinutes);
        Assert.Equal(bytes, SaveGame.Save(loaded.Game));
    }

    /// <summary>Migration der Spur (vorläufig 11 → 12): ältere Spielstände ohne Strafminuten laden mit 0.</summary>
    [Fact]
    public void OldSave_WithoutPenaltyMinutes_LoadsWithZero()
    {
        var game = NewGame1();
        TestSeason.PlayMatchDay(game);
        var root = JsonNode.Parse(SaveGame.Save(game))!.AsObject();
        root["schema"] = SaveGame.SchemaVersion - 1;
        var removed = 0;
        foreach (var club in root["vereine"]!.AsArray())
            if (club!.AsObject().Remove("LeaguePenaltyMinutes"))
                removed++;
        Assert.Equal(game.Clubs.Count, removed);
        var loaded = SaveGame.Load(System.Text.Encoding.UTF8.GetBytes(root.ToJsonString()));
        Assert.True(loaded.Success, loaded.Error);
        Assert.All(loaded.Game!.Clubs, c => Assert.Equal(0, c.LeaguePenaltyMinutes));
    }

    [Fact]
    public void Screens_ReadOnly_RankingDoesNotChangeState()
    {
        var game = NewGame1();
        TestSeason.PlayMatchDay(game);
        var before = SaveGame.Save(game);
        _ = PlayerStatistics.Ranking(game, League.Bundesliga, SkaterRanking.Goals);
        _ = game.FairPlayTable(League.Oberliga);
        _ = game.SinglePremiumEvents(game.ActiveManager, League.Bundesliga);
        Assert.Equal(before, SaveGame.Save(game));
    }
}
