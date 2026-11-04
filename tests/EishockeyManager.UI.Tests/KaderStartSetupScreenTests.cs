using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur <c>kader-start</c>, Runde 13, Teil 2: Regler der Spielvorbereitung über die Bedienung (übersprungen ohne „gamedata“).
/// U15-b ([ORIGINAL] Datei 0x2890E–0x2891C) und L1 (Start-Liga für alle Manager, DGROUP 0x11A).
/// </summary>
public class KaderStartSetupScreenTests
{
    private static void AddManager(ScreenDriver d, int slot, int head, string name)
    {
        var area = SetupScreen.NameArea(slot);
        d.Click(area.X + 5, area.Y + 5);
        var cell = SetupScreen.HeadCell(head);
        d.Click(cell.X + 5, cell.Y + 5);
        d.Type(name);
        d.Press(Key.Enter);
    }

    private static void Arrow(ScreenDriver d, SetupSlider slider, int delta, int times = 1)
    {
        var arrow = delta < 0 ? SetupScreen.ArrowUp((int)slider) : SetupScreen.ArrowDown((int)slider);
        for (var i = 0; i < times; i++)
            d.Click(arrow.X + 2, arrow.Y + 2);
    }

    [GameDataFact]
    public void OneSeason_LocksStartLeague_ForAllManagers()
    {
        var d = FlowTests.CreateDriver();
        d.Host.Show(new TitleScreen(d.Host));
        d.Frame();
        d.Click(160, 120);
        d.Click(20, 60);
        var head = SetupScreen.HeadCell(0);
        d.Click(head.X + 5, head.Y + 5);
        d.Type("Erster");
        d.Press(Key.Enter);
        var setup = Assert.IsType<SetupScreen>(d.Host.Current).Setup;

        Arrow(d, SetupSlider.StartLeague, +1); // Manager 0 gewählt: 2. Liga
        AddManager(d, 1, 1, "Zweiter"); // neuer Manager übernimmt die gemeinsame Start-Liga
        Assert.All(setup.Managers, m => Assert.Equal(League.ZweiteLiga, m.StartLeague));

        Arrow(d, SetupSlider.Duration, -1, times: 5);
        Assert.Equal(GameDuration.DreiSaisons, setup.Duration); // bleibt stehen (file28_4)

        Arrow(d, SetupSlider.StartLeague, -1, times: 2);
        Arrow(d, SetupSlider.Duration, -1);
        Assert.Equal(GameDuration.EineSaison, setup.Duration);
        Arrow(d, SetupSlider.StartLeague, +1);
        Assert.All(setup.Managers, m => Assert.Equal(League.Bundesliga, m.StartLeague)); // file28_3
        d.Frame();
    }

    /// <summary>
    /// R-K5 (#549), Teil 3: Die Vereinsauswahl zeigt die Startmischung (Datei 0x43A17 vor 0x28030). Zelle k der Seite 0 wählt
    /// den Verein auf Platz k der Mischung derselben Saat, Seite 3 Zelle 8 bleibt HEC Bonn (DB 44, Video <c>oberflaeche_0136</c>).
    /// Vorher wählte Zelle k den Datenbankverein k.
    /// </summary>
    [GameDataFact]
    public void ClubPicker_ShowsStartOrder_PageThreeInDatabaseOrder()
    {
        var d = FlowTests.CreateDriver(seed: 1993);
        d.Host.Show(new TitleScreen(d.Host));
        d.Frame();
        d.Click(160, 120);
        d.Click(20, 60);
        var head = SetupScreen.HeadCell(0);
        d.Click(head.X + 5, head.Y + 5);
        d.Type("Erster");
        d.Press(Key.Enter);
        var screen = Assert.IsType<SetupScreen>(d.Host.Current);
        var places = NewGame.StartOrder(1993);
        Assert.NotEqual(0, places[0]);

        d.Click(150, 60);
        var cell = SetupScreen.ClubCell(0);
        d.Click(cell.X + 5, cell.Y + 5);
        Assert.Equal(places[0], screen.Setup.Managers[0].ClubIndex);
        Assert.Equal((0, 0), screen.CellOf(places[0]));

        d.Click(150, 60);
        for (var p = 0; p < 3; p++)
            d.Wheel(-1);
        cell = SetupScreen.ClubCell(8);
        d.Click(cell.X + 5, cell.Y + 5);
        Assert.Equal(44, screen.Setup.Managers[0].ClubIndex);
        Assert.Equal((3, 8), screen.CellOf(44));
    }
}
