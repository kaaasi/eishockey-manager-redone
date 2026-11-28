using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Competitions;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur bedienung Teil 2: B12 (A296) und B18 (A297, A301) nach docs/archiv/analyse/re-abweichungen.md (Hauptmenü Datei 0x44E22,
/// Schleife 0x4535B–0x4592F). Vorher rot: Rechtsklick wählte ab, Logo und Kacheln nur links, Klick auf den Kopf wählte ab,
/// Markierung als gelbe Unterkante.
/// </summary>
public class BedienungHauptmenueTests
{
    private static (ScreenDriver Driver, GameState Game, MainMenuScreen Menu) Start()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        return (d, d.Host.Game!, Assert.IsType<MainMenuScreen>(d.Host.Current));
    }

    private static (int X, int Y) Center(Rect r) => (r.X + r.W / 2, r.Y + r.H / 2);

    /// <summary>B12: Rechtsklick aufs Logo lässt den Tag weiterlaufen wie der Linksklick (Datei 0x45483–0x4551E, [bp-7] = 3).</summary>
    [GameDataFact]
    public void RightClickOnLogo_ContinuesLikeLeftClick()
    {
        var (left, _, _) = Start();
        var (right, _, _) = Start();
        var (x, y) = Center(MainMenuScreen.ContinueLogo);
        left.Click(x, y);
        right.RightClick(x, y);
        Assert.IsNotType<MainMenuScreen>(right.Host.Current);
        Assert.Equal(left.Host.Current!.GetType(), right.Host.Current!.GetType());
    }

    /// <summary>B12: Rechtsklick wählt eine Kachel, eine leere Fläche wählt nicht ab, ein Eintrag öffnet seinen Bildschirm.</summary>
    [GameDataFact]
    public void RightClick_SelectsTile_KeepsSelection_OpensEntry()
    {
        var (d, _, menu) = Start();
        var (x, y) = Center(MainMenuScreen.BigTile(0));
        d.RightClick(x, y);
        Assert.Equal(0, menu.SelectedBigTile);
        d.RightClick(3, 150); // links neben den großen Kacheln
        Assert.Equal(0, menu.SelectedBigTile);
        d.RightClick(x, y); // Kachel 1 erneut
        Assert.Equal(0, menu.SelectedBigTile);
        var entry = Array.FindIndex(MenuTiles.Small[0], t => !t.IsEmpty);
        var (ex, ey) = Center(MainMenuScreen.SmallTile(entry));
        d.RightClick(ex, ey);
        Assert.IsNotType<MainMenuScreen>(d.Host.Current);
    }

    /// <summary>
    /// B12 / Befund #1014 (BD-01): Vereins- und Gegnername in der Infoleiste öffnen ihr Fenster nur mit links (Datei
    /// 0x45375–0x453DF, nur linke Maustaste DGROUP 0x5A16 = 1). Rückschaltprobe: <c>Clicked</c> → <c>Clicked || RightClicked</c> macht rot.
    /// </summary>
    [GameDataFact]
    public void InfoBarNames_OpenOnlyWithLeftClick()
    {
        var (d, game, menu) = Start();
        var own = Center(InfoBarWindows.OwnClubName);
        d.RightClick(own.X, own.Y);
        Assert.False(menu.InfoWindows.Visible);
        var next = game.UpcomingMatch(NationalMenu.Team(game));
        Assert.NotNull(next); // Machbarkeit: sonst prüfte der Gegnername nichts
        var opp = Center(InfoBarWindows.OpponentName);
        d.RightClick(opp.X, opp.Y);
        Assert.False(menu.InfoWindows.Visible);
        d.Click(opp.X, opp.Y);
        Assert.True(menu.InfoWindows.Visible); // Gegenprobe: links öffnet
    }

    /// <summary>Escape wählt als Komfort weiter ab (B11 = A).</summary>
    [GameDataFact]
    public void Escape_StillDeselects()
    {
        var (d, _, menu) = Start();
        var (x, y) = Center(MainMenuScreen.BigTile(1));
        d.Click(x, y);
        Assert.Equal(1, menu.SelectedBigTile);
        d.Press(Key.Escape);
        Assert.Equal(-1, menu.SelectedBigTile);
    }

    /// <summary>
    /// B18/A297: Klick ins Feld neben dem Kopf (x 0xE9–0x10F, y 0x22–0x47) zeigt die Nachrichtenleiste, die Auswahl bleibt, ein
    /// Eintrag öffnet danach weiter seinen Bildschirm.
    /// </summary>
    [GameDataFact]
    public void HeadClick_ShowsBar_KeepsSelection_EntryStillOpens()
    {
        var (d, _, menu) = Start();
        var (x, y) = Center(MainMenuScreen.BigTile(0));
        d.Click(x, y);
        d.Click(250, 40);
        Assert.Equal(0, menu.SelectedBigTile);
        Assert.True(menu.NewsBarVisible);
        var s = new Surface();
        menu.Draw(s);
        var bar = new Surface();
        bar.Palette = s.Palette;
        ManagerAreaDraw(bar, d);
        for (var py = 201; py < 237; py++)
            for (var px = 3; px < 317; px++)
                Assert.True(bar.Pixels[py * Surface.Width + px] == s.Pixels[py * Surface.Width + px], $"Leiste ({px} | {py})");

        var entry = Array.FindIndex(MenuTiles.Small[0], t => !t.IsEmpty);
        var (ex, ey) = Center(MainMenuScreen.SmallTile(entry));
        d.Click(ex, ey);
        Assert.IsNotType<MainMenuScreen>(d.Host.Current);
    }

    private static void ManagerAreaDraw(Surface s, ScreenDriver d) =>
        Screens.ManagerArea.NewsBar.Draw(s, d.Host.Assets, d.Host.Game!);

    /// <summary>
    /// B18/A301: Umriss 0x0B5A7 von (X + 4 | Y + 4) bis (X + 67 | Y + 52) in Farbe 24, die übrigen Kacheln dort Farbe 30
    /// (Datei 0x45579–0x45659; b18_hauptmenue_aufbau.py: Kachel 1 (12 | 84)–(75 | 132)).
    /// </summary>
    [GameDataFact]
    public void SelectedTile_HasOutline24_OthersStay30()
    {
        var (d, _, menu) = Start();
        for (var selected = 0; selected < 7; selected++)
        {
            var (x, y) = Center(MainMenuScreen.BigTile(selected));
            d.Click(x, y);
            Assert.Equal(selected, menu.SelectedBigTile);
            var s = new Surface();
            menu.Draw(s);
            for (var i = 0; i < 7; i++)
            {
                var r = MainMenuScreen.BigTile(i);
                var color = i == selected ? Colors.Yellow : Colors.TileBackground;
                for (var px = r.X + 4; px <= r.X + 67; px++)
                {
                    Assert.Equal(color, s.Pixels[(r.Y + 4) * Surface.Width + px]);
                    Assert.Equal(color, s.Pixels[(r.Y + 52) * Surface.Width + px]);
                }
                for (var py = r.Y + 4; py <= r.Y + 52; py++)
                {
                    Assert.Equal(color, s.Pixels[py * Surface.Width + r.X + 4]);
                    Assert.Equal(color, s.Pixels[py * Surface.Width + r.X + 67]);
                }
            }
        }
        Assert.Equal((12, 84, 75, 132), (MainMenuScreen.BigTile(0).X + 4, MainMenuScreen.BigTile(0).Y + 4,
            MainMenuScreen.BigTile(0).X + 67, MainMenuScreen.BigTile(0).Y + 52));
    }
}
