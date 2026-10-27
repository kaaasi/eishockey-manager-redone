using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Competitions;

namespace EishockeyManager.UI.Tests;

/// <summary>Paket „wettbewerbe“: WM-Bildschirm (Paarungen → WM, Tabellen → Tabelle WM).</summary>
public class WorldCupScreenTests
{
    [GameDataFact]
    public void Tile_ShowsGroupWithFlags_ButtonSwitchesGroup_StateUnchanged()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        var before = StateFingerprint.Of(game);

        d.OpenTile("WM");
        d.Frame();
        var screen = Assert.IsType<WorldCupScreen>(d.Host.Current);
        Assert.Same(game.WorldCup!.Groups[0], screen.Group);

        // Jede Flagge (Logo des Nationalteams) steht pixelgenau an ihrem Platz
        var rows = screen.Group!.Table(game.Clubs);
        for (var n = 0; n < rows.Count; n++)
        {
            var logo = d.Host.Assets.Image(rows[n].Club.LogoGraphic!.Value);
            var x = WorldCupScreen.FlagX[n & 1];
            var y = WorldCupScreen.FirstBaseline + WorldCupScreen.RowPitch * n - 18;
            for (var py = 0; py < logo.Height; py++)
                for (var px = 0; px < logo.Width; px++)
                    Assert.Equal(logo.Pixels[py * logo.Width + px], d.Surface.Pixels[(y + py) * Surface.Width + x + px]);
        }

        d.Click(WorldCupScreen.GroupButton.X + 5, WorldCupScreen.GroupButton.Y + 5);
        Assert.Same(game.WorldCup.Groups[1], screen.Group);
        d.Press(Key.Escape);
        Assert.IsType<MainMenuScreen>(d.Host.Current);

        d.OpenTile("Tabelle WM");
        Assert.IsType<WorldCupScreen>(d.Host.Current);
        FlowTests.ReturnToMainMenu(d);
        Assert.Equal(before, StateFingerprint.Of(game));
    }

    [GameDataFact]
    public void NationalTeams_HaveDistinctFlags()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var flags = Enumerable.Range(NationalTeams.First, 12).Select(i => d.Host.Game!.Clubs[i].LogoGraphic).ToList();
        Assert.All(flags, f => Assert.NotNull(f));
        Assert.Equal(12, flags.Distinct().Count());
    }
}
