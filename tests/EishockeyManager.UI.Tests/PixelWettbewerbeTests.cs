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

    /// <summary>Gruppe a–d (Vereine 64–67) mit den angegebenen Spielen; Ergebnisse „links:rechts“ wie im Video.</summary>
    private static (ScreenDriver Driver, CupGroup Group) EuropeanGroup(params (int Round, int Leg, int Left, int Right)[] games)
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var group = new CupGroup(1, [64, 65, 66, 67]);
        var dates = SeasonCalendar.DaysOf(DayEvents.EuropeanCup);
        foreach (var (round, leg, left, right) in games)
        {
            var (home, away) = group.Pairings(round)[0];
            group.Results.Add(leg == 0
                ? new KnockoutMatch(dates[round], home, away, left, right)
                : new KnockoutMatch(dates[round + 3], away, home, right, left));
        }
        d.Host.Show(new EuropeanCupScreen(d.Host, new Screens.MainMenuScreen(d.Host), 0, group));
        d.Frame();
        return (d, group);
    }

    /// <summary>
    /// Befund PW-03: Ergebnisse stehen im Original wie die Paarung „links:rechts“ – auch das Rückspiel, in dem der rechte Verein
    /// Heimrecht hat (folge04_01241: Chamonix – Dynamo „5: 6  2: 6“, Dynamo mit 12:0 Punkten; ebenso eisbaeren_f58_01323) – und mit
    /// zweistelligen Zahlen wie die Tabelle („5: 6“, Ziffer bei x = 230 bzw. 276). Vorher: Rückspiel „Heim:Gast“, ohne Auffüllen.
    /// </summary>
    private static string Result(int left, int right) => CompetitionWidgets.Number(left, 2) + ":" + CompetitionWidgets.Number(right, 2);

    [GameDataFact]
    public void EuropeanCup_ResultsLeftToRight_Padded()
    {
        var (d, _) = EuropeanGroup((0, 0, 5, 6), (0, 1, 2, 6));
        var before = d.Surface.Pixels.ToArray();
        var mini = d.Host.Assets.Mini;
        CompetitionWidgets.Left(d.Surface, mini, 226, EuropeanCupScreen.PairingBaseline, Result(5, 6), Colors.TileBackground);
        CompetitionWidgets.Left(d.Surface, mini, 272, EuropeanCupScreen.PairingBaseline, Result(2, 6), 1);
        Assert.Equal(before, d.Surface.Pixels);
    }

    /// <summary>
    /// Befund PW-04: Sobald in der Gruppe gespielt wurde, zeigt das Original keine Termine mehr, auch nicht für noch offene Rückspiele
    /// (eisbaeren_f51_03346 nach den drei Hinspielen, eisbaeren_f58_01323 nach vier Terminen); vorher stand das Datum des Rückspiels.
    /// </summary>
    [GameDataFact]
    public void EuropeanCup_NoDatesOnceGroupHasResults()
    {
        var (d, _) = EuropeanGroup((0, 0, 4, 3), (1, 0, 1, 1), (2, 0, 3, 3));
        for (var round = 0; round < 3; round++)
        {
            var y = EuropeanCupScreen.PairingBaseline + 26 * round;
            for (var x = 268; x < 310; x++)
                for (var py = y - 6; py <= y; py++)
                    Assert.NotEqual((byte)1, d.Surface.Pixels[py * Surface.Width + x]);
        }
    }
}
