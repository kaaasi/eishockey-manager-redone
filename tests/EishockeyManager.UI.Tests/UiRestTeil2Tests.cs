using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Team;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur ui-rest (Runde 17), Teil 2: Ereignissymbole im Fenster „Saisonende“ (Raster und Rahmen 0x06CA6/0x06CDF, Symbol 9 der
/// Jugend, Datei 0x27302) und der Hintergrund der Verhandlungen am Saisonende (Datei 0x27BDB–0x27CBD, DOSBox-Bild <c>file46_3</c>).
/// </summary>
public class UiRestTeil2Tests
{
    private static byte At(Surface s, int x, int y) => s.Pixels[y * Surface.Width + x];

    /// <summary>Anzahl Pixel im Ziel bei (x | y), die von der Quelle in kb\13 abweichen.</summary>
    private static int Differing(Surface s, VgaImage sheet, Rect source, int x, int y, Rect? skip = null)
    {
        var differing = 0;
        for (var yy = 0; yy < source.H; yy++)
            for (var xx = 0; xx < source.W; xx++)
            {
                if (skip is { } k && k.Contains(xx, yy))
                    continue;
                if (At(s, x + xx, y + yy) != sheet.Pixels[(source.Y + yy) * sheet.Width + source.X + xx])
                    differing++;
            }
        return differing;
    }

    /// <summary>[ORIGINAL] Datei 0x06CA6–0x06CC7: Start (76 | 101), x + 35, über 230 neue Zeile (x = 76, y + 30).</summary>
    [Theory]
    [InlineData(0, 76, 101)]
    [InlineData(1, 111, 101)]
    [InlineData(4, 216, 101)]
    [InlineData(5, 76, 131)]
    [InlineData(9, 216, 131)]
    [InlineData(10, 76, 161)]
    public void Position_FiveSymbolsPerRow_LeftAligned(int index, int x, int y) =>
        Assert.Equal((x, y), EventSymbols.Position(index));

    /// <summary>Symbol 9 (Jugend) und 10 (Vertrag) im Raster mit sechs Zellen je Zeile (Datei 0x06D21–0x06D35).</summary>
    [Fact]
    public void Source_YouthAndContract()
    {
        Assert.Equal(new Rect(72, 21, 24, 21), EventSymbols.Source(EventSymbols.Youth));
        Assert.Equal(new Rect(96, 21, 24, 21), EventSymbols.Source(EventSymbols.Contract));
        Assert.Equal(SeasonEndSummaryScreen.Symbol(9), EventSymbols.Source(9));
    }

    /// <summary>
    /// Fenster 13 × 6 Teile bei (4 | 80) ([ORIGINAL] Datei 0x26DE6–0x26E04) und Platzzahl mit Mindestbreite 5 (Zahlenausgabe 0x0BAB2,
    /// DGROUP 0x374; <c>file46_2</c> „10. Platz der Bundesliga“ pixelgleich).
    /// </summary>
    [Fact]
    public void Window_AndPlaceText()
    {
        Assert.Equal(new Rect(4, 80, 312, 114), SeasonEndSummaryScreen.Window);
        var fig = Screens.Competitions.CompetitionWidgets.FigureSpace;
        Assert.Equal($"{new string(fig, 3)}10. Platz der Bundesliga",
            SeasonEndSummaryScreen.PlaceText(new ManagerSeasonResult(0, 5, League.Bundesliga, 10, 100_000, [])));
        // [ORIGINAL] DGROUP 0x9EC: „Zweite Liga“ (file16_9), nicht „Zweiten Liga“.
        Assert.Equal($"{new string(fig, 4)}1. Platz der Zweite Liga",
            SeasonEndSummaryScreen.PlaceText(new ManagerSeasonResult(0, 5, League.ZweiteLiga, 1, 0, [])));
    }

    /// <summary>
    /// [ORIGINAL] Zentrieren 0x0B6A5 (Datei 0x0B6D9–0x0B6EF): x = x1 + (x2 − x1) / 2 − Breite / 2; bei ungerader Breite und geradem
    /// Abstand 1 Pixel weiter rechts als (x2 − x1 − Breite) / 2 („Erhaltene Prämien: 100.000 DM“ in <c>file46_2</c>).
    /// </summary>
    [GameDataFact]
    public void Centered_RoundsHalvesSeparately()
    {
        var d = FlowTests.CreateDriver();
        var flow = d.Host.Assets.Flow;
        var text = Enumerable.Range(1, 40).Select(n => new string('l', n)).First(t => Screens.Competitions.CompetitionWidgets.Width(flow, t) % 2 == 1);
        var width = Screens.Competitions.CompetitionWidgets.Width(flow, text);
        var s = new Surface();
        Screens.Competitions.CompetitionWidgets.Centered(s, flow, 0, 320, 50, text, Colors.White, null);
        var expected = new Surface();
        Screens.Competitions.CompetitionWidgets.Left(expected, flow, 160 - width / 2, 50, text, Colors.White);
        Assert.True(s.Pixels.AsSpan().SequenceEqual(expected.Pixels));
    }

    /// <summary>
    /// A332, Lampe 1: rot, solange zu einem eigenen Spieler eine offene Meldung vorliegt ([ORIGINAL] Pool +0x1E, Datei 0x42AC9–0x42BD2);
    /// grün nach dem Löschen (0x4B270) und bei einer Meldung zu einem fremden Spieler. Das Prüfen legt keine Finanzdaten an.
    /// </summary>
    [GameDataFact]
    public void Lamp1_RedWhileOwnPlayerHasOpenMessage()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        var manager = game.ActiveManager;
        var club = game.Clubs[manager.ClubIndex];
        Assert.False(MenuLamps.Red(game)[1]);

        var foreign = game.Clubs.First(c => c != club && c.Players.Count > 0).Players[0];
        Assert.NotNull(game.NotifyAbout(manager, foreign, "fremd"));
        Assert.False(MenuLamps.Red(game)[1]);

        var own = club.Players[0];
        Assert.NotNull(game.NotifyAbout(manager, own, "eigen"));
        Assert.True(MenuLamps.Red(game)[1]);
        game.RemoveMessagesOf(own);
        Assert.False(MenuLamps.Red(game)[1]);

        // Ohne Finanzdaten eines Managers: Prüfen legt sie nicht an.
        var other = game.Managers.FirstOrDefault(m => m != manager && m.FinanceData is null);
        if (other is not null)
        {
            MenuLamps.OpenMessage(game, other, game.Clubs[other.ClubIndex]);
            Assert.Null(other.FinanceData);
        }
    }

    private static (ScreenDriver Driver, GameState Game) AtSeasonEndSummary()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        for (var guard = 0; d.Host.Current is not SeasonEndSummaryScreen && guard++ < 20000;)
            d.Advance();
        Assert.IsType<SeasonEndSummaryScreen>(d.Host.Current);
        return (d, d.Host.Game!);
    }

    /// <summary>
    /// Je Jugendspieler ein Symbol 9 nach allen übrigen Symbolen ([ORIGINAL] Datei 0x27302–0x2730F nach den Einzelprämien
    /// 0x2717F–0x2725D). Gegenprobe: ohne Jugend bleibt die Liste wie vorher.
    /// </summary>
    [GameDataFact]
    public void Icons_YouthSymbolsComeLast()
    {
        var (_, game) = AtSeasonEndSummary();
        var record = game.History[^1];
        var result = record.Managers[0];
        var without = SeasonEndSummaryScreen.Icons(game, record, result, 0);
        var with = SeasonEndSummaryScreen.Icons(game, record, result, 2);
        Assert.Equal(without.Count + 2, with.Count);
        Assert.Equal(without, with.Take(without.Count));
        Assert.All(with.Skip(without.Count), r => Assert.Equal(EventSymbols.Source(EventSymbols.Youth), r));
    }

    /// <summary>
    /// Das Fenster zeichnet jedes Symbol mit dem Rahmen aus kb\13 (144 | 0) an der Rasterstelle, das Symbol 3 Pixel versetzt
    /// ([ORIGINAL] 0x06CDF; <c>file16_9</c>: drei Symbole ab x = 76, y = 101).
    /// </summary>
    [GameDataFact]
    public void SummaryWindow_DrawsFramedSymbolsOnGrid()
    {
        var (d, game) = AtSeasonEndSummary();
        var record = game.History[^1];
        var result = record.Managers[0];
        // Mindestens ein Symbol: Verein auf Platz 1 der 1. Liga (Statusbit 6, Symbol 8).
        var tables = (IDictionary<League, IReadOnlyList<int>>)record.FinalTables;
        tables[League.Bundesliga] = [result.Club, .. tables[League.Bundesliga].Where(c => c != result.Club)];
        d.Frame();
        var icons = SeasonEndSummaryScreen.Icons(game, record, result, 0);
        Assert.NotEmpty(icons);
        var sheet = d.Host.Assets.Image(EventSymbols.Sheet);
        var inner = new Rect(EventSymbols.Inset, EventSymbols.Inset, EventSymbols.SymbolWidth, EventSymbols.SymbolHeight);
        for (var i = 0; i < icons.Count; i++)
        {
            var (x, y) = EventSymbols.Position(i);
            Assert.Equal(0, Differing(d.Surface, sheet, EventSymbols.Frame, x, y, inner));
            Assert.Equal(0, Differing(d.Surface, sheet, icons[i], x + EventSymbols.Inset, y + EventSymbols.Inset));
        }
    }

    private static ContractsScreen SeasonEndContracts(ScreenDriver d, GameState game)
    {
        var menu = new MainMenuScreen(d.Host);
        foreach (var m in game.ActiveManagers)
            game.Clubs[m.ClubIndex].Players.First(x => x.Role == PlayerRole.Forward && !x.OnLoan).ContractYears = 1;
        game.StartNextSeasonWithRenewals();
        var screen = Assert.IsType<ContractsScreen>(ContractsScreen.ForSeasonEnd(d.Host, () => menu));
        d.Host.Show(screen);
        d.Frame();
        return screen;
    }

    /// <summary>
    /// Hintergrund der Verhandlungen am Saisonende: Vereinsname oben (Schrift 2, Farbe 0x1E, Grundlinie 10), Kachel 2 × 2 Teile
    /// bei (265 | 195) und Symbol 10 mit Rahmen bei (274 | 200) ([ORIGINAL] Datei 0x27BF3–0x27CB8; <c>file46_3</c>).
    /// Gegenprobe: der Bildschirm „Verträge“ aus dem Mannschaftsmenü zeigt beides nicht (<c>file40_11</c>).
    /// </summary>
    [GameDataFact]
    public void SeasonEndContracts_ClubNameAndContractTile()
    {
        var (d, game) = AtSeasonEndSummary();
        var screen = SeasonEndContracts(d, game);
        Assert.True(screen.SeasonEnd);
        var sheet = d.Host.Assets.Image(EventSymbols.Sheet);
        var inner = new Rect(EventSymbols.Inset, EventSymbols.Inset, EventSymbols.SymbolWidth, EventSymbols.SymbolHeight);
        Assert.Equal(0, Differing(d.Surface, sheet, EventSymbols.Frame, ContractsScreen.SeasonEndSymbolX, ContractsScreen.SeasonEndSymbolY, inner));
        Assert.Equal(0, Differing(d.Surface, sheet, EventSymbols.Source(EventSymbols.Contract),
            ContractsScreen.SeasonEndSymbolX + EventSymbols.Inset, ContractsScreen.SeasonEndSymbolY + EventSymbols.Inset));

        // Vereinsname: Erwartung wie der Text des Originals (0x0B6A5) auf dem Hintergrund kb\22.
        var expected = new Surface();
        expected.Blit(d.Host.Assets.Image(MainMenuScreen.Background), 0, 0);
        var club = game.Clubs[game.ActiveManager.ClubIndex];
        Screens.Competitions.CompetitionWidgets.Centered(expected, d.Host.Assets.Flow, 0, 319, 10, club.Name, 0x1E, 0);
        var heading = Enumerable.Range(0, 13 * Surface.Width).Count(i => d.Surface.Pixels[i] == 0x1E);
        Assert.True(heading > 20, $"Vereinsname fehlt ({heading} Pixel in Farbe 0x1E)");
        Assert.True(Enumerable.Range(0, 13 * Surface.Width).All(i => d.Surface.Pixels[i] == expected.Pixels[i]));

        // Gegenprobe „Verträge“ aus dem Menü
        d.Host.Show(new ContractsScreen(d.Host, new MainMenuScreen(d.Host)));
        d.Frame();
        Assert.NotEqual(0, Differing(d.Surface, sheet, EventSymbols.Source(EventSymbols.Contract),
            ContractsScreen.SeasonEndSymbolX + EventSymbols.Inset, ContractsScreen.SeasonEndSymbolY + EventSymbols.Inset));
        Assert.DoesNotContain(Enumerable.Range(0, 13 * Surface.Width), i => d.Surface.Pixels[i] == 0x1E);
    }
}
