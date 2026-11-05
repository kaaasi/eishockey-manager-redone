using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens.ManagerArea;

namespace EishockeyManager.UI.Tests;

/// <summary>ui-rest Teil 1 (#535): Pfeile im Stadion über <see cref="ArrowPair"/> (em.exe Dateien 0x3DD89, 0x3E0BC).</summary>
public class StadiumArrowTests
{
    [GameDataFact]
    public void TicketArrows_UseOriginalAreas_AndStepOnlyOnPress()
    {
        Assert.Equal(new Rect(192, 189, 8, 9), StadiumScreen.TicketUp);
        Assert.Equal(new Rect(202, 189, 8, 9), StadiumScreen.TicketDown);
        Assert.Equal(new Rect(192, 210, 8, 9), StadiumScreen.LevelUp);
        Assert.Equal(new Rect(202, 210, 8, 9), StadiumScreen.LevelDown);

        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        d.OpenTile("Stadion");
        var stadium = d.Host.Game!.Finance(d.Host.Game.ActiveManager).Stadium;
        var price = stadium.TicketPrice;

        d.Click(200, 195); // x1 + 8 liegt außerhalb
        Assert.Equal(price, stadium.TicketPrice);
        d.Click(196, 195); // linker Pfeil liefert −1: Eintritt − (−1) (0x3DDB3)
        Assert.Equal(price + 1, stadium.TicketPrice);
        d.Click(206, 195);
        Assert.Equal(price, stadium.TicketPrice);

        // [ORIGINAL] 0x3DD93: nur das Ereignis „Taste gedrückt“ zählt, gehaltene Taste wiederholt nichts.
        d.Frame(i => { i.MouseX = 196; i.MouseY = 195; i.LeftClick = true; i.LeftDown = true; });
        for (var f = 0; f < 60; f++)
            d.Frame(i => { i.MouseX = 196; i.MouseY = 195; i.LeftDown = true; });
        Assert.Equal(price + 1, stadium.TicketPrice);
    }

    /// <summary>
    /// #597 (R13IU-01): [ORIGINAL] Datei 0x3DAE0–0x3DB29 (Eintritt, y 189) und 0x3DFAB–0x3DFF4 (Stufe, y 210): je Pfeil j = 0, 1 zuerst die
    /// Grundgrafik kb\4 (20 | 9 · j) 10 × 8 nach (192 + 10 · j | y) (Maske 0xC0), erst dann der 8 × 7 große Pfeil. Jeder Pfeil ist 10 Pixel breit.
    /// </summary>
    [GameDataFact]
    public void TicketArrows_AreTenPixelsWide_FromKb4BaseGraphic()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        d.OpenTile("Stadion");
        d.Frame();
        var kb4 = d.Host.Assets.Image(4);
        for (var j = 0; j < 2; j++)
            for (var y = 0; y < 8; y++)
                for (var x = 0; x < 10; x++)
                {
                    var screenX = 192 + 10 * j + x;
                    var screenY = 189 + y;
                    var baseValue = kb4.Pixels[(9 * j + y) * kb4.Width + 20 + x];
                    // Der Pfeil (8 × 7 bei x + 2 | y + 1) liegt darüber; nur Rand- und Schattenspalten gehören allein der Grundgrafik.
                    var insideArrow = x >= 2 && y >= 1 && y < 8;
                    if (!insideArrow)
                        Assert.True(baseValue == d.Surface.Pixels[screenY * 320 + screenX], $"Pfeil {j}: Pixel ({x}|{y}) ist nicht die Grundgrafik");
                }
    }
}

/// <summary>ui-rest Teil 3 (G7): Tabelle „Bau“ im Stadion (em.exe Datei 0x3D45F, Modus 1; Umschalten Datei 0x3DB5B).</summary>
public class StadiumBuildTableTests
{
    [GameDataFact]
    public void RightClickBelowY167_TogglesBuildTable_OtherwiseLeaves()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        d.OpenTile("Stadion");
        var screen = Assert.IsType<StadiumScreen>(d.Host.Current);
        Assert.False(screen.ShowsBuildTable);
        d.RightClick(100, 167);                    // y > 167 verlangt: 167 selbst nicht
        Assert.IsNotType<StadiumScreen>(d.Host.Current);

        d.OpenTile("Stadion");
        screen = Assert.IsType<StadiumScreen>(d.Host.Current);
        d.RightClick(100, 168);
        Assert.True(screen.ShowsBuildTable);
        Assert.Same(screen, d.Host.Current);
        d.RightClick(250, 230);
        Assert.False(screen.ShowsBuildTable);
    }

    [GameDataFact]
    public void BuildTexts_FollowTheOriginalStrings()
    {
        var stadium = new Stadium();
        Assert.Equal("(keine Bautätigkeiten)", StadiumScreen.FeatureValue(stadium, StadiumFeature.Kino, buildMode: true));
        Assert.Equal(stadium.LevelText(StadiumFeature.Kino), StadiumScreen.FeatureValue(stadium, StadiumFeature.Kino, buildMode: false));
        Assert.Equal("(kein Bau...)", StadiumScreen.PlacesValue(stadium, StadiumProject.Sitzplaetze, buildMode: true));

        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        var me = game.ActiveManager;
        me.Money = 50_000_000;
        var built = game.Finance(me).Stadium;
        void Build(StadiumProject project, StadiumFeature feature, int target) =>
            game.AnswerConstruction(me, game.QuoteConstruction(me, project, feature, target)!, accept: true);

        Build(StadiumProject.Feature, StadiumFeature.Kino, 1);
        var weeks = built.WeeksLeft(StadiumProject.Feature, StadiumFeature.Kino);
        Assert.Equal(Stadium.Features[(int)StadiumFeature.Kino].Levels[1] + " (Noch " + weeks + (weeks == 1 ? " Woche)" : " Wochen)"),
            StadiumScreen.FeatureValue(built, StadiumFeature.Kino, buildMode: true));
        Build(StadiumProject.Sitzplaetze, StadiumFeature.Ueberdachung, 1);
        Assert.Equal(StadiumScreen.PlacesValue(built, StadiumProject.Sitzplaetze, buildMode: true),
            $"{built.SeatsTarget:#,0}".Replace(",", ".") + " (" + built.WeeksLeft(StadiumProject.Sitzplaetze, StadiumFeature.Ueberdachung) + ")");
    }
}
