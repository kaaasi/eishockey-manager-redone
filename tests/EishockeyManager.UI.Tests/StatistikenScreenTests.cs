using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Statistics;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur statistiken (Runde 8, #135): Statistiken → Spieler (em.exe 0x25E30) und Fairplay (0x2A1E0), Symbole der
/// Einzelprämien im Fenster „Saisonende“. Gegen Base 10eb6c5 rot: „Spieler“ und „Fairplay“ waren Platzhalter.
/// </summary>
public class StatistikenScreenTests
{
    /// <summary>Synthetische Datenbank (wie <c>NewsBarHintTests</c>), Manager bei Verein 5 der 1. Liga.</summary>
    private static GameState NewGame1()
    {
        var data = new byte[ClubDatabase.FileLength];
        for (var i = 0; i < ClubDatabase.ClubsWithSquad; i++)
        {
            System.Text.Encoding.ASCII.GetBytes($"VEREIN {i}").CopyTo(data, ClubDatabase.SquadOffset + i * ClubDatabase.SquadRecordLength);
            data[ClubDatabase.StrengthOffset + i * 3] = (byte)(100 - i % 12 * 5);
        }
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "Test", Head = 0, ClubIndex = 5, StartLeague = League.Bundesliga });
        return NewGame.Create(ClubDatabase.Parse(data), setup, 3);
    }

    private static (ScreenHost Host, PlayerStatisticsScreen Screen) Players(GameState game)
    {
        var host = new ScreenHost(null!) { Game = game };
        return (host, new PlayerStatisticsScreen(host, new MainMenuScreen(host)));
    }

    private static IEnumerable<Player> Skaters(GameState game) =>
        game.LeagueClubs(League.Bundesliga).OrderBy(c => c.Index).SelectMany(c => c.Players).Where(p => p.Role != PlayerRole.Goalkeeper);

    [Fact]
    public void Layout_TwelveSkatersAndThreeGoalies_RowsAndColors()
    {
        var game = NewGame1();
        var n = 0;
        foreach (var p in Skaters(game).Take(15))
            (p.Goals, p.Assists, p.Games) = (20 - n++, 1, 10);
        foreach (var g in game.LeagueClubs(League.Bundesliga).Select(c => c.Players.First(p => p.Role == PlayerRole.Goalkeeper)).Take(5))
            (g.GoalsAgainst, g.GoalieMinutes) = (10 + n++, 600);
        var (_, screen) = Players(game);

        var (rows, hint) = screen.Layout(game);
        Assert.False(hint);
        Assert.Equal(12, rows.Count(r => r.Block == 0));
        Assert.Equal(3, rows.Count(r => r.Block == 1));
        Assert.Equal(Enumerable.Range(0, 12).Select(i => 54 + 7 * i), rows.Where(r => r.Block == 0).Select(r => r.Baseline));
        Assert.Equal([161, 168, 175], rows.Where(r => r.Block == 1).Select(r => r.Baseline));
        Assert.All(rows, r => Assert.Equal(r.Entry.Club.Index == 5 ? Colors.Yellow : Colors.TileBackground, r.Color));
        Assert.Equal("Die besten SCORER der Liga", screen.Title(0));
        Assert.Equal("Die besten KEEPER der Liga", screen.Title(1));
    }

    [Fact]
    public void ManagerView_ShowsOnlyManagerClubs_RankStaysLeagueRank_AndHint()
    {
        var game = NewGame1();
        var rival = game.Clubs[1].Players.First(p => p.Role != PlayerRole.Goalkeeper);
        var own = game.Clubs[5].Players.First(p => p.Role != PlayerRole.Goalkeeper);
        (rival.Goals, rival.Games) = (9, 5);
        (own.Goals, own.Games) = (4, 5);
        var (host, screen) = Players(game);
        host.Show(screen);

        Click(host, PlayerStatisticsScreen.ViewButtonX[1], PlayerStatisticsScreen.ViewButtonY);
        Assert.Equal(PlayerStatisticsView.Managers, screen.View);
        Assert.Equal("Die besten SCORER der Manager", screen.Title(0));
        var (rows, hint) = screen.Layout(game);
        var row = Assert.Single(rows);
        Assert.Same(own, row.Entry.Player);
        Assert.Equal(2, row.Entry.Rank);
        Assert.Equal(54, row.Baseline);
        // [ORIGINAL] 0x269B0 prüft die Zeilenlage des zuletzt Aufgenommenen auf 54: der eigene Spieler ist der letzte und
        // steht auf Zeile 54 – der Hinweis erscheint (so im Code).
        Assert.True(hint);

        var later = game.Clubs[7].Players.First(p => p.Role != PlayerRole.Goalkeeper);
        (later.Goals, later.Assists, later.Games) = (2, 1, 5); // nach dem eigenen Spieler, in „Manager“ ausgeblendet
        (rows, hint) = screen.Layout(game);
        Assert.Same(own, Assert.Single(rows).Entry.Player);
        Assert.False(hint); // letzter Aufgenommener hätte Zeile 61

        own.Goals = 1; // nur noch ein Punkt: nicht aufgenommen
        (rows, hint) = screen.Layout(game);
        Assert.Empty(rows);
        Assert.True(hint);
    }

    [Fact]
    public void EmptyList_NoHint_AsInCode()
    {
        var game = NewGame1();
        var (_, screen) = Players(game);
        var (rows, hint) = screen.Layout(game);
        Assert.Empty(rows);
        Assert.False(hint);
    }

    [Fact]
    public void ModeButtons_SwitchSorting()
    {
        var game = NewGame1();
        var (host, screen) = Players(game);
        host.Show(screen);
        Click(host, PlayerStatisticsScreen.ModeButtonX[0], PlayerStatisticsScreen.ModeButtonY);
        Assert.Equal(SkaterRanking.Goals, screen.Mode);
        Assert.Equal("Die besten TORSCH. der Liga", screen.Title(0));
        Click(host, PlayerStatisticsScreen.ModeButtonX[1], PlayerStatisticsScreen.ModeButtonY);
        Assert.Equal(SkaterRanking.Assists, screen.Mode);
        host.Update(new InputState { RightClick = true });
        Assert.IsType<MainMenuScreen>(host.Current);
    }

    [Fact]
    public void Columns_FormattedAsInOriginal()
    {
        var game = NewGame1();
        var skater = game.Clubs[1].Players.First(p => p.Role != PlayerRole.Goalkeeper);
        (skater.Games, skater.Goals, skater.Assists) = (15, 7, 12);
        var goalie = game.Clubs[1].Players.First(p => p.Role == PlayerRole.Goalkeeper);
        (goalie.GoalsAgainst, goalie.GoalieMinutes) = (31, 845);
        var s = new PlayerRankingEntry(skater, game.Clubs[1], 1);
        var g = new PlayerRankingEntry(goalie, game.Clubs[1], 1);
        const char f = UI.Screens.Competitions.CompetitionWidgets.FigureSpace;
        Assert.Equal($"19({f}7/12)", PlayerStatisticsScreen.FirstColumn(s));
        Assert.Equal("0.46", PlayerStatisticsScreen.SecondColumn(s));
        Assert.Equal($"{f}31", PlayerStatisticsScreen.FirstColumn(g));
        Assert.Equal($"{f}845({f}2.20)", PlayerStatisticsScreen.SecondColumn(g)); // 31 · 6000 / 845 = 220
    }

    [Fact]
    public void Fairplay_TableAndPerGame()
    {
        var game = NewGame1();
        var row = new FairPlayRow(1, game.Clubs[3], 95, 8);
        const char f = UI.Screens.Competitions.CompetitionWidgets.FigureSpace;
        Assert.Equal("11.87 min.", FairplayScreen.PerGame(row));
        Assert.Equal($"{f}0.05 min.", FairplayScreen.PerGame(row with { PenaltyMinutes = 2, Games = 40 }));
        Assert.Equal(12, FairplayScreen.Table(game).Count);
    }

    /// <summary>Regressionstest: „Spieler“ und „Fairplay“ waren Platzhalter; jetzt echte Bildschirme, zustandsrein.</summary>
    [GameDataFact]
    public void Tiles_OpenRealScreens_StateUnchanged()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        for (var i = 0; i < 6; i++)
            d.PlayMatchDay();
        if (d.Host.Current is not MainMenuScreen)
            d.Host.Show(new MainMenuScreen(d.Host));
        var before = StateFingerprint.Of(game);

        d.OpenTile("Spieler");
        d.Frame();
        var players = Assert.IsType<PlayerStatisticsScreen>(d.Host.Current);
        d.Click(PlayerStatisticsScreen.ModeButtonX[0] + 5, PlayerStatisticsScreen.ModeButtonY + 5);
        d.Click(PlayerStatisticsScreen.ViewButtonX[1] + 5, PlayerStatisticsScreen.ViewButtonY + 5);
        Assert.Equal(SkaterRanking.Goals, players.Mode);
        FlowTests.ReturnToMainMenu(d);

        d.OpenTile("Fairplay");
        d.Frame();
        Assert.IsType<FairplayScreen>(d.Host.Current);
        Assert.True(FairplayScreen.Table(game).Sum(r => r.PenaltyMinutes) > 0);
        FlowTests.ReturnToMainMenu(d);

        Assert.Equal(before, StateFingerprint.Of(game));
    }

    /// <summary>[ORIGINAL] Datei 0x2718A/0x271D0/0x2721D: Symbole 12, 3, 13 nach den Statusbits und Symbol 8.</summary>
    [Fact]
    public void SeasonEnd_SinglePremiumIcons()
    {
        Assert.Equal(SeasonEndSummaryScreen.Symbol(12), SeasonEndSummaryScreen.EventIcon(SeasonEvent.BestGoalie));
        Assert.Equal(SeasonEndSummaryScreen.Symbol(3), SeasonEndSummaryScreen.EventIcon(SeasonEvent.TopScorer));
        Assert.Equal(SeasonEndSummaryScreen.Symbol(13), SeasonEndSummaryScreen.EventIcon(SeasonEvent.FairPlay));
        Assert.True(SeasonEndSummaryScreen.IsSinglePremium(SeasonEvent.FairPlay));
        Assert.False(SeasonEndSummaryScreen.IsSinglePremium(SeasonEvent.CupWinner));
    }

    private static void Click(ScreenHost host, int x, int y) =>
        host.Update(new InputState { MouseX = x + 5, MouseY = y + 5, LeftClick = true });
}
