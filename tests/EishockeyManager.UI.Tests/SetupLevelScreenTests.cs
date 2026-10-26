using EishockeyManager.UI.Screens;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Level-Regler der Spielvorbereitung mit den Originaldateien (übersprungen ohne „gamedata“).
/// Original: Wert ± 1, begrenzt auf 0 … 5, kein Umlauf (Overlay ab Datei 0x2890A, Höchstwerte DGROUP 0x11C).
/// </summary>
public class SetupLevelScreenTests
{
    [Fact]
    public void LevelSlider_ReachesFiveAndStopsAtBothEnds()
    {
        var d = FlowTests.CreateDriver();
        if (d is null)
            return; // keine Originaldateien vorhanden

        d.Host.Show(new TitleScreen(d.Host));
        d.Frame();
        d.Click(160, 120);
        d.Click(20, 60);
        var head = SetupScreen.HeadCell(0);
        d.Click(head.X + 5, head.Y + 5);
        d.Type("Level");
        d.Press(Key.Enter);
        var setup = Assert.IsType<SetupScreen>(d.Host.Current).Setup;
        Assert.Equal(3, setup.Managers[0].Level);

        var down = SetupScreen.ArrowDown(0);
        var up = SetupScreen.ArrowUp(0);
        d.Click(down.X + 2, down.Y + 2);
        d.Click(down.X + 2, down.Y + 2);
        Assert.Equal(5, setup.Managers[0].Level);
        d.Click(down.X + 2, down.Y + 2);
        Assert.Equal(5, setup.Managers[0].Level); // vorher Umlauf nach 0

        for (var i = 0; i < 7; i++)
            d.Click(up.X + 2, up.Y + 2);
        Assert.Equal(0, setup.Managers[0].Level); // bleibt bei 0

        d.Frame(); // Zeichnen mit allen Streifen
    }
}
