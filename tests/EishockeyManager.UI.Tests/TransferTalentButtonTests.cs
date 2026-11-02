using EishockeyManager.Tests.Support;
using EishockeyManager.UI;
using EishockeyManager.UI.Testing;
using EishockeyManager.UI.Screens.Team;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Integration Runde 11 (Anschluss aus geld Teil 6, #448): Der Knopf „Talent“ im Transferbildschirm wird nur gezeichnet
/// und ausgewertet, solange keine Talentsuche läuft (em.exe Datei 0x46704 Zeichnen, 0x468C1 Auswertung).
/// </summary>
public class TransferTalentButtonTests
{
    [GameDataFact]
    public void TalentButton_HiddenAndInactiveWhileSearchRuns()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        d.OpenTile("Transfer");
        var screen = Assert.IsType<TransferScreen>(d.Host.Current);
        var game = d.Host.Game!;
        var manager = game.ActiveManager;

        d.Frame();
        var idle = ButtonPixels(d);

        manager.Money = Math.Max(manager.Money, 5_000_000);
        Assert.Equal(Core.TalentSearchStart.Started, game.StartTalentSearch(manager));
        d.Frame();
        var running = ButtonPixels(d);
        Assert.NotEqual(idle, running); // Knopf nicht gezeichnet

        // Ohne Knopf fällt der Klick durch wie jeder Klick außerhalb der Fenster: zurück ins Hauptmenü, kein Dialog.
        d.Click(TransferScreen.TalentButton.X + 5, TransferScreen.TalentButton.Y + 5);
        Assert.Null(screen.Dialog);
        Assert.IsNotType<TransferScreen>(d.Host.Current);

        game.Finance(manager).TalentSearch = null;
        d.OpenTile("Transfer");
        screen = Assert.IsType<TransferScreen>(d.Host.Current);
        d.Frame();
        Assert.NotEqual(running, ButtonPixels(d)); // Knopf wieder gezeichnet (Kontostand geändert, daher kein Vergleich mit idle)
        d.Click(TransferScreen.TalentButton.X + 5, TransferScreen.TalentButton.Y + 5);
        Assert.Equal(TransferDialog.DialogKind.Talent, screen.Dialog!.Kind);
    }

    private static byte[] ButtonPixels(ScreenDriver d)
    {
        var r = TransferScreen.TalentButton;
        var pixels = new byte[r.W * r.H];
        for (var y = 0; y < r.H; y++)
            Array.Copy(d.Surface.Pixels, (r.Y + y) * Surface.Width + r.X, pixels, y * r.W, r.W);
        return pixels;
    }
}
