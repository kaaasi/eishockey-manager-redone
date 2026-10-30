using EishockeyManager.Tests.Support;
using EishockeyManager.Core;
using EishockeyManager.UI;
using EishockeyManager.Formats;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur saisonende-status, Befund #125: Symbol 8 (Weltkugel) im Fenster „Saisonende“.
/// [ORIGINAL] em.exe Datei 0x6CA6 + 0x39: Symbolnummer n → Quelle (n % 6 · 24, n / 6 · 21), 24 × 21, mit Rahmen 30 × 27 dahinter;
/// Bit 6 des Statusbytes zeigt nach Tabelle DGROUP 0x4B2E (Datei 0x2714F) das Symbol 8.
/// </summary>
public class SeasonEndStatusUiTests
{
    /// <summary>Raster mit sechs Zellen je Zeile (Datei 0x6CA6 + 0x39).</summary>
    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(1, 24, 0)]
    [InlineData(2, 48, 0)]
    [InlineData(5, 120, 0)]
    [InlineData(6, 0, 21)]
    [InlineData(8, 48, 21)]
    [InlineData(12, 0, 42)]
    public void Symbol_MapsToSixColumnGrid(int symbol, int x, int y) =>
        Assert.Equal(new Rect(x, y, 24, 21), SeasonEndSummaryScreen.Symbol(symbol));

    /// <summary>Statusbit 6 → Symbol 8; die übrigen Ereignisse behalten ihre Zellen (Tabelle 0x4B2E: 2, 1, …, 4, 6).</summary>
    [Fact]
    public void EventIcon_TopFiveIsSymbolEight_OthersUnchanged()
    {
        Assert.Equal(new Rect(48, 21, 24, 21), SeasonEndSummaryScreen.TopFiveIcon);
        Assert.Equal(new Rect(48, 0, 24, 21), SeasonEndSummaryScreen.EventIcon(SeasonEvent.Promotion));   // Bit 0 → 2
        Assert.Equal(new Rect(24, 0, 24, 21), SeasonEndSummaryScreen.EventIcon(SeasonEvent.Relegation));  // Bit 1 → 1
        Assert.Equal(new Rect(96, 0, 24, 21), SeasonEndSummaryScreen.EventIcon(SeasonEvent.CupWinner));   // Bit 4 → 4
        Assert.Equal(new Rect(0, 21, 24, 21), SeasonEndSummaryScreen.EventIcon(SeasonEvent.EuropeanCupWinner)); // Bit 5 → 6
        Assert.Equal(new Rect(72, 0, 24, 21), SeasonEndSummaryScreen.EventIcon(SeasonEvent.GermanChampion)); // [APPROX]
    }

    /// <summary>
    /// Das Fenster zeichnet Symbol 8 aus kb\13 mit schwarzem Rahmen als letztes Symbol, wenn der Verein des Managers unter den ersten
    /// fünf der 1. Liga steht (Verein auf Platz 1 gesetzt) – und kein Weltkugel-Symbol, wenn er außerhalb steht.
    /// </summary>
    [GameDataFact]
    public void SummaryWindow_DrawsGlobeOnlyForTopFive()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        for (var guard = 0; d.Host.Current is not SeasonEndSummaryScreen && guard++ < 20000;)
            d.Press(Key.Space);
        Assert.IsType<SeasonEndSummaryScreen>(d.Host.Current);

        var record = game.History[^1];
        var result = record.Managers[0];
        var tables = (IDictionary<League, IReadOnlyList<int>>)record.FinalTables;
        var others = tables[League.Bundesliga].Where(c => c != result.Club).ToList();
        var icons = d.Host.Assets.Image(13);

        // Auf Platz 1 der 1. Liga: Symbol 8 steht als letztes Symbol.
        tables[League.Bundesliga] = [result.Club, .. others];
        Assert.True(game.HasTopFiveStatus(record, result));
        d.Frame();
        var count = result.Events.Count + 1;
        var x = 160 - (count * SeasonEndSummaryScreen.IconPitch - (SeasonEndSummaryScreen.IconPitch - SeasonEndSummaryScreen.IconWidth)) / 2
            + (count - 1) * SeasonEndSummaryScreen.IconPitch;
        Assert.Equal(0, Differing(d, icons, SeasonEndSummaryScreen.TopFiveIcon, x));
        Assert.Equal(0, d.Surface.Pixels[102 * Surface.Width + x - 1]); // Rahmen (Palettenindex 0)

        // Außerhalb der ersten fünf: kein Weltkugel-Symbol an dieser Stelle (letztes Symbol ist ein anderes oder fehlt).
        tables[League.Bundesliga] = [.. others, result.Club];
        Assert.False(game.HasTopFiveStatus(record, result));
        d.Frame();
        var countWithout = result.Events.Count;
        if (countWithout > 0)
        {
            var xLast = 160 - (countWithout * SeasonEndSummaryScreen.IconPitch - (SeasonEndSummaryScreen.IconPitch - SeasonEndSummaryScreen.IconWidth)) / 2
                + (countWithout - 1) * SeasonEndSummaryScreen.IconPitch;
            Assert.NotEqual(0, Differing(d, icons, SeasonEndSummaryScreen.TopFiveIcon, xLast));
        }
    }

    private static int Differing(ScreenDriver d, VgaImage icons, Rect source, int x)
    {
        const int y = 103;
        var differing = 0;
        for (var yy = 0; yy < source.H; yy++)
            for (var xx = 0; xx < source.W; xx++)
                if (d.Surface.Pixels[(y + yy) * Surface.Width + x + xx] != icons.Pixels[(source.Y + yy) * icons.Width + source.X + xx])
                    differing++;
        return differing;
    }
}
