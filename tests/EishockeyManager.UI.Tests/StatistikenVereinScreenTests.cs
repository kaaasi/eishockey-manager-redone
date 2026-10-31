using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Competitions;
using EishockeyManager.UI.Screens.Statistics;
using EishockeyManager.UI.Screens.Team;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur statistiken-verein (Runde 8, Welle 3): Bildschirm „Verein“ (em.exe 0x2D5AE), Knöpfe der Ranglisten (0x4BFF2) und
/// Spielerdaten-Fenster aus Ranglisten (0x26B13) und Aufstellung (0x400E9). Gegen Base 99fa995 rot: „Verein“ war ein
/// Platzhalter, die Knöpfe zeigten „Liga“/„Manager“ und waren gedrückt, kein Aufruf des Spielerdaten-Fensters.
/// </summary>
public class StatistikenVereinScreenTests
{
    private const char F = CompetitionWidgets.FigureSpace;

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

    /// <summary>[ORIGINAL] 0x0B954 mit Mindestbreite und Tausenderpunkt (Video: „2.624“, „182.491 DM“, „  0 DM“).</summary>
    [Fact]
    public void Number_LikeTheOriginal()
    {
        Assert.Equal($"{F}{F}0", ClubStatisticsScreen.Number(0, 3, true));
        Assert.Equal("2.624", ClubStatisticsScreen.Number(2624, 3, true));
        Assert.Equal("182.491", ClubStatisticsScreen.Number(182_491, 3, true));
        Assert.Equal($"{F}{F}42.637", ClubStatisticsScreen.Number(42_637, 8, true));
        Assert.Equal("1.000.000", ClubStatisticsScreen.Number(1_000_000, 3, true));
        Assert.Equal("1000", ClubStatisticsScreen.Number(1000, 3, false));
        Assert.Equal($"{F}35", ClubStatisticsScreen.Number(35, 3, false));
        Assert.Equal("-1.500", ClubStatisticsScreen.Number(-1500, 8, true));
    }

    /// <summary>[ORIGINAL] 0x2D57A und 0x2ECA0 (Video: „–( –)“, „1( 1)“, „4.3“).</summary>
    [Fact]
    public void Cells_SeriesAndGoals()
    {
        Assert.Equal($"{F}-({F}-)", ClubStatisticsScreen.SeriesCell(0, 0));
        Assert.Equal($"{F}1({F}1)", ClubStatisticsScreen.SeriesCell(1, 1));
        Assert.Equal($"12({F}-)", ClubStatisticsScreen.SeriesCell(12, 0));
        Assert.Equal("  4.3", ClubStatisticsScreen.GoalsCell(43));
        Assert.Equal("  0.0", ClubStatisticsScreen.GoalsCell(0));
        Assert.Equal("  10.5", ClubStatisticsScreen.GoalsCell(105));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(99, 9)]
    [InlineData(100, 10)]
    [InlineData(1500, 38)]
    [InlineData(16000, 126)]
    public void IntegerSqrt_LikeTheOriginal(int x, int root) => Assert.Equal(root, ClubStatisticsScreen.IntegerSqrt(x));

    /// <summary>Kontobalken: Video folge01_00522 zeigt bei 0 DM 35 Pixel in sechs Farbstufen ab Rot (Farbe 16).</summary>
    [Fact]
    public void AccountBar_ZeroMoney_35Pixels_SixSteps()
    {
        var bar = ClubStatisticsScreen.AccountBar(0);
        Assert.Equal(35, bar.Count);
        Assert.Equal(Enumerable.Range(223, 35), bar.Select(b => b.X));
        Assert.Equal([16, 17, 18, 10, 11, 8], bar.Select(b => b.Color).Distinct());
        Assert.All(bar.Take(6), b => Assert.Equal(16, b.Color));
        Assert.Equal(71, ClubStatisticsScreen.AccountBar(3_000_000).Count);
        Assert.Empty(ClubStatisticsScreen.AccountBar(-3_000_000));
    }

    [Fact]
    public void DebtBar_GrowsWithDebtsAndNegativeAccount()
    {
        Assert.Empty(ClubStatisticsScreen.DebtBar(0, 0));
        Assert.Single(ClubStatisticsScreen.DebtBar(1, 0));
        var bar = ClubStatisticsScreen.DebtBar(0, -100_000); // 100.000 Drittel: Schwellen 0, 25.000, 50.000, 75.000
        Assert.Equal(4, bar.Count);
        Assert.All(bar, b => Assert.Equal(28, b.Color));
    }

