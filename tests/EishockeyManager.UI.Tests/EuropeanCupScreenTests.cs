using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Competitions;

namespace EishockeyManager.UI.Tests;

/// <summary>Paket „wettbewerbe“: Bildschirm „Europacup Gruppe / Endrunde“ (Paarungen → Europapokal).</summary>
public class EuropeanCupScreenTests
{
    [Fact]
    public void Texts_AsInOriginal()
    {
        Assert.Equal("Gruppe 2", EuropeanCupScreen.Label(1));
        Assert.Equal("Endrunde", EuropeanCupScreen.Label(2));
        Assert.Equal(" 4", CompetitionWidgets.Number(4, 2));
        Assert.Equal("11", CompetitionWidgets.Number(11, 2));
    }

    [GameDataFact]
    public void Tile_ShowsGroups_ButtonsSwitchView_StateUnchanged()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        var before = StateFingerprint.Of(game);

        d.OpenTile("Europapokal");
        d.Frame();
        var screen = Assert.IsType<EuropeanCupScreen>(d.Host.Current);
        Assert.Equal(0, screen.View);
        Assert.Same(game.EuropeanCup!.Groups[0], screen.Group);

        d.Click(EuropeanCupScreen.LeftButton.X + 10, EuropeanCupScreen.LeftButton.Y + 10);
        Assert.Equal(1, screen.View);
        Assert.Same(game.EuropeanCup.Groups[1], screen.Group);

        d.Click(EuropeanCupScreen.RightButton.X + 10, EuropeanCupScreen.RightButton.Y + 10);
        Assert.Equal(2, screen.View);
        Assert.Null(screen.Group); // Endrunde steht vor dem siebten Termin noch nicht fest
        d.Frame();

        d.Press(Key.Escape);
        Assert.IsType<MainMenuScreen>(d.Host.Current);
        Assert.Equal(before, StateFingerprint.Of(game));
    }

    /// <summary>Nach der Gruppenphase: Endrunde sichtbar, Öffnen verändert nichts.</summary>
    [GameDataFact]
    public void AfterGroupStage_FinalRoundShown_WithoutChangingState()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        for (var guard = 0; game.EuropeanCup!.FinalRound is null; guard++)
        {
            Assert.True(guard < 40, "Endrunde nicht erreicht");
            d.PlayMatchDay();
        }
        var before = StateFingerprint.Of(game);

        d.OpenTile("Europapokal");
        d.Click(EuropeanCupScreen.RightButton.X + 10, EuropeanCupScreen.RightButton.Y + 10);
        d.Frame();
        var screen = Assert.IsType<EuropeanCupScreen>(d.Host.Current);
        Assert.Same(game.EuropeanCup.FinalRound, screen.Group);
        FlowTests.ReturnToMainMenu(d);

        Assert.Equal(before, StateFingerprint.Of(game));
    }
}
