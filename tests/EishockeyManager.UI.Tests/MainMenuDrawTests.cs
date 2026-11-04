using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// ui-rest Teil 4 (R12U-03, #539): Gegenprobe zu „Zeichnen ändert nichts“ im Hauptmenü. Der Test in
/// <c>MainMenuEventUiTests</c> nimmt den Abdruck erst, nachdem Kennzeichen und Aufstellung schon bereinigt sind; ein Ereignis im
/// Zeichnen bliebe dort unsichtbar. Hier wird <b>nach</b> dem Aufbau ein Kennzeichen gesetzt und die Aufstellung lückig gemacht:
/// Zeichnen darf beides nicht anfassen (<c>OnMainMenu</c> in <c>Draw</c> löschte das Kennzeichen und füllte die Lücke).
/// </summary>
public class MainMenuDrawTests
{
    [GameDataFact]
    public void Draw_DoesNotConsumeNewMessageOrRepairLineup()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var menu = Assert.IsType<MainMenuScreen>(d.Host.Current);
        var game = d.Host.Game!;
        var club = game.Clubs[game.ActiveManager.ClubIndex];
        Assert.True(club.AutoLineupEnabled);
        d.Frame();

        game.Notify(game.ActiveManager, "Der Stadion-Ausbau (Test)", "ist abgeschlossen.");
        club.Lineup.Remove(club.Lineup.Players.First());
        var count = club.Lineup.Count;
        Assert.True(game.ActiveManager.HasNewMessage);
        var before = StateFingerprint.Of(game);

        for (var i = 0; i < 5; i++)
            d.Frame(); // nur Zeichnen und Mausbewegung

        Assert.Same(menu, d.Host.Current);
        Assert.True(game.ActiveManager.HasNewMessage);
        Assert.Equal(count, club.Lineup.Count);
        var after = StateFingerprint.Of(game);
        Assert.True(before == after, StateFingerprint.FirstDifference(before, after));
    }
}
