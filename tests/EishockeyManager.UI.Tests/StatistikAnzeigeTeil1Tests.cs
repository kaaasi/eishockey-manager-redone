using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Statistics;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur statistik-anzeige, Teil 1 (Runde 25): B25 Meisterfoto aus den Einzelteilen von kb\16 (A343) und B28 Balken der Historie
/// (A356, A805, A806) nach <c>docs/analyse/re-abweichungen.md</c> B28.
/// </summary>
public class StatistikAnzeigeTeil1Tests
{
    private static ManagerSeasonHistory Season(League league, int rank, LeagueOutcome outcome = LeagueOutcome.Rank) =>
        new(1993, 0, league, rank, outcome, CupOutcome.Eliminated, 0, CupOutcome.NotParticipating, false);

    /// <summary>
    /// [ORIGINAL] Liga: drei gestapelte Blöcke in 29/22/23 (Nachbildung b28_historie_balken.py, Achse 173). Vorher ein Balken in
    /// Farbe 29, Meister in Farbe 7.
    /// </summary>
    [Fact]
    public void LeagueBars_StackedBlocks_LikeOriginal()
    {
        // Byte 6 (6. Platz der Bundesliga) → 25/25/14 Pixel in 29/22/23
        Assert.Equal([(148, 172, (byte)29), (124, 148, (byte)22), (111, 124, (byte)23)],
            HistoryScreen.Bars(2, Season(League.Bundesliga, 6), 173));
        // Byte 13 (1. Platz der Zweiten Liga) → 25/24 Pixel
        Assert.Equal([(148, 172, (byte)29), (125, 148, (byte)22)], HistoryScreen.Bars(2, Season(League.ZweiteLiga, 1), 173));
        // Byte 25 → 24 Pixel, Byte 36 → 2 Pixel
        Assert.Equal([(149, 172, (byte)29)], HistoryScreen.Bars(2, Season(League.Oberliga, 1), 173));
        Assert.Equal([(171, 172, (byte)29)], HistoryScreen.Bars(2, Season(League.Oberliga, 12), 173));
        // Play-off: 0x33 wie Byte 6, 0x34 wie 4, 0x35 wie 2
        Assert.Equal(111, HistoryScreen.Bars(2, Season(League.Bundesliga, 7, LeagueOutcome.PlayoffQuarterFinal), 173)[2].Y1);
        Assert.Equal(107, HistoryScreen.Bars(2, Season(League.Bundesliga, 3, LeagueOutcome.PlayoffSemiFinal), 173)[2].Y1);
        Assert.Equal(103, HistoryScreen.Bars(2, Season(League.Bundesliga, 2, LeagueOutcome.PlayoffFinal), 173)[2].Y1);
        // Meister (Bit 7) → 25/25/24 Pixel in Farbe 30
        Assert.Equal([(148, 172, (byte)30), (124, 148, (byte)30), (101, 124, (byte)30)],
            HistoryScreen.Bars(2, Season(League.Bundesliga, 1, LeagueOutcome.GermanChampion), 173));
    }

    /// <summary>[ORIGINAL] DEB-Pokal 3 · Runde Pixel Farbe 27, Sieger 18 Pixel Farbe 30; Europacup 3 · Stufe, Sieger 18 Pixel Farbe 30.</summary>
    [Fact]
    public void CupBars_LikeOriginal()
    {
        var h = Season(League.Bundesliga, 9);
        Assert.Equal([(70, 75, (byte)27)], HistoryScreen.Bars(1, h with { DebRound = 1 }, 76));
        Assert.Equal([(58, 75, (byte)30)], HistoryScreen.Bars(1, h with { Deb = CupOutcome.Winner, DebRound = 5 }, 76));
        Assert.Empty(HistoryScreen.Bars(1, h with { Deb = CupOutcome.NotParticipating }, 76));
        Assert.Empty(HistoryScreen.Bars(0, h, 30));
        Assert.Equal([(27, 29, (byte)27)], HistoryScreen.Bars(0, h with { Europe = CupOutcome.Eliminated }, 30));
        Assert.Equal([(24, 29, (byte)27)], HistoryScreen.Bars(0, h with { Europe = CupOutcome.Eliminated, EuropeFinalRound = true }, 30));
        Assert.Equal([(12, 29, (byte)30)], HistoryScreen.Bars(0, h with { Europe = CupOutcome.Winner }, 30));
    }

    /// <summary>Bildschirm „Früher“: Meisterbalken bis y 101 in Farbe 30 (vorher Farbe 7 und 72 Pixel hoch).</summary>
    [GameDataFact]
    public void HistoryScreen_DrawsChampionBarInColor30()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        var field = typeof(GameState).GetField("_managerHistory",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        var map = (Dictionary<int, List<ManagerSeasonHistory>>)field.GetValue(game)!;
        map[game.CurrentManager] = [Season(League.Bundesliga, 1, LeagueOutcome.GermanChampion) with { Club = game.ActiveManager.ClubIndex }];
        d.OpenTile("Früher");
        d.Frame();
        Assert.IsType<HistoryScreen>(d.Host.Current);
        byte At(int x, int y) => d.Surface.Pixels[y * Surface.Width + x];
        foreach (var y in new[] { 172, 148, 124, 101 })
            Assert.Equal(30, At(117, y));
        Assert.NotEqual(30, At(117, 100));
    }

