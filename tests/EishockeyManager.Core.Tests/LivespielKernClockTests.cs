using EishockeyManager.Core;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur livespiel-kern, Teil A2 (L5): Ablaufmodell der Live-Anzeige (<c>docs/analyse/re-livespiel.md</c> §1, §10, §13).
/// Vor dem Teil rot: es gab kein Ablaufmodell (<c>LiveClock</c> fehlte, Build-Fehler).
/// </summary>
public sealed class LivespielKernClockTests
{
    /// <summary>[ORIGINAL] lrandom 0xBFD5: x = (16x + 0xA7) mod 4 000 000, Ergebnis x · (b − a + 1) / 4 000 000 + a.</summary>
    [Fact]
    public void LRandom_FollowsOriginalFormula()
    {
        var r = new LRandom();
        long x = 0;
        for (var i = 0; i < 1000; i++)
        {
            x = (x * 16 + 167) % 4_000_000;
            var expected = (int)(x * 5 / 4_000_000);
            Assert.Equal(expected, r.Next(0, 4));
            Assert.Equal((int)x, r.State);
        }
        Assert.Equal(167, new LRandom().Next(0, 3_999_999));
        Assert.InRange(new LRandom(3_999_999).Next(7, 9), 7, 9);
    }

    /// <summary>
    /// Abnahme A2: Bilder je Minute bei g = 0/60/70. [ORIGINAL] Pause 45 − g/2 bzw. 30 − g/2 mit Zwinkern, n/3 Ticks; ab 70
    /// keine Pause und kein Zufallszug.
    /// </summary>
    [Theory]
    [InlineData(0, 45, 15, 30, 10)]
    [InlineData(10, 40, 13, 25, 8)]
    [InlineData(60, 15, 5, 0, 0)]
    public void Minute_PauseAndTicks(int speed, int units, int ticks, int winkUnits, int winkTicks)
    {
        var options = new GameOptions { MatchSpeed = speed };
        var clock = new LiveClock(options);
        var winks = 0;
        for (var i = 0; i < 500; i++)
        {
            var reference = new LRandom(clock.WinkRandom.State).Next(0, 4) == 0;
            var minute = clock.Minute(display: true);
            Assert.Equal(reference, minute.Wink);
            Assert.Equal(minute.Wink ? (winkUnits, winkTicks) : (units, ticks), (minute.PauseUnits, minute.Ticks));
            winks += minute.Wink ? 1 : 0;
        }
        Assert.InRange(winks, 60, 140); // etwa jede fünfte Minute
    }

    [Theory]
    [InlineData(70)]
    [InlineData(80)]
    public void Minute_FastRun_NoPauseNoDraw(int speed)
    {
        var clock = new LiveClock(new GameOptions { MatchSpeed = speed });
        Assert.True(clock.Fast);
        Assert.Equal(new LiveMinute(false, 0, 0), clock.Minute(display: true));
        Assert.Equal(0, clock.WinkRandom.State);
        Assert.False(clock.WaitsForClick(60, display: true));
    }

    [Fact]
    public void Minute_WithoutDisplay_NoPauseNoDraw()
    {
        var clock = new LiveClock(new GameOptions { MatchSpeed = 0 });
        Assert.Equal(new LiveMinute(false, 0, 0), clock.Minute(display: false));
        Assert.Equal(0, clock.WinkRandom.State);
    }

    /// <summary>[ORIGINAL] Datei 0x3855F–0x38587: Klick nur nach Minute 60 bzw. 70, mit Anzeige und unter 70.</summary>
    [Theory]
    [InlineData(20, true, false)]
    [InlineData(40, true, false)]
    [InlineData(60, true, true)]
    [InlineData(65, true, false)]
    [InlineData(70, true, true)]
    [InlineData(60, false, false)]
    public void WaitsForClick_AfterSixtyAndSeventy(int periodEnd, bool display, bool waits) =>
        Assert.Equal(waits, new LiveClock(new GameOptions { MatchSpeed = 30 }).WaitsForClick(periodEnd, display));

    /// <summary>
    /// Abnahme A2: Lampe an → g = 70 und Torszenen aus, aus → Sicherung zurück ([ORIGINAL] 0x38DBB–0x38E19, Sicherung
    /// 0x36B65–0x36B77, Rückschreiben 0x37833).
    /// </summary>
    [Fact]
    public void Lamp_SetsFastRunAndRestoresBackup()
    {
        var options = new GameOptions { MatchSpeed = 30, LiveMode = 2 };
        var clock = new LiveClock(options);
        clock.BeginMatchDay();
        Assert.False(clock.Lamp);
        Assert.Equal((30, 2), (clock.SavedSpeed, clock.SavedLiveMode));

        clock.ToggleLamp();
        Assert.True(clock.Lamp);
        Assert.Equal((70, 0), (options.MatchSpeed, options.LiveMode));
        Assert.True(clock.Fast);

        clock.ToggleLamp();
        Assert.False(clock.Lamp);
        Assert.Equal((30, 2), (options.MatchSpeed, options.LiveMode));

        clock.ToggleLamp();
        clock.EndMatchDay();
        Assert.Equal((30, 2), (options.MatchSpeed, options.LiveMode));
    }

    /// <summary>
    /// Abnahme A2: Leertaste-Optionen setzen Lampe = 0 ([ORIGINAL] 0x37D29–0x37D5B: vorher Sicherung zurück, danach neu
    /// sichern, Lampe aus).
    /// </summary>
    [Fact]
    public void Options_RestoreThenSaveAndLampOff()
    {
        var options = new GameOptions { MatchSpeed = 20, LiveMode = 1 };
        var clock = new LiveClock(options);
        clock.BeginMatchDay();
        clock.ToggleLamp();
        Assert.Equal(70, options.MatchSpeed);

        clock.BeforeOptions();
        Assert.Equal((20, 1), (options.MatchSpeed, options.LiveMode));
        options.SetMatchSpeed(50); // Spieler ändert in den Optionen
        options.LiveMode = 0;
        clock.AfterOptions();
        Assert.False(clock.Lamp);
        Assert.Equal((50, 0), (clock.SavedSpeed, clock.SavedLiveMode));

        clock.ToggleLamp();
        clock.ToggleLamp();
        Assert.Equal((50, 0), (options.MatchSpeed, options.LiveMode));
    }
}
