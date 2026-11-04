using EishockeyManager.Formats;

namespace EishockeyManager.Core.Tests;

/// <summary>
/// Spur nationalteam, Runde 13, Teil 2: Ablauf der WM nach der DOSBox-Aufnahme `runde-12`
/// (<c>docs/analyse/re-runde-13.md</c>, Sitzung 1: A027, A028).
/// </summary>
public class NationalteamRunde13Teil2Tests
{
    /// <summary>
    /// A027: [ORIGINAL] Modus „nur Weltkugel“: Sprung zum ersten WM-Tag, Ernennung, Halt mit leerem Kader, noch kein WM-Spiel
    /// (Datei 0x54D19/0x54D1E; DOSBox FILE11).
    /// </summary>
    [Fact]
    public void A027_WorldOnly_HaltsAtFirstWorldCupDay_WithEmptySquad()
    {
        var game = GameModeTests.Create(7, false, true, 2);
        Assert.Empty(game.Continue());
        Assert.Equal(GameState.FirstWorldCupDay, game.CurrentDay);
        Assert.NotNull(game.NationalCoach);
        Assert.Empty(game.NationalCoach!.Squad);
        Assert.Empty(game.NationalCoach.Lineup().Players);
    }

    /// <summary>
    /// A028: Regressionstest (vorher rot: Halt nur an Spieltagen Deutschlands). [ORIGINAL] Der Bundestrainer ist an jedem
    /// WM-Tag am Zug, auch nach dem Ausscheiden („Spielfrei“, Datei 0x5557F–0x555A3; DOSBox `file19_28`, `file20_02`,
    /// `file20_04`); danach folgt der Saisonwechsel ohne Halt.
    /// </summary>
    [Theory]
    [InlineData(7)]
    [InlineData(11)]
    public void A028_CoachHaltsOnEveryWorldCupDay(int seed)
    {
        var game = GameModeTests.Create(seed, false, true, 2);
        game.Continue();
        game.AutoNominate();
        var halts = new List<int>();
        for (var guard = 0; game.Phase == SeasonPhase.Running; guard++)
        {
            Assert.True(guard < 100);
            halts.Add(game.CurrentDay);
            game.Continue();
        }
        Assert.Equal(SeasonCalendar.DaysOf(DayEvents.WorldCup), halts);
    }
}
