using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens.ManagerArea;

namespace EishockeyManager.UI.Tests;

/// <summary>ui-rest Teil 4 (Hinweis Review #546): <c>NewsBar.Shown</c> bei gleicher Anzahl, aber anderen Nachrichten.</summary>
public class NewsBarShownTests
{
    [GameDataFact]
    public void Shown_ResetsToNewest_WhenOneMessageDropsAndAnotherArrives()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        var me = game.ActiveManager;
        game.Notify(me, "Erste", "Nachricht");
        game.Notify(me, "Zweite", "Nachricht");
        game.Notify(me, "Dritte", "Nachricht");
        var inbox = game.Finance(me).Inbox;
        Assert.Equal(inbox.Count - 1, NewsBar.Shown(game));
        var wheel = new InputState { MouseX = NewsBar.Area.X + 5, MouseY = NewsBar.Area.Y + 5, Wheel = 1 };
        Assert.True(NewsBar.Update(d.Host, d.Host.Current!, wheel)); // Rad nach oben: eine Nachricht zurück
        Assert.Equal(inbox.Count - 2, NewsBar.Shown(game));

        // gleiche Anzahl: die älteste fällt weg, eine neue kommt hinzu (Tagesablauf)
        var count = inbox.Count;
        inbox.RemoveAt(0);
        game.Notify(me, "Vierte", "Nachricht");
        Assert.Equal(count, inbox.Count);
        Assert.Equal(inbox.Count - 1, NewsBar.Shown(game));
    }
}
