using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Competitions;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur ui-meldungen, Teil 2: Infofile (M-K5, em.exe 0x432EC) und Tabellenkurve (M-K6, 0x445E0), Regeln in
/// <c>docs/analyse/re-meldungen.md</c> §D und §E. Pixel an <c>folge06_00000</c> (35 Spieltage) und <c>oberflaeche_0842</c>
/// (vor dem ersten Spieltag) geprüft.
/// </summary>
public class InfoFileAndRankCurveTests
{
    private static ScreenDriver MainMenu()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        Assert.IsType<MainMenuScreen>(d.Host.Current);
        return d;
    }

    private static byte Px(Surface s, int x, int y) => s.Pixels[y * Surface.Width + x];

    // ---- Tabellenkurve ---------------------------------------------------------------------------------------------

    /// <summary>[ORIGINAL] Schritt ⌊85 / (n − 1)⌋, 1 wird zu 2. Vorher: Kurve auf die ganze Breite gestreckt.</summary>
    [Theory]
    [InlineData(1, 85)]
    [InlineData(2, 42)]
    [InlineData(17, 5)]
    [InlineData(35, 2)]
    [InlineData(43, 2)]
    [InlineData(60, 2)]
    public void Step_IsFloorOf85DividedByPlayedDays_OneBecomesTwo(int played, int expected) =>
        Assert.Equal(expected, RankCurve.Step(played));

    [Fact]
    public void Points_StartAtX6_AndEndAtPlayedTimesStep()
    {
        var ranks = Enumerable.Repeat(3, 35).ToList();
        var points = RankCurve.Points(ranks);
        Assert.Equal((6, 51), points[0]);            // X + 3 | Y + 49 + 2 · 2
        Assert.Equal((5 + 2 * 1, 51), points[1]);    // Punkt k = 0 bei X + 2 + d
        Assert.Equal((5 + 2 * 35, 51), points[^1]);  // Ende bei 75 (Bild folge06_00000)
    }

    [Fact]
    public void Y_TwoPixelsPerRank_FromRow47()
    {
        Assert.Equal(47, RankCurve.Y(1));
        Assert.Equal(69, RankCurve.Y(12));
    }

    [GameDataFact]
    public void DrawAxes_AtOriginalPosition_AlsoBeforeFirstMatchDay()
    {
        var d = MainMenu();
        d.Frame();
        var s = d.Surface;
        Assert.Equal(RankCurve.VerticalAxisColor, Px(s, 5, 46));
        Assert.Equal(RankCurve.VerticalAxisColor, Px(s, 5, 69));
        Assert.Equal(RankCurve.HorizontalAxisColor, Px(s, 6, 70));
        Assert.Equal(RankCurve.HorizontalAxisColor, Px(s, 101, 70));
        Assert.NotEqual(RankCurve.HorizontalAxisColor, Px(s, 102, 70));
    }

    [GameDataFact]
    public void Curve_InMainMenu_UsesStepAndYellowForOwnClub()
    {
        var d = MainMenu();
        d.PlayMatchDay();
        d.PlayMatchDay();
        d.PlayMatchDay();
        d.Frame();
        var game = d.Host.Game!;
        var own = game.RankHistory(NationalMenu.Team(game));
        var step = RankCurve.Step(own.Count);
        var end = RankCurve.Points(own)[^1];
        Assert.Equal(5 + own.Count * step, end.X);
        Assert.Equal(Colors.Yellow, Px(d.Surface, end.X, end.Y));
    }

    // ---- Infofile --------------------------------------------------------------------------------------------------

    private static IReadOnlyList<InfoFilePage> Layout(string text, int width = 5) =>
        InfoFileScreen.Layout(System.Text.Encoding.Latin1.GetBytes(text), _ => width);

    [Fact]
    public void Layout_27LinesPerPage_FirstBaseline19_LastBaseline227()
    {
        var pages = Layout(string.Concat(Enumerable.Range(0, 34).Select(_ => "A\r\n")));
        Assert.Equal(2, pages.Count);
        Assert.Equal(19, pages[0].Glyphs.First().Baseline);
        Assert.Equal(227, pages[0].Glyphs.Where(g => g.Code == 'A').Max(g => g.Baseline));
        Assert.Equal(19, pages[1].Glyphs.First().Baseline);
    }

    [Fact]
    public void Layout_Tab_JumpsToNextMultipleOf20_ButNotBeyond285()
    {
        var p = Layout("A\tB").Single().Glyphs;
        Assert.Equal(20, p[1].X);
        var wide = Layout(new string('A', 57) + "\tB", 5).Single().Glyphs; // x = 285: kein Sprung, der Tabulator wird als Zeichen gesetzt
        Assert.Equal(290, wide.Last(g => g.Code == 'B').X);
    }

    [Fact]
    public void Layout_HashEndsPage_AndLineWrapsAfter310()
    {
        Assert.Equal(2, Layout("A#B").Count);
        var wrapped = Layout(new string('A', 64), 5).Single().Glyphs; // 62 · 5 = 310 → noch nicht, nach dem 63. Zeichen (x = 315) neue Zeile
        Assert.Equal(19, wrapped[0].Baseline);
        Assert.Equal(27, wrapped[^1].Baseline);
        Assert.Equal(0, wrapped[^1].X);
    }

    [Fact]
    public void Translate_Latin1Umlauts_MapToGameCharacters_OemUmlautsStay()
    {
        Assert.Equal((byte)'{', InfoFileScreen.Translate(0xE4));
        Assert.Equal((byte)'\\', InfoFileScreen.Translate(0xF6));
        Assert.Equal((byte)'\\', InfoFileScreen.Translate(0xD6));
        Assert.Equal((byte)'~', InfoFileScreen.Translate(0xDF));
        Assert.Equal(0x81, InfoFileScreen.Translate(0x81));
    }

    [Fact]
    public void Trigger_OnlyLeftClickOnPixel319_0()
    {
        Assert.True(InfoFileScreen.IsTrigger(new InputState { MouseX = 319, MouseY = 0, LeftClick = true }));
        Assert.False(InfoFileScreen.IsTrigger(new InputState { MouseX = 318, MouseY = 0, LeftClick = true }));
        Assert.False(InfoFileScreen.IsTrigger(new InputState { MouseX = 319, MouseY = 1, LeftClick = true }));
        Assert.False(InfoFileScreen.IsTrigger(new InputState { MouseX = 319, MouseY = 0, RightClick = true }));
    }

    /// <summary>Regressionstest M-K5: Vorher öffnete ein Klick auf (319 | 0) die Credits (Titelleiste y &lt; 21), kein Infofile.</summary>
    [GameDataFact]
    public void ClickOnHiddenField_OpensInfoFile_PagesThenBack_WithoutChangingGame()
    {
        var d = MainMenu();
        var day = d.Host.Game!.DateText;
        d.Click(319, 0);
        var screen = Assert.IsType<InfoFileScreen>(d.Host.Current);
        Assert.Equal(2, screen.PageCount); // Original-infofile: 1153 Byte, 34 Zeilen
        d.Click(100, 100);
        Assert.IsType<InfoFileScreen>(d.Host.Current);
        d.Click(100, 100);
        Assert.IsType<MainMenuScreen>(d.Host.Current);
        Assert.Equal(day, d.Host.Game!.DateText);
    }

    [GameDataFact]
    public void InfoFilePage_Draws_HeaderLine_AndPrompt()
    {
        var d = MainMenu();
        d.Click(319, 0);
        d.Frame();
        var s = d.Surface;
        Assert.All(Enumerable.Range(0, 320), x => Assert.Equal(0, Px(s, x, 9)));
        var promptPixels = Enumerable.Range(275, 40).Any(x => Enumerable.Range(233, 8).Any(y => Px(s, x, y) == Colors.White));
        Assert.True(promptPixels);
    }
}
