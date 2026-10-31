using EishockeyManager.Tests.Support;
using EishockeyManager.UI;
using EishockeyManager.UI.Screens;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Befund NB-01 (#205): „und tschüß…“ (große Kachel 7) beendet im Original das Programm nach einer Sicherheitsfrage,
/// der nächste Tag startet nur per Klick auf das Vereinslogo der Infoleiste; die Leertaste hat im Hauptmenü keine
/// Funktion (em.exe Datei 0x45483–0x4555C, Frage 0x45C33).
/// </summary>
public class MainMenuQuitTests
{
    // Mitte der großen Kachel 7 (x 239–310, y 140–196) und des Vereinslogos (x 275–312, y 34–71)
    private const int TileX = 274, TileY = 168;
    private const int LogoX = 293, LogoY = 52;

    [GameDataFact]
    public void QuitTile_AsksInsteadOfPlayingDay()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        var before = StateFingerprint.Of(game);

        d.Click(TileX, TileY);
        d.Frames(3);

        var menu = Assert.IsType<MainMenuScreen>(d.Host.Current);
        Assert.NotNull(menu.QuitQuestion);
        Assert.False(d.Host.ExitRequested);
        Assert.Equal(0, game.PlayedMatchDays);
        Assert.Equal(before, StateFingerprint.Of(game));
    }

    [GameDataFact]
    public void QuitQuestion_KeinGedanke_ReturnsToMenuUnchanged()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        var before = StateFingerprint.Of(game);
        var menu = Assert.IsType<MainMenuScreen>(d.Host.Current);

        d.Click(TileX, TileY);
        // Solange die Frage offen ist, startet auch das Logo keinen Tag
        d.Click(LogoX, LogoY);
        Assert.Same(menu, d.Host.Current);
        var no = QuitQuestion.NoButton;
        d.Click(no.X + 5, no.Y + 5);

        Assert.Same(menu, d.Host.Current);
        Assert.Null(menu.QuitQuestion);
        Assert.False(d.Host.ExitRequested);
        Assert.Equal(before, StateFingerprint.Of(game));
    }

    [GameDataFact]
    public void QuitQuestion_EscapeAndRightClick_MeanKeinGedanke()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var menu = Assert.IsType<MainMenuScreen>(d.Host.Current);

        d.Click(TileX, TileY);
        d.Press(Key.Escape);
        Assert.Null(menu.QuitQuestion);
        d.Click(TileX, TileY);
        d.RightClick(160, 120);
        Assert.Null(menu.QuitQuestion);
        Assert.False(d.Host.ExitRequested);
    }

    [GameDataFact]
    public void QuitQuestion_LeiderJa_ExitsProgram()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        var before = StateFingerprint.Of(game);

        d.Click(TileX, TileY);
        var yes = QuitQuestion.YesButton;
        d.Click(yes.X + 5, yes.Y + 5);

        Assert.True(d.Host.ExitRequested);
        Assert.Equal(0, game.PlayedMatchDays);
        Assert.Equal(before, StateFingerprint.Of(game));
    }

    [GameDataFact]
    public void Space_InMainMenu_DoesNothing()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        var before = StateFingerprint.Of(game);

        d.Press(Key.Space);
        d.Frames(3);

        Assert.IsType<MainMenuScreen>(d.Host.Current);
        Assert.Equal(before, StateFingerprint.Of(game));
    }

    [GameDataFact]
    public void LogoClick_StartsDay()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();

        d.Click(LogoX, LogoY);

        Assert.IsType<MatchDayScreen>(d.Host.Current);
    }

    [GameDataFact]
    public void LogoClick_WorksWithSmallTilesOpen()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var tile = MainMenuScreen.BigTile(0);
        d.Click(tile.X + 10, tile.Y + 10);

        d.Click(LogoX, LogoY);

        Assert.IsType<MatchDayScreen>(d.Host.Current);
    }

    [GameDataTheory]
    [InlineData(274, 33)] // eine Zeile über dem Klickfeld
    [InlineData(274, 52)] // linker Rand des Kastens
    [InlineData(313, 52)] // rechts außerhalb
    [InlineData(293, 72)] // unter dem Klickfeld
    public void ClickBesideLogo_DoesNothing(int x, int y)
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var before = StateFingerprint.Of(d.Host.Game!);

        d.Click(x, y);

        Assert.IsType<MainMenuScreen>(d.Host.Current);
        Assert.Equal(before, StateFingerprint.Of(d.Host.Game!));
    }

    [GameDataFact]
    public void NextDay_ClicksLogo()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();

        d.NextDay();

        Assert.IsType<MatchDayScreen>(d.Host.Current);
    }
}
