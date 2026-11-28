using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Screens;
using EishockeyManager.UI.Screens.Live;
using EishockeyManager.UI.Screens.Team;

namespace EishockeyManager.UI.Tests;

/// <summary>Spur wettbewerbe-regeln (Runde 22): Anschluss der Kernereignisse an die Bildschirme.</summary>
public class TacticsLeaveWritesUiTests
{
    /// <summary>
    /// A553: [ORIGINAL] Verlassen des Taktikbildschirms ruft 0x48E62 (Aufruf Datei 0x48E19): Vereinsfeld 0x26 = Grundwert +
    /// „Unentschieden“ (Datei 0x4902B–0x4903E), Angriff bleibt – ohne Bedingung, auch ohne Änderung (B31 = A, A959; vorher hier
    /// „nur nach einer Änderung“). Das bloße Öffnen ändert nichts (AGENTS.md §4, PassiveScreenTests).
    /// </summary>
    [GameDataFact]
    public void TacticsScreen_Leave_WritesMatchEffort()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        var t = game.Clubs[game.ActiveManager.ClubIndex].Tactics;
        t.EffortMode = TacticMode.Semi;
        t.SetEffort(0, ScoreSituation.Base, 50);
        t.SetEffort(0, ScoreSituation.Tied, 10);
        t.RecordMatchFields(33, 2);

        d.OpenTile("Taktik");
        Assert.IsType<TacticsScreen>(d.Host.Current);
        Assert.Equal((33, 2), (t.MatchEffort, t.MatchAttack)); // Öffnen ändert nichts
        d.Press(Key.Escape);
        Assert.IsNotType<TacticsScreen>(d.Host.Current);
        Assert.Equal((60, 2), (t.MatchEffort, t.MatchAttack)); // ohne Änderung: 50 + 10

        d.OpenTile("Taktik");
        t.SetEffort(0, ScoreSituation.Tied, 15); // wie ein Klick auf die Pfeile der Zeile „Unentschieden“
        d.Press(Key.Escape);
        Assert.Equal((65, 2), (t.MatchEffort, t.MatchAttack));
    }

    /// <summary>
    /// A808: [ORIGINAL] Status 1 (Strafbank, +0x09 Bit 0 und +0x0D = Minuten, Datei 0x35BFD–0x35C0C) in der Aufstellung im Spiel:
    /// Klick in der Kaderliste → nur „Spieler ist“ / „Gesperrt.“ (Datei 0x40DAF–0x40E2A), Klick auf seinen Platz → „Spieler ist“
    /// / „noch gesperrt !“ (Datei 0x4095E–0x40B55); Aufstellung unverändert. Vorher (rot): Herausnehmen bzw. Platz leeren.
    /// </summary>
    [GameDataFact]
    public void LineupInMatch_PlayerInPenaltyBox_OnlyMessages()
    {
        var d = FlowTests.CreateDriver();
        d.StartNewGame();
        var game = d.Host.Game!;
        game.Options.MatchSpeed = 0;
        d.NextDay();
        while (d.Host.Current is MatchDayScreen)
            d.Frame();
        var live = Assert.IsType<LiveMatchScreen>(d.Host.Current);
        var shown = Assert.Single(live.Session.Shown);
        var side = shown.HomeTeam!.Managed ? 0 : 1;
        var team = side == 0 ? shown.HomeTeam! : shown.AwayTeam!;
        var club = side == 0 ? shown.Home : shown.Away;
        club.AutoLineupEnabled = false;
        // Strafe wie MatchEngine.Penalty (0x35941–0x35963, 0x35BFD–0x35C0C) für einen aufgestellten Stürmer, falls keiner sitzt.
        var slot = LineupSlot.Forward(0, 0);
        var boxed = team.Squad!.Players.Single(p => p.Player == club.Lineup[slot]);
        if (!team.Box.Occupants.Contains(boxed))
        {
            var free = team.Box.Count;
            Assert.True(free < 2);
            team.Box.Add(free, 13);
            boxed.Dressed = false;
            boxed.PenaltyMinutes = 5;
            team.Box.Occupants[free] = boxed;
        }
        Assert.True(MatchEngine.PenaltyBoxMinutes(club, boxed.Player) > 0);

        d.Click(LiveMatchScreen.CrestX(side) + 10, 20);
        var lineup = Assert.IsType<LineupScreen>(d.Host.Current);
        var before = LineupSlot.All.Select(s => club.Lineup[s]).ToList();
        var row = TeamUi.Rows(club).ToList().IndexOf(boxed.Player);
        d.Click(50, LineupScreen.FirstRowY + row * LineupScreen.RowPitch + 2);
        Assert.Equal(["Spieler ist", "Gesperrt."], lineup.MessageLines);
        Assert.Equal(before, LineupSlot.All.Select(s => club.Lineup[s]).ToList());
        for (var i = 0; lineup.MessageLines.Count > 0; i++)
        {
            Assert.True(i < 2000);
            d.Frame();
        }

        var r = LineupScreen.SlotButton(slot);
        d.Click(r.X + 2, r.Y + 2);
        Assert.Equal(["Spieler ist", "noch gesperrt !"], lineup.MessageLines);
        Assert.Equal(before, LineupSlot.All.Select(s => club.Lineup[s]).ToList());
    }

    /// <summary>
    /// A808: [ORIGINAL] Hinweiszeile 0x41683 bei Status 1: „Noch n Minuten Gesperrt.“ mit n = +0x0D (2 bzw. 5, im Spiel nicht
    /// heruntergezählt); +0x0A = 0 auf der Strafbank, also nie „RESERVE“. Vorher (rot): „RESERVE“ bzw. „IM TEAM“.
    /// </summary>
    [Theory]
    [InlineData(2, 2)]
    [InlineData(5, 0)]
    public void HintLine_PenaltyBox_Minutes(int minutes, int line)
    {
        var p = new Player { Name = "A", Role = PlayerRole.Forward };
        Assert.Equal($"Status: Noch {minutes} Minuten Gesperrt.",
            PlayerHintLine.Text(p, 14, new PlayerHintInfo(true, line, minutes), showExhaustion: true));
        Assert.Equal(line == 0 ? "Status: RESERVE" : "Status: IM TEAM",
            PlayerHintLine.Text(p, 14, new PlayerHintInfo(true, line), showExhaustion: true));
    }
}
