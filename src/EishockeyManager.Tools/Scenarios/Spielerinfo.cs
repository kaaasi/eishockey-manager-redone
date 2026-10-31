using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.UI.Screens.Team;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.Tools.Scenarios;

/// <summary>
/// Spur spielerinfo (Runde 8): Spielerdaten-Fenster (em.exe 0x041AC) und Hinweiszeile der Aufstellung (0x41683).
/// Vorlage für das Fenster: Referenzbild <c>oberflaeche_1115</c> („HANFT“, Transferliste); der Spieler wird mit den dort
/// lesbaren Werten nachgebaut (Alter 18, Stärken 16/20/53, Talent 8 – aus dem Gesicht ermittelt, Kaufpreis 150.000 DM).
/// </summary>
internal static class Spielerinfo
{
    [Scenario("spielerinfo")]
    private static void Run(ScreenDriver d)
    {
        d.StartNewGame();
        d.OpenTile("Transfer");
        var screen = (TransferScreen)d.Host.Current!;
        var hanft = new Player
        {
            Name = "HANFT", Role = PlayerRole.Forward, Age = 18, Talent = 8, Condition = 16, Technique = 20, Form = 53,
            Energy = 70, Trend = PlayerTrend.Constant,
        };
        screen.Info.Show(hanft, 150_000);
        d.Move(300, 5);
        d.Save("01_spielerdaten_oberflaeche_1115");

        d.Click(PlayerInfoPopup.AhaButton.X + 5, PlayerInfoPopup.AhaButton.Y + 5);
        d.OpenTile("Aufstellung");
        var y = 29 + 6 * 3;
        foreach (var (x, name) in new[] { (12, "nr"), (30, "art"), (60, "name"), (100, "spiele"), (120, "kotefo"), (150, "tore"),
                     (170, "strafen"), (200, "status"), (228, "tendenz") })
        {
            d.Move(x, y);
            d.Save($"02_hinweis_{name}");
        }
    }
}
