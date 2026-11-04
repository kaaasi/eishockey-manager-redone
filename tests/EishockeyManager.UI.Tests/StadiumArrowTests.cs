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
}
