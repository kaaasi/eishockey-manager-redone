using EishockeyManager.UI;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.Tools.Scenarios;

/// <summary>
/// Spur pixel-verein (Runde 6): Vereins- und Kachelbildschirme im Zustand der neuen Referenzbilder
/// (Reihe „Eishockey Manager EM“, Folge 1: Manager „Kingsday d.2“ mit dem Berliner SC, Saisonbeginn 1993).
/// Bildnamen nennen das Vorlagebild (<c>docs/referenz-videos.md</c>).
/// </summary>
internal static class PixelVerein
{
    private const string VideoManager = "Kingsday d.2";

    /// <summary>Kopf des Managers im Video (Vergleich der Kopfzellen mit folge01_00600: Kopf 4).</summary>
    private const int VideoHead = 4;

    /// <summary>Berliner SC (wie in <see cref="ScreenDriver.StartNewGame"/>).</summary>
    private const int VideoClub = 42;

    /// <summary>
    /// Spielstart wie im Video: Spiel-Level 5 (Kontostand 400.000 DM, folge01_00510;
    /// <c>docs/finanzen-rekonstruktion.md</c>), sonst wie <see cref="ScreenDriver.StartNewGame"/>.
    /// </summary>
    private static void StartLikeVideo(ScreenDriver d)
    {
        d.Host.Show(new TitleScreen(d.Host));
        d.Frame();
        d.Click(160, 120);
        d.Click(20, 60);
        var cell = SetupScreen.HeadCell(VideoHead);
        d.Click(cell.X + 5, cell.Y + 5);
        d.Type(VideoManager);
        d.Press(Key.Enter);
        d.Click(150, 60);
        for (var p = 0; p < VideoClub / SetupScreen.ClubsPerPage; p++)
            d.Wheel(-1);
        var c = SetupScreen.ClubCell(VideoClub % SetupScreen.ClubsPerPage);
        d.Click(c.X + 5, c.Y + 5);
        // Spielmodus „nur Fahne“: in folge01 und im Duell sind die WM-Kacheln gesperrt (folge01_00526,
        // duell_s2a_01203), anders als in der Eisbären-Reihe mit beiden Knöpfen (eisbaeren_f01_01431).
        d.Click(SetupScreen.ModeGlobeHit.X + 20, SetupScreen.ModeGlobeHit.Y + 7);
        var down = SetupScreen.ArrowDown(0);
        d.Click(down.X + 3, down.Y + 3);
        d.Click(down.X + 3, down.Y + 3);
        d.Click(SetupScreen.StartButton.X + 10, SetupScreen.StartButton.Y + 10);
        if (d.Host.Current is UI.Screens.Competitions.DebCupDrawLiveScreen)
            d.Press(Key.Escape); // „Kein Gedanke“
        if (d.Host.Current is not MainMenuScreen)
            throw new InvalidOperationException("Spielstart fehlgeschlagen.");
        if (d.Host.Game?.ActiveManager.Level != 5)
            throw new InvalidOperationException("Spiel-Level 5 nicht eingestellt.");
        if (d.Host.Game.Mode != Core.GameMode.ClubOnly)
            throw new InvalidOperationException("Spielmodus „nur Fahne“ nicht eingestellt.");
    }

    /// <summary>
    /// Spielmodus „Liga + Weltkugel“ (beide Knöpfe, Eisbären-Reihe): Kachelreihen mit offenen WM-Kacheln.
    /// Infoleiste und Kopf weichen ab (anderer Manager, Verein „Eisbären Berlin“, 2. Spieltag) und werden maskiert.
    /// </summary>
    [Scenario("pixel-verein-weltkugel")]
    private static void WorldRows(ScreenDriver d)
    {
        d.StartNewGame();
        if (d.Host.Game?.Mode != Core.GameMode.Full)
            throw new InvalidOperationException("Spielmodus „Liga + Weltkugel“ erwartet.");
        (int Big, string Name)[] rows = [(5, "staerken_eisbaeren_f01_01430"), (4, "paarungen_eisbaeren_f01_01431"),
            (2, "statistiken_eisbaeren_f01_01011")];
        var n = 1;
        foreach (var (big, name) in rows)
        {
            var b = MainMenuScreen.BigTile(big);
            d.Click(b.X + 10, b.Y + 10); d.Move(1, 239); d.Save($"{n++:00}_{name}");
        }
    }

    /// <summary>
    /// Modus 4 (Eisbären-Reihe, Folge 24): Menü des Bundestrainers am 14. April mit den Kachelreihen Manager,
    /// Mannschaft, Statistiken und Tabellen. Wie <c>bundestrainer-modus4</c>: Düsseldorfer EG (Index 2), Saison im
    /// Schnelldurchlauf bis zum ersten Zug des Bundestrainers. Infoleiste, Kopf und Gegner weichen ab (anderer Verlauf).
    /// </summary>
    [Scenario("pixel-verein-bundestrainer")]
    private static void CoachRows(ScreenDriver d)
    {
        d.StartNewGame(clubIndex: 2);
        var game = d.Host.Game!;
        for (var guard = 0; !game.NationalTurn; guard++)
        {
            if (guard > 400 || game.Phase is not (Core.SeasonPhase.Running or Core.SeasonPhase.ClubSeasonOver))
                throw new InvalidOperationException("Kein Zug des Bundestrainers erreicht.");
            d.PlayMatchDay();
        }
        d.Move(1, 239); d.Save("01_menue_eisbaeren_f24_00040");
        (int Big, string Name)[] rows = [(0, "manager_eisbaeren_f24_00049"), (1, "mannschaft_eisbaeren_f24_00051"),
            (2, "statistiken_eisbaeren_f24_00053"), (3, "tabellen_eisbaeren_f24_00057")];
        var n = 2;
        foreach (var (big, name) in rows)
        {
            var b = MainMenuScreen.BigTile(big);
            d.Click(b.X + 10, b.Y + 10); d.Move(1, 239); d.Save($"{n++:00}_{name}");
        }
    }

    [Scenario("pixel-verein")]
    private static void Run(ScreenDriver d)
    {
        StartLikeVideo(d);

        // Hauptmenü am Spielbeginn mit der Reihe „Gemischtes“: folge01_00600 (Mauszeiger maskieren)
        var mixed = MainMenuScreen.BigTile(6);
        d.Click(mixed.X + 10, mixed.Y + 10); d.Move(1, 239); d.Save("01_hauptmenue_folge01_00600");

        // Kachelreihen
        var stats = MainMenuScreen.BigTile(2);
        d.Click(stats.X + 10, stats.Y + 10); d.Move(1, 239); d.Save("02_statistiken_folge01_00526");
        var tables = MainMenuScreen.BigTile(3);
        d.Click(tables.X + 10, tables.Y + 10); d.Move(1, 239); d.Save("03_tabellen_duell_s2a_01203");
        d.Click(mixed.X + 10, mixed.Y + 10); d.Move(1, 239); d.Save("04_gemischtes_duell_s2a_00135");

        // Finanzen (Bank) zu Spielbeginn: folge01_00508
        d.OpenTile("Finanzen"); d.Move(1, 239); d.Save("05_finanzen_folge01_00508");
        d.Press(Key.Escape);

        // Aufstellung zu Spielbeginn: folge01_00528
        d.OpenTile("Aufstellung"); d.Move(1, 239); d.Save("06_aufstellung_folge01_00528");
        d.Press(Key.Escape);

        // Taktik zu Spielbeginn: folge10_00300
        d.OpenTile("Taktik"); d.Move(1, 239); d.Save("07_taktik_folge10_00300");
    }
}
