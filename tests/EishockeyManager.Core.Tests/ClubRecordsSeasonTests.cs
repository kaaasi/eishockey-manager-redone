using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// ui-rest Teil 2 (#535): Vereinsrekorde aus Pokal- und Play-off-Spielen. [ORIGINAL] 0x50283 springt nach der Begegnungstabelle
/// (Datei 0x5030D–0x5031B: <c>cmp [0xD0C2], 7 / 8</c>, sonst Ende) ans Ende, bevor die Rekorde geschrieben werden (0x503A4 ff.);
/// der Wettbewerbscode ist beim DEB-Pokal 6 (Datei 0x35E7C), bei Play-offs und Aufstiegsrunden 1 (0x31AB7), bei der Liga 7
/// (0x35EC4). Pokal- und Play-off-Spiele tragen also keine Rekorde ein; vorher war das nur als <c>[APPROX]</c> vermerkt.
/// </summary>
public class ClubRecordsSeasonTests
{
    [Theory]
    [InlineData(5)]
    [InlineData(6)]
    public void Season_WithCupAndPlayoffs_RecordsComeOnlyFromLeagueMatches(int seed)
    {
        var game = TestGames.Create(seed: seed);
        Assert.True(game.Setup.DebCup);
        var cupMatches = 0;
        for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
        {
            Assert.True(guard < 2000, "Saisonende nicht erreicht");
            foreach (var manager in game.Managers)
                manager.Money = Math.Max(manager.Money, 10_000_000);
            if (game.Phase == SeasonPhase.Running && game.MatchOn(game.CurrentDay, game.ActiveManager.ClubIndex) is { Competition: Competition.DebCup })
                cupMatches++;
            game.Continue();
        }
        Assert.True(cupMatches > 0, "kein Pokalspiel gespielt");
        Assert.NotEmpty(game.Results);
        var titles = game.History[^1].Titles;
        Assert.True(titles.ContainsKey(Title.CupWinner), "DEB-Pokal nicht zu Ende gespielt");
        Assert.True(titles.ContainsKey(Title.GermanChampion), "Play-offs nicht zu Ende gespielt");

        var words = new int[TeamRecords.Teams * TeamRecords.Kinds];
        var opponents = new int[TeamRecords.Teams * TeamRecords.Kinds];
        foreach (var r in game.Results)
            TeamRecords.Enter(words, opponents, r.Home, r.Away, r.HomeGoals, r.AwayGoals);

        for (var club = 0; club < NationalTeams.First; club++)
            foreach (var kind in Enum.GetValues<TeamRecordKind>())
            {
                var i = TeamRecords.Index(club, kind);
                Assert.Equal((words[i], opponents[i]), game.TeamRecord(club, kind));
            }
    }
}
