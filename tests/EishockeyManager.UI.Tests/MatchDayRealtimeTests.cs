using EishockeyManager.Core;
using EishockeyManager.UI.Screens;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// #838 (Spur abgleich-dosbox-19, Teil 2): Echtzeit eines Spieltags gegen die Messung der DOSBox-Aufnahme `runde-19`
/// (Sitzung 2, DOSBox 0.74-3, <c>cycles=fixed 3000</c>, FILE05, von Minute 1 bis 60 handgestoppt). Bericht:
/// <c>docs/archiv/analyse/abgleich-dosbox.md</c> §5.
/// </summary>
public class MatchDayRealtimeTests
{
    /// <summary>[ORIGINAL] Messwerte ohne Pause (Geschwindigkeit 70: acht Pinguine bzw. Lampe „Schnelldurchlauf“).</summary>
    public const double MeasuredFastSeconds = 15.9, MeasuredLampSeconds = 20.0;

    /// <summary>[ORIGINAL] Messwert Geschwindigkeit 0 (ein Pinguin) zwischen den Reporter-Meldungen: eine Sekunde je Minute.</summary>
    public const double MeasuredSlowMinuteSeconds = 1.0;

    /// <summary>
    /// „Spiele laufen“ wartet wie das Original ohne Anzeige nicht; seine Dauer ist Rechenzeit. Das Remake (1/3 s je Minute)
    /// liegt im gemessenen Bereich der Spieltage ohne Pause (mit einer halben Sekunde Spielraum für die Handstoppung).
    /// </summary>
    [Fact]
    public void RunningDay_WithinMeasuredNoPauseRange()
    {
        Assert.InRange(MatchDayScreen.RunningSeconds, MeasuredFastSeconds - 0.5, MeasuredLampSeconds + 0.5);
    }

    /// <summary>
    /// Geschwindigkeit 70 (acht Pinguine, Wert = Pinguine − 1 mal 10) ist Schnelldurchlauf: keine Pause, kein Zwinkern.
    /// </summary>
    [Fact]
    public void FullSpeed_IsFastWithoutPause()
    {
        var options = new GameOptions();
        options.SetMatchSpeed(70);
        var clock = new LiveClock(options);
        Assert.True(clock.Fast);
        Assert.Equal(new LiveMinute(false, 0, 0), clock.Minute(display: true));
    }

    /// <summary>
    /// Geschwindigkeit 0: Pause des Modells (Mittel über das Zwinkern, 18,2 Ticks/s) plus gemessene Rechenzeit je Minute
    /// (20 s / 60) ergibt die gemessene Sekunde je Minute (±0,15 s für die Handangabe).
    /// </summary>
    [Fact]
    public void SlowestSpeed_PausePlusComputeTime_MatchesMeasuredMinute()
    {
        var options = new GameOptions();
        options.SetMatchSpeed(0);
        var clock = new LiveClock(options);
        var ticks = Enumerable.Range(0, 5000).Select(_ => clock.Minute(display: true).Ticks).Average();
        var minute = ticks / 18.2 + MeasuredLampSeconds / 60;
        Assert.InRange(minute, MeasuredSlowMinuteSeconds - 0.15, MeasuredSlowMinuteSeconds + 0.15);
    }
}
