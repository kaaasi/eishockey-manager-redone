using EishockeyManager.Core;
using EishockeyManager.UI;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Competitions;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.Tools.Scenarios;

/// <summary>Paket „wettbewerbe“: DEB-Pokal-Auslosung (und später Europapokal, WM, Statistiken).</summary>
internal static class Wettbewerbe
{
    /// <summary>
    /// Ziehung aus dem Video „Die Oberfläche von EM“ (3:55), links gegen rechts, Zeile für Zeile – für den
    /// Pixelvergleich mit <c>oberflaeche_0355.png</c>.
    /// </summary>
    public static readonly string[] VideoDraw =
    [
        "SC PREUSSEN BERLIN", "EC BAD T", "BERLINER SC", "EA KEMPTEN", "KREFELDER EV", "MANNHEIMER ERC",
        "ECD SAUERLAND", "TSV ERDING", "SC RIESSERSEE", "EC BAD NAUHEIM", "EC HEDOS M", "ERC SCHWENNINGEN",
        "EC HARZ BRAUNLAGE", "EHC FREIBURG", "SV BAYREUTH", "EHC 80 N", "ESV KAUFBEUREN", "LNER EC",
        "ES WEI", "EV LANDSBERG", "SB ROSENHEIM", "SC MEMMINGEN", "HEC HEILBRONN", "EC RATINGEN",
        "ESC WOLFSBURG", "EISB", "ESC FRANKFURT", "ETC TIMMENDORF", "EC HANNOVER", "ESC WEDEMARK",
        "AUGSBURGER EV", "EV LANDSHUT",
    ];

    [Scenario("wettbewerbe")]
    private static void Competitions(ScreenDriver d)
    {
        d.StartNewGame(); // Berliner SC wie im Video
        var game = d.Host.Game!;

        // Paarungen → DEB-Pokal: erste Runde der laufenden Saison
        d.OpenTile("DEB-Pokal");
        d.Frame(); d.Save("01_debpokal_auslosung");
        d.Press(Key.Space);

        // Gleiche Ziehung wie im Video (3:55) für den Pixelvergleich
        var used = new HashSet<int>();
        var slots = VideoDraw.Select(prefix =>
        {
            var club = game.Clubs.First(c => c.Index < 48 && !used.Contains(c.Index) && c.Name.Contains(prefix, StringComparison.Ordinal));
            used.Add(club.Index);
            return (int?)club.Index;
        }).ToList();
        d.Host.Show(new DebCupDrawScreen(d.Host, new MainMenuScreen(d.Host), slots));
        d.Frame(); d.Save("02_debpokal_video_3_55");

        // Leere Ziehung: grüne Streifen wie vor der ersten Kugel
        d.Host.Show(new DebCupDrawScreen(d.Host, new MainMenuScreen(d.Host), Enumerable.Repeat<int?>(null, DebCup.Teams).ToList()));
        d.Frame(); d.Save("03_debpokal_leer");
        d.Press(Key.Space);

        // Nach dem dritten Termin der ersten Runde: Auslosung der zweiten Runde
        while (game.DebCup!.CurrentRound < 1 && game.Phase == SeasonPhase.Running)
            d.PlayMatchDay();
        if (d.Host.Current is not MainMenuScreen)
            d.Host.Show(new MainMenuScreen(d.Host));
        d.OpenTile("DEB-Pokal");
        d.Frame(); d.Save("04_debpokal_runde2");
    }
}
