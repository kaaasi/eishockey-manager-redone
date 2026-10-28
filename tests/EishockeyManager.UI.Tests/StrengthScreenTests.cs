using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Statistics;

namespace EishockeyManager.UI.Tests;

/// <summary>Paket „wettbewerbe“: Bildschirm „STÄRKEN …“ (Hauptmenü → Stärken).</summary>
public class StrengthScreenTests
{
    [GameDataFact]
    public void AllFourTiles_Open_ButtonsSwitchView_StateUnchanged()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        var before = StateFingerprint.Of(game);

        foreach (var (tile, title) in new[]
        {
            ("Stärken 1. Liga", "STÄRKEN Bundesliga - Gesamt"), ("Stärken 2. Liga", "STÄRKEN Zweite Liga - Gesamt"),
            ("Stärken Oberliga", "STÄRKEN Oberliga - Gesamt"), ("Stärken Länderteams", "STÄRKEN Länderteams - Gesamt"),
        })
        {
            d.OpenTile(tile);
            d.Frame();
            var screen = Assert.IsType<StrengthScreen>(d.Host.Current);
            Assert.Equal(title, screen.Title);
            Assert.Equal(12, screen.Rows.Count);
            d.Click(StrengthScreen.ButtonRect(1).X + 5, StrengthScreen.ButtonRect(1).Y + 5);
            Assert.Equal(StrengthView.Abwehr, screen.View);
            d.Click(StrengthScreen.ButtonRect(2).X + 5, StrengthScreen.ButtonRect(2).Y + 5);
            Assert.Equal(StrengthView.Sturm, screen.View);
            Assert.EndsWith("- Sturm", screen.Title);
            FlowTests.ReturnToMainMenu(d);
        }
        Assert.Equal(before, StateFingerprint.Of(game));
    }

    /// <summary>[ORIGINAL] Länderteams: Kondition und Technik aus em.exe wie im Video (Kanada 98/93, Deutschland 88/91).</summary>
    [GameDataFact]
    public void NationalTeams_ConditionAndTechnique_AsInVideo()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        d.OpenTile("Stärken Länderteams");
        var rows = Assert.IsType<StrengthScreen>(d.Host.Current).Rows.ToDictionary(r => r.Club.Name);
        Assert.Equal((99, 99), (rows["RUSSLAND"].Condition, rows["RUSSLAND"].Technique));
        Assert.Equal((98, 93), (rows["KANADA"].Condition, rows["KANADA"].Technique));
        Assert.Equal((96, 96), (rows["TSCHECHEI"].Condition, rows["TSCHECHEI"].Technique));
        Assert.Equal((88, 91), (rows["DEUTSCHLAND"].Condition, rows["DEUTSCHLAND"].Technique));
        Assert.Equal((72, 79), (rows["FRANKREICH"].Condition, rows["FRANKREICH"].Technique));
    }
}
