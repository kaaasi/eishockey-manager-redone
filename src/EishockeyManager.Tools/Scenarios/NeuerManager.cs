using EishockeyManager.UI;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.Tools.Scenarios;

/// <summary>
/// Spur nutzer-befunde, Teil 3 (#841): Kachel „Neuer“ → Spielvorbereitung im Hinzufügen-Modus (em.exe Datei 0x45755 → 0x28165).
/// Bilder: 1 Vorbereitung mit dem vorhandenen Manager, 2 neuer Manager mit Kopf, Name und Verein, 3 Hauptmenü danach.
/// </summary>
internal static class NeuerManager
{
    [Scenario("neuer-manager")]
    private static void Run(ScreenDriver d)
    {
        d.StartNewGame();
        d.PlayMatchDay();
        d.OpenTile("Neuer");
        d.Save("neuer-1-vorbereitung");
        d.Click(20, 40 + SetupScreen.SlotPitch + 5);
        var head = SetupScreen.HeadCell(7);
        d.Click(head.X + 5, head.Y + 5);
        d.Type("Zweiter");
        d.Press(Key.Enter);
        d.Click(150, 40 + SetupScreen.SlotPitch + 5);
        var cell = SetupScreen.ClubCell(2);
        d.Click(cell.X + 5, cell.Y + 5);
        d.Move(1, 1);
        d.Save("neuer-2-zweiter-manager");
        d.Click(SetupScreen.StartButton.X + 10, SetupScreen.StartButton.Y + 10);
        d.Move(1, 1);
        d.Save("neuer-3-hauptmenue");
    }
}
