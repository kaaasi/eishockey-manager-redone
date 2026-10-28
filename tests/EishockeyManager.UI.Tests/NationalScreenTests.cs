using EishockeyManager.Core;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Competitions;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Paket „wettbewerbe“, PR 6: „Spieler in WM-Kader“ und „WM Vorbereitung“ (Bundestrainer). Kein Videobeleg –
/// geprüft werden Bedienung nach em.exe und dass bloßes Öffnen, Überfahren und Blättern nichts verändert.
/// </summary>
public class NationalScreenTests
{
    [Fact]
    public void Layout_FromEmExe()
    {
        Assert.Equal(new Rect(21, 28, 42, 42), FriendliesScreen.Tile(0));
        Assert.Equal(new Rect(150, 114, 42, 42), FriendliesScreen.Tile(11));
        Assert.Equal(new Rect(193, 168, 106, 19), FriendliesScreen.Row(7));
        Assert.Equal(new Rect(217, 165, Widgets.ButtonWidth, Widgets.ButtonHeight), NationalSquadScreen.ButtonRect(2));
        Assert.Equal(["TOR", "ABW", "STU"], Enum.GetValues<Formats.PlayerRole>().Select(NationalSquadScreen.Role));
    }

    private static (ScreenDriver Driver, GameState Game) Start()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        game.AppointNationalCoach(game.Managers[0]);
        return (d, game);
    }

    [GameDataFact]
    public void Squad_OpenScrollHover_ChangesNothing_ClicksNominate()
    {
        var (d, game) = Start();
        var before = StateFingerprint.Of(game);
        var screen = new NationalSquadScreen(d.Host, new MainMenuScreen(d.Host));
        d.Host.Show(screen);
        d.Frame();
        d.Click(NationalSquadScreen.ButtonRect(2).X + 5, NationalSquadScreen.ButtonRect(2).Y + 5);
        Assert.Equal(NationalSquadScreen.Rows, screen.First);
        d.Click(NationalSquadScreen.ButtonRect(0).X + 5, NationalSquadScreen.ButtonRect(0).Y + 5);
        Assert.Equal(0, screen.First);
        d.Move(100, 60);
        Assert.Equal(before, StateFingerprint.Of(game));

        var row = NationalSquadScreen.List.Y + 2 * NationalSquadScreen.RowPitch + 3;
        var candidate = screen.At(100, row)!;
        Assert.Same(screen.Candidates[2].Player, candidate.Player);
        d.Click(100, row);
        Assert.True(game.NationalCoach!.Contains(candidate.Player));
        d.Click(100, row);
        Assert.False(game.NationalCoach.Contains(candidate.Player));

        d.Click(NationalSquadScreen.ButtonRect(1).X + 5, NationalSquadScreen.ButtonRect(1).Y + 5);
        Assert.Equal(NationalCoach.MaxSquad, game.NationalCoach.Squad.Count);
        d.Frame();
        d.Press(Key.Escape);
        Assert.IsType<MainMenuScreen>(d.Host.Current);
    }

    [GameDataFact]
    public void Friendlies_HoverSelectPlay_ToggleHome_LimitMessage()
    {
        var (d, game) = Start();
        game.AutoNominate();
        var screen = new FriendliesScreen(d.Host, new MainMenuScreen(d.Host));
        d.Host.Show(screen);
        var before = StateFingerprint.Of(game);

        var own = Enumerable.Range(0, WorldCup.Teams).First(i => screen.Nations[i] == NationalTeams.Germany);
        var other = (own + 1) % WorldCup.Teams;
        var tile = FriendliesScreen.Tile(other);
        d.Move(FriendliesScreen.Tile(own).X + 5, FriendliesScreen.Tile(own).Y + 5);
        Assert.Null(screen.Hovered); // das eigene Land ist gesperrt
        d.Move(tile.X + 5, tile.Y + 5);
        Assert.Equal(screen.Nations[other], screen.Hovered);
        d.Click(tile.X + 5, tile.Y + 5);
        Assert.Equal(screen.Nations[other], screen.Selected);
        d.Move(5, 120);
        Assert.Null(screen.Selected); // Maus verlässt das Land: Wahl verfällt
        d.Click(FriendliesScreen.InfoBox.X + 20, FriendliesScreen.InfoBox.Y + 10);
        Assert.False(screen.Home);
        d.Click(FriendliesScreen.InfoBox.X + 20, FriendliesScreen.InfoBox.Y + 10);
        Assert.True(screen.Home);
        d.Frame();
        Assert.Equal(before, StateFingerprint.Of(game));

        game.NationalAccount = 10_000_000; // Konto des Bundestrainers (Spur finanzen-2)
        for (var i = 0; i < NationalCoach.MaxFriendlies; i++)
        {
            d.Move(5, 120);
            d.Click(tile.X + 5, tile.Y + 5);
            d.Click(tile.X + 5, tile.Y + 5);
            Assert.Equal(i + 1, game.NationalCoach!.Friendlies.Count);
        }
        Assert.Equal(10_000_000 - 8 * NationalCoach.FriendlyCost(screen.Nations[other], true), game.NationalAccount);
        d.Click(tile.X + 5, tile.Y + 5);
        d.Click(tile.X + 5, tile.Y + 5);
        Assert.Equal(["Nicht mehr als", "8 Freundschaftsspiele"], screen.Message.Lines);
        d.Frame();
    }

    [GameDataFact]
    public void Friendlies_NotEnoughMoney_ShowsMessage()
    {
        var (d, game) = Start();
        var screen = new FriendliesScreen(d.Host, new MainMenuScreen(d.Host));
        d.Host.Show(screen);
        game.NationalAccount = 1_000;
        var i = Enumerable.Range(0, WorldCup.Teams).First(i => screen.Nations[i] != NationalTeams.Germany);
        var t = FriendliesScreen.Tile(i);
        d.Click(t.X + 5, t.Y + 5);
        d.Click(t.X + 5, t.Y + 5);
        Assert.Equal(["Sie haben nicht", "genug Geld !"], screen.Message.Lines);
        Assert.Empty(game.NationalCoach!.Friendlies);
        Assert.Equal(1_000, game.NationalAccount);
    }
}