    /// <summary>[ORIGINAL] Teil je Titel (DGROUP 0x4E2C = 3, 1, 0, 2) und Lage des Titels (Datei 0x351BF–0x351D3).</summary>
    [Fact]
    public void TrophyParts_PerTitle()
    {
        Assert.Equal(new SeasonEndTrophyScreen.TrophyPart(new Rect(247, 1, 46, 60), 108, 2), SeasonEndTrophyScreen.Part(Title.GermanChampion));
        Assert.Equal(new SeasonEndTrophyScreen.TrophyPart(new Rect(201, 0, 47, 26), 108, 36), SeasonEndTrophyScreen.Part(Title.CupWinner));
        Assert.Equal(new SeasonEndTrophyScreen.TrophyPart(new Rect(201, 26, 46, 35), 108, 27), SeasonEndTrophyScreen.Part(Title.WorldChampion));
        Assert.Null(SeasonEndTrophyScreen.Part(Title.EuropeanCupWinner));
        Assert.Equal(149, SeasonEndTrophyScreen.TitleX(Title.GermanChampion));
        Assert.Equal(181, SeasonEndTrophyScreen.TitleX(Title.WorldChampion));
    }

    /// <summary>
    /// Meisterfoto: das Teil liegt über dem Pokal des Fotos (Maske 0xC0), der Bereich der Teile ist schwarz (0x0B620), der Titel steht
    /// ab x 149 in Farbe 0x1F. Vorher: Pokal des Fotos unverändert, Titel ab x 147 in Farbe 136.
    /// </summary>
    [GameDataTheory]
    [InlineData(Title.GermanChampion)]
    [InlineData(Title.CupWinner)]
    [InlineData(Title.WorldChampion)]
    public void TrophyScreen_ComposesPartOverPhoto(Title title)
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var back = d.Host.Current!;
        d.Host.Show(new SeasonEndTrophyScreen(d.Host, new TrophyStep(0, title, d.Host.Game!.ActiveManager.ClubIndex), back));
        d.Frame();
        var photo = d.Host.Assets.Image(SeasonEndTrophyScreen.Photo);
        var part = SeasonEndTrophyScreen.Part(title)!.Value;
        byte At(int x, int y) => d.Surface.Pixels[y * Surface.Width + x];
        var copied = 0;
        for (var y = 0; y < part.Source.H; y++)
            for (var x = 0; x < part.Source.W; x++)
            {
                var src = photo.Pixels[(part.Source.Y + y) * photo.Width + part.Source.X + x];
                var dx = part.DestX + x;
                var dy = part.DestY + y;
                if (src != SeasonEndTrophyScreen.PartMask && !InTitle(dx, dy))
                {
                    Assert.Equal(src, At(dx, dy));
                    if (src != photo.Pixels[dy * photo.Width + dx])
                        copied++;
                }
            }
        Assert.True(copied > 100, $"nur {copied} Pixel des Teils weichen vom Foto ab");
        for (var y = 0; y <= 60; y++)
            for (var x = 201; x <= 292; x++)
                if (!InTitle(x, y))
                    Assert.Equal(0, At(x, y));
        // Titel ab x = TitleX in Farbe 0x1F (dort, wo das Bild ohne Text eine andere Farbe hat); links davon kein Titelpixel
        var plain = (byte[])photo.Pixels.Clone();
        for (var y = 0; y < part.Source.H; y++)
            for (var x = 0; x < part.Source.W; x++)
                if (photo.Pixels[(part.Source.Y + y) * photo.Width + part.Source.X + x] is var c && c != SeasonEndTrophyScreen.PartMask)
                    plain[(part.DestY + y) * photo.Width + part.DestX + x] = c;
        int TitlePixels(int x0, int x1) => Enumerable.Range(5, 30).Sum(y => Enumerable.Range(x0, x1 - x0 + 1)
            .Count(x => At(x, y) == SeasonEndTrophyScreen.TitleColor && plain[y * photo.Width + x] != SeasonEndTrophyScreen.TitleColor));
        var tx = SeasonEndTrophyScreen.TitleX(title);
        Assert.True(TitlePixels(tx, tx + 7) > 0);
        Assert.Equal(0, TitlePixels(tx - 8, tx - 1));
    }

    /// <summary>Titelzeile (Schrift 1 ab x 149 bzw. 181, Grundlinie 26, mit Schatten und Unterlängen) – dort überdeckt der Text Teil und Füllung.</summary>
    private static bool InTitle(int x, int y) => y is >= 5 and <= 34 && x >= 145;
}
