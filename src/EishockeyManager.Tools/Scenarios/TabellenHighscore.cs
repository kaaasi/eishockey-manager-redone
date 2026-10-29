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
    /// „Tabelle DEB-Pokal“ und „Tabelle Europapokal“: gleiche Bildschirme wie bei den Paarungen (em.exe 0x45829 ff.),
    /// zu Saisonbeginn und nach den ersten Terminen.
    /// </summary>
    [Scenario("tabellen-pokale")]
    private static void CupTables(ScreenDriver d)
    {
        d.StartNewGame(); // Berliner SC wie im Video
        var game = d.Host.Game!;

        d.OpenTile("Tabelle DEB-Pokal");
        d.Frame(); d.Save("01_tabelle_debpokal_runde1");
        d.Press(Key.Space);
        d.OpenTile("Tabelle Europapokal");
        d.Move(240, 230); d.Save("02_tabelle_europapokal_gruppe1");
        d.Press(Key.Escape);

        while (game.EuropeanCup!.Groups[0].Results.Count == 0 && game.Phase == SeasonPhase.Running)
            d.PlayMatchDay();
        if (d.Host.Current is not MainMenuScreen)
            d.Host.Show(new MainMenuScreen(d.Host));
        d.OpenTile("Tabelle Europapokal");
        d.Move(240, 230); d.Save("03_tabelle_europapokal_gespielt");
        d.Press(Key.Escape);
        d.OpenTile("Tabelle DEB-Pokal");
        d.Frame(); d.Save("04_tabelle_debpokal_spaeter");
        d.Press(Key.Space);
    }
}
