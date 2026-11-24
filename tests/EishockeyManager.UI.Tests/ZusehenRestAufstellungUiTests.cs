using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Team;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur zusehen-rest Teil 3, A858: Verlassen der Vereinsaufstellung schreibt die Felder 0x18–0x1D des Managervereins aus der
/// Aufstellung (Datei 0x41513–0x4151A, <see cref="GameState.OnLineupLeft"/>). Vorher rot: die Felder blieben bis zum nächsten Tag
/// mit Ereignis stehen.
/// </summary>
public sealed class ZusehenRestAufstellungUiTests
{
    [GameDataFact]
    public void LeavingChangedLineup_WritesClubFields()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        var club = game.Clubs[game.ActiveManager.ClubIndex];
        var before = MatchEngine.ClubFieldValues(club);
        Assert.Equal(before, Parts(MatchEngine.ClubLineupTeamValues(club)));
        var back = d.Host.Current!;
        d.Host.Show(new LineupScreen(d.Host, back));
        d.Frame();
        var slot = new LineupSlot(0, PlayerRole.Forward, 0);
        Assert.NotNull(club.Lineup[slot]);
        var r = LineupScreen.SlotButton(slot);
        d.Click(r.X + r.W / 2, r.Y + r.H / 2);
        Assert.Null(club.Lineup[slot]); // Platz geleert, Automatik aus
        while (d.Host.Current is LineupScreen screen && screen.MessageLines.Count > 0)
            d.Frame();
        d.Press(Key.Escape);

        Assert.Same(back, d.Host.Current);
        var after = MatchEngine.ClubFieldValues(club);
        Assert.Equal(Parts(MatchEngine.ClubLineupTeamValues(club)), after);
        Assert.NotEqual(before, after);
    }

    /// <summary>
    /// B31 (A892, Runde 25): [ORIGINAL] 0x952C beim Verlassen ohne Bedingung (Datei 0x41513–0x4151A) – auch ohne Änderung schreibt
    /// das Verlassen die Felder aus der Aufstellung (B31 = A, Nutzer 24.11.2026). Vorher (rot): Felder blieben stehen (Prüfung
    /// „nur nach einer Änderung“). Das bloße Öffnen ändert nichts (<see cref="PassiveScreenTests"/>).
    /// </summary>
    [GameDataFact]
    public void LeavingUnchangedLineup_WritesClubFields()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        var club = game.Clubs[game.ActiveManager.ClubIndex];
        club.SetAutoLineup(false); // sonst stellt die Rückkehr ins Menü neu auf (Datei 0x450C6)
        var before = MatchEngine.ClubFieldValues(club);
        foreach (var p in club.Lineup.Block(0))
            p.Technique = Math.Max(1, p.Technique - 30); // Felder lesen die Aufstellung erst beim nächsten Schreiben
        var back = d.Host.Current!;
        d.Host.Show(new LineupScreen(d.Host, back));
        d.Frames(3);
        Assert.Equal(before, MatchEngine.ClubFieldValues(club)); // Öffnen ändert nichts
        d.Press(Key.Escape);

        Assert.Same(back, d.Host.Current);
        var after = MatchEngine.ClubFieldValues(club);
        Assert.NotEqual(before, after);
        Assert.Equal(Parts(MatchEngine.ClubLineupTeamValues(club)), after);
    }

    private static (PartValues, PartValues) Parts(
        (PartValues Defense, PartValues Attack, int) values) => (values.Defense, values.Attack);
}
