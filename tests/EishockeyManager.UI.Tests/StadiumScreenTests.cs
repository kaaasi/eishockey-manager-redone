using EishockeyManager.Core;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.ManagerArea;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>Bildschirm Stadion mit den Originaldateien (ohne „gamedata“ übersprungen).</summary>
public class StadiumScreenTests
{
    private static void Click(ScreenDriver d, Rect r) => d.Click(r.X + r.W / 2, r.Y + r.H / 2);

    [Fact]
    public void SelectLevelConfirmAndAdjustTicketPrice()
    {
        var d = FlowTests.CreateDriver();
        if (d is null)
            return;
        d.StartNewGame();
        d.OpenTile("Stadion");
        var screen = Assert.IsType<StadiumScreen>(d.Host.Current);
        var game = d.Host.Game!;
        var me = game.ActiveManager;
        var stadium = game.Finance(me).Stadium;

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
        Assert.Equal(640_000, screen.CurrentOffer()!.Value.Cost);

        Click(d, StadiumScreen.LevelBox);
        Assert.True(screen.AsksConfirmation);
        Click(d, StadiumScreen.YesButton);
        Assert.NotNull(stadium.Construction);
        Assert.Equal(GameState.StartMoney - 640_000, me.Money);

        var price = stadium.TicketPrice;
        Click(d, StadiumScreen.TicketUp);
        Click(d, StadiumScreen.TicketUp);
        Click(d, StadiumScreen.TicketDown);
        Assert.Equal(price + 1, stadium.TicketPrice);

        d.Press(Key.Escape);
        Assert.IsType<MainMenuScreen>(d.Host.Current);
    }
}
