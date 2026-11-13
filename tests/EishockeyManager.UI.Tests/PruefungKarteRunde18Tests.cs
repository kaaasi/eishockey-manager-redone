using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.UI.Screens.Team;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur pruefung-karte Teil 1 (Runde 18): #805 (R801-02, Inventur A370) – Erschöpfung in der Hinweiszeile und im
/// Spielerdaten-Fenster aus der Belastung (Kaderplatz +0x13, <see cref="Player.Strain"/>, 50 … 150) nach em.exe.
/// </summary>
public class PruefungKarteRunde18Tests
{
    private static Player Skater(int strain) => new()
    {
        Name = "TEST", Role = PlayerRole.Forward, Age = 24, Talent = 3, Condition = 50, Technique = 50, Form = 50,
        Strain = strain, Trend = PlayerTrend.Rising,
    };

    private static readonly PlayerHintInfo InLine2 = new(true, 2);

    /// <summary>
    /// [ORIGINAL] Hinweiszeile 0x41683, Spalte 15 nur mit Rand 0xE9 (Datei 0x41B0A): „ (Erschöpfung:n)“ mit
    /// n = Byte +0x13 · 10 / 15 (Datei 0x41B29–0x41B38: <c>mov al,[es:bx+0x13]; imul 10; idiv 15</c>) – derselbe Wert wie
    /// Spalte E der Listen (<see cref="TrainingRules.ExhaustionDisplay"/>). Vorher (Strain − 50, 0…100) · 10 / 15.
    /// </summary>
    [Theory]
    [InlineData(50, 33)]
    [InlineData(80, 53)]
    [InlineData(100, 66)]
    [InlineData(131, 87)]
    [InlineData(150, 100)]
    public void Hint_Exhaustion_IsStrainTimesTenFifteenths(int strain, int shown)
    {
        var p = Skater(strain);
        Assert.Equal($"Tendenz des Spielers: Steigend. (Erschöpfung:{shown})", PlayerHintLine.Text(p, 15, InLine2, true));
        Assert.Equal(TrainingRules.ExhaustionDisplay(p), shown);
    }

    /// <summary>
    /// [ORIGINAL] Spielerdaten 0x041AC, vierter Balken (Versätze 0xA1C–0xA60, Aufruf der Balkenroutine 0xAC9 bei 0xA63, #819): Länge = (100 − +0x13) · 94 / 100
    /// (lange Multiplikation 0x3CA, Division 0x47C), im Balken auf 1 … 94 begrenzt (0xBBA–0xBC7). Frisch (50) → 47 Pixel,
    /// ab Belastung 99 → 1 Pixel. Vorher (Strain − 50, 0…100) als Prozent · 94 / 99, also umgekehrt (frisch → 1, erschöpft → 94).
    /// </summary>
    [Theory]
    [InlineData(50, 47)]
    [InlineData(60, 37)]
    [InlineData(66, 31)] // #818: Divisor 100 (bei 99 wären es 32)
    [InlineData(80, 18)]
    [InlineData(98, 1)]
    [InlineData(100, 1)]
    [InlineData(150, 1)]
    public void InfoPopup_ExhaustionBar_FollowsOriginal(int strain, int pixels) =>
        Assert.Equal(pixels, PlayerInfoPopup.ExhaustionBarPixels(Skater(strain)));

    /// <summary>
    /// #817 (R813 P3-1): Der Zeichenweg (<see cref="PlayerInfoPopup.Draw"/> über <see cref="PlayerInfoPopup.BarPixelLengths"/>)
    /// nutzt für den vierten Balken die Länge nach 0x041AC, nicht die Prozentskala der anderen Balken. Erwartete Werte
    /// unabhängig von <see cref="PlayerInfoPopup.ExhaustionBarPixels"/> ausgeschrieben.
    /// </summary>
    [Theory]
    [InlineData(50, 47)]
    [InlineData(66, 31)]
    [InlineData(80, 18)]
    [InlineData(120, 1)]
    public void InfoPopup_DrawnBarLengths_UseExhaustionPixels(int strain, int pixels)
    {
        var lengths = PlayerInfoPopup.BarPixelLengths(Skater(strain));
        Assert.Equal(4, lengths.Length);
        Assert.Equal(pixels, lengths[3]);
    }
}
