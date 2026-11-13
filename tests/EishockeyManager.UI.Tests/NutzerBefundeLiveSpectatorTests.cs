using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Live;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur nutzer-befunde, #837: Im Live-Heimspiel fehlte die Zuschauerzahl (Nutzerprobe main 01ed8a5), weil das Remake die
/// Zuschauer erst nach dem Spieltag rechnete. [ORIGINAL] Die Spielabrechnung 0x544C0 läuft je Paarung vor dem Spiel
/// (Datei 0x36DBD/0x36F9C), die Zuschauerzeile (Datei 0x53A11–0x53B37) liest ihren Satz 0x5D99.
/// </summary>
public class NutzerBefundeLiveSpectatorTests
{
    /// <summary>
    /// Im Live-Bildschirm eines Managerheimspiels steht die Zahl, die die Abrechnung danach bucht (Eintritt = Zahl ·
    /// Eintrittspreis); ein Auswärtsspiel zeigt „(ausw.)“.
    /// </summary>
    [GameDataFact]
    public void LiveHomeGame_ShowsSpectatorsBookedAfterwards()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        game.Options.MatchSpeed = 0;
        var manager = game.ActiveManager;
        for (var day = 0; day < 40; day++)
        {
            d.NextDay();
            while (d.Host.Current is MatchDayScreen)
                d.Frame();
            if (d.Host.Current is LiveMatchScreen live && live.Session.Shown.Count > 0)
            {
                var shown = live.Session.Shown[0];
                var (text, color) = LiveMatchScreen.SpectatorLine(game, shown.Home, shown.Away, shown.Competition);
                if (shown.Home.Index != manager.ClubIndex)
                {
                    Assert.Equal(("(ausw.)", (byte)0), (text, color));
                }
                else
                {
                    var before = game.Finance(manager).Book.Current(FinanceCategory.Eintritt);
                    Finish(d);
                    var (spectators, income) = game.Finance(manager).LastHomeGame!.Value;
                    Assert.True(spectators > 0);
                    Assert.Equal(EishockeyManager.UI.Screens.Statistics.ClubStatisticsScreen.Number(spectators, 1, true), text);
                    var soldOut = spectators == game.Finance(manager).Stadium.Capacity;
                    Assert.Equal(soldOut ? LiveMatchScreen.SoldOutColor : (byte)0, color);
                    Assert.Equal(income, game.Finance(manager).Book.Current(FinanceCategory.Eintritt) - before);
                    return;
                }
            }
            Finish(d);
        }
        Assert.Fail("Kein Live-Heimspiel des Managers in 40 Tagen.");
    }

    /// <summary>Ein ausverkauftes Heimspiel zeigt die Zahl in Farbe 0x10 (Datei 0x53AF5).</summary>
    [Fact]
    public void SoldOutColor_AsInOriginal() => Assert.Equal(0x10, LiveMatchScreen.SoldOutColor);

    private static void Finish(EishockeyManager.UI.Testing.ScreenDriver d)
    {
        for (var i = 0; d.Host.Current is not MainMenuScreen; i++)
        {
            Assert.True(i < 200_000, "Spieltag endet nicht");
            if (d.Host.Current is LiveMatchScreen { WaitingForClick: true })
                d.Click(160, 120);
            else
                d.Press(Key.Enter);
        }
    }
}
