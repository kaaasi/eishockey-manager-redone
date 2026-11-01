using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Competitions;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur wettbewerbe-komplett Teil 2: Gegner-Fenster 0x128:0x2A (Datei 0x4D385) an den WM-Bildschirmen. Vorher fehlte
/// das Fenster ganz: Ein Klick auf eine Zeile der WM-Tabelle schloss die Tabelle.
/// </summary>
public class GegnerFensterTests
{
    private static GameState Synthetic()
    {
        var data = new byte[Formats.ClubDatabase.FileLength];
        for (var i = 0; i < Formats.ClubDatabase.ClubsWithSquad; i++)
        {
            System.Text.Encoding.ASCII.GetBytes($"VEREIN {i}").CopyTo(data, Formats.ClubDatabase.SquadOffset + i * Formats.ClubDatabase.SquadRecordLength);
            data[Formats.ClubDatabase.StrengthOffset + i * 3] = (byte)(100 - i % 12 * 5);
        }
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "A", Head = 0, ClubIndex = 8 });
        return NewGame.Create(Formats.ClubDatabase.Parse(data), setup, 3);
    }

    private sealed class Dummy(ScreenHost host) : Screen(host)
    {
        public override void Update(InputState input) { }
        public override void Draw(Surface surface) { }
    }

    private static InputState Click(int x, int y) => new() { MouseX = x, MouseY = y, LeftClick = true };

    /// <summary>
    /// Regression: Klick auf Zeile 3 der WM-Tabelle (y = 42 + 28 · 2 + 1) öffnet das Fenster für das dritte Land der Tabelle;
    /// die Tabelle bleibt stehen, Rechtsklick schließt nur das Fenster. Vorher kehrte der Klick ins Menü zurück.
    /// </summary>
    [Fact]
    public void WorldCupTable_RowClick_OpensWindowForThatNation()
    {
        var game = Synthetic();
        var host = new ScreenHost(null!) { Game = game };
        var menu = new Dummy(host);
        var screen = new WorldCupScreen(host, menu);
        host.Show(screen);
        screen.Update(Click(5, 42 + 28 * 2 + 1));
        Assert.Same(screen, host.Current);
        Assert.Equal(screen.Group!.Table(game.Clubs)[2].Club.Index, screen.Info.Club);
        screen.Update(new InputState { RightClick = true });
        Assert.False(screen.Info.Visible);
        Assert.Same(screen, host.Current);
    }

    /// <summary>[ORIGINAL] Trefferflächen: WM-Tabelle 0x2FA37, Paarungen 0x30251, WM-Statistik 0x3B437.</summary>
    [Fact]
    public void HitAreas_FollowTheOriginal()
    {
        Assert.Null(WorldCupScreen.RowAt(42));
        Assert.Equal(0, WorldCupScreen.RowAt(43));
        Assert.Equal(0, WorldCupScreen.RowAt(69));
        Assert.Equal(1, WorldCupScreen.RowAt(70));
        Assert.Equal(5, WorldCupScreen.RowAt(209));
        Assert.Null(WorldCupScreen.RowAt(210));
        Assert.Null(CupPairingsScreen.FlagAt(100, 39));
        Assert.Equal((0, false), CupPairingsScreen.FlagAt(125, 81));
        Assert.Equal((1, true), CupPairingsScreen.FlagAt(126, 82));
        Assert.Null(CupPairingsScreen.FlagAt(100, 168));
        Assert.Null(WorldCupStatisticsScreen.FlagAt(23, 150));
        Assert.Equal(0, WorldCupStatisticsScreen.FlagAt(24, 132));
        Assert.Equal(7, WorldCupStatisticsScreen.FlagAt(70, 178));
        Assert.Equal(11, WorldCupStatisticsScreen.FlagAt(298, 222));
        Assert.Null(WorldCupStatisticsScreen.FlagAt(100, 223));
    }

    /// <summary>
    /// Inhalt vor der ersten WM wie DOSBox <c>nur-weltkugel/14</c> (Russland): leere Bilanz, „Keine bemerkenswerte Serie
    /// vorhanden.“ (x 103, Farbe 6), vier Mal „Noch keine Eintragung.“ (x 183) und die Kopfzeile der Ergebnisse gegen
    /// Deutschland ohne Ergebnisse. Grundlinien 109, 115, 121, 127, 133, 141 … 159, 167, 173.
    /// </summary>
    [Fact]
    public void Lines_BeforeTheFirstWorldCup_MatchDosBox14()
    {
        var game = Synthetic();
        const int nation = NationalTeams.Germany + 1;
        var lines = TeamInfoWindow.Lines(game, nation);
        Assert.Equal(new[]
        {
            ("Bilanz:", 98, 109), ("0 Siege, 0 Unent., 0 Nied.", 195, 109),
            ("Aus d. letzten Spielen:", 98, 115), ("0:0 Punkte", 195, 115),
            ("Tore / Spiel (Gesamt):  ", 98, 121), ("0.0:0.0", 195, 121),
            ("Serien (Gesamt):  ", 98, 127),
            (TeamInfoWindow.NoSeries, 103, 133),
            ("Höchster Sieg", 98, 141), (TeamInfoWindow.NoEntry, 183, 141),
            ("Höchste Niederl.", 98, 147), (TeamInfoWindow.NoEntry, 183, 147),
            ("Erzielte Tore", 98, 153), (TeamInfoWindow.NoEntry, 183, 153),
            ("Kassierte Tore", 98, 159), (TeamInfoWindow.NoEntry, 183, 159),
            ("Historische Ergebnisse gegen " + game.Clubs[NationalTeams.Germany].Name, 98, 167),
            ("", 98, 173),
        }, lines.Select(l => (l.Text, l.X, l.Baseline)));
        Assert.Equal(TeamInfoWindow.NoSeriesColor, lines[7].Color);
        Assert.Null(TeamInfoWindow.HeadToHeadSlot(game, NationalTeams.Germany)); // DOSBox 44: Deutschland ohne die Zeile
    }

    /// <summary>
    /// Werte nach Spielen wie <c>bundestrainer_00610</c>: Rekord „ 8: 3 (Land)“ (Mindestbreite 2), Serie „ 2 Spiele in Folge
    /// gewonnen“, Punkte aus den Bilanzzeichen, Ergebnisse gegen Deutschland „Gegner:Deutschland“ durch Komma getrennt.
    /// </summary>
    [Fact]
    public void Lines_AfterMatches_FormatLikeTheVideo()
    {
        var game = Synthetic();
        const int nation = NationalTeams.Germany + 1, third = NationalTeams.Germany + 2;
        game.EnterNationalResult(nation, third, 8, 3, NationalMatchKind.WorldCupGroup);
        game.EnterNationalResult(NationalTeams.Germany, nation, 2, 5, NationalMatchKind.WorldCupGroup);
        game.EnterNationalResult(nation, NationalTeams.Germany, 4, 3, NationalMatchKind.Friendly);
        var text = TeamInfoWindow.Lines(game, nation).Select(l => l.Text).ToList();
        Assert.Contains("4:0 Punkte", text);
        Assert.Contains(" " + "2 Spiele in Folge gewonnen", text);
        Assert.Contains(" " + "8: 3 (" + game.Clubs[third].Name + ")", text);
        Assert.Equal("5:2,4:3", text[^1]);
    }

    /// <summary>
    /// [ORIGINAL] Auswahl Heim-/Auswärtsrekord (Datei 0x4DF8C–0x4E030): beide unteren Bytes 0 → Heimtext (Heimsieg 5:0);
    /// sonst gewinnt bei Sieg/Niederlage die größere Halbbyte-Differenz des unteren Bytes – ein Auswärtsrekord verdrängt
    /// darum einen Heimsieg zu null.
    /// </summary>
    [Fact]
    public void RecordValue_ChoosesLikeTheOriginal()
    {
        var game = Synthetic();
        const int nation = NationalTeams.Germany + 1, other = NationalTeams.Germany + 2;
        game.EnterNationalResult(nation, other, 5, 0, NationalMatchKind.WorldCupGroup);
        Assert.StartsWith(" 5: 0", TeamInfoWindow.RecordValue(game, nation, 0));
        game.EnterNationalResult(other, nation, 6, 1, NationalMatchKind.WorldCupGroup); // A-Niederlage „ 1: 6“
        Assert.StartsWith(" 1: 6", TeamInfoWindow.RecordValue(game, nation, 1));
        Assert.Equal(" 1 (" + game.Clubs[other].Name + ")", TeamInfoWindow.RecordValue(game, nation, 2)); // Erzielte Tore: Auswärtsrekord (unteres Byte 1 > 0)
    }

    /// <summary>Das Fenster zu öffnen und zu zeichnen verändert den Spielstand nicht; Rahmen bei (93 | 44).</summary>
    [GameDataFact]
    public void Window_Draw_IsStatePure()
    {
        var d = ScreenDriver.Create(TestGameData.Load());
        d.StartNewGame();
        var game = d.Host.Game!;
        var before = Core.SaveGame.Save(game);
        var screen = new WorldCupScreen(d.Host, new MainMenuScreen(d.Host));
        d.Host.Show(screen);
        d.Click(200, 50);
        d.Frame();
        Assert.True(screen.Info.Visible);
        Assert.Equal(before, Core.SaveGame.Save(game));
        Assert.NotEqual(d.Surface.Pixels[44 * 320 + 200], d.Surface.Pixels[44 * 320 + 60]);
    }
}
