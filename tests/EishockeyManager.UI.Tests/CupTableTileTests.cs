using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Competitions;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur tabellen-highscore: die Kacheln „Tabelle DEB-Pokal“ und „Tabelle Europapokal“.
/// [ORIGINAL] Beide rufen in em.exe (Hauptmenü Datei 0x45829 ff.) dieselben Bildschirmfunktionen auf wie die
/// Kacheln „DEB-Pokal“ und „Europapokal“ der Reihe Paarungen: „Tabelle DEB-Pokal“ → Stub 0xE8:0x75 (0, 0)
/// (Datei 0x4584A → 0x45897, Bildschirm „DEB-Pokal Auslosung“, Datei 0x33B83), „Tabelle Europapokal“ → Stub 0xF8:0x3E
/// mit dem Seitenwert DGROUP 0x5123 = 3 (gleicher Wert wie DGROUP 0x5126 der Paarungen: Europacup Gruppe 1).
/// </summary>
public class CupTableTileTests
{
    [Fact]
    public void Tiles_AreRegistered_WithoutPlaceholder()
    {
        Assert.True(MenuEntries.All.ContainsKey("Tabelle DEB-Pokal"));
        Assert.True(MenuEntries.All.ContainsKey("Tabelle Europapokal"));
    }

    [GameDataFact]
    public void TableDebCup_ShowsCurrentRoundDraw_WithoutChangingState()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        var before = StateFingerprint.Of(game);

        d.OpenTile("Tabelle DEB-Pokal");
        d.Frame();
        var screen = Assert.IsType<DebCupDrawScreen>(d.Host.Current);
        Assert.Equal(game.DebCup!.Draw.Select(c => (int?)c), screen.Slots);

        d.Press(Key.Space);
        Assert.IsType<MainMenuScreen>(d.Host.Current);
        Assert.Equal(before, StateFingerprint.Of(game));
    }

    /// <summary>Nach einem nachgestellten Pokalverlauf zeigt die Kachel den neuen Stand (zweite Runde, 16 Plätze).</summary>
    [GameDataFact]
    public void TableDebCup_FollowsCupProgress()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        var cup = game.DebCup!;
        var first = cup.Draw.ToList();
        while (cup.Rounds.Count < 2 && !game.IsGameOver)
            d.PlayMatchDay();
        Assert.True(cup.Rounds.Count >= 2, "Pokalverlauf nicht bis zur zweiten Runde gespielt");

        d.OpenTile("Tabelle DEB-Pokal");
        d.Frame();
        var screen = Assert.IsType<DebCupDrawScreen>(d.Host.Current);
        Assert.Equal(DebCup.Teams / 2, screen.Slots.Count);
        Assert.NotEqual(first.Count, screen.Slots.Count);
        Assert.Equal(cup.Draw.Select(c => (int?)c), screen.Slots);
    }

    [GameDataFact]
    public void TableEuropeanCup_ShowsGroupOne_WithoutChangingState()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        var before = StateFingerprint.Of(game);

        d.OpenTile("Tabelle Europapokal");
        d.Frame();
        var screen = Assert.IsType<EuropeanCupScreen>(d.Host.Current);
        Assert.Equal(0, screen.View);
        Assert.Same(game.EuropeanCup!.Groups[0], screen.Group);

        d.Press(Key.Escape);
        Assert.IsType<MainMenuScreen>(d.Host.Current);
        Assert.Equal(before, StateFingerprint.Of(game));
    }

    /// <summary>Während der Gruppenphase zeigt die Tabelle die Ergebnisse der gespielten Termine.</summary>
    [GameDataFact]
    public void TableEuropeanCup_ShowsPlayedResults()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        var cup = game.EuropeanCup!;
        while (cup.Groups[0].Results.Count == 0 && !game.IsGameOver)
            d.PlayMatchDay();
        Assert.NotEmpty(cup.Groups[0].Results);

        d.OpenTile("Tabelle Europapokal");
        d.Frame();
        var screen = Assert.IsType<EuropeanCupScreen>(d.Host.Current);
        var table = screen.Group!.Table(game.Clubs);
        Assert.Equal(cup.Groups[0].Teams.Count, table.Count);
        Assert.Equal(cup.Groups[0].Results.Count, table.Sum(r => r.Wins + r.Draws + r.Losses) / 2);
    }
}
