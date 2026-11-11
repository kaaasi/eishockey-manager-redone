namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur abgleich-rest Teil 4 (#699 Rest): Tag 0 im Modus „nur Weltkugel“. [ORIGINAL] Spielbeginn und Saisonwechsel springen im
/// Modus 1 ohne Tageslauf zum ersten WM-Tag (Datei 0x54CEE–0x54D38: Zähler weiter, dann 0x553FF → Schleife 0x55570); der Halt
/// dort ist der gesicherte Stand (FILE11, FILE21), dessen Tageswechsel als gerechnet gilt (Import: <c>DayOpened</c> = true). Zu
/// rechnen gibt es an diesem Tag nichts (<c>OpenWorldOnlyLoopDay</c> erst ab dem Tag danach).
/// </summary>
public class AbgleichRestWorldOnlyTests
{
    [Fact]
    public void WorldOnly_NewGame_JumpToWorldCup_OpensDayWithoutDayRun()
    {
        var game = GameModeTests.Create(11, flag: false, globe: true, 5);
        Assert.Equal(GameMode.WorldOnly, game.Mode);
        var money = game.Managers[0].Money;
        var trained = game.TrainedUntil;

        game.Continue();

        Assert.Equal(GameState.FirstWorldCupDay, game.CurrentDay);
        Assert.True(game.DayOpened);
        Assert.Equal(money, game.Managers[0].Money);
        Assert.Equal(trained, game.TrainedUntil);
    }

    [Fact]
    public void WorldOnly_SeasonChange_JumpToWorldCup_OpensDay()
    {
        var game = GameModeTests.Create(11, flag: false, globe: true, 5);
        game.Continue();
        game.AutoNominate();
        for (var guard = 0; game.Phase == SeasonPhase.Running; guard++)
        {
            Assert.True(guard < 100, "Saisonende nicht erreicht");
            game.Continue();
        }
        game.StartNextSeason();
        game.ReturnToFirstActiveManager();

        game.Continue();

        Assert.Equal(GameState.FirstWorldCupDay, game.CurrentDay);
        Assert.Equal(SeasonPhase.Running, game.Phase);
        Assert.True(game.DayOpened);
    }
}
