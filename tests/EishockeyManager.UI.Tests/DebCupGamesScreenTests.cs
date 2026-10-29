using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Competitions;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// R4-01 (#108): „Tabelle DEB-Pokal“ zeigt die Übersicht „DEB-Pokalspiele“ (em.exe Datei 0x32E23), nicht die Auslosung –
/// Paarungen der laufenden Runde nach Spielklassen, Ergebnisse der gespielten Termine.
/// </summary>
public class DebCupGamesScreenTests
{
    /// <summary>[ORIGINAL] Rahmen aus der Emulation der Funktion: 16 Paarungen mit drei leeren Gruppen → 12 Teile ab y = 4, 2 Paarungen mit vier leeren → 8 Teile ab y = 42 …</summary>
    [Theory]
    [InlineData(16, 3, 12, 4)]
    [InlineData(2, 4, 8, 42)]
    [InlineData(1, 5, 8, 42)]
    [InlineData(8, 3, 10, 23)]
    public void FrameFor_MatchesOriginal(int pairs, int emptyGroups, int parts, int y) =>
        Assert.Equal((parts, y), DebCupGamesScreen.FrameFor(pairs, emptyGroups));

    /// <summary>[ORIGINAL] Spalten der Ergebnisse: erste Spalte x = 207 − 10 · (Termine − 1), Abstand 36.</summary>
    [Fact]
    public void ResultColumns_AsInOriginal()
    {
        Assert.Equal(207, DebCupGamesScreen.ResultX(1, 0));
        Assert.Equal(197, DebCupGamesScreen.ResultX(2, 0));
        Assert.Equal(233, DebCupGamesScreen.ResultX(2, 1));
        Assert.Equal(259, DebCupGamesScreen.ResultX(3, 2));
    }

    [GameDataFact]
    public void Tile_ShowsOverviewNotDraw_WithoutChangingState()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        var before = StateFingerprint.Of(game);

        d.OpenTile("Tabelle DEB-Pokal");
        d.Frame();
        var screen = Assert.IsType<DebCupGamesScreen>(d.Host.Current);
        var overview = screen.Overview!;
        Assert.Equal(0, overview.Round);
        Assert.Equal(0, overview.DatesPlayed);
        Assert.Equal(DebCup.Teams / 2, overview.Groups.Sum(g => g.Rows.Count));
        Assert.Equal(6, overview.Groups.Count);
        Assert.Contains(overview.Groups.SelectMany(g => g.Rows), r =>
            r.LeftClub == game.ActiveManager.ClubIndex || r.RightClub == game.ActiveManager.ClubIndex);

        d.Press(Key.Escape);
        Assert.IsType<MainMenuScreen>(d.Host.Current);
        Assert.Equal(before, StateFingerprint.Of(game));
    }

    /// <summary>Regression R4-01: Ergebnisse eines gespielten Pokaltermins stehen in der Übersicht und auf dem Bildschirm.</summary>
    [GameDataFact]
    public void PlayedDate_ResultsAppearInOverview()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        for (var guard = 0; !game.DebCup!.Rounds[0].Any(t => t.Games.Count > 0); guard++)
        {
            Assert.True(guard < 40, "Kein Pokaltermin gespielt");
            d.PlayMatchDay();
        }

        d.OpenTile("Tabelle DEB-Pokal");
        d.Frame();
        var screen = Assert.IsType<DebCupGamesScreen>(d.Host.Current);
        var overview = screen.Overview!;
        Assert.Equal(1, overview.DatesPlayed);
        var rows = overview.Groups.SelectMany(g => g.Rows).ToList();
        Assert.Equal(DebCup.Teams / 2, rows.Count);
        Assert.All(rows, r => Assert.NotNull(Assert.Single(r.Results)));
        var tie = game.DebCup!.Rounds[0][0];
        var row = rows.Single(r => r.LeftClub == tie.First && r.RightClub == tie.Second);
        var match = tie.Games[0];
        Assert.Equal(match.Home == tie.First ? match.HomeGoals : match.AwayGoals, row.Results[0]!.Value.Left);

        // Auf dem Bildschirm steht je Paarung Tinte in der Ergebnisspalte (x = 207, Schriftfarbe der Spalte)
        var (parts, y) = DebCupGamesScreen.FrameFor(rows.Count, overview.Groups.Count(g => g.Rows.Count == 0));
        Assert.True(parts >= 8 && y >= 4);
        var ink = 0;
        for (var py = 0; py < Surface.Height; py++)
            for (var px = DebCupGamesScreen.ResultX(1, 0); px < DebCupGamesScreen.ResultX(1, 0) + 20; px++)
                if (d.Surface.Pixels[py * Surface.Width + px] == DebCupGamesScreen.ResultColors[0])
                    ink++;
        Assert.True(ink > rows.Count * 8, $"Zu wenig Ergebnistinte: {ink}");
    }

    /// <summary>Nach dem dritten Termin der Runde zeigt die Übersicht schon die nächste Runde ohne Ergebnisse.</summary>
    [GameDataFact]
    public void AfterRoundEnd_ShowsNextRound()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        for (var guard = 0; game.DebCup!.CurrentRound < 1; guard++)
        {
            Assert.True(guard < 80, "Zweite Runde nicht erreicht");
            d.PlayMatchDay();
        }

        d.OpenTile("Tabelle DEB-Pokal");
        d.Frame();
        var overview = Assert.IsType<DebCupGamesScreen>(d.Host.Current).Overview!;
        Assert.Equal(1, overview.Round);
        Assert.Equal(0, overview.DatesPlayed);
        Assert.Equal(DebCup.Teams / 4, overview.Groups.Sum(g => g.Rows.Count));
        Assert.All(overview.Groups.SelectMany(g => g.Rows), r => Assert.Empty(r.Results));
    }
}
