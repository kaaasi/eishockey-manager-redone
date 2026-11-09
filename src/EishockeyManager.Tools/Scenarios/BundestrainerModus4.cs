using EishockeyManager.Core;
using EishockeyManager.UI;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.Tools.Scenarios;

/// <summary>
/// Spur bundestrainer-modus4: Spielmodus 4 (beide Knöpfe), Manager beim stärksten Verein der 1. Liga
/// (Düsseldorfer EG, Index 2). Saison im Schnelldurchlauf bis zum 14. April; der Manager ist am 13. April unter den
/// ersten fünf und wird Bundestrainer. Bilder: Vereinsmenü am ersten WM-Tag, danach der Zug „Manager 5“ mit dem Menü
/// des Bundestrainers (Kachelsperren, WM-Kader) und das Vereinsmenü am nächsten WM-Tag. Kein Videobeleg für Modus 4.
/// [APPROX] (Review #662, R14NT-03): Ein passiver Manager kommt mit Verein 2 nicht sicher unter die ersten fünf der 1. Liga
/// (Ernennung Datei 0x34769 nur für diese), seit Runde 12 brach der Ablauf deshalb ab. Wird der Manager am 13. April nicht
/// ernannt, ernennt ihn der Ablauf danach ausdrücklich (<see cref="GameState.AppointNationalCoach"/>) – wie die Ernennung des
/// Originals, aber nach dem Auswürfeln der Länderwerte (Datei 0x554CF), das im Original nur ohne Bundestrainer läuft.
/// </summary>
internal static class BundestrainerModus4Scenarios
{
    [Scenario("bundestrainer-modus4")]
    private static void Mode4Coach(ScreenDriver d)
    {
        d.StartNewGame(clubIndex: 2);
        var game = d.Host.Game!;
        for (var guard = 0; !game.NationalTurn; guard++)
        {
            if (guard > 400 || game.Phase is not (SeasonPhase.Running or SeasonPhase.ClubSeasonOver))
                throw new InvalidOperationException("Kein Zug des Bundestrainers erreicht.");
            if (game.CurrentDay == SeasonCalendar.ClubSeasonEnd + 1)
            {
                if (game.NationalCoach is null)
                    game.AppointNationalCoach(game.Managers[0]); // [APPROX] siehe Klassenkommentar (R14NT-03)
                d.Move(10, 230); d.Save("01_verein_erster_wm_tag");
            }
            d.PlayMatchDay();
        }
        d.Move(10, 230); d.Save("02_bundestrainer_zug");
        d.Click(MainMenuScreen.BigTile(0).X + 10, MainMenuScreen.BigTile(0).Y + 10);
        d.Move(10, 230); d.Save("03_bundestrainer_manager_gesperrt");
        d.Click(MainMenuScreen.BigTile(1).X + 10, MainMenuScreen.BigTile(1).Y + 10);
        d.Move(10, 230); d.Save("04_bundestrainer_mannschaft");
        d.OpenTile("Transfer");
        d.Move(5, 120); d.Save("05_bundestrainer_wm_kader");
        d.Press(Key.Escape);
        d.PlayMatchDay();
        d.Move(10, 230); d.Save("06_verein_naechster_wm_tag");
    }
}
