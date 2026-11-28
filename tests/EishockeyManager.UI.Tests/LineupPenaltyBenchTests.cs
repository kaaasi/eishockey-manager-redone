using EishockeyManager.Core;
using EishockeyManager.Formats;
using EishockeyManager.Tests.Support;
using EishockeyManager.UI.Testing;
using EishockeyManager.UI.Screens.Competitions;
using EishockeyManager.UI.Screens.Live;
using EishockeyManager.UI.Screens.Team;

namespace EishockeyManager.UI.Tests;

/// <summary>
/// Spur aufstellung Teil 3. Aufräumkonzept K2: [ORIGINAL] Die Strafbank-Abfrage der Aufstellung im Spiel (Datei 0x40DAF–0x40E27)
/// gilt für den Bundestrainer wie für einen Verein (Beleg `k2_strafbank_bundestrainer.py`, Status 1 → „Spieler ist“ / „Gesperrt.“).
/// Vorher (rot): der Klick nahm den Spieler heraus. B13 (A757): Rechtsklick in die Namensspalte öffnet die Spielerdaten des
/// WM-Kaderplatzes (Beleg `b13_nationalaufstellung_rechtsklick.py`); vorher (rot): der Spieler wurde eingesetzt.
/// </summary>
public sealed class LineupPenaltyBenchTests
{
    [GameDataFact]
    public void B13_NationalLineup_RightClickOnName_OpensPlayerData()
    {
        var (d, game) = NationalLineupScreenTests.StartWorldOnly();
        game.AutoNominate();
        var coach = game.NationalCoach!;
        d.OpenTile("Aufstellung");
        var screen = Assert.IsType<NationalLineupScreen>(d.Host.Current);
        d.Frame();
        var before = StateFingerprint.Of(game);
        var row = coach.Rows.ToList().FindIndex(p => !coach.IsDressed(p));
        Assert.True(row >= 0);
        // Zeile k aus der Hinweiszeilen-Rechnung 0x41683 (Zeilen 6 hoch ab y 29), x 0x3C wie im Beleg.
        d.RightClick(0x3C, LineupScreen.FirstRowY + row * LineupScreen.RowPitch + 2);

        Assert.True(screen.Info.Visible);
        Assert.Same(coach.SlotOf(coach.Rows[row]), screen.Info.Player);
        Assert.False(coach.IsDressed(coach.Rows[row]));
        Assert.Same(screen, d.Host.Current);
        Assert.Equal(before, StateFingerprint.Of(game));
    }

    [GameDataFact]
    public void NationalLineupInMatch_PlayerInPenaltyBox_OnlyMessages()
    {
        var (d, game, coach) = NationalCoachLiveInterventionScreenTests.StartWorldCup();
        var (live, match, side) = NationalCoachLiveInterventionScreenTests.RunToCoachMatch(d, coach);
        var team = side == 0 ? match.HomeTeam! : match.AwayTeam!;
        var nation = side == 0 ? match.Home : match.Away;
        coach.SwitchOffAutoLineup();
        // Strafe wie MatchEngine.Penalty (0x35941–0x35963, 0x35BFD–0x35C0C) für einen aufgestellten Stürmer, falls keiner sitzt.
        var slot = LineupSlot.All.First(s => s.Role == PlayerRole.Forward && coach.Formation[s] is not null);
        var boxed = team.Squad!.Players.Single(p => p.Player == coach.Formation[slot]);
        if (!team.Box.Occupants.Contains(boxed))
        {
            var free = team.Box.Count;
            Assert.True(free < 2);
            team.Box.Add(free, 13);
            boxed.Dressed = false;
            boxed.PenaltyMinutes = 5;
            team.Box.Occupants[free] = boxed;
        }
        Assert.True(MatchEngine.PenaltyBoxMinutes(nation, boxed.Player) > 0);

        d.Click(LiveMatchScreen.CrestX(side) + 10, 20);
        var lineup = Assert.IsType<NationalLineupScreen>(d.Host.Current);
        Assert.True(lineup.InMatch);
        var before = LineupSlot.All.Select(s => coach.Formation[s]).ToList();
        var row = coach.Rows.ToList().IndexOf(boxed.Player);
        Assert.True(row >= 0);
        Assert.Same(coach.SlotOf(coach.Rows[row]), boxed.Player);
        d.Click(50, LineupScreen.FirstRowY + row * LineupScreen.RowPitch + 2);
        Assert.Equal(["Spieler ist", "Gesperrt."], lineup.Message.Lines);
        Assert.Equal(before, LineupSlot.All.Select(s => coach.Formation[s]).ToList());
        for (var i = 0; lineup.Message.Lines.Count > 0; i++)
        {
            Assert.True(i < 2000);
            d.Frame();
        }

        // Klick auf seinen Platz: „noch gesperrt !“ (Datei 0x4095E–0x40B55), Platz bleibt.
        var r = LineupScreen.SlotButton(slot);
        d.Click(r.X + 2, r.Y + 2);
        Assert.Equal(["Spieler ist", "noch gesperrt !"], lineup.Message.Lines);
        Assert.Equal(before, LineupSlot.All.Select(s => coach.Formation[s]).ToList());
    }
}
