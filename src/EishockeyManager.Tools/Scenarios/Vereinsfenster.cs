using EishockeyManager.UI.Testing;

namespace EishockeyManager.Tools.Scenarios;

/// <summary>Spur ui-meldungen, Teil 2 (UM-2): Vereinsfenster aus der Infoleiste (em.exe Datei 0x4D385).</summary>
internal static class Vereinsfenster
{
    [Scenario("vereinsfenster")]
    private static void All(ScreenDriver d)
    {
        d.StartNewGame();
        for (var i = 0; i < 12; i++)
            d.PlayMatchDay();
        d.Frame();
        d.Click(272, 26); // eigener Vereinsname (Datei 0x453B4)
        d.Move(0, 230);
        d.Save("01_eigener_verein");
        d.RightClick(160, 120);
        d.Click(184, 39); // Gegnername (Datei 0x45375)
        d.Move(0, 230);
        d.Save("02_gegner");
    }
}
