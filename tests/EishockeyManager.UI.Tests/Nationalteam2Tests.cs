using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Competitions;
using EishockeyManager.UI.Screens.Team;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur nationalteam-2 (Runde 8, Welle 3). Bildnamen beziehen sich auf `dosbox/nur-weltkugel/` im Referenz-Repo.
/// </summary>
public class Nationalteam2Tests
{
    /// <summary>Spielvorbereitung mit einem Manager im Modus „nur Weltkugel“ bis „Los jetzt!“.</summary>
    internal static ScreenDriver StartWorldOnlyThroughSetup()
    {
        var d = FlowTests.CreateDriver();
        d.Host.Show(new TitleScreen(d.Host));
        d.Frame();
        d.Click(160, 120);
        Assert.IsType<SetupScreen>(d.Host.Current);
        d.Click(20, 60);
        var cell = SetupScreen.HeadCell(3);
        d.Click(cell.X + 5, cell.Y + 5);
        d.Type("LetsShow");
        d.Press(Key.Enter);
        d.Click(SetupScreen.ModeFlag.X + 5, SetupScreen.ModeFlag.Y + 5); // Fahne aus → nur Weltkugel
        d.Click(SetupScreen.StartButton.X + 5, SetupScreen.StartButton.Y + 5);
        return d;
    }

    /// <summary>
    /// #195, DOSBox <c>06_hauptmenue_bundestrainer_14_april.png</c>: Nach „Los jetzt!“ hält das Original im Modus 1 sofort am
    /// Samstag, 14. April, im Menü des Bundestrainers (Gegner Russland, Konto 500.000 DM). [ORIGINAL] Der Halt liegt in der
    /// Tagesfunktion Datei 0x54C9F: Sie springt am ersten Tag (0x54CEE) zum WM-Termin, ernennt den Bundestrainer (0xD0BA)
    /// und ruft am Tagesende das Hauptmenü 0x44E22 für Manager 5 (0x555A3, `call 0x110:0x66`) – ein Menü am 10. Sept.
    /// davor gibt es nicht. Vorher zeigte das Remake das Menü am Tag 0; gesprungen wurde erst mit „und tschüß“.
    /// </summary>
    [GameDataFact]
    public void Mode1_FirstHaltAfterStart_IsFirstWorldCupDay()
    {
        var d = StartWorldOnlyThroughSetup();
        Assert.IsType<MainMenuScreen>(d.Host.Current);
        var game = d.Host.Game!;
        Assert.Equal(GameMode.WorldOnly, game.Mode);
        Assert.Equal(0, game.PlayedMatchDays);
        Assert.Equal(GameState.FirstWorldCupDay, game.CurrentDay);
        Assert.NotNull(game.NationalCoach);
        Assert.True(game.NationalCoachMenu);
        Assert.Equal(500_000, game.Managers[0].Money);
    }
    /// <summary>
    /// R8W3-06: Die Hinweiszeile der Nationalaufstellung ist die Hinweiszeile 0x41683 der Vereinsaufstellung (gleicher
    /// Bildschirm Datei 0x40364, Aufruf 0x40845). Vorher stand dort eine genäherte Liste („Spiele gesamt (Liga und Pokal)“
    /// usw.) ohne Reihe, Erschöpfung und Stärke. Geprüft wird das Band der Statuszeile gegen <see cref="PlayerHintLine"/>
    /// für die Spalten NR, Art, Stärke, Status und Tendenz; Überfahren ändert nichts.
    /// </summary>
    [GameDataFact]
    public void NationalLineup_HintLine_IsPlayerHintLine()
    {
        var (d, game) = NationalLineupScreenTests.StartWorldOnly();
        game.AutoNominate();
        var coach = game.NationalCoach!;
        coach.AutoFormation();
        d.OpenTile("Aufstellung");
        Assert.IsType<NationalLineupScreen>(d.Host.Current);
        var before = StateFingerprint.Of(game);
        var rows = coach.Rows;
        var layout = PlayerListLayout.Lineup(rows.Count);
        var row = rows.ToList().FindIndex(p => coach.Formation.Contains(p));
        Assert.True(row >= 0);
        var y = LineupScreen.FirstRowY + row * LineupScreen.RowPitch + 2;
        var checkedColumns = 0;
        foreach (var column in new[] { 0, 1, 7, 14, 15 })
        {
            var x = Enumerable.Range(0, 240).First(x => PlayerHintLine.Column(layout, x) == column);
            Assert.Equal(row, PlayerHintLine.Row(layout, y));
            var expected = PlayerHintLine.Text(rows[row], column, new PlayerHintInfo(coach.IsDressed(rows[row]), Line(coach, rows[row])), true);
            Assert.NotNull(expected);
            d.Move(x, y);
            var actual = Band(d.Surface);
            d.Move(0, 0);
            var reference = new Surface();
            Array.Copy(d.Surface.Pixels, reference.Pixels, reference.Pixels.Length);
            TeamUi.StatusLine(reference, d.Host.Assets, 131, LineupScreen.StatusY, expected);
            Assert.True(actual.SequenceEqual(Band(reference)), $"Spalte {column}: „{expected}“");
            checkedColumns++;
        }
        Assert.Equal(5, checkedColumns);
        Assert.Equal(before, StateFingerprint.Of(game));
    }

