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
}
