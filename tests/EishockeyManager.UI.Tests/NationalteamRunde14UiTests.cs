using EishockeyManager.Core;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Competitions;

namespace EishockeyManager.UI.Tests;

/// <summary>Spur nationalteam, Runde 14, Teil 2: Nebenbefunde #580 im Menü des Bundestrainers.</summary>
public class NationalteamRunde14UiTests
{
    private static MenuTile Tile(string name) => MenuTiles.Small.SelectMany(r => r).Single(t => t.Name == name);

    /// <summary>Modus „nur Weltkugel“ auf synthetischer Datenbank (ohne Originaldaten).</summary>
    private static GameState WorldOnly()
    {
        var data = new byte[Formats.ClubDatabase.FileLength];
        for (var i = 0; i < Formats.ClubDatabase.ClubsWithSquad; i++)
        {
            System.Text.Encoding.ASCII.GetBytes($"VEREIN {i}").CopyTo(data, Formats.ClubDatabase.SquadOffset + i * Formats.ClubDatabase.SquadRecordLength);
            data[Formats.ClubDatabase.StrengthOffset + i * 3] = (byte)(100 - i % 12 * 5);
        }
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "A", Head = 0, ClubIndex = 2 });
        setup.ToggleClubCompetitions();
        var game = NewGame.Create(Formats.ClubDatabase.Parse(data), setup, 22);
        Assert.Equal(GameMode.WorldOnly, game.Mode);
        return game;
    }

    /// <summary>
    /// #580: Regressionstest (vorher rot: „Transfer“ und „Training“ blieben während der WM offen). [ORIGINAL] Datei
    /// 0x4509E–0x450A4: Sperrbytes DGROUP 0x518C/0x518D = WM-Stufe 0x7AE. Nach der Ernennung vor dem ersten Termin offen
    /// (`file11_03`), ab dem ersten gespielten Termin gesperrt (`file19_26`: Aufst., Taktik offen, drei gesperrt, ein Fisch);
    /// in der neuen Saison wieder offen.
    /// </summary>
    [Fact]
    public void CoachMenu_TransferTraining_LockedFromFirstWorldCupDate()
    {
        var game = WorldOnly();
        game.Continue(); // Sprung zur WM, Ernennung
        Assert.NotNull(game.NationalCoach);
        foreach (var name in new[] { "Aufstellung", "Taktik", "Transfer", "Training" })
            Assert.False(NationalMenu.IsLocked(game, Tile(name)), name);
        Assert.True(NationalMenu.IsLocked(game, Tile("Verträge")));

        game.AutoNominate();
        game.Continue(); // erster WM-Termin
        Assert.True(game.NationalCoachMenu);
        foreach (var name in new[] { "Aufstellung", "Taktik" })
            Assert.False(NationalMenu.IsLocked(game, Tile(name)), name);
        foreach (var name in new[] { "Verträge", "Transfer", "Training" })
            Assert.True(NationalMenu.IsLocked(game, Tile(name)), name);

        while (game.Phase == SeasonPhase.Running)
            game.Continue();
        game.StartNextSeason();
        game.Continue();
        Assert.NotNull(game.NationalCoach);
        Assert.False(NationalMenu.IsLocked(game, Tile("Transfer")));
        Assert.False(NationalMenu.IsLocked(game, Tile("Training")));
    }

    private sealed class Dummy(ScreenHost host) : Screen(host)
    {
        public override void Update(InputState input) { }
        public override void Draw(Surface surface) { }
    }

    /// <summary>
    /// A293 (Sitzung 5, Review #649 R14RD2-02): Regressionstest (vorher rot: „Paarungen → WM“ begann immer mit Gruppe 1).
    /// [ORIGINAL] Im Zug des Bundestrainers öffnet die Kachel nach den fünf Gruppenterminen (WM-Stufe 5, Datei 0x44B15–0x44B1C)
    /// „WM-Endrundenspiele“ (DOSBox `file49_7`); „Gruppe“ führt zu „Gruppe 1   5.Spieltag“ (`file49_8`). Davor die Gruppenseite.
    /// Review #662 (R14NT-05, L11): nur bei Stufe = 5 und nur beim ersten Aufbau eines Zugs (Datei 0x45961 → 0x44FBD umgeht
    /// 0x44A48); sonst die gemerkte Seite (Datei 0x458A2–0x458BB). Bei Stufe 6 also die zuletzt gezeigte Gruppe.
    /// </summary>
    [Fact]
    public void CoachMenu_WorldCupPairings_OpensKnockoutFromStageFive()
    {
        var game = WorldOnly();
        game.Continue();
        game.AutoNominate();
        var host = new ScreenHost(null!) { Game = game };
        var menu = new Dummy(host);
        var open = MenuEntries.All["WM"];
        while (game.WorldCupStage < WorldCupKnockoutScreen.CoachStartStage)
        {
            Assert.IsType<CupPairingsScreen>(open(host, menu));
            game.Continue();
        }
        Assert.True(game.NationalCoachMenu);
        var knockout = Assert.IsType<WorldCupKnockoutScreen>(open(host, menu));
        host.Show(knockout);
        var b = WorldCupKnockoutScreen.Button;
        knockout.Update(new InputState { MouseX = b.X + 2, MouseY = b.Y + 2, LeftClick = true });
        var group = Assert.IsType<CupPairingsScreen>(host.Current);
        Assert.Equal("Gruppe 1   5.Spieltag", group.Title);

        game.Continue(); // Viertelfinale gespielt: Stufe 6, neuer Zug
        Assert.Equal(WorldCupKnockoutScreen.CoachStartStage + 1, game.WorldCupStage);
        var remembered = Assert.IsType<CupPairingsScreen>(open(host, menu)); // gemerkt: Gruppe 1 (vorher rot: Endrunde)
        Assert.Equal(0, remembered.Group);
    }

    /// <summary>
    /// R14NT-05, Gegenbeispiel (4) aus Review #662: Regressionstest (vorher rot: wieder die Endrunde). Stufe 5, erster Aufbau →
    /// Endrunde; „Gruppe“ → „Gruppe 1   5.Spieltag“; Rückkehr ins Hauptmenü (<c>OnMainMenu(isReturn: true)</c>) und die Kachel
    /// erneut → [ORIGINAL] die gemerkte Gruppenseite (Datei 0x45961 → 0x44FBD umgeht 0x44A48, Datei 0x458A2–0x458BB).
    /// Die gemerkte Seite ist Zustand der Oberfläche: Spielstand und Spielzustand bleiben unberührt.
    /// </summary>
    [Fact]
    public void CoachMenu_WorldCupPairings_ReturnInSameTurn_KeepsRememberedGroup()
    {
        var game = WorldOnly();
        game.Continue();
        game.AutoNominate();
        var host = new ScreenHost(null!) { Game = game };
        var menu = new Dummy(host);
        var open = MenuEntries.All["WM"];
        while (game.WorldCupStage < WorldCupKnockoutScreen.CoachStartStage)
            game.Continue();
        game.OnMainMenu(game.Managers[0], isReturn: false);
        var knockout = Assert.IsType<WorldCupKnockoutScreen>(open(host, menu));
        host.Show(knockout);
        var b = WorldCupKnockoutScreen.Button;
        knockout.Update(new InputState { MouseX = b.X + 2, MouseY = b.Y + 2, LeftClick = true });
        Assert.Equal("Gruppe 1   5.Spieltag", Assert.IsType<CupPairingsScreen>(host.Current).Title);

        game.OnMainMenu(game.Managers[0], isReturn: true);
        var before = SaveGame.Save(game);
        var again = Assert.IsType<CupPairingsScreen>(open(host, menu));
        Assert.Equal("Gruppe 1   5.Spieltag", again.Title);
        Assert.Equal(before, SaveGame.Save(game));
    }
}
