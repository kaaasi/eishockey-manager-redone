using EishockeyManager.Tests.Support;
using EishockeyManager.Core;
using EishockeyManager.UI.Screens.Team;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Integration Runde 11 (Anschluss aus wettbewerbe-spiel Teil 8): Ein Eingriff in der Aufstellung schaltet die Einstellung
/// „automatisch aufstellen“ ab und meldet das einmal (em.exe Datei 0x40ADE, DGROUP 0x124D).
/// </summary>
public class LineupAutoDisabledMessageTests
{
    [GameDataFact]
    public void ManualChange_DisablesAutoLineup_AndShowsMessageOnce()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        d.OpenTile("Aufstellung");
        var screen = Assert.IsType<LineupScreen>(d.Host.Current);
        var club = d.Host.Game!.Clubs[d.Host.Game.ActiveManager.ClubIndex];
        club.AutoLineupEnabled = true;

        var slot = LineupSlot.Defender(0, 0);
        var b = LineupScreen.SlotButton(slot);
        d.RightClick(b.X + 5, b.Y + 5);
        Assert.Null(club.Lineup[slot]);
        Assert.False(club.AutoLineupEnabled);
        Assert.Equal(["Auto-Aufstellung", "ist deaktiviert !"], screen.MessageLines);

        d.Frames(100); // Integration Runde 11 (ui-meldungen, K2): Kurzmeldung 0x0C0B8 ohne Knopf, schließt von selbst (Klick wirkt nicht)
        Assert.Empty(screen.MessageLines);
        var slot2 = LineupSlot.Defender(0, 1);
        var b2 = LineupScreen.SlotButton(slot2);
        d.RightClick(b2.X + 5, b2.Y + 5);
        Assert.Null(club.Lineup[slot2]);
        Assert.Empty(screen.MessageLines); // schon aus: keine zweite Meldung
    }
}
