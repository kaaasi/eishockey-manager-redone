using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Competitions;
using EishockeyManager.UI.Screens.Team;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur nationalteam-2 (Runde 8, Welle 3). Bildnamen beziehen sich auf `dosbox/nur-weltkugel/` im Referenz-Repo.
/// </summary>
public class Nationalteam2Tests
{
    /// <summary>Spielvorbereitung mit einem Manager im Modus „nur Weltkugel“ bis „Los jetzt!“.</summary>
    internal static ScreenDriver StartWorldOnlyThroughSetup()
    {
        var d = FlowTests.CreateDriver();
        d.Host.Show(new TitleScreen(d.Host));
        d.Frame();
        d.Click(160, 120);
        Assert.IsType<SetupScreen>(d.Host.Current);
        d.Click(20, 60);
        var cell = SetupScreen.HeadCell(3);
        d.Click(cell.X + 5, cell.Y + 5);
        d.Type("LetsShow");
        d.Press(Key.Enter);
        d.Click(SetupScreen.ModeFlag.X + 5, SetupScreen.ModeFlag.Y + 5); // Fahne aus → nur Weltkugel
        d.Click(SetupScreen.StartButton.X + 5, SetupScreen.StartButton.Y + 5);
        return d;
    }

    /// <summary>
    /// #195, DOSBox <c>06_hauptmenue_bundestrainer_14_april.png</c>: Nach „Los jetzt!“ hält das Original im Modus 1 sofort am
    /// Samstag, 14. April, im Menü des Bundestrainers (Gegner Russland, Konto 500.000 DM). [ORIGINAL] Der Halt liegt in der
    /// Tagesfunktion Datei 0x54C9F: Sie springt am ersten Tag (0x54CEE) zum WM-Termin, ernennt den Bundestrainer (0xD0BA)
    /// und ruft am Tagesende das Hauptmenü 0x44E22 für Manager 5 (0x555A3, `call 0x110:0x66`) – ein Menü am 10. Sept.
    /// davor gibt es nicht. Vorher zeigte das Remake das Menü am Tag 0; gesprungen wurde erst mit „und tschüß“.
    /// </summary>
    [GameDataFact]
    public void Mode1_FirstHaltAfterStart_IsFirstWorldCupDay()
    {
        var d = StartWorldOnlyThroughSetup();
        Assert.IsType<MainMenuScreen>(d.Host.Current);
        var game = d.Host.Game!;
        Assert.Equal(GameMode.WorldOnly, game.Mode);
        Assert.Equal(0, game.PlayedMatchDays);
        Assert.Equal(GameState.FirstWorldCupDay, game.CurrentDay);
        Assert.NotNull(game.NationalCoach);
        Assert.True(game.NationalCoachMenu);
        Assert.Equal(500_000, game.Managers[0].Money);
    }
    /// <summary>
    /// R8W3-06: Die Hinweiszeile der Nationalaufstellung ist die Hinweiszeile 0x41683 der Vereinsaufstellung (gleicher
    /// Bildschirm Datei 0x40364, Aufruf 0x40845). Vorher stand dort eine genäherte Liste („Spiele gesamt (Liga und Pokal)“
    /// usw.) ohne Reihe, Erschöpfung und Stärke. Geprüft wird das Band der Statuszeile gegen <see cref="PlayerHintLine"/>
    /// für die Spalten NR, Art, Stärke, Status und Tendenz; Überfahren ändert nichts.
    /// </summary>
    [GameDataFact]
    public void NationalLineup_HintLine_IsPlayerHintLine()
    {
        var (d, game) = NationalLineupScreenTests.StartWorldOnly();
        game.AutoNominate();
        var coach = game.NationalCoach!;
        coach.AutoFormation();
        d.OpenTile("Aufstellung");
        Assert.IsType<NationalLineupScreen>(d.Host.Current);
        var before = StateFingerprint.Of(game);
        var rows = coach.Rows;
        var layout = PlayerListLayout.Lineup(rows.Count);
        var row = rows.ToList().FindIndex(p => coach.Formation.Contains(p));
        Assert.True(row >= 0);
        var y = LineupScreen.FirstRowY + row * LineupScreen.RowPitch + 2;
        var checkedColumns = 0;
        foreach (var column in new[] { 0, 1, 7, 14, 15 })
        {
            var x = Enumerable.Range(0, 240).First(x => PlayerHintLine.Column(layout, x) == column);
            Assert.Equal(row, PlayerHintLine.Row(layout, y));
            var expected = PlayerHintLine.Text(rows[row], column, new PlayerHintInfo(coach.IsDressed(rows[row]), Line(coach, rows[row])), true);
            Assert.NotNull(expected);
            d.Move(x, y);
            var actual = Band(d.Surface);
            d.Move(0, 0);
            var reference = new Surface();
            Array.Copy(d.Surface.Pixels, reference.Pixels, reference.Pixels.Length);
            TeamUi.StatusLine(reference, d.Host.Assets, 131, LineupScreen.StatusY, expected);
            Assert.True(actual.SequenceEqual(Band(reference)), $"Spalte {column}: „{expected}“");
            checkedColumns++;
        }
        Assert.Equal(5, checkedColumns);
        Assert.Equal(before, StateFingerprint.Of(game));
    }

    private static int Line(NationalCoach coach, Core.Player p) =>
        LineupSlot.All.Where(s => coach.Formation[s] == p).Select(s => s.Block + 1).FirstOrDefault();

    /// <summary>Pixel der Statuszeile (y = StatusY − 8 … StatusY + 8, ganze Breite).</summary>
    private static byte[] Band(Surface s) =>
        s.Pixels.Skip((LineupScreen.StatusY - 8) * Surface.Width).Take(17 * Surface.Width).ToArray();
}
