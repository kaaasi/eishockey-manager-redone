using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Competitions;
using EishockeyManager.UI.Screens.ManagerArea;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur pixel-marker Teil 1: R-K2 (Datei 0x4CCB4), R-K3 (0x35D5D), R-K4 (0x2A6A2), R-K5 (0x28030).
/// Vorlage ist die Disassembly von em.exe (<c>docs/analyse/re-rest.md</c>), nicht das Videobild.
/// </summary>
public class PixelMarkerTests
{
    /// <summary>R-K2: Zeichen in den Farben 18/12/22 bei x = 100 · Liga + 20, Name 8 Pixel dahinter.</summary>
    [GameDataFact]
    public void SeasonEnd_RowsUseOriginalColorsAndColumns()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var guard = 0;
        while (d.Host.Current is not SeasonEndScreen && guard++ < 20000)
        {
            KeepSquadHelper.KeepSquads(d.Host.Game!);
            d.Advance();
        }
        Assert.IsType<SeasonEndScreen>(d.Host.Current);
        d.Frame();

        Assert.Equal([20, 120, 220], SeasonEndScreen.LeagueColumns);
        Assert.Equal(8, SeasonEndScreen.NameOffset);
        var mini = d.Host.Assets.Mini;
        var found = new HashSet<byte>();
        foreach (var color in new byte[] { 18, 12, 22 })
        for (var column = 0; column < 3; column++)
        for (var y = SeasonEndScreen.TopRowY; y < SeasonEndScreen.BottomRowY + 12; y++)
        for (var x = SeasonEndScreen.LeagueColumns[column]; x < SeasonEndScreen.LeagueColumns[column] + 7; x++)
            if (d.Surface.Pixels[y * Surface.Width + x] == color)
                found.Add(color);
        // Aufsteiger (12) gibt es in jedem Jahr; Absteiger (18) in den unteren Ligen
        Assert.Contains((byte)12, found);
        Assert.Contains((byte)18, found);
        Assert.DoesNotContain((byte)238, d.Surface.Pixels.Skip(150 * Surface.Width).Take(40 * Surface.Width));
        Assert.True(mini.Height > 0);
    }

    /// <summary>R-K3: Text Farbe 11 auf Grundlinie 126, Schatten Farbe 0 eine Zeile tiefer ohne seitlichen Versatz; 60 Takte.</summary>
    [GameDataFact]
    public void Banner_TextOnBaseline126_ShadowOneRowBelow_60Ticks()
    {
        var d = FlowTests.CreateDriver();
        var a = d.Host.Assets;
        var s = new Surface();
        s.Palette = a.Palette;
        SeasonInfo.DrawBanner(s, a, "Ligaspiel");

        var top = CompetitionWidgets.Top(a.Bold, 126);
        var width = a.Bold.Measure("Ligaspiel");
        var x0 = 160 - width / 2;
        var mask = new bool[width * a.Bold.Height];
        var probe = new Surface();
        probe.Fill(new Rect(0, 0, Surface.Width, Surface.Height), 255);
        probe.Text(a.Bold, x0, top, "Ligaspiel", 11);
        var shadowProbe = new Surface();
        shadowProbe.Fill(new Rect(0, 0, Surface.Width, Surface.Height), 255);
        shadowProbe.Text(a.Bold, x0, top + 1, "Ligaspiel", 0);
        for (var y = top; y < top + a.Bold.Height + 1; y++)
        for (var x = x0; x < x0 + width; x++)
        {
            var i = y * Surface.Width + x;
            if (probe.Pixels[i] == 11)
                Assert.Equal(11, s.Pixels[i]);
            else if (shadowProbe.Pixels[i] == 0)
                Assert.Equal(0, s.Pixels[i]);
        }
        Assert.Equal(198, MatchDayScreen.BannerFrames);
    }

    /// <summary>R-K4: Trikotbild 31 × 26 aus der eigenen Spalte.</summary>
    [Fact]
    public void JerseyLogoTile_Is31x26_FromOwnColumn()
    {
        Assert.Equal(new Rect(70, 0, 31, 26), AdvertisingScreen.LogoTile(AdSlotKind.Trikot, 0));
        Assert.Equal(new Rect(70, 182, 31, 26), AdvertisingScreen.LogoTile(AdSlotKind.Trikot, 7));
        Assert.Equal(new Rect(39, 130, 31, 26), AdvertisingScreen.LogoTile(AdSlotKind.Trikot, 8));
        Assert.Equal(new Rect(39, 156, 31, 26), AdvertisingScreen.LogoTile(AdSlotKind.Trikot, 9));
    }

    /// <summary>R-K5: die Vereinsauswahl blättert zyklisch über vier Seiten (Datei 0x28E4B).</summary>
    [GameDataFact]
    public void ClubPicker_PagesCycle()
    {
        var d = FlowTests.CreateDriver();
        d.Host.Show(new TitleScreen(d.Host));
        d.Frame();
        d.Click(160, 120);
        d.Click(20, 60);
        var cell = SetupScreen.HeadCell(3);
        d.Click(cell.X + 5, cell.Y + 5);
        d.Type("Test");
        d.Press(Key.Enter);
        Assert.IsType<SetupScreen>(d.Host.Current);
        d.Click(150, 60); // Vereinsauswahl des Managers öffnen
        d.Frame();
        var first = (byte[])d.Surface.Pixels.Clone();
        d.Press(Key.PageUp); // Seite 0 → 3 (zyklisch, vorher auf 0 festgehalten)
        d.Frame();
        var wrapped = (byte[])d.Surface.Pixels.Clone();
        Assert.NotEqual(first, wrapped);
        d.Press(Key.PageDown); // 3 → 0
        d.Frame();
        Assert.Equal(first, d.Surface.Pixels);
        for (var i = 0; i < SetupScreen.ClubPages; i++)
        {
            d.Press(Key.PageDown);
            d.Frame();
        }
        Assert.Equal(first, d.Surface.Pixels);
    }
}
