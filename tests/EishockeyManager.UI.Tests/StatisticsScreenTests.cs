using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Statistics;

namespace EishockeyManager.UI.Tests;

/// <summary>Paket „wettbewerbe“, PR 5: „Ewige Tabelle“ (Totale) und „Früher“.</summary>
public class StatisticsScreenTests
{
    /// <summary>[ORIGINAL] Texte aus em.exe (0x2C099, 0x2C9D6), Beispiel aus dem Video (16:52).</summary>
    [Fact]
    public void HistoryLines_AsInOriginal()
    {
        var video = new ManagerSeasonHistory(1993, 42, League.Oberliga, 8, LeagueOutcome.Rank, CupOutcome.Eliminated, 0, CupOutcome.NotParticipating, false);
        Assert.Equal([("8. Platz der Oberliga", (byte)11), ("DEB-Pokal: Erste Runde ", (byte)11), ("Europacup: Nicht im Wettbewerb", (byte)11)],
            HistoryScreen.Lines(video));

        var champion = video with { League = League.Bundesliga, LeagueOutcome = LeagueOutcome.GermanChampion, Deb = CupOutcome.Winner, Europe = CupOutcome.Eliminated, EuropeFinalRound = true };
        Assert.Equal([("Deutscher Meister", (byte)7), ("DEB-Pokal: SIEGER", (byte)7), ("Europacup: Endrunde", (byte)11)], HistoryScreen.Lines(champion));
        Assert.Equal("Play-Off HALBFINALE", HistoryScreen.Lines(video with { LeagueOutcome = LeagueOutcome.PlayoffSemiFinal })[0].Text);
    }

    /// <summary>Regressionstest: „Totale“ und „Früher“ waren Platzhalter; jetzt echte Bildschirme, zustandsrein.</summary>
    [GameDataFact]
    public void Tiles_OpenRealScreens_StateUnchanged()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        var before = StateFingerprint.Of(game);

        d.OpenTile("Totale");
        d.Frame();
        var eternal = Assert.IsType<EternalTableScreen>(d.Host.Current);
        d.Press(Key.Down);
        Assert.Equal(1, eternal.Scroll);
        d.Press(Key.Up);
        Assert.Equal(0, eternal.Scroll);
        FlowTests.ReturnToMainMenu(d);

        d.OpenTile("Früher");
        d.Frame();
        Assert.IsType<HistoryScreen>(d.Host.Current);
        FlowTests.ReturnToMainMenu(d);

        Assert.Equal(before, StateFingerprint.Of(game));
    }

    /// <summary>Nach der ersten Saison: Ewige Tabelle mit Punkten, „Früher“ mit einem Eintrag; Öffnen ändert nichts.</summary>
    [GameDataFact]
    public void AfterFirstSeason_ScreensShowRecordedSeason()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        for (var guard = 0; game.History.Count == 0; guard++)
        {
            Assert.True(guard < 5000);
            d.Press(Key.Space);
        }
        for (var guard = 0; d.Host.Current is not MainMenuScreen; guard++)
        {
            Assert.True(guard < 50);
            d.Press(Key.Space);
        }
        Assert.Single(game.ManagerHistory(0));
        Assert.True(game.EternalTable[0].Points > 0);
        var before = StateFingerprint.Of(game);

        d.OpenTile("Totale");
        for (var i = 0; i < 60; i++)
            d.Press(Key.Down);
        Assert.Equal(game.EternalTable.Count - EternalTableScreen.VisibleRows, Assert.IsType<EternalTableScreen>(d.Host.Current).Scroll);
        FlowTests.ReturnToMainMenu(d);
        d.OpenTile("Früher");
        Assert.Equal(0, Assert.IsType<HistoryScreen>(d.Host.Current).Index);
        FlowTests.ReturnToMainMenu(d);

        Assert.Equal(before, StateFingerprint.Of(game));
    }
}
