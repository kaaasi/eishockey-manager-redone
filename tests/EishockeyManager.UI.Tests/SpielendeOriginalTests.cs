using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Statistics;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// L8: Spielende-Bildschirm em.exe Datei 0x51F3D und Reihenfolge 0x5642E/0x56438 (Bestenliste, dann „Das wars...“).
/// </summary>
public class SpielendeOriginalTests
{
    private static ScreenDriver StartedDriver()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        return d;
    }

    private static (int MinX, int MaxX, int MinY, int MaxY, int Count) Box(Surface s, byte color, int y0, int y1)
    {
        int minX = int.MaxValue, maxX = -1, minY = int.MaxValue, maxY = -1, count = 0;
        for (var y = y0; y <= y1; y++)
            for (var x = 0; x < Surface.Width; x++)
                if (s.Pixels[y * Surface.Width + x] == color)
                {
                    minX = Math.Min(minX, x);
                    maxX = Math.Max(maxX, x);
                    minY = Math.Min(minY, y);
                    maxY = Math.Max(maxY, y);
                    count++;
                }
        return (minX, maxX, minY, maxY, count);
    }

    [GameDataFact]
    public void GameEnd_DrawsBlackScreenWithTwoLines()
    {
        var d = StartedDriver();
        d.Host.Game!.Setup.Managers[0].Level = 1; // ohne Bestenliste (Schlüssel null): der Bildschirm bleibt stehen
        Assert.Null(HighscoreRules.Key(d.Host.Game.Setup));
        d.Host.Show(new SeasonEndGameOverScreen(d.Host, null, null));
        d.Frame();
        var s = d.Surface;

        // [ORIGINAL] 0x51F3D: nur Farbe 0, 7 und 30
        Assert.All(s.Pixels, p => Assert.True(p is 0 or (byte)7 or (byte)30, $"Farbe {p}"));

        var bold = d.Host.Assets.Bold;
        var line1 = Box(s, (byte)7, 0, Surface.Height - 1);
        var line2 = Box(s, (byte)30, 0, Surface.Height - 1);
        Assert.True(line1.Count > 0 && line2.Count > 0);
        // Grundlinien 100 und 140: „Das wars...“ ohne Unterlänge endet auf der Grundlinie; die zweite Zeile liegt 40 tiefer
        // (gleiche Oberkante der Großbuchstaben, „p“ mit Unterlänge darunter)
        Assert.Equal(100, line1.MaxY);
        Assert.Equal(line1.MinY + 40, line2.MinY);
        Assert.True(line2.MaxY > 140);
        // zentriert in x 8 … 319: Start = 8 + (311 − Breite) / 2
        var w1 = bold.Measure("Das wars...");
        var w2 = bold.Measure("Carpe diem !");
        Assert.InRange(line1.MinX, 8 + (311 - w1) / 2, 8 + (311 - w1) / 2 + 2);
        Assert.InRange(line2.MinX, 8 + (311 - w2) / 2, 8 + (311 - w2) / 2 + 2);
    }

    [GameDataFact]
    public void GameEnd_ShowsHighscoreFirstThenEndScreenThenTitle()
    {
        var d = StartedDriver();
        Assert.NotNull(HighscoreRules.Key(d.Host.Game!.Setup));
        d.Host.Show(new SeasonEndGameOverScreen(d.Host, null, null));

        // [ORIGINAL] 0x5642E: zuerst die Bestenliste
        Assert.IsType<HighscoreScreen>(d.Host.Current);
        d.Click(10, 10);
        Assert.IsType<SeasonEndGameOverScreen>(d.Host.Current);
        Assert.NotNull(d.Host.Game);
        d.Frame();
        Assert.IsType<SeasonEndGameOverScreen>(d.Host.Current);
        d.Click(10, 10);
        Assert.IsType<TitleScreen>(d.Host.Current);
        Assert.Null(d.Host.Game);
    }

    [GameDataFact]
    public void Dismissal_LeadsToHighscoreThenEndScreen()
    {
        var d = StartedDriver();
        d.Host.Show(new SeasonEndGameOverScreen(d.Host, 0, null));
        d.Frame();
        Assert.IsType<SeasonEndGameOverScreen>(d.Host.Current);
        d.Click(10, 10);
        Assert.IsType<HighscoreScreen>(d.Host.Current);
        d.Click(10, 10);
        Assert.IsType<SeasonEndGameOverScreen>(d.Host.Current);
    }
}
