using EishockeyManager.Core;
using EishockeyManager.UI;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Competitions;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.Tools.Scenarios;

/// <summary>Spur tabellen-highscore: Pokaltabellen (Hauptmenü → Tabellen) und Bestenliste.</summary>
internal static class TabellenHighscore
{
    /// <summary>
    /// „Tabelle Europapokal“: gleicher Bildschirm wie bei den Paarungen (em.exe 0x45829 ff.), zu Saisonbeginn und nach
    /// den ersten Terminen. „Tabelle DEB-Pokal“ zeigt die Übersicht „DEB-Pokalspiele“ (Datei 0x32E23, Issue #108) vor und
    /// nach dem ersten Pokaltermin.
    /// </summary>
    [Scenario("tabellen-pokale")]
    private static void CupTables(ScreenDriver d)
    {
        d.StartNewGame(); // Berliner SC wie im Video
        var game = d.Host.Game!;

        d.OpenTile("Tabelle DEB-Pokal");
        d.Move(240, 230); d.Save("00_tabelle_deb_pokal_vor_dem_ersten_termin");
        d.Press(Key.Escape);

        d.OpenTile("Tabelle Europapokal");
        d.Move(240, 230); d.Save("02_tabelle_europapokal_gruppe1");
        d.Press(Key.Escape);

        while (game.DebCup!.Rounds[0].All(t => t.Games.Count == 0) && game.Phase == SeasonPhase.Running)
            d.PlayMatchDay();
        if (d.Host.Current is not MainMenuScreen)
            d.Host.Show(new MainMenuScreen(d.Host));
        d.OpenTile("Tabelle DEB-Pokal");
        d.Move(240, 230); d.Save("01_tabelle_deb_pokal_nach_termin_1");
        d.Press(Key.Escape);

        while (game.EuropeanCup!.Groups[0].Results.Count == 0 && game.Phase == SeasonPhase.Running)
            d.PlayMatchDay();
        if (d.Host.Current is not MainMenuScreen)
            d.Host.Show(new MainMenuScreen(d.Host));
        d.OpenTile("Tabelle Europapokal");
        d.Move(240, 230); d.Save("03_tabelle_europapokal_gespielt");
        d.Press(Key.Escape);
    }

    /// <summary>
    /// „Highscore“ wie im Video „Die Oberfläche von EM“ (8:40–8:55, <c>oberflaeche_0848</c>): zwei Einträge mit festen Werten,
    /// dazu die Liste nach einer Saison im laufenden Spiel.
    /// </summary>
    [Scenario("highscore")]
    private static void Highscore(ScreenDriver d)
    {
        var table = new HighscoreTable();
        table.Insert(new HighscoreEntry("LetsShow", "BERLINER SC", 0, 0, 0, 446, 1));
        table.Insert(new HighscoreEntry("UltraSchwer", "SC PREUSSEN BERLIN", 0, 0, 0, 384, 1));
        d.Host.Show(new EishockeyManager.UI.Screens.Statistics.HighscoreScreen(d.Host, new TitleScreen(d.Host), table));
        d.Move(240, 230); d.Save("01_highscore_video");

        d.StartNewGame();
        d.OpenTile("Highscore");
        d.Move(240, 230); d.Save("02_highscore_spielbeginn");
        d.Press(Key.Escape);
    }
}
