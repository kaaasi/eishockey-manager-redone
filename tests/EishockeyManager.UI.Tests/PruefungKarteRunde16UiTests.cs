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
        // Saat 1994 (bis Spur start-lader 1993): Mit dem Lader beim Spielstart (Datei 0x439BB, anderer Zufallsverlauf) bekommt der
        // Manager bei Saat 1993 keine Prämie. Die Probe hängt an der Saat.
        // Runde 21, Spur zufallsstrom Teil 1 (R21-D2 = A): Saat 1995 statt 1994 – Pool (Datei 0x29896), Starttendenz (Datei 0x42E6B) und der Tageslauf 0x07169 (Datei 0x07402–0x07AEF) ziehen aus dem gemeinsamen Strom, anderer Zufallsverlauf;
        // bei Saat 1994 bekommt der Manager keine Prämie, 1993, 1995, 1996 erfüllen die Vorbedingung.
        // Runde 21, Spur zufallsstrom Teil 4 (R21-D2 = A, R15-E2 = C): Saat 1994 statt 1995 – Werbung, Börse, Zuschauer, Talentsuche, Jugend und Baufirmen ziehen aus dem gemeinsamen Strom statt aus FinanceRandom (Werbeangebote des Spielstarts Datei 0x2A03B–0x2A061, Börse Datei 0x43AD5), anderer Zufallsverlauf ab dem Spielstart; bei Saat 1995 bekommt der Manager keine Prämie, unter 1993–1998 erfüllen 1994 und 1998 die Vorbedingung.
        // Runde 22, Spur kaderplatz-werte Teil 2 (A093; R21-D2 = A, R15-E2 = C): Saat 1995 statt 1994 – die Spielabrechnung 0x544C0 würfelt vor jedem Spiel eines Managervereins je Kaderplatz random(0, 4) (Datei 0x548A8–0x548E7) aus dem gemeinsamen Strom, anderer Zufallsverlauf ab dem ersten Managerspiel; bei Saat 1994 bekommt der Manager keine Prämie, unter 1990–2005 erfüllen alle außer 1992–1994,
        // 2003 und 2004 die Vorbedingung. Rückschaltprobe: src/ von faa64d2 mit Saat 1995 rot (keine Prämie) – die Probe
        // hängt an der Saat.
        var d = FlowTests.CreateDriver(1995);
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