    private static int Line(NationalCoach coach, Core.Player p) =>
        LineupSlot.All.Where(s => coach.Formation[s] == p).Select(s => s.Block + 1).FirstOrDefault();

    /// <summary>Pixel der Statuszeile (y = StatusY − 8 … StatusY + 8, ganze Breite).</summary>
    private static byte[] Band(Surface s) =>
        s.Pixels.Skip((LineupScreen.StatusY - 8) * Surface.Width).Take(17 * Surface.Width).ToArray();
    /// <summary>
    /// #194, DOSBox <c>20_paarungen_wm_gruppe1_spieltag1.png</c>, <c>21_…spieltag2.png</c>, <c>24_paarungen_wm_endrunde_vor_den_spielen.png</c>:
    /// Paarungen → WM öffnet die Paarungsansicht „Gruppe 1   1.Spieltag“ (em.exe 0x2FC2F, Seite 8) mit den drei Begegnungen
    /// in Auslosungsfolge „abcdef“ und 0:0; Pfeile blättern 1 … 5 mit Umlauf, „Gruppe 2“ wechselt, „Endrunde“ zeigt „Die
    /// Paarungen stehen noch nicht fest...“, dessen „Gruppe“ zurück zur Paarungsansicht führt. Vorher öffnete die Kachel die
    /// Gruppentabelle (wie Tabellen → WM), die Paarungsansicht fehlte. Alles zustandsrein.
    /// </summary>
    [GameDataFact]
    public void PairingsTile_ShowsMatchDayView_LikeDosbox()
    {
        var d = StartWorldOnlyThroughSetup();
        var game = d.Host.Game!;
        var before = StateFingerprint.Of(game);
        d.OpenTile("WM");
        var screen = Assert.IsType<CupPairingsScreen>(d.Host.Current);
        Assert.Equal("Gruppe 1   1.Spieltag", screen.Title);
        Assert.Equal("Gruppe 2", screen.GroupButtonText);
        var group = game.WorldCup!.Groups[0];
        Assert.Equal([(group.Teams[0], group.Teams[1]), (group.Teams[2], group.Teams[3]), (group.Teams[4], group.Teams[5])],
            screen.Rows.Select(r => (r.Home, r.Away)));
        Assert.All(screen.Rows, r => Assert.Equal((0, 0), (r.HomeGoals, r.AwayGoals)));

        // Flaggen pixelgenau bei (x + 2 | y + 2), Umriss Farbe 7 bzw. 18 für Deutschland mit Bundestrainer
        d.Frame();
        for (var k = 0; k < 3; k++)
            for (var side = 0; side < 2; side++)
            {
                var club = side == 0 ? screen.Rows[k].Home : screen.Rows[k].Away;
                var x = CupPairingsScreen.BoxX[side];
                var y = CupPairingsScreen.FirstBoxY + CupPairingsScreen.BoxPitch * k;
                var logo = d.Host.Assets.Image(game.Clubs[club].LogoGraphic!.Value);
                for (var py = 0; py < logo.Height; py++)
                    for (var px = 0; px < logo.Width; px++)
                        Assert.Equal(logo.Pixels[py * logo.Width + px], d.Surface.Pixels[(y + 2 + py) * Surface.Width + x + 2 + px]);
                Assert.Equal(club == NationalTeams.Germany ? Colors.Red : Colors.White, d.Surface.Pixels[y * Surface.Width + x + 20]);
                Assert.Equal(4, d.Surface.Pixels[(y + 1) * Surface.Width + x + 20]);
            }

        Assert.False(ShowsTrophy(d)); // Bundestrainer am Zug: keine Pokale (DOSBox 20, Datei 0x2FE15)

        Click(d, CupPairingsScreen.Buttons[3]);
        Assert.Equal("Gruppe 1   2.Spieltag", screen.Title); // DOSBox 21
        Assert.Equal(group.Pairings(1).Select(p => (p.Home, p.Away)), screen.Rows.Select(r => (r.Home, r.Away)));
        Click(d, CupPairingsScreen.Buttons[0]);
        Click(d, CupPairingsScreen.Buttons[0]);
        Assert.Equal(4, screen.Day); // vor 1 → 5
        Click(d, CupPairingsScreen.Buttons[3]);
        Assert.Equal(0, screen.Day); // nach 5 → 1

        Click(d, CupPairingsScreen.Buttons[2]);
        var other = Assert.IsType<CupPairingsScreen>(d.Host.Current);
        Assert.Equal("Gruppe 2   1.Spieltag", other.Title);
        Assert.Equal("Gruppe 1", other.GroupButtonText);

        Click(d, CupPairingsScreen.Buttons[1]);
        var knockout = Assert.IsType<WorldCupKnockoutScreen>(d.Host.Current); // DOSBox 24
        Assert.Empty(knockout.Sections);
        Click(d, WorldCupKnockoutScreen.Button);
        var back = Assert.IsType<CupPairingsScreen>(d.Host.Current);
        Assert.Equal(1, back.Group); // zurück zur vorigen Seite (0x3AC75)
        d.Press(Key.Escape);
        Assert.IsType<MainMenuScreen>(d.Host.Current);
        Assert.Equal(before, StateFingerprint.Of(game));
    }

