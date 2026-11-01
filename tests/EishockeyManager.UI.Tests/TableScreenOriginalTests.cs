using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Competitions;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur tabellen-original-2: Tabellenansicht nach em.exe 0x4F78D, vermessen am Video „Die Oberfläche von EM“ 13:07
/// (<c>oberflaeche_1307</c>) und folge21_02711.
/// </summary>
public class TableScreenOriginalTests
{
    private static string Plain(string text) => text.Replace(CompetitionWidgets.FigureSpace, ' ');

    [Fact]
    public void Title_FirstMatchDayBeforeAnyGame()
    {
        // [ORIGINAL] DGROUP 0x7A9 = 1 beim Spielstart (0x43A7E): Video 13:07 „TABELLE Gesamt 1.Spieltag“ ohne Spiele.
        Assert.Equal("   TABELLE Gesamt 1.Spieltag", TableScreen.Title(TableView.Gesamt, 0));
        Assert.Equal("   TABELLE Heim 3.Spieltag", TableScreen.Title(TableView.Heim, 3));
    }

    [Fact]
    public void Balance_PaddedWithDashes()
    {
        // [ORIGINAL] Bilanz DGROUP 0x825E: acht Zeichen, Anfang „--------“ (Video 13:07), nach drei Spielen „-----sSs“ (folge21_02711).
        var club = new Club { Index = 0, Name = "X", Strength = 50 };
        var row = new TableRow { Club = club };
        Assert.Equal("--------", TableScreen.Balance(row));
        row.Form.AddRange("sSs");
        Assert.Equal("-----sSs", TableScreen.Balance(row));
    }

    [Fact]
    public void Numbers_OneTextLikeOriginal()
    {
        // [ORIGINAL] 0x4F78D: S U N (Breite 2), „    “, P:M (2), „   “, T:G (3), „      “, Vorzeichen („`“ bei 0) und Betrag (3).
        var row = new TableRow { Club = new Club { Index = 0, Name = "X", Strength = 50 } };
        Assert.Equal(" 0  0  0     0: 0     0:  0      `  0", Plain(TableScreen.Numbers(row)));
        row.Wins = 12; row.Draws = 1; row.Losses = 3; row.GoalsFor = 83; row.GoalsAgainst = 138;
        Assert.Equal("12  1  3    25: 7    83:138      - 55", Plain(TableScreen.Numbers(row)));
    }

    [GameDataFact]
    public void Markers_FirstLeagueOnlyChampion_StateUnchanged()
    {
        // [ORIGINAL] 0x4FAA7 ff.: 1. Liga nur Platz 1 grün (Farbe 15), andere Ligen Platz 1–2; die letzten zwei rot (Farbe 18).
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var before = StateFingerprint.Of(d.Host.Game!);

        d.OpenTile("Tabelle 1. Liga");
        d.Frame();
        Assert.IsType<TableScreen>(d.Host.Current);
        Assert.Equal(15, Pixel(d, 10, 68));
        Assert.Equal(15, Pixel(d, 20, 74));
        Assert.NotEqual(15, Pixel(d, 10, 76));
        Assert.Equal(18, Pixel(d, 10, 138));
        Assert.Equal(18, Pixel(d, 20, 151));
        Assert.NotEqual(18, Pixel(d, 10, 136));
        d.Click(60, 170); // Heim
        d.Frame();
        FlowTests.ReturnToMainMenu(d);

        d.OpenTile("Tabelle Oberliga");
        d.Frame();
        Assert.Equal(15, Pixel(d, 10, 76));
        Assert.NotEqual(15, Pixel(d, 10, 83));
        FlowTests.ReturnToMainMenu(d);

        Assert.Equal(before, StateFingerprint.Of(d.Host.Game!));
    }

    private static byte Pixel(ScreenDriver d, int x, int y) => d.Surface.Pixels[y * Surface.Width + x];
}
