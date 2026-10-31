using EishockeyManager.Core;
using EishockeyManager.UI;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Competitions;
using EishockeyManager.UI.Screens.Statistics;
using EishockeyManager.UI.Screens.Team;
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
        d.Host.Show(new DebCupDrawScreen(d.Host, new MainMenuScreen(d.Host))); // Kachel zeigt seit R4-01 die Übersicht
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
        d.Host.Show(new DebCupDrawScreen(d.Host, new MainMenuScreen(d.Host)));
        d.Frame(); d.Save("04_debpokal_runde2");
        d.Press(Key.Space);
    }

    /// <summary>Europapokal: Gruppen vor dem ersten Termin (Video 13:11), nach der Gruppenphase, Endrunde.</summary>
    [Scenario("europapokal")]
    private static void European(ScreenDriver d)
    {
        d.StartNewGame(); // Berliner SC wie im Video
        var game = d.Host.Game!;
        // Gruppe aus dem Video (13:11) für den Pixelvergleich
        var video = new[] { "JYP HT", "SSELDORFER EG", "VITKOVICE", "LION MAILAND" }
            .Select(n => game.Clubs.First(c => c.Name.Contains(n, StringComparison.Ordinal)).Index).ToList();
        d.Host.Show(new EuropeanCupScreen(d.Host, new MainMenuScreen(d.Host), 0, new CupGroup(1, video)));
        d.Move(240, 230); d.Save("00_gruppe1_video_13_11");
        d.Host.Show(new MainMenuScreen(d.Host));

        d.OpenTile("Europapokal");
        d.Frame(); d.Save("01_gruppe1");
        d.Click(EuropeanCupScreen.LeftButton.X + 10, EuropeanCupScreen.LeftButton.Y + 10);
        d.Frame(); d.Save("02_gruppe2");
        d.Click(EuropeanCupScreen.RightButton.X + 10, EuropeanCupScreen.RightButton.Y + 10);
        d.Frame(); d.Save("03_endrunde_offen");
        d.Press(Key.Escape);

        while (game.EuropeanCup!.FinalRound is null && game.Phase == SeasonPhase.Running)
            d.PlayMatchDay();
        if (d.Host.Current is not MainMenuScreen)
            d.Host.Show(new MainMenuScreen(d.Host));
        d.OpenTile("Europapokal");
        d.Frame(); d.Save("04_gruppe1_gespielt");
        d.Click(EuropeanCupScreen.RightButton.X + 10, EuropeanCupScreen.RightButton.Y + 10);
        d.Frame(); d.Save("05_endrunde");
        d.Press(Key.Escape);
    }

    /// <summary>WM: Gruppe aus dem Video (13:13), laufende Gruppen, K.-o.-Phase nach der Vereinssaison.</summary>
    [Scenario("wm")]
    private static void World(ScreenDriver d)
    {
        d.StartNewGame();
        var game = d.Host.Game!;
        var video = new[] { "DEUTSCHLAND", "FRANKREICH", "FINNLAND", "SCHWEIZ", "USA", "RUSSLAND" }
            .Select(n => game.Clubs.First(c => c.Name == n).Index).ToList();
        d.Host.Show(new WorldCupScreen(d.Host, new MainMenuScreen(d.Host), 0, new CupGroup(1, video)));
        d.Move(300, 225); d.Save("00_gruppe1_video_13_13");
        d.Host.Show(new MainMenuScreen(d.Host));

        d.OpenTile("WM");
        d.Frame(); d.Save("01_gruppe1");
        d.Click(WorldCupScreen.GroupButton.X + 5, WorldCupScreen.GroupButton.Y + 5);
        d.Frame(); d.Save("02_gruppe2");
        d.Press(Key.Escape);

        while (game.Phase != SeasonPhase.SeasonOver && game.Phase != SeasonPhase.GameOver)
        {
            d.Advance();
            if (game.WorldCup!.Knockout.Count == 3)
                break;
        }
        d.Host.Show(new WorldCupScreen(d.Host, new MainMenuScreen(d.Host)));
        d.Frame(); d.Save("03_gruppe1_gespielt");
        d.Click(WorldCupScreen.KnockoutButton.X + 5, WorldCupScreen.KnockoutButton.Y + 5);
        d.Frame(); d.Save("04_endrunde");
    }

    /// <summary>Stärken: Länderteams und Oberliga wie im Video (13:18, 13:36), Ansichten Abwehr und Sturm.</summary>
    [Scenario("staerken")]
    private static void Strengths(ScreenDriver d)
    {
        d.StartNewGame(); // Berliner SC (Oberliga) wie im Video
        d.OpenTile("Stärken Länderteams");
        d.Move(5, 120); d.Save("01_laenderteams_gesamt");
        d.Press(Key.Escape);
        d.OpenTile("Stärken Oberliga");
        d.Move(5, 120); d.Save("02_oberliga_gesamt");
        d.Click(StrengthScreen.ButtonRect(1).X + 5, StrengthScreen.ButtonRect(1).Y + 5);
        d.Move(5, 120); d.Save("03_oberliga_abwehr");
        d.Click(StrengthScreen.ButtonRect(2).X + 5, StrengthScreen.ButtonRect(2).Y + 5);
        d.Move(5, 120); d.Save("04_oberliga_sturm");
        d.Press(Key.Escape);
        d.OpenTile("Stärken 1. Liga");
        d.Move(5, 120); d.Save("05_bundesliga_gesamt");
        d.Press(Key.Escape);
    }

    /// <summary>Statistiken nach der ersten Saison wie im Video (16:38–16:52): Ewige Tabelle oben und gescrollt, Früher.</summary>
    [Scenario("statistik")]
    private static void Statistics(ScreenDriver d)
    {
        d.StartNewGame(); // Berliner SC wie im Video
        var game = d.Host.Game!;
        d.OpenTile("Früher");
        d.Move(5, 120); d.Save("00_frueher_erste_saison");
        d.Press(Key.Escape);
        for (var guard = 0; game.History.Count == 0 && guard < 5000; guard++)
            d.Advance();
        for (var guard = 0; d.Host.Current is not MainMenuScreen && guard < 50; guard++)
            d.Advance();
        d.OpenTile("Totale");
        d.Move(5, 120); d.Save("01_ewige_tabelle");
        for (var i = 0; i < 30; i++)
            d.Press(Key.Down);
        d.Move(5, 120); d.Save("02_ewige_tabelle_unten");
        d.Press(Key.Escape);
        d.OpenTile("Früher");
        d.Move(5, 120); d.Save("03_frueher");
        d.Press(Key.Escape);
    }

    /// <summary>
    /// Bundestrainer (Spielmodus „nur Weltkugel“, kein Videobeleg): „Spieler in WM-Kader“ leer, nach „auto“ und
    /// geblättert; „WM Vorbereitung“ mit Infofeld, gewähltem Gegner, gespielten Freundschaftsspielen und Meldung.
    /// Der Spielmodus selbst folgt in einem eigenen PR; hier wird der Bundestrainer direkt ernannt.
    /// </summary>
    [Scenario("nationalteam")]
    private static void NationalTeam(ScreenDriver d)
    {
        d.StartNewGame();
        var game = d.Host.Game!;
        game.AppointNationalCoach(game.Managers[0]);
        var menu = new MainMenuScreen(d.Host);

        d.Host.Show(new NationalSquadScreen(d.Host, menu));
        d.Move(5, 120); d.Save("01_wm_kader_leer");
        d.Click(NationalSquadScreen.ButtonRect(1).X + 5, NationalSquadScreen.ButtonRect(1).Y + 5);
        d.Move(5, 120); d.Save("02_wm_kader_auto");
        d.Click(NationalSquadScreen.ButtonRect(2).X + 5, NationalSquadScreen.ButtonRect(2).Y + 5);
        d.Move(5, 120); d.Save("03_wm_kader_seite2");

        d.Host.Show(new FriendliesScreen(d.Host, menu));
        d.Move(5, 120); d.Save("04_wm_vorbereitung");
        var tile = FriendliesScreen.Tile(0);
        d.Move(tile.X + 20, tile.Y + 20); d.Save("05_wm_vorbereitung_info");
        d.Click(tile.X + 20, tile.Y + 20); d.Save("06_wm_vorbereitung_gewaehlt");
        game.Managers[0].Money = 5_000_000;
        var screen = (FriendliesScreen)d.Host.Current!;
        var others = Enumerable.Range(0, WorldCup.Teams).Where(i => screen.Nations[i] != game.NationalCoach!.Nation).ToList();
        for (var i = 0; i < NationalCoach.MaxFriendlies; i++)
        {
            var t = FriendliesScreen.Tile(others[i % others.Count]);
            d.Move(5, 120);
            d.Click(t.X + 20, t.Y + 20);
            d.Click(t.X + 20, t.Y + 20);
        }
        d.Move(5, 120); d.Save("07_wm_vorbereitung_acht_spiele");
        d.Move(tile.X + 20, tile.Y + 20);
        d.Click(tile.X + 20, tile.Y + 20);
        d.Click(tile.X + 20, tile.Y + 20);
        d.Save("08_wm_vorbereitung_meldung");
    }

    /// <summary>
    /// Spur wettbewerbe-weltkugel: Spielvorbereitung mit Spielmodus „nur Weltkugel“ und Hauptmenü des Bundestrainers
    /// (vor der Ernennung, nach dem Sprung zum ersten WM-Termin, Kachelsperren). Kein Videobeleg für Modus 1.
    /// </summary>
    [Scenario("weltkugel")]
    private static void WorldOnly(ScreenDriver d)
    {
        d.Host.Show(new TitleScreen(d.Host));
        d.Frame();
        d.Click(160, 120);
        d.Click(20, 60);
        var cell = SetupScreen.HeadCell(3);
        d.Click(cell.X + 5, cell.Y + 5);
        d.Type("LetsShow");
        d.Press(Key.Enter);
        d.Move(100, 150); d.Save("01_spielvorbereitung_beide");
        d.Click(SetupScreen.ModeFlag.X + 5, SetupScreen.ModeFlag.Y + 5);
        d.Move(100, 150); d.Save("02_spielvorbereitung_nur_weltkugel");
        d.Click(SetupScreen.ModeGlobe.X + 5, SetupScreen.ModeGlobe.Y + 5);
        d.Move(100, 150); d.Save("03_spielvorbereitung_kein_modus");
        d.Click(SetupScreen.ModeGlobe.X + 5, SetupScreen.ModeGlobe.Y + 5);
        d.Click(SetupScreen.StartButton.X + 10, SetupScreen.StartButton.Y + 10);
        d.Move(10, 230); d.Save("04_bundestrainer_saisonbeginn");
        d.Click(MainMenuScreen.BigTile(1).X + 10, MainMenuScreen.BigTile(1).Y + 10);
        d.Move(10, 230); d.Save("05_bundestrainer_mannschaft_gesperrt");
        d.PlayMatchDay();
        d.Move(10, 230); d.Save("06_bundestrainer_erster_wm_termin");
        d.Click(MainMenuScreen.BigTile(1).X + 10, MainMenuScreen.BigTile(1).Y + 10);
        d.Move(10, 230); d.Save("07_bundestrainer_mannschaft");
        d.Click(MainMenuScreen.BigTile(3).X + 10, MainMenuScreen.BigTile(3).Y + 10);
        d.Move(10, 230); d.Save("08_bundestrainer_tabellen");
        d.Click(MainMenuScreen.BigTile(0).X + 10, MainMenuScreen.BigTile(0).Y + 10);
        d.Move(10, 230); d.Save("09_bundestrainer_manager");
        d.OpenTile("Transfer");
        d.Move(5, 120); d.Save("10_bundestrainer_wm_kader");
    }

    /// <summary>
    /// Spur nationalteam-2: Aufstellung und Taktik des Nationalteams im Modus „nur Weltkugel“. Die Aufstellung wird
    /// wie im Video `bundestrainer_00056` belegt (gleiche Zeilen: TW 2, Blöcke 14 15 16/4 5, 17 18 20/6 7,
    /// 19 21 22/8 11, Reserve 1 3 9 10 25 26 – „Eingesetzt: 22“), damit Bild und Video Zeile für Zeile vergleichbar sind.
    /// </summary>
    [Scenario("nationalteam-2")]
    private static void NationalLineup(ScreenDriver d)
    {
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
        d.PlayMatchDay();
        var game = d.Host.Game!;
        game.AutoNominate();
        d.OpenTile("Aufstellung");
        d.Move(300, 235); d.Save("01_aufstellung_leer");

        var coach = game.NationalCoach!;
        Player Row(int number) => coach.Rows[number - 1];
        coach.Assign(LineupSlot.Goalkeeper, Row(2));
        int[][] forwards = [[14, 15, 16], [17, 18, 20], [19, 21, 22]];
        int[][] defenders = [[4, 5], [6, 7], [8, 11]];
        for (var b = 0; b < 3; b++)
        {
            for (var i = 0; i < 3; i++)
                coach.Assign(LineupSlot.Forward(b, i), Row(forwards[b][i]));
            for (var i = 0; i < 2; i++)
                coach.Assign(LineupSlot.Defender(b, i), Row(defenders[b][i]));
        }
        foreach (var reserve in new[] { 1, 3, 9, 10, 25, 26 })
            coach.Dress(Row(reserve));
        d.Move(300, 235); d.Save("02_aufstellung_wie_video_00056");

        d.Click(TeamUi.SwitchButton.X + 5, TeamUi.SwitchButton.Y + 5);
        d.Move(300, 235); d.Save("03_taktik_wie_video_00058");
    }

    /// <summary>
    /// Spur wettbewerbe-weltkugel: Hauptmenü im Spielmodus „nur Fahne“ mit gesperrten WM-Kacheln – für den
    /// Pixelvergleich mit dem Karriere-Video (Tabellen: <c>karriere_00443</c>, Stärken: <c>karriere_03730</c>).
    /// </summary>
    [Scenario("nurfahne")]
    private static void ClubOnly(ScreenDriver d)
    {
        d.Host.Show(new TitleScreen(d.Host));
        d.Frame();
        d.Click(160, 120);
        d.Click(20, 60);
        var cell = SetupScreen.HeadCell(3);
        d.Click(cell.X + 5, cell.Y + 5);
        d.Type("LetsShow");
        d.Press(Key.Enter);
        d.Click(SetupScreen.ModeGlobe.X + 5, SetupScreen.ModeGlobe.Y + 5);
        d.Move(100, 150); d.Save("01_spielvorbereitung_nur_fahne");
        d.Click(SetupScreen.StartButton.X + 10, SetupScreen.StartButton.Y + 10);
        d.Click(MainMenuScreen.BigTile(3).X + 10, MainMenuScreen.BigTile(3).Y + 10);
        d.Move(0, 0); d.Save("02_tabellen");
        d.Click(MainMenuScreen.BigTile(4).X + 10, MainMenuScreen.BigTile(4).Y + 10);
        d.Move(0, 0); d.Save("03_paarungen");
        d.Click(MainMenuScreen.BigTile(5).X + 10, MainMenuScreen.BigTile(5).Y + 10);
        d.Move(0, 0); d.Save("04_staerken");
        d.Click(MainMenuScreen.BigTile(2).X + 10, MainMenuScreen.BigTile(2).Y + 10);
        d.Move(0, 0); d.Save("05_statistiken");
    }

    /// <summary>
    /// Spur weltkugel-dosbox: Modus „nur Weltkugel“ nach dem Sprung – Reihe Manager mit offener „Highscore“
    /// (DOSBox <c>dosbox/nur-weltkugel/07</c>) und „WM-Endrundenspiele“ vor den Spielen (<c>24</c>).
    /// </summary>
    [Scenario("weltkugel-dosbox")]
    private static void WorldOnlyDosbox(ScreenDriver d)
    {
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
        d.Click(MainMenuScreen.BigTile(0).X + 10, MainMenuScreen.BigTile(0).Y + 10);
        d.Move(10, 230); d.Save("01_reihe_manager_wie_dosbox_07");
        var menu = d.Host.Current!;
        d.Host.Show(new WorldCupKnockoutScreen(d.Host, menu));
        d.Move(100, 230); d.Save("02_wm_endrunde_wie_dosbox_24");
    }
}
