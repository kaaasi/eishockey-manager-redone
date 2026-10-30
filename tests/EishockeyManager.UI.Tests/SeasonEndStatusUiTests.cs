using EishockeyManager.Tests.Support;
using EishockeyManager.Core;
using EishockeyManager.UI;
using EishockeyManager.UI.Screens;

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
        Assert.Equal(new Rect(48, 21, 24, 21), SeasonEndSummaryScreen.EventIcon(SeasonEvent.TopFive));
        Assert.Equal(new Rect(48, 0, 24, 21), SeasonEndSummaryScreen.EventIcon(SeasonEvent.Promotion));   // Bit 0 → 2
        Assert.Equal(new Rect(24, 0, 24, 21), SeasonEndSummaryScreen.EventIcon(SeasonEvent.Relegation));  // Bit 1 → 1
        Assert.Equal(new Rect(96, 0, 24, 21), SeasonEndSummaryScreen.EventIcon(SeasonEvent.CupWinner));   // Bit 4 → 4
        Assert.Equal(new Rect(0, 21, 24, 21), SeasonEndSummaryScreen.EventIcon(SeasonEvent.EuropeanCupWinner)); // Bit 5 → 6
        Assert.Equal(new Rect(72, 0, 24, 21), SeasonEndSummaryScreen.EventIcon(SeasonEvent.GermanChampion)); // [APPROX]
    }

    /// <summary>Das Fenster zeichnet Symbol 8 aus kb\13 mit schwarzem Rahmen an der Stelle des ersten Symbols.</summary>
    [GameDataFact]
    public void SummaryWindow_DrawsGlobeForTopFive()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        for (var guard = 0; d.Host.Current is not SeasonEndSummaryScreen && guard++ < 20000;)
            d.Press(Key.Space);
        Assert.IsType<SeasonEndSummaryScreen>(d.Host.Current);

        var record = game.History[^1];
        record.Managers[0] = record.Managers[0] with { Events = [SeasonEvent.TopFive] };
        d.Frame();

        var icons = d.Host.Assets.Image(13);
        const int x = 160 - (SeasonEndSummaryScreen.IconPitch - (SeasonEndSummaryScreen.IconPitch - SeasonEndSummaryScreen.IconWidth)) / 2;
        const int y = 103;
        var source = SeasonEndSummaryScreen.Symbol(8);
        var differing = 0;
        for (var yy = 0; yy < source.H; yy++)
            for (var xx = 0; xx < source.W; xx++)
                if (d.Surface.Pixels[(y + yy) * Surface.Width + x + xx] != icons.Pixels[(source.Y + yy) * icons.Width + source.X + xx])
                    differing++;
        Assert.Equal(0, differing);
        Assert.Equal(0, d.Surface.Pixels[(y - 1) * Surface.Width + x - 1]); // Rahmen (Palettenindex 0)
    }
}
