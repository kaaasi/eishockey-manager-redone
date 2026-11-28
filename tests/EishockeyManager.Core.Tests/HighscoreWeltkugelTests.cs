using System.Reflection;
using EishockeyManager.Core;
using Xunit;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// #826 (Bericht <c>docs/archiv/analyse/abgleich-dosbox.md</c> §3.3): Punkte der Bestenliste im Spielmodus „nur Weltkugel“.
/// [ORIGINAL] em.exe Datei 0x5D59B–0x5D607: Bei DGROUP 0xD0B9 = 1 summiert die Eintragsfunktion 0x5D4C2 über die
/// Platzierungsfolge des Landes DGROUP 0x828B + 0x50 · Land (bis zur ersten 0) je Byte 9 − Byte, mindestens 1. Die Folge
/// trägt die Plätze 1 … 8 und für alle anderen WM-Teilnehmer 11 (Datei 0x31266–0x31291), also −2 je WM ab Platz 9.
/// Vorher: Weltmeistertitel + 1.
/// </summary>
public class HighscoreWeltkugelTests
{
    private static void Record(GameState game, int germanyPlace)
    {
        var others = Enumerable.Range(NationalTeams.First, 12).Where(n => n != NationalTeams.Germany).ToList();
        others.Insert(germanyPlace - 1, NationalTeams.Germany);
        typeof(GameState).GetMethod("RecordWorldCupRanking", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(game, [others]);
    }

    private static int Score(params int[] germanyPlaces)
    {
        var game = GameModeTests.Create(7, false, true, 2);
        Assert.True(game.WorldOnly);
        foreach (var p in germanyPlaces)
            Record(game, p);
        return HighscoreRules.Entry(game, 0, 0).Score;
    }

    /// <summary>FILE54 → Bild <c>file54_4</c>: eine WM, Deutschland Vierter → 5 (vorher 1).</summary>
    [Fact]
    public void FourthPlace_Gives5_LikeFile54()
    {
        Assert.Equal(5, Score(4));
    }

    /// <summary>FILE20 → Bilder <c>highscore-weltkugel_2/_4</c>: in der Gruppe ausgeschieden → 1 (9 − 11 = −2, Untergrenze 1).</summary>
    [Fact]
    public void GroupExit_Gives1_LikeFile20()
    {
        Assert.Equal(1, Score(10));
    }

    [Fact]
    public void NoWorldCupYet_Gives1()
    {
        Assert.Equal(1, Score());
    }

    /// <summary>Summe über alle gespeicherten WMs; Plätze 9 … 12 zählen −2 (Byte 11), auch zwischen guten Plätzen.</summary>
    [Theory]
    [InlineData(new[] { 1, 1 }, 16)]
    [InlineData(new[] { 1, 12, 2 }, 8 - 2 + 7)]
    [InlineData(new[] { 8, 9 }, 1)]          // 1 − 2 = −1 → 1
    [InlineData(new[] { 3, 9, 9, 9 }, 1)]    // 6 − 6 = 0 → 1
    [InlineData(new[] { 2, 9, 9 }, 3)]
    public void SumsAllWorldCups(int[] places, int expected)
    {
        Assert.Equal(expected, Score(places));
    }
}
