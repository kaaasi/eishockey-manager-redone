using EishockeyManager.Tests.Support;
using EishockeyManager.Core;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.ManagerArea;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>Bildschirm Stadion mit den Originaldateien (ohne „gamedata“ übersprungen).</summary>
public class StadiumScreenTests
{
    private static void Click(ScreenDriver d, Rect r) => d.Click(r.X + r.W / 2, r.Y + r.H / 2);

    [GameDataFact]
    public void SelectLevelConfirmAndAdjustTicketPrice()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        d.OpenTile("Stadion");
        var screen = Assert.IsType<StadiumScreen>(d.Host.Current);
        var game = d.Host.Game!;
        var me = game.ActiveManager;
        var stadium = game.Finance(me).Stadium;

        // ▲ nimmt eine höhere Stufe nur an, wenn das Konto die Kosten deckt (em.exe Datei 0x3E107–0x3E137, Spur geld Teil 1).
        me.Money = 1_000_000;
        d.Click(50, 65);
        Click(d, StadiumScreen.LevelUp);
        Assert.Equal(1, screen.Target); // 2 · 520.000 DM > Konto
        me.Money = 5_000_000;

        // Kino über die Fläche oben links, dann zwei Stufen höher
        d.Click(50, 65);
        Assert.Equal(new StadiumScreen.Choice(StadiumProject.Feature, StadiumFeature.Kino), screen.Selected);
        Assert.Equal(1, screen.Target);
        Click(d, StadiumScreen.LevelUp);
        Click(d, StadiumScreen.LevelUp);
        Click(d, StadiumScreen.LevelUp);
        Assert.Equal(3, screen.Target);
        Click(d, StadiumScreen.LevelDown);
        Assert.Equal(2, screen.Target);

        // Zeile der Liste wählt ebenfalls
        Click(d, StadiumScreen.FeatureRow((int)StadiumFeature.Kuehlanlage));
        Assert.Equal(StadiumFeature.Kuehlanlage, screen.Selected?.Feature);
        Assert.Equal(640_000, screen.CurrentCost());

        Click(d, StadiumScreen.LevelBox);
        Assert.True(screen.AsksConfirmation);
        var quote = screen.Quote!;
        Assert.Equal(640_000, quote.Cost);
        Click(d, StadiumScreen.YesButton);
        Assert.True(stadium.IsUnderConstruction(StadiumProject.Feature, StadiumFeature.Kuehlanlage));
        // Rabatt 0–10 % (Spur geld, Teil 1, em.exe Datei 0x3E1FE ff.)
        Assert.Equal(5_000_000 - quote.DiscountedCost, me.Money);

        // Projekt im Bau: keine Auswahl; anderes Projekt parallel wählbar
        Click(d, StadiumScreen.FeatureRow((int)StadiumFeature.Kuehlanlage));
        Assert.Null(screen.Selected);
        Click(d, StadiumScreen.FeatureRow((int)StadiumFeature.Bowlingbahn));
        Assert.Equal(StadiumFeature.Bowlingbahn, screen.Selected?.Feature);

        // „Kein Gedanke“: Baufirma beleidigt, Hinweis statt Auswahl
        Click(d, StadiumScreen.LevelBox);
        Assert.True(screen.AsksConfirmation);
        Click(d, StadiumScreen.NoButton);
        Assert.False(screen.AsksConfirmation);
        Assert.False(stadium.IsUnderConstruction(StadiumProject.Feature, StadiumFeature.Bowlingbahn));
        Click(d, StadiumScreen.FeatureRow((int)StadiumFeature.Bowlingbahn));
        Assert.Null(screen.Selected);
        Assert.Equal("Die Baufirma ist noch beleidigt...", screen.Notice);

        var price = stadium.TicketPrice;
        Click(d, StadiumScreen.TicketUp);
        Click(d, StadiumScreen.TicketUp);
        Click(d, StadiumScreen.TicketDown);
        Assert.Equal(price + 1, stadium.TicketPrice);

        d.Press(Key.Escape);
        Assert.IsType<MainMenuScreen>(d.Host.Current);
    }
}
