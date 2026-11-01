using System.Reflection;
using EishockeyManager.Core;
using Xunit;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur wettbewerbe-komplett Teil 2 (Nationalteam): Schreibstelle der WM-Platzierungen (em.exe Datei 0x311CF–0x31351).
/// </summary>
public class WettbewerbeNationalteamTests
{
    private static void Record(GameState game, IReadOnlyList<int> ranking) =>
        typeof(GameState).GetMethod("RecordWorldCupRanking", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(game, [ranking]);

    /// <summary>WM k: Land k + 1 (Folge um k gedreht) wird Weltmeister.</summary>
    private static int[] Ranking(int k) =>
        [.. Enumerable.Range(0, 12).Select(i => NationalTeams.First + (i + k) % 12)];

    /// <summary>
    /// [ORIGINAL] Datei 0x311F8–0x3125C: Hat die Platzierungsfolge schon 16 Einträge, rücken alle zwölf Folgen um ein Byte
    /// nach vorn – die älteste WM entfällt, die WM-Statistik zeigt und zählt die letzten 16. Vorher behielt das Remake alle
    /// WMs: 17 Einträge, die Punkte zählten auch die erste WM, der Bildschirm zeigte die ersten 16 Spalten.
    /// </summary>
    [Fact]
    public void SeventeenthWorldCup_DropsTheOldest()
    {
        var game = GameModeTests.Create(7, false, true, 2);
        for (var k = 0; k < 17; k++)
            Record(game, Ranking(k));

        Assert.Equal(GameState.MaxWorldCupRankings, game.WorldCupRankings.Count);
        Assert.Equal(Ranking(1), game.WorldCupRankings[0]);
        Assert.Equal(Ranking(16), game.WorldCupRankings[^1]);
        // Land First war nur in WM 0 Weltmeister (8 P.); in WM 1 … 16 steht es auf Platz 12, 11, …, 1 (gedreht).
        var expected = Enumerable.Range(1, 16).Select(k => Ranking(k).ToList().IndexOf(NationalTeams.First) + 1)
            .Where(p => p <= 8).Sum(p => 9 - p);
        Assert.Equal(expected, game.WorldCupPoints(NationalTeams.First));
        Assert.Equal(36 * 16, Enumerable.Range(NationalTeams.First, 12).Sum(game.WorldCupPoints));
    }

    [Fact]
    public void SixteenWorldCups_AllStay()
    {
        var game = GameModeTests.Create(7, false, true, 2);
        for (var k = 0; k < 16; k++)
            Record(game, Ranking(k));
        Assert.Equal(16, game.WorldCupRankings.Count);
        Assert.Equal(Ranking(0), game.WorldCupRankings[0]);
    }
}
