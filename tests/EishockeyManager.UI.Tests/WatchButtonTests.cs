using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Competitions;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur zusehen, Teil 1 (A286, A752): dritter Knopf des Gegner-/Vereinsfensters, Markierung zum Zusehen (em.exe Datei 0x4D78A,
/// Klick Datei 0x4E440–0x4E46A → 0x0AE2B). Vorher fehlte der Knopf ganz; ein Klick auf (250 | 220) bewirkte nichts.
/// </summary>
public class WatchButtonTests
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
        setup.Managers.Add(new ManagerSetup { Name = "A", Head = 0, ClubIndex = 2 });
        return NewGame.Create(Formats.ClubDatabase.Parse(data), setup, 3);
    }

    private static InputState Click(int x, int y) => new() { MouseX = x, MouseY = y, LeftClick = true };

    /// <summary>[ORIGINAL] Knopf bei (x + 150 | y + 171) = (243 | 215), Datei 0x4D7C6–0x4D7D3.</summary>
    [Fact]
    public void WatchButton_AtTheOriginalPlace() =>
        Assert.Equal(new Rect(243, 215, Widgets.ButtonWidth, Widgets.ButtonHeight), TeamInfoWindow.WatchButton);

    /// <summary>
    /// Regression: Klick auf den dritten Knopf im Vereinsfenster des Gegners schaltet 0 → 1 → 2 → 0 ([ORIGINAL] Datei
    /// 0x4E440–0x4E44D), das Fenster bleibt offen; ein Rechtsklick schließt es ohne Änderung.
    /// </summary>
    [Fact]
    public void ClubWindow_Click_CyclesWatchMode()
    {
        var game = Synthetic();
        var host = new ScreenHost(null!) { Game = game };
        var menu = new MainMenuScreen(host);
        host.Show(menu);
        menu.InfoWindows.Show(7);
        var club = game.Clubs[7];
        Assert.Equal(0, club.WatchMode);

        menu.Update(Click(250, 220));
        Assert.Equal(1, club.WatchMode);
        Assert.True(menu.InfoWindows.Club.Visible);
        menu.Update(Click(250, 220));
        Assert.Equal(2, club.WatchMode);
        menu.Update(Click(250, 220));
        Assert.Equal(0, club.WatchMode);
        menu.Update(Click(120, 220)); // „Reicht mir“
        Assert.False(menu.InfoWindows.Visible);
        Assert.Equal(0, club.WatchMode);
    }

    /// <summary>Länderfenster: derselbe Knopf schaltet das Land um (Länder als eigene Gruppe, Datei 0xAE41).</summary>
    [Fact]
    public void NationWindow_Click_TogglesNation()
    {
        var game = Synthetic();
        var window = new TeamInfoWindow();
        window.Show(NationalTeams.First + 3);
        Assert.True(window.Update(Click(250, 220), game));
        Assert.Equal(1, game.Clubs[NationalTeams.First + 3].WatchMode);
        Assert.True(window.Update(Click(250, 220))); // ohne Spielstand: nur Anzeige, keine Änderung
        Assert.Equal(1, game.Clubs[NationalTeams.First + 3].WatchMode);
    }

    /// <summary>
    /// [ORIGINAL] Bild mit Originaldaten: Knopfbild kb\2 und Symbol 12 × 10 aus kb\4 (19 | 36 + 11 · Modus) bei (266 | 216)
    /// (Datei 0x4C10A–0x4C14A); am Video <c>eisbaeren_f24_00046</c> (Modus 0) und <c>_00122</c> (Modus 2) vermessen. Das
    /// eigene Vereinsfenster zeigt Modus 2 (Spielstart), das des Gegners Modus 0; Öffnen ändert den Spielzustand nicht.
    /// </summary>
    [GameDataFact]
    public void Render_SymbolOfTheMode()
    {
        var d = ScreenDriver.Create(TestGameData.Load());
        d.StartNewGame();
        d.PlayMatchDay();
        var game = d.Host.Game!;
        d.Frame();
        var before = StateFingerprint.Of(game);
        d.Click(272, 26); // eigener Verein
        d.Frame();
        AssertSymbol(d, GameState.WatchLive);
        Assert.Equal(before, StateFingerprint.Of(game));

        d.RightClick(160, 120);
        d.Click(184, 39); // Gegner
        d.Frame();
        AssertSymbol(d, GameState.WatchOff);
        d.Click(250, 220);
        d.Frame();
        AssertSymbol(d, GameState.WatchGoals);
    }

    /// <summary>
    /// Review #907 P3-4 (#902): feste Sollwerte der Symbolraster (Datei 0x4C110–0x4C13A: x = 19, y = 36 + 11 · Modus, 12 × 10); der
    /// Vergleich in <see cref="AssertSymbol"/> liest dieselbe Funktion und hielte die Konstante nicht fest.
    /// </summary>
    [Fact]
    public void WatchSymbol_HasFixedRasterPerMode()
    {
        Assert.Equal(new Rect(19, 36, 12, 10), TeamInfoWindow.WatchSymbol(0));
        Assert.Equal(new Rect(19, 47, 12, 10), TeamInfoWindow.WatchSymbol(1));
        Assert.Equal(new Rect(19, 58, 12, 10), TeamInfoWindow.WatchSymbol(2));
    }

    private static void AssertSymbol(ScreenDriver d, int mode)
    {
        var sheet = d.Host.Assets.Image(TeamInfoWindow.WatchSymbolImage);
        var source = TeamInfoWindow.WatchSymbol(mode);
        for (var y = 0; y < source.H; y++)
            for (var x = 0; x < source.W; x++)
                Assert.Equal(sheet[source.X + x, source.Y + y], d.Surface.Pixels[(216 + y) * Surface.Width + 266 + x]);
        // Knopfrahmen aus kb\2 (17 | 179) links des Symbols
        var button = d.Host.Assets.Image(2);
        Assert.Equal(button[17 + 5, 179 + 6], d.Surface.Pixels[(215 + 6) * Surface.Width + 243 + 5]);
    }
}
