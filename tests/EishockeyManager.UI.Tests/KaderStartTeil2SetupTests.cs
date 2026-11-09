using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur <c>kader-start</c>, Runde 14, Teil 2: A056 über die Bedienung der Spielvorbereitung (übersprungen ohne „gamedata“).
/// [ORIGINAL] em.exe Datei 0x28ACF: bei „nur Weltkugel“ führt jeder Klick in die Managerzeilen auf Zeile 0
/// (<c>docs/analyse/re-runde-14.md</c>, Bilder <c>file29_1</c>/<c>file29_2</c>).
/// </summary>
public class KaderStartTeil2SetupTests
{
    [GameDataFact]
    public void WorldOnly_ClickOnSecondRow_EditsFirstManager_NoSecondManager()
    {
        var d = FlowTests.CreateDriver();
        d.Host.Show(new TitleScreen(d.Host));
        d.Frame();
        d.Click(160, 120);
        var screen = Assert.IsType<SetupScreen>(d.Host.Current);
        var setup = screen.Setup;

        // Fahne aus → „nur Weltkugel“ (ohne Manager ist die Fahne frei).
        d.Click(SetupScreen.ModeFlagHit.X + 5, SetupScreen.ModeFlagHit.Y + 5);
        Assert.Equal(GameMode.WorldOnly, setup.Mode);

        // Klick in Zeile 2 (leer) legt Manager 0 an, nicht einen Manager in Zeile 2.
        var name2 = SetupScreen.NameArea(2);
        d.Click(name2.X + 5, name2.Y + 5);
        var head = SetupScreen.HeadCell(0);
        d.Click(head.X + 5, head.Y + 5);
        d.Type("Erster");
        d.Press(Key.Enter);
        Assert.Equal("Erster", Assert.Single(setup.Managers).Name);

        // Klick in Zeile 1 (Name): Bearbeiten von Manager 0, kein zweiter Manager.
        var name1 = SetupScreen.NameArea(1);
        d.Click(name1.X + 5, name1.Y + 5);
        d.Press(Key.Backspace);
        d.Type("X");
        d.Press(Key.Enter);
        Assert.Single(setup.Managers);
        Assert.Equal("ErsteX", setup.Managers[0].Name);
        d.Frame();
    }
}
