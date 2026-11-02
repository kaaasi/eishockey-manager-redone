using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.UI.Screens.Team;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur kader-transfer, Teil 2 (A368, A369): Anzeige der neuen Kaderplatzfelder im Spielerdaten-Fenster (0x041AC) und in der
/// Hinweiszeile (0x41683). Vorher rot: Versicherung „ja“ statt der Prämie, „Verletzt.“ ohne Art.
/// </summary>
public class KaderTransferTeil2UiTests
{
    private static Player Skater() => new() { Name = "TEST", Role = PlayerRole.Forward, Age = 24, Condition = 50, Technique = 50, Form = 50 };

    private static readonly PlayerHintInfo InLine2 = new(true, 2);

    /// <summary>[ORIGINAL] 0x041AC Feld 2 = Kaderplatz +0x30 (Drittel / 3 + „ DM“), 0 → „keine“.</summary>
    [Fact]
    public void Data_Insurance_ShowsPremium()
    {
        var p = Skater();
        Assert.Equal("keine", PlayerDataWindow.DataValues(p, 0, PlayerDataContext.Unknown)[2]);
        p.InsurancePremium = 4_950;
        Assert.Equal("4.950 DM", PlayerDataWindow.DataValues(p, 0, PlayerDataContext.Unknown)[2]);
    }

    /// <summary>[ORIGINAL] 0x041AC Feld 0 = +0x2C: Kaderspieler zeigen ihren Start- bzw. Kaufpreis, 0 → „-“.</summary>
    [Fact]
    public void Data_PurchasePrice_OfSquadPlayer()
    {
        var p = Skater();
        Assert.Equal("-", PlayerDataWindow.DataValues(p, PlayerDataWindow.PurchasePrice(p), PlayerDataContext.Unknown)[0]);
        p.PurchasePrice = 400_000;
        Assert.Equal("400.000 DM", PlayerDataWindow.DataValues(p, PlayerDataWindow.PurchasePrice(p), PlayerDataContext.Unknown)[0]);
    }

    /// <summary>
    /// [ORIGINAL] 0x41683 Versätze 0x3D4–0x421: „Verletzt.“ (DGROUP 0xB84) + „(“ (0x50BE) + Name (0xA00 + 4 · +0x17) + „)“
    /// (0x50C0), ohne Leerzeichen. Unbekannte Art (ältere Spielstände): ohne Klammer.
    /// </summary>
    [Fact]
    public void Hint_Injured_ShowsKind()
    {
        var p = Skater();
        p.InjuredGames = 3;
        Assert.Equal("Status: Verletzt.", PlayerHintLine.Text(p, 14, InLine2, false));
        p.InjuryKind = 2;
        Assert.Equal("Status: Verletzt.(Gehirnerschütterung)", PlayerHintLine.Text(p, 14, InLine2, false));
        p.InjuryKind = 27;
        Assert.Equal("Status: Verletzt.(Darmerkrankung)", PlayerHintLine.Text(p, 14, InLine2, false));
        p.InjuredGames = 0; // geheilt: Art bleibt im Feld, Anzeige nicht
        Assert.Equal("Status: IM TEAM", PlayerHintLine.Text(p, 14, InLine2, false));
    }
}
