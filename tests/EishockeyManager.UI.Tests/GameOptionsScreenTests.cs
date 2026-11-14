using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Optionsbildschirm des Originals (em.exe 0x2CD1C, Spur optionen-wirkung Teil 2): Schalter, Regler, Zeichnung.
/// Vorher rot: Typ <see cref="GameOptionsScreen"/> fehlte, die Optionen waren nur lesend angezeigt.
/// </summary>
public class GameOptionsScreenTests
{
    private static (ScreenDriver Driver, GameOptionsScreen Screen, GameState Game) Open()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        d.OpenTile("Einstellungen"); // #836 (R15-E2 = C): Kachel öffnet das Optionsfenster direkt (Original 0x2CD1C)
        return (d, Assert.IsType<GameOptionsScreen>(d.Host.Current), d.Host.Game!);
    }

    private static void ClickSwitch(ScreenDriver d, int index)
    {
        var r = GameOptionsScreen.SwitchRect(index);
        d.Click(r.X + r.W / 2, r.Y + r.H / 2);
    }

    /// <summary>Öffnen und Zeichnen ändern weder Optionen noch Geld noch Tag.</summary>
    [GameDataFact]
    public void Opening_ChangesNothing()
    {
        var (d, _, game) = Open();
        var day = game.CurrentDay;
        var money = game.ActiveManager.Money;
        d.Frames(3);
        var o = game.Options;
        Assert.All(o.Reports, Assert.False);
        Assert.Equal((0, true, false, false, GameOptions.DefaultMatchSpeed),
            (o.LiveMode, o.ShowDebCup, o.ShowEuropeanCup, o.ShowWorldCup, o.MatchSpeed));
        Assert.Equal((day, money), (game.CurrentDay, game.ActiveManager.Money));
    }

    /// <summary>[ORIGINAL] Index 0–8 = Liga · 3 + Zeile; Bild 49: Ergebnisse 1. Liga, Tabelle 1. und 2. Liga an.</summary>
    [GameDataFact]
    public void MatrixSwitches_ToggleReportOfLeagueAndRow()
    {
        var (d, _, game) = Open();
        ClickSwitch(d, GameOptionsScreen.ReportSwitch(ReportKind.Results, League.Bundesliga));
        ClickSwitch(d, GameOptionsScreen.ReportSwitch(ReportKind.Table, League.Bundesliga));
        ClickSwitch(d, GameOptionsScreen.ReportSwitch(ReportKind.Table, League.ZweiteLiga));
        Assert.True(game.Options.ShowsReport(ReportKind.Results, League.Bundesliga));
        Assert.True(game.Options.ShowsReport(ReportKind.Table, League.Bundesliga));
        Assert.True(game.Options.ShowsReport(ReportKind.Table, League.ZweiteLiga));
        Assert.Equal(3, game.Options.Reports.Count(r => r));
        // Lage: Spalte 1.Liga x = 222, Zeilen y = 130/144/158
        Assert.Equal(new Rect(222, 144, 11, 13), GameOptionsScreen.SwitchRect(1));
        Assert.Equal(new Rect(252, 158, 11, 13), GameOptionsScreen.SwitchRect(5));
        ClickSwitch(d, GameOptionsScreen.ReportSwitch(ReportKind.Table, League.ZweiteLiga));
        Assert.False(game.Options.ShowsReport(ReportKind.Table, League.ZweiteLiga));
    }

    /// <summary>[ORIGINAL] Index 10–12 schalten DEB-Pokal, Europapokal, WM-Spiele (xor 1).</summary>
    [GameDataFact]
    public void CupSwitches_Toggle()
    {
        var (d, _, game) = Open();
        ClickSwitch(d, 10);
        ClickSwitch(d, 11);
        ClickSwitch(d, 12);
        Assert.Equal((false, true, true), (game.Options.ShowDebCup, game.Options.ShowEuropeanCup, game.Options.ShowWorldCup));
        ClickSwitch(d, 12);
        Assert.False(game.Options.ShowWorldCup);
    }

    /// <summary>[ORIGINAL] Torszenen (Index 9): 0 → 1 → 2 (Meldung „Neuer Spielmodus:“, einmal) → 0.</summary>
    [GameDataFact]
    public void GoalScenes_CyclesThreeStagesAndShowsMessageOnce()
    {
        var (d, screen, game) = Open();
        ClickSwitch(d, GameOptionsScreen.GoalScenesSwitch);
        Assert.Equal(1, game.Options.LiveMode);
        Assert.Empty(screen.MessageLines);
        ClickSwitch(d, GameOptionsScreen.GoalScenesSwitch);
        Assert.Equal(2, game.Options.LiveMode);
        Assert.Equal("Neuer Spielmodus:", screen.MessageLines[0]);
        d.Click(5, 5); // Meldung schließen, sonst nichts
        Assert.Empty(screen.MessageLines);
        Assert.Equal(2, game.Options.LiveMode);
        ClickSwitch(d, GameOptionsScreen.GoalScenesSwitch);
        Assert.Equal(0, game.Options.LiveMode);
        ClickSwitch(d, GameOptionsScreen.GoalScenesSwitch);
        ClickSwitch(d, GameOptionsScreen.GoalScenesSwitch);
        Assert.Equal(2, game.Options.LiveMode);
        Assert.Empty(screen.MessageLines);
    }

    /// <summary>[APPROX] Hintergrundgrafik und Scrolling sind im Remake ohne Bedeutung: fest an, Klick wirkungslos.</summary>
    [GameDataFact]
    public void BackgroundAndScrolling_AreFixed()
    {
        var (d, screen, game) = Open();
        var before = (game.Options.Reports.ToArray(), game.Options.LiveMode, game.Options.ShowDebCup);
        ClickSwitch(d, GameOptionsScreen.BackgroundSwitch);
        ClickSwitch(d, GameOptionsScreen.ScrollingSwitch);
        Assert.Equal(1, screen.SwitchValue(GameOptionsScreen.BackgroundSwitch));
        Assert.Equal(1, screen.SwitchValue(GameOptionsScreen.ScrollingSwitch));
        Assert.Equal(before.Item1, game.Options.Reports);
        Assert.Equal((before.LiveMode, before.ShowDebCup), (game.Options.LiveMode, game.Options.ShowDebCup));
    }

    /// <summary>[ORIGINAL] Regler: Klick x 191–285 setzt (x − 190) / 12 · 10; Pinguine = Wert / 9 + 1 (höchstens 8).</summary>
    [GameDataTheory]
    [InlineData(191, 0)]
    [InlineData(201, 0)]
    [InlineData(202, 10)]
    [InlineData(250, 50)]
    [InlineData(285, 70)]
    public void SpeedSlider_SetsSpeedFromClick(int x, int expected)
    {
        var (d, _, game) = Open();
        d.Click(x, 90);
        Assert.Equal(expected, game.Options.MatchSpeed);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(10, 2)]
    [InlineData(60, 7)]
    [InlineData(70, 8)]
    [InlineData(80, 8)]
    public void SpeedPenguins_FollowOriginal(int speed, int penguins) =>
        Assert.Equal(penguins, GameOptionsScreen.SpeedPenguins(speed));

    /// <summary>Kopfzeile wie Bild 48 („V1.00 - Ende: Nie. (Level 1)“): Endjahr bzw. „Nie.“, Level + 1.</summary>
    [GameDataFact]
    public void Header_ShowsEndAndLevel()
    {
        var (_, screen, game) = Open();
        Assert.Equal($"V1.00 - Ende:  Nie. (Level {game.ActiveManager.Level + 1})", screen.HeaderText());
    }

    /// <summary>
    /// Zeichnung: Schalter aus kb\33 – an = Pinguin (Bauch Farbe 11), aus = Loch (Farbe 219). Der
    /// Wechsel eines Schalters ändert nur dessen Rechteck.
    /// </summary>
    [GameDataFact]
    public void Draw_ShowsPenguinOrHole()
    {
        var (d, _, _) = Open();
        d.Move(5, 235);
        var before = (byte[])d.Surface.Pixels.Clone();
        var r = GameOptionsScreen.SwitchRect(11); // Europapokal: aus
        Assert.Equal(219, before[(r.Y + 10) * Surface.Width + r.X + 5]); // Loch: Farbe 219 (kb\33)
        ClickSwitch(d, 11);
        d.Move(5, 235);
        var after = d.Surface.Pixels;
        Assert.Equal(11, after[(r.Y + 10) * Surface.Width + r.X + 5]); // Pinguin: Bauch hell
        for (var y = 0; y < Surface.Height; y++)
            for (var x = 0; x < Surface.Width; x++)
                if (!r.Contains(x, y) && !(x < 20 && y > 220))
                    Assert.True(before[y * Surface.Width + x] == after[y * Surface.Width + x], $"Pixel ({x}|{y}) geändert");
    }

    /// <summary>Rechtsklick, Escape und Enter führen zurück zu den Einstellungen.</summary>
    [GameDataFact]
    public void RightClick_ReturnsToSettings()
    {
        var (d, _, _) = Open();
        d.RightClick(100, 100);
        Assert.IsType<MainMenuScreen>(d.Host.Current); // #836: zurück ins Hauptmenü wie im Original
    }

    /// <summary>Zusatzschalter „Ganzer Tag“ (#349): Vorgabe aus, Klick schaltet die Einstellung um.</summary>
    [GameDataFact]
    public void ManagerDaySwitch_TogglesOption()
    {
        var (d, _, game) = Open();
        Assert.False(game.Options.ManagerDayMinuteLoop);
        ClickSwitch(d, GameOptionsScreen.ManagerDaySwitch);
        Assert.True(game.Options.ManagerDayMinuteLoop);
        ClickSwitch(d, GameOptionsScreen.ManagerDaySwitch);
        Assert.False(game.Options.ManagerDayMinuteLoop);
    }
}