    /// <summary>
    /// [ORIGINAL] Startspieltag = WM-Stufe DGROUP 0x7AE (gespielte WM-Termine), höchstens der fünfte; Ergebnisse stehen bei
    /// ihren Begegnungen.
    /// </summary>
    [GameDataFact]
    public void PairingsView_StartsAtNextMatchDay_ShowsResults()
    {
        var d = StartWorldOnlyThroughSetup();
        var game = d.Host.Game!;
        game.AutoNominate();
        for (var guard = 0; game.WorldCup!.Groups[0].Results.Count < 6; guard++)
        {
            Assert.True(guard < 20, "keine zwei WM-Spieltage");
            game.Continue();
        }
        var screen = new CupPairingsScreen(d.Host, d.Host.Current!);
        Assert.Equal(2, screen.Day);
        d.Host.Show(screen);
        Click(d, CupPairingsScreen.Buttons[0]);
        var group = game.WorldCup.Groups[0];
        var played = group.Results.Skip(3).Take(3).ToList();
        Assert.Equal(played.Select(m => (m.Home, m.Away, m.HomeGoals, m.AwayGoals)),
            screen.Rows.Select(r => (r.Home, r.Away, r.HomeGoals, r.AwayGoals)));

        for (var guard = 0; game.Phase is SeasonPhase.Running or SeasonPhase.ClubSeasonOver; guard++)
        {
            Assert.True(guard < 50, "Saisonende nicht erreicht");
            game.Continue();
        }
        Assert.Equal(CupPairingsScreen.Days - 1, CupPairingsScreen.StartDay(game));
    }

    private static void Click(ScreenDriver d, Rect r) => d.Click(r.X + 5, r.Y + 5);

    private static bool ShowsTrophy(ScreenDriver d)
    {
        var sheet = d.Host.Assets.Image(EuropeanCupScreen.TrophyGraphic);
        var src = CupPairingsScreen.TrophySource;
        for (var y = 0; y < src.H; y++)
            for (var x = 0; x < src.W; x++)
                if (sheet.Pixels[(src.Y + y) * sheet.Width + src.X + x] != d.Surface.Pixels[(10 + y) * Surface.Width + 29 + x])
                    return false;
        return true;
    }

