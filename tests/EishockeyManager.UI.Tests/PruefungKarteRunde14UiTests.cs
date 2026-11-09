using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens.Team;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur <c>pruefung-karte</c> Runde 14, Teil 2: Transferliste nach DOSBox-Sitzung 2 (R14UR-10 Länge der Trennlinien, R14KS9-03
/// Beschriftung der Kontoleiste).
/// </summary>
public class PruefungKarteRunde14UiTests
{
    private static byte At(Surface s, int x, int y) => s.Pixels[y * Surface.Width + x];

    /// <summary>
    /// R14UR-10: Beim Aufbau reichen die Trennlinien von x 181 bis 313 (Datei 0x46386–0x463AB, `file40_04`); Markierung und ihr
    /// Zurücksetzen ziehen x 180 bis 314 (Datei 0x46AFE–0x46BAF), die Linien eines einmal markierten Eintrags bleiben danach
    /// zwei Pixel länger (`file40_05`–`07`). Vorher rot: Das Remake zog jede Linie von 180 bis 314.
    /// </summary>
    [GameDataFact]
    public void TransferList_Separators_181To313_LongerAfterHover()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        d.OpenTile("Transfer");
        Assert.IsType<TransferScreen>(d.Host.Current);
        Assert.True(d.Host.Game!.TransferMarket.Listings.Count >= 4);
        d.Move(100, 100); // Maus im Kader, kein Eintrag markiert
        var s = d.Surface;
        const int entry = 2;
        var top = TransferScreen.FirstEntryY + TransferScreen.EntryPitch * entry - 4;
        var bottom = top + TransferScreen.EntryPitch;
        foreach (var y in new[] { top, bottom })
        {
            Assert.Equal(31, At(s, 181, y));
            Assert.Equal(31, At(s, 313, y));
            Assert.NotEqual(31, At(s, 180, y));
            Assert.NotEqual(31, At(s, 314, y));
        }

        d.Move(250, top + 6); // Eintrag markieren …
        Assert.Equal(16, At(s, 180, top));
        Assert.Equal(16, At(s, 314, bottom));
        d.Move(100, 100); // … und verlassen
        foreach (var y in new[] { top, bottom })
        {
            Assert.Equal(31, At(s, 180, y));
            Assert.Equal(31, At(s, 314, y));
        }
        var other = bottom + TransferScreen.EntryPitch; // nie markiert
        Assert.NotEqual(31, At(s, 180, other));
        Assert.NotEqual(31, At(s, 314, other));
    }

    /// <summary>
    /// R14KS9-03: Beschriftung der Kontoleiste auf Grundlinie 0xE5 = 229 (oberste Zeile y 225) bei x = 3 + 65 · k (Datei
    /// 0x46533–0x4657A; `file40_03`/`file40_04`: „Anlagen“ ab x 133). Vorher rot: oberste Zeile y 226, „Anlagen“ ab x 132.
    /// </summary>
    [GameDataFact]
    public void TransferScreen_AccountLabels_TopRow225_AnlagenAt133()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        d.OpenTile("Transfer");
        d.Move(100, 100);
        var s = d.Surface;
        foreach (var (x0, x1) in new[] { (2, 66), (67, 131), (132, 196), (197, 260) })
        {
            var ink = Enumerable.Range(x0, x1 - x0).SelectMany(x => Enumerable.Range(224, 6).Select(y => (x, y)))
                .Where(p => At(s, p.x, p.y) != TeamUi.DialogFill).ToList();
            Assert.Equal(225, ink.Min(p => p.y));
            Assert.Equal(x0 + 1, ink.Min(p => p.x));
        }
    }
}
