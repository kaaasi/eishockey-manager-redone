using EishockeyManager.UI.Testing;

namespace EishockeyManager.Tools.Scenarios;

/// <summary>Spur ui-meldungen, Teil 2: Tabellenkurve der Infoleiste und Infofile (versteckter Klick auf 319 | 0).</summary>
internal static class InfofileKurve
{
    [Scenario("infofile-kurve")]
    private static void All(ScreenDriver d)
    {
        d.StartNewGame();
        d.Frame();
        d.Save("01_vor_dem_ersten_spieltag");
        for (var i = 0; i < 12; i++)
            d.PlayMatchDay();
        d.Frame();
        d.Save("02_kurve");
        d.Click(319, 0);
        d.Move(160, 120);
        d.Save("03_infofile_seite1");
        d.Click(160, 120);
        d.Move(161, 120);
        d.Save("04_infofile_seite2");
    }
}
