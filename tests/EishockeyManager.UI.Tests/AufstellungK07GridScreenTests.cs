using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Team;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur <c>aufstellung</c>, Teil 2 K07 (4), A971/A972 im Bildschirm: Kennzeichen „sechster Platz offen“ ist Zustand des
/// Aufstellungsbildschirms (<see cref="LineupScreen.Grid"/>, Beleg <c>k07_sechster.py</c> Fälle a und d).
/// </summary>
public sealed class AufstellungK07GridScreenTests
{
    private static (ScreenDriver Driver, Club Club, Player Sixth) OpenWithSixth()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var club = TeamUi.OwnClub(d.Host);
        club.AutoLineupEnabled = false;
        club.ClearLineupSlot(LineupSlot.Goalkeeper);
        int? line = 0;
        var sixth = club.Players.First(p => p.Role == PlayerRole.Defender && p.Available && !club.Lineup.Contains(p));
        Assert.Equal(LineupResult.Assigned, club.ClickPlayer(sixth, ref line, right: false));
        d.Host.Show(new LineupScreen(d.Host, new MainMenuScreen(d.Host)));
        d.Frame();
        return (d, club, sixth);
    }

    /// <summary>
    /// Fall a: Aufbau mit sechs in Reihe 1 ohne Torhüter → Kennzeichen 1 (Datei 0x403DB–0x404FD); Klick auf Sturmzelle 0 → Kennzeichen
    /// 0, der Knopf der Sturmzelle zeigt die Nummer des sechsten (Datei 0x40B95). Fall d: neuer Aufbau → Kennzeichen 0, der
    /// Knopf der Sturmzelle ist leer, der sechste bleibt aufgestellt und eingesetzt.
    /// </summary>
    [GameDataFact]
    public void ClearForwardCell_ShowsSixthThere_RebuildHidesHim()
    {
        var (d, club, sixth) = OpenWithSixth();
        var screen = Assert.IsType<LineupScreen>(d.Host.Current);
        Assert.True(screen.Grid.SixthOpen);
        var cell = LineupScreen.SlotButton(LineupSlot.Forward(0, 0), sixthOpen: true);

        d.Click(cell.X + cell.W / 2, cell.Y + cell.H / 2);

        Assert.False(screen.Grid.SixthOpen);
        Assert.Same(sixth, screen.Grid.PlayerAt(club.Lineup, LineupSlot.Forward(0, 0)));
        Assert.Null(club.Lineup[LineupSlot.Forward(0, 0)]);
        Assert.Equal(1, club.Lineup.PositionOf(sixth));

        d.Host.Show(new LineupScreen(d.Host, new MainMenuScreen(d.Host)));
        d.Frame();
        var rebuilt = Assert.IsType<LineupScreen>(d.Host.Current);
        Assert.False(rebuilt.Grid.SixthOpen);
        Assert.Null(rebuilt.Grid.PlayerAt(club.Lineup, LineupSlot.Forward(0, 0)));
        Assert.Same(sixth, club.Lineup.SixthSkater);
        Assert.True(club.IsDressed(sixth));
        Assert.Equal(default, LineupScreen.SlotButton(LineupSlot.SixthSkater, rebuilt.Grid.SixthOpen));
    }
}
