using EishockeyManager.Tests.Support;
using EishockeyManager.Core;
using EishockeyManager.UI;
using EishockeyManager.UI.Screens;

namespace EishockeyManager.UI.Tests;

/// <summary>Saisonablauf über die Oberfläche mit den Originaldateien (übersprungen ohne „gamedata“).</summary>
public class SeasonUiTests
{
    [GameDataFact]
    public void ThreeSeasons_ThroughTheUserInterface()
    {
        var d = FlowTests.CreateDriver();

        d.StartNewGame();
        var game = d.Host.Game!;
        var seenSeasonEnd = 0;
        var seenSummary = 0;
        var guard = 0;
        while (game.History.Count < 3 && guard++ < 20000)
        {
            KeepSquadHelper.KeepSquads(game); // Spur heimspiel-statistik (#136): sonst ab Saison 3 entlassen
            d.Press(Key.Space);
            if (d.Host.Current is SeasonEndScreen) seenSeasonEnd++;
            if (d.Host.Current is SeasonEndSummaryScreen) seenSummary++;
        }
        // Nach dem dritten Saisonende zurück ins Hauptmenü der vierten Saison. Mit WM steht vor „Saisonende“ noch das
        // Meisterfoto „Weltmeister“ (Paket „wettbewerbe“) – der dritte Saisonende-Bildschirm erscheint erst hier.
        while (d.Host.Current is not MainMenuScreen && guard++ < 20000)
        {
            d.Press(Key.Space);
            if (d.Host.Current is SeasonEndScreen) seenSeasonEnd++;
            if (d.Host.Current is SeasonEndSummaryScreen) seenSummary++;
        }

        Assert.Equal(3, game.History.Count);
        Assert.Equal(1996, game.Season);
        Assert.True(seenSeasonEnd >= 3);
        Assert.True(seenSummary >= 3);
        Assert.Equal(0, game.CurrentDay);
        Assert.All(Enum.GetValues<League>(), l => Assert.Equal(12, game.LeagueClubs(l).Count()));
        Assert.All(game.History, r => Assert.Equal(2, r.PromotedInto[League.Oberliga].Count));
    }

    [GameDataFact]
    public void InfoBar_ShowsSeasonEndAfterClubSeason()
    {
        var d = FlowTests.CreateDriver();

        d.StartNewGame();
        var game = d.Host.Game!;
        while (game.Phase == SeasonPhase.Running)
            d.PlayMatchDay();
        Assert.Equal(SeasonPhase.ClubSeasonOver, game.Phase);
        Assert.Equal("Samstag, 14. April  1994", game.DateText);
        Assert.IsType<MainMenuScreen>(d.Host.Current);
        Assert.Equal("Saisonende", SeasonInfo.NoMatchText(game));
    }

    [GameDataFact]
    public void GameEnds_AfterChosenDuration()
    {
        var d = FlowTests.CreateDriver();

        d.StartNewGame();
        var game = d.Host.Game!;
        game.Setup.Duration = GameDuration.EineSaison;
        var guard = 0;
        while (d.Host.Current is not SeasonEndGameOverScreen && guard++ < 10000)
            d.Press(Key.Space);
        Assert.True(game.IsGameOver);
        d.Press(Key.Space);
        // Spielende: erst die Bestenliste (Spur tabellen-highscore, em.exe 0x5642E), dann der Titel
        var highscore = Assert.IsType<EishockeyManager.UI.Screens.Statistics.HighscoreScreen>(d.Host.Current);
        Assert.Contains(highscore.Table.Entries, e => e.Name == "LetsShow" && !e.IsEmpty);
        d.Press(Key.Space);
        Assert.IsType<TitleScreen>(d.Host.Current);
        Assert.Null(d.Host.Game);
    }
}
