using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Competitions;
using EishockeyManager.UI.Screens.Team;

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

    /// <summary>
    /// R14NT-N1 (Nachprüfung #665): Regressionstest für das Rücksetzen der gemerkten WM-Seite beim Saisonwechsel (Mutation M5a –
    /// Zeile in <c>WorldCupPairingsPages.Of</c> auskommentiert – macht ihn rot: die Kachel zeigte in der neuen Saison Gruppe 2).
    /// [ORIGINAL] Datei 0x5649D–0x564BA: Der Saisonwechsel kopiert die Startwerte 0x5128 … 0x512C nach 0x5123 … 0x5127, die Seite
    /// der Kachel „WM“ ist danach wieder Gruppe 1. Ablauf aus der Nachprüfung: „nur Weltkugel“, „WM“, „Gruppe 2“, Kachel erneut →
    /// Gruppe 2; Saisonende, <c>StartNextSeason</c>, <c>Continue</c>, Kachel → Gruppe 1.
    /// </summary>
    [Fact]
    public void CoachMenu_WorldCupPairings_SeasonChange_ResetsRememberedGroup()
    {
        var game = WorldOnly();
        game.Continue();
        game.AutoNominate();
        var host = new ScreenHost(null!) { Game = game };
        var menu = new Dummy(host);
        var open = MenuEntries.All["WM"];
        game.OnMainMenu(game.Managers[0], isReturn: false);
        var first = Assert.IsType<CupPairingsScreen>(open(host, menu));
        Assert.Equal(0, first.Group);
        host.Show(first);
        var b = CupPairingsScreen.Buttons[2];
        first.Update(new InputState { MouseX = b.X + 2, MouseY = b.Y + 2, LeftClick = true });
        Assert.Equal(1, Assert.IsType<CupPairingsScreen>(host.Current).Group);
        game.OnMainMenu(game.Managers[0], isReturn: true);
        Assert.Equal(1, Assert.IsType<CupPairingsScreen>(open(host, menu)).Group);

        for (var guard = 0; game.Phase != SeasonPhase.SeasonOver; guard++)
        {
            Assert.True(guard < 100, "Saisonende nicht erreicht");
            game.Continue();
        }
        game.StartNextSeason();
        game.Continue();
        game.OnMainMenu(game.Managers[0], isReturn: false);
        Assert.True(game.WorldCupStage < WorldCupKnockoutScreen.CoachStartStage);
        Assert.Equal(0, Assert.IsType<CupPairingsScreen>(open(host, menu)).Group);
    }

    /// <summary>
    /// Hinweis aus #667 (Regressionstest, vorher rot: Rechtsklick auf einen Platz leerte ihn, der Bildschirm blieb offen).
    /// [ORIGINAL] Derselbe Code wie beim Verein (Datei 0x40364): Die rechte Taste (DGROUP 0x5A16 = 3) erreicht Blöcke und Plätze
    /// nicht (Datei 0x40886–0x4088D) und verlässt außerhalb der Listenzeilen den Bildschirm (Datei 0x40D6B–0x40D78). Die linke
    /// Taste wählt die Reihe (Datei 0x40890–0x40943) und leert einen belegten Platz (Datei 0x40AA3–0x40D0D); war „automatisch
    /// aufstellen“ an, schaltet sie es ab und meldet „Auto-Aufstellung / ist deaktiviert !“ (Datei 0x40ACF–0x40AF3).
    /// </summary>
    [GameDataFact]
    public void NationalLineup_RightClickOnSlotLeaves_LeftClickClears()
    {
        var (d, game) = NationalLineupScreenTests.StartWorldOnly();
        game.AutoNominate();
        var coach = game.NationalCoach!;
        coach.AutoFormation();
        var slot = LineupSlot.Defender(0, 0);
        var placed = coach.Formation[slot];
        Assert.NotNull(placed);

        d.OpenTile("Aufstellung");
        Assert.IsType<NationalLineupScreen>(d.Host.Current);
        d.Frame();
        NationalLineupScreenTests.ClickSlot(d, slot, right: true);
        Assert.IsType<MainMenuScreen>(d.Host.Current);
        Assert.Same(placed, coach.Formation[slot]);
        Assert.True(coach.AutoLineupEnabled);

        d.OpenTile("Aufstellung");
        var screen = Assert.IsType<NationalLineupScreen>(d.Host.Current);
        d.Frame();
        NationalLineupScreenTests.ClickSlot(d, slot);
        Assert.IsType<NationalLineupScreen>(d.Host.Current);
        Assert.Null(coach.Formation[slot]);
        Assert.False(coach.IsDressed(placed));
        Assert.Equal(0, screen.SelectedLine);
        Assert.False(coach.AutoLineupEnabled);
        Assert.Equal(["Auto-Aufstellung", "ist deaktiviert !"], screen.Message.Lines);
        d.Frames(100);

        var box = LineupScreen.BlockBox(1);
        d.RightClick(box.X + 1, box.Y + 1); // Block ohne Platz: ebenfalls zurück
        Assert.IsType<MainMenuScreen>(d.Host.Current);
    }
}