    [Fact]
    public void AudienceBars_ScaleAroundRequiredAverage()
    {
        Assert.Empty(ClubStatisticsScreen.AudienceBars([], 0, 1514));
        // k = 1, Werte 4 und 0: Streuung √((9 + 1) / 2) = 2 → 10; T = 1 + 10 = 11, r = 10.
        var bars = ClubStatisticsScreen.AudienceBars([4, 0], 2, 1514);
        Assert.Equal([(227, 143 - (3 * 19 / 10 + 19)), (230, 143 - (-1 * 19 / 10 + 19))], bars);
        // Werte weit über k: r = Max − k, der größte Balken erreicht 2 · 19 = 38 → 37.
        var high = ClubStatisticsScreen.AudienceBars([50], 1, 0);
        Assert.Equal(143 - 37, high[0].Top);
    }

    /// <summary>[ORIGINAL] Zeilenlauf 0x2D6CC–0x2DE7C: Überschriften und Grundlinien.</summary>
    [Fact]
    public void Layout_HeadingsAndBaselines()
    {
        var game = NewGame1();
        var items = ClubStatisticsScreen.Layout(game, game.ActiveManager);
        var headings = items.Where(i => i.Heading).Select(i => (i.Text, i.X, i.Baseline));
        Assert.Equal(
        [
            ("!Serien", 19, 22), ("!überzahl/Unterzahl", 19, 111), ("!Zuschauer", 217, 19), ("!Finanzen", 217, 159),
            ("!Einnahmen,Ausgaben / Monat", 19, 213),
        ], headings);
        Assert.Contains(items, i => i.Text == "GEWONNEN" && i.Baseline == 33 && i.Color == 1);
        Assert.Contains(items, i => i.Text == "GEGENTORE/SPIEL" && i.Baseline == 97);
        Assert.Contains(items, i => i.Text == "UNTERZAHL ERFOLG" && i.Baseline == 170);
        Assert.Contains(items, i => i.Text == $"{F}{F}0#" && i.X == 135 && i.Baseline == 162);
        Assert.Contains(items, i => i.Text == "100#" && i.Baseline == 170);
        Assert.Contains(items, i => i.Text == ClubStatisticsScreen.NoneMasculine && i.X == 217 && i.Baseline == 36);
        Assert.Contains(items, i => i.Text == ClubStatisticsScreen.NoneFeminine && i.Baseline == 88);
        Assert.Contains(items, i => i.Text == "KONTOSTAND" && i.Baseline == 170);
        Assert.Contains(items, i => i.Text == "SCHULDEN GESAMT" && i.Baseline == 197);
        Assert.Contains(items, i => i.Text == $"{F}{F}0 DM" && i.Baseline == 203);
        Assert.Contains(items, i => i.Text.EndsWith(" DM") && i.X == 149 && i.Baseline == 224);
        Assert.Contains(items, i => i.Text.EndsWith(" DM") && i.X == 149 && i.Baseline == 231);
        Assert.Equal(7 * 3 + 2 * 3, items.Count(i => i.X is 99 or 133 or 167));
    }

    [Fact]
    public void Layout_ShowsSeriesRecordsAndAudience()
    {
        var game = NewGame1();
        var manager = game.ActiveManager;
        var club = game.Clubs[manager.ClubIndex];
        club.SeriesCurrent[Series.Index(SeriesKind.Won, SeriesColumn.Home)] = 2;
        manager.SeriesRecords[Series.Index(SeriesKind.Won, SeriesColumn.Home)] = 3;
        var stats = game.Finance(manager).HomeStatistics;
        stats.Add(4335, 2);
        stats.Add(914, 3);
        var items = ClubStatisticsScreen.Layout(game, manager);
        Assert.Contains(items, i => i.Text == $"{F}2({F}3)" && i.X == 133 && i.Baseline == 33);
        Assert.Contains(items, i => i.Text == "2.624" && i.Baseline == 36);
        Assert.Contains(items, i => i.Text == "4.335" && i.Baseline == 66);
        Assert.Contains(items, i => i.Text == "(" + game.Clubs[2].Name + ")" && i.Baseline == 72);
        Assert.Contains(items, i => i.Text == "914" && i.Baseline == 88);
        Assert.Contains(items, i => i.Text == "(" + game.Clubs[3].Name + ")" && i.Baseline == 94);
    }

