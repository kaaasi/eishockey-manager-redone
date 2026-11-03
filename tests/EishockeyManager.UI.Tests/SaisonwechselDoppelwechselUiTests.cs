using EishockeyManager.Core;
using EishockeyManager.UI;
using EishockeyManager.UI.Screens;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur saisonwechsel, Teil 5 (Gesamtreview #474, D3): Fenster „Ereignisse“ beim Zwangsabstieg.
/// [ORIGINAL] DOSBox-Beleg `file16_9_ereignisse_29april` (Referenz-Repo, FILE16 → FILE17): SV Bayreuth, Konto −1.234.094 DM,
/// zeigt am 29. April die Symbole 0 (Tabelle mit rotem Pfeil), 3 (Topscorer) und 13 (Fair-Play), keinen Auf- oder Abstiegspfeil.
/// Statusbit 0x04 → Symbol 0 (Tabelle DGROUP 0x4B2E, Datei 0x2714F).
/// </summary>
public class SaisonwechselDoppelwechselUiTests
{
    /// <summary>Vorher rot: Zwangsabstieg fiel auf den Vorgabewert Symbol 6 (Landkarte mit Pokal, Europapokal).</summary>
    [Fact]
    public void EventIcon_ForcedRelegation_IsSymbolZero()
    {
        Assert.Equal(SeasonEndSummaryScreen.Symbol(0), SeasonEndSummaryScreen.EventIcon(SeasonEvent.ForcedRelegation));
        Assert.Equal(new Rect(0, 0, 24, 21), SeasonEndSummaryScreen.EventIcon(SeasonEvent.ForcedRelegation));
    }

    /// <summary>Die übrigen Symbole des Belegs bleiben: Topscorer 3, Fair-Play 13, Europapokal weiter 6.</summary>
    [Fact]
    public void EventIcon_OtherSymbolsOfTheRecording_Unchanged()
    {
        Assert.Equal(SeasonEndSummaryScreen.Symbol(3), SeasonEndSummaryScreen.EventIcon(SeasonEvent.TopScorer));
        Assert.Equal(SeasonEndSummaryScreen.Symbol(13), SeasonEndSummaryScreen.EventIcon(SeasonEvent.FairPlay));
        Assert.Equal(SeasonEndSummaryScreen.Symbol(6), SeasonEndSummaryScreen.EventIcon(SeasonEvent.EuropeanCupWinner));
    }
}
