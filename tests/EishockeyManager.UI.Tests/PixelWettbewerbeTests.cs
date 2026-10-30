using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens.Competitions;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur pixel-wettbewerbe (Runde 6): Befunde aus dem Pixelvergleich der Wettbewerbsbildschirme mit den neuen Referenzbildern
/// (Abläufe <c>pixel-*</c> in <c>Tools/Scenarios/WettbewerbeVideo.cs</c>, Kennzahlen in <c>docs/wettbewerbe.md</c>).
/// </summary>
public class PixelWettbewerbeTests
{
    private static (ScreenDriver Driver, int FrameY) DebCupOverview(int season)
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        d.Host.Game!.Season = season;
        d.OpenTile("Tabelle DEB-Pokal");
        d.Frame();
        var screen = Assert.IsType<DebCupGamesScreen>(d.Host.Current);
        var overview = screen.Overview!;
        var (_, y) = DebCupGamesScreen.FrameFor(overview.Groups.Sum(g => g.Rows.Count), overview.Groups.Count(g => g.Rows.Count == 0));
        return (d, y);
    }

    /// <summary>Erste Spalte mit Schrift der Datumszeile (Farbe 30, Grundlinie y + 34, Flow 8 Pixel hoch).</summary>
    private static int HeadingLeft(ScreenDriver d, int frameY)
    {
        for (var x = 0; x < Surface.Width; x++)
            for (var y = frameY + 27; y <= frameY + 34; y++)
                if (d.Surface.Pixels[y * Surface.Width + x] == Colors.TileBackground)
                    return x;
        return -1;
    }

    /// <summary>
    /// Befund PW-01: In der ersten Runde ab der zweiten Saison zeigt das Original nur das Datum („22.9.“ zentriert, duell_s2a_01217,
    /// folge13_02545), in der ersten Saison „Erste Runde    22.9.“ (oberflaeche_0400). Grund: Namensindex DGROUP 0x7AD steht beim
    /// Programmstart auf 1 und wird beim Saisonwechsel gelöscht (0x565E0); erst die Auslosung der Runden 2–5 setzt ihn (0x348E2).
    /// Vorher stand im Remake immer „Erste Runde“.
    /// </summary>
    [GameDataFact]
    public void DebCupOverview_FirstRound_NameOnlyInFirstSeason()
    {
        var (first, y1) = DebCupOverview(GameState.FirstSeason);
        var (later, y2) = DebCupOverview(GameState.FirstSeason + 1);

        // Video: „Erste Runde“ beginnt bei x = 101 (oberflaeche_0400), das Datum allein bei x = 150 (duell_s2a_01217)
        Assert.InRange(HeadingLeft(first, y1), 99, 103);
        Assert.InRange(HeadingLeft(later, y2), 148, 152);
    }

    /// <summary>
    /// Befund PW-02: Die Gruppenüberschriften stehen im Original in Großbuchstaben („BUNDESLIGA GEGEN BUNDESLIGA:“) in allen
    /// Videobildern der Übersicht; das Remake zeichnete die gemischt geschriebenen DGROUP-Texte („Bundesliga GEGEN Bundesliga:“).
    /// Prüfung: Die Großschrift noch einmal an dieselbe Stelle gezeichnet ändert kein Pixel.
    /// </summary>
    [GameDataFact]
    public void DebCupOverview_GroupHeadingsInCapitals()
    {
        var (d, y) = DebCupOverview(GameState.FirstSeason);
        var before = d.Surface.Pixels.ToArray();
        CompetitionWidgets.Centered(d.Surface, d.Host.Assets.Mini, 15, 183, y + 46, "BUNDESLIGA GEGEN BUNDESLIGA:", Colors.White, Colors.Black);
        Assert.Equal(before, d.Surface.Pixels);
    }
}