    /// <summary>[ORIGINAL] 0x2D666: Grün 0x0C0 auf Index 28, Rot 0xD00 auf Index 16.</summary>
    [Fact]
    public void GradientPalette_GreenToRed()
    {
        var palette = ClubStatisticsScreen.GradientPalette(Palette.Grayscale());
        Assert.Equal((0, Palette.DacToRgb(48), 0), ((int)palette[28].R, (int)palette[28].G, (int)palette[28].B));
        Assert.Equal((Palette.DacToRgb(52), 0, 0), (palette[16].R, (int)palette[16].G, (int)palette[16].B));
    }

    /// <summary>[ORIGINAL] 0x4BFF2 und DGROUP 0x974/0x978.</summary>
    [Fact]
    public void RankingButtons_Uppercase()
    {
        Assert.Equal(["LIGA", "MANAGER"], PlayerStatisticsScreen.ViewButtonText);
    }

    /// <summary>[ORIGINAL] 0x26B13–0x26BF5: Klick auf einen Spieler eines Managerkaders öffnet das Spielerdaten-Fenster.</summary>
    [Fact]
    public void Ranking_ClickOnManagerPlayer_OpensPlayerData_OthersNot()
    {
        var game = NewGame1();
        var own = game.Clubs[5].Players.First(p => p.Role != PlayerRole.Goalkeeper);
        var rival = game.Clubs[1].Players.First(p => p.Role != PlayerRole.Goalkeeper);
        (rival.Goals, rival.Games) = (9, 5);
        (own.Goals, own.Games) = (4, 5);
        var host = new ScreenHost(null!) { Game = game };
        var screen = new PlayerStatisticsScreen(host, new MainMenuScreen(host));
        host.Show(screen);

        host.Update(new InputState { MouseX = 60, MouseY = 54, LeftClick = true }); // Zeile 1: fremder Spieler
        Assert.False(screen.Info.Visible);
        host.Update(new InputState { MouseX = 60, MouseY = 61, LeftClick = true }); // Zeile 2: eigener Spieler
        Assert.True(screen.Info.Visible);
        Assert.Same(own, screen.Info.Player);
        host.Update(new InputState { RightClick = true }); // schließt nur das Fenster
        Assert.False(screen.Info.Visible);
        Assert.Same(screen, host.Current);
    }

    /// <summary>[ORIGINAL] 0x400E9–0x4010D: rechte Maustaste über der Namensspalte der Aufstellung.</summary>
    [Fact]
    public void Lineup_RightClickOnName_OpensPlayerData()
    {
        var game = NewGame1();
        var host = new ScreenHost(null!) { Game = game };
        var club = game.Clubs[5];
        var lineup = new LineupScreen(host, new MainMenuScreen(host));
        host.Show(lineup);
        var rows = TeamUi.Rows(club);
        var dressed = club.IsDressed(rows[1]);
        host.Update(new InputState { MouseX = 50, MouseY = LineupScreen.FirstRowY + 6 + 2, RightClick = true });
        Assert.True(lineup.Info.Visible);
        Assert.Same(rows[1], lineup.Info.Player);
        Assert.Equal(dressed, club.IsDressed(rows[1])); // kein Einsetzen
    }

    /// <summary>Bildschirm „Verein“ über die Kachel; Öffnen und Zeichnen ändern nichts.</summary>
    [GameDataFact]
    public void Tile_OpensClubStatistics_StateUnchanged()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        for (var i = 0; i < 3; i++)
            d.PlayMatchDay();
        if (d.Host.Current is not MainMenuScreen)
            d.Host.Show(new MainMenuScreen(d.Host));
        var before = StateFingerprint.Of(game);
        d.OpenTile("Verein");
        d.Frame();
        Assert.IsType<ClubStatisticsScreen>(d.Host.Current);
        Assert.Contains(game.Clubs[game.ActiveManager.ClubIndex].SeriesCurrent, v => v > 0);
        FlowTests.ReturnToMainMenu(d);
        Assert.Equal(before, StateFingerprint.Of(game));
    }
}
