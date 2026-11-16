using EishockeyManager.UI.Testing;

namespace EishockeyManager.Tools.Scenarios;

/// <summary>
/// Spur zusehen, Teil 1: dritter Knopf des Gegner-/Vereinsfensters (Markierung zum Zusehen, em.exe Datei 0x4D78A, 0x4E440).
/// Vergleich mit <c>eisbaeren_f24_00046</c> (Modus 0) und <c>eisbaeren_f24_00122</c> (Modus 2).
/// </summary>
internal static class Zusehen
{
    [Scenario("zusehen")]
    private static void All(ScreenDriver d)
    {
        d.StartNewGame();
        d.PlayMatchDay();
        d.Frame();
        d.Click(184, 39); // Gegnername (Datei 0x45375): Modus 0
        d.Move(0, 230);
        d.Save("01_gegner_modus0");
        d.Click(250, 220); // dritter Knopf: 0 → 1
        d.Move(0, 230);
        d.Save("02_gegner_modus1");
        d.Click(250, 220); // 1 → 2
        d.Move(0, 230);
        d.Save("03_gegner_modus2");
        d.RightClick(160, 120);
        d.Click(272, 26); // eigener Verein: Modus 2 seit dem Spielstart (Datei 0x298EA)
        d.Move(0, 230);
        d.Save("04_eigener_verein");
    }
}
