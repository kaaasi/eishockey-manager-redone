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

    /// <summary>[APPROX] Ohne Änderung kein Ereignis: bloßes Öffnen und Verlassen lässt die Felder stehen (AGENTS.md, E4).</summary>
    [GameDataFact]
    public void LeavingUnchangedLineup_KeepsClubFields()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        var club = game.Clubs[game.ActiveManager.ClubIndex];
        var before = MatchEngine.ClubFieldValues(club);
        foreach (var p in club.Lineup.Block(0))
            p.Technique = Math.Max(1, p.Technique - 30); // Felder lesen die Aufstellung erst beim nächsten Schreiben
        var back = d.Host.Current!;
        d.Host.Show(new LineupScreen(d.Host, back));
        d.Frames(3);
        d.Press(Key.Escape);

        Assert.Same(back, d.Host.Current);
        Assert.Equal(before, MatchEngine.ClubFieldValues(club));
        Assert.NotEqual(before, Parts(MatchEngine.ClubLineupTeamValues(club)));
    }

    private static (PartValues, PartValues) Parts(
        (PartValues Defense, PartValues Attack, int) values) => (values.Defense, values.Attack);
}
