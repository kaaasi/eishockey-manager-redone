using EishockeyManager.Tests.Support;
using EishockeyManager.UI;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Competitions;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur pixel-verein (Runde 6): Abweichungen der Vereins- und Kachelbildschirme von den neuen Referenzbildern
/// (Vermessung und Bildnamen in <c>docs/anforderungen/bildschirme.md</c>, Abschnitt „Pixelvergleich Runde 6“).
/// </summary>
public class PixelVereinTests
{
    /// <summary>
    /// Befund PV-01: Hinweis der Kopfwahl. [ORIGINAL] em.exe Datei 0x28CF8–0x28D43: Aufruf 0x30:0x885 mit x 188 … 319,
    /// Grundlinie 214 bzw. 222, Farbe 0, ohne Schatten (0xFF). Bisher Grundlinie 2 bzw. 3 Pixel tiefer, Zeilenabstand 7
    /// statt 8 und Mitte 252 statt 253 (Vorlage speedrun_t1_00056).
    /// </summary>
    [GameDataFact]
    public void HeadPicker_Hint_SitsOnOriginalBaselines()
    {
        var d = FlowTests.CreateDriver();
        d.Host.Show(new TitleScreen(d.Host));
        d.Frame();
        d.Click(160, 120);
        d.Click(20, 60);
        d.Move(1, 239);
        Assert.IsType<SetupScreen>(d.Host.Current);

        var expected = new Surface();
        expected.Fill(new Rect(188, 200, 132, 30), 255);
        var mini = d.Host.Assets.Mini;
        CompetitionWidgets.Centered(expected, mini, 188, 319, 214, "Suchen Sie sich einen", Colors.Black, null);
        CompetitionWidgets.Centered(expected, mini, 188, 319, 222, "passenden Kopf aus.", Colors.Black, null);

        for (var y = 200; y < 230; y++)
        for (var x = 188; x < 320; x++)
        {
            var text = expected.Pixels[y * Surface.Width + x] == Colors.Black;
            var drawn = d.Surface.Pixels[y * Surface.Width + x] == Colors.Black;
            Assert.True(text == drawn, $"Hinweistext weicht bei ({x} | {y}) ab");
        }
    }

    /// <summary>
    /// Befund PV-02: Kopfzeile der Aufstellung. [ORIGINAL] em.exe Datei 0x424DF–0x42525: Text 0x30:0x831 auf der
    /// Grundlinie y, Linie 0x30:0x995 bei y + 2 – zwischen Schrift und Linie bleibt eine Zeile frei (Vorlagen
    /// folge01_00528, folge01_00550: Großbuchstaben 19–23, Linie 25). Bisher lag die Schrift eine Zeile tiefer und
    /// berührte die Linie.
    /// </summary>
    [GameDataFact]
    public void Lineup_Header_LeavesOneRowAboveTheLine()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        d.OpenTile("Aufstellung");
        d.Move(1, 239);
        var mini = d.Host.Assets.Mini;
        var baseline = Screens.Team.LineupScreen.HeaderBaseline;
        Assert.Equal(25, baseline + 2);

        bool RowHasHeader(int y)
        {
            for (var x = 6; x < 60; x++)
                if (d.Surface.Pixels[y * Surface.Width + x] == Screens.Team.TeamUi.HeaderColor)
                    return true;
            return false;
        }

