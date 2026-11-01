using EishockeyManager.Core;
using EishockeyManager.UI;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Competitions;
using EishockeyManager.UI.Screens.Team;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.Tools.Scenarios;

/// <summary>Spur nationalteam (Runde 8): Taktik des Nationalteams ohne Startwert (#192); Welle 3 (nationalteam-2): Paarungsansicht WM (#194).</summary>
internal static class Nationalteam
{
    /// <summary>
    /// Modus 1 („nur Weltkugel“): Taktik nach „auto“ vor dem ersten WM-Spiel (alle Werte 0, wie DOSBox
    /// `nur-weltkugel/26`) und nach dem ersten WM-Spiel (Werte der Automatik dieses Spiels).
    /// </summary>
    [Scenario("nationalteam-taktik")]
    private static void Tactics(ScreenDriver d)
    {
        d.Host.Show(new TitleScreen(d.Host));
        d.Frame();
        d.Click(160, 120);
        d.Click(20, 60);
        var cell = SetupScreen.HeadCell(3);
        d.Click(cell.X + 5, cell.Y + 5);
        d.Type("Joerg");
        d.Press(Key.Enter);
        d.Click(SetupScreen.ModeFlag.X + 5, SetupScreen.ModeFlag.Y + 5);
        d.Click(SetupScreen.StartButton.X + 10, SetupScreen.StartButton.Y + 10); // #195: Halt am ersten WM-Termin
        var game = d.Host.Game!;
        game.AutoNominate();
        game.NationalCoach!.AutoFormation();
        d.OpenTile("Aufstellung");
        d.Click(TeamUi.SwitchButton.X + 5, TeamUi.SwitchButton.Y + 5);
        d.Move(300, 235); d.Save("01_taktik_vor_dem_ersten_spiel_wie_dosbox_26");
        d.Press(Key.Escape);

        d.PlayMatchDay();
        d.OpenTile("Aufstellung");
        d.Click(TeamUi.SwitchButton.X + 5, TeamUi.SwitchButton.Y + 5);
        d.Move(300, 235); d.Save("02_taktik_nach_dem_ersten_wm_spiel");
    }

    /// <summary>
    /// #194, Modus 1 nach „Los jetzt!“: Paarungen → WM wie DOSBox `nur-weltkugel/20` (Gruppe 1, 1. Spieltag), `21` (Pfeil
    /// rechts: 2. Spieltag) und `24` („Endrunde“ vor den Spielen); dazu die Anzeige im Spielablauf mit den Begegnungen aus
    /// Video `bundestrainer_00437` (Gruppe 1, 60. Min.: Schweden–USA 3:3, Frankreich–Russland 1:5, Norwegen–Kanada 4:5).
    /// </summary>
    [Scenario("cup-paarungen")]
    private static void CupPairings(ScreenDriver d)
    {
        d.Host.Show(new TitleScreen(d.Host));
        d.Frame();
        d.Click(160, 120);
        d.Click(20, 60);
        var cell = SetupScreen.HeadCell(3);
        d.Click(cell.X + 5, cell.Y + 5);
        d.Type("Joerg");
        d.Press(Key.Enter);
        d.Click(SetupScreen.ModeFlag.X + 5, SetupScreen.ModeFlag.Y + 5);
        d.Click(SetupScreen.StartButton.X + 10, SetupScreen.StartButton.Y + 10);
        d.OpenTile("WM");
        d.Move(275, 147); d.Save("01_paarungen_wm_wie_dosbox_20");
        d.Click(CupPairingsScreen.Buttons[3].X + 5, CupPairingsScreen.Buttons[3].Y + 5);
        d.Move(275, 147); d.Save("02_spieltag2_wie_dosbox_21");
        d.Click(CupPairingsScreen.Buttons[1].X + 5, CupPairingsScreen.Buttons[1].Y + 5);
        d.Move(275, 147); d.Save("03_endrunde_wie_dosbox_24");
        d.Click(WorldCupKnockoutScreen.Button.X + 5, WorldCupKnockoutScreen.Button.Y + 5);
        d.Move(275, 147); d.Save("04_zurueck_gruppe1");

        var game = d.Host.Game!;
        int Nation(string name) => game.Clubs.Single(c => c.Index >= NationalTeams.First && c.Index <= NationalTeams.Last
            && string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)).Index;
        CupPairingRow[] video =
        [
            new(Nation("Schweden"), Nation("USA"), 3, 3),
            new(Nation("Frankreich"), Nation("Russland"), 1, 5),
            new(Nation("Norwegen"), Nation("Kanada"), 4, 5),
        ];
        d.Host.Show(new CupPairingsScreen(d.Host, new MainMenuScreen(d.Host), 0, 0, video, liveMinute: 60, trophies: true));
        d.Move(319, 239); d.Save("05_live_wie_video_00437");
    }

    /// <summary>
    /// WM-Statistik (0x3ADE1): Modus 1 nach „Los jetzt!“ wie DOSBox `nur-weltkugel/17` (vor der ersten WM) und mit den drei
    /// WMs und der Länderfolge aus Video `eisbaeren_f07_03843` (Spalten aus dem Bild abgelesen, Punkte 22/21/18/14/13/8/7/4).
    /// </summary>
    [Scenario("wm-statistik")]
    private static void WorldCupStatistics(ScreenDriver d)
    {
        d.Host.Show(new TitleScreen(d.Host));
        d.Frame();
        d.Click(160, 120);
        d.Click(20, 60);
        var cell = SetupScreen.HeadCell(3);
        d.Click(cell.X + 5, cell.Y + 5);
        d.Type("Joerg");
        d.Press(Key.Enter);
        d.Click(SetupScreen.ModeFlag.X + 5, SetupScreen.ModeFlag.Y + 5);
        d.Click(SetupScreen.StartButton.X + 10, SetupScreen.StartButton.Y + 10);
        d.OpenTile("WM-Statistik");
        d.Move(319, 239); d.Save("01_wm_statistik_wie_dosbox_17");

        var game = d.Host.Game!;
        int N(string name) => game.Clubs.Single(c => c.Index >= NationalTeams.First && c.Index <= NationalTeams.Last
            && string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)).Index;
        IReadOnlyList<int> Wm(params string[] names) => [.. names.Select(N)];
        IReadOnlyList<IReadOnlyList<int>> rankings =
        [
            Wm("Tschechei", "Russland", "Finnland", "Norwegen", "Kanada", "Schweden", "USA", "Schweiz"),
            Wm("Russland", "Tschechei", "Kanada", "Schweden", "Finnland", "USA", "Norwegen", "Schweiz"),
            Wm("Finnland", "Tschechei", "Russland", "Schweden", "Kanada", "USA", "Schweiz", "Österreich"),
        ];
        var order = Wm("USA", "Tschechei", "Norwegen", "Schweiz", "Frankreich", "Schweden",
            "Finnland", "Russland", "Österreich", "Kanada", "Deutschland", "Italien");
        d.Host.Show(new WorldCupStatisticsScreen(d.Host, new MainMenuScreen(d.Host), rankings, order));
        d.Move(319, 239); d.Save("02_wm_statistik_wie_video_f07_03843");
    }
}