    /// <summary>
    /// Anzeige im Spielablauf wie Video <c>bundestrainer_00437</c> („Gruppe 1   60. Min.“, Pokale, ohne Knöpfe) – nur für
    /// Bildschirmfotos (der Spielablauf selbst gehört zu `livespiel`).
    /// </summary>
    [GameDataFact]
    public void LiveView_LikeVideo_HasTrophiesNoButtons()
    {
        var d = StartWorldOnlyThroughSetup();
        var group = d.Host.Game!.WorldCup!.Groups[0];
        var screen = new CupPairingsScreen(d.Host, d.Host.Current!, 0, 0,
            [new(group.Teams[0], group.Teams[1], 3, 3)], liveMinute: 60, trophies: true);
        d.Host.Show(screen);
        d.Frame();
        Assert.Equal("Gruppe 1   60. Min.", screen.Title);
        Assert.True(ShowsTrophy(d));
    }
    /// <summary>
    /// WM-Statistik 0x3ADE1, DOSBox <c>17_wm_statistik_leer.png</c>: Die Kachel öffnet die WM-Statistik (vorher kein
    /// Bildschirm); vor der ersten WM acht Zeilen „  0 P.“, darunter alle zwölf Länder in der Reihenfolge der laufenden WM
    /// (Gruppe 1 oben). Kleine Flaggen aus kb\12 pixelgenau, Knöpfe mit Flagge; Öffnen ändert nichts.
    /// </summary>
    [GameDataFact]
    public void WorldCupStatisticsTile_BeforeFirstWorldCup_LikeDosbox()
    {
        var d = StartWorldOnlyThroughSetup();
        var game = d.Host.Game!;
        var before = StateFingerprint.Of(game);
        d.OpenTile("WM-Statistik");
        var screen = Assert.IsType<WorldCupStatisticsScreen>(d.Host.Current);
        d.Frame();
        Assert.Empty(screen.Rankings);
        Assert.Equal(game.WorldCup!.Order, screen.Order);
        Assert.All(screen.Order, n => Assert.Equal(0, screen.Points(n)));
        Assert.Equal(game.WorldCup.Order.Take(8), screen.Standings().Take(8)); // Gleichstand: WM-Reihenfolge bleibt
        for (var i = 0; i < 12; i++)
        {
            var x = 24 + 46 * (i % 6);
            AssertSmallFlag(d, screen.Order[i], x + 16, (i < 6 ? 122 : 220) + 1);
            var logo = d.Host.Assets.Image(game.Clubs[screen.Order[i]].LogoGraphic!.Value);
            var y = i < 6 ? 131 : 177;
            Assert.Equal(logo.Pixels[0], d.Surface.Pixels[(y + 2) * Surface.Width + x + 2]);
            Assert.Equal(23, d.Surface.Pixels[y * Surface.Width + x + 20]);
            Assert.Equal(29, d.Surface.Pixels[(y + 41) * Surface.Width + x + 20]);
        }
        for (var r = 0; r < 8; r++)
            AssertSmallFlag(d, screen.Standings()[r], 33, 37 + 9 * r);
        d.Press(Key.Escape);
        Assert.IsType<MainMenuScreen>(d.Host.Current);
        Assert.Equal(before, StateFingerprint.Of(game));
    }

    /// <summary>
    /// Video <c>eisbaeren_f07_03843</c> (nach drei WMs): Punkte 22/21/18/14/13/8/7/4 für Tschechei, Russland, Finnland,
    /// Kanada, Schweden, USA, Norwegen, Schweiz; je WM-Spalte k die Flagge des Landes auf Platz p bei (75 + 14 k | 28 + 9 p).
    /// </summary>
    [GameDataFact]
    public void WorldCupStatistics_ThreeWorldCups_LikeVideo()
    {
        var d = StartWorldOnlyThroughSetup();
        var game = d.Host.Game!;
        int N(string name) => game.Clubs.Single(c => c.Index >= NationalTeams.First && c.Index <= NationalTeams.Last
            && string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)).Index;
        IReadOnlyList<int> Wm(params string[] names) => [.. names.Select(N)];
        IReadOnlyList<IReadOnlyList<int>> rankings =
        [
            Wm("Tschechei", "Russland", "Finnland", "Norwegen", "Kanada", "Schweden", "USA", "Schweiz"),
            Wm("Russland", "Tschechei", "Kanada", "Schweden", "Finnland", "USA", "Norwegen", "Schweiz"),
            Wm("Finnland", "Tschechei", "Russland", "Schweden", "Kanada", "USA", "Schweiz", "Österreich"),
        ];
        var order = Wm("USA", "Tschechei", "Norwegen", "Schweiz", "Frankreich", "Schweden",
            "Finnland", "Russland", "Österreich", "Kanada", "Deutschland", "Italien");
        var screen = new WorldCupStatisticsScreen(d.Host, d.Host.Current!, rankings, order);
        d.Host.Show(screen);
        d.Frame();
        var standings = screen.Standings();
        Assert.Equal(Wm("Tschechei", "Russland", "Finnland", "Kanada", "Schweden", "USA", "Norwegen", "Schweiz"), standings.Take(8));
        Assert.Equal([22, 21, 18, 14, 13, 8, 7, 4], standings.Take(8).Select(screen.Points));
        Assert.Equal(1, screen.Points(N("Österreich")));
        for (var k = 0; k < 3; k++)
            for (var p = 0; p < 8; p++)
                AssertSmallFlag(d, rankings[k][p], 75 + 14 * k, 28 + 9 * (p + 1));
    }

    private static void AssertSmallFlag(ScreenDriver d, int nation, int x, int y)
    {
        var sheet = d.Host.Assets.Image(WorldCupStatisticsScreen.SmallFlagGraphic);
        var sx = (nation - NationalTeams.First) * WorldCupStatisticsScreen.SmallFlagWidth;
        for (var py = 0; py < WorldCupStatisticsScreen.SmallFlagHeight; py++)
            for (var px = 0; px < WorldCupStatisticsScreen.SmallFlagWidth; px++)
                Assert.Equal(sheet.Pixels[py * sheet.Width + sx + px], d.Surface.Pixels[(y + py) * Surface.Width + x + px]);
    }
}