        Assert.True(RowHasHeader(baseline), "Großbuchstaben enden auf der Grundlinie");
        Assert.False(RowHasHeader(baseline + 1), "Zeile zwischen Schrift und Linie ist frei");
        Assert.True(RowHasHeader(CompetitionWidgets.Top(mini, baseline)), "Kopf der Großbuchstaben");
    }

    /// <summary>Lampe i der Infoleiste rot (Quelle kb\3 wie im Original, Ziel (226 | 33 + 7 · i))?</summary>
    internal static bool LampRed(ScreenDriver d, int i)
    {
        int x = 226, y = 33 + 7 * i;
        var red = new Surface();
        Widgets.Lamp(red, d.Host.Assets, x, y, red: true);
        var green = new Surface();
        Widgets.Lamp(green, d.Host.Assets, x, y, red: false);
        int redHits = 0, greenHits = 0;
        for (var yy = y; yy < y + 6; yy++)
        for (var xx = x; xx < x + 6; xx++)
        {
            var p = yy * Surface.Width + xx;
            if (red.Pixels[p] == green.Pixels[p])
                continue;
            if (d.Surface.Pixels[p] == red.Pixels[p]) redHits++;
            if (d.Surface.Pixels[p] == green.Pixels[p]) greenHits++;
        }
        Assert.True(redHits != greenHits, $"Lampe {i} weder eindeutig rot noch grün");
        return redHits > greenHits;
    }

    private static bool[] Lamps(ScreenDriver d) => Enumerable.Range(0, 6).Select(i => LampRed(d, i)).ToArray();

    /// <summary>
    /// Befund PV-03: Die Lampen sind Warnlampen des Managers am Zug (em.exe Datei 0x42996–0x42CA6), bei (226 | 33 + 7 · i).
    /// Spielbeginn: nur Lampe 4 (freie Werbeflächen) rot – folge01_00600, duell_s2a_00135. Bisher: Lampe 0 rot
    /// („Manager noch nicht bereit“), zwei Pixel zu hoch.
    /// </summary>
    [GameDataFact]
    public void Lamps_AtGameStart_OnlyFreeAdSpaceIsRed()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        d.Move(1, 239);
        Assert.Equal([false, false, false, false, true, false], Lamps(d));
    }

    /// <summary>PV-03: Lampe 0 wird rot, sobald eine der Reihen 1–3 nicht voll ist; Lampe 3 bei Trainingskosten.</summary>
    [GameDataFact]
    public void Lamps_FollowLineupAndTrainingCosts()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        var club = game.Clubs[game.ActiveManager.ClubIndex];

        club.ClearLineupSlot(Core.LineupSlot.Forward(2, 0));
        d.Move(1, 239);
        Assert.True(LampRed(d, 0));

        club.AutoLineup();
        game.ActiveManager.Training[0, 0] = Core.TrainingKind.Special;
        d.Move(1, 239);
        Assert.False(LampRed(d, 0));
        Assert.True(LampRed(d, 3));
    }

    /// <summary>Zeigt die Infoleiste an (x | y) genau den Ausschnitt <paramref name="source"/> aus kb\3?</summary>
    private static bool Shows(ScreenDriver d, Rect source, int x, int y)
    {
        var expected = new Surface();
        expected.Blit(d.Host.Assets.Image(MainMenuScreen.TileSheet), source, x, y);
        var other = new Surface();
        other.Clear(1);
        other.Blit(d.Host.Assets.Image(MainMenuScreen.TileSheet), source, x, y);
        for (var yy = y; yy < y + source.H; yy++)
        for (var xx = x; xx < x + source.W; xx++)
        {
            var p = yy * Surface.Width + xx;
            if (expected.Pixels[p] == other.Pixels[p] && d.Surface.Pixels[p] != expected.Pixels[p])
                return false; // deckendes Pixel des Symbols fehlt
        }
        return true;
    }

    private static readonly Rect HomeIcon = new(54, 231, 12, 11), AwayIcon = new(66, 231, 12, 11);

    /// <summary>
    /// Befund PV-04: Infoleiste des Bundestrainers vor einem WM-Spiel – Symbol „WM“ (Weltkugel), kein Heim-/Auswärtssymbol.
    /// Belege: Modus „nur Weltkugel“ bundestrainer_00030, _00101; Modus 4 eisbaeren_f24_00039, _00115 (14. April). Bisher
    /// zeigte die Leiste im Modus 4 die Zielflagge „Saisonende“ und in beiden Modi das Heim-/Auswärtssymbol.
    /// </summary>
    [GameDataFact]
    public void CoachInfoBar_BeforeWorldCupMatch_ShowsGlobeWithoutVenue()
    {
        // Modus 4: Saison bis zum ersten Zug des Bundestrainers
        var d = FlowTests.CreateDriver();
        d.StartNewGame(clubIndex: 2);
        var game = d.Host.Game!;
        for (var guard = 0; !game.NationalTurn; guard++)
        {
            Assert.True(guard < 400, "Kein Zug des Bundestrainers erreicht.");
            d.PlayMatchDay();
        }
        d.Move(1, 239);
        Assert.Equal(Core.Competition.WorldCup, game.UpcomingMatch(Screens.Competitions.NationalMenu.Team(game))?.Competition);
        Assert.True(Shows(d, SeasonInfo.IconSource(Core.Competition.WorldCup), 111, 35), "WM-Symbol fehlt (Modus 4)");
        Assert.False(Shows(d, HomeIcon, 126, 35) || Shows(d, AwayIcon, 126, 35), "Heim-/Auswärtssymbol bei einem WM-Spiel (Modus 4)");
    }

    /// <summary>PV-04 im Modus „nur Weltkugel“: erster WM-Termin wie bundestrainer_00030.</summary>
    [GameDataFact]
    public void CoachInfoBar_WorldOnly_ShowsGlobeWithoutVenue()
    {
        var d = FlowTests.CreateDriver();
        d.Host.Show(new TitleScreen(d.Host));
        d.Frame();
        d.Click(160, 120);
        d.Click(20, 60);
        var cell = SetupScreen.HeadCell(3);
        d.Click(cell.X + 5, cell.Y + 5);
        d.Type("LetsShow");
        d.Press(Key.Enter);
        d.Click(SetupScreen.ModeFlag.X + 5, SetupScreen.ModeFlag.Y + 5);
        d.Click(SetupScreen.StartButton.X + 10, SetupScreen.StartButton.Y + 10);
        Assert.Equal(Core.GameMode.WorldOnly, d.Host.Game!.Mode);
        d.PlayMatchDay();
        d.Move(1, 239);
        Assert.True(Shows(d, SeasonInfo.IconSource(Core.Competition.WorldCup), 111, 35), "WM-Symbol fehlt");
        Assert.False(Shows(d, HomeIcon, 126, 35) || Shows(d, AwayIcon, 126, 35), "Heim-/Auswärtssymbol bei einem WM-Spiel");
    }
}
