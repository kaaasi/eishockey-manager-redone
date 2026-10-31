using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur befunde-p3: Klickzone Managerkopf (#216) und Bundestrainer-Warnung (#217) im Hauptmenü
/// (em.exe Datei 0x4549D–0x454F2).
/// </summary>
public class MainMenuBefundeP3Tests
{
    private static GameState Synthetic()
    {
        var data = new byte[Formats.ClubDatabase.FileLength];
        for (var i = 0; i < Formats.ClubDatabase.ClubsWithSquad; i++)
        {
            System.Text.Encoding.ASCII.GetBytes($"VEREIN {i}").CopyTo(data, Formats.ClubDatabase.SquadOffset + i * Formats.ClubDatabase.SquadRecordLength);
            data[Formats.ClubDatabase.StrengthOffset + i * 3] = (byte)(100 - i % 12 * 5);
        }
        var setup = new GameSetup();
        setup.Managers.Add(new ManagerSetup { Name = "A", Head = 0, ClubIndex = 8 });
        setup.Managers.Add(new ManagerSetup { Name = "B", Head = 1, ClubIndex = 2 });
        return NewGame.Create(Formats.ClubDatabase.Parse(data), setup, 3);
    }

    private static void PlayToNationalTurn(GameState game)
    {
        for (var guard = 0; !game.NationalTurn; guard++)
        {
            Assert.True(guard < 3000, "Kein Zug des Bundestrainers");
            foreach (var m in game.Managers)
                m.Money = Math.Max(m.Money, 10_000_000);
            game.Continue();
        }
    }

    private static (ScreenDriver Driver, MainMenuScreen Menu) Open(GameState game)
    {
        var d = FlowTests.CreateDriver();
        d.Host.Game = game;
        var menu = new MainMenuScreen(d.Host);
        d.Host.Show(menu);
        d.Frame();
        return (d, menu);
    }

    /// <summary>Regressionstest #216: Vorher blieb der Klick auf den Managerkopf ohne Wirkung.</summary>
    [Fact]
    public void ClickOnManagerHead_ShowsNewsBar_EvenWithoutNewMessage_AndChangesNoState()
    {
        var game = Synthetic();
        var (d, menu) = Open(game);
        Assert.False(menu.NewsBarVisible);
        var before = StateFingerprint.Of(game);

        d.Click(250, 50);
        Assert.True(menu.NewsBarVisible);
        Assert.Equal(before, StateFingerprint.Of(game));
    }

    /// <summary>[ORIGINAL] 232 &lt; x &lt; 272 und 33 &lt; y &lt; 72 (0x4549D, 0x4548D–0x45495).</summary>
    [Theory]
    [InlineData(232, 50, false)]
    [InlineData(233, 34, true)]
    [InlineData(271, 71, true)]
    [InlineData(272, 50, false)]
    [InlineData(250, 33, false)]
    [InlineData(250, 72, false)]
    public void ManagerHead_Edges(int x, int y, bool hit)
    {
        var (d, menu) = Open(Synthetic());
        d.Click(x, y);
        Assert.Equal(hit, menu.NewsBarVisible);
    }

    /// <summary>Regressionstest #217: Vorher lief der Tag trotz Kader unter 10 Spielern weiter.</summary>
    [Fact]
    public void Coach_WithFewerThan10Players_GetsWarning_AndDayDoesNotAdvance()
    {
        var game = Synthetic();
        PlayToNationalTurn(game);
        Assert.True(game.NationalCoachMenu);
        Assert.True(game.NationalCoach!.Squad.Count < MainMenuScreen.CoachMinPlayers);
        var (d, menu) = Open(game);
        var before = StateFingerprint.Of(game);

        d.Click(290, 50);
        Assert.True(menu.Message.Visible);
        Assert.Equal(MainMenuScreen.CoachWarning, menu.Message.Lines);
        Assert.Same(menu, d.Host.Current);
        Assert.Equal(before, StateFingerprint.Of(game));

        d.Click(10, 10); // schließt die Meldung (und löst nichts weiter aus)
        Assert.False(menu.Message.Visible);
        Assert.Same(menu, d.Host.Current);
    }

    [Fact]
    public void Coach_WithEnoughPlayers_ContinuesWithoutWarning()
    {
        var game = Synthetic();
        PlayToNationalTurn(game);
        game.AutoNominate();
        Assert.True(game.NationalCoach!.Squad.Count >= MainMenuScreen.CoachMinPlayers);
        var (d, menu) = Open(game);

        d.Click(290, 50);
        Assert.False(menu.Message.Visible);
        Assert.NotSame(menu, d.Host.Current);
    }

    [Fact]
    public void ClubManager_WithoutCoachRole_NeverGetsWarning()
    {
        var game = Synthetic();
        var (d, menu) = Open(game);
        d.Click(290, 50);
        Assert.False(menu.Message.Visible);
    }
}
