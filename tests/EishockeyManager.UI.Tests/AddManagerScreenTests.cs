using EishockeyManager.Tests.Support;
using EishockeyManager.Core;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.ManagerArea;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// #841 (Spur nutzer-befunde, Teil 3): Kachel „Neuer“ öffnet wie im Original die Spielvorbereitung für weitere Manager
/// (em.exe Datei 0x45755 → 0x28165), nicht den Posteingang; der Posteingang bleibt über die Nachrichtenleiste erreichbar.
/// </summary>
public class AddManagerScreenTests
{
    private static int Row(int row) => 40 + row * SetupScreen.SlotPitch;

    [GameDataFact]
    public void NewTile_OpensSetupInAddMode_WithoutChangingState()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        var before = SaveGame.Save(game);

        d.OpenTile("Neuer");

        var setup = Assert.IsType<SetupScreen>(d.Host.Current);
        Assert.True(setup.AddsToRunningGame);
        d.Frames(3);
        // [ORIGINAL] Datei 0x286AD–0x286D7: Klicks auf die vorhandenen Manager bleiben ohne Wirkung, die rechte Seite auch.
        d.Click(20, Row(0) + 5);
        d.Click(90, Row(0) + 5);
        d.Click(SetupScreen.ArrowDown(0).X + 5, SetupScreen.ArrowDown(0).Y + 3);
        d.Click(SetupScreen.ModeGlobeHit.X + 5, SetupScreen.ModeGlobeHit.Y + 5);
        d.Click(SetupScreen.LoadButton.X + 5, SetupScreen.LoadButton.Y + 5);
        Assert.Same(setup, d.Host.Current);
        Assert.Empty(setup.Setup.Managers);
        // „Los jetzt!“ ohne neuen Manager kehrt ohne Änderung zurück (Datei 0x297CA).
        d.Click(SetupScreen.StartButton.X + 10, SetupScreen.StartButton.Y + 10);
        Assert.IsType<MainMenuScreen>(d.Host.Current);
        Assert.Equal(before, SaveGame.Save(game));
    }

    [GameDataFact]
    public void NewTile_AddsManager_WhoTakesTheNextTurn() => AddsManagerViaSetup(reload: false);

    /// <summary>
    /// Review P1-1 (2026-11-14): nach dem Laden eines Spielstands warf „Los jetzt!“ (Managerliste des Lesers war ein Array).
    /// </summary>
    [GameDataFact]
    public void NewTile_AfterLoad_AddsManager() => AddsManagerViaSetup(reload: true);

    private static void AddsManagerViaSetup(bool reload)
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        if (reload)
        {
            var loaded = SaveGame.Load(SaveGame.Save(d.Host.Game!));
            Assert.True(loaded.Success, loaded.Error);
            d.Host.Game = loaded.Game!;
        }
        var game = d.Host.Game!;
        var own = game.Managers[0].ClubIndex;

        d.OpenTile("Neuer");
        var setup = Assert.IsType<SetupScreen>(d.Host.Current);
        d.Click(20, Row(1) + 5); // neue Zeile → Kopfauswahl
        var head = SetupScreen.HeadCell(5);
        d.Click(head.X + 5, head.Y + 5);
        d.Type("Zweiter");
        d.Press(Key.Enter);
        // Ohne Verein nimmt „Los jetzt!“ keinen Klick an (Datei 0x29617–0x29693).
        d.Click(SetupScreen.StartButton.X + 10, SetupScreen.StartButton.Y + 10);
        Assert.Same(setup, d.Host.Current);
        Assert.Single(game.Managers);

        d.Click(150, Row(1) + 5); // Logo → Vereinsauswahl in Satzfolge des Spiels
        var club = game.ClubRecordOrder()[1];
        Assert.NotEqual(own, club);
        var name = game.Clubs[club].Name;
        var cell = SetupScreen.ClubCell(1);
        d.Click(cell.X + 5, cell.Y + 5);
        d.Click(SetupScreen.StartButton.X + 10, SetupScreen.StartButton.Y + 10);

        Assert.IsType<MainMenuScreen>(d.Host.Current);
        Assert.Equal(2, game.Managers.Count);
        Assert.Equal("Zweiter", game.Managers[1].Name);
        Assert.Equal(5, game.Managers[1].Head);
        // Spur neuer-manager (#847): [ORIGINAL] Platztausch in die Start-Liga (Datei 0x29C3B–0x29CF7, 0x36169 Schalter 0x80) –
        // der gewählte Verein (Kennung) steht danach auf dem gezogenen Platz; diese Saat zieht einen anderen als den eigenen.
        var placed = game.Managers[1].ClubIndex;
        Assert.NotEqual(club, placed);
        Assert.Equal(name, game.Clubs[placed].Name);
        Assert.Equal(game.Setup.StartLeague, game.Clubs[placed].League);
        Assert.Equal(0, game.CurrentManager);
        Assert.NotEmpty(game.Clubs[placed].Players);
        Assert.Equal(2, game.Setup.Managers.Count);
        var saved = SaveGame.Save(game);
        Assert.Equal(saved, SaveGame.Save(SaveGame.Load(saved).Game!));
    }

    [GameDataFact]
    public void AddMode_ClubOfExistingManager_CannotBeChosen()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        var own = game.Managers[0].ClubIndex;
        var place = game.ClubRecordOrder().ToList().IndexOf(own);

        d.OpenTile("Neuer");
        var setup = Assert.IsType<SetupScreen>(d.Host.Current);
        d.Click(20, Row(1) + 5);
        var head = SetupScreen.HeadCell(5);
        d.Click(head.X + 5, head.Y + 5);
        d.Type("Zweiter");
        d.Press(Key.Enter);
        d.Click(150, Row(1) + 5);
        for (var p = 0; p < place / SetupScreen.ClubsPerPage; p++)
            d.Wheel(-1);
        var cell = SetupScreen.ClubCell(place % SetupScreen.ClubsPerPage);
        d.Click(cell.X + 5, cell.Y + 5);

        Assert.Null(setup.Setup.Managers[0].ClubIndex);
    }

    [Fact]
    public void QuestionHint_PointsToTheNewsBar()
    {
        Assert.Equal("(Klicken)", NewsBar.QuestionHint(null!));
    }
}
