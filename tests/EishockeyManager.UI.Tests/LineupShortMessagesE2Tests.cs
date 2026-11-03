using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens.Team;
using EishockeyManager.UI.Testing;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur kader-transfer Teil 4 (E2, docs/analyse/re-runde-12.md): Kurzmeldungen der Aufstellung wie in em.exe
/// Datei 0x40B37–0x40B55 (Klick auf den Platz) und 0x40E01–0x40EB9 (Klick auf die Kaderliste).
/// </summary>
public class LineupShortMessagesE2Tests
{
    private static (ScreenDriver Driver, LineupScreen Screen, Club Club) Open()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        d.OpenTile("Aufstellung");
        var screen = Assert.IsType<LineupScreen>(d.Host.Current);
        var club = d.Host.Game!.Clubs[d.Host.Game.ActiveManager.ClubIndex];
        club.AutoLineupEnabled = false;
        return (d, screen, club);
    }

    private static void ClickSlot(ScreenDriver d, LineupSlot slot)
    {
        var b = LineupScreen.SlotButton(slot);
        d.Click(b.X + 5, b.Y + 5);
    }

    private static void ClickRow(ScreenDriver d, Club club, Player p)
    {
        var rows = TeamUi.Rows(club).ToList();
        d.Click(60, LineupScreen.FirstRowY + rows.IndexOf(p) * LineupScreen.RowPitch + 2);
    }

    private static Player FreeForward(Club club) =>
        TeamUi.Rows(club).First(p => p.Role == PlayerRole.Forward && p.Available && !club.Lineup.Contains(p));

    /// <summary>[ORIGINAL] Datei 0x40E01: Gesperrter aus der Liste → „Spieler ist“ / „Gesperrt.“ (DGROUP 0xB88).</summary>
    [GameDataFact]
    public void List_Suspended_ShowsSpielerIstGesperrt()
    {
        var (d, screen, club) = Open();
        var p = FreeForward(club);
        p.SuspendedGames = 2;
        var before = club.Lineup[LineupSlot.Forward(0, 0)];
        ClickSlot(d, LineupSlot.Forward(0, 0));
        ClickRow(d, club, p);
        Assert.Equal(["Spieler ist", "Gesperrt."], screen.MessageLines);
        Assert.True(screen.MessageIsShort);
        Assert.Same(before, club.Lineup[LineupSlot.Forward(0, 0)]);
        Assert.NotSame(p, club.Lineup[LineupSlot.Forward(0, 0)]);
    }

    /// <summary>[ORIGINAL] Datei 0x40E01: Verletzter aus der Liste → „Spieler ist“ / „Verletzt.“ (DGROUP 0xB84), Kurzmeldung.</summary>
    [GameDataFact]
    public void List_Injured_ShowsSpielerIstVerletzt()
    {
        var (d, screen, club) = Open();
        var p = FreeForward(club);
        p.InjuredGames = 3;
        ClickSlot(d, LineupSlot.Forward(0, 0));
        ClickRow(d, club, p);
        Assert.Equal(["Spieler ist", "Verletzt."], screen.MessageLines);
        Assert.True(screen.MessageIsShort);
    }

    /// <summary>[ORIGINAL] Datei 0x40B37–0x40B55: Klick auf einen Spieler im Feld mit Restdauer ≠ 0 → „Spieler ist“ / „noch gesperrt !“, keine Aktion.</summary>
    [GameDataTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void Field_ClickOnSuspendedOrInjured_ShowsNochGesperrt_AndKeepsPlayer(bool injured)
    {
        var (d, screen, club) = Open();
        var p = FreeForward(club);
        var slot = LineupSlot.Forward(0, 0);
        Assert.Equal(LineupResult.Assigned, club.AssignToLineup(slot, p));
        if (injured)
            p.InjuredGames = 4;
        else
            p.SuspendedGames = 2;
        ClickSlot(d, slot);
        Assert.Equal(["Spieler ist", "noch gesperrt !"], screen.MessageLines);
        Assert.True(screen.MessageIsShort);
        Assert.Same(p, club.Lineup[slot]);
    }

    /// <summary>Gesunder Spieler im Feld: kein Meldungsfenster (Klick wählt den Platz wie bisher).</summary>
    [GameDataFact]
    public void Field_ClickOnHealthyPlayer_ShowsNoMessage()
    {
        var (d, screen, club) = Open();
        var p = FreeForward(club);
        var slot = LineupSlot.Forward(0, 0);
        club.AssignToLineup(slot, p);
        ClickSlot(d, slot);
        Assert.Empty(screen.MessageLines);
    }

    /// <summary>[ORIGINAL] Datei 0x40ACF–0x40AF3 vor 0x40B55: Bei eingeschalteter Automatik erst „Auto-Aufstellung / ist deaktiviert !“, danach die zweite Meldung.</summary>
    [GameDataFact]
    public void Auto_DeactivationMessage_ComesFirst_ThenSecondMessage()
    {
        var (d, screen, club) = Open();
        var p = FreeForward(club);
        var slot = LineupSlot.Forward(0, 0);
        club.AssignToLineup(slot, p);
        p.SuspendedGames = 2;
        club.AutoLineupEnabled = true;
        ClickSlot(d, slot);
        Assert.False(club.AutoLineupEnabled);
        Assert.Equal(["Auto-Aufstellung", "ist deaktiviert !"], screen.MessageLines);
        d.Frames(100);
        Assert.Equal(["Spieler ist", "noch gesperrt !"], screen.MessageLines);
        d.Frames(100);
        Assert.Empty(screen.MessageLines);
    }
}
