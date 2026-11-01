using EishockeyManager.Core;
using EishockeyManager.UI;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.Tools.Scenarios;

/// <summary>
/// Spur wettbewerbe-komplett Teil 1: Tabellenansicht (em.exe 0x4F78D) nach dem 1. Spieltag der zweiten Saison zum
/// Pixelvergleich mit <c>folge06_02238</c> („TABELLE Gesamt 1.Spieltag“, 2. Liga, Berliner SC nach dem Aufstieg).
/// Prüft Titelzahl nach dem Saisonwechsel (0x4F986) und den Aufbau; Namen und Werte hängen am Spielverlauf.
/// </summary>
internal static class WettbewerbeTabellen
{
    [Scenario("tabelle-saison2")]
    private static void SecondSeason(ScreenDriver d)
    {
        d.StartNewGame(); // Berliner SC wie im Video
        var game = d.Host.Game!;
        for (var i = 0; i < 5000 && !(game.Season > GameState.FirstSeason && game.PlayedMatchDays >= 1
                                      && d.Host.Current is MainMenuScreen); i++)
            d.Advance();
        if (game.Season == GameState.FirstSeason || game.PlayedMatchDays < 1)
            throw new TimeoutException("Erster Spieltag der zweiten Saison nicht erreicht.");
        d.OpenTile("Tabelle 2. Liga");
        d.Move(118, 214); d.Save("00_tabelle_2liga_saison2_1_spieltag");
        d.Press(Key.Escape);
    }
}
