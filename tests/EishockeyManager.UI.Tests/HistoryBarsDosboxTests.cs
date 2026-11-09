using System.Reflection;
using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens.Statistics;

namespace EishockeyManager.UI.Tests;

/// <summary>ui-rest Teil 2 (A357): Balken und Markierung der Vereinshistorie nach DOSBox-Sitzung 2, <c>file14_5</c>.</summary>
public class HistoryBarsDosboxTests
{
    [GameDataFact]
    public void Bars_ColorsHeightsAndMarker_LikeOriginal()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        var field = typeof(GameState).GetField("_managerHistory", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var map = (Dictionary<int, List<ManagerSeasonHistory>>)field.GetValue(game)!;
        // „12. Platz der Oberliga“ (Rang 12, 1-basiert wie im Kern: `StatisticsHistory` speichert IndexOf + 1), „DEB-Pokal: Erste Runde“
        map[game.CurrentManager] =
        [
            new ManagerSeasonHistory(1993, game.ActiveManager.ClubIndex, League.Oberliga, 12, LeagueOutcome.Rank,
                CupOutcome.Eliminated, 0, CupOutcome.NotParticipating, false),
        ];
        d.OpenTile("Früher");
        d.Frame();
        Assert.IsType<HistoryScreen>(d.Host.Current);
        Assert.Equal("12. Platz der Oberliga", HistoryScreen.Lines(map[game.CurrentManager][0])[0].Text); // `file14_5`

        byte At(int x, int y) => d.Surface.Pixels[y * Surface.Width + x];
        // Liga: Balken x 117–118, y 171–172 (Index 29), Achse y 173
        foreach (var x in new[] { 117, 118 })
        {
            Assert.Equal(29, At(x, 172));
            Assert.Equal(29, At(x, 171));
            Assert.NotEqual(29, At(x, 170));
        }
        Assert.NotEqual(29, At(119, 172));
        // DEB-Pokal: Balken x 77–78, y 73–75 (Index 27)
        foreach (var x in new[] { 77, 78 })
        {
            Assert.Equal(27, At(x, 75));
            Assert.Equal(27, At(x, 73));
            Assert.NotEqual(27, At(x, 72));
        }
        // Marke unter der Achse: 2 / 4 / 6 Pixel, Stiel 2 × 3 (Liga y 174–179)
        int Width(int y) => Enumerable.Range(100, 30).Count(x => At(x, y) == HistoryScreen.MarkerColor);
        Assert.Equal([2, 4, 6, 2, 2, 2], new[] { 174, 175, 176, 177, 178, 179 }.Select(Width));
        Assert.Equal(0, Width(180));
        Assert.Equal(HistoryScreen.MarkerColor, At(115, 176));
        Assert.Equal(HistoryScreen.MarkerColor, At(120, 176));
        // R14UR-09: Lage jeder Marken-Zeile (Spitze x 117–118 ab y 174, Mitte 116–119, Basis 115–120, Stiel 117–118)
        foreach (var (y, x0, x1) in new[] { (174, 117, 118), (175, 116, 119), (176, 115, 120), (177, 117, 118), (178, 117, 118), (179, 117, 118) })
            for (var x = 112; x <= 123; x++)
                Assert.Equal(x >= x0 && x <= x1, At(x, y) == HistoryScreen.MarkerColor);
        Assert.NotEqual(HistoryScreen.MarkerColor, At(117, 173));
    }
}
