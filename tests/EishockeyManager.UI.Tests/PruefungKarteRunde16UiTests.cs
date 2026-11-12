using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI;
using EishockeyManager.UI.Screens;

namespace EishockeyManager.UI.Tests;

/// <summary>Spur pruefung-karte Teil 2 (Runde 16): gesammelte P3 mit Oberfläche.</summary>
public class PruefungKarteRunde16UiTests
{
    /// <summary>
    /// #768: Die Infoleiste des Fensters „Saisonende – Ereignisse“ zeigt das Konto vor der Prämie. [ORIGINAL] 0x26F20 ruft je
    /// Manager zuerst das Fenster 0x26D9A (Datei 0x2700A) und bucht danach die Prämien (Datei 0x270DB–0x27258); Beleg
    /// <c>file46_2</c> (17.523.876 DM = FILE44 − April-Abschluss, „Erhaltene Prämien: 100.000 DM“). Vergleich: Kontofeld der
    /// Infoleiste (Zeilen 62–70) gleich dem Hauptmenü mit Konto − Prämie, verschieden vom Hauptmenü mit dem gebuchten Konto.
    /// Vorher rot: Das Fenster zeichnete das Konto nach der Prämie.
    /// </summary>
    [GameDataFact]
    public void SeasonEndEvents_InfoBar_ShowsMoneyBeforePremium()
    {
        // Runde 17, Spur wettbewerbe-rest Teil 3 (A113/A142, R15-E2 = C): Saat 1994 statt 1993 – mit dem Spielplan über die Satzfolge (Datei 0x4E7C7) bekommt der Manager bei
        // 1993 keine Prämie (Vorbedingung); 1994–1998, 2000 je 100.000 DM. Rückschaltprobe: alter Code mit Saat 1994 rot (keine
        // Prämie) – die Probe hängt an der Saat.
        var d = FlowTests.CreateDriver(1994);
        d.StartNewGame();
        var game = d.Host.Game!;
        for (var guard = 0; d.Host.Current is not SeasonEndSummaryScreen && guard++ < 20000;)
            d.Advance();
        Assert.IsType<SeasonEndSummaryScreen>(d.Host.Current);
        var result = game.History[^1].Managers[0];
        Assert.Equal(result.Manager, game.CurrentManager);
        Assert.True(result.Premium > 0, "Probe ohne Prämie prüft nichts");
        var booked = game.ActiveManager.Money;

        d.Frame();
        var window = MoneyRow(d.Surface);

        var menu = new Surface();
        new MainMenuScreen(d.Host) { SelectedBigTile = -1 }.Draw(menu);
        Assert.NotEqual(MoneyRow(menu), window); // gebuchtes Konto

        game.ActiveManager.Money = booked - result.Premium; // nur der Vergleich, kein Spielablauf danach
        new MainMenuScreen(d.Host) { SelectedBigTile = -1 }.Draw(menu);
        Assert.Equal(MoneyRow(menu), window);
        game.ActiveManager.Money = booked;
    }

    /// <summary>Kontofeld der Infoleiste (Text bei x = 184, y = 64, Mini-Schrift).</summary>
    private static byte[] MoneyRow(Surface s)
    {
        var bytes = new List<byte>();
        for (var y = 62; y <= 70; y++)
            for (var x = 130; x < 240; x++)
                bytes.Add(s.Pixels[y * Surface.Width + x]);
        return [.. bytes];
    }
}
