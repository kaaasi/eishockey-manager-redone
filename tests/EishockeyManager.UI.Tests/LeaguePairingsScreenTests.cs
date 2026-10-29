using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Competitions;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur paarungen-liga: die Kacheln „Paarungen 1. Liga / 2. Liga / Oberliga“ (em.exe 0x128:0x39, Datei 0x4E8A3).
/// Kein Video vorhanden – geprüft werden Inhalt aus dem Spielplan, Blättern und Zustandsfreiheit, kein Pixelvergleich.
/// </summary>
public class LeaguePairingsScreenTests
{
    private static readonly (string Tile, League League)[] Tiles =
    [
        ("Paarungen 1. Liga", League.Bundesliga),
        ("Paarungen 2. Liga", League.ZweiteLiga),
        ("Paarungen Oberliga", League.Oberliga),
    ];

    [Fact]
    public void Tiles_AreRegistered_WithoutPlaceholder()
    {
        foreach (var (tile, _) in Tiles)
        {
            Assert.True(MenuEntries.All.ContainsKey(tile), tile);
            Assert.DoesNotContain(tile, MenuEntryTests.DocumentedPlaceholders);
        }
    }

    [GameDataFact]
    public void Open_ShowsNextMatchDay_OfTheTilesLeague_WithoutChangingState()
    {
        foreach (var (tile, league) in Tiles)
        {
            var d = FlowTests.CreateDriver();
            d.StartNewGame();
            var game = d.Host.Game!;
            var before = StateFingerprint.Of(game);

            d.OpenTile(tile);
            d.Frame();
            var screen = Assert.IsType<LeaguePairingsScreen>(d.Host.Current);
            Assert.Equal(league, screen.League);
            Assert.Equal(1, screen.Day);
            Assert.Equal(game.Schedules[league][0].Matches, screen.Pairings);
            Assert.False(screen.IsPlayed);

            d.Press(Key.Escape);
            Assert.IsType<MainMenuScreen>(d.Host.Current);
            Assert.Equal(before, StateFingerprint.Of(game));
        }
    }

    /// <summary>Nach einem gespielten Spieltag: Start beim nächsten Tag, Tag 1 zeigt die echten Ergebnisse.</summary>
    [GameDataFact]
    public void AfterMatchDay_StartsAtNextDay_AndShowsPlayedResults()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        d.PlayMatchDay();
        Assert.Equal(1, game.PlayedMatchDays);

        d.OpenTile("Paarungen 1. Liga");
        d.Frame();
        var screen = Assert.IsType<LeaguePairingsScreen>(d.Host.Current);
        Assert.Equal(2, screen.Day);
        Assert.False(screen.IsPlayed);

        d.Click(LeaguePairingsScreen.Buttons[0].X + 5, LeaguePairingsScreen.Buttons[0].Y + 5);
        Assert.Equal(1, screen.Day);
        Assert.True(screen.IsPlayed);
        foreach (var (home, away) in screen.Pairings)
        {
            var result = screen.ResultOf(home, away);
            Assert.NotNull(result);
            Assert.Equal(game.Results.Single(r => r.League == League.Bundesliga && r.MatchDay == 1 && r.Home == home && r.Away == away), result);
        }
    }

    [GameDataFact]
    public void Arrows_WrapAround_AndDoNotChangeState()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        var before = StateFingerprint.Of(game);
        d.OpenTile("Paarungen Oberliga");
        var screen = Assert.IsType<LeaguePairingsScreen>(d.Host.Current);
        var count = game.Schedules[League.Oberliga].Count;
        var left = LeaguePairingsScreen.Buttons[0];
        var right = LeaguePairingsScreen.Buttons[2];

        d.Click(left.X + 5, left.Y + 5);
        Assert.Equal(count, screen.Day);
        d.Click(right.X + 5, right.Y + 5);
        Assert.Equal(1, screen.Day);
        d.Click(right.X + 5, right.Y + 5);
        Assert.Equal(2, screen.Day);
        d.Frame();
        Assert.Equal(before, StateFingerprint.Of(game));
    }

    [GameDataFact]
    public void Header_PadsDayToWidthTwo_OnlyAfterPlayedDay()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        d.OpenTile("Paarungen 1. Liga");
        var screen = Assert.IsType<LeaguePairingsScreen>(d.Host.Current);
        var date = SeasonCalendar.LayoutDate(d.Host.Game!.MatchDayDates[0]);
        Assert.Equal($"Bundesliga   1.Spieltag  {date.Day}.{date.Month}.", screen.Header(false));
        Assert.Equal($"Bundesliga   {CompetitionWidgets.FigureSpace}1.Spieltag  {date.Day}.{date.Month}.", screen.Header(true));
    }

    [GameDataFact]
    public void Draw_IsStateFree_AndRendersEveryTile()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        d.PlayMatchDay();
        var game = d.Host.Game!;
        var before = StateFingerprint.Of(game);
        foreach (var (tile, _) in Tiles)
        {
            d.OpenTile(tile);
            d.Frame();
            d.Press(Key.Escape);
        }
        Assert.Equal(before, StateFingerprint.Of(game));
    }
}
